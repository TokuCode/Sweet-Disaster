using System;
using System.Collections.Generic;
using Code.Helpers.SerializableGuid;

namespace Code.Gameplay.Character.Framework
{
    public class PropertyBag
    {
        protected Dictionary<string, SerializableGuid> _index;
        protected Dictionary<SerializableGuid, Property> _data;
        
        public PropertyBag()
        {
            _index = new Dictionary<string, SerializableGuid>();
            _data = new Dictionary<SerializableGuid, Property>();
        }
        
        public void Add(Property value, string name)
        {
            if (_data.ContainsKey(value.Id))
                throw new Exception($"Direction {value.Id} already exists in data.");
            
            if (_index.ContainsKey(name))
                throw new Exception($"Name {name} already exists in the index.");
            
            _data.Add(value.Id, value);
            _index.Add(name, value.Id);
        }
        
        public bool TryGetValue(SerializableGuid id, out Property value)
        {
            return _data.TryGetValue(id, out value);
        }
        
        public bool TryGetValue(string name, out Property value)
        {
            if (TryGetIndex(name, out var id))
            {
                return TryGetValue(id, out value);
            }

            value = default;
            return false;
        }
        
        public bool TryGetIndex(string name, out SerializableGuid id)
        {
            return _index.TryGetValue(name, out id);
        }
    }
}