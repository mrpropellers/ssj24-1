using Unity.Entities;
using UnityEngine;

namespace Simulation
{
    public struct Cauldron : IComponentData
    {
        public int NumRatsDeposited;
        public int NumRatsAllowed;
        public Vector3 SplashZone;
    }

    public class CauldronAuthoring : MonoBehaviour
    {
        public Transform CauldronCenter;
        
        public class CauldronBaker : Baker<CauldronAuthoring>
        {
            public override void Bake(CauldronAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Cauldron() {SplashZone = authoring.CauldronCenter.position});
            }
        }
    }
}
