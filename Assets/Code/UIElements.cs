using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement;

public class UIElements : MonoBehaviour
{
    [SerializeField] private float tweenDuration;
    [SerializeField] private float transitionDuration;

    private void Start()
    {
        gameObject.GetComponent<CanvasGroup>().alpha = 1;
        FadeOut(gameObject, transitionDuration);
    }
    public void PopUp(GameObject go)
    {
        go.transform.localScale = Vector3.zero;
        go.transform.DOScale(Vector3.one, tweenDuration);
    }
    public void PopDown(GameObject go) => StartCoroutine(popDown(go));
    private IEnumerator popDown(GameObject go)
    {
        go.transform.DOScale(Vector3.zero, tweenDuration);
        yield return new WaitUntil(() => go.transform.localScale == Vector3.zero);
        go.SetActive(false);
        go.transform.localScale = Vector3.one;
    }
    public void FadeIn(GameObject go, float duration) => go.GetComponent<CanvasGroup>().DOFade(1, duration);
    public void FadeOut(GameObject go, float duration) => go.GetComponent<CanvasGroup>().DOFade(0, duration);
    public void LoadScene(string sceneName) => StartCoroutine(loadScene(sceneName, gameObject));
    private IEnumerator loadScene(string sceneName, GameObject go)
    {
        FadeIn(go, transitionDuration);
        yield return new WaitForSeconds(transitionDuration);
        SceneManager.LoadScene(sceneName);
    }
    public void Quit() => Application.Quit();
}