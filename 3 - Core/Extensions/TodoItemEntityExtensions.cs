using Core.DTOs;
using Core.Entities;

namespace Core.Extensions
{
    public static class TodoItemEntityExtensions
    {
        public static TodoItemDetailDto ToDetailDto(this TodoItemEntity entity)
        {
            return new TodoItemDetailDto
            {
                Id = entity.Id,
                Date = entity.Date,
                Title = entity.Title,
                Status = entity.Status
            };
        }
    }
}
