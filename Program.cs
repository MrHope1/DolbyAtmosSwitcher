using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;
using Microsoft.Win32;

public static class Logger
{
    internal static string LogFilePath
    {
        get
        {
#if TESTING
            string testLogDirectory = Environment.GetEnvironmentVariable(
                "DOLBY_SWITCHER_TEST_LOG_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(testLogDirectory))
            {
                return Path.Combine(testLogDirectory, "debug_log.txt");
            }
#endif
            string logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DolbyAtmosSwitcher");
            return Path.Combine(logDirectory, "debug_log.txt");
        }
    }

    public static void Log(string component, string message)
    {
        try
        {
            string path = LogFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - [" + component + "] " + message + "\r\n");
        }
        catch { }
    }
}

internal static class WindowsSystemPaths
{
    internal static string ExplorerExecutablePath
    {
        get
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "explorer.exe");
        }
    }
}

internal static class CliArguments
{
    private static readonly string[] Profiles = new string[] { "Game", "Movie", "Music", "Voice" };

    internal static bool TryNormalizeProfile(string value, out string profileName)
    {
        profileName = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (string profile in Profiles)
        {
            if (profile.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                profileName = profile;
                return true;
            }
        }

        return false;
    }

    internal static bool TryGetProfile(string[] args, out string profileName)
    {
        profileName = string.Empty;
        if (args == null || args.Length < 2)
        {
            return false;
        }

        bool isChangeCommand =
            args[0].Equals("/change", StringComparison.OrdinalIgnoreCase) ||
            args[0].Equals("-c", StringComparison.OrdinalIgnoreCase);
        return isChangeCommand && TryNormalizeProfile(args[1], out profileName);
    }
}

internal static class ProfileState
{
    internal static int ResolveIndex(
        string[] profiles,
        string profileName,
        int currentIndex,
        bool switchSucceeded)
    {
        if (!switchSucceeded)
        {
            return currentIndex;
        }

        for (int i = 0; i < profiles.Length; i++)
        {
            if (profiles[i].Equals(profileName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return currentIndex;
    }
}

public static class DolbyAccessAutomator
{
    private const string AppUserModelId = "DolbyLaboratories.DolbyAccess_rz1tebttyb220!App";

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;

    private const uint WM_CLOSE = 0x0010;

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    public static bool SwitchProfile(string profileName, out string errorMessage)
    {
        errorMessage = string.Empty;
        string normalizedProfile;
        if (!CliArguments.TryNormalizeProfile(profileName, out normalizedProfile))
        {
            errorMessage = "Invalid profile. Choose Game, Movie, Music, or Voice.";
            Logger.Log("Automator", "Rejected an invalid profile request.");
            return false;
        }
        profileName = normalizedProfile;
        Logger.Log("Automator", "SwitchProfile requested: " + profileName);

        // Start background thread to keep the window pinned off-screen
        bool keepOffscreen = true;
        Thread offscreenThread = new Thread(delegate()
        {
            Logger.Log("Automator", "Offscreen pinning thread started.");
            while (keepOffscreen)
            {
                IntPtr hwnd = FindDolbyWindow();
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowPos(hwnd, IntPtr.Zero, -10000, -10000, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
                }
                Thread.Sleep(50); // Pin position every 50ms
            }
            Logger.Log("Automator", "Offscreen pinning thread stopped.");
        });
        offscreenThread.IsBackground = true;
        offscreenThread.Start();

        try
        {
            // 1. Find window
            IntPtr hWnd = FindDolbyWindow();
            bool justLaunched = false;
            if (hWnd == IntPtr.Zero)
            {
                Logger.Log("Automator", "Dolby Access window not found. Launching UWP app...");
                justLaunched = true;
                try
                {
                    Process.Start(WindowsSystemPaths.ExplorerExecutablePath, "shell:AppsFolder\\" + AppUserModelId);
                }
                catch (Exception ex)
                {
                    errorMessage = "Failed to start Dolby Access: " + ex.Message;
                    Logger.Log("Automator", "Error launching app: " + errorMessage);
                    return false;
                }

                // Wait for window (up to 8 seconds)
                for (int i = 0; i < 32; i++)
                {
                    Thread.Sleep(250);
                    hWnd = FindDolbyWindow();
                    if (hWnd != IntPtr.Zero)
                    {
                        Logger.Log("Automator", "Dolby Access window handle found after launch.");
                        break;
                    }
                }
            }
            else
            {
                Logger.Log("Automator", "Dolby Access window already exists.");
            }

            if (hWnd == IntPtr.Zero)
            {
                errorMessage = "Failed to launch or find Dolby Access window.";
                Logger.Log("Automator", errorMessage);
                return false;
            }

            // Set foreground to guarantee UWP UI tree loads
            Logger.Log("Automator", "Setting foreground window...");
            SetForegroundWindow(hWnd);

            // If just launched, wait longer for initial rendering
            if (justLaunched)
            {
                Thread.Sleep(1500);
            }
            else
            {
                Thread.Sleep(400);
            }

            Logger.Log("Automator", "Creating AutomationElement from handle " + hWnd + "...");
            AutomationElement root = AutomationElement.FromHandle(hWnd);
            if (root == null)
            {
                errorMessage = "Failed to obtain automation element from window handle.";
                Logger.Log("Automator", errorMessage);
                return false;
            }

            AutomationElement profileBtn = null;

            // Poll for the elements to load (up to 8 seconds)
            Logger.Log("Automator", "Polling for Settings tab or profile button...");
            for (int i = 0; i < 32; i++)
            {
                // First try to find Settings tab and invoke it if present
                AutomationElement settingsTab = root.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.NameProperty, "Settings"));

                if (settingsTab != null)
                {
                    Logger.Log("Automator", "Settings tab found. Invoking...");
                    InvokeElement(settingsTab);
                    Thread.Sleep(500); // Wait for transition
                }

                // Try to find the profile button
                profileBtn = root.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.NameProperty, profileName));

                if (profileBtn != null)
                {
                    Logger.Log("Automator", "Profile button '" + profileName + "' found.");
                    break;
                }

                Logger.Log("Automator", "Profile button not found yet. Sleeping 250ms...");
                Thread.Sleep(250);
            }

            if (profileBtn == null)
            {
                errorMessage = "Profile button '" + profileName + "' not found in Dolby Access UI after polling.";
                Logger.Log("Automator", errorMessage);
                return false;
            }

            Logger.Log("Automator", "Invoking profile button...");
            if (!InvokeElement(profileBtn))
            {
                errorMessage = "Profile button '" + profileName + "' could not be invoked.";
                Logger.Log("Automator", errorMessage);
                return false;
            }
            Thread.Sleep(400); // Wait for click to register

            // Close the window programmatically to clean up
            Logger.Log("Automator", "Closing Dolby Access window...");
            PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            Logger.Log("Automator", "SwitchProfile complete. Success!");
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            Logger.Log("Automator", "Exception during automation: " + ex.GetType().Name);
            return false;
        }
        finally
        {
            keepOffscreen = false; // Stop the offscreen pinning thread
        }
    }

    private static IntPtr FindDolbyWindow()
    {
        IntPtr foundHWnd = IntPtr.Zero;
        EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
        {
            System.Text.StringBuilder classBuf = new System.Text.StringBuilder(256);
            GetClassName(hWnd, classBuf, 256);
            if (classBuf.ToString().Equals("ApplicationFrameWindow", StringComparison.OrdinalIgnoreCase))
            {
                System.Text.StringBuilder titleBuf = new System.Text.StringBuilder(256);
                GetWindowText(hWnd, titleBuf, 256);
                string title = titleBuf.ToString();
                if (title.IndexOf("Dolby Access", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    foundHWnd = hWnd;
                    return false; // Stop enumeration
                }
            }
            return true;
        }, IntPtr.Zero);

        return foundHWnd;
    }

    internal static bool InvokeElement(AutomationElement element)
    {
        if (element == null)
        {
            return false;
        }

        AutomationElement current = element;
        while (current != null)
        {
            object pattern;
            if (current.TryGetCurrentPattern(InvokePattern.Pattern, out pattern))
            {
                Logger.Log("Automator", "Invoking element via InvokePattern on '" + current.Current.Name + "' (" + current.Current.ControlType.ProgrammaticName + ")");
                ((InvokePattern)pattern).Invoke();
                return true;
            }
            if (current.TryGetCurrentPattern(SelectionItemPattern.Pattern, out pattern))
            {
                Logger.Log("Automator", "Selecting element via SelectionItemPattern on '" + current.Current.Name + "' (" + current.Current.ControlType.ProgrammaticName + ")");
                ((SelectionItemPattern)pattern).Select();
                return true;
            }

            try
            {
                current = TreeWalker.ControlViewWalker.GetParent(current);
            }
            catch (Exception ex)
            {
                Logger.Log("Automator", "Failed to get parent element: " + ex.Message);
                current = null;
            }
        }

        Logger.Log("Automator", "No supported UI Automation pattern was found on the element or its parents.");
        return false;
    }
}

public class DarkFluentColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground { get { return Color.FromArgb(28, 28, 28); } }
    public override Color MenuBorder { get { return Color.FromArgb(48, 48, 48); } }
    public override Color MenuItemSelected { get { return Color.FromArgb(48, 48, 48); } }
    public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(48, 48, 48); } }
    public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(48, 48, 48); } }
    public override Color MenuItemBorder { get { return Color.Transparent; } }
    public override Color CheckBackground { get { return Color.FromArgb(28, 28, 28); } }
    public override Color CheckSelectedBackground { get { return Color.FromArgb(0, 150, 255); } }
    public override Color CheckPressedBackground { get { return Color.FromArgb(0, 130, 220); } }
    public override Color ImageMarginGradientBegin { get { return Color.FromArgb(28, 28, 28); } }
    public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(28, 28, 28); } }
    public override Color ImageMarginGradientEnd { get { return Color.FromArgb(28, 28, 28); } }
    public override Color SeparatorDark { get { return Color.FromArgb(48, 48, 48); } }
    public override Color SeparatorLight { get { return Color.Transparent; } }
}

public class DarkFluentRenderer : ToolStripProfessionalRenderer
{
    public DarkFluentRenderer() : base(new DarkFluentColorTable()) { }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Color.FromArgb(240, 240, 240) : Color.FromArgb(120, 120, 120);
        e.Item.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = Color.FromArgb(240, 240, 240);
        base.OnRenderArrow(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Draw a modern Windows 11 style rounded accent checkmark
        var rect = e.ImageRectangle;
        rect.Inflate(-1, -1);

        using (var brush = new SolidBrush(Color.FromArgb(0, 150, 255)))
        {
            g.FillEllipse(brush, rect);
        }

        using (var pen = new Pen(Color.White, 1.8f))
        {
            g.DrawLine(pen, rect.Left + 4, rect.Top + 7, rect.Left + 7, rect.Top + 10);
            g.DrawLine(pen, rect.Left + 7, rect.Top + 10, rect.Left + 12, rect.Top + 4);
        }
    }
}

public class DolbyAtmosAppContext : ApplicationContext
{
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;
    private string[] profiles = new string[] { "Game", "Movie", "Music", "Voice" };
    private int currentProfileIndex = 0;
    private Icon currentIcon;

    private const string RegistryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppValueName = "DolbyAtmosSwitcher";

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    public DolbyAtmosAppContext()
    {
        Logger.Log("GUI", "Initializing DolbyAtmosAppContext...");

        trayMenu = new ContextMenuStrip();
        trayMenu.Renderer = new DarkFluentRenderer(); // Set Dark Fluent Renderer

        BuildMenu();

        currentIcon = CreateDynamicIcon();
        trayIcon = new NotifyIcon
        {
            Icon = currentIcon,
            ContextMenuStrip = trayMenu,
            Visible = true,
            Text = "Dolby Atmos: Game"
        };

        trayIcon.MouseUp += TrayIcon_MouseUp;
        Logger.Log("GUI", "DolbyAtmosAppContext initialized successfully.");
    }

    private Icon CreateDynamicIcon()
    {
        using (Bitmap bitmap = new Bitmap(16, 16))
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);

                // Draw speaker outline in DeepSkyBlue
                using (Brush brush = new SolidBrush(Color.DeepSkyBlue))
                {
                    Point[] speakerPoints = new Point[]
                    {
                        new Point(2, 5),
                        new Point(5, 5),
                        new Point(9, 2),
                        new Point(9, 13),
                        new Point(5, 10),
                        new Point(2, 10)
                    };
                    g.FillPolygon(brush, speakerPoints);
                }

                // Draw sound wave lines
                using (Pen pen = new Pen(Color.DeepSkyBlue, 1.5f))
                {
                    g.DrawArc(pen, 8, 4, 6, 8, -45, 90);
                    g.DrawArc(pen, 10, 2, 8, 12, -45, 90);
                }
            }
            IntPtr hIcon = bitmap.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);
            return icon;
        }
    }

    private void BuildMenu()
    {
        trayMenu.Items.Clear();

        for (int i = 0; i < profiles.Length; i++)
        {
            string profileName = profiles[i];
            ToolStripMenuItem item = new ToolStripMenuItem(profileName);
            item.Checked = (i == currentProfileIndex);

            // Re-capture local variable for closure
            string pName = profileName;
            item.Click += (s, e) => ApplyProfile(pName);

            trayMenu.Items.Add(item);
        }

        trayMenu.Items.Add(new ToolStripSeparator());

        ToolStripMenuItem startupItem = new ToolStripMenuItem("Run at Startup");
        startupItem.Checked = IsStartupEnabled();
        startupItem.Click += ToggleStartup;
        trayMenu.Items.Add(startupItem);

        trayMenu.Items.Add(new ToolStripSeparator());

        ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", null, (s, e) => Exit());
        trayMenu.Items.Add(exitItem);
    }

    private void TrayIcon_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Logger.Log("GUI", "Left-click detected. Cycling profile...");
            // Cycle to the next profile
            int nextProfileIndex = (currentProfileIndex + 1) % profiles.Length;
            ApplyProfile(profiles[nextProfileIndex]);
        }
    }

    private void ApplyProfile(string profileName)
    {
        Logger.Log("GUI", "Applying profile: " + profileName);
        string error;
        bool success = DolbyAccessAutomator.SwitchProfile(profileName, out error);

        if (success)
        {
            currentProfileIndex = ProfileState.ResolveIndex(
                profiles,
                profileName,
                currentProfileIndex,
                true);
            BuildMenu();
            trayIcon.Text = "Dolby Atmos: " + profileName;
            Logger.Log("GUI", "Profile applied successfully: " + profileName);
            trayIcon.ShowBalloonTip(2000, "Dolby Atmos Switcher", "Profile changed to: " + profileName, ToolTipIcon.Info);
        }
        else
        {
            Logger.Log("GUI", "Profile application failed: " + error);
            trayIcon.ShowBalloonTip(3000, "Dolby Atmos Switcher Error", "Failed: " + error, ToolTipIcon.Error);
        }
    }

    private bool IsStartupEnabled()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryRunKey))
        {
            return key != null && key.GetValue(AppValueName) != null;
        }
    }

    private void ToggleStartup(object sender, EventArgs e)
    {
        bool enable = !IsStartupEnabled();
        Logger.Log("GUI", "Toggling Run at Startup. Enable: " + enable);
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true))
        {
            if (key != null)
            {
                if (enable)
                {
                    key.SetValue(AppValueName, "\"" + Application.ExecutablePath + "\"");
                }
                else
                {
                    key.DeleteValue(AppValueName, false);
                }
            }
        }
        BuildMenu();
    }

    private void Exit()
    {
        Logger.Log("GUI", "Exiting application...");
        trayIcon.Visible = false;
        trayIcon.Dispose();

        if (currentIcon != null)
        {
            DestroyIcon(currentIcon.Handle);
            currentIcon.Dispose();
        }

        Application.Exit();
    }
}

static class Program
{
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);
    private const int ATTACH_PARENT_PROCESS = -1;

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetStdHandle(int nStdHandle);
    private const int STD_OUTPUT_HANDLE = -11;
    private const int STD_ERROR_HANDLE = -12;

    private static void RedirectConsole()
    {
        IntPtr stdOutput = GetStdHandle(STD_OUTPUT_HANDLE);
        if (stdOutput != IntPtr.Zero && stdOutput != new IntPtr(-1))
        {
            var safeFileHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(stdOutput, true);
            var fileStream = new System.IO.FileStream(safeFileHandle, System.IO.FileAccess.Write);
            var standardOutput = new System.IO.StreamWriter(fileStream, System.Text.Encoding.Default);
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);
        }
        IntPtr stdError = GetStdHandle(STD_ERROR_HANDLE);
        if (stdError != IntPtr.Zero && stdError != new IntPtr(-1))
        {
            var safeFileHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(stdError, true);
            var fileStream = new System.IO.FileStream(safeFileHandle, System.IO.FileAccess.Write);
            var standardError = new System.IO.StreamWriter(fileStream, System.Text.Encoding.Default);
            standardError.AutoFlush = true;
            Console.SetError(standardError);
        }
    }

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            if (AttachConsole(ATTACH_PARENT_PROCESS))
            {
                RedirectConsole();
            }

            Logger.Log("Program", "Main CLI mode started.");

            string target;
            if (CliArguments.TryGetProfile(args, out target))
            {
                Logger.Log("Program", "CLI profile change requested: " + target);
                string error;
                Console.WriteLine("\nSwitching Dolby Atmos profile to: " + target + "...");
                bool success = DolbyAccessAutomator.SwitchProfile(target, out error);

                if (success)
                {
                    Console.WriteLine("Success!");
                    return 0;
                }

                Console.WriteLine("Failed: " + error);
                return 1;
            }

            if (args[0].Equals("/change", StringComparison.OrdinalIgnoreCase) ||
                args[0].Equals("-c", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("\nError: Specify profile (Game, Movie, Music, Voice)");
                Logger.Log("Program", "Rejected an invalid CLI profile request.");
                return 2;
            }

            Console.WriteLine("\nUsage:");
            Console.WriteLine("  DolbyAtmosSwitcher.exe                  - Start background tray app");
            Console.WriteLine("  DolbyAtmosSwitcher.exe /change [Mode]   - Instantly switch to Mode (Game, Movie, Music, Voice)");
            return 2;
        }

        Logger.Log("Program", "Main GUI mode starting...");
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new DolbyAtmosAppContext());
        return 0;
    }
}
