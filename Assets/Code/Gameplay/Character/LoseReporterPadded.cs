using Code.Gameplay.Character.Features;
using Code.Gameplay.Character.Framework;
using Code.Gameplay.Objects.SceneBox;
using Code.Networking.ClientPrediction;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character
{
    public class LoseReporterPadded : Feature
    {
        private SceneBox _sceneBox;
        private bool _out;

        public override void UpdateFeature()
        {
            CheckLoss();
        }

        public void CheckLoss()
        {
            if (_out) return;
            
            _sceneBox = SceneBox.Instance;
            
            if (_sceneBox == null) return;

            if (!_invoker.CenterPosition.Request(out var position).success) return;

            bool outX = position.x < _sceneBox.Left || position.x > _sceneBox.Right;
            bool outY = position.y < _sceneBox.Bottom || position.y > _sceneBox.Top;

            if (outX || outY)
            {
                _out = true;
                ReportLossToServerRpc();
            }
        }

        public override void FixedUpdateFeature() { }

        public override void Apply(ref InputPayload @event) { }
        
        [Rpc(SendTo.Server)]
        private void ReportLossToServerRpc(RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
#if UNITY_EDITOR
            Debug.Log($"[Server] Player {clientId} reported that lost.");
#endif
            LoseTracker.Instance.ReportPlayerLoss(clientId);
        }
    }
}