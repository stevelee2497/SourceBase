using System.Net;

namespace Core.Exceptions
{
    public class SystemApiException : Exception
    {
        public int StatusCode { get; set; }

        public string Code { get; set; }

        public SystemApiException(string message, string code = "GENERIC CODE", int statusCode = (int)HttpStatusCode.InternalServerError) : base(message)
        {
            Code = code;
            StatusCode = statusCode;
        }
    }
}
