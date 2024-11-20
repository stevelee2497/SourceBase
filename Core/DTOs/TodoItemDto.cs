using Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Core.DTOs;

public class TodoItemDto
{
    public DateOnly Date { get; set; }

    [Required]
    public required string Title { get; set; }

    public ItemStatus Status { get; set; }

    public DateTime? CreatedOn { get; set; }
}

public class TodoItemDetailDto : TodoItemDto
{
    [JsonPropertyOrder(-1)]
    public Guid Id { get; set; }
}