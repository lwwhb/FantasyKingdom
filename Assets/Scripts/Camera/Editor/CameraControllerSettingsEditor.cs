using UnityEngine;
using UnityEditor;

namespace Unity.FantasyKingdom
{
    [CustomEditor(typeof(CameraControllerSettings))]
    public class CameraControllerSettingsEditor : Editor
    {
        SerializedProperty isRestrictedProp;
        SerializedProperty zoomLevelDataProp;
        SerializedProperty cameraRotateMinProp;
        SerializedProperty cameraRotateMaxProp;
        SerializedProperty cameraTiltMinProp;
        SerializedProperty cameraTiltMaxProp;
        SerializedProperty freeCamHeightFogProp;
        SerializedProperty freeCamCubeFogProp;

        // Other properties
        SerializedProperty cameraScreenSideSpeedProp;
        SerializedProperty cameraDragSpeedProp;
        SerializedProperty cameraRotateSpeedProp;
        SerializedProperty cameraZoomSpeedProp;
        SerializedProperty screenSidesZoneSizeProp;
        SerializedProperty cameraZoomMinProp;
        SerializedProperty cameraZoomMaxProp;
        SerializedProperty cameraZoomSmoothTimeProp;
        SerializedProperty cameraTargetRotateSmoothTimeProp;
        SerializedProperty nearClip;
        SerializedProperty farClip;
        private void OnEnable()
        {
            isRestrictedProp = serializedObject.FindProperty("IsRestricted");
            zoomLevelDataProp = serializedObject.FindProperty("ZoomLevelData");
            cameraRotateMinProp = serializedObject.FindProperty("CameraRotateMin");
            cameraRotateMaxProp = serializedObject.FindProperty("CameraRotateMax");
            cameraTiltMinProp = serializedObject.FindProperty("CameraTiltMin");
            cameraTiltMaxProp = serializedObject.FindProperty("CameraTiltMax");
            freeCamHeightFogProp = serializedObject.FindProperty("FreeCamHeightFog");
            freeCamCubeFogProp = serializedObject.FindProperty("FreeCamCubeFog");

         
            cameraScreenSideSpeedProp = serializedObject.FindProperty("CameraScreenSideSpeed");
            cameraDragSpeedProp = serializedObject.FindProperty("CameraDragSpeed");
            cameraRotateSpeedProp = serializedObject.FindProperty("CameraRotateSpeed");
            cameraZoomSpeedProp = serializedObject.FindProperty("CameraZoomSpeed");
            screenSidesZoneSizeProp = serializedObject.FindProperty("ScreenSidesZoneSize");
            cameraZoomMinProp = serializedObject.FindProperty("CameraZoomMin");
            cameraZoomMaxProp = serializedObject.FindProperty("CameraZoomMax");
            cameraZoomSmoothTimeProp = serializedObject.FindProperty("CameraZoomSmoothTime");
            cameraTargetRotateSmoothTimeProp = serializedObject.FindProperty("CameraTargetRotateSmoothTime");
            nearClip = serializedObject.FindProperty("NearClip");
            farClip = serializedObject.FindProperty("FarClip");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(isRestrictedProp);
            if (isRestrictedProp.boolValue)
            {
                EditorGUILayout.PropertyField(zoomLevelDataProp);
                EditorGUILayout.PropertyField(cameraRotateMinProp);
                EditorGUILayout.PropertyField(cameraRotateMaxProp);
            }
            else
            {
                EditorGUILayout.PropertyField(cameraTiltMinProp);
                EditorGUILayout.PropertyField(cameraTiltMaxProp);
                EditorGUILayout.PropertyField(freeCamHeightFogProp);
                EditorGUILayout.PropertyField(freeCamCubeFogProp);
            }
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Common Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(cameraScreenSideSpeedProp);
            EditorGUILayout.PropertyField(cameraDragSpeedProp);
            EditorGUILayout.PropertyField(cameraRotateSpeedProp);
            EditorGUILayout.PropertyField(cameraZoomSpeedProp);
            EditorGUILayout.PropertyField(screenSidesZoneSizeProp);
            EditorGUILayout.PropertyField(cameraZoomMinProp);
            EditorGUILayout.PropertyField(cameraZoomMaxProp);
            EditorGUILayout.PropertyField(cameraZoomSmoothTimeProp);
            EditorGUILayout.PropertyField(cameraTargetRotateSmoothTimeProp);

            if(!isRestrictedProp.boolValue)
            {
                EditorGUILayout.PropertyField(nearClip);
                EditorGUILayout.PropertyField(farClip);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
