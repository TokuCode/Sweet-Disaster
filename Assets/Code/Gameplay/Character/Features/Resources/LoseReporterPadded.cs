using System.Collections;
using Code.Gameplay.Character.Features;
using Code.Gameplay.Character.Framework;
using Code.Gameplay.Objects.ObjectBox;
using Code.Networking.ClientPrediction;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character
{
    public class LoseReporterPadded : Feature
    {
        private Health health;
        
        private const int _extraFramesToWaitRespawn = 0;
        [SerializeField] private int baseStockCount;
        private NetworkVariable<int> _stocks = new ();
        [SerializeField] private float _timeToRespawn;
        private NetworkVariable<float> _timerToRespawn = new();
        public float TimeToRespawn => _timerToRespawn.Value;
        private bool _respawning;
        private bool _out;
        private bool _respawningProcess;
        public int StockCount => _stocks.Value;

        public override void ResetFeature() { }

        public override void InitializeFeature(Controller controller)
        {
            base.InitializeFeature(controller);
            if(IsServer) _stocks.Value = baseStockCount;
            _dependencies.TryGetFeature(out health);
        }

        public override void UpdateFeature()
        {
            if(!IsServer) return;
            
            if(!_out) CheckLoss();
            
            if(_timerToRespawn.Value > 0) _timerToRespawn.Value -= Time.deltaTime;
            else if(_respawning && !_respawningProcess)
            {
                Respawn();
            }
        }

        public void CheckLoss()
        {
            if (!_invoker.CenterPosition.Request(out var position).success) return;

            if (SceneBox.Instance.Outside(position))
            {
                _out = true;
                _stocks.Value--;

                if (_stocks.Value <= 0) ReportDefeat();
                else ScheduleRespawn();
                
                ResetController(_stocks.Value > 0);
                RequestResetOnOwnerRpc(_stocks.Value > 0);
            }
        }

        public void ReportDefeat()
        {
            if (!IsServer) return;
            
            int stocks = _stocks.Value;
            float damage = _dependencies.TryGetFeature(out Health health) ? health.HealthAmount : Mathf.Infinity;
            
            _invoker.PlayerNumber.Request(out var clientId);
            _invoker.Defeat.Perform(true);
            LoseTracker.Instance.ReportPlayerLoss((ulong)clientId, stocks, damage);
        }

        private void ScheduleRespawn()
        {
            _timerToRespawn.Value = _timeToRespawn;
            _respawning = true;
        }

        private void Respawn()
        {
            _respawningProcess = true;
            _invoker.Respawn.Perform(true);
            if(IsOwner) RespawnCompleted();
        }

        public void ReportRespawnCompleted()
        {
            ReportRespawnCompletedToServerRpc();
        }
        
        private void RespawnCompleted()
        {
            StartCoroutine(RespawnCompletedSequence());
        }

        private IEnumerator RespawnCompletedSequence()
        {
            for(int i = 0; i < _extraFramesToWaitRespawn; i++)
                yield return null;
            
            _out = false;
            _respawning = false;
            _respawningProcess = false;
        }

        [ServerRpc]
        private void ReportRespawnCompletedToServerRpc()
        {
            RespawnCompleted();
        }
        
        public override void FixedUpdateFeature() { }

        public override void Apply(ref InputPayload @event) { }

        [Rpc(SendTo.Owner)]
        private void RequestResetOnOwnerRpc(bool resetMovement)
        {
            ResetController(resetMovement);
        }

        private void ResetController(bool resetMovement)
        {
            _invoker.Reset.Perform(resetMovement);
        }
    }
}