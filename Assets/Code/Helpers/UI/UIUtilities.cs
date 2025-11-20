using System;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Code.Helpers.Singleton;
using UnityEditor;

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

        [Header("Gear")] 
        [SerializeField] private GameObject bigGear;
        [SerializeField] private GameObject smallGear;
        
        // Public members
        public float TransitionDuration => transitionDuration;
        public Button MessageOkBtn => okBtn;
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
            yield return go.transform.DOScale(Vector3.zero, tweenDuration).WaitForCompletion();
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

        public void MoveDown(RectTransform rectTransform)
        {
            rectTransform.gameObject.SetActive(false);
            Vector2 initialPos = rectTransform.anchoredPosition;

            float offsetY = rectTransform.parent.GetComponent<RectTransform>().rect.height * 1f;
            rectTransform.anchoredPosition = new Vector2(initialPos.x, initialPos.y + offsetY);
            
            rectTransform.gameObject.SetActive(true);
            rectTransform.DOAnchorPosY(initialPos.y, 1f).SetEase(Ease.OutBounce);
        }

        public void MoveUp(RectTransform rectTransform) => StartCoroutine(StartMoveUp(rectTransform));
        
        private IEnumerator StartMoveUp(RectTransform rectTransform)
        {
            Vector2 initialPos = rectTransform.anchoredPosition;
            
            float offsetY = rectTransform.parent.GetComponent<RectTransform>().rect.height * 1f;
            yield return rectTransform.DOAnchorPosY(initialPos.y + offsetY, 1f).SetEase(Ease.InBounce).WaitForCompletion();
            
            rectTransform.anchoredPosition = initialPos;
            rectTransform.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (bigGear.activeInHierarchy)
            {
                bigGear.transform.localEulerAngles = new Vector3(bigGear.transform.localEulerAngles.x, bigGear.transform.localEulerAngles.y, 
                    bigGear.transform.localEulerAngles.z - 10 * Time.deltaTime);
            }

            if (smallGear.activeInHierarchy)
            {
                smallGear.transform.localEulerAngles = new Vector3(smallGear.transform.localEulerAngles.x, smallGear.transform.localEulerAngles.y, 
                    smallGear.transform.localEulerAngles.z + 10 * Time.deltaTime);
            }
        }

        public void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}