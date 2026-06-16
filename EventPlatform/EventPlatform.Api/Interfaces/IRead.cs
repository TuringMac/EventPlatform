namespace EventPlatform.Api.Interfaces
{
    public interface IRead<out T>
    {
        IEnumerable<T> GetAll();
        T GetById(Guid id);
    }
}
