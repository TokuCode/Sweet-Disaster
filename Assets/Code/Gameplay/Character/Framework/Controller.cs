using System.Collections.Generic;
using Code.Helpers.NetworkSingleton;
using UnityEngine;

namespace Code.Gameplay.Character.Framework
{
    public abstract class Controller<T> : NetworkSingleton<T> where T : Component
    {
        protected List<Feature<T>> _features = new ();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            foreach (var feature in _features)
                feature.InitializeFeature(this);
        }
        
        protected virtual void Update()
        {
            foreach (var feature in _features)
                feature.UpdateFeature();
        }
        
        private void FixedUpdate()
        {
            foreach (var feature in _features)
                feature.FixedUpdateFeature();
        }
    }
}