using Core.DTOs;
using Core.Entities;

namespace Core.Extensions
{
    public static class UserEntityExtensions
    {
        public static UserInfoDto ToDto(this UserEntity entity)
        {
            return new UserInfoDto
            {
                Id = entity.Id,
                Email = entity.Email,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                PhoneNumber = entity.PhoneNumber,
                Roles = entity.Roles.Select(x => x.Name).ToArray()!
            };
        }
    }
}
