using UnityEngine;

namespace Critter3DPixelCamera
{
	public class PointerEvents : MonoBehaviour
	{
		private CameraAdjuster _camAdjuster;
		private Transform _canvasParent;
		private Vector3 _canvasTargetLocalPos = new Vector3(0, 0, -1);
		private Quaternion _targetLocalRotation;

		void Awake()
		{
			if ((_camAdjuster = FindObjectOfType<CameraAdjuster>()) == null)
			{
				Debug.LogError("ERROR: PixelatedCamera did not find CameraAdjuster in scene! Remember to add the prefab to the scene so this works, and check out the documentation for step-by-step guide.");
			}
		}
		
		void Start()
		{
			_canvasParent = _camAdjuster.transform.parent;
			_canvasParent.parent = this.transform;
			ResetTransform();
		}
		
		void Update()
		{
			bool positionHasChanged = (_canvasParent.localPosition != _canvasTargetLocalPos) || (_canvasParent != _camAdjuster.transform.parent);

			if (positionHasChanged)
			{
				ResetTransform();
			}
		}
		
		private void ResetTransform()
		{
			_canvasParent.position = this.transform.position;
			_canvasParent.localPosition = _canvasTargetLocalPos;
			_canvasParent.LookAt(this.transform);	
		}
	}
}