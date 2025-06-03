using System;
using System.Collections.Generic;

namespace Code.Gameplay.Character.Framework
{
    public interface IDependencyManager
    {
        void TryAddFeature(IFeature feature);
        bool TryGetFeature<T>(out T feature) where T : IFeature;
    }
    
    public class DependencyManager : IDependencyManager
    {
        protected Dictionary<Type, IFeature> _featuresDict = new();
        
        public void TryAddFeature(IFeature feature)
        {
            if (!_featuresDict.ContainsKey(feature.GetType()))
            {
                _featuresDict.Add(feature.GetType(), feature);
            }
            else 
                throw new ArgumentException("There is already a feature with the same type");
        }

        public bool TryGetFeature<T>(out T feature) where T : IFeature
        {
            if(_featuresDict.TryGetValue(typeof(T), out var rawFeature))
            {
                feature = (T)rawFeature;
                return true;
            }
            feature = default;
            return false;
        } 
    }
}