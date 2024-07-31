namespace API.Models
{
    public class SystemApiErrorModel
    {
        public required string Code { get; set; }
        public required string Message { get; set; }
        public string? StackTrace { get; set; }
        public Dictionary<string, object>? Details { get; set; }
    }
}
