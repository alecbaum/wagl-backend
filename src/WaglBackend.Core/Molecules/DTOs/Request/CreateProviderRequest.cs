using System.ComponentModel.DataAnnotations;

namespace WaglBackend.Core.Molecules.DTOs.Request;

public class CreateProviderRequest
{
    [Required(ErrorMessage = "Provider name is required")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Provider name must be between 3 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? ContactEmail { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    public string[]? AllowedIpAddresses { get; set; }
}