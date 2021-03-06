﻿using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace FitBot.Services
{
    public interface IWebRequestService
    {
        CookieContainer Cookies { get; set; }
        Task<Stream> Get(string endpoint, object args = null, string expectedContentType = null);
        Task Post(string endpoint, object data = null, NameValueCollection headers = null);
    }
}