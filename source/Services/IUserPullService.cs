using System.Collections.Generic;
using System.Threading;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IUserPullService
    {
        IEnumerable<User> Pull(CancellationToken cancel = default);
    }
}