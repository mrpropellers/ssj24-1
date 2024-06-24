using Unity.Entities;
using UnityEngine;

namespace Simulation
{
    public struct Cauldron : IComponentData
    {
        public int NumRatsDeposited;
        public int NumRatsAllowed;
    }

    public class CauldronAuthoring : MonoBehaviour
    {
        public class CauldronBaker : Baker<CauldronAuthoring>
        {
            public override void Bake(CauldronAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Cauldron>(entity);
            }
        }
    }
}
