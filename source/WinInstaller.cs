using System.ComponentModel;

namespace FitBot
{
    [RunInstaller(true)]
    public partial class WinInstaller : System.Configuration.Install.Installer
    {
        public WinInstaller()
        {
            InitializeComponent();
        }
    }
}