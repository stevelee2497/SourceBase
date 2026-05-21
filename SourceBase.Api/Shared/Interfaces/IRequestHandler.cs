namespace SourceBase.Api.Shared.Interfaces;

public interface IRequestHandler<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}
