using Unity.Collections;
using Unity.Entities;

namespace Simulation
{
    public struct MarkedForDestroy : IComponentData, IEnableableComponent
    {
        public bool PresentationDisabled;
    }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct DestroyOldProjectilesSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var now = (float)SystemAPI.Time.ElapsedTime;
            foreach (var (projectile, entity) in SystemAPI
                         .Query<RefRO<Projectile>>().WithEntityAccess())
            {
                var timeSinceSpawn = now - projectile.ValueRO.TimeSpawned;
                if (timeSinceSpawn < 5f)
                    continue;
                
                ecb.DestroyEntity(entity);
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct DestroyOnServerSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI
                         .Query<MarkedForDestroy>()
                         .WithAll<MarkedForDestroy>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

namespace Presentation
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class DestroyOnServerSystem : SystemBase 
    {
        protected override void OnUpdate()
        {
            foreach (var (tfLink, destroyMe) in SystemAPI
                         .Query<TransformLink, RefRW<Simulation.MarkedForDestroy>>()
                         .WithAll<Simulation.MarkedForDestroy>())
            {
                if (destroyMe.ValueRO.PresentationDisabled)
                    continue;
                
                tfLink.Root.gameObject.SetActive(false);
                destroyMe.ValueRW.PresentationDisabled = true;
            }
        }
    }
}
