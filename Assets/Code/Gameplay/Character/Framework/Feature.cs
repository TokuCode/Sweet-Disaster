using UnityEngine;

namespace Code.Gameplay.Character.Framework
{
    public interface Feature<T> where T : Component
    {
        void InitializeFeature(Controller<T> controller);
        void UpdateFeature();
        void FixedUpdateFeature();
    }
}