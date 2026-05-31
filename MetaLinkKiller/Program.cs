using System.Diagnostics;
namespace MetaLinkKiller
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {

            Application.EnableVisualStyles();
            NotifyIcon trayIcon = new NotifyIcon();

            var stream = typeof(Program).Assembly.GetManifestResourceStream("MetaLinkKiller.softwareicon.ico");
            trayIcon.Icon = new Icon(stream);

            trayIcon.Visible = true;
            trayIcon.Text = "MetaLinkKiller";
            trayIcon.ShowBalloonTip(3000, "MetaLinkKiller", "Initialized! Check the system tray.", ToolTipIcon.Info);
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Kill OVR Process", null, (s, e) => KillQuestProcesses(trayIcon));
            menu.Items.Add("About", null, (s, e) => MessageBox.Show("MetaLinkKiller\n\nKills Meta Horizon Link-related processes after app shutdown to keep your data away from the pesky corporations.\n\nIf you find any bugs, report in Issues on GitHub\n\nCreated by Digit4LSpace, 2026", "About MetaLinkKiller", MessageBoxButtons.OK, MessageBoxIcon.Information));
            menu.Items.Add("Exit", null, (s, e) => Application.Exit());
            trayIcon.ContextMenuStrip = menu;
            var autoStartItem = new ToolStripMenuItem("Run at startup");
            autoStartItem.Checked = IsAutoStartEnabled();
            autoStartItem.Click += (s, e) => {
                SetAutoStart(!autoStartItem.Checked);
                autoStartItem.Checked = !autoStartItem.Checked;
            };
            menu.Items.Add(autoStartItem);

            var killOnStartItem = new ToolStripMenuItem("Kill on startup");
            killOnStartItem.Checked = GetKillOnStartup();
            killOnStartItem.Click += (s, e) => {
                killOnStartItem.Checked = !killOnStartItem.Checked;
                SetKillOnStartup(killOnStartItem.Checked);
            };
            menu.Items.Add(killOnStartItem);

            if (GetKillOnStartup())
                KillQuestProcesses(trayIcon);

            Application.Run();
        }
        static void KillQuestProcesses(NotifyIcon trayIcon)
        {
            string[] targets = { "OVRRedir", "OVRServiceLauncher", "OVRServer_x64" };
            int killed = 0;
            foreach (var name in targets)
                foreach (var proc in Process.GetProcessesByName(name))
                {
                    try { proc.Kill(); killed++; } catch { }
                }
            if (killed > 0)
                trayIcon.ShowBalloonTip(3000, "MetaLinkKiller", "All processes killed!", ToolTipIcon.Info);
            else
                trayIcon.ShowBalloonTip(3000, "MetaLinkKiller", "No OVR processes found.", ToolTipIcon.Warning);
        }
        static bool IsAutoStartEnabled()
        {
            var psi = new ProcessStartInfo("schtasks", "/query /tn MetaLinkKiller")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            try
            {
                var p = Process.Start(psi);
                p.WaitForExit();
                return p.ExitCode == 0;
            }
            catch { return false; }
        }

        static void SetAutoStart(bool enable)
        {
            if (enable)
            {
                var script = $@"$action = New-ScheduledTaskAction -Execute '{Application.ExecutablePath}'
                $trigger = New-ScheduledTaskTrigger -AtLogOn
                $principal = New-ScheduledTaskPrincipal -UserId '{Environment.UserName}' -RunLevel Highest
                Register-ScheduledTask -TaskName 'MetaLinkKiller' -Action $action -Trigger $trigger -Principal $principal -Force";

                var psi = new ProcessStartInfo("powershell", $"-Command \"{script.Replace("\"", "\\\"").Replace("\n", "; ")}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi)?.WaitForExit();
            }
            else
            {
                var psi = new ProcessStartInfo("powershell", "-Command \"Unregister-ScheduledTask -TaskName 'MetaLinkKiller' -Confirm:$false\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi)?.WaitForExit();
            }
        }
        static bool GetKillOnStartup()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\MetaLinkKiller");
            return key?.GetValue("KillOnStartup")?.ToString() == "1";
        }

        static void SetKillOnStartup(bool enable)
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\MetaLinkKiller");
            key.SetValue("KillOnStartup", enable ? "1" : "0");
        }
    }
}