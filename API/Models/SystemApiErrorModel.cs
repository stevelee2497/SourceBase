namespace API.Models
{
    public class SystemApiErrorModel
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public Dictionary<string, object> Details { get; set; }
    }
}
