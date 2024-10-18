using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FantasyKingdom
{
    public class ButtonAnimation : MonoBehaviour
    {
        public Color OnColor, OffColor;
        public TextMeshProUGUI OnText, OffText;
        public RectTransform ElipseRect;
        public Image Background;

        public float AnimDuration = 0.25f;

        private bool toggle;

        Coroutine animRoutine;

        public void PlayAnim()
        {
            if(animRoutine !=null)
            {
                return;
            }
            animRoutine= StartCoroutine(DoAnimation());
        }

        IEnumerator DoAnimation()
        {
            float time = 0;
            toggle = !toggle;
            while(time<AnimDuration)
            {
                float t = time / AnimDuration;
                if (toggle)
                {
                    Background.color = Color.Lerp(OnColor, OffColor, t);
                    OnText.alpha = Mathf.Lerp(1, 0, t);
                    OffText.alpha = Mathf.Lerp(0, 1, t);
                    Vector2 elipsePos = ElipseRect.anchoredPosition;
                    elipsePos.x = Mathf.Lerp(18, -18, t);
                    ElipseRect.anchoredPosition = elipsePos;
                }
                else
                {
                    Background.color = Color.Lerp(OffColor, OnColor, t);
                    OnText.alpha = Mathf.Lerp(0, 1, t);
                    OffText.alpha = Mathf.Lerp(1, 0, t);
                    Vector2 elipsePos = ElipseRect.anchoredPosition;
                    elipsePos.x = Mathf.Lerp(-18, 18, t);
                    ElipseRect.anchoredPosition = elipsePos;
                }
                time += Time.deltaTime;
                yield return null;
            }
            //Sanitation 
            if (toggle)
            {
                Background.color = OffColor;
                OnText.alpha = 0;
                OffText.alpha = 1;
                Vector2 elipsePos = ElipseRect.anchoredPosition;
                elipsePos.x = -18;
                ElipseRect.anchoredPosition = elipsePos;
            }
            else
            {
                Background.color = OnColor;
                OnText.alpha = 1;
                OffText.alpha = 0;
                Vector2 elipsePos = ElipseRect.anchoredPosition;
                elipsePos.x = 18;
                ElipseRect.anchoredPosition = elipsePos;
            }
            animRoutine = null;
        }
    }
}
