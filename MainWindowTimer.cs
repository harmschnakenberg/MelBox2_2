using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MelBox2_2
{
    partial class MainWindow : Window
    {
        #region Felder
        public static int PollingIntervall { get; set; } = 300;

        private static int _Counter = PollingIntervall;

        public static int Counter
        {
            get { return _Counter; }
            set
            {
                _Counter = value;
                NotifyStaticPropertyChanged(nameof(Counter));
            }
        }

        internal static DateTime LastHeartBeat { get; set; }

        DispatcherTimer _myTimer;

        public void StartTimer()
        {
            _myTimer = new DispatcherTimer();
            _myTimer.Tick += MyTimerTick;
            _myTimer.Interval = new TimeSpan(0, 0, 0, 1);
            _myTimer.Start();
        }

        private void MyTimerTick(object sender, EventArgs e)
        {
            Counter--;
            if (Counter <= 0)
            {
                Counter = PollingIntervall;
                RaiseCountDownEvent();
            }
        }

        private static ObservableCollection<NightShift> _Timer_CurrentShifts;
        public static ObservableCollection<NightShift> Timer_CurrentShifts
        {
            get { return _Timer_CurrentShifts; }
            set
            {
                _Timer_CurrentShifts = value;
                NotifyStaticPropertyChanged(nameof(Timer_CurrentShifts));
            }
        }

        private static DataTable _Timer_LastMessages;

        public static DataTable Timer_LastMessages
        {
            get { return _Timer_LastMessages; }
            set 
            { 
                _Timer_LastMessages = value;
                NotifyStaticPropertyChanged(nameof(Timer_LastMessages));
            }
        }


        // Declare the delegate (if using non-generic pattern).
        public delegate void CountDownEventHandler(object sender);

        // Declare the event.
        public event CountDownEventHandler CountDownEvent;

        #endregion

        // Wrap the event in a protected virtual method
        // to enable derived classes to raise the event.
        protected virtual void RaiseCountDownEvent()
        {
            // Raise the event in a thread-safe manner using the ?. operator.
            CountDownEvent?.Invoke(this);
        }

        /// <summary>
        /// Legt Aktionen
        /// </summary>
        /// <param name="sender"></param>
        async void HandleCountdownEvent(object sender)
        {           
            MessageCollection messages = new MessageCollection();

            //SMS lesen
            MessageCollection Sms = gsm.ReadSms("REC UNREAD"); //"ALL", "REC UNREAD"
            if (Sms != null)
            {
                messages.AddRange(Sms);
            }

            //Emails lesen
            MessageCollection Emails = await ReadEmailsAsync();
            messages.AddRange( Emails );

            //Diensthabende ermitteln:
            Timer_CurrentShifts = sql.GetCurrentShifts();

            ProcessRecievedMessages(messages);

            GetUnknownPersons();

            gsm.ClosePort();
                    
            StatusBarText = "neue Nachrichten wurden zuletzt verarbeitet um " + DateTime.Now.ToShortTimeString();
        }

        /// <summary>
        /// Stößt ein Backup der SQLite-Datenbank an, wenn Voraussetzungen erfüllt sind.
        /// </summary>
        /// <param name="sender"></param>
        void DatabaseBackupTrigger(object sender)
        {
            // Backup nur, wenn Weitersendung nicht kritisch ist (Tagsüber, in der Woche, wenn kein Feiertag ist):
            bool isDayTime = (DateTime.Now.Hour > 8);            
            if (!isDayTime) return;
            bool isWorkDay = (DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday);
            if (!isWorkDay) return;
            bool isHolyDay = HelperClass.Feiertage(DateTime.Now).Contains(DateTime.Now.Date);
            if (isHolyDay) return;
           
            var t = System.Threading.Tasks.Task.Run(() => Sql.BackupDatabase());
            t.Wait();
        }    
        
        /// <summary>
        /// Sendet ein Lebensbit zu bestimmtem Empfänger, wenn Voraussetzungen erfüllt sind.
        /// </summary>
        /// <param name="sender"></param>
        void MelBox2HeartBeat(object sender)
        {
            //Wenn größer 0, dann ist das zweite Datum später als das erste
            //Wenn letztes Mal + 1 Tag später ist als jetzt
            if (LastHeartBeat.AddDays(1).CompareTo(DateTime.Now) > 0) return;

           //  gsm.SendSMS(4916095285304, "MelBox2 OK");

            System.Net.Mail.MailAddressCollection addresses = new System.Net.Mail.MailAddressCollection();
            addresses.Add(new System.Net.Mail.MailAddress("harm.schnakenberg@kreutztraeger.de", "HarmLifeBeatReciever"));
            MainWindow.SendEmail(addresses, "MelBox2 OK", "MelBox2 OK - gesendet um " + DateTime.Now.ToLongTimeString() );

            //Setze LastHeartBeat
            LastHeartBeat = DateTime.Now.Date.AddHours(8); // 8 Uhr morgens
            StatusBarText = "Lebenszeichen gesendet um " + DateTime.Now.ToShortTimeString();
        }
        
        /// <summary>
        /// Erzeugt Einen EIntrag in der Nachrichten-Datenbank, Behandelt SMSAbruf-Test, sendet die Nachricht weiter an die aktuell eingeteilte Schicht.
        /// </summary>
        /// <param name="messages"></param>
        private void ProcessRecievedMessages(MessageCollection messages)
        {
            //Gsm gsm = new Gsm();

            foreach (Message message in messages)
            {
                //Nachrichten in Datenbank schreiben
                uint newMsgId = sql.CreateMessageEntry(message);

                //Ist message eine Testnachricht?
                gsm.IsTestSmsRoute(message);

                if (!sql.IsMessageBlocked(message) && newMsgId != 0)
                {
                    System.Net.Mail.MailAddressCollection mailAddresses = new System.Net.Mail.MailAddressCollection();                   
                    List<uint> sendSmsRecieverIds = new List<uint>();
                    List<uint> sendMailRecieverIds = new List<uint>();

                    //Nachrichten an Bereitschaft senden
                    foreach (NightShift shift in sql.GetCurrentShifts() )
                    {
                        if (shift.SendViaSMS)
                        {
                            StatusBarText = "Sende SMS an +" + shift.SendToCellphone;
                            if (shift.SendToCellphone > 0)
                            {
                                sendSmsRecieverIds.Add(sql.GetIdFromEntry("Persons", "Cellphone", shift.SendToCellphone.ToString()));
                                gsm.SendSMS(shift.SendToCellphone, message.Content);

                                //Status gesendet in die Datenbank schreiben.
                                sql.UpdateSentMessageEntry(newMsgId, MessageType.SentToSms, sendSmsRecieverIds);
                            }
                        }

                        if (shift.SendViaEmail)
                        {
                            StatusBarText = "Sende Email...";
                            sendMailRecieverIds.Add(sql.GetIdFromEntry("Persons", "Name", shift.GuardName));
                            if (HelperClass.IsValidEmailAddress(shift.SendToEmail))
                            {
                                mailAddresses.Add(new System.Net.Mail.MailAddress(shift.SendToEmail, shift.GuardName));
                                SendEmail(mailAddresses, message.Content, message.Content);

                                //Status gesendet in die Datenbank schreiben.
                                sql.UpdateSentMessageEntry(newMsgId, MessageType.SentToEmail, sendMailRecieverIds);
                            }
                        }
                    }                                    
                }
            }

            //Neue Nachrichten anzeigen
            Timer_LastMessages = sql.GetLastMessagesForShow();
        }

        private void Timer_Button_BlockMessage_Click(object sender, RoutedEventArgs e)
        {
            if (Timer_DataGrid_LastMessages.SelectedItems == null) return;

            DataRowView selectedRow = (DataRowView)Timer_DataGrid_LastMessages.SelectedItems[0];

            string selectedContent = selectedRow.Row.Field<string>("Inhalt");

            MessageBoxResult result = MessageBox.Show("Wirklich die Nachricht mit diesem Inhalt sperren?\r\n\r\n" + selectedContent, "Wirklich sperren?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            Sql sql = new Sql();
            sql.CreateBlockedMessage(selectedContent);
        }

        private void Timer_Label_CurrentShift_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Timer_CurrentShifts = sql.GetCurrentShifts();
            
            Timer_LastMessages = sql.GetLastMessagesForShow();
        }

    }

}
