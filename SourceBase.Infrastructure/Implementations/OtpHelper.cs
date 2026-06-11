using System.Security.Cryptography;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Infrastructure.Implementations;

public class OtpHelper(AppSettings appSettings, IDateTime dateTime) : IOtpHelper
{
    public (string, DateTime) Generate()
    {
        var otp = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
        var expiresOn = dateTime.UtcNow.AddMinutes(appSettings.OtpTokenExpirationMinutes);
        return (otp, expiresOn);
    }
}
