using System;
using System.Collections.Specialized;
using System.Diagnostics;
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

        public CookieContainer Cookies { get; set; } = new();

        public async Task<Stream> Get(string endpoint, object args, string expectedContentType)
        {
            var url = RootUrl + endpoint + "/";
            if (args != null)
            {
                url += "?" + FormatArgs(args);
            }

            try
            {
                return await GetInternal(url, expectedContentType);
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.Timeout ||
                                          ex.Status == WebExceptionStatus.KeepAliveFailure ||
                                          ((HttpWebResponse) ex.Response)?.StatusCode == HttpStatusCode.GatewayTimeout)
            {
                Trace.TraceWarning(ex.Message + ", retrying in 10 seconds");
                await Task.Delay(TimeSpan.FromSeconds(10));
                return await GetInternal(url, expectedContentType);
            }
        }

        private async Task<Stream> GetInternal(string url, string expectedContentType)
        {
            var request = WebRequest.CreateHttp(url);
            request.CookieContainer = Cookies;
            request.UserAgent = "FitBot";
            var response = await request.GetResponseAsync();
            await AssertResponseContentType(request, response, expectedContentType);
            return response.GetResponseStream();
        }

        public async Task Post(string endpoint, object data, NameValueCollection headers)
        {
            var url = RootUrl + endpoint + "/";
            var request = WebRequest.CreateHttp(url);
            request.CookieContainer = Cookies;
            request.Method = "POST";
            request.Referer = url;
            request.UserAgent = "FitBot";
            if (data != null)
            {
                await using var stream = await request.GetRequestStreamAsync();
                var bytes = Encoding.UTF8.GetBytes(FormatArgs(data));
                await stream.WriteAsync(bytes.AsMemory(0, bytes.Length));
            }

            var response = await request.GetResponseAsync();
            await AssertResponseContentType(request, response, "application/json");

            JsonObject json;
            await using (var stream = response.GetResponseStream())
            {
                json = JsonSerializer.DeserializeFromStream<JsonObject>(stream);
            }

            if (json != null && json["success"] != "true" && json["result"] != "true")
            {
                await DumpLogFile(request, response, file =>
                {
                    JsonSerializer.SerializeToStream(json, file);
                    return Task.CompletedTask;
                });

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

        private static async Task AssertResponseContentType(WebRequest request, WebResponse response, string expectedContentType)
        {
            if (expectedContentType != null &&
                expectedContentType != response.ContentType &&
                new ContentType(expectedContentType).MediaType != new ContentType(response.ContentType).MediaType)
            {
                await using var stream = response.GetResponseStream();
                await DumpLogFile(request, response, stream.CopyToAsync);

                throw new ApplicationException($"Unexpected content type '{response.ContentType}' in response from '{request.RequestUri}'");
            }
        }

        private static async Task DumpLogFile(WebRequest request, WebResponse response, Func<Stream, Task> writeContent)
        {
            await using var file = File.OpenWrite(string.Join("_", DateTime.UtcNow.ToString("yyyyMMddHHmmss"), nameof(WebRequestService)) + ".log");
            await using var writer = new StreamWriter(file);
            await writer.WriteAsync(request.Method);
            await writer.WriteAsync(" ");
            writer.WriteLine(request.RequestUri);
            foreach (string key in request.Headers)
            {
                await writer.WriteLineAsync($"{key}: {request.Headers[key]}");
            }
            await writer.WriteLineAsync();

            foreach (string key in response.Headers)
            {
                await writer.WriteLineAsync($"{key}: {response.Headers[key]}");
            }
            await writer.WriteLineAsync();

            await writer.FlushAsync();
            await writeContent(file);
        }
    }
}