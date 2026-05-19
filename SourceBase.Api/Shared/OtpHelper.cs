using System.Security.Cryptography;

namespace SourceBase.Api.Shared;

public static class OtpHelper
{
    public static string Generate() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
}
