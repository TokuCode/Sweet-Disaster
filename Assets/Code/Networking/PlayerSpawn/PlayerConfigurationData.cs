using System;
using UnityEngine;

namespace Code.Networking.PlayerSpawn
{
    [Serializable]
    public struct PlayerConfigurationData
    {
        public Transform spawn;
        public string tag;
        public GameObject prefab;
    }
}