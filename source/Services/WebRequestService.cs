using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading;
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

        public Stream Get(string endpoint, object args, string expectedContentType)
        {
            var url = RootUrl + endpoint + "/";
            if (args != null)
            {
                url += "?" + FormatArgs(args);
            }

            try
            {
                return GetInternal(url, expectedContentType);
            }
            catch (WebException ex)
            {
                if (ex.Status != WebExceptionStatus.Timeout &&
                    ex.Status != WebExceptionStatus.KeepAliveFailure &&
                    ex.Response != null &&
                    ((HttpWebResponse) ex.Response).StatusCode != HttpStatusCode.GatewayTimeout)
                {
                    throw;
                }

                Trace.TraceWarning(ex.Message + ", retrying in 10 seconds");
                Thread.Sleep(TimeSpan.FromSeconds(10));
                return GetInternal(url, expectedContentType);
            }
        }

        private Stream GetInternal(string url, string expectedContentType)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.CookieContainer = Cookies;
            request.UserAgent = "FitBot";
            var response = request.GetResponse();
            AssertResponseContentType(request, response, expectedContentType);
            return response.GetResponseStream();
        }

        public void Post(string endpoint, object data, NameValueCollection headers)
        {
            var url = RootUrl + endpoint + "/";
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.CookieContainer = Cookies;
            request.Method = "POST";
            request.Referer = url;
            request.UserAgent = "FitBot";
            if (data != null)
            {
                using (var stream = request.GetRequestStream())
                {
                    var bytes = Encoding.UTF8.GetBytes(FormatArgs(data));
                    stream.Write(bytes, 0, bytes.Length);
                }
            }

            var response = request.GetResponse();
            AssertResponseContentType(request, response, "application/json");

            JsonObject json;
            using (var stream = response.GetResponseStream())
            {
                json = JsonSerializer.DeserializeFromStream<JsonObject>(stream);
            }

            if (json != null && json["success"] != "true" && json["result"] != "true")
            {
                DumpLogFile(request, response, file => JsonSerializer.SerializeToStream(json, file));

                if (!string.IsNullOrEmpty(json["error"]))
                {
                    throw new ApplicationException($"Request '{endpoint}' failed: {json["error"]}");
                }
                if (!string.IsNullOrEmpty(json["reason"]))
                {
                    throw new ApplicationException($"Request '{endpoint}' failed: {json["reason"]}");
                }
                throw new ApplicationException($"Request '{endpoint}' failed with no reason");
            }

            if (headers != null)
            {
                foreach (string key in response.Headers)
                {
                    headers[key] = response.Headers[key];
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

        private static void AssertResponseContentType(WebRequest request, WebResponse response, string expectedContentType)
        {
            if (expectedContentType != null &&
                expectedContentType != response.ContentType &&
                new ContentType(expectedContentType).MediaType != new ContentType(response.ContentType).MediaType)
            {
                using (var stream = response.GetResponseStream())
                {
                    DumpLogFile(request, response, stream.CopyTo);
                }

                throw new ApplicationException($"Unexpected content type '{response.ContentType}' in response from '{request.RequestUri}'");
            }
        }

        private static void DumpLogFile(WebRequest request, WebResponse response, Action<Stream> writeContent)
        {
            using (var file = File.OpenWrite(string.Join("_", DateTime.UtcNow.ToString("yyyyMMddHHmmss"), nameof(WebRequestService)) + ".log"))
            {
                using (var writer = new StreamWriter(file))
                {
                    writer.Write(request.Method);
                    writer.Write(" ");
                    writer.WriteLine(request.RequestUri);
                    foreach (string key in request.Headers)
                    {
                        writer.WriteLine($"{key}: {request.Headers[key]}");
                    }
                    writer.WriteLine();

                    foreach (string key in response.Headers)
                    {
                        writer.WriteLine($"{key}: {response.Headers[key]}");
                    }
                    writer.WriteLine();

                    writer.Flush();
                    writeContent(file);
                }
            }
        }
    }
}