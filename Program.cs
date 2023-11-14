using Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using IdentityModel;
using IdentityModel.Client;
using LichessClient;
using LichessClient.Models;
using Newtonsoft.Json;
using Xamarin.Essentials;

class Program
{
    private static string m_codeVerifier;
    private const string s_serviceName = "OauthTokenService";
    private const string s_accountName = "LichessClient";
    private static string m_codeChallenge;
    
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace().UseReactiveUI();

    public static bool HasToken()
    {
        Console.WriteLine("LOGGED "+ KeychainHelper.GetTokenFromKeychain(s_serviceName, s_accountName));
        return !string.IsNullOrEmpty(KeychainHelper.GetTokenFromKeychain(s_serviceName, s_accountName));
    }
    public static void StartAuthentication()
    {
        m_codeVerifier = CryptoRandom.CreateUniqueId(32);

        // Generate code challenge
        var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(m_codeVerifier));
        m_codeChallenge = Base64Url.Encode(challengeBytes);
        
        var requestUrl = new RequestUrl(AppAuth.authorizationEndpoint).CreateAuthorizeUrl(
            responseType: "code",
            clientId: AppAuth.clientId,
            redirectUri: AppAuth.redirectUri,
            codeChallengeMethod: "S256", 
            codeChallenge: m_codeChallenge,
            scope: AppAuth.scope
        );

        Console.WriteLine($"Authorization Request URL: {requestUrl}");
        Console.WriteLine($"Code Verifier: {m_codeVerifier}");
        
        
        Process.Start(new ProcessStartInfo(requestUrl) { UseShellExecute = true });
        ListenForOAuthRedirect();
    }
    
    private static async void ListenForOAuthRedirect()
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
                Console.WriteLine(tokenResponse.access_token);
                KeychainHelper.AddTokenToKeychain(s_serviceName, s_accountName, tokenResponse.access_token);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HttpListener: {ex.Message}");
        }
        finally
        {
            listener.Close();
        }
    }


    public static async Task<TokenResult> ExchangeAuthorizationCodeForToken(string authorizationCode)
    {
        using (var client = new HttpClient())
        {
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("code_verifier", m_codeVerifier),
                new KeyValuePair<string, string>("redirect_uri", AppAuth.redirectUri),
                new KeyValuePair<string, string>("client_id", AppAuth.clientId)
            });

            try
            {
                var response = await client.PostAsync(AppAuth.tokenEndpoint, requestContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error during token exchange: {response.StatusCode}");
                    Console.WriteLine($"Error content: {responseContent}");
                    return null; // or handle the error appropriately
                }

                // Assuming responseContent is in JSON format. Adjust the parsing based on the actual response format.
                Console.WriteLine(responseContent);
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
