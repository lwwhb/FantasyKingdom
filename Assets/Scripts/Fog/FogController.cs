using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.FantasyKingdom.RTSCameraController;
using CameraType = Unity.FantasyKingdom.RTSCameraController.CameraType;

namespace Unity.FantasyKingdom
{

    public class FogController : MonoBehaviour
    {
        public QualitySettingsController qualityController;
        public GameObject VolumeHolder;
        public CinemachineCamera VirtualCamera;
        private Volume freeCamVolume;
        
        public RTSCameraController camController;

        private Volume[] volumes;
        private int currentZoomLevel = 0;
        private int currentSettings;

        public float LerpTime = 1;

        private float time = 0;
        List<ScriptableRendererFeature> features;

        private Material _originalHeightFog, _originalCubeFog;

        FullScreenPassRendererFeature HeightPass, CubePass;

        Coroutine lerpRoutine;
        UniversalRenderPipelineAsset urp;
        int originalCascadeCount;
        float originalMaxShadowDist, originalLastBorder, originalCascade2Split;
        Vector2 originalcascade3Split;
        Vector3 originalcascade4Split;

        Dictionary<int, Tuple<int, float>> lodSettingsDict;

        void OnEnable()
        {
            urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            lodSettingsDict = new();
            lodSettingsDict.Add(QualitySettings.GetQualityLevel(), new Tuple<int, float>(QualitySettings.maximumLODLevel, QualitySettings.lodBias));
            QualitySettings.activeQualityLevelChanged += OnQualitySettingsChanged;
            originalMaxShadowDist = urp.shadowDistance;
            originalLastBorder = urp.cascadeBorder;
            originalCascade2Split = urp.cascade2Split;
            originalcascade3Split = urp.cascade3Split;
            originalcascade4Split = urp.cascade4Split;
            originalCascadeCount = urp.shadowCascadeCount;
       
            volumes = VolumeHolder.GetComponentsInChildren<Volume>();
            
            currentZoomLevel = 0;
            freeCamVolume = GameObject.FindGameObjectWithTag("FreeCamVolume").GetComponent<Volume>();
            var renderer = (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).GetRenderer(0);

            var property = typeof(ScriptableRenderer).GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);

            features = property.GetValue(renderer) as List<ScriptableRendererFeature>;
            HeightPass = (FullScreenPassRendererFeature)features[0];
            _originalHeightFog = HeightPass.passMaterial;
            HeightPass.passMaterial = new Material(_originalHeightFog);
            CubePass = (FullScreenPassRendererFeature)features[1];
            _originalCubeFog = CubePass.passMaterial;
            CubePass.passMaterial = new Material(_originalCubeFog);

            camController.OnZoomHandled += HandleZoomLerp;
            camController.OnZoomDone += ZoomDone;
            camController.OnZoomMidPoint += ForceLodsOnZoomMidPoint;
            camController.OnCameraSettingsChanged += StartLerpFog;
        }

        private void ForceLodsOnZoomMidPoint(object sender,OnZoomMidPointEventArgs args)
        {
            if (ShouldChangeLODSettings())
            {
                QualitySettings.maximumLODLevel = args.zoomLevel;
            }
        }
        private void OnQualitySettingsChanged(int prev, int curr)
        {
            if (!lodSettingsDict.ContainsKey(curr))
                    lodSettingsDict.Add(curr, new Tuple<int, float>(QualitySettings.maximumLODLevel, QualitySettings.lodBias));
            if (currentSettings == (int)CameraType.GameplayCamera && ShouldChangeLODSettings())
            { 
                QualitySettings.lodBias = 50000;
                QualitySettings.maximumLODLevel = currentZoomLevel;
            }        
        }
        private void HandleZoomLerp(object sender,OnZoomHandledEventArgs args)
        {
            float t = args.zoomDelta;
            HeightPass.passMaterial.Lerp(args.prevHeight, args.height,t);
            CubePass.passMaterial.Lerp(args.prevCube, args.cube, t);
            
            // The assumption is that volume for level 0 has priority 0 configured in the scene, level 1 has pri 1, etc.
            // This way when going up we need to only fade in the target volume. When going down we need to only fade out the current volume.
            // Volume for level 0 always keeps its starting weight of 1, to prevent the default volume from kicking in.
            int targetZoomLevel = args.zoomLevel;
            if (targetZoomLevel > currentZoomLevel)
                volumes[targetZoomLevel].weight = Mathf.Lerp(0, 1, t);
            else if (targetZoomLevel < currentZoomLevel)
                volumes[currentZoomLevel].weight = Mathf.Lerp(1, 0, t);
            urp.shadowDistance = Mathf.Lerp(args.prevShadowDist, args.shadowDist, t);
            VirtualCamera.Lens.NearClipPlane = Mathf.Lerp(args.prevData.CameraNearPlane, args.data.CameraNearPlane, t);
            VirtualCamera.Lens.FarClipPlane = Mathf.Lerp(args.prevData.CameraFarPlane, args.data.CameraFarPlane, t);
        }
        private void ZoomDone(object sender, OnZoomDoneEventArgs args)
        {
            int targetZoomLevel = args.zoomLevel;
            if (targetZoomLevel > currentZoomLevel)
                volumes[targetZoomLevel].weight = 1;
            else if (targetZoomLevel < currentZoomLevel)
                volumes[currentZoomLevel].weight = 0;
            currentZoomLevel = args.zoomLevel;
            urp.shadowDistance = args.shadowDist;
            VirtualCamera.Lens.NearClipPlane = args.data.CameraNearPlane;
            VirtualCamera.Lens.FarClipPlane = args.data.CameraFarPlane;
        }
        
        private void StartLerpFog(object sender, OnCameraSettingsChangedEventArgs args)
        {
            if(lerpRoutine != null)
            {
                StopCoroutine(LerpFogMaterialsForFreeCam(args.prevHeightFog, args.prevCubeFog, args.heightFog, args.CubeFog,
                    args.settings_index, args.nearClip, args.farClip, args.shadowDistance, args.zoomLevel));
                lerpRoutine = null;
            }
           
            lerpRoutine = StartCoroutine(LerpFogMaterialsForFreeCam(args.prevHeightFog, args.prevCubeFog,
                args.heightFog, args.CubeFog, args.settings_index, args.nearClip, args.farClip, args.shadowDistance, args.zoomLevel));
        }


        IEnumerator LerpFogMaterialsForFreeCam(Material fromHeight, Material fromCube, Material toHeight,
            Material toCube, int settingsIndex, float near, float far, float shadowDist, int zoomLevel)
        {
            currentSettings = settingsIndex;
            time = 0;
            if (settingsIndex == (int)CameraType.FreeCamera)
            {
                urp.shadowCascadeCount = originalCascadeCount;
                urp.shadowDistance = originalMaxShadowDist;
                urp.cascade2Split = originalCascade2Split;
                urp.cascade3Split = originalcascade3Split;
                urp.cascade4Split = originalcascade4Split;
                urp.cascadeBorder = originalLastBorder;
                int currentQualityIndex = QualitySettings.GetQualityLevel();
                Tuple<int, float> lodData = lodSettingsDict[currentQualityIndex];
                QualitySettings.maximumLODLevel = lodData.Item1;
                QualitySettings.lodBias = lodData.Item2;
            }
            else
            {
                urp.shadowDistance = shadowDist;
                urp.shadowCascadeCount = 1;
                urp.cascadeBorder = 0;
                
                if(ShouldChangeLODSettings())
                {
                    QualitySettings.maximumLODLevel = zoomLevel;
                    QualitySettings.lodBias = 50000;
                }
            }
            float nearClip = VirtualCamera.Lens.NearClipPlane;
            float farClip = VirtualCamera.Lens.FarClipPlane; 
            while (time < LerpTime)
            {
                float t = time / LerpTime;
                HeightPass.passMaterial.Lerp(fromHeight, toHeight, t);
                CubePass.passMaterial.Lerp(fromCube, toCube, t);
                
                // freeCamVolume has higher priority than all the other volumes, so we just fade it in or out
                freeCamVolume.weight = settingsIndex == 1 ? Mathf.Lerp(0, 1, t) : Mathf.Lerp(1, 0, t);
                time += Time.deltaTime;
                VirtualCamera.Lens.NearClipPlane = Mathf.Lerp(nearClip, near, t);
                VirtualCamera.Lens.FarClipPlane = Mathf.Lerp(farClip,far, t);
                yield return null;
            }
            VirtualCamera.Lens.NearClipPlane = near;
            VirtualCamera.Lens.FarClipPlane = far;
            freeCamVolume.weight = settingsIndex == 1 ? 1 : 0;

        }

        private bool ShouldChangeLODSettings()
        {
            return qualityController.currentQualityLevelName.Contains("Desktop") || qualityController.currentQualityLevelName.Contains("High");
        }

        private void OnDisable()
        {
            CleanUpFog();
            urp.shadowDistance = originalMaxShadowDist;
            urp.cascade2Split = originalCascade2Split;
            urp.cascade3Split = originalcascade3Split;
            urp.cascade4Split = originalcascade4Split;
            urp.cascadeBorder = originalLastBorder;
            urp.shadowCascadeCount = originalCascadeCount;
        }

        void CleanUpFog()
        {
            var urpAssets = new UniversalRenderPipelineAsset[QualitySettings.names.Length];
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                urpAssets[i] = QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;
                var renderer = urpAssets[i].GetRenderer(0);

                var property = typeof(ScriptableRenderer).GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);

                features = property.GetValue(renderer) as List<ScriptableRendererFeature>;
                HeightPass = (FullScreenPassRendererFeature)features[0];
                HeightPass.passMaterial = _originalHeightFog;
                CubePass = (FullScreenPassRendererFeature)features[1];
                CubePass.passMaterial = _originalCubeFog;
            }
        }
    }
}
