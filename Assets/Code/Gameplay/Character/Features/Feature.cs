using Code.Gameplay.Character.Command;
using Code.Gameplay.Character.Framework;
using Code.Helpers.Pipeline;
using Code.Networking.ClientPrediction;
using Unity.Netcode;

namespace Code.Gameplay.Character.Features
{
    public abstract class Feature : NetworkBehaviour, IFeature, IProcess<InputPayload>
    {
        protected PlayerCommandInvoker _invoker;
        protected IDependencyManager _dependencies; 
        
        public virtual void InitializeFeature(Controller controller)
        {
            if (controller is PlayerController player)
            {
                _invoker = player.Invoker;
                _dependencies = player.Dependencies;
                player.InputPipeline.Register(this);
            }
        }

        public abstract void UpdateFeature();
        public abstract void FixedUpdateFeature();

        public abstract void Apply(ref InputPayload @event);
    }
}