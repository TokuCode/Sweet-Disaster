using System.Collections.Generic;
using Code.Gameplay.Character.Features;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Framework
{
    public class Controller : NetworkBehaviour
    {
        [SerializeField] protected List<Feature> _features = new();
        public IDependencyManager Dependencies { get; } = new DependencyManager();

        public override void OnNetworkSpawn()
        {
            foreach (var feature in _features)
            {
                Dependencies.TryAddFeature(feature);
            }

            foreach (var feature in _features)
            {
                feature.InitializeFeature(this);
            }
        }
        
        protected virtual void Update()
        {
            if (!IsOwner && !IsServer) return;

            foreach (var feature in _features)
            {
                feature.UpdateFeature();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!IsOwner && !IsServer) return;

            foreach (var feature in _features)
            {
                feature.FixedUpdateFeature();
            }
        }

        public bool Get<T>(out T feature) where T : IFeature
        {
            return Dependencies.TryGetFeature(out feature);
        }
    }
}