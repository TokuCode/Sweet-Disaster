using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Code.Helpers.Singleton;

namespace Code.Helpers.UI
{
    public class UIUtilities : Singleton<UIUtilities>
    {
        // Private members
        [Header("Tweening duration")]
        [SerializeField] private float tweenDuration;
        [SerializeField] private float transitionDuration;
        
        [Header("Pop message references")]
        [SerializeField] private GameObject messageGameObject;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button okBtn;
        
        [Header("Transition panel")]
        [SerializeField] private CanvasGroup transitionPanel;
        
        // Public members
        public float TransitionDuration => transitionDuration;
        public GameObject MessageGameObject => messageGameObject;
        public CanvasGroup TransitionPanel => transitionPanel;

        private void Start()
        {
            transitionPanel.alpha = 1;
            FadeOut(transitionPanel, TransitionDuration);
        }
        
        public void PopUp(GameObject go)
        {
            go.transform.localScale = Vector3.zero;
            go.SetActive(true);
            go.transform.DOScale(Vector3.one, tweenDuration);
        }

        public void MessagePopUp(string message, bool withOk)
        {
            okBtn.gameObject.SetActive(withOk);
            messageText.text = message;
            PopUp(messageGameObject);
        }
        
        public void PopDown(GameObject go) => StartCoroutine(StartPopDown(go));
        
        private IEnumerator StartPopDown(GameObject go)
        {
            go.transform.DOScale(Vector3.zero, tweenDuration);
            yield return new WaitUntil(() => go.transform.localScale == Vector3.zero);
            go.SetActive(false);
            go.transform.localScale = Vector3.one;
        }
        
        public void FadeIn(CanvasGroup canvasGroup, float duration) => canvasGroup.DOFade(1, duration);
        
        public void FadeOut(CanvasGroup canvasGroup, float duration) => canvasGroup.DOFade(0, duration);
        
        public void LoadScene(string sceneName) => StartCoroutine(StartLoadScene(sceneName, TransitionPanel));
        
        private IEnumerator StartLoadScene(string sceneName, CanvasGroup canvasGroup)
        {
            FadeIn(canvasGroup, TransitionDuration);
            yield return new WaitForSeconds(TransitionDuration);
            SceneManager.LoadScene(sceneName);
        }
        
        public void Quit() => Application.Quit();
    }
}