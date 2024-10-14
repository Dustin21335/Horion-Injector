using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;
using Microsoft.Win32;
using Path = System.IO.Path;
using System.Linq;
using System.Windows.Shapes;
using System.Text;

namespace HorionInjector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Mutex _mutex = new Mutex(true, "HorionInjector");
        private static Process MinecraftClient = Process.GetProcessesByName("Minecraft.Windows").FirstOrDefault();
        private static string _InjectionStatus;

        public MainWindow()
        {
            if (!_mutex.WaitOne(0, false))
            {
                MessageBox.Show("Horion Injector is already open!");
                Process.GetProcessesByName("HorionInjector").Where(p => p.MainWindowHandle != IntPtr.Zero).ToList().ForEach(p => SetForegroundWindow(p.MainWindowHandle));
                Application.Current.Shutdown();
                return;
            }

            Initialize();
            this.Closing += OnWindowClose;
        }

        private void Initialize()
        {
            InitializeComponent();
            SetVersionLabel();
            SetInjectionStatus();
            _InjectionStatus = GetInjectionStatus();
            SetInjectionStatus();
            if (_InjectionStatus == "Injected") OnMinecraftClose();
            CheckForUpdate();
        }

        private void InjectButton_Left(object sender, RoutedEventArgs e)
        {
            if (_InjectionStatus == "Injected") return;
            if (!CheckConnection()) WaitForConnection(10);
            WebClient wc = new WebClient();
            wc.DownloadFileCompleted += (_, __) => Inject(Path.Combine(Path.GetTempPath(), "Horion.dll"));
            wc.DownloadFileAsync(new Uri("https://horion.download/bin/Horion.dll"), Path.Combine(Path.GetTempPath(), "Horion.dll"));
        }

        private void WaitForConnection(int retries)
        {
            while (!CheckConnection() && retries != 0)
            {
                MessageBox.Show($"Failed to connect to the download server. Retrying {retries}...");
                retries--;
                Thread.Sleep(1000); 
            }
        }

        private bool CheckConnection()
        {
            try
            {
                WebRequest request = WebRequest.Create("https://horion.download");
                request.Timeout = 1000;
                return request.GetResponse() is HttpWebResponse response && response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool GetMinecraftLoaded(Process process = null)
        {
            Process Minecraft = process ?? MinecraftClient;
            if (Minecraft == null) return false;
            string[] dlls = { "cohtml.UWP.dll", "RenoirCore.UWP.dll", "MSVCP140_APP.dll", "VCRUNTIME140_APP.dll", "VCRUNTIME140_1_APP.dll", "Windows.ApplicationModel.Store.dll", "Windows.ApplicationModel.dll", "Windows.System.Diagnostics.Telemetry.PlatformTelemetryClient.dll" };
            bool loaded = dlls.All(d => Minecraft.Modules.Cast<ProcessModule>().Any(m => m.ModuleName.Equals(d, StringComparison.OrdinalIgnoreCase)));
            return loaded;
        }

        private string GetInjectionStatus()
        {
            if (MinecraftClient == null) return "Ejected";
            return GetInjected() ? "Injected" : "Ejected";
        }

        private void OnMinecraftClose()
        {
            if (MinecraftClient == null || _InjectionStatus == "Ejected") return;
            MinecraftClient.EnableRaisingEvents = true;
            MinecraftClient.Exited += OnMinecraftProcessExited;
        }

        private void SetStatus(string text) => StatusLabel.Content = text;
        private void SetVersionLabel() => VersionLabel.Content = $"v{GetVersion().Major}.{GetVersion().Minor}.{GetVersion().Build}";
        private void SetInjectionStatus(string text = null) => InjectionStatus.Content = text ?? _InjectionStatus;
        private Version GetVersion() => Assembly.GetExecutingAssembly().GetName().Version;
        public bool GetInjected() => MinecraftClient.Modules.Cast<ProcessModule>().Any(m => m.FileName.Equals(Path.Combine(Path.GetTempPath(), "Horion.dll"), StringComparison.OrdinalIgnoreCase));
        private void OnMinecraftProcessExited(object sender, EventArgs e) => Application.Current.Dispatcher.Invoke(() => SetInjectionStatus());
        private void CloseWindow(object sender, MouseButtonEventArgs e) => Application.Current.Shutdown();
        private void DragWindow(object sender, MouseButtonEventArgs e) => DragMove();
        private void OnWindowClose(object sender, System.ComponentModel.CancelEventArgs e) => Application.Current.Shutdown();
    }
}
