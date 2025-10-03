using System.ComponentModel.DataAnnotations;

namespace TaskManager.Api.DTOs;

public record ProjectCreateDto(
    [Required, MaxLength(120)] string Name,
    [MaxLength(500)] string? Description
);

public record ProjectUpdateDto(
    [Required, MaxLength(120)] string Name,
    [MaxLength(500)] string? Description
);
