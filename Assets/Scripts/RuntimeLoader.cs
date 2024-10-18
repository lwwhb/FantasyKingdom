using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.FantasyKingdom
{
    public class RuntimeLoader : MonoBehaviour
    {
        public CanvasGroup group;

        IEnumerator Start()
        {
            if (!group.gameObject.activeInHierarchy)
            {
                group.gameObject.SetActive(true);
            }
            if (Application.isEditor)
            {
                StartCoroutine(FadePanel());
                yield break;
            }

            Application.backgroundLoadingPriority = ThreadPriority.High;
            
            for (var i = 1; i < SceneManager.sceneCountInBuildSettings; ++i)
            {
                Debug.Log($"Begin loading scene {i}.");
                
                var op = SceneManager.LoadSceneAsync(i, LoadSceneMode.Additive);
                while (!op.isDone)
                {
                    yield return null;
                }
                
                Debug.Log($"Done loading scene {i}.");
            }

            Application.backgroundLoadingPriority = ThreadPriority.Normal;

            StartCoroutine(FadePanel());
        }

        IEnumerator FadePanel()
        {
            float time = 0;
            while (time<1)
            {
                group.alpha = Mathf.Lerp(1, 0, time / 1);
                time += Time.deltaTime;
                yield return null;
            }
        }
    }
}