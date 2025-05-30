using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Code.Helpers.Singleton;

namespace Code.Systems.Session
{
    public class UITransitionManager : Singleton<UITransitionManager>
    {
        [SerializeField] private float tweenDuration;
        public float transitionDuration;

        public GameObject MessageObject;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button okBtn;

        private void Start()
        {
            gameObject.GetComponent<CanvasGroup>().alpha = 1;
            FadeOut(gameObject, transitionDuration);
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
            PopUp(MessageObject);
        }
        
        public void PopDown(GameObject go) => StartCoroutine(StartPopDown(go));
        
        private IEnumerator StartPopDown(GameObject go)
        {
            go.transform.DOScale(Vector3.zero, tweenDuration);
            yield return new WaitUntil(() => go.transform.localScale == Vector3.zero);
            go.SetActive(false);
            go.transform.localScale = Vector3.one;
        }
        
        public void FadeIn(GameObject go, float duration) => go.GetComponent<CanvasGroup>().DOFade(1, duration);
        
        public void FadeOut(GameObject go, float duration) => go.GetComponent<CanvasGroup>().DOFade(0, duration);
        
        public void LoadScene(string sceneName) => StartCoroutine(StartLoadScene(sceneName, gameObject));
        
        private IEnumerator StartLoadScene(string sceneName, GameObject go)
        {
            FadeIn(go, transitionDuration);
            yield return new WaitForSeconds(transitionDuration);
            SceneManager.LoadScene(sceneName);
        }
        
        public void Quit() => Application.Quit();
    }
}