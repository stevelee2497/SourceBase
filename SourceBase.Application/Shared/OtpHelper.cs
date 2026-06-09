using System.Security.Cryptography;

namespace SourceBase.Application.Shared;

public static class OtpHelper
{
    public static (string, DateTime) Generate(int expirationMinutes, DateTime utcNow)
    {
        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var expiresOn = utcNow.AddMinutes(expirationMinutes);
        return (otp, expiresOn);
    }
}
