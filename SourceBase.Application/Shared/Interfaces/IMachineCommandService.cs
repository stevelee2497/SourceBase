namespace SourceBase.Application.Shared.Interfaces;

public interface IMachineCommandService
{
    Task SendCommandAsync(Guid userId, Guid machineId, MachineCommandType type, CancellationToken ct);
}
