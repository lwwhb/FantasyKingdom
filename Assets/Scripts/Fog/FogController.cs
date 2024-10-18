using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.FantasyKingdom.RTSCameraController;

namespace Unity.FantasyKingdom
{

    public class FogController : MonoBehaviour
    {
        public GameObject VolumeHolder;
        public CinemachineCamera VirtualCamera;
        private Volume freeCamVolume;
        
        public RTSCameraController camController;

        private Volume[] volumes;
        private int currentZoomLevel = 0;

        public float LerpTime = 1;

        private float time = 0;
        List<ScriptableRendererFeature> features;

        private Material _originalHeightFog, _originalCubeFog;

        FullScreenPassRendererFeature HeightPass, CubePass;

        Coroutine lerpRoutine;
        UniversalRenderPipelineAsset urp;
        float originalMaxShadowDist, originalLastBorder, originalCascade2Split;
        Vector2 originalcascade3Split;
        Vector3 originalcascade4Split;

        void Awake()
        {
           
            urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            originalMaxShadowDist = urp.shadowDistance;
            originalLastBorder = urp.cascadeBorder;
            originalCascade2Split = urp.cascade2Split;
            originalcascade3Split = urp.cascade3Split;
            originalcascade4Split = urp.cascade4Split;
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
            camController.OnCameraSettingsChanged += StartLerpFog;
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
            switch (urp.shadowCascadeCount)
            {
                case 2:
                    urp.cascade2Split = Mathf.Lerp(args.prevData.Split1, args.data.Split1, t);
                    break;
                case 3:
                    Vector2 split3;
                    split3.x = Mathf.Lerp(args.prevData.Split1, args.data.Split1, t);
                    split3.y = Mathf.Lerp(args.prevData.Split2, args.data.Split2, t);
                    urp.cascade3Split = split3;
                    break;
                case 4:
                    Vector3 split4;
                    split4.x = Mathf.Lerp(args.prevData.Split1, args.data.Split1, t);
                    split4.y = Mathf.Lerp(args.prevData.Split2, args.data.Split2, t);
                    split4.z = Mathf.Lerp(args.prevData.Split3, args.data.Split3, t);
                    urp.cascade4Split = split4;
                    break;
            }
            urp.cascadeBorder = Mathf.Lerp(args.prevData.CascadeBorder, args.data.CascadeBorder, t);

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
            switch (urp.shadowCascadeCount)
            {
                case 2:
                    urp.cascade2Split = args.data.Split1;
                    break;
                case 3:
                    Vector2 split3;
                    split3.x = args.data.Split1;
                    split3.y = args.data.Split2;
                    urp.cascade3Split = split3;
                    break;
                case 4:
                    Vector3 split4;
                    split4.x = args.data.Split1;
                    split4.y = args.data.Split2;
                    split4.z = args.data.Split3;
                    urp.cascade4Split = split4;
                    break;
            }
            urp.cascadeBorder = args.data.CascadeBorder;
            VirtualCamera.Lens.NearClipPlane = args.data.CameraNearPlane;
            VirtualCamera.Lens.FarClipPlane = args.data.CameraFarPlane;
        }
        
        private void StartLerpFog(object sender, OnCameraSettingsChangedEventArgs args)
        {
            if(lerpRoutine != null)
            {
                StopCoroutine(LerpFogMaterialsForFreeCam(args.prevHeightFog, args.prevCubeFog, args.heightFog, args.CubeFog, 
                    args.settings_index, args.zoomLevel, args.nearClip,args.farClip));
                lerpRoutine = null;
            }
           
            lerpRoutine = StartCoroutine(LerpFogMaterialsForFreeCam(args.prevHeightFog, args.prevCubeFog, 
                args.heightFog,args.CubeFog,args.settings_index,args.zoomLevel, args.nearClip,args.farClip));
        }


        IEnumerator LerpFogMaterialsForFreeCam(Material fromHeight, Material fromCube, Material toHeight, 
            Material toCube, int settingsIndex, int zoomLevel, float near, float far)
        {
            time = 0;
            urp.shadowDistance = 1000;
            urp.shadowDistance = originalMaxShadowDist;
            urp.cascade2Split = originalCascade2Split;
            urp.cascade3Split = originalcascade3Split;
            urp.cascade4Split = originalcascade4Split;
            urp.cascadeBorder = originalLastBorder;
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

        private void OnDisable()
        {
            HeightPass.passMaterial = _originalHeightFog;
            CubePass.passMaterial = _originalCubeFog;
            urp.shadowDistance = originalMaxShadowDist;
            urp.cascade2Split = originalCascade2Split;
            urp.cascade3Split = originalcascade3Split;
            urp.cascade4Split = originalcascade4Split;
            urp.cascadeBorder = originalLastBorder;
        }
    }
}
