using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MelBox2_2
{
    //Infos zu AT-Befehlen: https://www.smssolutions.net/tutorials/gsm/receivesmsat/
    // Falcon SAMBA 75 GSM Modem

    //AT-Befehle-Tutorial: https://www.developershome.com/sms/checkCommandSupport3.asp
    //Befehl    getestet    Funktion
    //AT        ok          Erreichbarkeit Modem
    //AT+CSQ    ok          Antwort: Signalqualität, BitErrorRate

    partial class MainWindow : Window
    {
        #region Felder
        private static int _GsmSignalQuality = 0;
        public static int GsmSignalQuality
        {
            get
            {
                return _GsmSignalQuality;
            }
            set
            {
                _GsmSignalQuality = value;
                NotifyStaticPropertyChanged(nameof(GsmSignalQuality));
            }
        }

        private static bool _GsmComPortIsOpen = false;
        public static bool GsmComPortIsOpen 
        { 
            get
            {
                return _GsmComPortIsOpen;
            }
            set 
            {
                _GsmComPortIsOpen = value;
                NotifyStaticPropertyChanged(nameof(GsmComPortIsOpen)); 
            } 
        }
        private static bool _GsmCommunicationIsGood = false;
        public static bool GsmCommunicationIsGood
        {
            get
            {
                return _GsmCommunicationIsGood;
            }
            set
            {
                _GsmCommunicationIsGood = value;
                NotifyStaticPropertyChanged();
            }
        }
        private static bool _GsmTextModeIsSet = false;
        public static bool GsmTextModeIsSet
        {
            get
            {
                return _GsmTextModeIsSet;
            }
            set
            {
                _GsmTextModeIsSet = value;
                NotifyStaticPropertyChanged();
            }
        }
        private static bool _GsmCharactersetIsSet = false;
        public static bool GsmCharactersetIsSet
        {
            get
            {
                return _GsmCharactersetIsSet;
            }
            set
            {
                _GsmCharactersetIsSet = value;
                NotifyStaticPropertyChanged();
            }
        }
        private static bool _GsmMemoryIsSet = false;
        public static bool GsmMemoryIsSet
        {
            get
            {
                return _GsmMemoryIsSet;
            }
            set
            {
                _GsmMemoryIsSet = value;
                NotifyStaticPropertyChanged();
            }
        }

        #endregion

        private void Gsm_SignalQuality_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //Gsm gsm = new Gsm();
            
            MainWindow.GsmSignalQuality = gsm.GetSignalQualityPercent();
        }

        private void Gsm_AvailableComPorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Gsm_AvailableComPorts.SelectedValue.ToString().Length > 3)
            {
                Gsm.PortName = Gsm_AvailableComPorts.SelectedValue.ToString();
            }
        }

        private void Gsm_AvailableBaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedBaudrate = Gsm_AvailableBaudRate.Text;

            if (int.TryParse(selectedBaudrate, out int baudrate))
            {
                if (baudrate > 0)
                {
                    Gsm.BaudRate = baudrate;
                }
            }
        }

        private void Gsm_Button_RecieveSmsTest_Click(object sender, RoutedEventArgs e)
        {   
            MessageCollection mc = gsm.ReadSms("ALL");

            if (mc == null)
            {
                MessageBox.Show("Es konnte keine Nachricht gelesen werden.", "Keine SMS gefunden.");
            }
            else
            {
                MessageBox.Show("Index:\t" + mc.LastOrDefault().Index + "\r\nStatus:\t" + mc.LastOrDefault().Status + "\r\nNummer:\t" + mc.LastOrDefault().Cellphone + "\r\n\r\nInhalt:\t" + mc.LastOrDefault().Content, "Gelesene SMS-Nachricht");
            }
        }

        private void Gsm_Button_PortClose_Click(object sender, RoutedEventArgs e)
        {
            gsm.ClosePort();
        }
        
        private void Gsm_Button_SendTestSms_Click(object sender, RoutedEventArgs e)
        {
            gsm.SendSMS(4915142265412, "SMSAbruf");
        }

        private void Gsm_Button_RecieveDummySms_Click(object sender, RoutedEventArgs e)
        {
            ulong celphone = HelperClass.ConvertStringToPhonenumber(Gsm_TextBox_DummySmsCellphone.Text);

            if ( celphone == 0 ) 
            {
                MessageBox.Show(Gsm_TextBox_DummySmsCellphone.Text + "\r\nDie angegebene Telefonnummer ist ungültig.");
                return;
            }

            if ( Gsm_TextBox_DummySmsContent.Text.Length < 3)
            {
                MessageBox.Show(Gsm_TextBox_DummySmsContent.Text + "\r\nDer Inhalt der Nachricht muss mehr als drei Zeichen haben.");
                return;
            }

            string MessageContent = Gsm_TextBox_DummySmsContent.Text;

            Message message = new Message();
            message.Cellphone = celphone;
            message.Content = MessageContent;
            message.Status = "REC UNREAD";
            message.SentTime = Sql.ConvertToUnixTime(DateTime.Now);
            message.Type = (ushort) MessageType.RecievedFromUnknown;
            message.CustomerKeyWord = Gsm.GetKeyWords(MessageContent);

            MessageCollection mc = new MessageCollection();
            mc.Add(message);

            ProcessRecievedMessages(mc);
            GetUnknownPersons();
            Timer_LastMessages = sql.GetLastMessagesForShow();

            MessageBox.Show("Dummy-Nachricht erzeugt:\r\nvon +" + message.Cellphone + "\r\n"  + message.Content);
        }

    }

    public class Gsm
    {

        #region Felder
        public static string PortName { get; set; } = "COM3";
        public static int BaudRate { get; set; } = 9600; //38400;

        internal static SerialPort serialPort;

        private AutoResetEvent ReceiveNow { get; } = new AutoResetEvent(false);


        #endregion

        #region GSM-Verbindung
        /// <summary>
        /// Prüft, ob der COM-Port offen ist, öffnet ihn ggf., prüft die Verbindung und setzt den Textmodus im Modem.
        /// </summary>
        /// <param name="portName"></param>
        /// <returns></returns>
        private void OpenPort(string portName)
        {
            #region COM-Port öffnen
            if (serialPort != null && serialPort.IsOpen)
            {
                return;
            }

            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            if (ports.Length == 0)
            {
                Log.Write(Log.Type.SMS, string.Format("Es wurden keine seriellen Ports auf diesem Rechner gefunden."));
                return;
            }
            else if (!ports.Contains(portName))
            {
                Log.Write(Log.Type.SMS, string.Format("Der Port {0} wurde nicht gefunden.", portName));
                return;
            }

            SerialPort port = new SerialPort
            {
                PortName = portName,
                BaudRate = BaudRate,
                DataBits = 8,
                StopBits = StopBits.One,
                Parity = Parity.None,
                ReadTimeout = 300,
                WriteTimeout = 500,
                Encoding = Encoding.GetEncoding("iso-8859-1"),
                DtrEnable = true,
                RtsEnable = true
            };

            port.DataReceived += new SerialDataReceivedEventHandler(Port_DataReceived);

            try
            {
                port.Open();
            }
            catch (System.IO.IOException )
            {
                Log.Write(Log.Type.SMS, string.Format("Ein an das System angeschlossenes Gerät funktioniert nicht."));
            }
            catch (UnauthorizedAccessException ex_uaae)
            {
                Log.Write(Log.Type.SMS, string.Format("Der Zugriff auf Port {0} wurde verweigert. {1}", portName, ex_uaae.Message));
            }
            //catch (Exception ex)
            //{
            //    Log.Write(Log.Type.SMS, string.Format("Der Port {0} konnte nicht geöffnet werden: {1}; {2}", portName, ex.GetType(), ex.Message));
            //}

            if ( port == null || !port.IsOpen)
            {
                MainWindow.GsmComPortIsOpen = false;
                return;
            }
            else
            {
                serialPort = port;
                MainWindow.GsmComPortIsOpen = true;
            }
            #endregion

            #region Modem-Setup
            string result = ExecCommand("AT", 300, "No phone connected at " + PortName + ".");

            if (result.EndsWith("\r\nOK\r\n", StringComparison.OrdinalIgnoreCase))
            {
                MainWindow.GsmCommunicationIsGood = true;
            }
            else
            {
                MainWindow.GsmCommunicationIsGood = false;
            }

            // Use message format "Text mode"
            result = ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
            if (result.EndsWith("\r\nOK\r\n"))
            {
                MainWindow.GsmTextModeIsSet = true;
            }
            else
            {
                MainWindow.GsmTextModeIsSet = false;
            }

            // Use character set. Samba75: "GSM; "UCS2"
            result = ExecCommand("AT+CSCS=\"GSM\"", 300, "Failed to set character set.");
            if (result.EndsWith("\r\nOK\r\n"))
            {
                MainWindow.GsmCharactersetIsSet = true;
            }
            else
            {
                MainWindow.GsmCharactersetIsSet = false;
            }

            // Select SIM storage (SM) , Phonememory (ME), MT = SM + ME
            result = ExecCommand("AT+CPMS=\"MT\"", 300, "Failed to select message storage.");

            if (result.EndsWith("\r\nOK\r\n", StringComparison.OrdinalIgnoreCase))
            {
                MainWindow.GsmMemoryIsSet = true;
            }
            else
            {
                result = ExecCommand("AT+CPMS=\"SM\"", 300, "Failed to select message storage.");
                if (result.EndsWith("\r\nOK\r\n", StringComparison.OrdinalIgnoreCase))
                {
                    MainWindow.GsmMemoryIsSet = true;
                }
                else
                {
                    MainWindow.GsmMemoryIsSet = false;
                }
            }
            #endregion
        }

        public void ClosePort()
        {
            if (serialPort != null)
            {
                serialPort.DataReceived -= new SerialDataReceivedEventHandler(Port_DataReceived);
                serialPort.Close();
                serialPort = null;

                MainWindow.GsmComPortIsOpen = false;
                MainWindow.GsmCommunicationIsGood = false;
                MainWindow.GsmTextModeIsSet = false;
                MainWindow.GsmCharactersetIsSet = false;
                MainWindow.GsmMemoryIsSet = false;
                MainWindow.GsmSignalQuality = 0;
            }           
        }

        void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars)
                ReceiveNow.Set();
        }

        private string ExecCommand(string command, int responseTimeout, string errorMessage)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                OpenPort(PortName);
            }
                
            if(serialPort == null || !serialPort.IsOpen)
            {
                Log.Write(Log.Type.SMS, "ExecCommand(): Serieller Port " + PortName + " nicht bereit.");
                return errorMessage;
            }

            serialPort.DiscardOutBuffer();
            serialPort.DiscardInBuffer();
            _ = ReceiveNow.Reset();
            serialPort.Write(command + "\r");

            string input = ReadResponse(responseTimeout);
            Log.Text(Log.Type.SMS, input + "##" + input.EndsWith("\r\nOK\r\n") );

            if ((input.Length == 0) || !input.EndsWith("\r\nOK\r\n")  )
            {
                MainWindow.StatusBarText = "ExecCommand(): Fehler bei AT-Command  Antwort: >" + input +"< " + errorMessage;
                Log.Write(Log.Type.SMS, MainWindow.StatusBarText);
            }
            return input;

        }

        private string ReadResponse(int timeout)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                OpenPort(PortName);
            }

            if (serialPort == null || !serialPort.IsOpen)
            {
                Log.Write(Log.Type.SMS, "ReadResponse(): Serieller Port " + PortName + " nicht bereit.");
                return string.Empty;
            }

            string buffer = string.Empty;
            string t;

            try
            {
                do
                {

                    t = string.Empty;
                    if (ReceiveNow.WaitOne(timeout, false))
                    {
                        t = serialPort.ReadExisting();
                        buffer += t;
                    }
                    else
                    {
                        if (buffer.Length > 0)
                        {
                            Log.Write(Log.Type.SMS, "Empfangene SMS war nicht komplett.");
                        }
                        else
                        {
                            Log.Write(Log.Type.SMS, "Empfangene SMS enthielt keine Daten.");
                        }

                        return buffer;
                    }
                }
                while (!buffer.EndsWith("\r\nOK\r\n") && !buffer.EndsWith("\r\nERROR\r\n") && !buffer.EndsWith("=1\r\n"));

                //MessageBox.Show(buffer, "BUFFER");
            }
            catch (InvalidOperationException ex)
            {
                string msg = "ReadResponse(): " + ex.GetType().FullName + "; " + ex.Message + ex.InnerException + " " + ex.StackTrace;
                MainWindow.StatusBarText = msg;
                Log.Write(Log.Type.SMS, msg);
            }

            return buffer;
        }

        ///// <summary>
        ///// RESERVE / BAUSTELLE
        ///// Bisher musste ich keinen PIN eingeben, da SIM-Karte ohne PIN-Sperre 
        ///// </summary>
        ///// <param name="PIN"></param>
        //private void SetPinCode(string PIN)
        //{
        //    //https://www.smssolutions.net/tutorials/gsm/sendsmsat/
        //    //AT-Commands_Samba75 Manual Seite 72ff.

        //    string result = ExecCommand("AT+CPIN?", 300, "Failed to set PIN.");
        //    if (result.EndsWith("\r\nOK\r\n")) return;
        //    {
        //        ExecCommand("AT+CPIN=\"" + PIN + "\"", 300, "Failed to set PIN.");
        //        Syste.Threading.Thread.Sleep(5000);
        //    }
        //}

        public static MessageCollection ParseMessages(string input)
        {
            if (input.Length == 0) return null;

           // Log.Text(Log.Type.Internal, input+"#################");

            MessageCollection messages = new MessageCollection();
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex("\\+CMGL: (\\d+),\"(.+)\",\"(.+)\",(.*),\"(.+)\"\\r\\n(.+)"); // "\\+CMGL: (\\d+),\"(.+)\",\"(.+)\",(.*),\"(.+)\"\n(.+)\n\n\""
            System.Text.RegularExpressions.Match m = r.Match(input);

            while (m.Success)
            {
                //string gr0 = m.Groups[0].Value; // <alles>
                string gr1 = m.Groups[1].Value; //6
                string gr2 = m.Groups[2].Value; //STO SENT
                string gr3 = m.Groups[3].Value; //+49123456789
                //string gr4 = m.Groups[4].Value; // -LEER-
                string gr5 = m.Groups[5].Value.Replace(',', ' '); //18/09/28,11:05:51 + 105
                string gr6 = m.Groups[6].Value; //Nachricht
                string gr7 = m.Groups[7].Value; //Nachricht (notwendig?)
                
                //MessageBox.Show(string.Format("0:{0}\r\n1:{1}\r\n2:{2}\r\n3:{3}\r\n4:{4}\r\n5:{5}\r\n6:{6}\r\n7:{7}\r\n", gr0, gr1, gr2, gr3, gr4, gr5, gr6, gr7), "Rohdaten");

                int.TryParse(gr1, out int smsId);
                DateTime.TryParse(gr5, out DateTime time);

                //MessageBox.Show("Zeit interpretiert: " + time.ToString("dd.MM.yyyy HH:mm:ss"));

                //Message Status zu MessageType
                MessageType type;
                switch (gr2)
                {
                    case "REC UNREAD":
                    case "REC READ":
                        type = MessageType.RecievedFromSms;
                        break;
                    case "STO SENT":
                        type = MessageType.SentToSms;
                        break;
                    default:
                        type = MessageType.RecievedFromSms;
                        break;
                }
                
                //Nachricht erstellen
                Message msg = new Message
                {
                    Index = smsId,
                    Status = gr2,
                    //Alphabet -leer-
                    Type = (ushort)type,
                    Cellphone = HelperClass.ConvertStringToPhonenumber(gr3),
                    SentTime = Sql.ConvertToUnixTime(time),
                    CustomerKeyWord = GetKeyWords(gr6),
                    Content = gr6 + gr7
                };

                messages.Add(msg);
                
                m = m.NextMatch();
            }

            return messages;
        }
        #endregion

        #region Senden und Empfangen

        public int GetSignalQualityPercent()
        {

            string signalQuality = ExecCommand("AT+CSQ", 300, "GSM-Signalqualität nicht verfügbar.");
            if (signalQuality.Length < 2)
            {
                return 0;
            }
            else
            {
                Regex rS = new Regex(@"[0-9]+,");
                Match m = rS.Match(signalQuality);
                if (!m.Success) return 0;

                int.TryParse(m.Value.Trim(','), out int qual);

                return (int)(qual / 0.3);
            }

        }

        /// <summary>
        /// Liest SMS aus dem GSM-Modem
        /// </summary>
        /// <param name="smsStatus">Status der zu lesenden SMSen z.B. "REC UNREAD", "ALL"</param>
        /// <returns></returns>
        internal MessageCollection ReadSms(string smsStatus = "REC UNREAD")
        {
            MessageCollection messages = new MessageCollection();

            MainWindow.GsmSignalQuality = GetSignalQualityPercent();

            // Read the messages
            string input = ExecCommand("AT+CMGL=\"" + smsStatus + "\"", 5000, "Failed to read the messages.");
            // Set up the phone/gsm and read the messages
            if (input.Length > 0)
            {
                messages = ParseMessages(input);
            }

            return messages;
        }

        //BAUSTELLE:
        internal void SendSMS(ulong phoneNumber, string message)
        {
            MainWindow.GsmSignalQuality = GetSignalQualityPercent();

            //https://www.smssolutions.net/tutorials/gsm/sendsmsat/


            //ExecCommand("AT+CPIN=\"0000\"", 300, "Failed to set PIN.");
            // Use character set "HEX"
            ExecCommand("AT+CSCS=\"GSM\"", 300, "Failed to set character set to GSM.");
            // Select SIM storage (SM) , Phonememory (ME)
            //ExecCommand("AT+CSMP=1,167,0,8", 300, "Failed to set DCS (Data Coding Scheme) for Unicode messages (0x08).");

            string result =  ExecCommand("AT+CMGS=\"+" + phoneNumber + "\"\r", 300, "Faild to Dial.");
            Log.Write(Log.Type.SMS, "SendSMS()" + result);

            //if (result.EndsWith(">"))
            {
                //System.Threading.Thread.Sleep(1000);


                string ctrlz = "\u001a";

                result = ExecCommand(message + ctrlz, 500, "Faild to send Message.");
                Log.Write(Log.Type.SMS, "SendSMS()" + result);
            }
        }

        /// <summary>
        /// Liest die Schlüsselworte aus, die am Anfang einer SMS-Nachrist stehen.
        /// </summary>
        /// <param name="MessageContent"></param>
        /// <returns></returns>
        internal static string GetKeyWords(string MessageContent)
        {
            int IndexOfDash = MessageContent.IndexOf('-');
            int IndexOfComma = MessageContent.IndexOf(',');
            int IndexOfContent = 0;

            if (IndexOfDash > 0 && IndexOfDash < 20)
            {
                if (IndexOfDash < IndexOfComma || IndexOfComma < 1)
                {
                    IndexOfContent = IndexOfDash;
                }
            }

            if (IndexOfComma > 0 && IndexOfComma < 20)
            {
                if (IndexOfComma < IndexOfDash || IndexOfDash < 1)
                {
                    IndexOfContent = IndexOfComma;
                }
            }

            return MessageContent.Substring(0, IndexOfContent);
        }

        #endregion


        #region Spezielle SMS

        /// <summary>
        /// Testet die Funktion der Meldelinie:
        /// Beginnt eine Nachricht mit dem Signalwort, wird sie an den Absender per SMS zurückgeschickt.
        /// </summary>
        /// <param name="message">zu prüfende Nachricht</param>
        /// <returns>message war Test-SMS</returns>
        internal bool IsTestSmsRoute(Message message)
        {
            string smsSignalWord = "SMSAbruf";

            if (message.Content.StartsWith(smsSignalWord) && message.Cellphone > 0)
            {
                SendSMS(message.Cellphone, DateTime.Now + " " + message.Content);
                return true;
            }

            return false;
        }
        #endregion


    }
}
