using System.Collections.Generic;
using System.Linq;

namespace Code.Helpers.Pipeline
{
    public class Pipeline<T>
    {
        private readonly List<IProcess<T>> bindings = new ();
        
        public void Register(IProcess<T> binding) => bindings.Add(binding);
        public void Deregister(IProcess<T> binding) => bindings.Remove(binding);
        
        public void Process(ref T @event)
        {
            foreach (var binding in bindings)
                binding.Apply(ref @event);
        }
        
        public void Clear() => bindings.Clear();
    }
}