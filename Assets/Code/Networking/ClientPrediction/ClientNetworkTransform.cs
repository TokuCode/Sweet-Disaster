using Unity.Netcode.Components;

namespace Code.Networking.ClientPrediction {
    public enum AuthorityType {
        Server,
        Client
    }
    
    public class ClientNetworkTransform : NetworkTransform {
        public AuthorityType authority = AuthorityType.Client;

        protected override bool OnIsServerAuthoritative() => authority == AuthorityType.Server;
    }
}