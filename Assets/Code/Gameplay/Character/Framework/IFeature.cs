namespace Code.Gameplay.Character.Framework
{
    public interface IFeature
    {
        void InitializeFeature(Controller controller);
        void UpdateFeature();
        void FixedUpdateFeature();
    }
}