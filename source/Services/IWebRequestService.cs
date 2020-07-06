using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace FitBot.Services
{
    public interface IWebRequestService
    {
        CookieContainer Cookies { get; set; }
        Stream Get(string endpoint, object args = null, string expectedContentType = null);
        void Post(string endpoint, object data = null, NameValueCollection headers = null);
    }
}