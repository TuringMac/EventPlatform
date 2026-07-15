using EventPlatform.Api.Model;

namespace EventPlatform.Api.Interfaces;

public interface ICRUD<T> : IRead<T>, IWrite<T>
    where T : IEntity
{

}
