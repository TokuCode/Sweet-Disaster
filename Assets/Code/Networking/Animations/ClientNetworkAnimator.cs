using UnityEngine;
using Unity.Netcode.Components;

namespace Code.Networking.Animations
{
    public class ClientNetworkAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}