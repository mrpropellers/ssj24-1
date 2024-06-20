using System;
using Unity.Transforms;
using UnityEngine;

namespace Presentation
{
    public class TransformSetter : MonoBehaviour
    {
        // Cache the TF to avoid a crazy amount of lookups
        Transform m_Tf;
        Vector3 m_Velocity = Vector3.zero;
        float m_AngularVelocity = 0f;
        
        public LocalTransform EcsTransform { get; set; }
        
        [SerializeField]
        bool ApplySmoothing;
        [SerializeField, Range(0f, 1f)]
        float LinearSmoothingTime = 0.1f;
        [SerializeField, Range(0f, 1f)] 
        float AngularSmoothingTime = 0.02f;

        void Start()
        {
            m_Tf = transform;
        }

        void Update()
        {
            if (!ApplySmoothing)
            {
                m_Tf.position = EcsTransform.Position;
                m_Tf.rotation = EcsTransform.Rotation;
                return;
            }

            // We damp the updates coming from our Systems to smooth out prediction jitter
            // TODO: Optimize by porting to a System.Update
            //  I think there are SystemGroups that only execute once per MonoBehaviour.Update where we could
            //  move this code so it can be parallelized
            m_Tf.position = Vector3.SmoothDamp(
                m_Tf.position, EcsTransform.Position, ref m_Velocity, LinearSmoothingTime);
            var targetRotation = (Quaternion)EcsTransform.Rotation;
            var delta = Quaternion.Angle(transform.rotation, targetRotation);
            if (delta > 0f)
            {
                var t = Mathf.SmoothDampAngle(delta, 0f, ref m_AngularVelocity, AngularSmoothingTime);
                t = 1.0f - (t / delta);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
            }
        }
    }
}
