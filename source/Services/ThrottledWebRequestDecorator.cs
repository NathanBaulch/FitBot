using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FitBot.Services
{
    public class ThrottledWebRequestDecorator : IWebRequestService
    {
        private readonly Random _random = new();
        private readonly IWebRequestService _decorated;
        private readonly ILogger<ThrottledWebRequestDecorator> _logger;

        public ThrottledWebRequestDecorator(IWebRequestService decorated, ILogger<ThrottledWebRequestDecorator> logger)
        {
            _decorated = decorated;
            _logger = logger;
        }

        public CookieContainer Cookies
        {
            get => _decorated.Cookies;
            set => _decorated.Cookies = value;
        }

        public Stream Get(string endpoint, object args, string expectedContentType, CancellationToken cancel)
        {
            Delay(cancel);

            try
            {
                return _decorated.Get(endpoint, args, expectedContentType, cancel);
            }
            catch (Exception ex) when (ex.GetBaseException() is WebException webEx &&
                                       (webEx.Status == WebExceptionStatus.Timeout ||
                                        webEx.Status == WebExceptionStatus.KeepAliveFailure ||
                                        webEx.Status == WebExceptionStatus.NameResolutionFailure ||
                                        ((HttpWebResponse) webEx.Response)?.StatusCode == HttpStatusCode.GatewayTimeout))
            {
                _logger.LogWarning(webEx.Message + ", retrying in 10 seconds");
                Task.Delay(TimeSpan.FromSeconds(10), cancel).Wait(cancel);
                return _decorated.Get(endpoint, args, expectedContentType, cancel);
            }
        }

        public void Post(string endpoint, object data, NameValueCollection headers, CancellationToken cancel)
        {
            Delay(cancel);
            _decorated.Post(endpoint, data, headers, cancel);
        }

        private void Delay(CancellationToken cancel) => Task.Delay(TimeSpan.FromSeconds(_random.NextDouble() + 0.5), cancel).Wait(cancel);
    }
}