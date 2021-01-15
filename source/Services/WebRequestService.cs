﻿using System;
using System.Collections.Specialized;
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

        public CookieContainer Cookies { get; set; } = new();

        public Stream Get(string endpoint, object args, string expectedContentType, CancellationToken cancel)
        {
            var url = RootUrl + endpoint + "/";
            if (args != null)
            {
                url += "?" + FormatArgs(args);
            }

            var request = WebRequest.CreateHttp(url);
            request.CookieContainer = Cookies;
            request.UserAgent = "FitBot";
            var response = GetResponse(request, expectedContentType, cancel);
            return response.GetResponseStream();
        }

        public void Post(string endpoint, object data, NameValueCollection headers, CancellationToken cancel)
        {
            var url = RootUrl + endpoint + "/";
            var request = WebRequest.CreateHttp(url);
            request.CookieContainer = Cookies;
            request.Method = "POST";
            request.Referer = url;
            request.UserAgent = "FitBot";
            if (data != null)
            {
                using var stream = request.GetRequestStream();
                var bytes = Encoding.UTF8.GetBytes(FormatArgs(data));
                stream.Write(bytes, 0, bytes.Length);
            }

            var response = GetResponse(request, "application/json", cancel);

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

        private static WebResponse GetResponse(WebRequest request, string expectedContentType, CancellationToken cancel)
        {
            using (cancel.Register(request.Abort))
            {
                try
                {
                    var response = request.GetResponseAsync().Result;

                    if (expectedContentType != null &&
                        expectedContentType != response.ContentType &&
                        new ContentType(expectedContentType).MediaType != new ContentType(response.ContentType).MediaType)
                    {
                        using var stream = response.GetResponseStream();
                        DumpLogFile(request, response, stream.CopyTo);

                        throw new ApplicationException($"Unexpected content type '{response.ContentType}' in response from '{request.RequestUri}'");
                    }

                    return response;
                }
                catch (Exception ex) when (ex.GetBaseException() is WebException {Status: WebExceptionStatus.RequestCanceled})
                {
                    throw new OperationCanceledException(ex.Message, ex, cancel);
                }
            }
        }

        private static void DumpLogFile(WebRequest request, WebResponse response, Action<Stream> writeContent)
        {
            using var file = File.OpenWrite(string.Join("_", DateTime.UtcNow.ToString("yyyyMMddHHmmss"), nameof(WebRequestService)) + ".log");
            using var writer = new StreamWriter(file);
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