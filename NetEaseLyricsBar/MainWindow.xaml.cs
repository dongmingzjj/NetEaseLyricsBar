using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using NetEaseLyricsBar.Models;
using NetEaseLyricsBar.ViewModels;
using NetEaseLyricsBar.Services;

namespace NetEaseLyricsBar
{
    /// <summary>
    /// 简单的 ICommand 实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object? parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public partial class MainWindow : Window
    {
        // Win32 API for enhanced topmost
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        private MainViewModel _viewModel;
        private bool _isLocked = false;
        private WindowSettings? _windowSettings;
        private const string SettingsFile = "windowsettings.json";
        private bool _isUpdatingPosition = false;  // 防止循环更新
        private string _taskbarPosition = "bottom";  // 任务栏位置
        private System.Windows.Threading.DispatcherTimer? _colorCheckTimer;  // 背景颜色检测定时器
        private System.Windows.Threading.DispatcherTimer? _taskbarSizeCheckTimer;  // 任务栏大小检测定时器

        // 托盘图标命令
        private ICommand _showWindowCommand;
        private ICommand _hideWindowCommand;

        /// <summary>
        /// 显示窗口命令
        /// </summary>
        public ICommand ShowWindowCommand => _showWindowCommand;

        /// <summary>
        /// 隐藏窗口命令
        /// </summary>
        public ICommand HideWindowCommand => _hideWindowCommand;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _viewModel = new MainViewModel();
                DataContext = _viewModel;

                // 初始化托盘图标命令
                _showWindowCommand = new RelayCommand(ShowWindow);
                _hideWindowCommand = new RelayCommand(HideWindow);

                // 添加未处理异常捕获
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                Dispatcher.UnhandledException += OnDispatcherUnhandledException;

                // 加载窗口设置
                LoadWindowSettings();

                // 监听窗口位置和大小变化
                LocationChanged += MainWindow_LocationChanged;
                SizeChanged += MainWindow_SizeChanged;

                // 监听窗口激活，确保置顶
                Activated += (s, e) => ForceTopmost();

                // 监听窗口关闭事件，关闭到托盘
                Closing += MainWindow_Closing;

                // 初始化任务栏大小检测定时器
                InitializeTaskbarSizeCheckTimer();

                // 启动时定位并保存位置
                Loaded += (s, e) =>
                {
                    try
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            // 首先定位到任务栏
                            CenterWindow();

                            // 设置托盘图标
                            SetTrayIcon();

                            // 强制置顶（使用Win32 API确保高于任务栏）
                            ForceTopmost();

                            // 启动任务栏大小检测定时器
                            _taskbarSizeCheckTimer?.Start();

                            // 更新锁定菜单项状态
                            UpdateLockMenuItem();

                            // 检测并更新文字颜色 (暂时禁用，可能导致性能问题)
                            // UpdateTextColorBasedOnBackground();

                            // 延迟保存位置
                            System.Threading.Thread.Sleep(100);
                            SaveWindowSettings();
                        }, System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                    catch (Exception ex)
                    {
                        LogError("Loaded event", ex);
                    }
                };
            }
            catch (Exception ex)
            {
                LogError("MainWindow constructor", ex);
                MessageBox.Show($"启动失败: {ex.Message}\n\n详细信息已记录到调试日志", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 捕获未处理的异常
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogError("UnhandledException", ex);
                MessageBox.Show($"未处理的异常: {ex.Message}\n\n详细信息已记录到调试日志", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 捕获 Dispatcher 线程的未处理异常
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError("DispatcherUnhandledException", e.Exception);
            MessageBox.Show($"UI线程异常: {e.Exception.Message}\n\n详细信息已记录到调试日志", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // 防止应用崩溃
        }

        // 记录错误到调试日志
        private void LogError(string location, Exception ex)
        {
            try
            {
                var logPath = Path.Combine(Path.GetTempPath(), "netease_lyrics_debug.txt");
                var errorMsg = $"[{DateTime.Now:HH:mm:ss.fff}] ERROR at {location}: {ex.GetType().Name}\n";
                errorMsg += $"  Message: {ex.Message}\n";
                errorMsg += $"  StackTrace: {ex.StackTrace}\n";
                if (ex.InnerException != null)
                {
                    errorMsg += $"  InnerException: {ex.InnerException.Message}\n";
                }
                File.AppendAllText(logPath, errorMsg);
            }
            catch { }
        }

        // 初始化背景颜色检测定时器
        private void InitializeColorCheckTimer()
        {
            _colorCheckTimer = new System.Windows.Threading.DispatcherTimer();
            _colorCheckTimer.Interval = TimeSpan.FromSeconds(2); // 每2秒检测一次
            _colorCheckTimer.Tick += (s, e) => UpdateTextColorBasedOnBackground();
            _colorCheckTimer.Start();
        }

        // 根据背景颜色更新文字颜色
        private void UpdateTextColorBasedOnBackground()
        {
            try
            {
                var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (windowHandle == IntPtr.Zero) return;

                var textColor = BackgroundColorService.GetOptimalTextColor(windowHandle, Width, Height);
                var secondaryTextColor = BackgroundColorService.GetSecondaryTextColor(textColor);
                var bgColor = BackgroundColorService.GetAcrylicBackgroundColor(textColor);

                // 更新主歌词文字颜色
                LyricsText.Foreground = new SolidColorBrush(textColor);

                // 更新歌手信息文字颜色
                LyricsInfo.Foreground = new SolidColorBrush(secondaryTextColor);

                // 更新背景颜色（如果需要）
                // LyricsContainer.Background = new SolidColorBrush(bgColor);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新文字颜色失败: {ex.Message}");
            }
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            ConstrainWindowPositionAndSize();
            SaveWindowSettings();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ConstrainWindowPositionAndSize();
            SaveWindowSettings();
        }

        // 根据任务栏位置约束窗口的移动和调整大小
        private void ConstrainWindowPositionAndSize()
        {
            if (_isUpdatingPosition) return;

            try
            {
                // 获取任务栏信息
                var taskbarInfo = TaskbarService.GetTaskbarInfo();
                _taskbarPosition = taskbarInfo.Position;

                _isUpdatingPosition = true;

                switch (_taskbarPosition)
                {
                    case "bottom":
                    case "top":
                        // 水平任务栏：固定Y坐标和高度
                        Top = taskbarInfo.Rect.Top;
                        Height = taskbarInfo.Rect.Height;
                        break;

                    case "left":
                    case "right":
                        // 垂直任务栏：固定X坐标和宽度
                        Left = taskbarInfo.Rect.Left;
                        Width = taskbarInfo.Rect.Width;
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"约束窗口位置失败: {ex.Message}");
            }
            finally
            {
                _isUpdatingPosition = false;
            }
        }

        // 窗口拖动
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                if (_isLocked) return;

                try
                {
                    DragMove();
                }
                catch (InvalidOperationException)
                {
                    // 窗口最大化时不处理
                }
            }
        }

        // 歌词容器拖动
        private void LyricsContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                if (_isLocked) return;

                try
                {
                    DragMove();
                }
                catch (InvalidOperationException)
                {
                    // 窗口最大化时不处理
                }
            }
        }

        // 菜单事件处理
        // 已注释：置顶切换功能已移除，窗口保持始终置顶
        // private void ToggleTopmostMenuItem_Click(object sender, RoutedEventArgs e)
        // {
        //     ToggleTopmost();
        // }

        private void LockMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ToggleLock();
        }

        private void ResetPositionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ResetPosition();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var aboutWindow = new AboutWindow
                {
                    Owner = this
                };
                aboutWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LogError("AboutMenuItem_Click", ex);
                // Fallback to message box if custom window fails
                MessageBox.Show(
                    "网易云音乐歌词栏\n\n" +
                    "版本: 1.0.0\n" +
                    "技术栈: C# WPF\n\n" +
                    "功能特点:\n" +
                    "• 透明无边框窗口\n" +
                    "• 窗口拖动和位置记忆\n" +
                    "• 三种歌词动画效果\n" +
                    "• 系统托盘支持\n" +
                    "• 锁定位置功能\n\n" +
                    "快捷键:\n" +
                    "← → 切换歌词\n" +
                    "空格 切换动画\n" +
                    "Ctrl+Alt+R 重置位置\n" +
                    "右键 打开菜单",
                    "关于",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // 切换置顶状态
        // 已注释：置顶切换功能已移除，窗口保持始终置顶
        // private void ToggleTopmost()
        // {
        //     try
        //     {
        //         Topmost = !Topmost;
        //         UpdateTopmostMenuItem();
        //         SaveWindowSettings();
        //     }
        //     catch (Exception ex)
        //     {
        //         LogError("ToggleTopmost", ex);
        //     }
        // }

        // private void UpdateTopmostMenuItem()
        // {
        //     try
        //     {
        //         var contextMenu = LyricsContainer.ContextMenu as ContextMenu;
        //         if (contextMenu != null && contextMenu.Items.Count > 0)
        //         {
        //             var topmostMenuItem = contextMenu.Items[0] as MenuItem;
        //             if (topmostMenuItem != null)
        //             {
        //                 topmostMenuItem.Header = Topmost ? "✓ 始终置顶" : "📌 始终置顶";
        //             }
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         LogError("UpdateTopmostMenuItem", ex);
        //     }
        // }

        // 切换锁定状态
        private void ToggleLock()
        {
            try
            {
                _isLocked = !_isLocked;
                UpdateLockMenuItem();
            }
            catch (Exception ex)
            {
                LogError("ToggleLock", ex);
            }
        }

        private void UpdateLockMenuItem()
        {
            try
            {
                // 更新菜单项文本（注意：置顶菜单项已移除，第一个菜单项现在是锁定）
                var contextMenu = LyricsContainer.ContextMenu as ContextMenu;
                if (contextMenu != null && contextMenu.Items.Count > 0)
                {
                    var lockMenuItem = contextMenu.Items[0] as MenuItem;
                    if (lockMenuItem != null)
                    {
                        lockMenuItem.Header = _isLocked ? "🔒 解锁位置" : "🔓 锁定位置";
                    }
                }

                // 锁定时改变背景色
                if (_isLocked)
                {
                    var color = System.Windows.Media.Color.FromArgb(230, 0, 60, 100);
                    LyricsContainer.Background = new System.Windows.Media.SolidColorBrush(color);
                }
                else
                {
                    var color = System.Windows.Media.Color.FromArgb(230, 0, 0, 0);
                    LyricsContainer.Background = new System.Windows.Media.SolidColorBrush(color);
                }
            }
            catch (Exception ex)
            {
                LogError("UpdateLockMenuItem", ex);
            }
        }

        // 重置窗口位置
        private void ResetPosition()
        {
            // 如果窗口已最大化或最小化，恢复正常状态
            if (WindowState != WindowState.Normal)
            {
                WindowState = WindowState.Normal;
            }

            // 居中窗口
            CenterWindow();
        }

        private void CenterWindow()
        {
            // 定位到任务栏的空位区域，避免遮挡任务栏图标
            try
            {
                var availableSpace = TaskbarService.GetAvailableTaskbarSpace();

                Left = availableSpace.X;
                Top = availableSpace.Y;
                Width = availableSpace.Width;
                Height = availableSpace.Height;

                System.Diagnostics.Debug.WriteLine($"窗口定位到: X={Left}, Y={Top}, Width={Width}, Height={Height}");
            }
            catch (Exception ex)
            {
                // 如果获取任务栏信息失败，使用默认底部居中位置
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                double defaultWidth = Math.Min(600, screenWidth - 100);
                double defaultHeight = 40;

                Left = (screenWidth - defaultWidth) / 2;
                Top = screenHeight - defaultHeight;
                Width = defaultWidth;
                Height = defaultHeight;

                System.Diagnostics.Debug.WriteLine($"获取任务栏信息失败，使用默认位置: {ex.Message}");
            }
        }

        // 保存窗口设置
        private void SaveWindowSettings()
        {
            try
            {
                _windowSettings = new WindowSettings
                {
                    X = Left,
                    Y = Top,
                    Width = Width,
                    Height = Height,
                    IsLocked = _isLocked
                };

                // 使用应用程序设置保存（不保存Topmost，始终保持置顶）
                var properties = NetEaseLyricsBar.Properties.Settings.Default;
                properties.WindowLeft = Left;
                properties.WindowTop = Top;
                properties.WindowWidth = Width;
                properties.WindowHeight = Height;
                properties.IsLocked = _isLocked;
                // Topmost状态不保存，窗口始终置顶
                properties.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
            }
        }

        // 加载窗口设置
        private void LoadWindowSettings()
        {
            try
            {
                var properties = NetEaseLyricsBar.Properties.Settings.Default;
                if (properties.WindowWidth > 0)
                {
                    Left = properties.WindowLeft;
                    Top = properties.WindowTop;
                    Width = properties.WindowWidth;
                    Height = properties.WindowHeight;
                    _isLocked = properties.IsLocked;
                    // Topmost不加载，始终保持置顶（XAML中已设置Topmost="True"）
                    UpdateLockMenuItem();
                    // 已注释：置顶菜单项已移除
                    // UpdateTopmostMenuItem();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
                // 使用默认位置（居中）
                CenterWindow();
            }
        }

        // 托盘图标事件处理
        private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void ShowWindowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void HideWindowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            HideWindow();
        }

        // 键盘快捷键
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.R && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
            {
                e.Handled = true;
                ResetPosition();
            }
        }

        // 窗口关闭时停止定时器
        protected override void OnClosed(EventArgs e)
        {
            _colorCheckTimer?.Stop();
            _taskbarSizeCheckTimer?.Stop();
            base.OnClosed(e);
        }

        // 窗口关闭事件 - 关闭到托盘
        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            e.Cancel = true; // 取消关闭
            Hide(); // 隐藏到托盘
        }

        // 显示窗口（从托盘）
        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            ForceTopmost();
        }

        // 隐藏窗口（到托盘）
        private void HideWindow()
        {
            Hide();
        }

        // 强制置顶（使用Win32 API）
        private void ForceTopmost()
        {
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                var hwnd = helper.Handle;

                if (hwnd != IntPtr.Zero)
                {
                    // 设置窗口为最顶层，高于任务栏
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

                    System.Diagnostics.Debug.WriteLine("[MainWindow] ✓ 强制置顶成功");
                }
            }
            catch (Exception ex)
            {
                LogError("ForceTopmost", ex);
            }
        }

        // 初始化任务栏大小检测定时器
        private void InitializeTaskbarSizeCheckTimer()
        {
            _taskbarSizeCheckTimer = new System.Windows.Threading.DispatcherTimer();
            _taskbarSizeCheckTimer.Interval = TimeSpan.FromSeconds(2); // 每2秒检测一次
            _taskbarSizeCheckTimer.Tick += (s, e) => CheckTaskbarSize();
        }

        // 检查任务栏大小变化
        private void CheckTaskbarSize()
        {
            try
            {
                if (_isLocked) return; // 锁定时不调整大小

                var availableSpace = TaskbarService.GetAvailableTaskbarSpace();
                var taskbarInfo = TaskbarService.GetTaskbarInfo();

                // 只在水平任务栏模式下调整宽度
                if (_taskbarPosition == "bottom" || _taskbarPosition == "top")
                {
                    // 获取当前可用宽度
                    int availableWidth = availableSpace.Width;
                    int currentWidth = (int)Width;

                    // 如果可用宽度变化超过20px，调整窗口宽度
                    if (Math.Abs(availableWidth - currentWidth) > 20)
                    {
                        int newWidth = Math.Min(availableWidth - 20, 800); // 最大800px，留20px边距
                        newWidth = Math.Max(newWidth, 300); // 最小300px

                        System.Diagnostics.Debug.WriteLine($"[MainWindow] 任务栏宽度变化: {currentWidth} -> {newWidth}");

                        Width = newWidth;
                        SaveWindowSettings();
                    }

                    // 确保X坐标在可用范围内
                    int currentX = (int)Left;
                    int targetX = availableSpace.X;

                    if (Math.Abs(currentX - targetX) > 10)
                    {
                        Left = targetX;
                    }
                }

                // 强制置顶
                ForceTopmost();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查任务栏大小失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置托盘图标
        /// </summary>
        private void SetTrayIcon()
        {
            try
            {
                // 从资源流加载图标文件
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream("NetEaseLyricsBar.1.ico");

                if (stream != null)
                {
                    // 直接加载 ICO 文件
                    var icon = new System.Drawing.Icon(stream);
                    NotifyIcon.Icon = icon;
                    stream.Dispose();
                    System.Diagnostics.Debug.WriteLine("[MainWindow] ✓ 托盘图标已加载: 1.ico");
                }
                else
                {
                    // 如果资源加载失败，尝试从文件系统加载
                    var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "1.ico");
                    if (System.IO.File.Exists(iconPath))
                    {
                        using (var iconStream = System.IO.File.OpenRead(iconPath))
                        {
                            var icon = new System.Drawing.Icon(iconStream);
                            NotifyIcon.Icon = icon;
                            System.Diagnostics.Debug.WriteLine("[MainWindow] ✓ 托盘图标已加载（文件）: 1.ico");
                        }
                    }
                    else
                    {
                        // 使用系统默认图标
                        NotifyIcon.Icon = System.Drawing.SystemIcons.Application;
                        System.Diagnostics.Debug.WriteLine("[MainWindow] ⚠ 图标文件未找到，使用系统默认图标");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] ⚠ 加载托盘图标失败: {ex.Message}");
                // 使用系统默认图标
                try
                {
                    NotifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
                catch { }
            }
        }
    }
}
