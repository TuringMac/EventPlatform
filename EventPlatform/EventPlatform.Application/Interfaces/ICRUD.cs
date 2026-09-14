using EventPlatform.Domain.Interfaces;

namespace EventPlatform.Application.Interfaces;

public interface ICRUD<T> : IRead<T>, IWrite<T>
    where T : IEntity
{

}
