using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IUserPullService
    {
        Task<IEnumerable<User>> Pull(CancellationToken cancel = default);
    }
}