using System;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character
{
    public class LoseReporter : NetworkBehaviour
    {
        private bool _hasLost;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsOwner) return; // only the local client should detect and report

            if (other.gameObject.CompareTag("Deathbox") && !_hasLost)
            {
                _hasLost = true;

                Debug.Log($"[Client] Detected Deathbox hit, reporting to server...");
                ReportLossToServerRpc();
            } 
        }

        [Rpc(SendTo.Server)]
        private void ReportLossToServerRpc(RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"[Server] Player {clientId} reported that lost.");
            LoseTracker.Instance.ReportPlayerLoss(clientId);
        }
    }
}