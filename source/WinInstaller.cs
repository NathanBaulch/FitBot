using System.ComponentModel;
using System.Configuration.Install;

namespace FitBot
{
    [RunInstaller(true)]
    public partial class WinInstaller : Installer
    {
        public WinInstaller()
        {
            InitializeComponent();
        }
    }
}