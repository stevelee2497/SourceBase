using Core.Entities;
using Services;

namespace Core.Extensions
{
    public static class UserEntityExtensions
    {
        public static UserInfoDto ToDto(this UserEntity entity)
        {
            return new UserInfoDto
            {
                Id = entity.Id,
                Email = entity.Email!,
                FirstName = entity.FirstName,
                LastName = entity.LastName
            };
        }
    }
}
