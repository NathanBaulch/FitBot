using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public class UserPullService : IUserPullService
    {
        private readonly IDatabaseService _database;
        private readonly IFitocracyService _fitocracy;

        public UserPullService(IDatabaseService database, IFitocracyService fitocracy)
        {
            _database = database;
            _fitocracy = fitocracy;
        }

        public async Task<IEnumerable<User>> Pull(CancellationToken cancel)
        {
            var users = new List<User>();

            var staleUsers = (await _database.GetUsers()).ToDictionary(user => user.Id);
            var newUsers = new List<User>();
            var pageNum = 0;
            var processedIds = new HashSet<long>();
            while (true)
            {
                var freshUsers = await _fitocracy.GetFollowers(pageNum);
                if (freshUsers.Count == 0)
                {
                    break;
                }
                pageNum++;

                freshUsers = freshUsers.Where(user => processedIds.Add(user.Id)).ToList();
                foreach (var freshUser in freshUsers)
                {
                    if (!staleUsers.TryGetValue(freshUser.Id, out var staleUser))
                    {
                        newUsers.Add(freshUser);
                    }
                    else
                    {
                        if (freshUser.HasChanges(staleUser))
                        {
                            _database.Update(freshUser);
                        }
                        staleUsers.Remove(freshUser.Id);
                        freshUser.InsertDate = staleUser.InsertDate;
                    }
                }

                users.AddRange(freshUsers);
                cancel.ThrowIfCancellationRequested();
            }

            foreach (var staleUser in staleUsers.Values)
            {
                _database.Delete(staleUser);
            }

            foreach (var newUser in newUsers)
            {
                _database.Insert(newUser);
            }

            return users.OrderBy(_ => Guid.NewGuid());
        }
    }
}