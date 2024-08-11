using System.Collections.Generic;
using System.Runtime.InteropServices;
using Presentation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Simulation
{
    [BurstCompile]
    public struct CauldronDepositJob : ITriggerEventsJob
    {
        public DynamicBuffer<PendingRatScored> RatScoringBuffer;
        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        public ComponentLookup<Projectile> ProjectileLookup;
        [ReadOnly]
        public ComponentLookup<Cauldron> CauldronLookup;
        
        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity cauldronEntity;
            Entity projectileEntity;
            if (CauldronLookup.TryGetComponent(triggerEvent.EntityA, out var cauldron))
            {
                cauldronEntity = triggerEvent.EntityA;
                projectileEntity = triggerEvent.EntityB;
            }
            else if (CauldronLookup.TryGetComponent(triggerEvent.EntityB, out cauldron))
            {
                cauldronEntity = triggerEvent.EntityB;
                projectileEntity = triggerEvent.EntityA;
            }
            else
            {
                return;
            }

            if (!ProjectileLookup.TryGetComponent(projectileEntity, out var projectile) ||
                projectile.HasScored ||
                !TransformLookup.TryGetComponent(projectileEntity, out var projectileTf))
            {
                return;
            }

            Debug.Log("Rat cauldron dunk detected!");
            projectile.HasScored = true;
            RatScoringBuffer.Add(new PendingRatScored()
            {
                RatEntityScored = projectileEntity,
                OwnerId = projectile.InstigatorNetworkId,
                LocationTriggered = projectileTf.Position,
                CauldronSplashCenter = cauldron.SplashZone
            });
        }
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [StructLayout(LayoutKind.Auto)]
    public partial struct CauldronTriggerProcessingSystem : ISystem
    {
        ComponentLookup<LocalTransform> _tfLookup;
        ComponentLookup<Projectile> _projectileLookup;
        ComponentLookup<Cauldron> _cauldronLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PendingRatScored>();
            _tfLookup = state.GetComponentLookup<LocalTransform>(true);
            _projectileLookup = state.GetComponentLookup<Projectile>(false);
            _cauldronLookup = state.GetComponentLookup<Cauldron>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _cauldronLookup.Update(ref state);
            _projectileLookup.Update(ref state);
            _tfLookup.Update(ref state);
            var ratScoringBuffer = SystemAPI.GetSingletonBuffer<PendingRatScored>();

            state.Dependency = new CauldronDepositJob()
            {
                CauldronLookup = _cauldronLookup, 
                ProjectileLookup = _projectileLookup, 
                TransformLookup = _tfLookup, 
                RatScoringBuffer = ratScoringBuffer
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            state.CompleteDependency();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [StructLayout(LayoutKind.Auto)]
    public partial class UpdateScoreSystem : SystemBase 
    {
        Dictionary<int, int> m_PlayerScores;

        protected override void OnCreate()
        {
            RequireForUpdate<PendingRatScored>();
            m_PlayerScores = new Dictionary<int, int>();
        }

        protected override void OnUpdate()
        {
            var pendingScores = SystemAPI.GetSingletonBuffer<PendingRatScored>();
            if (pendingScores.Length == 0)
                return;
            
            Debug.Log("Processing some rat scores");
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            m_PlayerScores.Clear();
            foreach (var pending in pendingScores)
            {
                if (m_PlayerScores.ContainsKey(pending.OwnerId))
                {
                    m_PlayerScores[pending.OwnerId]++;
                }
                else
                {
                    m_PlayerScores.Add(pending.OwnerId, 1);
                }
            }
            
            foreach (var (ghostOwner, score) in SystemAPI.Query<RefRO<GhostOwner>, RefRW<Score>>())
            {
                if (m_PlayerScores.TryGetValue(ghostOwner.ValueRO.NetworkId, out var playerScore))
                {
                    score.ValueRW.Value += playerScore;
                }
            }
            
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct BroadcastScoreEventsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PendingRatScored>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var pendingScores = SystemAPI.GetSingletonBuffer<PendingRatScored>();
            if (pendingScores.Length == 0)
                return;

            foreach (var score in pendingScores)
            {
                GameEventQueues.Instance.RatsScored.Enqueue(score);
            }
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class CleanUpLastFrameScores : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<GameState>();
            RequireForUpdate<PendingRatScored>();
        }

        protected override void OnUpdate()
        {
            var isServer = World.IsServer();
            var pendingScores = SystemAPI.GetSingletonBuffer<PendingRatScored>();
            var gameStateEntity = SystemAPI.GetSingletonEntity<GameState>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            if (pendingScores.Length > 0)
            {
                Debug.Log($"Clearing {pendingScores.Length} entries from the rat score buffer");
            }

            if (isServer)
            {
                foreach (var rat in pendingScores)
                {
                    ecb.DestroyEntity(rat.RatEntityScored);
                }
            }
            else
            {
                foreach (var rat in pendingScores)
                {
                    var gameObject = EntityManager.GetComponentObject<TransformLink>(rat.RatEntityScored).Root;
                    gameObject.SetActive(false);
                }
            }

            // IMPORTANT: We assume the PendingRatScored buffer is attached to GameState (from GameStateAuthoring)
            //  If this stops being true, the following will break and we will never properly clear the buffer and/or
            //  violate some rule about singleton buffers
            ecb.SetBuffer<PendingRatScored>(gameStateEntity);
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
