using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FantasyKingdom
{

    [CreateAssetMenu(fileName = "New Camera Settings" ,menuName ="Fantasy Kingdom/Camera Settings", order = 0)]
    public class CameraControllerSettings : ScriptableObject
    {
        public Material FreeCamHeightFog, FreeCamCubeFog;


        public bool IsRestricted = false;
        public List<ZoomLevelData> ZoomLevelData;

        
        [Min(0)]
        public float CameraScreenSideSpeed = 6.0f;
        [Min(0)]
        public float CameraDragSpeed = 6.0f;
        [Min(0)]
        public float CameraRotateSpeed = 2.0f;

        [Min(0)]
        public float CameraZoomSpeed = 4f;


        [Space]
        [Header("Screen Sides")]
        [SerializeField, Min(0)]
        [Tooltip("The size of the Screen Sides Zone in pixels.")]
        public float ScreenSidesZoneSize = 75f;


        [Space]
        [Header("Limits")]

        [SerializeField, Range(0, 89)]
        [Tooltip("The Minimum Tilt of the camera.")]
        public float CameraTiltMin = 15f;

        [SerializeField, Range(0, 89)]
        [Tooltip("The Maximum Tilt of the camera.")]
        public float CameraTiltMax = 75f;

        
       
        public float CameraRotateMin = 0;


      
        public float CameraRotateMax = 180f;

        [SerializeField, Min(0)]
        [Tooltip("The Minimum zoom factor")]
        public float CameraZoomMin = 5;

        [SerializeField, Min(0)]
        [Tooltip("The Maximum zoom factor")]
        public float CameraZoomMax = 200;

        [Space]
        [Header("Smoothing")]
        [SerializeField, Min(0)]
        public float CameraZoomSmoothTime = 7f;

        [SerializeField, Min(0)]
        public float CameraTargetRotateSmoothTime = 4f;

        [Space]
        [Header("Clip Planes")]
        public float NearClip;
        public float FarClip;
    }

    [Serializable]
    public class ZoomLevelData
    {
        public float MaxShadowDistance = 2000;
        public float ZoomAmount;
        public float TiltAmount;
        public float CameraNearPlane = 5.0f;
        public float CameraFarPlane = 1500f;
        public Material HeightFog;
        public Material CubeFog;
    }
}
