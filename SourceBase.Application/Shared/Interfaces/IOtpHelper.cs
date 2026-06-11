namespace SourceBase.Application.Shared.Interfaces;

public interface IOtpHelper
{
    (string Otp, DateTime ExpiresOn) Generate();
}
