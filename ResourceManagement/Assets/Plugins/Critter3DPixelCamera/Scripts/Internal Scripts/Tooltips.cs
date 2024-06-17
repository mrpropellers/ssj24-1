namespace Critter3DPixelCamera
{
	/**
	What should main camera look at?
	
	LockToTransform					locked to a transform (could be anything, for example, player character)
	Vector3Position					look at Vector3 position (you can update this in an update loop for smooth effect)
	*/
	public enum TargetMode
	{
		LockToTransform,
		Vector3Position
	}
	
	/**
	If aspect ratio is changed at runtime, what should happen to low-res pixel resolution?
	
	SyncWidthWithHeight,			adjusts horizontal pixel resolution based on the vertical resolution to fit aspect ratio if necessary
	SyncHeightWithWidth,			adjusts vertical pixel resolution based on the horizontal resolution to fit aspect ratio if necessary
	Manual							no automatic adjustments, the pixel resolution remains fixed
	*/
	public enum ResolutionSyncMode
	{
		SyncWidthWithHeight,
		SyncHeightWithWidth,
		Manual
	}
	
	// Tooltips for Unity editor
	public static class Tooltips
	{
		public const string TT_CAMERA = "Your main camera that is in the world looking at your game. Note that this 'PixelatedCamera' script can also be placed on some \"Camera controller\" object or parent you have"+
		", so that your system does not break and only your controller is moved around.";
		
		public const string TT_PIXEL_PERFECT = "Stationary objects in 3D world actually look stationary without jittering colors or outlines. This does not affect moving objects.";
		
		public const string TT_SUB_PIXEL_CAMERA = "Subpixel adjustments counter the blocky movement of pixel perfect rendering.";
		
		public const string TT_PIXEL_RESOLUTION = "The resolution of the low-res pixel look. Lower values look more pixelated.";
		
		public const string TT_RESOLUTION_SYNC_MODE = "How 'pixelResolution' should behave if screen's aspect ratio is not in sync with it?"+
			"\n\nThis is useful for devices where screen rotations are common, to ensure consistent pixelation look and square pixels. "+
			"Both calculation modes keep perfectly square pixels at all time by keeping 'pixelResolution' always in sync with aspect ratio.";
			
		public const string TT_TARGET_MODE = "Determines if camera points at Vector3 position or Transform";
		
		public const string TT_TARGET_TRANSFORM = "Locks the camera to look have Transform always in the middle of the screen. This works by reference so no updating needed. "+
		"Remember to set Target Mode to 'TRANSFORM' for this to work.";
		
		public const string TT_TARGET_POSITION = "Camera will simply look at this position. Remember to set Target Mode to 'POSITION' for this to work.";
		
		public const string TT_DISTANCE_FROM_TARGET = "How far away the camera is from target.";
		
		public const string TT_RAW_ZOOM = "Zoom where low resolution pixels grow visually larger during zooming in. The resolution of the game appears to stay the same.";

		public const string TT_ENHANCE_ZOOM = "Zoom where final view's low-res pixel count is constant. In other words, zooming in makes the game look like it has higher resolution.\n\n"+
		"This is effectively the same as the pixel camera's orthographic size.";
		
	}
}