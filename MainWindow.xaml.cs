﻿using Microsoft.Win32;
using NotifyIconLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;

namespace CSAuto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        NotifyIconWrapper notifyicon = new NotifyIconWrapper();
        ContextMenu exitcm = new ContextMenu();
        System.Windows.Threading.DispatcherTimer appTimer = new System.Windows.Threading.DispatcherTimer();
        const string VER = "1.0.1";
        Point csgoResolution = new Point();
        Point ORIGINAL_BUTTON_LOCATION = new Point(591, 393);
        System.Windows.Point CORRECT_BUTTON_LOCATION = new System.Windows.Point();
        Color BUTTON_COLOR = Color.FromArgb(76, 175, 80);
        Color ACTIVE_BUTTON_COLOR = Color.FromArgb(90, 203, 95);
        int frame = 0;
        MenuItem startUpCheck = new MenuItem();
        MenuItem debugCheck = new MenuItem();
        public ImageSource ToImageSource(Icon icon)
        {
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return imageSource;
        }
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                killDuplicates();
                //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
                Application.Current.Exit += Current_Exit;
                MenuItem exit = new MenuItem();
                exit.Header = "Exit";
                exit.Click += Exit_Click;
                MenuItem about = new MenuItem();
                about.Header = $"{typeof(MainWindow).Namespace} - {VER}";
                about.IsEnabled = false;
                about.Icon = new System.Windows.Controls.Image
                {
                    Source = ToImageSource(System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location))
                };
                startUpCheck.IsChecked = Properties.Settings.Default.runAtStartUp;
                startUpCheck.Header = "Start With Windows";
                startUpCheck.IsCheckable = true;
                startUpCheck.Click += StartUpCheck_Click;
                debugCheck.IsChecked = Properties.Settings.Default.saveDebugFrames;
                debugCheck.Header = "Save Frames";
                debugCheck.IsCheckable = true;
                debugCheck.Click += DebugCheck_Click;
                exitcm.Items.Add(about);
                exitcm.Items.Add(new Separator());
                exitcm.Items.Add(debugCheck);
                exitcm.Items.Add(startUpCheck);
                exitcm.Items.Add(new Separator());
                exitcm.Items.Add(exit);
                exitcm.StaysOpen = false;
                Top = -1000;
                Left = -1000;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DebugCheck_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.saveDebugFrames = debugCheck.IsChecked;
            Properties.Settings.Default.Save();
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        //This is a replacement for Cursor.Position in WinForms
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        //This simulates a left mouse click
        public static void LeftMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
            Debug.WriteLine($"Left clicked at ({xpos},{ypos})");
        }
        private void StartUpCheck_Click(object sender, RoutedEventArgs e)
        {
            string appname = Assembly.GetEntryAssembly().GetName().Name;
            string executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if ((bool)startUpCheck.IsChecked)
                {
                    Properties.Settings.Default.runAtStartUp = true;
                    rk.SetValue(appname, executablePath);
                }
                else
                {
                    Properties.Settings.Default.runAtStartUp = false;
                    rk.DeleteValue(appname, false);
                }
            }
            Properties.Settings.Default.Save();
        }
        Process GetActiveProcess()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return null;
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            Process p = Process.GetProcessById((int)pid);
            return p;
        }
        private void TimerCallback(object sender, EventArgs e)
        {
            try
            {
                Process activeProcces = GetActiveProcess();
                if (activeProcces == null)
                    return;
                string proccesName = activeProcces.ProcessName;
                if (proccesName == "csgo")
                {
                    if (csgoResolution == new Point())
                    {
                        csgoResolution = new Point(
                            (int)SystemParameters.PrimaryScreenWidth,
                            (int)SystemParameters.PrimaryScreenHeight);
                    }
                    System.Windows.Point aspectRatio = new System.Windows.Point(csgoResolution.X / 1400.0, csgoResolution.Y / 1050.0);
                    CORRECT_BUTTON_LOCATION = new System.Windows.Point(ORIGINAL_BUTTON_LOCATION.X * aspectRatio.X, ORIGINAL_BUTTON_LOCATION.Y * aspectRatio.Y);
                    using (Bitmap bitmap = new Bitmap(218, 86))
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.CopyFromScreen(new Point((int)CORRECT_BUTTON_LOCATION.X, (int)CORRECT_BUTTON_LOCATION.Y), Point.Empty, new System.Drawing.Size(218, 86));
                        }
                        if (Properties.Settings.Default.saveDebugFrames)
                        {
                            Directory.CreateDirectory($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location.ToString())}\\FRAMES");
                            bitmap.Save($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location.ToString())}\\FRAMES\\Frame{frame++}.jpeg", ImageFormat.Jpeg);
                        }
                        Color pixelColor = bitmap.GetPixel(70, 10);
                        if (pixelColor == BUTTON_COLOR || pixelColor == ACTIVE_BUTTON_COLOR)
                        {
                            Debug.WriteLine("Found accept button");
                            var clickpoint = new Point((int)CORRECT_BUTTON_LOCATION.X + 70, (int)CORRECT_BUTTON_LOCATION.Y + 10);
                            int X = clickpoint.X;
                            int Y = clickpoint.Y;
                            LeftMouseClick(X, Y);
                        }
                    }
                }
                else
                {
                    csgoResolution = new Point();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex}");
            }
        }
        private void Notifyicon_RightMouseButtonClick(object sender, NotifyIconLibrary.Events.MouseLocationEventArgs e)
        {
            exitcm.IsOpen = true;
            Activate();
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void Current_Exit(object sender, ExitEventArgs e)
        {
            this.notifyicon.Close();
        }
        private bool killDuplicates()
        {
            bool success = true;
            var currentProcess = Process.GetCurrentProcess();
            var duplicates = Process.GetProcessesByName(currentProcess.ProcessName).Where(o => o.Id != currentProcess.Id);

            if (duplicates.Count() > 0)
            {
                notifyicon.Close();
                App.Current.Shutdown();
            }

            return success;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            try
            {
                Visibility = Visibility.Hidden;
                this.notifyicon.Icon = Properties.Resources.main;
                this.notifyicon.Tip = "CSAuto - Auto csgo match acceptor";
                this.notifyicon.ShowTip = true;
                this.notifyicon.RightMouseButtonClick += Notifyicon_RightMouseButtonClick;
                this.notifyicon.Update();
                appTimer.Interval = TimeSpan.FromMilliseconds(1000);
                appTimer.Tick += new EventHandler(TimerCallback);
                appTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
