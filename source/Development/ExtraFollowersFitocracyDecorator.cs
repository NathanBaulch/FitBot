using System.Collections.Generic;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class ExtraFollowersFitocracyDecorator : BaseFitocracyDecorator
    {
        public ExtraFollowersFitocracyDecorator(IFitocracyService decorated)
            : base(decorated)
        {
        }

        public override async Task<IList<User>> GetFollowers(int pageNum = 0)
        {
            var users = await base.GetFollowers(pageNum);
            if (pageNum == 0)
            {
                //20 EARLY USERS
                users.Insert(0, new User {Id = 1, Username = "FRED"});
                users.Insert(0, new User {Id = 2, Username = "xenowang"});
                users.Insert(0, new User {Id = 4, Username = "robinsparkles"});
                users.Insert(0, new User {Id = 5, Username = "July"});
                users.Insert(0, new User {Id = 6, Username = "kynes"});
                users.Insert(0, new User {Id = 12, Username = "Patrick"});
                users.Insert(0, new User {Id = 14, Username = "crazedazn203"});
                users.Insert(0, new User {Id = 15, Username = "atryon"});
                users.Insert(0, new User {Id = 16, Username = "TiderA"});
                users.Insert(0, new User {Id = 17, Username = "ScotterC"});
                users.Insert(0, new User {Id = 18, Username = "heavyliftah"});
                users.Insert(0, new User {Id = 19, Username = "huangbp"});
                users.Insert(0, new User {Id = 20, Username = "pablitoguth"});
                users.Insert(0, new User {Id = 21, Username = "alanwdang"});
                users.Insert(0, new User {Id = 22, Username = "Alesin"});
                users.Insert(0, new User {Id = 23, Username = "kyoung"});
                users.Insert(0, new User {Id = 28, Username = "dfrancis"});
                users.Insert(0, new User {Id = 31, Username = "mgadow"});
                users.Insert(0, new User {Id = 36, Username = "severedties"});
                users.Insert(0, new User {Id = 38, Username = "Damienmyers"});

                //20 TOP USERS
                users.Insert(0, new User {Id = 370742, Username = "K-BEAST"});
                users.Insert(0, new User {Id = 108557, Username = "EVeksler"});
                users.Insert(0, new User {Id = 11020, Username = "Silenced_Knight"});
                users.Insert(0, new User {Id = 202035, Username = "WiselyChosen"});
                users.Insert(0, new User {Id = 103397, Username = "TheGreatMD"});
                users.Insert(0, new User {Id = 444100, Username = "jkresh"});
                users.Insert(0, new User {Id = 39535, Username = "fellrnr"});
                users.Insert(0, new User {Id = 459746, Username = "Eric_Brown"});
                users.Insert(0, new User {Id = 420532, Username = "Clefspeare"});
                users.Insert(0, new User {Id = 102630, Username = "j0n"});
                users.Insert(0, new User {Id = 118691, Username = "ocja0201"});
                users.Insert(0, new User {Id = 614442, Username = "RyanLovesBacon"});
                users.Insert(0, new User {Id = 944953, Username = "skellar2006"});
                users.Insert(0, new User {Id = 97120, Username = "Motivated_Doc"});
                users.Insert(0, new User {Id = 127947, Username = "MikeLaBossiere"});
                users.Insert(0, new User {Id = 78493, Username = "kkitsune"});
                users.Insert(0, new User {Id = 145779, Username = "JohnRock"});
                users.Insert(0, new User {Id = 89520, Username = "Saracenn"});
                users.Insert(0, new User {Id = 291168, Username = "Varangian"});
                users.Insert(0, new User {Id = 39881, Username = "Segugio"});
            }
            return users;
        }
    }
}