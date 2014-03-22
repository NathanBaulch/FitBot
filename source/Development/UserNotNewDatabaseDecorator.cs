using System.Collections.Generic;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class UserNotNewPullDecorator : IUserPullService
    {
        private readonly IUserPullService _decorated;

        public UserNotNewPullDecorator(IUserPullService decorated)
        {
            _decorated = decorated;
        }

        public async Task<IEnumerable<User>> Pull()
        {
            var users = await _decorated.Pull();
            foreach (var user in users)
            {
                user.IsNew = false;
            }
            return users;
        }
    }
}