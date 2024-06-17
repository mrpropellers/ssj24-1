using TMPro;
using UnityEditor;
using UnityEngine;

namespace Critter3DPixelCamera
{
    [CustomEditor(typeof(PixelatedCamera))]
    public class PixelatedCameraEditor : Editor
    {
        SerializedProperty targetMode;
        SerializedProperty resolutionSynchronization;

        private void OnEnable()
        {
            targetMode = serializedObject.FindProperty("targetMode");
            resolutionSynchronization = serializedObject.FindProperty("resolutionSynchronization");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(16f);

            PixelatedCamera pixelatedCamera = (PixelatedCamera)target;

            if (GUILayout.Button("Refresh In Editor"))
            {
                pixelatedCamera.RefreshInEditor();
            }

            DrawPropertiesExcluding(serializedObject, "m_Script", "targetMode", "resolutionSynchronization");


            EditorGUILayout.PropertyField(targetMode);

            switch ((TargetMode)targetMode.enumValueIndex)
            {
                case TargetMode.LockToTransform:
                    pixelatedCamera.targetTransform = EditorGUILayout.ObjectField("Target Transform", pixelatedCamera.targetTransform, typeof(Transform), true) as Transform;
                    break;
                case TargetMode.Vector3Position:
                    pixelatedCamera.targetPosition = EditorGUILayout.Vector3Field("Target Position", pixelatedCamera.targetPosition);
                    break;
            }

            EditorGUILayout.PropertyField(resolutionSynchronization);

            switch ((ResolutionSyncMode)resolutionSynchronization.enumValueIndex)
            {
                case ResolutionSyncMode.Manual:
                    pixelatedCamera.pixelResolution = EditorGUILayout.Vector2IntField("Pixel Resolution", pixelatedCamera.pixelResolution);
                    break;
                case ResolutionSyncMode.SyncHeightWithWidth:
                    pixelatedCamera.pixelResolution = new Vector2Int(EditorGUILayout.IntField("Pixel Resolution Width", pixelatedCamera.pixelResolution.x), pixelatedCamera.pixelResolution.y);
                    break;
                case ResolutionSyncMode.SyncWidthWithHeight:
                    pixelatedCamera.pixelResolution = new Vector2Int(pixelatedCamera.pixelResolution.x, EditorGUILayout.IntField("Pixel Resolution Height", pixelatedCamera.pixelResolution.y));
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}