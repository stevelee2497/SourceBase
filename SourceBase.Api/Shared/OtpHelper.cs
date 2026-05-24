using System.Security.Cryptography;

namespace SourceBase.Api.Shared;

public static class OtpHelper
{
    public static (string, DateTime) Generate(int expirationMinutes)
    {
        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var expiresOn = DateTime.UtcNow.AddMinutes(expirationMinutes);
        return (otp, expiresOn);
    }
}
