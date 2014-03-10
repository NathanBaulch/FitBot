using System.Linq;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public class UserSyncService : IUserSyncService
    {
        private readonly IDatabaseService _database;
        private readonly IFitocracyService _fitocracy;

        public UserSyncService(IDatabaseService database, IFitocracyService fitocracy)
        {
            _database = database;
            _fitocracy = fitocracy;
        }

        public async Task Execute()
        {
            var staleUsers = (await _database.GetUsers()).ToDictionary(user => user.Id);
            var pageNum = 0;
            while (true)
            {
                var freshUsers = await _fitocracy.GetFollowers(pageNum);
                foreach (var freshUser in freshUsers)
                {
                    User staleUser;
                    if (!staleUsers.TryGetValue(freshUser.Id, out staleUser))
                    {
                        _database.Insert(freshUser);
                    }
                    else
                    {
                        if (staleUser.Username != freshUser.Username)
                        {
                            _database.Update(freshUser);
                        }
                        staleUsers.Remove(freshUser.Id);
                    }
                }
                if (freshUsers.Count < 5)
                {
                    break;
                }
                pageNum++;
            }
            foreach (var staleUser in staleUsers.Values)
            {
                _database.Delete(staleUser);
            }
        }
    }
}