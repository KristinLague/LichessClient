using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using Newtonsoft.Json;

namespace LichessClient.Models;

public class AuthorizationProcess
{
    public delegate void AuthEventHandler(object sender, AuthEventArgs e);
    public event AuthEventHandler OnAuthCompleted
    {
        add => s_AuthCompleted += value;
        remove => s_AuthCompleted -= value;
    }
    
    private static event AuthEventHandler s_AuthCompleted;
    private static string s_codeChallenge;
    private static string s_codeVerifier;

    private void AuthCompleted(bool isSuccessful)
    {
        var e = new AuthEventArgs(isSuccessful);
        s_AuthCompleted?.Invoke(this, e);
    }
    public static bool HasToken()
    {
        return !string.IsNullOrEmpty(KeychainHelper.GetTokenFromKeychain());
    }
    public void StartAuthentication()
    {
        s_codeVerifier = CryptoRandom.CreateUniqueId(32);
        
        var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(s_codeVerifier));
        s_codeChallenge = Base64Url.Encode(challengeBytes);
        
        var requestUrl = new RequestUrl(AppAuth.authorizationEndpoint).CreateAuthorizeUrl(
            responseType: "code",
            clientId: AppAuth.clientId,
            redirectUri: AppAuth.redirectUri,
            codeChallengeMethod: "S256", 
            codeChallenge: s_codeChallenge,
            scope: AppAuth.scope
        );
        
        Process.Start(new ProcessStartInfo(requestUrl) { UseShellExecute = true });
        ListenForOAuthRedirect();
    }
    
    private async void ListenForOAuthRedirect()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add(AppAuth.redirectUri);
        try
        {
            Console.WriteLine($"Listening for OAuth redirect at: {AppAuth.redirectUri}");
            listener.Start();

            var context = await listener.GetContextAsync();
            var code = context.Request.QueryString.Get("code");
            Console.WriteLine($"Received authorization code: {code}");

            // Send a response to the browser
            var response = context.Response;
            string responseString = "<html><body>You can return to the app.</body></html>";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            responseOutput.Write(buffer, 0, buffer.Length);
            responseOutput.Close();

            if (!string.IsNullOrEmpty(code))
            {
                var tokenResponse = await ExchangeAuthorizationCodeForToken(code);
                //Store token using KeyChain on MacOS
                //TODO: Add support for Windows and Linux to store tokens
                KeychainHelper.AddTokenToKeychain(tokenResponse.access_token);
                AuthCompleted(true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HttpListener: {ex.Message}");
            AuthCompleted(false);
        }
        finally
        {
            listener.Close();
        }
    }

    private static async Task<TokenResult> ExchangeAuthorizationCodeForToken(string authorizationCode)
    {
        using (var client = new HttpClient())
        {
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("code_verifier", s_codeVerifier),
                new KeyValuePair<string, string>("redirect_uri", AppAuth.redirectUri),
                new KeyValuePair<string, string>("client_id", AppAuth.clientId)
            });

            try
            {
                var response = await client.PostAsync(AppAuth.tokenEndpoint, requestContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return null; 
                }
                
                var tokenResponse = JsonConvert.DeserializeObject<TokenResult>(responseContent);
                return tokenResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during token exchange: {ex.Message}");
                return null; // or handle the exception appropriately
            }
        }
    }
}

public class AuthEventArgs : EventArgs
{
    public bool IsSuccessful { get; private set; }
    public AuthEventArgs(bool isSuccessful)
    {
        IsSuccessful = isSuccessful;
    }
}