using System;

namespace FitBot.Model
{
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public DateTime? DirtyDate { get; set; }
    }
}