using UnityEditor;
using UnityEngine;

namespace Critter3DPixelCamera
{

	public class PixelatedCamera : MonoBehaviour
	{
		// ---------------------- Public variables ----------------------

		[SerializeField]
		public bool forceOrthographic;
		
		[Tooltip(Tooltips.TT_CAMERA)] [SerializeField]
		public Camera pixelCamera;

		[Header("Effect settings")]
		[Tooltip(Tooltips.TT_PIXEL_PERFECT)] [SerializeField]
		public bool pixelPerfect = true;
		[Tooltip(Tooltips.TT_SUB_PIXEL_CAMERA)] [SerializeField]
		public bool subPixelCamera = true;
		
		[Header("Pixelation resolution")]
		[Tooltip(Tooltips.TT_RESOLUTION_SYNC_MODE)] [SerializeField]
		public ResolutionSyncMode resolutionSynchronization = ResolutionSyncMode.SyncWidthWithHeight;

		[Tooltip(Tooltips.TT_PIXEL_RESOLUTION)]
		[HideInInspector]
		public Vector2Int pixelResolution = new Vector2Int(640, 360);

		[Header("Target")]
		[Tooltip(Tooltips.TT_TARGET_MODE)] [SerializeField]
		public TargetMode targetMode = TargetMode.LockToTransform;
		
		[Tooltip(Tooltips.TT_TARGET_TRANSFORM)] [HideInInspector]
		public Transform targetTransform;
		
		[Tooltip(Tooltips.TT_TARGET_POSITION)] [HideInInspector]
		public Vector3 targetPosition;
		
		[Tooltip(Tooltips.TT_DISTANCE_FROM_TARGET)] [SerializeField]
		public float distanceFromTarget = 40f;
		
		[Header("Zoom")]
		[Tooltip(Tooltips.TT_RAW_ZOOM)] [SerializeField] [Range(-1f, 1f)]
		public float rawZoom = 1f;
		
		[Tooltip(Tooltips.TT_ENHANCE_ZOOM)] [SerializeField]
		public float enhanceZoom = 5f;

		// ---------------------- Private variables ----------------------

		private const float _CANVAS_HEIGHT = 10f;

		private Camera _canvasCamera;
		private CameraAdjuster _camAdjuster;
		private Transform _canvas;
		private float _previousAspect;
		[HideInInspector] [SerializeField] private Material _canvasMaterial;		

		// ---------------------- Unity Lifecycle Methods ----------------------

		void Awake()
		{ 
			pixelResolution = new Vector2Int(1422, 800);

			if ((_camAdjuster = FindObjectOfType<CameraAdjuster>()) == null)
			{
				Debug.LogError("ERROR: PixelatedCamera did not find CameraAdjuster in scene! Remember to add the prefab to the scene so this works, and check out the documentation for step-by-step guide.");
			}

			if ((_canvasCamera = _camAdjuster.GetComponent<Camera>()) == null)
			{
				Debug.LogError("ERROR: PixelatedCamera did not find Camera component from CameraAdjuster.");
			}

			if (forceOrthographic)
				pixelCamera.orthographic = true;

			_canvas = _camAdjuster.canvas;
			_canvasMaterial = _canvas.GetComponent<Renderer>().material;

			pixelCamera.targetTexture = CreateRenderTexture();
			UpdateCanvasSize();
		}
	
		void Update()
		{
			UpdateAspectRatio();

			UpdateRawZoom();

			UpdateEnhanceZoom();

			UpdateCamPos();
		}
		
		void LateUpdate()
		{
			if (subPixelCamera && pixelPerfect)
				_camAdjuster.Adjust(pixelCamera.WorldToViewportPoint(GetTargetPos()));
		}
		
		// ------------------------- Public Methods  --------------------------

		/// <summary>
		/// Synchronizes values. This method provides an interface from 'PixelatedCameraEditor.cs' to this script.
		/// </summary>
		public void RefreshInEditor() 
		{ 
			#if UNITY_EDITOR
			if (!EditorApplication.isPlaying)
			{
				if ((_camAdjuster = FindObjectOfType<CameraAdjuster>()) == null)
				{
					Debug.LogError("ERROR: PixelatedCamera did not find CameraAdjuster in scene! Remember to add the prefab to the scene so this works, and check out the documentation for step-by-step guide.");
				}

				if ((_canvasCamera = _camAdjuster.GetComponent<Camera>()) == null)
				{
					Debug.LogError("ERROR: PixelatedCamera did not find Camera component from CameraAdjuster.");
				}

				if (pixelCamera.targetTexture == null)
				{
					Debug.LogError("ERROR: No target render texture set on camera.");
				}

				if (forceOrthographic)
					pixelCamera.orthographic = true;
				_canvasCamera.orthographic = true;
				_canvas = _camAdjuster.canvas;
				_canvasCamera.orthographicSize = _CANVAS_HEIGHT / 2f;

				UpdateRenderTextureSize();

				UpdateCamPos();

				UpdateEnhanceZoom();

				UpdateCamPos();

				UpdateCanvasSize();

				_canvasCamera.transform.localPosition = new Vector3(0, 0, -1);
			}
			#endif
		}

		// ------------------------- Private Methods  --------------------------

		private float PixelWorldSize => 2f * pixelCamera.orthographicSize / pixelCamera.pixelHeight;


		private Vector3 GetMovement(Vector3 idealCamPos, float multiplier, bool roundToInt)
		{
			Vector3 worldDiff = idealCamPos - pixelCamera.transform.position;
			Vector3 localDiff = pixelCamera.transform.InverseTransformDirection(worldDiff) / multiplier;

			if (roundToInt)
			{
				localDiff.x = Mathf.RoundToInt(localDiff.x);
				localDiff.y = Mathf.RoundToInt(localDiff.y);
				localDiff.z = Mathf.RoundToInt(localDiff.z);
			}
			
			return localDiff;
		}
		
		
		private void MoveCam(Vector3 movement, float multiplier)
		{
			this.transform.transform.position += pixelCamera.transform.right * movement.x * multiplier
							 + pixelCamera.transform.up * movement.y * multiplier
							 + pixelCamera.transform.forward * movement.z * multiplier;
		}

		private Vector3 GetTargetPos()
		{
			return (targetMode == TargetMode.LockToTransform) ? targetTransform.position : targetPosition;
		}


		private Vector3 GetIdealCamPos()
		{
			return GetTargetPos() + pixelCamera.transform.forward * -distanceFromTarget;
		}


		private void UpdateCamPos()
		{
			float multiplier = pixelPerfect ? PixelWorldSize : 1;
			Vector3 movement = GetMovement(GetIdealCamPos(), multiplier, pixelPerfect);
			MoveCam(movement, multiplier);
		}


		private Vector2Int GetTextureSize()
		{
			float aspect = (_canvasCamera != null) ? _canvasCamera.aspect : _previousAspect;
			if (resolutionSynchronization == ResolutionSyncMode.SyncWidthWithHeight)
				return new Vector2Int(Mathf.RoundToInt(pixelResolution.y * aspect), pixelResolution.y);
				
			if (resolutionSynchronization == ResolutionSyncMode.SyncHeightWithWidth)
				return new Vector2Int(pixelResolution.x, Mathf.RoundToInt(pixelResolution.x / aspect));

			return new Vector2Int(pixelResolution.x, pixelResolution.y);
		}
		

		private RenderTexture CreateRenderTexture()
		{	
			if (pixelCamera?.targetTexture != null)
			{
				pixelCamera.targetTexture.Release();
			}
			
			Vector2Int textureSize = GetTextureSize();
			RenderTexture newTexture = new RenderTexture(textureSize.x, textureSize.y, 32, RenderTextureFormat.ARGB32);
			newTexture.filterMode = FilterMode.Point;
			newTexture.Create();
			
			_canvasMaterial.SetTexture("_LowResTexture", newTexture);

			return newTexture;
		}
		
		
		private void UpdateRenderTextureSize()
		{
			Vector2Int textureSize = GetTextureSize();
			if (pixelCamera.targetTexture != null)
			{
				pixelCamera.targetTexture.Release();
				pixelCamera.targetTexture.width = textureSize.x;
				pixelCamera.targetTexture.height = textureSize.y;
			}
		}
		
		
		private void UpdateCanvasSize()
		{
			Vector2 pixelViewSize = Vector2.one / new Vector2Int(pixelCamera.targetTexture.width, pixelCamera.targetTexture.height);			
			_canvas.localScale = new Vector3(_CANVAS_HEIGHT * _canvasCamera.aspect * (1f + pixelViewSize.x * 2f), _CANVAS_HEIGHT * (1f + pixelViewSize.y * 2f), 1f);
			_previousAspect = _canvasCamera.aspect;
		}


		private void UpdateAspectRatio()
		{
			bool textureChanged = GetTextureSize() != new Vector2Int(pixelCamera.targetTexture.width, pixelCamera.targetTexture.height);
			if (_previousAspect != _canvasCamera.aspect || textureChanged)
			{
				pixelCamera.targetTexture = CreateRenderTexture();
				UpdateCanvasSize();
			}
		}


		private void UpdateRawZoom()
		{
			float halfCanvasHeight = _CANVAS_HEIGHT / 2f;
			if (halfCanvasHeight / this.rawZoom != _canvasCamera.orthographicSize)
			{
				float newZoom = this.rawZoom * halfCanvasHeight;
				
				if (newZoom == 0)
					newZoom = 0.01f;
				
				_canvasCamera.orthographicSize = Mathf.Clamp(newZoom, -halfCanvasHeight, halfCanvasHeight); ;
			}
		}


		private void UpdateEnhanceZoom()
		{
			if (this.enhanceZoom != pixelCamera.orthographicSize)
			{
				pixelCamera.orthographicSize = this.enhanceZoom;
			}
		}
	}
}
