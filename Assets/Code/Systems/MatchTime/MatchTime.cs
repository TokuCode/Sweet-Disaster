using System;
using Code.Networking.Session;
using Unity.Netcode;
using UnityEngine;

namespace Code.Systems.MatchTime
{
    public class MatchTime : NetworkBehaviour
    {
        public static MatchTime Instance;
        
        [SerializeField] private float _matchTime;
        public NetworkVariable<float> MatchTimer { get; } = new();
        public bool MatchEnded { get; private set; }

        public event Action OnEndMatch;
        private bool started;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Instance of type {GetType().Name} already exists!");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            if(!IsServer) return;
            MatchTimer.Value = _matchTime;
            started = true;
        }

        private void Update()
        {
            if(!IsServer || !started) return;
            if (SessionManager.Instance.IsPracticeMode) return;
            TickTimer(Time.deltaTime);
        }

        private void TickTimer(float deltaTime)
        {
            if(MatchTimer.Value > 0) MatchTimer.Value -= deltaTime;
            else if (!MatchEnded)
            { 
                MatchEnded = true;
                OnEndMatch?.Invoke();
            }
        }
    }
}