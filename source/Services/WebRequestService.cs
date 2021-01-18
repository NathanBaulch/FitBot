using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace FitBot.Services
{
    public class WebRequestService : IWebRequestService
    {
        private const string RootUrl = "https://www.fitocracy.com/";

        public CookieContainer Cookies { get; set; } = new();

        public async Task<Stream> Get(string endpoint, object args, string expectedContentType, CancellationToken cancel)
        {
            var url = RootUrl + endpoint + "/";
            if (args != null)
            {
                url += "?" + FormatArgs(args);
            }

            var request = WebRequest.CreateHttp(url);
            request.CookieContainer = Cookies;
            request.UserAgent = "FitBot";
            var response = await GetResponse(request, expectedContentType, cancel);
            return response.GetResponseStream();
        }

        public async Task Post(string endpoint, object data, NameValueCollection headers, CancellationToken cancel)
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
                await stream.WriteAsync(bytes.AsMemory(0, bytes.Length), cancel);
            }

            var response = await GetResponse(request, "application/json", cancel);

            await using (var stream = response.GetResponseStream())
            using (var json = await JsonDocument.ParseAsync(stream, default, cancel))
            {
                var root = json.RootElement;
                if ((!root.TryGetProperty("success", out var success) || !success.GetBoolean()) &&
                    (!root.TryGetProperty("result", out var result) || !result.GetBoolean()))
                {
                    await DumpLogFile(request, response, file => JsonSerializer.SerializeAsync(file, json, new JsonSerializerOptions {WriteIndented = true}, cancel));

                    if (root.TryGetProperty("error", out var error) && !string.IsNullOrEmpty(error.GetString()))
                    {
                        throw new ApplicationException($"Request '{endpoint}' failed: {error}");
                    }
                    if (root.TryGetProperty("reason", out var reason) && !string.IsNullOrEmpty(reason.GetString()))
                    {
                        throw new ApplicationException($"Request '{endpoint}' failed: {reason}");
                    }
                    throw new ApplicationException($"Request '{endpoint}' failed with no reason");
                }
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

        private static async Task<WebResponse> GetResponse(WebRequest request, string expectedContentType, CancellationToken cancel)
        {
            await using (cancel.Register(request.Abort))
            {
                try
                {
                    var response = await request.GetResponseAsync();

                    if (expectedContentType != null &&
                        expectedContentType != response.ContentType &&
                        new ContentType(expectedContentType).MediaType != new ContentType(response.ContentType).MediaType)
                    {
                        await using var stream = response.GetResponseStream();
                        await DumpLogFile(request, response, file => stream.CopyToAsync(file, cancel));

                        throw new ApplicationException($"Unexpected content type '{response.ContentType}' in response from '{request.RequestUri}'");
                    }

                    return response;
                }
                catch (Exception ex) when (ex.GetBaseException() is WebException {Status: WebExceptionStatus.RequestCanceled})
                {
                    throw new OperationCanceledException(ex.Message, cancel);
                }
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