using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management;
using System.ServiceProcess;

namespace WpfSqlServiceController
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Non serve inizializzazione servizi: nomi hardcoded
        }

        private void AppendOutput(string message)
        {
            OutputTextBox.AppendText($"{DateTime.Now:T}: {message}\n");
            OutputTextBox.ScrollToEnd();
        }

        private void StartService_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var serviceName = btn.Tag as string;
            ControlService(serviceName, true);
        }

        private void StopService_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var serviceName = btn.Tag as string;
            ControlService(serviceName, false);
        }

        private void ControlService(string serviceName, bool start)
        {
            try
            {
                using (var sc = new ServiceController(serviceName))
                {
                    var targetStatus = start ? ServiceControllerStatus.Running : ServiceControllerStatus.Stopped;
                    if (sc.Status != targetStatus)
                    {
                        if (start) sc.Start(); else sc.Stop();
                        sc.WaitForStatus(targetStatus, TimeSpan.FromSeconds(30));
                        AppendOutput($"{serviceName} {(start ? "started" : "stopped")}.");
                    }
                    else
                        AppendOutput($"{serviceName} is already {(start ? "running" : "stopped")}.");
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"Error {(start ? "start" : "stop")}: {ex.Message}");
            }
        }

        private void ServiceRow_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var panel = sender as StackPanel;
            if (panel.ContextMenu != null) return; // già creato

            // Prende il nome tecnico del servizio dal tag del pulsante "Avvia"
            var startButton = panel.Children[1] as Button;
            var serviceName = startButton?.Tag as string;
            if (string.IsNullOrEmpty(serviceName))
                return;

            // Costruisci ContextMenu al volo
            var menu = new ContextMenu();
            string[] modes = { "Automatic", "Manual", "Disabled" };
            foreach (var mode in modes)
            {
                var item = new MenuItem { Header = mode, Tag = serviceName + "|" + mode };
                item.Click += ChangeStartMode_Click;
                menu.Items.Add(item);
            }
            panel.ContextMenu = menu;
        }

        private void ChangeStartMode_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            var parts = (item.Tag as string).Split('|');
            var serviceName = parts[0];
            var mode = parts[1];
            try
            {
                string path = $"Win32_Service.Name='{serviceName}'";
                using (var svc = new ManagementObject(path))
                {
                    var inParams = svc.GetMethodParameters("ChangeStartMode");
                    inParams["StartMode"] = mode;
                    var outParams = svc.InvokeMethod("ChangeStartMode", inParams, null);
                    uint ret = (uint)outParams["ReturnValue"];
                    if (ret == 0)
                        AppendOutput($"StartupType of {serviceName} set on {mode}.");
                    else
                        AppendOutput($"Error ({ret}) changing StartupType of {serviceName}.");
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"Error ChangeStartMode: {ex.Message}");
            }
        }
    }
}
