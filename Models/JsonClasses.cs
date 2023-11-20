using System;
using System.Collections.Generic;

namespace LichessClient.Models.ChessEngine;

[Serializable]
public class TokenResult
{
    public string token_type;
    public string access_token;
    public long expires_in;
}

public class SeekRequestData
{
    public bool rated;
    public int time;
    public int increment;
    public string variant;
    public string color;
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