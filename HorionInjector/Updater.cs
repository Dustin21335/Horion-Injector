using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace HorionInjector
{
    partial class MainWindow
    {
        private void CheckForUpdate()
        {
            WaitForConnection(5);
            var latestVersion = Version.Parse(new WebClient().DownloadString("https://github.com/Dustin21335/Horion-Injector/releases/download/Release/version"));
            if (latestVersion > GetVersion() && MessageBox.Show("New update available! Do you want to update now?", "Update", MessageBoxButton.YesNo) == MessageBoxResult.Yes) Update();
        }

        private void Update()
        {
            string Injector = Assembly.GetExecutingAssembly().Location;
            string OldInjector = Path.ChangeExtension(Injector, ".old");
            File.Move(Injector, OldInjector);
            new WebClient().DownloadFile("https://github.com/Dustin21335/Horion-Injector/releases/download/Release/HorionInjector.exe", Injector);
            string CleanUpBat = Path.Combine(Path.GetTempPath(), "CleanUp.bat");
            File.WriteAllText(CleanUpBat, $"@echo off\nif exist \"{OldInjector}\" del \"{OldInjector}\"\n");
            Process.Start(Injector);
            Process.Start(new ProcessStartInfo { FileName = CleanUpBat, WindowStyle = ProcessWindowStyle.Hidden });
            Application.Current.Shutdown();
        }
    }
}
