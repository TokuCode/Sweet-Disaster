using System.Collections;
using UnityEngine;

namespace Code.Gameplay.Objects
{
    public class AttackVFX : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        [SerializeField] private float _persistenceTime;
        [SerializeField] private AnimationCurve _fadeCurve;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Init()
        {
            StartCoroutine(FadeSequence());
        }

        private IEnumerator FadeSequence()
        {
            float elapsedTime = 0;
            Color baseColor = spriteRenderer.color;

            while (elapsedTime < _persistenceTime)
            {
                elapsedTime += Time.deltaTime;
                float parameter = 1 - Mathf.Clamp01(elapsedTime / _persistenceTime);
                baseColor.a = _fadeCurve.Evaluate(parameter);
                spriteRenderer.color = baseColor;
                
                yield return null;
            }
            
            baseColor.a = 1;
            spriteRenderer.color = baseColor;
            gameObject.SetActive(false);
        }
    }
}