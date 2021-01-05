using System.Collections.Generic;
using System.Threading.Tasks;
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

        public override Task<IList<User>> GetFollowers(int pageNum)
        {
            var users = new List<User>();
            if (pageNum == 0)
            {
                users.AddRange(Users);
            }
            return Task.FromResult<IList<User>>(users);
        }
    }
}