using System.Collections.Generic;
using System.Linq;

namespace Code.Helpers.Pipeline
{
    public class Pipeline<T> where T : IEvent
    {
        private readonly List<IProcess<T>> bindings = new List<IProcess<T>>();
        
        public void Register(IProcess<T> binding) => bindings.Add(binding);
        public void Deregister(IProcess<T> binding) => bindings.Remove(binding);
        
        public void Process(ref T @event, out bool success)
        {
            success = true;
            var orderedBindings = bindings.OrderByDescending(b => b.Order);
            
            foreach (var binding in orderedBindings)
            {
                binding.Modify(ref @event, out success);
                if (!success)
                    break;
            }
        }
        
        public void Clear() => bindings.Clear();
    }
}