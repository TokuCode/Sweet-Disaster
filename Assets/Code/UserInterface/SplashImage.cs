using Code.Helpers.UI;
using UnityEngine;
using System.Collections;
using Code.Networking.Session;

namespace Code.UserInterface
{
    public class SplashImage : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image splashImage;
        [SerializeField] private float splashDuration;
        
        private void Start()
        {
            if (SessionManager.Instance.IsPracticeMode) return;
            StartCoroutine(Splash());
        }

        private IEnumerator Splash()
        {
            var canvasGroup = splashImage.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1;
            yield return new WaitForSeconds(splashDuration);
            UIUtilities.Instance.FadeOut(canvasGroup, UIUtilities.Instance.TransitionDuration);
        }
    }
}