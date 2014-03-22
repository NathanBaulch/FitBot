namespace FitBot.Model
{
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public bool IsNew { get; set; }

        public bool HasChanges(User user)
        {
            return Username != user.Username;
        }
    }
}