using System;
using Unity.Netcode;
using UnityEngine;

namespace Code.Systems.Attack
{
    public class AttackBus : NetworkBehaviour
    {
        public static AttackBus Singleton { get; private set; }

        public event Action<AttackEvent> Event;

        private void Awake()
        {
            if (Singleton != null && Singleton != this)
            {
                Debug.LogWarning($"More than one {GetType().Name} component in scene.");
                Destroy(gameObject);
                return;
            }
            Singleton = this;
        }

        public void BroadcastEvent(AttackEvent attack)
        {
            TriggerEvent(attack);
            BroadcastEventToAllRpc(attack);
        }

        [Rpc(SendTo.NotMe)]
        private void BroadcastEventToAllRpc(AttackEvent attack)
        {
            TriggerEvent(attack);
        }

        private void TriggerEvent(AttackEvent attack)
        {
            Event?.Invoke(attack);
        }
    }
}