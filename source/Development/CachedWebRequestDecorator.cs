using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Services;
using Microsoft.Extensions.Logging;

namespace FitBot.Development
{
    public class CachedWebRequestDecorator : IWebRequestService
    {
        private readonly string _cacheDir = Path.GetFullPath("WebCache");
        private readonly IWebRequestService _decorated;
        private readonly ILogger<CachedWebRequestDecorator> _logger;
        private NameValueCollection _headers;

        public CachedWebRequestDecorator(IWebRequestService decorated, ILogger<CachedWebRequestDecorator> logger)
        {
            _decorated = decorated;
            _logger = logger;
            Directory.CreateDirectory(_cacheDir);
        }

        public CookieContainer Cookies
        {
            get => _decorated.Cookies;
            set => _decorated.Cookies = value;
        }

        public async Task<Stream> Get(string endpoint, object args, string expectedContentType, CancellationToken cancel)
        {
            var cacheFileName = Path.Combine(_cacheDir, string.Concat((endpoint + "_" + args).Replace(" ", "").Split(Path.GetInvalidFileNameChars())));
            if (endpoint == "accounts/login")
            {
                if (File.Exists(cacheFileName))
                {
                    await using var stream = File.OpenRead(cacheFileName);
                    var formatter = new BinaryFormatter();
#pragma warning disable 618,SYSLIB0011
                    Cookies = (CookieContainer) formatter.Deserialize(stream);
                    _headers = (NameValueCollection) formatter.Deserialize(stream);
#pragma warning restore 618,SYSLIB0011
                    return Stream.Null;
                }
                return await _decorated.Get(endpoint, args, expectedContentType, cancel);
            }
            if (!File.Exists(cacheFileName))
            {
                await using var source = await _decorated.Get(endpoint, args, expectedContentType, cancel);
                await using var destination = File.OpenWrite(cacheFileName);
                await source.CopyToAsync(destination, cancel);
            }
            return File.OpenRead(Path.Combine(_cacheDir, cacheFileName));
        }

        public async Task Post(string endpoint, object data, NameValueCollection headers, CancellationToken cancel)
        {
            if (endpoint == "accounts/login")
            {
                var cacheFileName = Path.Combine(_cacheDir, "accountslogin_");
                if (!File.Exists(cacheFileName))
                {
                    await _decorated.Post(endpoint, data, headers, cancel);
                    await using var stream = File.OpenWrite(cacheFileName);
                    var formatter = new BinaryFormatter();
#pragma warning disable 618,SYSLIB0011
                    formatter.Serialize(stream, Cookies);
                    formatter.Serialize(stream, headers);
#pragma warning restore 618,SYSLIB0011
                }
                else if (headers != null)
                {
                    foreach (string key in _headers)
                    {
                        headers[key] = _headers[key];
                    }
                }
            }
            else
            {
                _logger.LogDebug("Skip POST to " + endpoint);
            }
        }
    }
}