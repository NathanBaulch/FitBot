using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace FitBot.Services
{
    public class ThrottledWebRequestDecorator : IWebRequestService
    {
        private readonly Random _random = new Random();
        private readonly IWebRequestService _decorated;
        private int _throttleFactor;

        public ThrottledWebRequestDecorator(IWebRequestService decorated)
        {
            _decorated = decorated;
            ThrottleFactor = 10;
        }

        public int ThrottleFactor
        {
            get { return _throttleFactor; }
            set { _throttleFactor = Math.Max(value, 0); }
        }

        public CookieContainer Cookies
        {
            get { return _decorated.Cookies; }
            set { _decorated.Cookies = value; }
        }

        public async Task<Stream> Get(string endpoint, object args, string expectedContentType)
        {
            await Delay();
            return await _decorated.Get(endpoint, args, expectedContentType);
        }

        public async Task Post(string endpoint, object data, NameValueCollection headers)
        {
            await Delay();
            await _decorated.Post(endpoint, data, headers);
        }

        private Task Delay()
        {
            return Task.Delay((int) ((1 << ThrottleFactor)*(1 + _random.NextDouble())));
        }
    }
}