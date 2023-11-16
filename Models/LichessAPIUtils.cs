using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    
    public static Action<Game>? OnGameStarted;
    public static Action<Game>? OnGameEnded;

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

    public static async Task EventStreamAsync(CancellationToken ct)
    {
        string uri = "https://lichess.org/api/stream/event";
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(k_authBearer, KeychainHelper.GetTokenFromKeychain());
        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        Console.WriteLine("Starting event stream");
        while (AppController.Instance.CurrenAppState != AppStates.Authorization)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                using (HttpResponseMessage response =
                       await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct))
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                using (StreamReader reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream && AppController.Instance.CurrenAppState != AppStates.Authorization)
                    {
                        ct.ThrowIfCancellationRequested();

                        string line = await reader.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                        {
                            GameEvent gameEvent = JsonConvert.DeserializeObject<GameEvent>(line);

                            if (gameEvent.type is "gameStart" or "gameFinish")
                            {
                                Game game = gameEvent.Game;
                                if (gameEvent.type is "gameStart")
                                {
                                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                       OnGameStarted?.Invoke(game);
                                       Console.WriteLine("Game ID: " + game.fullId);
                                    });
                                }
                                else
                                {
                                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        OnGameEnded?.Invoke(game);
                                    });
                                }
                                Console.WriteLine("Game " + line);
                            }
                            else if (gameEvent.type is "challenge")
                            {
                                Console.WriteLine("Challenge " + line);
                            }
                            else if (gameEvent.type == "challengeCanceled")
                            {
                                Console.WriteLine("Challenge canceled " + line);
                            }
                            else
                            {
                                Console.WriteLine("Challenge declined " + line);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
    
    public static async Task RequestStreamAsync(string gameId, CancellationToken ct)
    {
        Console.WriteLine(gameId);
        string uri = $"https://lichess.org/api/board/game/stream/{gameId}";
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", KeychainHelper.GetTokenFromKeychain());

    while (AppController.Instance.HasActiveGame)
    {
        ct.ThrowIfCancellationRequested();
        
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct))
            using (Stream stream = await response.Content.ReadAsStreamAsync())
            using (StreamReader reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream && AppController.Instance.HasActiveGame)
                {
                    ct.ThrowIfCancellationRequested();

                    string line = await reader.ReadLineAsync();

                    Console.WriteLine(line);
                    if (string.IsNullOrEmpty(line))
                    {
                        Console.WriteLine("Ping!");
                    }
                    else 
                    {
                        
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
            Console.WriteLine("Error: " + e.Message);
            await Task.Delay(1000); 
        }
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