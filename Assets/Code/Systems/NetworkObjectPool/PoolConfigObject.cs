using System;
using UnityEngine;

namespace Code.Systems.NetworkObjectPool
{
    [Serializable]
    public struct PoolConfigObject
    {
        public GameObject prefab;
        public int prewarmCount;
    }
}