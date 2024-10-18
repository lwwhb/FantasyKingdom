using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unity.FantasyKingdom
{
    public class StatsCanvasController : MonoBehaviour
    {
        public GameObject InputProviderObject;
        
        public GameObject Panel;

        public TextMeshProUGUI Draws, Verts, FPS, CPU, GPU;

        UniversalRenderPipelineAsset urp;

        IInputProvider inputProvider;

        bool gpuToggle = true;
        bool stpToggle = true;
        long vertexCount;
        List<long> vertexCountSamples = new List<long>();
        long drawCallCount;
        List<long> drawCallCountSamples = new List<long>();

        const int sampleCount = 30;
        const float renderScale = 0.5f;
        ProfilerRecorder drawCallsRecorder;
        ProfilerRecorder verticesRecorder;
        ProfilerRecorder cpuMainThreaTimeRecorder;
        ProfilerRecorder gpuFrameTimeRecorder;
        ProfilerRecorder mainThreadTimeRecorder;

        private void Start()
        {
            urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Panel.SetActive(false);
            inputProvider = InputProviderObject.GetComponent<IInputProvider>();
            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
            cpuMainThreaTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render,"CPU Main Thread Frame Time", sampleCount);
            gpuFrameTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time", sampleCount);
            mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", sampleCount);
        }

      
        


        // Update is called once per frame
        void Update()
        {
            if (inputProvider.StatPanelGesture || inputProvider.StatPanelButton) {
                Panel.SetActive(!Panel.activeInHierarchy);
            }
            UpdateStats();
            Draws.text = $"{drawCallCount}";
            Verts.text = $"{(vertexCount / 1000000f):F1} m";
            FPS.text = $"{1/(GetRecorderFrameAverage(mainThreadTimeRecorder) * (1e-9f)):F1}";
            CPU.text = $"{GetRecorderFrameAverage(cpuMainThreaTimeRecorder)*(1e-6f):F1}";
            GPU.text =$"{GetRecorderFrameAverage(gpuFrameTimeRecorder) * (1e-6f):F1}";

        }

        void UpdateStats()
        {
            if (verticesRecorder.Valid == true)
            {
                vertexCountSamples.Add(verticesRecorder.LastValue);
            }

            if (vertexCountSamples.Count > sampleCount)
            {
                vertexCountSamples.RemoveAt(0);
            }


            vertexCount = 0;
            for (int i = 0; i < vertexCountSamples.Count; i++)
            {
                if (vertexCountSamples[i] > vertexCount)
                {
                    vertexCount = vertexCountSamples[i];
                }
            }

            if (drawCallsRecorder.Valid == true)
            {
                drawCallCountSamples.Add(drawCallsRecorder.LastValue);
            }

            if (drawCallCountSamples.Count > sampleCount)
            {
                drawCallCountSamples.RemoveAt(0);
            }

            drawCallCount = 0;
            for (int i = 0; i < drawCallCountSamples.Count; i++)
            {
                if (drawCallCountSamples[i] > drawCallCount)
                {
                    drawCallCount = drawCallCountSamples[i];
                }
            }
        }

        static double GetRecorderFrameAverage(ProfilerRecorder recorder)
        {
            var samplesCount = recorder.Capacity;
            if (samplesCount == 0)
                return 0;

            double r = 0;
            var samples = new List<ProfilerRecorderSample>(samplesCount);
            recorder.CopyTo(samples);
            for (var i = 0; i < samples.Count; ++i)
                r += samples[i].Value;
            r /= samplesCount;

            return r;
        }


       

        public void ToggleResidentDrawer()
        {
            gpuToggle = !gpuToggle;
            urp.gpuResidentDrawerMode = gpuToggle ? GPUResidentDrawerMode.InstancedDrawing : GPUResidentDrawerMode.Disabled;
        }

        public void ToggleSTP()
        {
            stpToggle = !stpToggle;
            urp.renderScale = stpToggle ? renderScale : 1;
        }

        private void OnDestroy()
        {
            urp.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
            urp.renderScale = renderScale;
            drawCallsRecorder.Dispose();
            verticesRecorder.Dispose();
            cpuMainThreaTimeRecorder.Dispose();
            gpuFrameTimeRecorder.Dispose();
        }
    }
    }

