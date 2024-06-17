using UnityEngine;

namespace Critter3DPixelCamera
{
	public class CameraAdjuster : MonoBehaviour
	{
		void Start()
		{
			this.transform.parent.localScale = Vector3.one;
		}
		
		[SerializeField] public Transform canvas;
		public void Adjust(Vector2 worldCamViewPoint)
		{
			transform.localPosition = new Vector3(
				- (canvas.localScale.x / 2) + worldCamViewPoint.x * canvas.localScale.x,
				- (canvas.localScale.y / 2) + worldCamViewPoint.y * canvas.localScale.y,
				- 1);
		}
	}
}