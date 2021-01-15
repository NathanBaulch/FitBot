using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading;

namespace FitBot.Services
{
    public interface IWebRequestService
    {
        CookieContainer Cookies { get; set; }
        Stream Get(string endpoint, object args = null, string expectedContentType = null, CancellationToken cancel = default);
        void Post(string endpoint, object data = null, NameValueCollection headers = null, CancellationToken cancel = default);
    }
}