using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.Collections.Generic;
using System.Reflection;

namespace Unity.FantasyKingdom
{
    public class QualitySettingsController : MonoBehaviour
    {

        public GameObject QualitySettingsButton;
        
        public TextMeshProUGUI QualitySettingsText;

        public string currentQualityLevelName;

        public GameObject InputProviderholder;

        UniversalRenderPipelineAsset[] urpAssets;

        IInputProvider _inputProvider;

        void Awake()
        {
            _inputProvider = InputProviderholder.GetComponent<IInputProvider>();
            urpAssets = new UniversalRenderPipelineAsset[QualitySettings.names.Length];
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                urpAssets[i] = QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;
            }
            if(QualitySettings.count<2)
            {
                QualitySettingsButton.SetActive(false);
            }
            QualitySettingsText.text = $"Quality Level:\n{QualitySettings.names[QualitySettings.GetQualityLevel()]}";
            int currentQualityLevel = QualitySettings.GetQualityLevel();
            currentQualityLevelName = QualitySettings.names[currentQualityLevel];
        }



        void Update()
        {
            if(_inputProvider.CycleQualityUpButton())
            {
                CycleQuality(1);
            }
            if(_inputProvider.CycleQualityDownButton())
            {
                CycleQuality(-1 + QualitySettings.count);
            }
        }

        void UpdateURPAsset(int qualityLevel)
        {
            Debug.Assert(qualityLevel < urpAssets.Length,"Invalid quality level provided", this);

            UniversalRenderPipelineAsset urpAsset = urpAssets[qualityLevel];
           
            Debug.Assert(urpAsset != null,"Quality Level did not have a render pipeline asset", this);  
            GraphicsSettings.defaultRenderPipeline = urpAsset;
        }
        public void CycleQuality(int dir)
        {
            int currentQualityLevel = QualitySettings.GetQualityLevel();
            int nextQualityLevel = (currentQualityLevel + dir) % QualitySettings.count;
            UpdateFogSettings(nextQualityLevel);
            QualitySettings.SetQualityLevel(nextQualityLevel, true);
            currentQualityLevelName = QualitySettings.names[nextQualityLevel];
            QualitySettingsText.text = $"Quality Level:\n{currentQualityLevelName}";
            UpdateURPAsset(nextQualityLevel);
        }

        void UpdateFogSettings(int nextQuality)
        {
            var prevRenderer = (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).GetRenderer(0);
            var prevProperty = typeof(ScriptableRenderer).GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
            var prevFeatures = prevProperty.GetValue(prevRenderer) as List<ScriptableRendererFeature>;
            var prevHeightPass = (FullScreenPassRendererFeature)prevFeatures[0];
            var prevCubePass = (FullScreenPassRendererFeature)prevFeatures[1];

            var renderer = urpAssets[nextQuality].GetRenderer(0);
            var property = typeof(ScriptableRenderer).GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
            var features = property.GetValue(renderer) as List<ScriptableRendererFeature>;
            var HeightPass = (FullScreenPassRendererFeature)features[0];
            var CubePass = (FullScreenPassRendererFeature)features[1];

            HeightPass.passMaterial = prevHeightPass.passMaterial;
            CubePass.passMaterial = prevCubePass.passMaterial;

        }
    }
}
