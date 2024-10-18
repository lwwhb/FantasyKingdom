using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.SceneManagement;
#endif

namespace Unity.FantasyKingdom
{
    public class VerifyProject : ScriptableObject
    {
        [SerializeField] RenderPipelineAsset defaultRPAsset;
        [SerializeField] RenderPipelineAsset[] qualityRPAssets;
        [SerializeField] InputActionAsset inputActionAsset;
        
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void EnsureInitialize()
        {
            EditorSceneManager.sceneOpening += EditorSceneManagerOnSceneOpening;
        }

        static void EditorSceneManagerOnSceneOpening(string path, OpenSceneMode mode)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VerifyProject>("Assets/Data/VerifyProjectSettings.asset");
            asset.OnEnable();
            EditorApplication.delayCall += () => asset.OnEnable();
        }

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                return;
            }
            
            if (InputSystem.actions != inputActionAsset)
            {
                Debug.Log($"Updating input actions asset.");
                InputSystem.actions = inputActionAsset;
            }
            
            if (GraphicsSettings.defaultRenderPipeline != defaultRPAsset)
            {
                Debug.Log($"Updating default renderpipeline asset.");
                GraphicsSettings.defaultRenderPipeline = defaultRPAsset;
            }

            var qNames = QualitySettings.names;
            var so = new SerializedObject(QualitySettings.GetQualitySettings());
            var qsArray = so.FindProperty("m_QualitySettings");
            var didUpdate = false;
            for (int i = 0, n = qsArray.arraySize; i < n; ++i)
            {
                var qualityLevel = qsArray.GetArrayElementAtIndex(i);
                var customRenderPipeline = qualityLevel.FindPropertyRelative("customRenderPipeline");

                if (customRenderPipeline.objectReferenceValue != qualityRPAssets[i])
                {
                    Debug.Log($"Updating custom renderpipeline asset for quality level {i} {qNames[i]}.");
                    customRenderPipeline.objectReferenceValue = qualityRPAssets[i];
                    didUpdate = true;
                }
            }
            if (didUpdate)
            {
                Debug.Log("Applying quality setting changes.");
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
#endif
    }
}
