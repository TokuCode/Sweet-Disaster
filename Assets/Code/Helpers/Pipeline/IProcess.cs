namespace Code.Helpers.Pipeline
{
    public interface IProcess<T>
    {
        void Apply(ref T @event);
    }
}
