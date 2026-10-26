using EventPlatform.Users.Application.DTO;
using EventPlatform.Users.Domain.Model;

namespace EventPlatform.Users.Api.Mappers;

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
