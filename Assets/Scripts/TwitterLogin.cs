using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class TwitterLogin : MonoBehaviour
{
    private const string ConsumerKey = "YOUR_TWITTER_CONSUMER_KEY";
    private const string ConsumerSecret = "YOUR_TWITTER_CONSUMER_SECRET";
    private const string Prefix = "YOUR_CORS_EVERYWHERE_URL";
    private const string RequestTokenURL = "https://api.twitter.com/oauth/request_token";
    private const string AuthorizeURL = "https://api.twitter.com/oauth/authorize";
    private const string AccessTokenURL = "https://api.twitter.com/oauth/access_token";

    private string oauthToken;
    private string oauthTokenSecret;
    private string userId;
    private string userName;

    public GameObject LoginButton;
    public GameObject PINObject;
    public GameObject InfoObject;
    public Text pin;
    public Text info;

    public void Login()
    {
        StartCoroutine(RequestToken());
    }

    public void SubmitPIN()
    {
        PINObject.SetActive(false);
        StartCoroutine(GetAccessToken(pin.text));
    }

    private IEnumerator RequestToken()
    {
        // Step 1: Get a request token
        Dictionary<string, string> headers = GenerateOAuthHeaders("POST", RequestTokenURL, null);
        UnityWebRequest request = UnityWebRequest.PostWwwForm(Prefix + RequestTokenURL, "");
        foreach (KeyValuePair<string, string> header in headers)
        {
            request.SetRequestHeader(header.Key, header.Value);
        }
        request.SetRequestHeader("Origin", "google.com");

        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.Log("Error requesting token: " + request.error + ", " + request.downloadHandler.text);
            yield break;
        }

        // Step 2: Parse the request token response
        string response = request.downloadHandler.text;
        ParseOAuthTokenResponse(response);

        // Step 3: Redirect the user to authorize the app
        string authorizeURL = $"{AuthorizeURL}?oauth_token={oauthToken}";
        Debug.Log(authorizeURL);
        Application.OpenURL(authorizeURL);
        LoginButton.SetActive(false);
        PINObject.SetActive(true);
    }

    private IEnumerator GetAccessToken(string verifier)
    {
        Dictionary<string, string> headers = GenerateOAuthHeaders("POST", AccessTokenURL, null, verifier);
        UnityWebRequest request = UnityWebRequest.PostWwwForm($"{Prefix}{AccessTokenURL}?oauth_verifier={verifier}&oauth_token={oauthToken}", "");

        // In order to bypass the proxy error when served in Heroku
        request.SetRequestHeader("X-Requested-With", "CORS");
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.Log("Error getting access token: " + request.error + " " + headers.ToString());
            yield break;
        }

        string response = request.downloadHandler.text;
        Debug.Log(response);
        ParseOAuthTokenResponse(response);

        Debug.Log("Access Token: " + oauthToken);
        Debug.Log("Access Token Secret: " + oauthTokenSecret);
        Debug.Log("User ID: " + userId);
        Debug.Log("User Name: " + userName);
        InfoObject.SetActive(true);
        info.text = $"Welcome, {userName}!";
    }

    private void ParseOAuthTokenResponse(string response)
    {
        string[] parameters = response.Split('&');
        foreach (string parameter in parameters)
        {
            string[] parts = parameter.Split('=');
            if (parts.Length == 2)
            {
                if (parts[0] == "oauth_token")
                {
                    oauthToken = parts[1];
                }
                else if (parts[0] == "oauth_token_secret")
                {
                    oauthTokenSecret = parts[1];
                }
                else if (parts[0] == "user_id")
                {
                    userId = parts[1];
                }
                else if (parts[0] == "screen_name")
                {
                    userName = parts[1];
                }
            }
        }
    }

    private Dictionary<string, string> GenerateOAuthHeaders(string method, string url, Dictionary<string, string> parameters, string verifier = null)
    {
        Dictionary<string, string> headers = new Dictionary<string, string>();

        // OAuth parameters
        Dictionary<string, string> oauthParameters = new Dictionary<string, string>
        {
            {"oauth_consumer_key", ConsumerKey},
            {"oauth_nonce", GenerateNonce()},
            {"oauth_signature_method", "HMAC-SHA1"},
            {"oauth_timestamp", GenerateTimestamp()},
            {"oauth_version", "1.0"}
        };

        // Additional parameters (if any)
        if (parameters != null)
        {
            foreach (KeyValuePair<string, string> parameter in parameters)
            {
                oauthParameters.Add(parameter.Key, parameter.Value);
            }
        }

        // OAuth signature
        string signature = OAuthHelper.GenerateSignature(method, url, oauthParameters, ConsumerSecret, oauthTokenSecret, verifier);
        oauthParameters.Add("oauth_signature", signature);

        Debug.Log(signature);

        // Construct the Authorization header
        string authorizationHeader = OAuthHelper.GenerateAuthorizationHeader(oauthParameters);
        headers.Add("Authorization", authorizationHeader);

        Debug.Log(authorizationHeader);

        return headers;
    }

    private string GenerateNonce()
    {
        return new System.Random().Next(123400, 9999999).ToString();
    }

    private string GenerateTimestamp()
    {
        return ((int)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds).ToString();
    }
}
