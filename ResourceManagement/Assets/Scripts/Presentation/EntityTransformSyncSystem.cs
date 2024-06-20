using Unity.Entities;
using Unity.Transforms;

namespace Presentation
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class EntityGameObjectTransformSynchronizer : SystemBase
    {
        protected override void OnStartRunning()
        {
        }

        protected override void OnUpdate()
        {
            foreach (var (tf, gameObjectLink) in SystemAPI
                         .Query<RefRO<LocalTransform>, TransformLink>())
            {
                gameObjectLink.TransformSetter.EcsTransform = tf.ValueRO;
            }
        }
    }
}
