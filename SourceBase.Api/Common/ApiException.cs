using System.Net;

namespace SourceBase.Api.Common;

public abstract class ApiException(string message, string code, int statusCode) : Exception(message)
{
    public int StatusCode { get; set; } = statusCode;

    public string Code { get; set; } = code;
}

public class ApiInternalException(string message = "Something went wrong", string code = "GENERIC CODE", int statusCode = (int)HttpStatusCode.InternalServerError) : ApiException(message, code, statusCode)
{
}

public class NotFoundException(string message = "Item Not Found", string code = "NOT FOUND", int statusCode = (int)HttpStatusCode.InternalServerError) : ApiException(message, code, statusCode)
{
}

public class UnAuthorizedException(string message = "UNAUTHORIZE", string code = "UNAUTHORIZE", int statusCode = (int)HttpStatusCode.Unauthorized) : ApiException(message, code, statusCode)
{
}

public class ForbiddenException(string message = "FORBIDDEN", string code = "FORBIDDEN", int statusCode = (int)HttpStatusCode.Forbidden) : ApiException(message, code, statusCode)
{
}
