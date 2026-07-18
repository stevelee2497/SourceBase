namespace SourceBase.Web.Services;

public class ToastService
{
    public sealed record Toast(Guid Id, string Code, string Message, string TraceId, DateTimeOffset CreatedAt, string Type = "error");

    public List<Toast> Toasts { get; } = [];
    public event Action? OnChange;

    public void ShowError(ErrorResponse error)
    {
        Toasts.Add(new Toast(Guid.NewGuid(), error.Code, error.Message, error.TraceId, DateTimeOffset.UtcNow, "error"));
        OnChange?.Invoke();
    }

    public void ShowSuccess(string message)
    {
        Toasts.Add(new Toast(Guid.NewGuid(), "SUCCESS", message, string.Empty, DateTimeOffset.UtcNow, "success"));
        OnChange?.Invoke();
    }

    public void Dismiss(Guid id)
    {
        Toasts.RemoveAll(t => t.Id == id);
        OnChange?.Invoke();
    }
}
