using System.ComponentModel.DataAnnotations;

namespace WaglBackend.Core.Molecules.DTOs.Request;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}