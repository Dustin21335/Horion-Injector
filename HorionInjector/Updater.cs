using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace HorionInjector
{
    partial class MainWindow
    {
        private void CheckForUpdate()
        {
            MessageBox.Show(GetVersion().ToString());
            WaitForConnection(5);
            var latestVersion = Version.Parse(new WebClient().DownloadString(""));
            if (latestVersion > GetVersion() && MessageBox.Show("New update available! Do you want to update now?", null, MessageBoxButton.YesNo) == MessageBoxResult.Yes) Update();
        }

        private void Update()
        {
            string Injector = Assembly.GetExecutingAssembly().Location;
            string OldInjector = Path.ChangeExtension(Injector, ".old");
            File.Move(Injector, OldInjector);
            new WebClient().DownloadFile("", Injector);
            MessageBox.Show("Updater is done! The injector will now restart.");
            Process.Start(Injector);
            if (File.Exists(OldInjector)) File.Delete(OldInjector);
            Application.Current.Shutdown();
        }
    }
}