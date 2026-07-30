namespace StockScanTool.Application.Services;

public interface IApiKeyHasher
{
    string Hash(string apiKey);
}
