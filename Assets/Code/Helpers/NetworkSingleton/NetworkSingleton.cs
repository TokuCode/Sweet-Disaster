using Unity.Netcode;
using UnityEngine;

namespace Code.Helpers.NetworkSingleton
{
    public class NetworkSingleton<T> : NetworkBehaviour where T : Component
    {
        public static T Singleton { get; private set; }
        
        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            
            if (Singleton != null)
            {
                Debug.LogError($"Multiple instances of {typeof(T).Name} detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Singleton = this as T;
        }
    }
}