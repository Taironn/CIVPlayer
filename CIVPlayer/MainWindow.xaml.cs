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

namespace CIVPlayer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		NotifyIcon tray_icon;
		bool closing = false;
		public MainWindow()
		{
			InitializeComponent();
			tray_icon = new NotifyIcon();
			var c = System.AppDomain.CurrentDomain.BaseDirectory;
			tray_icon.Icon = new Icon(Environment.CurrentDirectory + "/Resources/Statue_Of_Liberty.ico");
			tray_icon.Text = "CIV5Player";
			tray_icon.ContextMenu = new System.Windows.Forms.ContextMenu();
			tray_icon.ContextMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Kilépés", (s, ev) => {
				this.closing = true;
				this.Close(); }));
			tray_icon.DoubleClick += Tray_icon_DoubleClick;
			tray_icon.Visible = true;
		}

		private void Tray_icon_DoubleClick(object sender, EventArgs e)
		{
			this.ShowInTaskbar = true;
			this.Show();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			// setting cancel to true will cancel the close request
			// so the application is not closed
			if (!closing)
				e.Cancel = true;

			this.Hide();
			this.tray_icon.Visible = false;
			base.OnClosing(e);
		}
	}
}