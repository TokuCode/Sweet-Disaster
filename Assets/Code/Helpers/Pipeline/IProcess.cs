namespace Code.Helpers.Pipeline
{
    public interface IProcess<T> where T : IEvent
    {
        int Order { get; }
        void Modify(ref T @event, out bool success);
    }
}
