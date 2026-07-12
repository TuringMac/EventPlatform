namespace EventPlatform.Api.Interfaces;

public interface IWrite<in T>
{
    void Add(T obj);
    void Update(Guid id, T obj);
    void Delete(Guid id);
}
