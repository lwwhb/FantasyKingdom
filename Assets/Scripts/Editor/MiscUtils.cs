using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Code.Editor
{
    public static class MiscUtils
    {
      
        [MenuItem("Fantasy Kingdom/Load Demo Scenes")]
        static void LoadDemoScenes()
        {
            var sceneSetups = EditorBuildSettings.scenes.Select(scene => new SceneSetup { path = scene.path, isLoaded = true }).ToArray();
            sceneSetups[1].isActive = true;
            EditorSceneManager.RestoreSceneManagerSetup(sceneSetups);
        }

    }
}
