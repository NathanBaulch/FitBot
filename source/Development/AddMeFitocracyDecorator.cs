using System.Collections.Generic;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Properties;
using FitBot.Services;

namespace FitBot.Development
{
    public class AddMeFitocracyDecorator : BaseFitocracyDecorator
    {
        public AddMeFitocracyDecorator(IFitocracyService decorated)
            : base(decorated)
        {
        }

        public override async Task<IList<User>> GetFollowers(int pageNum = 0)
        {
            var users = await base.GetFollowers(pageNum);
            if (pageNum == 0)
            {
                users.Insert(0, new User {Id = SelfUserId, Username = Settings.Default.Username});
            }
            return users;
        }
    }
}