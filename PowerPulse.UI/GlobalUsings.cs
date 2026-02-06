// Resolve ambiguity between System.Windows (WPF) and System.Windows.Forms types
// WinForms is included only for NotifyIcon tray icon support
global using Application = System.Windows.Application;
global using Window = System.Windows.Window;
global using MessageBox = System.Windows.MessageBox;
