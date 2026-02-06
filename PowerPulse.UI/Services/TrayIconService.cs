using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace PowerPulse.UI.Services;

/// <summary>
/// Manages the system tray (notification area) icon for PowerPulse.
/// Uses System.Windows.Forms.NotifyIcon for WPF interop.
/// </summary>
public class TrayIconService : IDisposable
{
    private Forms.NotifyIcon? _notifyIcon;
    private readonly Window _mainWindow;

    public TrayIconService(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    /// <summary>
    /// Creates and shows the tray icon with context menu.
    /// </summary>
    public void Initialize()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = CreateBatteryIcon(),
            Text = "PowerPulse — Battery Monitor",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    /// <summary>
    /// Updates the tray icon tooltip with current battery info.
    /// </summary>
    public void UpdateTooltip(string text)
    {
        if (_notifyIcon != null)
        {
            // NotifyIcon.Text max is 127 chars
            _notifyIcon.Text = text.Length > 127 ? text[..127] : text;
        }
    }

    /// <summary>
    /// Shows a balloon notification.
    /// </summary>
    public void ShowNotification(string title, string message, Forms.ToolTipIcon icon = Forms.ToolTipIcon.Info)
    {
        _notifyIcon?.ShowBalloonTip(3000, title, message, icon);
    }

    private void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private Forms.ContextMenuStrip CreateContextMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open PowerPulse", null, (_, _) => ShowMainWindow());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) =>
        {
            App.TrayIconActive = false;
            _notifyIcon!.Visible = false;
            Application.Current.Shutdown();
        });
        return menu;
    }

    /// <summary>
    /// Creates a simple battery icon programmatically (no .ico file needed).
    /// </summary>
    private static Icon CreateBatteryIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);

        g.Clear(Color.Transparent);

        // Battery body
        using var pen = new Pen(Color.White, 1);
        g.DrawRectangle(pen, 2, 3, 10, 10);

        // Battery tip
        g.FillRectangle(Brushes.White, 5, 1, 4, 2);

        // Fill (green)
        g.FillRectangle(Brushes.LimeGreen, 3, 5, 9, 7);

        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        GC.SuppressFinalize(this);
    }
}
