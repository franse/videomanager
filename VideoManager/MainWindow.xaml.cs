﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace VideoManager
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        VlcMediaPlayer player;
        SettingsWindow settingsWindow;
        FullscreenWindow fw;
        System.Windows.Forms.Control videoControl;

        public MainWindow()
        {
            InitializeComponent();

            fw = new FullscreenWindow(this);

            gridSplitter.DragDelta += new System.Windows.Controls.Primitives.DragDeltaEventHandler(gridSplitter_DragDelta);
            wfh.Child.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(PanelVideo_DoubleClick);
        }

        void PanelVideo_DoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            SetToFullscreen();
        }


		#region Fullscreen
		public void SetToFullscreen()
        {
            if (player != null && !player.IsFullscreen) // fullscreen only when video loaded, back always
            {
                player.IsFullscreen = true;
                videoControl = wfh.Child.Parent;
                wfh.Child.Parent = fw.wfh.Child;        // this.panelVideo.parent = fw.panelVideo
                wfh.Child.Dock = System.Windows.Forms.DockStyle.Fill;
                fw.Show();
            }
        }


        public void SetBackToNormalScreen()     // TODO event based
        {
            wfh.Child.Dock = System.Windows.Forms.DockStyle.None;
            System.Windows.Forms.Control ctrl = fw.wfh.Child.Controls[0];
            ctrl.Parent = videoControl;     // fw.panelVideo.child.parent = this.panelVideo.parent
            player.IsFullscreen = false;
        }
		#endregion


		private void gridSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            int delta = (int)(mainGrid.ColumnDefinitions[0].ActualWidth + e.HorizontalChange);
            if (delta > 0)
                mainGrid.ColumnDefinitions[0].Width = new GridLength(delta);
        }


        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // AppData folder
            Properties.Settings.Default.AppDataFolder = 
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + 
                System.IO.Path.DirectorySeparatorChar + Properties.Settings.Default.GeneralProductName;
#if DEBUG
            Properties.Settings.Default.AppDataFolder = 
                @"C:\Users\Franz\Documents\codeprojects\VideoManager\VideoManager\local";
#endif
            Properties.Settings.Default.ConnectionString = "data source=\"" + 
                Properties.Settings.Default.AppDataFolder + System.IO.Path.DirectorySeparatorChar + 
                Properties.Settings.Default.DatabaseFilename + "\"";

            //Database.FillFromDirectory(@"C:\Users\Franz\Documents\codeprojects\VideoManager\VideoManager\local\test", false);
            

            // init windows
            settingsWindow = new SettingsWindow();


            // init libraries
            if (!VideoMgr.InitLibraryPath())
            {
                MessageBox.Show("DLLs not found. Please enter the path to your VLC libraries!");
                settingsWindow.ShowDialog();
            }

            if (VideoMgr.InitLibraryPath())
                Utils.SetDllDirectory(Properties.Settings.Default.LibraryPath);
            else
            {
                MessageBox.Show("DLLs still not found. Exiting...");
                Application.Current.Shutdown();
                return;
            }

            string pluginPath = Environment.GetEnvironmentVariable("VLC_PLUGIN_PATH");
            if (pluginPath == null)
            {
                MessageBox.Show("You have to set the environment variable VLC_PLUGIN_PATH to " + 
                    Properties.Settings.Default.LibraryPath + " and restart " + 
                    Properties.Settings.Default.GeneralProductName + "!");
                Application.Current.Shutdown();
                return;
            }


            // restore old window state
            if (Properties.Settings.Default.WindowWidth > 0 && Properties.Settings.Default.WindowHeight > 0)
            {
                this.Width = Properties.Settings.Default.WindowWidth;
                this.Height = Properties.Settings.Default.WindowHeight;
                if (Properties.Settings.Default.WasMaximized)
                    this.WindowState = System.Windows.WindowState.Maximized;
                this.Left = Properties.Settings.Default.WindowLeft;
                this.Top = Properties.Settings.Default.WindowTop;
				double plWidth = Properties.Settings.Default.PlaylistWidth;
				this.mainGrid.ColumnDefinitions[2].Width = 
					new GridLength(this.mainGrid.ActualWidth - this.gridSplitter.ActualWidth - plWidth);
				this.mainGrid.ColumnDefinitions[0].Width = new GridLength(plWidth);
            }


            // init VLC
			player = new VlcMediaPlayer();

			player.Drawable = wfh.Child.Handle;
			player.PlayingStatusChanged += new VlcMediaPlayer.PlayingStatusChangedHandler(PlayingStatusChanged);
			player.LengthChanged += new VlcMediaPlayer.LengthChangedHandler(LengthChanged);

			player.SetVolume(sliVolume.Value);


            // init input
            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyDownEvent, new KeyEventHandler(keyDown), false);


            // init UI
            sliVolume.Value = Properties.Settings.Default.Volume;
        }


		#region Window Close Events
		private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// save window state
			Properties.Settings.Default.WindowWidth = this.Width;
			Properties.Settings.Default.WindowHeight = this.Height;
			Properties.Settings.Default.WasMaximized = (this.WindowState == System.Windows.WindowState.Maximized);
			Properties.Settings.Default.WindowLeft = this.Left;
			Properties.Settings.Default.WindowTop = this.Top;
			Properties.Settings.Default.PlaylistWidth = this.mainGrid.ColumnDefinitions[0].Width.Value;
			Properties.Settings.Default.Volume = (int)sliVolume.Value;
			Properties.Settings.Default.Save();

			// free memory
			if (player != null)
				player.Dispose();
		}


        private void mainWindow_Closed(object sender, EventArgs e)
        {
        }
#endregion


        private void keyDown(object sender, KeyEventArgs e)
        {
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);
            if (key == Key.Enter)// && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.System)))
                if (player != null)
                    SetToFullscreen();
        }


		#region Button Click Handlers
		private void button1_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.FileName = "";
            ofd.Filter = VideoMgr.GetAllowedFiletypesSelector();
            if (ofd.ShowDialog() != true)
                return;

			player.SetMediaFile(ofd.FileName);	// TODO player event media changed -> setmediafile, volume, etc.
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            if (player != null)
                player.Play();
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            if (player != null)
                player.Pause();
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            if (player != null)
                player.Stop();
        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {            
            settingsWindow.Show();
        }
		#endregion


		#region Volume Slider
		private void sliVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (player != null)
                player.SetVolume(sliVolume.Value);
        }
		#endregion


		#region Playing Status Change
		private System.Threading.Timer timerProgressUpdate;

		private void PlayingStatusChanged(object sender, VlcMediaPlayer.PlayingStatusEventArgs e)
		{
			switch (e.Status)
			{
				case VlcMediaPlayer.PlayingStatus.PLAYING:
					timerProgressUpdate = new System.Threading.Timer(new System.Threading.TimerCallback(ProgressUpdate), player, 0, 500);
					break;
				case VlcMediaPlayer.PlayingStatus.PAUSED:
					if (timerProgressUpdate != null)
						timerProgressUpdate.Dispose();
					break;
				case VlcMediaPlayer.PlayingStatus.STOPPED:
					if (timerProgressUpdate != null)
						timerProgressUpdate.Dispose();
					break;
				default: 
					break;
			}
		}

		private void ProgressUpdate(object state)
		{
			if (state == null)
				return;
			VlcMediaPlayer player = (VlcMediaPlayer)state;
			float pos = player.Position;
			sliTime.Dispatcher.Invoke(new Action(() => sliTime.Value = pos), null);
			float rel = pos * player.Length;
			int totalSecs = (int)rel / 1000;
			int mins = totalSecs / 60;
			int secs = totalSecs - mins * 60;
			string strTime = mins.ToString() + ":" + secs.ToString("0#");
			lblVideoTime.Dispatcher.Invoke(new Action(() => lblVideoTime.Content = strTime), null);
		}
		#endregion


		private void LengthChanged(object sender, VlcMediaPlayer.LengthEventArgs e)
		{
			long totalSeconds = e.Length / 1000;
			long hours = totalSeconds / 3600;
			long minutes = (totalSeconds - hours * 3600) / 60;
			long seconds = totalSeconds - hours * 3600 - minutes * 60;
			string strLength = "/" + ((hours > 0) ? (hours.ToString() + ":") : "") +
				minutes.ToString("0#") + ":" + seconds.ToString("0#");
			lblVideoLength.Dispatcher.Invoke(new Action(() => lblVideoLength.Content = strLength), null);

			// TODO save length to database
		}
	}
}
