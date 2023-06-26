using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public static class OAuthHelper
{
    public static string GenerateSignature(string method, string url, Dictionary<string, string> parameters, string consumerSecret, string tokenSecret, string verifier = null)
    {
        // Sort the parameters alphabetically
        List<KeyValuePair<string, string>> sortedParameters = new List<KeyValuePair<string, string>>(parameters);
        sortedParameters.Sort((x, y) => string.CompareOrdinal(x.Key, y.Key));

        // Concatenate the sorted parameter key-value pairs
        StringBuilder parameterString = new StringBuilder();
        foreach (KeyValuePair<string, string> parameter in sortedParameters)
        {
            if (parameterString.Length > 0)
            {
                parameterString.Append("&");
            }

            string encodedKey = Uri.EscapeDataString(parameter.Key) ?? string.Empty;
            string encodedValue = Uri.EscapeDataString(parameter.Value) ?? string.Empty;

            parameterString.Append($"{encodedKey}={encodedValue}");
        }

        // Construct the base string
        string baseString = $"{method.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(parameterString.ToString())}";

        // Add the verifier if provided
        if (!string.IsNullOrEmpty(verifier))
        {
            baseString += $"&{Uri.EscapeDataString(verifier)}";
        }

        // Calculate the HMAC-SHA1 signature
        string key = $"{Uri.EscapeDataString(consumerSecret ?? string.Empty)}&{Uri.EscapeDataString(tokenSecret ?? string.Empty)}";
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] baseStringBytes = Encoding.UTF8.GetBytes(baseString);
        byte[] signatureBytes;
        using (HMACSHA1 hmac = new HMACSHA1(keyBytes))
        {
            signatureBytes = hmac.ComputeHash(baseStringBytes);
        }

        // Convert the signature to a base64-encoded string
        string signature = Convert.ToBase64String(signatureBytes);

        return signature;
    }

    public static string GenerateAuthorizationHeader(Dictionary<string, string> parameters)
    {
        StringBuilder header = new StringBuilder("OAuth ");
        foreach (KeyValuePair<string, string> parameter in parameters)
        {
            if (header.Length > 6)
            {
                header.Append(", ");
            }
            header.Append($"{Uri.EscapeDataString(parameter.Key)}=\"{Uri.EscapeDataString(parameter.Value)}\"");
        }
        return header.ToString();
    }
}
