using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Text;

namespace FitBot.Services
{
    public class WebRequestService : IWebRequestService
    {
        private const string RootUrl = "https://www.fitocracy.com/";

        public WebRequestService()
        {
            Cookies = new CookieContainer();
        }

        public CookieContainer Cookies { get; set; }

        public async Task<Stream> Get(string endpoint, object args, string expectedContentType)
        {
            var url = RootUrl + endpoint + "/";
            if (args != null)
            {
                url += "?" + FormatArgs(args);
            }
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.CookieContainer = Cookies;
            request.UserAgent = "FitBot";
            var response = await request.GetResponseAsync();
            if (expectedContentType != null &&
                expectedContentType != response.ContentType &&
                new ContentType(expectedContentType).MediaType != new ContentType(response.ContentType).MediaType)
            {
                throw new ApplicationException("Unexpected content type: " + response.ContentType);
            }
            return response.GetResponseStream();
        }

        public async Task Post(string endpoint, object data, NameValueCollection headers)
        {
            var url = RootUrl + endpoint + "/";
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.CookieContainer = Cookies;
            request.Method = "POST";
            request.Referer = url;
            request.UserAgent = "FitBot";
            if (data != null)
            {
                using (var stream = await request.GetRequestStreamAsync())
                {
                    var bytes = Encoding.UTF8.GetBytes(FormatArgs(data));
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
            using (var response = await request.GetResponseAsync())
            {
                if (response.ContentType != "application/json")
                {
                    throw new ApplicationException("Unexpected content type: " + response.ContentType);
                }

                JsonObject json;
                using (var stream = response.GetResponseStream())
                {
                    json = JsonSerializer.DeserializeFromStream<JsonObject>(stream);
                }

                if (json != null && json["success"] != "true" && json["result"] != "true")
                {
                    if (!string.IsNullOrEmpty(json["error"]))
                    {
                        throw new ApplicationException(json["error"]);
                    }
                    if (!string.IsNullOrEmpty(json["reason"]))
                    {
                        throw new ApplicationException(json["reason"]);
                    }
                    throw new ApplicationException("Unknown server failure");
                }

                if (headers != null)
                {
                    foreach (string key in response.Headers)
                    {
                        headers[key] = response.Headers[key];
                    }
                }
            }
        }

        private static string FormatArgs(object args)
        {
            return string.Join(
                "&",
                args.GetType()
                    .GetProperties()
                    .Select(prop => $"{HttpUtility.UrlEncode(prop.Name)}={HttpUtility.UrlEncode(prop.GetValue(args).ToString())}"));
        }
    }
}