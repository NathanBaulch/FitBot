using System.Collections.Generic;
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

        public override IList<User> GetFollowers(int pageNum = 0)
        {
            var users = base.GetFollowers(pageNum);
            if (pageNum == 0)
            {
                users.Insert(0, new User {Id = SelfUserId, Username = Settings.Default.FitocracyUsername});
            }
            return users;
        }
    }
}