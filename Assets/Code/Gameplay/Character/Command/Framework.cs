namespace Code.Gameplay.Character.Command
{
    public interface ICommand<T> where T : unmanaged
    {
        Result Perform(T input);
    }

    public interface IRequest<T> where T : unmanaged
    {
        Result Request(out T output);
    }

    public struct Result
    {
        public bool success;
    }
}