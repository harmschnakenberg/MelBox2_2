using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace MelBox2_2
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Sql sql;
        private static Gsm gsm;

        #region Events
        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        private static void NotifyStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Anzeigen
        private static string _StatusBarText;

        public static string StatusBarText
        {
            get { return _StatusBarText; }
            set
            {
                _StatusBarText = value;
                NotifyStaticPropertyChanged();
            }
        }

        #endregion


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            sql = new Sql();
            gsm = new Gsm();

            Log.Write(Log.Type.General, "Programm gestartet.");

            //Tab Tabellen
            Tab_ComboBoxTables.ItemsSource = sql.GetAllTableNames(new string[] { "sqlite_sequence" });

            //Tab Personen
            Mast_ComboBox_Company.ItemsSource = sql.GetListOfCompanies();
            GetUnknownPersons();

            // Tab GSM-Modem
            Gsm_AvailableComPorts.ItemsSource = System.IO.Ports.SerialPort.GetPortNames().ToList();
            if (Gsm_AvailableComPorts.Items.Contains(Gsm.PortName))
            {
                Gsm_AvailableComPorts.SelectedValue = Gsm.PortName;
            }
            else if (Gsm_AvailableComPorts.Items.Count > 0)
            {
                Gsm_AvailableComPorts.SelectedValue = Gsm_AvailableComPorts.Items[0];
            }

            Gsm_AvailableBaudRate.SelectedIndex = Gsm_AvailableBaudRate.Items.Count - 1;

            //Gesperrte Nachrichten
            BlockedMessages = sql.GetBlockedMessages();

            // Tab Timer
            StartTimer();
            CountDownEvent += HandleCountdownEvent;
            CountDownEvent += DatabaseBackupTrigger;
            CountDownEvent += MelBox2HeartBeat;
            
            Timer_CurrentShifts = sql.GetCurrentShifts();
            Timer_LastMessages = sql.GetLastMessagesForShow();
                         
        }

        /// <summary>
        /// Beim Schließen des Fensters.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            gsm.ClosePort();
        }

    }
}
