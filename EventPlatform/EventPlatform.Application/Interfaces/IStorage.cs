using EventPlatform.Domain.Interfaces;

namespace EventPlatform.Application.Interfaces;

public interface IStorage<T> : ICRUD<T>
    where T : IEntity
{

}
