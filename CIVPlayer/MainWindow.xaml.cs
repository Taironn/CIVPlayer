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
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using CIVPlayer.Source;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Threading;

namespace CIVPlayer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public NotifyIcon tray_icon;
		bool closing = false;
		private AppMain appMain;
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public MainWindow()
		{
			InitializeComponent();
			this.Dispatcher.UnhandledException += My_DispatcherUnhandledException;
			tray_icon = new NotifyIcon();
			var c = System.AppDomain.CurrentDomain.BaseDirectory;
			tray_icon.Icon = new Icon(Environment.CurrentDirectory + "/Resources/Statue_Of_Liberty.ico");
			tray_icon.Text = "CIV5Player";
			tray_icon.ContextMenu = new System.Windows.Forms.ContextMenu();
			tray_icon.ContextMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Kilépés", (s, ev) =>
			{
				this.closing = true;
				this.Close();
			}));
			tray_icon.DoubleClick += Tray_icon_DoubleClick;
			tray_icon.Visible = true;

			appMain = new AppMain(this);
			this.DataContext = appMain;
			textBox1.IsReadOnly = true;
			textBox2.IsReadOnly = true;
			gameConfigListView.ItemsSource = appMain.GameConfigRows;
		}


		private void Tray_icon_DoubleClick(object sender, EventArgs e)
		{
			GetToForeground();
		}

		public void GetToForeground()
		{
			this.ShowInTaskbar = true;
			this.Show();
			this.Focus();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			// setting cancel to true will cancel the close request
			// so the application is not closed
			if (!closing)
				e.Cancel = true;

			this.Hide();
			//this.tray_icon.Visible = false;
			base.OnClosing(e);
		}

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			using (var dialog = new FolderBrowserDialog())
			{
				DialogResult result = dialog.ShowDialog();
				if (result == System.Windows.Forms.DialogResult.OK)
				{
					appMain.DropBoxFolder = dialog.SelectedPath;
				}
			}
		}

		private void button2_Click(object sender, RoutedEventArgs e)
		{
			using (var dialog = new FolderBrowserDialog())
			{
				DialogResult result = dialog.ShowDialog();
				if (result == System.Windows.Forms.DialogResult.OK)
				{
					appMain.CIV5Folder = dialog.SelectedPath;
				}
			}
		}

		private void button3_Click(object sender, RoutedEventArgs e)
		{
			if (comboBox.SelectedItem == null)
			{
				System.Windows.Forms.MessageBox.Show("Válassz melyik játékos vagy!");
				return;
			}
			appMain.PlayerName = comboBox.SelectedItem.ToString();
			appMain.checkAllSettingsGiven();
			Dispatcher.BeginInvoke((Action)(() => tabControl.SelectedIndex = 0));
		}

		private void buttonLoad_Click(object sender, RoutedEventArgs e)
		{
			if (appMain.LoadGameConfigData())
			{
				if (appMain.PlayerName == "" || appMain.PlayerName == null)
				{
					comboBox.SelectedIndex = 0;
				} else
				{
					comboBox.SelectedIndex = appMain.Players.FindIndex(x => x == appMain.PlayerName);
				}
			}
		}

		void My_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			log.Fatal("Unhandled exception", e.Exception);
			System.Windows.Forms.MessageBox.Show("Nem kezelt hiba! (szólj érte :P): " + e.Exception.Message + "; ");
		}

		private void civExePathButton_Click(object sender, RoutedEventArgs e)
		{
			using (var dialog = new OpenFileDialog())
			{
				dialog.CheckFileExists = true;
				dialog.CheckPathExists = true;
				dialog.Multiselect = false;
				dialog.Filter = "Exe files |*.exe";
				log.Info("User is selecting exe path");
				DialogResult result = dialog.ShowDialog();
				if (result == System.Windows.Forms.DialogResult.OK)
				{
					string filePath = dialog.FileName;
					FileInfo fInfo = new FileInfo(filePath);
					if (fInfo.Exists == false)
					{
						log.Info("File selected does not exist");
						System.Windows.Forms.MessageBox.Show("Ez a fájl nem létezik!");
					} else if (fInfo.Extension != ".exe")
					{
						log.Info("File selected is not .exe type");
						System.Windows.Forms.MessageBox.Show("Ez a fájl nem exe típusú!");
					} else
					{
						appMain.CivExePath = filePath;
						appMain.SaveExtras();
					}
				}
			}
		}

		private void startWithCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (checkForExePath())
			{
				appMain.StartOnUsesTurn = startWithCheckBox.IsChecked ?? false;
				appMain.SaveExtras();
			} else
			{
				startWithCheckBox.IsChecked = false;
			}
		}

		private void startWithoutPropmtCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (checkForExePath())
			{
				appMain.StartWithoutPrompt = startWithoutPropmtCheckBox.IsChecked ?? false;
				appMain.SaveExtras();
			} else
			{
				startWithoutPropmtCheckBox.IsChecked = false;
			}
		}

		private void MusicCheckBox_Checked(object sender, RoutedEventArgs e)
		{
				appMain.UsersTurnMusic = MusicCheckBox.IsChecked ?? false;
				appMain.SaveExtras();
		}
		private bool checkForExePath()
		{
			if (appMain.CivExePath == null || appMain.CivExePath == "")
			{
				System.Windows.Forms.MessageBox.Show("Nem adtál meg exe útvonalat!");
				return false;
			}
			return true;
		}
	}
}