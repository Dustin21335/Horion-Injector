using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using Microsoft.VisualBasic;

namespace HorionInjector
{
    partial class MainWindow
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(IntPtr dwDesiredAccess, bool bInheritHandle, uint processId);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, char[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, ref IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        public static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll")]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, IntPtr dwFreeType);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private void Inject(string path)
        {
            if (!File.Exists(path) || File.ReadAllBytes(path).Length < 10) 
            {
                SetStatus("DLL not found or is broken.");
                return; 
            }

            SetInjectionStatus("Injecting");

            try
            {
                var fileInfo = new FileInfo(path);
                var accessControl = fileInfo.GetAccessControl();
                accessControl.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier("S-1-15-2-1"), FileSystemRights.FullControl, AccessControlType.Allow));
                fileInfo.SetAccessControl(accessControl);
            }
            catch 
            { 
                SetStatus("Failed to set permissions, try running the injector as admin"); 
                return;
            }

            MinecraftClient = Process.GetProcessesByName("Minecraft.Windows").FirstOrDefault(p => p.Responding) ?? LaunchMinecraft();
            if (MinecraftClient == null) 
            {
                SetStatus("Failed to launch Minecraft (Is it installed?)"); 
                return;
            }

            Task.Delay(10000).Wait();
            if (GetInjected())
            { 
                SetStatus("Horion already injected!");
                return; 
            }

            IntPtr handle = OpenProcess((IntPtr)2035711, false, (uint)MinecraftClient.Id);
            if (handle == IntPtr.Zero || !MinecraftClient.Responding) 
            {
                SetStatus("Failed to get process handle"); 
                return; 
            }

            IntPtr p1 = VirtualAllocEx(handle, IntPtr.Zero, (uint)(path.Length + 1), 12288U, 64U);
            WriteProcessMemory(handle, p1, path.ToCharArray(), path.Length, out IntPtr p2);
            IntPtr procAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            IntPtr p3 = CreateRemoteThread(handle, IntPtr.Zero, 0U, procAddr, p1, 0U, ref p2);

            if (p3 == IntPtr.Zero)
            { 
                SetStatus("Failed to create remote thread");
                return; 
            }

            SetInjectionStatus("Injected");

            OnMinecraftClose();

            if (WaitForSingleObject(p3, 5000) != 128 && WaitForSingleObject(p3, 5000) != 258) 
            { 
                VirtualFreeEx(handle, p1, 0, (IntPtr)32768); 
                CloseHandle(p3); 
            }
          
            CloseHandle(handle); IntPtr windowH = FindWindow(null, "Minecraft");
            if (windowH == IntPtr.Zero) SetStatus("Failed to get window handle");
            else
            { 
                SetForegroundWindow(windowH);
                SetStatus("");
            }
        }

        public Process LaunchMinecraft()
        {
            Process Mc = Process.GetProcessesByName("Minecraft.Windows").FirstOrDefault(p => p.Responding);
            if (Mc == null)
            {
                Interaction.Shell("explorer.exe shell:appsFolder\\Microsoft.MinecraftUWP_8wekyb3d8bbwe!App", Wait: false);
                SetStatus("Launching Minecraft!");
                while ((Mc = Process.GetProcessesByName("Minecraft.Windows").FirstOrDefault()) == null || !GetMinecraftLoaded(Mc)) Task.Delay(1000);
            }
            return Mc;
        }
    }
}