using System.Collections.Generic;
using System.Linq;
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

        public async Task<IEnumerable<User>> Pull()
        {
            var users = new List<User>();

            var staleUsers = (await _database.GetUsers()).ToDictionary(user => user.Id);
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
                    User staleUser;
                    if (!staleUsers.TryGetValue(freshUser.Id, out staleUser))
                    {
                        _database.Insert(freshUser);
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
            }

            foreach (var staleUser in staleUsers.Values)
            {
                _database.Delete(staleUser);
            }

            return users;
        }
    }
}