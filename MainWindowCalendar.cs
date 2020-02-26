using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MelBox2_2
{
    public partial class MainWindow : Window
    {

        #region Felder Kalender
        public static int NightShiftStartHour { get; set; } = 17; //Stunde Bereitschaftsbeginn am Wochentag.

        public static int NightShiftStartHourFriday { get; set; } = 15; //Stunde Bereitschaftsbeginn am Freitag.

        public static int NightShiftEndHour { get; set; } = 7; //Stunde Bereitschaftsende am Folgetag.

        private static IEnumerable<string> _Cal_AvailablePersonal;
        public static IEnumerable<string> Cal_AvailablePersonal
        {
            get
            {
                return _Cal_AvailablePersonal;
            }

            set
            {
                _Cal_AvailablePersonal = value;
                NotifyStaticPropertyChanged(nameof(Cal_AvailablePersonal));
            }
        }

        private static DateTime _Cal_LastServiceDate;
        public static DateTime Cal_LastServiceDate
        {
            get
            {
                return _Cal_LastServiceDate;
            }

            set
            {
                _Cal_LastServiceDate = value;
                NotifyStaticPropertyChanged(nameof(Cal_LastServiceDate));
            }
        }

        private static ObservableCollection<NightShift> _Cal_ShiftsCollection;
        public static ObservableCollection<NightShift> Cal_ShiftsCollection
        {
            get
            {
                return _Cal_ShiftsCollection;
            }

            set
            {
                _Cal_ShiftsCollection = value;
                NotifyStaticPropertyChanged(nameof(Cal_ShiftsCollection));
            }
        }
        
        private static List<DateTime> _Cal_Holidays;
        public List<DateTime> Cal_Holidays
        {
            get { return _Cal_Holidays; }
            set
            {
                _Cal_Holidays = value;
                NotifyStaticPropertyChanged(nameof(Cal_Holidays));
            }
        }
        #endregion

        private void Cal_TabItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            LoadShiftsToNightShiftClass();
        }

        private void Cal_DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Cal_DataGrid.SelectedItem == null || Cal_DataGrid.SelectedItems.Count < 1)
            {
                Cal_Button_UpdateServiceTimeSpan.IsEnabled = false;
                Cal_Button_UpdateServiceWeek.IsEnabled = false;
                Cal_Button_DeleteServiceTimeSpan.IsEnabled = false;
                Cal_DataGrid.SelectedIndex = 0;
            }
            else
            {
                Cal_Button_UpdateServiceTimeSpan.IsEnabled = true;
                Cal_Button_UpdateServiceWeek.IsEnabled = true;
                Cal_Button_DeleteServiceTimeSpan.IsEnabled = true;

                try
                {
                    NightShift nightShift = (NightShift)Cal_DataGrid.SelectedItems[0];

                    Cal_ComboBox_Personal.SelectedValue = nightShift.GuardName;
                    Cal_TimeFrom.SelectedIndex = nightShift.StartTimeHour;
                    Cal_TimeTo.SelectedIndex = nightShift.EndTimeHour;
                    Cal_SendViaEmail_CheckBox.IsChecked = nightShift.SendViaEmail;
                    Cal_SendViaSMS_CheckBox.IsChecked = nightShift.SendViaSMS;

                    //Auswahl gesperrter Daten in DatePicker erzeugt einen Fehler:
                    DateTime DateFromList = (DateTime)nightShift.StartTime.Date;
                    if (!Cal_DatePicker.BlackoutDates.Contains(DateFromList))
                    {
                        try
                        {
                            Cal_DatePicker.SelectedDate = DateFromList;
                        }
                        catch
                        {
                            //nichts unternehmen
                        }

                        Cal_DatePicker.Text = nightShift.StartTime.ToShortDateString();
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    MessageBox.Show(ex.Message + "\r\n" + Cal_DataGrid.SelectedItems.Count + "\r\n\r\n" + ex.StackTrace);
                }
            }
        }

        private void Cal_StartTimeBeam_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Cal_DataGrid.SelectedItems.Count > 0)
            {
                NightShift nightShift = (NightShift)Cal_DataGrid.SelectedItems[0];
                Cal_TimeFrom.SelectedIndex = nightShift.StartTimeHour;
            }
        }

        private void Cal_EndTimeBeam_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Cal_DataGrid.SelectedItems.Count > 0)
            {
                NightShift nightShift = (NightShift)Cal_DataGrid.SelectedItems[0];
                Cal_TimeTo.SelectedIndex = nightShift.EndTimeHour;
            }
        }

        private void Cal_CheckBox_BlackOutCalenderDays_Checked(object sender, RoutedEventArgs e)
        {
            BlackOutCalenderDays(true);
            LoadShiftsToNightShiftClass();
        }

        private void Cal_CheckBox_BlackOutCalenderDays_Unchecked(object sender, RoutedEventArgs e)
        {
            BlackOutCalenderDays(false);
            LoadShiftsToNightShiftClass();
        }

        /// <summary>
        /// Sperrt die übergebenen Tage im Kalender Cal_DatePicker.
        /// Listet die verfügbaren Bereitschaftsnehmer auf.
        /// Listet die zuletzt erstellten Schichten auf.
        /// </summary>
        /// <param name="dates"></param>
        private void BlackOutCalenderDays(bool blackOutDays)
        {
            Sql sql = new Sql();

            IEnumerable<string> strLastServiceDate = sql.GetListFromColumn("Shifts", "datetime(StartTime, 'unixepoch') AS StartTime ", "1=1 ORDER BY StartTime LIMIT 100");

            if (blackOutDays)
            {
                //DatePicker: Geplante Tage sperren
                foreach (string date in strLastServiceDate)
                {
                    if (DateTime.TryParse(date, out DateTime dateChecked))
                    {
                        CalendarDateRange calendarDateRange = new CalendarDateRange(dateChecked);
                        try
                        {
                            Cal_DatePicker.BlackoutDates.Add(calendarDateRange);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //Log.Write(Log.Type.Internal, "Kalendertage sperren: " + ex.Message);
                            //nichts unternehmen.
                        }
                    }
                }
            }
            else
            {
                //DatePicker: Alle Tage freigeben
                while (Cal_DatePicker.BlackoutDates.Count > 0)
                {
                    Cal_DatePicker.BlackoutDates.Remove(Cal_DatePicker.BlackoutDates.FirstOrDefault());
                }
            }

            //Alle Tage in der Vergangenheit sperren.
            if ( Cal_DatePicker.SelectedDate != null)
            {
                try
                {
                    Cal_DatePicker.BlackoutDates.AddDatesInPast();
                }
                catch (ArgumentOutOfRangeException)
                {
                    //nichts unternehmen
                }
            }

            DateTime.TryParse(strLastServiceDate.LastOrDefault(), out DateTime dtLastServiceDate);
            if (dtLastServiceDate == null) dtLastServiceDate = DateTime.Now;
            Cal_LastServiceDate = dtLastServiceDate.AddDays(1);

            Cal_AvailablePersonal = sql.GetListFromColumn("Persons", "Name", "MessageType IN (8, 32, 40) LIMIT 50");
        }

        /// <summary>
        /// Liest Bereitschaftsdienste aus der Datenbank in die Klasse NightShift 
        /// und übergibt diese als Feld für die Visualisierung.
        /// </summary>
        internal static void LoadShiftsToNightShiftClass()
        {
            ObservableCollection<NightShift> nightShifts = new ObservableCollection<NightShift>();

            Sql sql = new Sql();
            DataTable dt = sql.GetShifts();

            foreach (DataRow row in dt.Rows)
            {
                string aName = row["Name"].ToString();

                int sendType = int.Parse(row["SendType"].ToString());

                NightShift nightShift = new NightShift
                {
                    ShiftsId = uint.Parse(row["ID"].ToString()),
                    EntryTime = DateTime.Parse(row["EntryTime"].ToString()),
                    GuardName = aName,
                    StartTime = DateTime.Parse(row["StartTime"].ToString()),
                    EndTime = DateTime.Parse(row["EndTime"].ToString()),
                    SendViaSMS = (sendType == (int)MessageType.SentToSms) || (sendType == (int)MessageType.SentToEmailAndSMS),
                    SendViaEmail = (sendType == (int)MessageType.SentToEmail) || (sendType == (int)MessageType.SentToEmailAndSMS)
                };

                nightShifts.Add(nightShift);
            }

            MainWindow.Cal_ShiftsCollection = nightShifts;
        }

        private void Cal_Button_DeleteServiceTimeSpan_Click(object sender, RoutedEventArgs e)
        {
            NightShift nightShift = (NightShift)Cal_DataGrid.SelectedItems[0];
            uint selectedId = nightShift.ShiftsId;

            string question = "Wirklich die Schicht " + selectedId + "\r\n\r\nvon\t" + nightShift.StartTime + "\r\nbis\t" + nightShift.EndTime + "\r\n\r\nLÖSCHEN?";
            MessageBoxResult r = MessageBox.Show(question, "Wirklich löschen?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r == MessageBoxResult.Yes)
            {
                int chandedRows = sql.DeleteShift(selectedId);
                if (chandedRows > 0)
                {
                    Log.Write(Log.Type.Calendar, "Schicht [" + selectedId + "] gelöscht.");
                }
                else
                {
                    MessageBox.Show("Die ausgwählte Schicht konnte nicht gelöscht werden.");
                }

                LoadShiftsToNightShiftClass();
            }

            //LoadShiftsToNightShiftClass();
            BlackOutCalenderDays(true);
        }

        private void Cal_Button_UpdateServiceTimeSpan_Click(object sender, RoutedEventArgs e)
        {
            NightShift nightShift = (NightShift)Cal_DataGrid.SelectedItems[0];
            uint selectedId = nightShift.ShiftsId;

            DateTime StartDate = DateTime.Now;
            try
            {
                StartDate = (DateTime)Cal_DatePicker.SelectedDate;
            }
            catch
            {
                //nichts unternehmen
            }

            if (Cal_ComboBox_Personal.SelectedValue == null) return;

            string Personal = Cal_ComboBox_Personal.SelectedValue.ToString();
            int indexFrom = Cal_TimeFrom.SelectedIndex;
            int indexTo = Cal_TimeTo.SelectedIndex;
            DateTime StartTime = StartDate.Date.AddHours(indexFrom);
            DateTime EndTime = StartDate.Date.AddDays(1).AddHours(indexTo);

            //Welches Sendemedium?
            MessageType sendType;
            if ((bool)Cal_SendViaSMS_CheckBox.IsChecked)
            {
                sendType = ((bool)Cal_SendViaEmail_CheckBox.IsChecked) ? MessageType.SentToEmailAndSMS : MessageType.SentToSms;
            }
            else
            {
                sendType = ((bool)Cal_SendViaEmail_CheckBox.IsChecked) ? MessageType.SentToEmail : MessageType.NoCategory;
            }

            Sql sql = new Sql();

            uint personId = sql.GetIdFromEntry("Persons", "Name", Personal);

            sql.UpdateShift(selectedId, personId, StartTime, EndTime, sendType);

            string message = "Geänderte Schicht für \r\n\r\n[" +
                    personId + "] " + Personal +
                    "\r\n\r\nSchicht-Nr.:\t" + selectedId +
                    "\r\nvon\t" + StartTime.ToString("dd.MM.yyyy HH:mm") +
                    "\r\nbis\t" + EndTime.ToString("dd.MM.yyyy HH:mm");

            switch (sendType)
            {
                case MessageType.SentToSms:
                    message += "\r\n\n Benachrichtigungen per SMS.";
                    break;
                case MessageType.SentToEmail:
                    message += "\r\n\n Benachrichtigungen per Email.";
                    break;
                case MessageType.SentToEmailAndSMS:
                    message += "\r\n\n Benachrichtigungen per SMS und Email.";
                    break;
                default:
                    message += "\r\n\n Benachrichtigungsweg nicht festgelegt.";
                    break;
            }

            MessageBox.Show(message, "Geänderte Schicht", MessageBoxButton.OK, MessageBoxImage.Information);
            Log.Write(Log.Type.Calendar, message);

            LoadShiftsToNightShiftClass();
            Timer_CurrentShifts = sql.GetCurrentShifts();
            BlackOutCalenderDays(true);
        }

        private void Cal_Button_UpdateServiceWeek_Click(object sender, RoutedEventArgs e)
        {
            if ( Cal_DataGrid.SelectedItem == null || Cal_ComboBox_Personal.SelectedItem == null) return;

            string person = Cal_ComboBox_Personal.SelectedItem.ToString();

            uint personId = sql.GetIdFromEntry("Persons", "Name", person);

            NightShift nightShift = (NightShift)Cal_DataGrid.SelectedItems[0];

            DateTime selectedDate = nightShift.StartTime.Date;

            DateTime StartDate = selectedDate.AddDays(DayOfWeek.Monday - selectedDate.DayOfWeek).Date;

            DateTime EndDate = StartDate.AddDays(7);

            string msg = "Bereitschaftswoche \r\nvon " + 
                        StartDate.ToShortDateString() + 
                        "\r\nbis " + 
                        EndDate.ToShortDateString() + 
                        "\r\nändern \r\nvon " + 
                        nightShift.GuardName + 
                        "\r\nauf [" + personId +"] " + person + "?";

            MessageBoxResult r = MessageBox.Show(msg, "Bereitschaftswoche ändern?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r == MessageBoxResult.Yes)
            {
                IEnumerable<string> ids = sql.GetListFromColumn("Shifts", "ID", "StartTime BETWEEN " + Sql.ConvertToUnixTime(StartDate) + " AND " + Sql.ConvertToUnixTime(StartDate.AddDays(7)));

                foreach (string strId in ids)
                {
                    if (uint.TryParse(strId, out uint id))
                    {
                        sql.UpdateShift(id, personId, HelperClass.GetMessageType(false, false, (bool)Cal_SendViaEmail_CheckBox.IsChecked, (bool)Cal_SendViaSMS_CheckBox.IsChecked ) );
                    }
                }

                LoadShiftsToNightShiftClass();
                BlackOutCalenderDays(true);
            }
        }

        private void Cal_Button_CreateServiceTimeSpan_Click(object sender, RoutedEventArgs e)
        {
            DateTime StartDate;

            try
            {
                StartDate = (DateTime)Cal_DatePicker.SelectedDate;
            }
            catch (ArgumentOutOfRangeException)
            {
                StartDate = DateTime.Now;
            }

            string Personal = Cal_ComboBox_Personal.SelectedValue.ToString();
            int indexFrom = Cal_TimeFrom.SelectedIndex;
            int indexTo = Cal_TimeTo.SelectedIndex;

            //Welches Sendemedium?
            MessageType sendType;
            if ((bool)Cal_SendViaSMS_CheckBox.IsChecked)
            {
                sendType = ((bool)Cal_SendViaEmail_CheckBox.IsChecked) ? MessageType.SentToEmailAndSMS : MessageType.SentToSms;
            }
            else
            {
                sendType = ((bool)Cal_SendViaEmail_CheckBox.IsChecked) ? MessageType.SentToEmail : MessageType.NoCategory;
            }

            DateTime StartTime = StartDate.Date.AddHours(indexFrom);
            DateTime EndTime = StartDate.Date.AddDays(1).AddHours(indexTo);

            Sql sql = new Sql();

            uint personId = sql.GetIdFromEntry("Persons", "Name", Personal);

            uint newShiftId = sql.CreateShift(personId, StartTime, EndTime, sendType);

            string message = "Neue Schicht für \r\n\r\n[" +
                                personId + "] " + Personal +
                                "\r\n\r\nSchicht-Nr.:\t" + newShiftId +
                                "\r\nvon\t\t" + StartTime.ToString("dd.MM.yyyy HH:mm") +
                                "\r\nbis\t\t" + EndTime.ToString("dd.MM.yyyy HH:mm");

            switch (sendType)
            {
                case MessageType.SentToSms:
                    message += "\r\n\n Benachrichtigungen per SMS.";
                    break;
                case MessageType.SentToEmail:
                    message += "\r\n\n Benachrichtigungen per Email.";
                    break;
                case MessageType.SentToEmailAndSMS:
                    message += "\r\n\n Benachrichtigungen per SMS und Email.";
                    break;
                default:
                    message += "\r\n\n Benachrichtigungsweg nicht festgelegt.";
                    break;
            }

            MessageBox.Show(message, "Neue Schicht", MessageBoxButton.OK, MessageBoxImage.Information);
            Log.Write(Log.Type.Calendar, message);

            LoadShiftsToNightShiftClass();
            BlackOutCalenderDays(true);
        }

        private void Cal_Button_CreateNewServiceWeek_Click(object sender, RoutedEventArgs e)
        {
            StatusBarText = "Erstelle neue Bereitschaftswoche...";
            CreateNewServiceWeek();
        }

        private void CreateNewServiceWeek()
        {
            Sql sql = new Sql();
            DateTime StartDate;
            int durationDays = 7; //Tage Bereitschaftsdienst

            try
            {
                StartDate = (DateTime)Cal_DatePicker.SelectedDate;
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }

            Cal_DatePicker.SelectedDate = StartDate.AddDays(durationDays + 1);

            string Personal = Cal_ComboBox_Personal.SelectedValue.ToString();
            uint personId = sql.GetIdFromEntry("Persons", "Name", Personal);

            //Welches Sendemedium?
            MessageType sendType;
            if ((bool)Cal_SendViaSMS_CheckBox.IsChecked)
            {
                sendType = ((bool)Cal_SendViaEmail_CheckBox.IsChecked) ? MessageType.SentToEmailAndSMS : MessageType.SentToSms;
            }
            else
            {
                sendType = ((bool)Cal_SendViaEmail_CheckBox.IsChecked) ? MessageType.SentToEmail : MessageType.NoCategory;
            }

            DateTime date = StartDate.AddDays(DayOfWeek.Monday - StartDate.DayOfWeek).Date;

            //MessageBox.Show("StartDate=" + date.ToLongDateString());

            List<DateTime> holidays = HelperClass.Feiertage(date);

            // Jahreswechsel?
            if (date.Year != date.AddDays(durationDays).Year)
            {
                holidays.AddRange(HelperClass.Feiertage(date.AddDays(durationDays)));
            }

            int endHour = NightShiftEndHour;
            DateTime StartTime;
            DateTime EndTime;

            for (int numDay = 0; numDay < durationDays; numDay++)
            {

                int startHour;
                if (holidays.Contains(date) || date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    startHour = NightShiftEndHour; //Start = Ende Vortag.
                }
                else
                {
                    if (date.DayOfWeek == DayOfWeek.Friday)
                    {
                        startHour = NightShiftStartHourFriday;
                    }
                    else
                    {
                        startHour = NightShiftStartHour;
                    }
                }

                StartTime = date.AddHours(startHour);
                EndTime = date.AddDays(1).AddHours(endHour);

                sql.CreateShift(personId, StartTime, EndTime, sendType);


                date = date.AddDays(1);
            }

            string message = "Neue Bereitschaftswoche für \r\n\r\n[" +
                                personId + "] " + Personal +
                                "\r\nvon\t\t" + date.AddDays(-durationDays).ToString("dd.MM.yyyy") +
                                "\r\nbis\t\t" + date.ToString("dd.MM.yyyy");

            switch (sendType)
            {
                case MessageType.SentToSms:
                    message += "\r\n\n Benachrichtigungen per SMS.";
                    break;
                case MessageType.SentToEmail:
                    message += "\r\n\n Benachrichtigungen per Email.";
                    break;
                case MessageType.SentToEmailAndSMS:
                    message += "\r\n\n Benachrichtigungen per SMS und Email.";
                    break;
                default:
                    message += "\r\n\n Benachrichtigungsweg nicht festgelegt.";
                    break;
            }

            MessageBox.Show(message, "Neue Schicht", MessageBoxButton.OK, MessageBoxImage.Information);
            Log.Write(Log.Type.Calendar, message);

            LoadShiftsToNightShiftClass();
            BlackOutCalenderDays(true);
        }

        private void Cal_DatePicker_CalendarClosed(object sender, RoutedEventArgs e)
        {
            UpdateCalendarHolidays();
        }

        private void UpdateCalendarHolidays()
        {
            DateTime selectedDate = (DateTime)Cal_DatePicker.SelectedDate;

            if (selectedDate == null) return;

            List<DateTime> Holidays = HelperClass.Feiertage(selectedDate);

            //An Wochenenden und Feiertagen den ganzen Tag vorbelegen.
            if (Holidays.Contains(selectedDate) || selectedDate.DayOfWeek == DayOfWeek.Saturday || selectedDate.DayOfWeek == DayOfWeek.Sunday)
            {
                Cal_TimeFrom.SelectedIndex = 0;
                Cal_TimeTo.SelectedIndex = Cal_TimeTo.Items.Count - 1;
            }
            else
            {
                Cal_TimeFrom.SelectedIndex = 17;
                Cal_TimeTo.SelectedIndex = 7;
            }
        }

        private void Cal_ComboBox_Personal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;

            string Personal = e.AddedItems[0].ToString();

            uint personId = sql.GetIdFromEntry("Persons", "Name", Personal);

            Cal_SendViaEmail_CheckBox.IsChecked = sql.GetPersonContactPossibility(MessageType.SentToEmail, personId);
            Cal_SendViaSMS_CheckBox.IsChecked = sql.GetPersonContactPossibility(MessageType.SentToSms, personId);

            UpdateCalendarHolidays();
        }

        /// <summary>
        /// Erstellt die Liste der Stillen Nachrichtenempfänger, die immer informiert werden.
        /// </summary>
        /// <returns>Liste der Stillen Nachrichtenempfänger</returns>
        internal static ObservableCollection<NightShift> GetSilentListeners()
        {
            ObservableCollection<NightShift> silentListeners = new ObservableCollection<NightShift>();

            NightShift nightShift1 = new NightShift
            {
                ShiftsId = 0,
                GuardName = "MelBox2Admin",
                SendToEmail = MainWindow.MelBoxAdmin.Address,
                SendToCellphone = 0, //hier ggf. eine Ausweichnummer eintragen?
                SendViaSMS = false,
                SendViaEmail = true
            };

            silentListeners.Add(nightShift1);

            return silentListeners;
        }

    }
}
