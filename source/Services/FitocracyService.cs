﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Properties;
using ServiceStack.Text;

namespace FitBot.Services
{
    public class FitocracyService : IFitocracyService
    {
        private readonly IWebRequestService _webRequest;
        private readonly IScrapingService _scraper;
        private readonly string _username;
        private readonly string _password;
        private string _csrfToken;
        private long _selfUserId;

        public FitocracyService(IWebRequestService webRequest, IScrapingService scraper)
        {
            _webRequest = webRequest;
            _scraper = scraper;
            _username = Settings.Default.FitocracyUsername;
            _password = Settings.Default.FitocracyPassword;
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
            using (var stream = await _webRequest.Get("activity_stream/" + offset, new {user_id = userId, types = "WORKOUT"}, "text/html"))
            {
                var workouts = _scraper.ExtractWorkouts(stream);
                foreach (var workout in workouts)
                {
                    workout.UserId = userId;
                }
                return workouts;
            }
        }

        public async Task<IDictionary<long, string>> GetWorkoutComments(long workoutId)
        {
            Trace.TraceInformation("Get comments for workout " + workoutId);
            await EnsureAuthenticated();
            using (var stream = await _webRequest.Get("entry/" + workoutId, null, "text/html"))
            {
                return _scraper.ExtractWorkoutComments(stream, SelfUserId);
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