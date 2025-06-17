using System;
using Code.Helpers;
using DG.Tweening;
using UnityEngine;

namespace Code.Gameplay.Objects
{
    public class VFXExplosion : MonoBehaviour
    {
        [SerializeField] private float _radius;
        [SerializeField] private float _persistenceTime;
        private CountdownTimer _lifeTimer;

        private void Awake()
        {
            _lifeTimer = new(1f);
            _lifeTimer.OnTimerStop += Reset;
        }

        public void Init(float radius)
        {
            _radius = radius;
            _lifeTimer.Reset(_persistenceTime);
            _lifeTimer.Start();
            transform.DOScale(Vector3.one * _radius, _persistenceTime).SetEase(Ease.OutBounce);
        }

        private void Update()
        {
            _lifeTimer.Tick(Time.deltaTime);
        }

        public void Reset()
        {
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }
    }
}