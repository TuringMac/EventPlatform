using EventPlatform.Api.Model;

namespace EventPlatform.Api.Interfaces
{
    public interface ICRUD<T>
        where T : IEntity
    {
        IEnumerable<T> GetAll();
        T GetById(Guid id);
        void Add(T obj);
        void Update(Guid id, T obj);
        void Delete(Guid id);
    }
}
