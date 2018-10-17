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
			appMain.checkAllSettingsGiven();
			//gameStatus.IsSelected = true;
			Dispatcher.BeginInvoke((Action)(() => tabControl.SelectedIndex = 0));
		}
	}
}