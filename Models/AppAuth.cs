using System;

namespace LichessClient.Models;

public abstract class AppAuth
{
    public const string clientId = "chess-dinikowski";
    public const string scope = "preference:read preference:write email:read challenge:read challenge:write challenge:bulk study:read study:write tournament:write racer:write team:read team:write team:lead follow:read follow:write msg:write board:play bot:play";
    public const string redirectUri = "http://localhost:8080/lichess/oauth2/";
    public const string authorizationEndpoint = "https://lichess.org/oauth/";
    public const string tokenEndpoint = "https://lichess.org/api/token";
    public const string profileEndpoint = "https://lichess.org/api/account";
    public const string gamesEndpoint = "https://lichess.org/api/account/playing";
}

[Serializable]
public class TokenResult
{
    public string token_type;
    public string access_token;
    public long expires_in;
}