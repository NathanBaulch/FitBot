using System.Collections.Generic;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class AdHocUsersFitocracyDecorator : BaseFitocracyDecorator
    {
        public AdHocUsersFitocracyDecorator(IFitocracyService decorated)
            : base(decorated)
        {
        }

        public IEnumerable<User> Users { get; set; }

        public override IList<User> GetFollowers(int pageNum = 0)
        {
            var users = new List<User>();
            if (pageNum == 0)
            {
                users.AddRange(Users);
            }
            return users;
        }
    }
}