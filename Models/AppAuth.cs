
namespace LichessClient.Models;

public abstract class AppAuth
{
    public const string k_AuthBearer = "Bearer";
    public const string k_ClientId = "chess-dinikowski";
    public const string k_Scope = "challenge:read challenge:write challenge:bulk  board:play";
    public const string k_AuthRedirectUri = "http://localhost:8080/lichess/oauth2/";
    public const string k_AuthorizationEndpoint = "https://lichess.org/oauth/";
    public const string k_TokenEndpoint = "https://lichess.org/api/token";
}