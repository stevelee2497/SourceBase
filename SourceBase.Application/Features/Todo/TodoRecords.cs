using SourceBase.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace SourceBase.Application.Features.Todo;

public record CreateTodoRequest([Required] DateOnly Date, [Required] string Title, ItemStatus Status);

public record TodoItemDetailResponse(Guid Id, DateOnly Date, string Title, ItemStatus Status, DateTime? CreatedOn, string? CreatedBy, DateTime? UpdatedOn, string? UpdatedBy)
{
    public TodoItemDetailResponse(TodoItemEntity todo) : this(todo.Id, todo.Date, todo.Title, todo.Status, todo.CreatedOn, todo.CreatedBy, todo.UpdatedOn, todo.UpdatedBy)
    {
    }
}