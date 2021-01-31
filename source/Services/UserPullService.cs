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

            var staleUsers = _database.GetUsers().ToDictionary(user => user.Id);
            var newUsers = new List<User>();
            var pageNum = 0;
            var processedIds = new HashSet<long>();
            while (true)
            {
                var freshUsers = await _fitocracy.GetFollowers(pageNum, cancel);
                if (freshUsers.Count == 0)
                {
                    break;
                }
                pageNum++;

                foreach (var freshUser in freshUsers.Where(user => processedIds.Add(user.Id)))
                {
                    if (!staleUsers.TryGetValue(freshUser.Id, out var staleUser))
                    {
                        newUsers.Add(freshUser);
                    }
                    else
                    {
                        staleUsers.Remove(freshUser.Id);
                        if (staleUser.IsBlocked)
                        {
                            continue;
                        }
                        if (freshUser.HasChanges(staleUser))
                        {
                            _database.Update(freshUser);
                        }
                        freshUser.InsertDate = staleUser.InsertDate;
                    }
                    users.Add(freshUser);
                }

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