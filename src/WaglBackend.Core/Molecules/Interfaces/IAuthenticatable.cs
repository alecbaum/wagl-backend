namespace WaglBackend.Core.Molecules.Interfaces;

public interface IAuthenticatable
{
    Guid Id { get; }
    bool IsActive { get; }
    DateTime CreatedAt { get; }
    DateTime? LastAccessedAt { get; set; }
}