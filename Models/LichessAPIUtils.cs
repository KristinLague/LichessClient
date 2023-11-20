using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LichessClient.Models.ChessEngine;
using Newtonsoft.Json;

namespace LichessClient.Models;

public class LichessAPIUtils
{
    private static readonly Lazy<LichessAPIUtils> m_Instance = new Lazy<LichessAPIUtils>(() => new LichessAPIUtils());
    public static LichessAPIUtils Instance => m_Instance.Value;
    
    private const string k_ProfileEnpoint = "https://lichess.org/api/account";
    private const string k_GamesEndpoint = "https://lichess.org/api/account/playing";
    
    public static Action<GameFull>? OnGameStarted;
    public static Action<GameState>? OnBoardUpdated;
    public static Action<GameState>? OnGameOver;
    public static Action<GameState>? OnDrawOffered;

    private const int k_DelayOnError = 5000;
    private const int k_DelayOnRateLimit = 60000;

    public string Username { get; private set; }

    private HttpClient m_EventStreamClient;
    private HttpClient m_GameStreamClient;
    private HttpClient m_RequestClient;

    private LichessAPIUtils()
    {
        m_RequestClient = GetHttpClient();
        m_EventStreamClient = GetHttpClient();
        m_GameStreamClient = GetHttpClient();
    }
    

    public async void TryGetProfile(Action<Profile> onSuccess)
    {
        try
        {
            var response = await m_RequestClient.GetAsync(k_ProfileEnpoint);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    //TODO: Deal with expired tokens correctly
                    Console.WriteLine("Token invalid or expired - get new token!");
                }
                else
                {
                    Console.WriteLine($"Error: {response.ReasonPhrase}");
                }
            }
            else
            {
                var json = await response.Content.ReadAsStringAsync();
                Profile profile = JsonConvert.DeserializeObject<Profile>(json);
                Username = profile.username;
                onSuccess?.Invoke(profile);
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request exception: {e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"General exception: {e.Message}");
        }
    }

    public async void TryGetGames(Action<ActiveGames> onSuccess)
    {
        try
        {
            var response = await m_RequestClient.GetAsync(k_GamesEndpoint);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("Token invalid or expired - get new token!");
                }
                else
                {
                    Console.WriteLine($"Error: {response.ReasonPhrase}");
                }
            }
            else
            {
                var json = await response.Content.ReadAsStringAsync();
                ActiveGames games = JsonConvert.DeserializeObject<ActiveGames>(json);
                onSuccess?.Invoke(games);
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request exception: {e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"General exception: {e.Message}");
        }
    }

    public async Task MakeMove(string gameId, string move)
    {
        string uri = $"https://lichess.org/api/board/game/{gameId}/move/{move}";

        try
        {
            HttpResponseMessage response = await m_RequestClient.PostAsync(uri, null);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Move {move} made successfully in game {gameId}.");
            }
            else
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to make move: {response.StatusCode} - {responseContent}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception occurred: {e.Message}");
        }
    }

    public async Task HandleDrawOfferAsync(string gameId, bool accept)
    {
        string uri = $"https://lichess.org/api/board/game/{gameId}/draw/{(accept ? "yes" : "no")}";

        try
        {
            HttpResponseMessage response = await m_RequestClient.PostAsync(uri, null);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Responded successfully to draw request for {gameId}.");
            }
            else
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to respond to draw request: {response.StatusCode} - {responseContent}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception occurred: {e.Message}");
        }
    }

    public async Task ResignGame(string gameId)
    {
        string uri = $"https://lichess.org/api/board/game/{gameId}/resign";

        try
        {
            HttpResponseMessage response = await m_RequestClient.PostAsync(uri, null);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Responded successfully to resign for {gameId}.");
            }
            else
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to resign: {response.StatusCode} - {responseContent}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception occurred: {e.Message}");
        }
    }

    public HttpClient GetHttpClient()
    {
        HttpClient client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(2f);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AppAuth.k_AuthBearer, KeychainHelper.GetTokenFromKeychain());
        return client;
    }
    
    public async Task SeekGameAsync(bool rated, float clockLimit, int increment, CancellationToken ct,Action<bool> onCompletion)
    {
        SeekRequestData requestData = new SeekRequestData
        {
            rated = rated,
            time = (int)clockLimit,
            increment = (int)increment,
            variant = "standard",
            color = "random"
        };

        string json = JsonConvert.SerializeObject(requestData);
        Console.WriteLine(json);

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AppAuth.k_AuthBearer, KeychainHelper.GetTokenFromKeychain());
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync("https://lichess.org/api/board/seek", content, ct);
                string responseContent = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error: {response.ReasonPhrase}");
                    Console.WriteLine(responseContent);
                    onCompletion?.Invoke(false);
                    return;
                }

                Console.WriteLine(responseContent);
                onCompletion?.Invoke(true);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Request was cancelled.");
                onCompletion?.Invoke(false);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e.Message}");
                onCompletion?.Invoke(false);
            }
        }
    }


    
    public async Task EventStreamAsync(CancellationToken ct)
    {
        string uri = "https://lichess.org/api/stream/event";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using (HttpResponseMessage response = await m_EventStreamClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                Console.WriteLine($"Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                    return;
                }

                using (Stream stream = await response.Content.ReadAsStreamAsync())
                using (StreamReader reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        ct.ThrowIfCancellationRequested();

                        string line = await reader.ReadLineAsync();
                        if (line == null)
                        {
                            continue;
                        }

                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine("EVENT: " + line);
                            Console.ResetColor();
                        });
                    }
                }
            }
        }
        catch (OperationCanceledException oce)
        {
            Console.WriteLine($"Event stream was cancelled: {oce.Message}");
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error occurred: {e.Message}");
            if (e.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {e.InnerException.Message}");
            }
            await Task.Delay(k_DelayOnError);
        }
    }
    
    public async Task GameStreamAsync(string gameId, CancellationToken ct)
    {
        string uri = $"https://lichess.org/api/board/game/stream/{gameId}";
       
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using (HttpResponseMessage response = await m_GameStreamClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                    return;
                }

                using (Stream stream = await response.Content.ReadAsStreamAsync())
                using (StreamReader reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        ct.ThrowIfCancellationRequested();
                        
                        string line = await reader.ReadLineAsync();
                        Avalonia.Threading.Dispatcher.UIThread.Post((() =>
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("GAME: " + line);
                            Console.ResetColor();
                        }));
                        
                        if (!string.IsNullOrEmpty(line))
                        {
                            if (line.Contains("gameFull"))
                            {
                                GameFull gameFull = JsonConvert.DeserializeObject<GameFull>(line);
                                Avalonia.Threading.Dispatcher.UIThread.Post(() => OnGameStarted?.Invoke(gameFull));
                            }
                            else if (line.Contains("chatLine"))
                            {
                                
                            }
                            else
                            {
                                GameState gameState = JsonConvert.DeserializeObject<GameState>(line);
                                Console.WriteLine("GAMESTATE " + gameState.status);
                                if (gameState.wdraw != false || gameState.bdraw != false)
                                {
                                    Avalonia.Threading.Dispatcher.UIThread.Post((() =>
                                    {
                                        OnDrawOffered?.Invoke(gameState);
                                    }));
                                }
                                else
                                {
                                    Avalonia.Threading.Dispatcher.UIThread.Post(() => OnBoardUpdated?.Invoke(gameState));
                                }

                                if (gameState.status == "mate" || gameState.status == "resign" ||
                                    gameState.status == "draw" || gameState.status == "aborted" ||
                                    gameState.status == "stalemate")
                                {
                                    
                                    Avalonia.Threading.Dispatcher.UIThread.Post(() => OnGameOver?.Invoke(gameState));
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Task was cancelled.");
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            await Task.Delay(k_DelayOnError);
        }
    }
    
    private static async Task HandleErrorResponse(HttpResponseMessage response)
    {
        if (response.StatusCode == (HttpStatusCode)429) // Too Many Requests
        {
            Console.WriteLine("Rate limit hit, waiting...");
            await Task.Delay(k_DelayOnRateLimit);
        }
        else
        {
            Console.WriteLine($"HTTP error: {response.StatusCode}");
            await Task.Delay(k_DelayOnError);
        }
    }
}
