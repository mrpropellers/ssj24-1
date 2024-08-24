using System.Collections.Generic;
using System.Runtime.InteropServices;
using Presentation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Simulation
{
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

            if (pendingScores.Length == 0)
            {
                Debug.Log("No more rat scores!");
            }

            foreach (var (ghostOwner, score) in SystemAPI
                         .Query<RefRO<GhostOwner>, RefRW<CharacterScore>>())
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
    [UpdateInGroup(typeof(InitializationSystemGroup))]
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

            Debug.Log("As far as Scott can tell, this never happens.");

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

            if (pendingScores.Length > 0)
            {
                Debug.Log($"Still have scores in q");
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
