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

        public async Task<Stream> Get(string endpoint, object args, string expectedContentType, CancellationToken cancel)
        {
            await Delay(cancel);

            var attempt = 0;

            while (true)
            {
                try
                {
                    return await _decorated.Get(endpoint, args, expectedContentType, cancel);
                }
                catch (Exception ex) when (++attempt < 5 &&
                                           ex.GetBaseException() is WebException webEx &&
                                           (webEx.Status == WebExceptionStatus.Timeout ||
                                            webEx.Status == WebExceptionStatus.KeepAliveFailure ||
                                            webEx.Status == WebExceptionStatus.NameResolutionFailure ||
                                            webEx is {Response: HttpWebResponse {StatusCode: HttpStatusCode.GatewayTimeout}}))
                {
                    _logger.LogWarning("{0} on attempt {1}, retrying in 10 seconds", webEx.Message.TrimEnd('.', ','), attempt);
                    await Task.Delay(TimeSpan.FromSeconds(10), cancel);
                }
            }
        }

        public async Task Post(string endpoint, object data, NameValueCollection headers, CancellationToken cancel)
        {
            await Delay(cancel);
            await _decorated.Post(endpoint, data, headers, cancel);
        }

        private Task Delay(CancellationToken cancel) => Task.Delay(TimeSpan.FromSeconds(_random.NextDouble() + 0.5), cancel);
    }
}