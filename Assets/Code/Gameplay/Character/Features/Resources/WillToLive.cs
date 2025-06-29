using System;
using Code.Gameplay.Character.Framework;
using Code.Helpers;
using Code.Networking.ClientPrediction;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Code.Gameplay.Character.Features
{
    public class WillToLive : Feature
    {
        private Health _health;
        
        [Header("Stun Minigame")]
        [SerializeField] private float _minigameDurationPerHealthRatio;
        [SerializeField] private float _minStunDuration;
        [SerializeField] private float _sweetSpotRatio;
        private float _cachedStunDuration;
        public float SweetSpotRatio => _sweetSpotRatio;
        [SerializeField] private float _onSuccessTimeReductionRatio;
        private CountdownTimer _minigameTimer;
        private NetworkVariable<bool> _onMinigame = new(false, NetworkVariableReadPermission.Owner);
        public float MinigameProgress => 1 - _minigameTimer.Progress;
        public bool OnMinigame => _onMinigame.Value;
        private bool _cachedMinigameInput;
        public bool CachedMinigameInput => _cachedMinigameInput;
        public event Action OnMinigameFailed; 
        public event Action OnMinigameSucces; 

        public override void InitializeFeature(Controller controller)
        {
            base.InitializeFeature(controller);
            _dependencies.TryGetFeature(out _health);
            _minigameTimer = new (_minigameDurationPerHealthRatio);
            _health.OnStun += StartMinigame;
            _health.OnUnStun += EndMinigame;
        }

        public override void UpdateFeature()
        {
            if(!IsOwner) return;
            
            if(!_onMinigame.Value) return;
            
            if(!_minigameTimer.IsRunning) _minigameTimer.Start();
            
            _minigameTimer.Tick(Time.deltaTime);
        }

        public override void FixedUpdateFeature() { }

        public override void Apply(ref InputPayload @event)
        {
            if (!IsOwner) return;

            _cachedMinigameInput = @event.crouch;

            if (!_onMinigame.Value) return;
            
            MinigameInput();
        }

        private void MinigameInput()
        {
            if (!_cachedMinigameInput || !_onMinigame.Value) return;

            if (_minigameTimer.Progress <= _sweetSpotRatio)
            {
                _health.AccelerateStun(Mathf.Max(1f, _onSuccessTimeReductionRatio * _cachedStunDuration));
                OnMinigameSucces?.Invoke();
            }
            else
            {
                OnMinigameFailed?.Invoke();
            }
            
            _minigameTimer.Reset();
            _minigameTimer.Start();
        }

        private void StartMinigame(float stunDuration, float healthRatio)
        {
            if(!IsServer) return;
            
            if(stunDuration < _minStunDuration) return;
            
            _onMinigame.Value = true;
            
            if(IsHost) StartMinigameAction(healthRatio, stunDuration);
            else StartMinigameOnClientRpc(healthRatio, stunDuration);
        }

        private void EndMinigame()
        {
            if(!IsServer) return;
            
            _onMinigame.Value = false;
            
            if(IsHost) EndMinigameAction();
            else EndMinigameOnClientRpc();
        }

        [ClientRpc]
        private void StartMinigameOnClientRpc(float healthRatio, float stunDuration)
        {
            if(!IsOwner) return;

            StartMinigameAction(healthRatio, stunDuration);
        }

        [ClientRpc]
        private void EndMinigameOnClientRpc()
        {
            if(!IsOwner) return;
                
            EndMinigameAction();
        }

        private void StartMinigameAction(float healthRatio, float stunDuration)
        {
            _minigameTimer = new (_minigameDurationPerHealthRatio * Mathf.Max(1, healthRatio));
            _minigameTimer.Start();
            _cachedStunDuration = stunDuration;
        }

        private void EndMinigameAction()
        {
            _minigameTimer.Stop();
            _cachedMinigameInput = false;
            _cachedStunDuration = 0;
        }
    }
}