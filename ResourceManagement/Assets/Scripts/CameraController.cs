using UnityEngine;

namespace QBitDigital.BunnyKnight
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance;

        [SerializeField] Transform follow;
        [SerializeField] float verticalFollowOffset = 0.25f;
        [SerializeField] float followSpeed = 10f;

        private void Awake()
        {
            Instance = this;
        }

        private void LateUpdate()
        {
            MoveCam();
        }

        private void MoveCam()
        {
            if (follow == null) return;

            transform.position = Vector3.Lerp(
                transform.position,
                follow.position + Vector3.up * verticalFollowOffset,
                followSpeed * Time.deltaTime
            );
        }
    }
}


