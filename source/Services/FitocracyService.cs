using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FitBot.Model;
using FitBot.Properties;
using ServiceStack.Text;

//TODO: possible connectivity problems

namespace FitBot.Services
{
    public class FitocracyService : IFitocracyService
    {
        private const string RootUri = "https://www.fitocracy.com/";

        private readonly IScrapingService _scraper;
        private readonly string _username;
        private readonly string _password;
        private string _csrfToken;

        public FitocracyService(IScrapingService scraper)
        {
            _scraper = scraper;
            _username = Settings.Default.Username;
            _password = Settings.Default.Password;
        }

        protected CookieContainer Cookies { get; set; }
        protected User SelfUser { get; set; }

        public async Task<IList<User>> GetFollowers(int pageNum)
        {
            Debug.WriteLine("Getting followers page " + pageNum);
            await EnsureAuthenticated();
            using (var stream = await Get("get-user-friends", new {followers = true, user = _username, page = pageNum}, "application/json"))
            {
                return JsonSerializer.DeserializeFromStream<IList<User>>(stream);
            }
        }

        public async Task<IList<Workout>> GetWorkouts(long userId, int offset)
        {
            Debug.WriteLine("Getting workouts for user {0} at offset {1}", userId, offset);
            await EnsureAuthenticated();
            using (var stream = await Get(string.Format("activity_stream/{0}", offset), new {user_id = userId, types = "WORKOUT"}, "text/html"))
            {
                var workouts = _scraper.ExtractWorkouts(stream, SelfUser);
                foreach (var workout in workouts)
                {
                    workout.UserId = userId;
                }
                return workouts;
            }
        }

        public async Task PostComment(long workoutId, string text)
        {
            Debug.WriteLine("Posting comment on workout " + workoutId);
            await EnsureAuthenticated();
            await Post("add_comment", new {ag = workoutId, comment_text = text});
        }

        public async Task DeleteComment(long commentId)
        {
            Debug.WriteLine("Deleting comment " + commentId);
            await EnsureAuthenticated();
            await Post("delete_comment", new {id = commentId});
        }

        public async Task GiveProp(long workoutId)
        {
            Debug.WriteLine("Giving prop on workout " + workoutId);
            await EnsureAuthenticated();
            await Post("give_prop", new {id = workoutId});
        }

        private async Task EnsureAuthenticated()
        {
            if (Cookies != null)
            {
                return;
            }

            Cookies = new CookieContainer();
            await Get("accounts/login");
            var tokenCookie = Cookies.GetCookies(new Uri(RootUri))["csrftoken"];
            if (tokenCookie == null)
            {
                throw new Exception("TODO: CSRF token not found");
            }
            _csrfToken = tokenCookie.Value;
            await Post("accounts/login", new {username = _username, password = _password, json = 1, is_username = 1});
        }

        protected virtual async Task<Stream> Get(string endpoint, object queryArgs = null, string expectedContentType = null)
        {
            //TODO: find a more permanent throttling solution
            Thread.Sleep(3000);

            var uri = RootUri + endpoint + "/";
            if (queryArgs != null)
            {
                uri += "?" + string.Join("&", FormatQueryArgs(queryArgs));
            }
            var request = (HttpWebRequest) WebRequest.Create(uri);
            request.CookieContainer = Cookies;
            var response = await request.GetResponseAsync();
            if (expectedContentType != null && expectedContentType != response.ContentType)
            {
                throw new Exception("TODO: unexpected content type");
            }
            return response.GetResponseStream();
        }

        protected virtual async Task Post(string endpoint, object formData = null)
        {
            var uri = RootUri + endpoint + "/";
            var request = (HttpWebRequest) WebRequest.Create(uri);
            request.CookieContainer = Cookies;
            request.Method = "POST";
            request.Referer = uri;
            using (var stream = await request.GetRequestStreamAsync())
            {
                var data = Encoding.UTF8.GetBytes("csrfmiddlewaretoken=" + _csrfToken);
                await stream.WriteAsync(data, 0, data.Length);
                if (formData != null)
                {
                    foreach (var queryArg in FormatQueryArgs(formData))
                    {
                        data = Encoding.UTF8.GetBytes("&" + queryArg);
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                }
            }
            using (var response = await request.GetResponseAsync())
            {
                if (endpoint == "accounts/login")
                {
                    if (response.ContentType != "application/json")
                    {
                        throw new Exception("TODO: unexpected content type");
                    }

                    JsonObject json;
                    using (var stream = response.GetResponseStream())
                    {
                        json = JsonSerializer.DeserializeFromStream<JsonObject>(stream);
                    }

                    if (json != null && json["success"] != "true")
                    {
                        throw new Exception("TODO: " + json["error"]);
                    }

                    long id;
                    if (!long.TryParse(response.Headers["X-Fitocracy-User"], out id))
                    {
                        throw new Exception("TODO: Self user ID cannot be found");
                    }
                    SelfUser = new User
                        {
                            Id = id,
                            Username = _username
                        };
                }
            }
        }

        private static IEnumerable<string> FormatQueryArgs(object queryArgs)
        {
            return queryArgs.GetType()
                            .GetProperties()
                            .Select(prop => string.Format("{0}={1}", HttpUtility.UrlEncode(prop.Name), HttpUtility.UrlEncode(prop.GetValue(queryArgs).ToString())));
        }
    }
}