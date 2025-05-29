using UnityEngine;

namespace Code.Helpers.Utils
{
    public static class LayerMaskUtils
    {
        public static bool CompareGameObjectLayerMask(GameObject gameObject, LayerMask layerMask)
        {
            return (layerMask & (1 << gameObject.layer)) != 0;
        }
    }
}