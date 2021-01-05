using System;

namespace FitBot.Model
{
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public DateTime InsertDate { get; set; }
        public DateTime? UpdateDate { get; set; }

        public bool HasChanges(User user) => Username != user.Username;
    }
}