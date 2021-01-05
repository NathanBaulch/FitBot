using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Model;
using ServiceStack.Text;

namespace FitBot.Services
{
    public record FitocracyOptions
    {
        public string Username { get; init; }
        public string Password { get; init; }
    }

    public class FitocracyService : IFitocracyService
    {
        private readonly IWebRequestService _webRequest;
        private readonly IScrapingService _scraper;
        private readonly string _username;
        private readonly string _password;
        private string _csrfToken;
        private long _selfUserId;

        public FitocracyService(IWebRequestService webRequest, IScrapingService scraper, FitocracyOptions options)
        {
            _webRequest = webRequest;
            _scraper = scraper;
            _username = options.Username;
            _password = options.Password;
        }

        public long SelfUserId
        {
            get
            {
                EnsureAuthenticated().Wait();
                return _selfUserId;
            }
        }

        public async Task<IList<User>> GetFollowers(int pageNum)
        {
            Trace.TraceInformation("Get followers page " + pageNum);
            await EnsureAuthenticated();
            using (var stream = await _webRequest.Get("get-user-friends", new {followers = true, user = _username, page = pageNum}, "application/json"))
            {
                return JsonSerializer.DeserializeFromStream<IList<User>>(stream);
            }
        }

        public async Task<IList<Workout>> GetWorkouts(long userId, int offset)
        {
            Trace.TraceInformation("Get workouts for user {0} at offset {1}", userId, offset);
            await EnsureAuthenticated();

            using (var webStream = await _webRequest.Get("activity_stream/" + offset, new {user_id = userId, types = "WORKOUT"}, "text/html"))
            {
                Stream stream;
                if (webStream.CanSeek)
                {
                    stream = webStream;
                }
                else
                {
                    stream = new MemoryStream();
                    webStream.CopyTo(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                }

                try
                {
                    return _scraper.ExtractWorkouts(stream, _selfUserId);
                }
                catch (InvalidDataException ex)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    using (var file = File.OpenWrite(string.Join("_", DateTime.UtcNow.ToString("yyyyMMddHHmmss"), nameof(FitocracyService), nameof(GetWorkouts), userId, offset) + ".log"))
                    {
                        stream.CopyTo(file);
                    }

                    throw new ApplicationException($"Workout extraction failed for user {userId} at offset {offset}: {ex.Message}", ex);
                }
            }
        }

        public async Task<Workout> GetWorkout(long workoutId)
        {
            Trace.TraceInformation("Get workout " + workoutId);
            await EnsureAuthenticated();

            Workout workout;

            using (var webStream = await _webRequest.Get("entry/" + workoutId, null, "text/html"))
            {
                Stream stream;
                if (webStream.CanSeek)
                {
                    stream = webStream;
                }
                else
                {
                    stream = new MemoryStream();
                    webStream.CopyTo(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                }

                try
                {
                    workout = _scraper.ExtractWorkouts(stream, _selfUserId).FirstOrDefault();
                }
                catch (InvalidDataException ex)
                {
                    DumpLogFile(stream, workoutId);
                    throw new ApplicationException($"Workout extraction failed for workout {workoutId}: {ex.Message}", ex);
                }

                if (workout == null)
                {
                    DumpLogFile(stream, workoutId);
                    throw new ApplicationException($"Workout extraction failed for workout {workoutId}");
                }
            }

            return workout;
        }

        private static void DumpLogFile(Stream stream, long workoutId)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using (var file = File.OpenWrite(string.Join("_", DateTime.UtcNow.ToString("yyyyMMddHHmmss"), nameof(FitocracyService), nameof(GetWorkout), workoutId) + ".log"))
            {
                stream.CopyTo(file);
            }
        }

        public async Task AddComment(long workoutId, string text)
        {
            Trace.TraceInformation("Add comment on workout " + workoutId);
            await EnsureAuthenticated();
            await _webRequest.Post("add_comment", new {csrfmiddlewaretoken = _csrfToken, ag = workoutId, comment_text = text});
        }

        public async Task DeleteComment(long commentId)
        {
            Trace.TraceInformation("Delete comment " + commentId);
            await EnsureAuthenticated();
            await _webRequest.Post("delete_comment", new {csrfmiddlewaretoken = _csrfToken, id = commentId});
        }

        public async Task GiveProp(long workoutId)
        {
            Trace.TraceInformation("Give prop on workout " + workoutId);
            await EnsureAuthenticated();
            await _webRequest.Post("give_prop", new {csrfmiddlewaretoken = _csrfToken, id = workoutId});
        }

        private async Task EnsureAuthenticated()
        {
            if (_csrfToken != null)
            {
                return;
            }

            using (await _webRequest.Get("accounts/login"))
            {
            }
            var tokenCookie = _webRequest.Cookies.GetCookies(new Uri("https://www.fitocracy.com"))["csrftoken"];
            if (tokenCookie == null)
            {
                throw new ApplicationException("CSRF token not found");
            }
            _csrfToken = tokenCookie.Value;
            var headers = new NameValueCollection();
            await _webRequest.Post("accounts/login", new {csrfmiddlewaretoken = _csrfToken, username = _username, password = _password, json = 1, is_username = 1}, headers);

            if (!long.TryParse(headers["X-Fitocracy-User"], out _selfUserId))
            {
                throw new ApplicationException("Self user ID not found");
            }
        }
    }
}