using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class AddMeFitocracyDecorator : BaseFitocracyDecorator
    {
        private readonly string _username;

        public AddMeFitocracyDecorator(IFitocracyService decorated, string username)
            : base(decorated) => _username = username;

        public override async Task<IList<User>> GetFollowers(int pageNum, CancellationToken cancel)
        {
            var users = await base.GetFollowers(pageNum, cancel);
            if (pageNum == 0)
            {
                users.Insert(0, new User {Id = await GetSelfUserId(cancel), Username = _username});
            }
            return users;
        }
    }
}