using System.Collections.Generic;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class OnlyMeFitocracyDecorator : BaseFitocracyDecorator
    {
        private readonly string _username;

        public OnlyMeFitocracyDecorator(IFitocracyService decorated, string username)
            : base(decorated) => _username = username;

        public override IList<User> GetFollowers(int pageNum = 0)
        {
            var users = new List<User>();
            if (pageNum == 0)
            {
                users.Add(new User {Id = SelfUserId, Username = _username});
            }
            return users;
        }
    }
}