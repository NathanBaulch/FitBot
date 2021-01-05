using System.Collections.Generic;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class AddMeFitocracyDecorator : BaseFitocracyDecorator
    {
        private readonly string _username;

        public AddMeFitocracyDecorator(IFitocracyService decorated, string username)
            : base(decorated) => _username = username;

        public override IList<User> GetFollowers(int pageNum = 0)
        {
            var users = base.GetFollowers(pageNum);
            if (pageNum == 0)
            {
                users.Insert(0, new User {Id = SelfUserId, Username = _username});
            }
            return users;
        }
    }
}