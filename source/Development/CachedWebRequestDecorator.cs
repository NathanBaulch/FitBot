using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using FitBot.Services;

namespace FitBot.Development
{
    public class CachedWebRequestDecorator : IWebRequestService
    {
        private readonly string _cacheDir = Path.GetFullPath("WebCache");
        private readonly IWebRequestService _decorated;
        private NameValueCollection _headers;

        public CachedWebRequestDecorator(IWebRequestService decorated)
        {
            _decorated = decorated;
            Directory.CreateDirectory(_cacheDir);
        }

        public CookieContainer Cookies
        {
            get => _decorated.Cookies;
            set => _decorated.Cookies = value;
        }

        public Stream Get(string endpoint, object args, string expectedContentType)
        {
            var cacheFileName = Path.Combine(_cacheDir, string.Concat((endpoint + "_" + args).Replace(" ", "").Split(Path.GetInvalidFileNameChars())));
            if (endpoint == "accounts/login")
            {
                if (File.Exists(cacheFileName))
                {
                    using (var stream = File.OpenRead(cacheFileName))
                    {
                        var formatter = new BinaryFormatter();
#pragma warning disable 618,SYSLIB0011
                        Cookies = (CookieContainer) formatter.Deserialize(stream);
                        _headers = (NameValueCollection) formatter.Deserialize(stream);
#pragma warning restore 618,SYSLIB0011
                    }
                    return Stream.Null;
                }
                return _decorated.Get(endpoint, args, expectedContentType);
            }
            if (!File.Exists(cacheFileName))
            {
                using (var source = _decorated.Get(endpoint, args, expectedContentType))
                using (var destination = File.OpenWrite(cacheFileName))
                {
                    source.CopyTo(destination);
                }
            }
            return File.OpenRead(Path.Combine(_cacheDir, cacheFileName));
        }

        public void Post(string endpoint, object data, NameValueCollection headers)
        {
            if (endpoint == "accounts/login")
            {
                var cacheFileName = Path.Combine(_cacheDir, "accountslogin_");
                if (!File.Exists(cacheFileName))
                {
                    _decorated.Post(endpoint, data, headers);
                    using (var stream = File.OpenWrite(cacheFileName))
                    {
                        var formatter = new BinaryFormatter();
#pragma warning disable 618,SYSLIB0011
                        formatter.Serialize(stream, Cookies);
                        formatter.Serialize(stream, headers);
#pragma warning restore 618,SYSLIB0011
                    }
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
                Trace.TraceInformation("Skip POST to " + endpoint);
            }
        }
    }
}