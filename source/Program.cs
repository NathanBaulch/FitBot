using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;

namespace FitBot
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args != null && args.Length == 1)
            {
                var arg = args[0];
                if (arg.StartsWith("-", StringComparison.Ordinal) || arg.StartsWith("/", StringComparison.Ordinal))
                {
                    var assemblyPath = Assembly.GetExecutingAssembly().Location;
                    switch (arg.Substring(1).ToLower())
                    {
                        case "i":
                        case "install":
                            try
                            {
                                ManagedInstallerClass.InstallHelper(new[] {assemblyPath});
                            }
                            catch
                            {
                                Environment.Exit(-1);
                            }
                            break;
                        case "u":
                        case "uninstall":
                            try
                            {
                                ManagedInstallerClass.InstallHelper(new[] {"/u", assemblyPath});
                            }
                            catch
                            {
                                Environment.Exit(-1);
                            }
                            break;
                    }
                }
            }

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    if (Environment.UserInteractive)
                    {
                        Console.Beep();
                    }

                    Trace.TraceError(e.ExceptionObject.ToString());
                };

            using (var service = new WinService())
            {
                if (Environment.UserInteractive)
                {
                    service.Start();
                    Console.WriteLine(@"Press Q to stop the service...");
                    while (Console.ReadKey(true).Key != ConsoleKey.Q)
                    {
                    }
                    service.Stop();
                }
                else
                {
                    ServiceBase.Run(service);
                }
            }
        }
    }
}