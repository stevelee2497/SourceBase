using System.Net;

namespace Domain.Common;

public class ApiInternalException(string message = "Something went wrong", string code = "GENERIC CODE", int statusCode = (int)HttpStatusCode.InternalServerError) : BaseException(message, code, statusCode)
{
}

public class NotFoundException(string message = "Item Not Found", string code = "NOT FOUND", int statusCode = (int)HttpStatusCode.InternalServerError) : BaseException(message, code, statusCode)
{
}

public class UnAuthorizedException(string message = "UNAUTHORIZE", string code = "UNAUTHORIZE", int statusCode = (int)HttpStatusCode.Unauthorized) : BaseException(message, code, statusCode)
{
}

public abstract class BaseException(string message, string code, int statusCode) : Exception(message)
{
    public int StatusCode { get; set; } = statusCode;

    public string Code { get; set; } = code;
}
