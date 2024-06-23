using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[GhostComponent]
public struct TestAddedComponent : IComponentData
{
}

[GhostComponent]
public struct TestGhostComponent : IComponentData
{
    [GhostField]
    public double TimeChanged;
    public bool HasComponent;
}

public class TestGhostComponentAuthoring : MonoBehaviour
{
    private class TestGhostComponentAuthoringBaker : Baker<TestGhostComponentAuthoring>
    {
        public override void Bake(TestGhostComponentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);
            AddComponent(entity, new TestGhostComponent() {TimeChanged = 5f});
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial struct TestGhostSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var time = SystemAPI.Time.ElapsedTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (testGhost, entity) in SystemAPI.Query<RefRW<TestGhostComponent>>().WithEntityAccess())
        {
            if (testGhost.ValueRO.TimeChanged + 3 < time)
                continue;

            if (testGhost.ValueRO.HasComponent)
            {
                ecb.RemoveComponent<TestAddedComponent>(entity);
            }
            else
            {
                ecb.AddComponent<TestAddedComponent>(entity);
            }

            testGhost.ValueRW.TimeChanged = time;
            testGhost.ValueRW.HasComponent = !testGhost.ValueRW.HasComponent;
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}