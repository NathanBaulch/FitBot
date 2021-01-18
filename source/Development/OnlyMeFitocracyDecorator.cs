using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class OnlyMeFitocracyDecorator : BaseFitocracyDecorator
    {
        private readonly string _username;

        public OnlyMeFitocracyDecorator(IFitocracyService decorated, string username)
            : base(decorated) => _username = username;

        public override async Task<IList<User>> GetFollowers(int pageNum, CancellationToken cancel)
        {
            var users = new List<User>();
            if (pageNum == 0)
            {
                users.Add(new User {Id = await GetSelfUserId(cancel), Username = _username});
            }
            return users;
        }
    }
}