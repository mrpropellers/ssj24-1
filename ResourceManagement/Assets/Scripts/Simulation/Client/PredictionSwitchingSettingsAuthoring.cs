using Unity.Entities;

namespace Simulation
{
    public struct PredictionSwitchingSettings : IComponentData
    {
        // public Entity Player;
        // public float PlayerSpeed;

        public float TransitionDurationSeconds;
        public float PredictionSwitchingRadius;
        /// <summary>The margin must be large enough that moving from predicted time to interpolated time does not move the ghost back into the prediction sphere.</summary>
        public float PredictionSwitchingMargin;
    }

    
    [UnityEngine.DisallowMultipleComponent]
    public class PredictionSwitchingSettingsAuthoring : UnityEngine.MonoBehaviour
    {
        // public UnityEngine.GameObject Player;
        // [RegisterBinding(typeof(PredictionSwitchingSettings), "PlayerSpeed")]
        // public float PlayerSpeed;
        [RegisterBinding(typeof(PredictionSwitchingSettings), "TransitionDurationSeconds")]
        public float TransitionDurationSeconds;
        [RegisterBinding(typeof(PredictionSwitchingSettings), "PredictionSwitchingRadius")]
        public float PredictionSwitchingRadius;
        [RegisterBinding(typeof(PredictionSwitchingSettings), "PredictionSwitchingMargin")]
        public float PredictionSwitchingMargin;

        class PredictionSwitchingSettingsBaker : Baker<PredictionSwitchingSettingsAuthoring>
        {
            public override void Bake(PredictionSwitchingSettingsAuthoring authoring)
            {
                PredictionSwitchingSettings component = default(PredictionSwitchingSettings);
                // component.Player = GetEntity(authoring.Player, TransformUsageFlags.Dynamic);
                // component.PlayerSpeed = authoring.PlayerSpeed;
                component.TransitionDurationSeconds = authoring.TransitionDurationSeconds;
                component.PredictionSwitchingRadius = authoring.PredictionSwitchingRadius;
                component.PredictionSwitchingMargin = authoring.PredictionSwitchingMargin;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }

}

