using System.CommandLine;
using FitBot.Services;

namespace FitBot.Tools
{
    public class Blocker
    {
        private readonly IDatabaseService _database;

        public Blocker(IDatabaseService database) => _database = database;

        public void Run(IConsole console, long userId)
        {
            var user = _database.GetUser(userId);
            if (user == null)
            {
                console.Error.Write("User not found");
                return;
            }

            if (user.IsBlocked)
            {
                console.Out.Write($"User '{user.Username}' already blocked");
                return;
            }

            user.IsBlocked = true;
            _database.Update(user);
            console.Out.Write($"User '{user.Username}' blocked");
        }
    }
}