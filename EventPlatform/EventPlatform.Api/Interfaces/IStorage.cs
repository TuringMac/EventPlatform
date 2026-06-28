using EventPlatform.Api.Model;

namespace EventPlatform.Api.Interfaces;

public interface IStorage<T> : ICRUD<T>
    where T : IEntity
{

}
