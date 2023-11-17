using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LichessClient.Models;

public class LichessAPIUtils
{
    private static readonly HttpClient client = new HttpClient();
    private const string k_authBearer = "Bearer";
    
    public static Action<GameFull>? OnGameStarted;
    public static Action<GameState>? OnBoardUpdated;
    public static Action<GameState>? OnGameOver;
    public static Action<GameState>? OnDrawOffered;
    
    private const int DelayOnError = 5000;
    private const int DelayOnRateLimit = 60000; 
    
    public static string Username { get; private set; }

    public static async void TryGetProfile(Action<Profile> onSuccess)
    {
        try
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(k_authBearer,
                    KeychainHelper.GetTokenFromKeychain());
            var response = await client.GetAsync(AppAuth.profileEndpoint);

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
                Console.WriteLine("Profile: " + json);
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

    public static async void TryGetGames(Action<ActiveGames> onSuccess)
    {
        try
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(k_authBearer,
                    KeychainHelper.GetTokenFromKeychain());
            var response = await client.GetAsync(AppAuth.gamesEndpoint);

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
                Console.WriteLine("Active Games: " + json);
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
    
    public static async Task MakeMove(string gameId, string move)
    {
        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromMinutes(2f);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", KeychainHelper.GetTokenFromKeychain());
            
            string uri = $"https://lichess.org/api/board/game/{gameId}/move/{move}";

            try
            {
                // Send a POST request to the URI
                HttpResponseMessage response = await client.PostAsync(uri, null);

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
    }
    
    public static async Task HandleDrawOfferAsync(string gameId, bool accept)
    {
        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromMinutes(2f);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", KeychainHelper.GetTokenFromKeychain());
            
            string uri = $"https://lichess.org/api/board/game/{gameId}/draw/{(accept ? "yes" : "no")}";

            try
            {
                // Send a POST request to the URI
                HttpResponseMessage response = await client.PostAsync(uri, null);

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
    }

    public static async Task ResignGame(string gameId)
    {
        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromMinutes(2f);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", KeychainHelper.GetTokenFromKeychain());
            
            string uri = $"https://lichess.org/api/board/game/{gameId}/resign";
            
            try
            {
                // Send a POST request to the URI
                HttpResponseMessage response = await client.PostAsync(uri, null);

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
    }

    public static void InitializeClient()
    {
        client.Timeout = TimeSpan.FromMinutes(2f);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(k_authBearer, KeychainHelper.GetTokenFromKeychain());
    }

    public static async Task EventStreamAsync(CancellationToken ct)
    {
        HttpClient eventClient = new HttpClient();
        eventClient.Timeout = TimeSpan.FromMinutes(2f);
        eventClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(k_authBearer, KeychainHelper.GetTokenFromKeychain());

        string uri = "https://lichess.org/api/stream/event";
        Console.WriteLine("Starting event stream");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using (HttpResponseMessage response = await eventClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct))
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
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("EVENT: " + line);
                        Console.ResetColor();
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
            await Task.Delay(DelayOnError);
        }
    }


    public static async Task RequestStreamAsync(string gameId, CancellationToken ct)
    {
        HttpClient requestClient = new HttpClient();
        requestClient.Timeout = TimeSpan.FromMinutes(2f);
        requestClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(k_authBearer, KeychainHelper.GetTokenFromKeychain());

        string uri = $"https://lichess.org/api/board/game/stream/{gameId}";
        Console.WriteLine($"Requesting Stream for {gameId}");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using (HttpResponseMessage response = await requestClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct))
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
                                Console.WriteLine("GAMEFULL " + gameFull.id);
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
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine("DRAW OFFERED");
                                        Console.ResetColor();
                                        
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
                                    Console.WriteLine("Game ended by " + gameState.status);
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
            await Task.Delay(DelayOnError);
        }
    }


    private static async Task HandleErrorResponse(HttpResponseMessage response)
    {
        if (response.StatusCode == (HttpStatusCode)429) // Too Many Requests
        {
            Console.WriteLine("Rate limit hit, waiting...");
            await Task.Delay(DelayOnRateLimit);
        }
        else
        {
            Console.WriteLine($"HTTP error: {response.StatusCode}");
            await Task.Delay(DelayOnError);
        }
    }
}


[Serializable]
public struct Profile
{
    public string id;
    public string username;
    public Perfs perfs;
    public long createdAt;
    public long seenAt;
    public PlayTime playTime;
    public string url;
    public string playing;
    public Count count;
    public bool followable;
    public bool following;
    public bool blocking;
    public bool followsYou;
}

[Serializable]
public struct Blitz
{
    public int games;
    public int rating;
    public int rd;
    public int prog;
    public bool prov;
}

[Serializable]
public struct Storm
{
    public int runs;
    public int score;
}

[Serializable]
public struct Puzzle
{
    public int games;
    public int rating;
    public int rd;
    public int prog;
    
}

[Serializable]
public struct Bullet
{
    public int games;
    public int rating;
    public int rd;
    public int prog;
    public bool prov;
}

[Serializable]
public struct Correspondence
{
    public int games;
    public int rating;
    public int rd;
    public int prog;
    public bool prov;
}

[Serializable]
public struct Classical
{
    public int games;
    public int rating;
    public int rd;
    public int prog;
    public bool prov;
}

[Serializable]
public struct Rapid
{
    public int games;
    public int rating;
    public int rd;
    public int prog;
    public bool prov;
}

[Serializable]
public struct PlayTime
{
    public int total;
    public int tv;
}

[Serializable]
public struct Count
{
    public int all;
    public int rated;
    public int ai;
    public int draw;
    public int drawH;
    public int loss;
    public int lossH;
    public int win;
    public int winH;
    public int bookmark;
    public int playing;
    public int import;
    public int me;
}

// This class represents the "gameState" type
[Serializable]
public class GameState
{
    public string type;
    public string moves;
    public long wtime;
    public long btime;
    public int winc;
    public int binc;
    public string status;
    public string winner;
    public bool wdraw;
    public bool bdraw;
}

// This is a generic class to identify the type
[Serializable]
public class Generic
{
    public string type;
}

[Serializable]
public class GameFull
{
    public string id;
    public bool rated;
    public Variant variant;
    public Clock clock;
    public string speed;
    public Perf perf;
    public long createdAt;
    public White white;
    public Black black;
    public string initialFen;
    public GameState state;

}

[Serializable]
public class Variant
{
    public string key;
    public string name;
}

[Serializable]
public class Clock
{
    public int initial;
    public int increment;
}

[Serializable]
public class TournamentClock
{
    public int limit;
    public int increment;
}

[Serializable]
public class Perfs
{
    public Blitz blitz;
    public Storm storm;
    public Puzzle puzzle;
    public Bullet bullet;
    public Correspondence correspondence;
    public Classical classical;
    public Rapid rapid;
}

[Serializable]
public class Perf
{
    public string name;
}

[Serializable]
public class White
{
    public string id;
    public string name;
    public bool provisional;
    public int rating;
    public string title;
}

[Serializable]
public class Black
{
    public string id;
    public string name;
    public bool provisional;
    public int rating;
    public string title;
}

[Serializable]
public class Status
{
    public int id;
    public string name;
}

[Serializable]
public class Opponent
{
    public string id;
    public string username;
    public int rating;
}

[Serializable]
public class ActiveGames
{
    public ActiveGame[] nowPlaying;
}

[Serializable]
public class ActiveGame
{
    public string fullId;
    public string gameId;
    public string fen;
    public string color;
    public string lastMove;
    public string source;
    public Status status;
    public Variant variant;
    public string speed;
    public string perf;
    public bool rated;
    public bool hasMoved;
    public Opponent opponent;
    public bool isMyTurn;
}

[Serializable]
public class GameEvent
{
    public string type;
    public Game Game;
}

[Serializable]
public class Game
{
    public string fullId;
    public string gameId;
    public string fen;
    public string color;
    public string lastMove;
    public string source;
    public Status status;
    public Variant variant;
    public string speed;
    public string perf;
    public bool rated;
    public bool hasMoved;
    public Opponent opponent;
    public bool isMyTurn;
    public int secondsLeft;
    public Compat compat;
    public string id;
}

[Serializable]
public class ArenaGame
{
    public string type;
    public Game game;
    public Variant variant;
    public string speed;
    public string perf;
    public bool rated;
    public bool hasMoved;
    public Opponent opponent;
    public bool isMyTurn;
    public int secondsLeft;
    public string tournamentId;
    public Compat compat;
    public string id;
}

[Serializable]
public class Compat
{
    public bool bot;
    public bool board;
}

[Serializable]
public class ChatMessage
{
    public string text;
    public string user;
}

[Serializable]
public class MessageList
{
    public List<ChatMessage> messages;
}

[Serializable]
public class ChatLine
{
    public string type;
    public string room;
    public string username;
    public string text;
}

[Serializable]
public class Tournaments
{
    public List<Tournament> created;
    public List<Tournament> started;
    public List<Tournament> finished;
}

[Serializable]
public class Tournament
{
    public string id;
    public string createdBy;
    public string system;
    public int minutes;
    public TournamentClock clock;
    public bool rated;
    public string fullName;
    public int nbPlayers;
    public Variant variant;
    public long startsAt;
    public long endsAt;
    public string status;
    public Perf perf;
    public int secondsToStart;

}