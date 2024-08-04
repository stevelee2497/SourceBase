using System.Net;

namespace Core.Exceptions
{
    public abstract class BaseException : Exception
    {
        public int StatusCode { get; set; }

        public string Code { get; set; }

        public BaseException(string message, string code, int statusCode) : base(message)
        {
            Code = code;
            StatusCode = statusCode;
        }
    }

    public class SystemApiException : BaseException
    {
        public SystemApiException(string message, string code = "GENERIC CODE", int statusCode = (int)HttpStatusCode.InternalServerError) : base(message, code, statusCode)
        {
        }
    }

    public class NotFoundException : BaseException
    {
        public NotFoundException(string message = "Item Not Found", string code = "GENERIC CODE", int statusCode = (int)HttpStatusCode.InternalServerError) : base(message, code, statusCode)
        {
        }
    }
}
