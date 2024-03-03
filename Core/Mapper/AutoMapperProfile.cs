using AutoMapper;
using Core.DTOs;
using Core.Entities;
using Services;

namespace Core.Mapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<TodoItemDto, TodoItemEntity>();
            CreateMap<TodoItemEntity, TodoItemDetailDto>();

            CreateMap<UserEntity, UserInfoDto>();
        }
    }
}
