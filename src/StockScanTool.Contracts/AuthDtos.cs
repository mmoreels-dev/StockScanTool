namespace StockScanTool.Contracts;

public record AdminLoginRequest(string Username, string Password);
public record AdminLoginResponse(string Token, string RefreshToken, int ExpiresInMinutes);
public record RefreshTokenRequest(string RefreshToken);
public record RefreshTokenResponse(string Token, string RefreshToken, int ExpiresInMinutes);
public record RevokeRefreshTokenRequest(string? RefreshToken = null);
