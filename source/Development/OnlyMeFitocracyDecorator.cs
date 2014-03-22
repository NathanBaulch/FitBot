using System.Collections.Generic;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Properties;
using FitBot.Services;

namespace FitBot.Development
{
    public class OnlyMeFitocracyDecorator : BaseFitocracyDecorator
    {
        public OnlyMeFitocracyDecorator(IFitocracyService decorated)
            : base(decorated)
        {
        }

        public override Task<IList<User>> GetFollowers(int pageNum = 0)
        {
            var users = new List<User>();
            if (pageNum == 0)
            {
                users.Add(new User {Id = SelfUserId, Username = Settings.Default.Username});
            }
            return Task.FromResult<IList<User>>(users);
        }
    }
}