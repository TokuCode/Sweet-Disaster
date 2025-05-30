using UnityEngine;
using Unity.Netcode.Components;

namespace Code.Systems
{
    public enum AuthorityMode
    {
        Server,
        Client
    }
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        public AuthorityMode authorityMode = Systems.AuthorityMode.Client;

        protected override bool OnIsServerAuthoritative() => authorityMode == Systems.AuthorityMode.Server;
    }
}