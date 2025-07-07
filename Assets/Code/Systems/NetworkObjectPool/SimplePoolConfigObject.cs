using System;
using UnityEngine;

namespace Code.Systems.NetworkObjectPool
{
    [Serializable]
    public struct SimplePoolConfigObject
    {
        public GameObject prefab;
        public int prewarmCount;
    }
}