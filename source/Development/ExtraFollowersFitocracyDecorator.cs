using System.Collections.Generic;
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

        public override IList<User> GetFollowers(int pageNum = 0)
        {
            var users = base.GetFollowers(pageNum);
            if (pageNum == 0)
            {
                //OTHER
                users.Insert(0, new User {Id = 1728298, Username = "jperona"});

                //RANDOM USERS
                users.Insert(0, new User {Id = 1499493, Username = "Lillith32"});
                users.Insert(0, new User {Id = 29512, Username = "JasonSuave"});
                users.Insert(0, new User {Id = 1287135, Username = "plin_a"});
                users.Insert(0, new User {Id = 1307284, Username = "BarrelOfSnakes"});
                users.Insert(0, new User {Id = 134275, Username = "magnet"});
                users.Insert(0, new User {Id = 1469979, Username = "vwalls99"});
                users.Insert(0, new User {Id = 1514806, Username = "alandw"});
                users.Insert(0, new User {Id = 154080, Username = "Shijin"});
                users.Insert(0, new User {Id = 1604393, Username = "lukasiniho"});
                users.Insert(0, new User {Id = 1629824, Username = "jbclay916"});
                users.Insert(0, new User {Id = 253665, Username = "jrcchicago"});
                users.Insert(0, new User {Id = 281261, Username = "Eyila"});
                users.Insert(0, new User {Id = 384412, Username = "KristinaBodin"});
                users.Insert(0, new User {Id = 385463, Username = "zmartin"});
                users.Insert(0, new User {Id = 419593, Username = "Max_Power"});
                users.Insert(0, new User {Id = 438823, Username = "AFitParent"});
                users.Insert(0, new User {Id = 548901, Username = "Lothil"});
                users.Insert(0, new User {Id = 792824, Username = "ChafedConfused"});
                users.Insert(0, new User {Id = 818265, Username = "melabelle"});
                users.Insert(0, new User {Id = 818810, Username = "danny_terrell29"});

                //EARLY USERS
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
                users.Insert(0, new User {Id = 39, Username = "Bagricula"});
                users.Insert(0, new User {Id = 40, Username = "edwkim"});
                users.Insert(0, new User {Id = 43, Username = "bliztacular"});
                users.Insert(0, new User {Id = 44, Username = "rockcreekperk"});
                users.Insert(0, new User {Id = 46, Username = "mobileglobal"});
                users.Insert(0, new User {Id = 48, Username = "missambitions"});
                users.Insert(0, new User {Id = 50, Username = "pescatello"});
                users.Insert(0, new User {Id = 51, Username = "dshih"});
                users.Insert(0, new User {Id = 52, Username = "smalter"});
                users.Insert(0, new User {Id = 56, Username = "Alli89"});
                users.Insert(0, new User {Id = 57, Username = "mschm"});
                users.Insert(0, new User {Id = 59, Username = "rhiesa"});
                users.Insert(0, new User {Id = 60, Username = "julianbell"});
                users.Insert(0, new User {Id = 63, Username = "zazamor"});
                users.Insert(0, new User {Id = 64, Username = "lilikoi"});
                users.Insert(0, new User {Id = 65, Username = "ivanpk"});
                users.Insert(0, new User {Id = 67, Username = "bbqcow"});
                users.Insert(0, new User {Id = 68, Username = "Matt"});
                users.Insert(0, new User {Id = 69, Username = "kevinh0143"});
                users.Insert(0, new User {Id = 70, Username = "stickshift"});

                //RECENT USERS
                users.Insert(0, new User {Id = 1052041, Username = "bette_"});
                users.Insert(0, new User {Id = 1092870, Username = "aztek1099"});
                users.Insert(0, new User {Id = 1129889, Username = "gusts94"});
                users.Insert(0, new User {Id = 1136408, Username = "Justin_44"});
                users.Insert(0, new User {Id = 1148675, Username = "BalintGabriel"});
                users.Insert(0, new User {Id = 1184136, Username = "alleday_cross"});
                users.Insert(0, new User {Id = 1212190, Username = "manahime193"});
                users.Insert(0, new User {Id = 1219800, Username = "Lanalia"});
                users.Insert(0, new User {Id = 125933, Username = "dottore"});
                users.Insert(0, new User {Id = 1347872, Username = "marster"});
                users.Insert(0, new User {Id = 1355466, Username = "cacheng88"});
                users.Insert(0, new User {Id = 136113, Username = "AprilCat"});
                users.Insert(0, new User {Id = 1363908, Username = "H_Saxon"});
                users.Insert(0, new User {Id = 137833, Username = "Eha"});
                users.Insert(0, new User {Id = 1430777, Username = "bbooth29"});
                users.Insert(0, new User {Id = 1482231, Username = "alisontgillaspy"});
                users.Insert(0, new User {Id = 1490528, Username = "dmatsuura"});
                users.Insert(0, new User {Id = 1510968, Username = "farshad"});
                users.Insert(0, new User {Id = 1551259, Username = "RAW-in-caps"});
                users.Insert(0, new User {Id = 15636, Username = "TheDart"});
                users.Insert(0, new User {Id = 1603955, Username = "saharat83"});
                users.Insert(0, new User {Id = 1609404, Username = "missdevonlynne"});
                users.Insert(0, new User {Id = 1612283, Username = "JohnDozer"});
                users.Insert(0, new User {Id = 1621554, Username = "anni_martikaine"});
                users.Insert(0, new User {Id = 1632861, Username = "chap2850"});
                users.Insert(0, new User {Id = 170355, Username = "LegaTron"});
                users.Insert(0, new User {Id = 176346, Username = "tapani"});
                users.Insert(0, new User {Id = 183486, Username = "Edgemo2001"});
                users.Insert(0, new User {Id = 202423, Username = "dvdlcs"});
                users.Insert(0, new User {Id = 2043, Username = "none"});
                users.Insert(0, new User {Id = 214700, Username = "Bantering_Ram"});
                users.Insert(0, new User {Id = 279913, Username = "powahmonkee"});
                users.Insert(0, new User {Id = 284099, Username = "Cheese_Sandwich"});
                users.Insert(0, new User {Id = 301366, Username = "hrr5010"});
                users.Insert(0, new User {Id = 309175, Username = "Wellfleet"});
                users.Insert(0, new User {Id = 41076, Username = "Brick_121"});
                users.Insert(0, new User {Id = 436689, Username = "Georgina"});
                users.Insert(0, new User {Id = 44036, Username = "erikjmc"});
                users.Insert(0, new User {Id = 458580, Username = "Sensei_Lars"});
                users.Insert(0, new User {Id = 492969, Username = "ParaMagician"});
                users.Insert(0, new User {Id = 511533, Username = "MightyMora"});
                users.Insert(0, new User {Id = 574536, Username = "likedevilsrain"});
                users.Insert(0, new User {Id = 593635, Username = "amukid92"});
                users.Insert(0, new User {Id = 596250, Username = "brianofarrell"});
                users.Insert(0, new User {Id = 596481, Username = "adamlazaro"});
                users.Insert(0, new User {Id = 610219, Username = "Carpathia_wave"});
                users.Insert(0, new User {Id = 629758, Username = "a_ron50"});
                users.Insert(0, new User {Id = 655193, Username = "sudofaulkner"});
                users.Insert(0, new User {Id = 714560, Username = "grekulf"});
                users.Insert(0, new User {Id = 762071, Username = "barbiefriend"});
                users.Insert(0, new User {Id = 78155, Username = "dpyro"});
                users.Insert(0, new User {Id = 809415, Username = "the_krister"});
                users.Insert(0, new User {Id = 820365, Username = "mathieu_f"});
                users.Insert(0, new User {Id = 832426, Username = "svenk"});
                users.Insert(0, new User {Id = 83350, Username = "edicius"});
                users.Insert(0, new User {Id = 852756, Username = "Mr_Dizzel"});
                users.Insert(0, new User {Id = 874017, Username = "kaschongs"});
                users.Insert(0, new User {Id = 881530, Username = "beanedge"});
                users.Insert(0, new User {Id = 89293, Username = "jmolife"});
                users.Insert(0, new User {Id = 95503, Username = "MichaelVo"});

                //ALL TIME LEADERS
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

                //90 DAY LEADERS
                users.Insert(0, new User {Id = 1317753, Username = "myrunningshoes"});
                users.Insert(0, new User {Id = 1313975, Username = "scullerzpe"});
                users.Insert(0, new User {Id = 1452700, Username = "regretting"});
                users.Insert(0, new User {Id = 1216860, Username = "olivia_lasche"});
                users.Insert(0, new User {Id = 1413891, Username = "JASON-MVP"});
                users.Insert(0, new User {Id = 291833, Username = "GDesfarges"});
                users.Insert(0, new User {Id = 526700, Username = "Xomby"});
                users.Insert(0, new User {Id = 1498364, Username = "-O-Boy-"});
                users.Insert(0, new User {Id = 600990, Username = "PeteT"});
                users.Insert(0, new User {Id = 1480500, Username = "drew_7"});
                users.Insert(0, new User {Id = 1094386, Username = "fit007"});
                users.Insert(0, new User {Id = 678604, Username = "MiguelFurtado"});
                users.Insert(0, new User {Id = 1487258, Username = "NicoleTrevisan"});
                users.Insert(0, new User {Id = 249804, Username = "Raurisz"});
                users.Insert(0, new User {Id = 1481812, Username = "rapidrich2"});
                users.Insert(0, new User {Id = 1398246, Username = "Xtrem-E"});
                users.Insert(0, new User {Id = 1311105, Username = "JackBrookes"});
                users.Insert(0, new User {Id = 1290340, Username = "JustinPBG"});
                users.Insert(0, new User {Id = 805162, Username = "LilPanda34"});
                users.Insert(0, new User {Id = 1285933, Username = "BDinthecrowd"});

                //30 DAY LEADERS
                users.Insert(0, new User {Id = 1590163, Username = "OkCountryBoy"});
                users.Insert(0, new User {Id = 1170581, Username = "mikeb12"});
                users.Insert(0, new User {Id = 553711, Username = "jigbim1"});
                users.Insert(0, new User {Id = 892834, Username = "mikeaguirre"});
                users.Insert(0, new User {Id = 1541513, Username = "benchythesequel"});
                users.Insert(0, new User {Id = 125884, Username = "ChinoZ32"});
                users.Insert(0, new User {Id = 574076, Username = "acronuts"});
                users.Insert(0, new User {Id = 665834, Username = "SonneJS"});
                users.Insert(0, new User {Id = 1275522, Username = "Ultimateprice16"});
                users.Insert(0, new User {Id = 1114402, Username = "ChristelKessler"});
                users.Insert(0, new User {Id = 1134161, Username = "euphoracy"});
                users.Insert(0, new User {Id = 116047, Username = "gloomchen"});
                users.Insert(0, new User {Id = 1326055, Username = "brennah"});
                users.Insert(0, new User {Id = 63328, Username = "breic"});
                users.Insert(0, new User {Id = 1410990, Username = "Sveneezy"});
                users.Insert(0, new User {Id = 939654, Username = "zvonimir_vanjak"});
                users.Insert(0, new User {Id = 1137716, Username = "djfunkep"});
                users.Insert(0, new User {Id = 605351, Username = "HappyRunnerGirl"});
                users.Insert(0, new User {Id = 265773, Username = "Jarkko"});
                users.Insert(0, new User {Id = 1515872, Username = "seeAnna"});
            }
            return users;
        }
    }
}