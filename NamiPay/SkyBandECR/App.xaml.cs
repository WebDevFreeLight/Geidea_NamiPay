using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.IO;

namespace SkybandECR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Determine the system architecture
            bool is64Bit = Environment.Is64BitOperatingSystem;

            // Set installer paths based on system architecture
            string vcInstallerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dependencies",
                                                  is64Bit ? "vc_redist.x64.exe" : "vc_redist.x86.exe");

            // Check if the appropriate VC++ Redistributable is installed
            if (!IsVcRedistributableInstalled(is64Bit))
            {
                InstallVcRedistributable(vcInstallerPath);
            }
        }

        private bool IsVcRedistributableInstalled(bool is64Bit)
        {
            string registryKey = is64Bit
                ? @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64"
                : @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86";

            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryKey))
            {
                return key?.GetValue("Installed")?.ToString() == "1";
            }
        }

        private void InstallVcRedistributable(string installerPath)
        {
            if (File.Exists(installerPath))
            {
                // Run the installer silently
                Process installer = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = installerPath,
                        Arguments = "/quiet /norestart",
                        UseShellExecute = true,
                        Verb = "runas" // Requires admin privileges
                    }
                };

                installer.Start();
                installer.WaitForExit();

                if (installer.ExitCode != 0)
                {
                    MessageBox.Show($"VC++ Redistributable installation failed with exit code {installer.ExitCode}.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show($"VC++ Redistributable installer not found at {installerPath}.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}
