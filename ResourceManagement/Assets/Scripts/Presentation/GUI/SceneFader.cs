using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation
{
    public class Fader : MonoBehaviour
    {
        public static Fader Instance;

        [SerializeField] private float startDelay = 1f;
        [SerializeField] private float time = 1f;

        private Image fadeImage;
        Tweener _fadeInTween;

        private Color solidBlack;
        private Color clearBlack;

        private void Awake()
        {
            // if (Instance != null)
            // {
            //     Debug.LogError("Another instance of the Fader exists!");
            //     Destroy(gameObject);
            // }

            solidBlack = Color.black;
            clearBlack = Color.black;
            clearBlack.a = 0f;

            fadeImage = GetComponentInChildren<Image>();
            FadeOutImmediate(); // Allows us to hide the fade image in editor
            Instance = this;
        }

        private IEnumerator Start()
        {
            yield return StartScene();
        }

        private IEnumerator StartScene()
        {
            yield return new WaitForSeconds(startDelay);
            yield return FadeIn();
        }

        public YieldInstruction FadeIn()
        {

            _fadeInTween = fadeImage.DOFade(0f, time);
            return _fadeInTween.OnComplete(FadeInComplete).WaitForCompletion();
        }

        public YieldInstruction FadeIn(float fadeInTime)
        {
            return fadeImage.DOFade(0f, fadeInTime).OnComplete(FadeInComplete).WaitForCompletion();
        }

        public YieldInstruction FadeOut()
        {
            _fadeInTween.Complete();
            fadeImage.gameObject.SetActive(true);
            return fadeImage.DOFade(1f, time).OnComplete(FadeOutComplete).WaitForCompletion();
        }

        public YieldInstruction FadeOut(float fade = 1.0f)
        {
            fadeImage.gameObject.SetActive(true);
            return fadeImage.DOFade(fade, time).OnComplete(FadeOutComplete).WaitForCompletion();
        }

        public void FadeInInstant()
        {
            fadeImage.color = clearBlack;
            fadeImage.gameObject.SetActive(false);
        }

        public void FadeOutImmediate()
        {
            fadeImage.color = solidBlack;
        }

        private void FadeInComplete()
        {
            fadeImage.gameObject.SetActive(false);
        }

        private void FadeOutComplete()
        {
        }

        private void OnDestroy()
        {
            Instance = null;
        }
    }
}