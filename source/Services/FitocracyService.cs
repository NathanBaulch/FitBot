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

        public async Task<IList<User>> GetFollowers(int pageNum)
        {
            Debug.WriteLine("Getting followers, page " + pageNum);
            await EnsureAuthenticated();
            using (var stream = await Get("get-user-friends", new {followers = true, user = _username, page = pageNum}))
            {
                return JsonSerializer.DeserializeFromStream<IList<User>>(stream);
            }
        }

        public async Task<IList<Workout>> GetWorkouts(long userId, int offset)
        {
            Debug.WriteLine("Getting workouts for user: {0}, offset {1}", userId, offset);
            await EnsureAuthenticated();
            using (var stream = await Get(string.Format("activity_stream/{0}", offset), new {user_id = userId, types = "WORKOUT"}))
            {
                var workouts = _scraper.ExtractWorkouts(stream);
                var importDate = DateTime.UtcNow;
                foreach (var workout in workouts)
                {
                    workout.ImportDate = importDate;
                    workout.UserId = userId;
                }
                return workouts;
            }
        }

        public async Task PostComment(long workoutId, string text)
        {
            Debug.WriteLine("Posting comment on workout: " + workoutId);
            await EnsureAuthenticated();
            await Post("add_comment", new {ag = workoutId, comment_text = text});
        }

        public async Task DeleteComment(long commentId)
        {
            Debug.WriteLine("Deleting comment: " + commentId);
            await EnsureAuthenticated();
            await Post("delete_comment", new {id = commentId});
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
            Debug.Assert(tokenCookie != null);
            _csrfToken = tokenCookie.Value;
            await Post("accounts/login", new {username = _username, password = _password});
        }

        protected virtual async Task<Stream> Get(string endpoint, object queryArgs = null)
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
            return (await request.GetResponseAsync()).GetResponseStream();
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
            using (await request.GetResponseAsync())
            {
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