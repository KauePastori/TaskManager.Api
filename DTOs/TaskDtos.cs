using System.ComponentModel.DataAnnotations;
using TaskManager.Api.Models;

namespace TaskManager.Api.DTOs;

public record TaskCreateDto(
    [Required, MaxLength(160)] string Title,
    string? Notes,
    DateTime? DueDate,
    WorkStatus Status,
    [Required] int ProjectId
);

public record TaskUpdateDto(
    [Required, MaxLength(160)] string Title,
    string? Notes,
    DateTime? DueDate,
    WorkStatus Status,
    [Required] int ProjectId
);
