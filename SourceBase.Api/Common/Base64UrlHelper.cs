namespace SourceBase.Api.Common;

public static class Base64UrlHelper
{
    public static string Base64UrlEncode(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        return base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public static byte[] Base64UrlDecode(string input)
    {
        string base64 = input.Replace("-", "+").Replace("_", "/");
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }
}