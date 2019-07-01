using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;

namespace CIVPlayer.Source
{
    class AppMain : INotifyPropertyChanged
    {
        private AppConfiguaraion appConfig;
        private GameConfig gameConfig { get; }
        private StateAPI stateApi;
        private SoundPlayer soundPlayer;
        private MailSender mailSender;
        private Statistics statistics;
        private string dropBoxFolder;
        public string DropBoxFolder
        {
            get
            {
                return dropBoxFolder;
            }
            set
            {
                dropBoxFolder = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DropBoxFolder"));
            }
        }
        private string civExePath;
        public string CivExePath
        {
            get
            {
                return civExePath;
            }

            set
            {
                civExePath = value;
                appConfig.CivExePath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CivExePath"));
            }
        }
        private string civ5Folder;
        public string CIV5Folder
        {
            get
            {
                return civ5Folder;
            }

            set
            {
                civ5Folder = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CIV5Folder"));
            }
        }
        private string playerName;
        public string PlayerName
        {
            get
            {
                return playerName;
            }

            set
            {
                playerName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlayerName"));
            }
        }
        public string CurrentPlayer
        {
            get
            {
                return stateApi.CurrentPlayer;
            }
        }
        public List<string> Players
        {
            get
            {
                return gameConfig.players;
            }
        }
        private bool startOnUsesTurn;
        public bool StartOnUsesTurn
        {
            get
            {
                return startOnUsesTurn;
            }

            set
            {
                startOnUsesTurn = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StartOnUsesTurn"));
            }
        }
        private bool startWithoutPrompt;
        public bool StartWithoutPrompt
        {
            get
            {
                return startWithoutPrompt;
            }

            set
            {
                startWithoutPrompt = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StartWithoutPrompt"));
                if (value)
                {
                    StartOnUsesTurn = true;
                }
            }
        }
        private bool usersTurnMusic;
        public bool UsersTurnMusic
        {
            get
            {
                return usersTurnMusic;
            }

            set
            {
                usersTurnMusic = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UsersTurnMusic"));
            }
        }
        public List<GameConfigListRow> GameConfigRows
        {
            get
            {
                return gameConfig.gameConfigRows;
            }
        }

        private StatisticRow statisticRow;
        public StatisticRow LastStatisticRow
        {
            get
            {
                return statisticRow;
            }

            set
            {
                statisticRow = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LastStatisticRow"));
            }
        }

        public FileInfo appConfigPath = new FileInfo(Environment.CurrentDirectory + "/Resources/appconfig.bin");
        private MainWindow appWindow;

        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AppMain(MainWindow appWindow)
        {
            this.appWindow = appWindow;
            log.Info("Application started");
            FileInfo soundFile = new FileInfo(Environment.CurrentDirectory + "/Resources/notification.wav");
            if (soundFile.Exists)
            {
                soundPlayer = new SoundPlayer(Environment.CurrentDirectory + "/Resources/notification.wav");
            }
            else
            {
                soundPlayer = null;
            }
            gameConfig = new GameConfig();
            appConfig = new AppConfiguaraion();
            stateApi = new StateAPI(appConfig, gameConfig, this.VarnUser, this.appWindow.Dispatcher.Invoke);
            stateApi.CurrentPlayerChanged += this.currentPlayerChangedHandler;
            stateApi.UsersTurn += this.UsersTurnHandler;
            stateApi.UserPassed += this.UserPassedHandler;
            ReadAppConfig();
        }

        public void checkAllSettingsGiven()
        {
            if (DropBoxFolder != "" && CIV5Folder != "" && PlayerName != "")
            {
                log.Info("Initializing setup");
                ResetStatus();
                appConfig.DropBoxFolder = this.DropBoxFolder;
                appConfig.CIV5Folder = this.CIV5Folder;
                appConfig.PlayerName = this.PlayerName;
                appConfig.EverythingSetup = true;
                ReadGameConfig();
                SaveAppConfig();
                log.Info("New setup finished");
            }
            else
            {
                log.Info("Settings not filled correctly.");
                VarnUser("Minden mezőt tölts ki!", null);
            }
        }

        public void SaveExtras()
        {
            appConfig.StartOnUsesTurn = this.startOnUsesTurn;
            appConfig.StartWithoutPrompt = this.startWithoutPrompt;
            appConfig.CivExePath = this.civExePath;
            appConfig.UsersTurnMusic = this.usersTurnMusic;
            SaveAppConfig();
        }

        protected void ReadAppConfig()
        {
            log.Info("Reading Appconfig file");
            if (!appConfigPath.Exists)
            {
                log.Info("Appconfig file does not exist! Creating new one");
                SaveAppConfig();
            }
            else
            {
                try
                {
                    IFormatter formatter = new BinaryFormatter();
                    Stream stream = new FileStream(appConfigPath.ToString(),
                                              FileMode.Open,
                                              FileAccess.Read,
                                              FileShare.Read);
                    appConfig = (AppConfiguaraion)formatter.Deserialize(stream);
                    stateApi.appConfig = this.appConfig;
                    stream.Close();
                    log.Info("Appconfig read successfully");
                    //appWindow.Dispatcher.Invoke(() => appWindow.refreshPlayersComboBox());

                }
                catch (Exception)
                {
                    SaveAppConfig();
                    log.Error("Cannot deserialize AppConfig! - Created new instead");
                }
            }
            CIV5Folder = appConfig.CIV5Folder;
            DropBoxFolder = appConfig.DropBoxFolder;
            PlayerName = appConfig.PlayerName;
            CivExePath = appConfig.CivExePath;
            StartWithoutPrompt = appConfig.StartWithoutPrompt;
            StartOnUsesTurn = appConfig.StartOnUsesTurn;
            UsersTurnMusic = appConfig.UsersTurnMusic;
            ReadGameConfig();
        }

        protected void ReadGameConfig()
        {
            try
            {
                if (appConfig.DropBoxFolder != "")
                {
                    log.Info("Reading config file from dropbox");
                    string[] lines = File.ReadAllLines(appConfig.DropBoxFolder + "/CIV/civ5.config");
                    gameConfig.RawData = lines.Select(l => l.Split('=')).ToDictionary(a => a[0], a => a[1]);
                    log.Info("Config file found and valid");
                    stateApi.Initialize();
                    if (gameConfig.fromMailAddress != null)
                    {
                        mailSender = new MailSender(gameConfig.fromMailAddress, gameConfig.fromMailPassword, "A te köröd jön!");
                    }
                    FileInfo fi = new FileInfo(appConfig.DropBoxFolder + "/CIV/civ5.config");
                    if (fi.Exists)
                    {
                        string statFilePath = appConfig.DropBoxFolder + "/CIV/civ5.stats";
                        FileInfo fiNew = new FileInfo(statFilePath);
                        if (!fiNew.Exists)
                        {
                            fiNew.Create().Close();
                        }
                        try
                        {
                            statistics = new Statistics(statFilePath);
                        }
                        catch (Exception e)
                        {
                            log.Error("Could not read statistics!", e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                VarnUser("A dropbox mappa nem helyes, vagy hiányzik a config fájl!", e);
            }
        }

        public bool LoadGameConfigData()
        {
            if (this.DropBoxFolder == "" || this.DropBoxFolder == null)
            {
                VarnUser("Nem adtad meg a dropbox elérési útját!", null);
                return false;
            }
            else
            {
                try
                {
                    log.Info("Loading config file for player selection");
                    string[] lines = File.ReadAllLines(this.DropBoxFolder + "/CIV/civ5.config");
                    gameConfig.RawData = lines.Select(l => l.Split('=')).ToDictionary(a => a[0], a => a[1]);
                    log.Info("Config file found and valid");
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Players"));
                    return true;
                }
                catch (Exception e)
                {
                    VarnUser("Nincs konfig fájl a megadott dropbox mappában, vagy nem helyes!", e);
                    return false;
                }
            }
        }

        protected void SaveAppConfig()
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(appConfigPath.ToString(),
                                     FileMode.Create,
                                     FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, appConfig);
            stream.Close();
            log.Info("Appconfig saved");
        }
        private void currentPlayerChangedHandler()
        {
            log.Info("Handling current player changed");
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CurrentPlayer"));
            appWindow.tray_icon.Text = CurrentPlayer + " köre van";
        }
        private void UsersTurnHandler()
        {
            log.Info("Handling user's turn");
            SystemSounds.Asterisk.Play();
            FileInfo soundFile = new FileInfo(Environment.CurrentDirectory + "/Resources/notification.wav");
            if (soundPlayer != null && soundFile.Exists && appConfig.UsersTurnMusic)
            {
                soundPlayer.Play();
            }
            if (appConfig.StartWithoutPrompt)
            {
                log.Info("Opening game exe");
                System.Diagnostics.Process.Start(CivExePath);
            }
            appWindow.Dispatcher.Invoke(() =>
            {
                appWindow.Show();
                appWindow.GetToForeground();
                appWindow.StatusGrid.Background = System.Windows.Media.Brushes.ForestGreen;
                System.Windows.Forms.MessageBox.Show(new Form { TopMost = true }, "Te lépsz!");
                stateApi.lastSetupTime = DateTime.Now;
            });
            if (appConfig.StartOnUsesTurn && !appConfig.StartWithoutPrompt)
            {
                Form myForm = new Form { TopMost = true, TopLevel = true };
                DialogResult dialogResult = MessageBox.Show(myForm, "Indíthatom a játékot?", "Játék indítása", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    log.Info("Opening game exe");
                    System.Diagnostics.Process.Start(CivExePath);
                }
            }
            try
            {
                LastStatisticRow = statistics.ReadStatistics(true);
            }
            catch (Exception e)
            {
                log.Error("Could not read statistics data!", e);
            }
        }
        private void ResetStatus()
        {
            appWindow.Dispatcher.Invoke(() =>
            appWindow.StatusGrid.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFE5E5E5")));
        }
        private void UserPassedHandler()
        {
            log.Info("Handling user passed");
            string email = gameConfig.gameConfigRows.Where(x => x.Player.Equals(stateApi.CurrentPlayer)).First().Email;
            if (!mailSender.sendMail(new System.Net.Mail.MailAddress(email), "A te köröd jön!"))
            {
                VarnUser("Couldn't send email to: " + stateApi.CurrentPlayer + ", with email: " + email, null);
            }
            try
            {
                LastStatisticRow = statistics.WriteStatLine(appConfig.PlayerName);
            }
            catch (Exception e)
            {

                log.Error("Could not log statistics line!", e);
            }
            ResetStatus();
        }
        private void VarnUser(string s, Exception e)
        {
            if (e != null) log.Error(s, e);
            else log.Warn(s);
            System.Windows.Forms.MessageBox.Show(new Form { TopMost = true }, s);
        }
    }
}
