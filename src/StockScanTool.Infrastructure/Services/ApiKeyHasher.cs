using System.Security.Cryptography;
using System.Text;
using StockScanTool.Application.Services;

namespace StockScanTool.Infrastructure.Services;

public class ApiKeyHasher : IApiKeyHasher
{
    private readonly byte[] _key;

    public ApiKeyHasher(string pepper)
    {
        _key = Encoding.UTF8.GetBytes(pepper);
    }

    public string Hash(string apiKey)
    {
        var bytes = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes);
    }
}
