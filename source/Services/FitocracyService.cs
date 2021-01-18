using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<FitocracyService> _logger;
        private readonly string _username;
        private readonly string _password;
        private string _csrfToken;
        private long _selfUserId;

        public FitocracyService(IWebRequestService webRequest, IScrapingService scraper, ILogger<FitocracyService> logger, FitocracyOptions options)
        {
            _webRequest = webRequest;
            _scraper = scraper;
            _logger = logger;
            _username = options.Username;
            _password = options.Password;
        }

        public async Task<long> GetSelfUserId(CancellationToken cancel)
        {
            await EnsureAuthenticated(cancel);
            return _selfUserId;
        }

        public async Task<IList<User>> GetFollowers(int pageNum, CancellationToken cancel)
        {
            _logger.LogDebug("Get followers page " + pageNum);
            await EnsureAuthenticated(cancel);
            await using var stream = await _webRequest.Get("get-user-friends", new {followers = true, user = _username, page = pageNum}, "application/json", cancel);
            return await JsonSerializer.DeserializeAsync<IList<User>>(stream, new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase}, cancel);
        }

        public async Task<IList<Workout>> GetWorkouts(long userId, int offset, CancellationToken cancel)
        {
            _logger.LogDebug("Get workouts for user {0} at offset {1}", userId, offset);
            await EnsureAuthenticated(cancel);

            await using var webStream = await _webRequest.Get("activity_stream/" + offset, new {user_id = userId, types = "WORKOUT"}, "text/html", cancel);
            Stream stream;
            if (webStream.CanSeek)
            {
                stream = webStream;
            }
            else
            {
                stream = new MemoryStream();
                await webStream.CopyToAsync(stream, cancel);
                stream.Seek(0, SeekOrigin.Begin);
            }
            try
            {
                return _scraper.ExtractWorkouts(stream, _selfUserId);
            }
            catch (InvalidDataException ex)
            {
                stream.Seek(0, SeekOrigin.Begin);
                await using var file = File.OpenWrite(string.Join("_", DateTime.UtcNow.ToString("yyyyMMddHHmmss"), nameof(FitocracyService), nameof(GetWorkouts), userId, offset) + ".log");
                await stream.CopyToAsync(file, cancel);

                throw new ApplicationException($"Workout extraction failed for user {userId} at offset {offset}: {ex.Message}", ex);
            }
        }

        public async Task<Workout> GetWorkout(long workoutId, CancellationToken cancel)
        {
            _logger.LogDebug("Get workout " + workoutId);
            await EnsureAuthenticated(cancel);

            Workout workout;

            await using var webStream = await _webRequest.Get("entry/" + workoutId, null, "text/html", cancel);
            Stream stream;
            if (webStream.CanSeek)
            {
                stream = webStream;
            }
            else
            {
                stream = new MemoryStream();
                await webStream.CopyToAsync(stream, cancel);
                stream.Seek(0, SeekOrigin.Begin);
            }
            try
            {
                workout = _scraper.ExtractWorkouts(stream, _selfUserId).FirstOrDefault();
            }
            catch (InvalidDataException ex)
            {
                await DumpLogFile(stream, workoutId, cancel);
                throw new ApplicationException($"Workout extraction failed for workout {workoutId}: {ex.Message}", ex);
            }
            if (workout == null)
            {
                await DumpLogFile(stream, workoutId, cancel);
                throw new ApplicationException($"Workout extraction failed for workout {workoutId}");
            }

            return workout;
        }

        private static async Task DumpLogFile(Stream stream, long workoutId, CancellationToken cancel)
        {
            stream.Seek(0, SeekOrigin.Begin);
            await using var file = File.OpenWrite(string.Join("_", DateTime.UtcNow.ToString("yyyyMMddHHmmss"), nameof(FitocracyService), nameof(GetWorkout), workoutId) + ".log");
            await stream.CopyToAsync(file, cancel);
        }

        public async Task AddComment(long workoutId, string text, CancellationToken cancel)
        {
            _logger.LogDebug("Add comment on workout " + workoutId);
            await EnsureAuthenticated(cancel);
            await _webRequest.Post("add_comment", new {csrfmiddlewaretoken = _csrfToken, ag = workoutId, comment_text = text}, null, cancel);
        }

        public async Task DeleteComment(long commentId, CancellationToken cancel)
        {
            _logger.LogDebug("Delete comment " + commentId);
            await EnsureAuthenticated(cancel);
            await _webRequest.Post("delete_comment", new {csrfmiddlewaretoken = _csrfToken, id = commentId}, null, cancel);
        }

        public async Task GiveProp(long workoutId, CancellationToken cancel)
        {
            _logger.LogDebug("Give prop on workout " + workoutId);
            await EnsureAuthenticated(cancel);
            await _webRequest.Post("give_prop", new {csrfmiddlewaretoken = _csrfToken, id = workoutId}, null, cancel);
        }

        private async Task EnsureAuthenticated(CancellationToken cancel)
        {
            if (_csrfToken != null)
            {
                return;
            }

            await using (await _webRequest.Get("accounts/login", null, null, cancel))
            {
            }
            var tokenCookie = _webRequest.Cookies.GetCookies(new Uri("https://www.fitocracy.com"))["csrftoken"];
            if (tokenCookie == null)
            {
                throw new ApplicationException("CSRF token not found");
            }
            _csrfToken = tokenCookie.Value;
            var headers = new NameValueCollection();
            await _webRequest.Post("accounts/login", new {csrfmiddlewaretoken = _csrfToken, username = _username, password = _password, json = 1, is_username = 1}, headers, cancel);

            if (!long.TryParse(headers["X-Fitocracy-User"], out _selfUserId))
            {
                throw new ApplicationException("Self user ID not found");
            }
        }
    }
}