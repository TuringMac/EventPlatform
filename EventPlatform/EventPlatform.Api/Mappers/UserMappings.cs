using EventPlatform.Application.DTO;
using EventPlatform.Domain.Model;

namespace EventPlatform.Api.Mappers;

public static class UserMappings
{
    public static UserResponse ToResponse(this User user)
    {
        return new UserResponse(
            user.Id,
            user.Login,
            user.Role.ToString());
    }
}
