using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MelBox2_2
{
    #region Datentypen für SMS und Mail

    /// <summary>
    /// Die Nachrichten werden hiermit in Kategorien eingeordnet.
    /// Kategorien können bitweise vergeben werden z.B. MessageType = 36 (= 4 + 32) heisst: Nachricht empfangen als SMS, weitergeleitet als Email.
    /// Die hier vergebenen Texte tauchen in der Visualisierung auf.
    /// </summary>
    [Flags]
    public enum MessageType : short
    {
        NoCategory = 0,             //Nicht zugeordnet
        RecievedFromUnknown = 1,    //Empfang von unbekannt
        SentToUnknown = 2,          //Senden an Unbekannt
        RecievedFromSms = 4,        //Empfang von SMS
        SentToSms = 8,              //Senden an SMS
        RecievedFromEmail = 16,     //Empgang von Email
        SentToEmail = 32,           //Senden an Email  
        SentToEmailAndSMS = 40      //Senden an Email und SMS    
    }

    /// <summary>
    /// Liste von ShortMessage - Nachrichten
    /// </summary>
    public class MessageCollection : List<Message> { }

    /// <summary>
    /// Container für Telegramme aus SMS oder EMAIL
    /// Format orientiert sich an Tabelle Messages
    /// </summary>
    public class Message
    {
        //SQL-Tabelle "Messages"
        //"ID" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "UnixTimeStamp" INTEGER NOT NULL,"PersonID" INTEGER NOT NULL, "Type" INTEGER NOT NULL, "Content" TEXT);",

        // Beispiel SMS:
        //+CMGL: 1,"REC READ","+918884100421","","13/04/05,08:24:36+22"
        //here's message one 

        private int _Index;
        private ushort _Type;
        private string _Status;
        private ulong _Cellphone;
        private string _Email;
        private string _CustomerKeyWord;
        private string _Alphabet;
        private ulong _SentTime;
        private string _Content;

        /// <summary>
        /// Für SMS und Email. 
        /// ID beim Abholen der Nachricht aus dem Speicher des Providers. 
        /// (Bisher Keine Verwendung. Später mal Löschen alter Nachrichten aus der InBox beim Provider?)
        /// </summary>
        public int Index
        {
            get { return _Index; }
            set { _Index = value; }
        }

        public string Status
        {
            get { return _Status; }
            set { _Status = value; }
        }

        /// <summary>
        /// Für SMS
        /// </summary>
        public ushort Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        /// <summary>
        /// Für SMS
        /// </summary>
        public ulong Cellphone
        {
            get { return _Cellphone; }
            set { _Cellphone = value; }
        }

        /// <summary>
        /// Für Email
        /// </summary>
        public string EMail
        {
            get { return _Email; }
            set
            {
                if (HelperClass.IsValidEmailAddress(value))
                {
                    _Email = value;
                }
            }
        }

        /// <summary>
        /// Für SMS
        /// </summary>
        public string CustomerKeyWord
        {
            get { return _CustomerKeyWord; }
            set { _CustomerKeyWord = value; }
        }

        /// <summary>
        /// Für SMS.
        /// (Keine Verwendung)
        /// </summary>
        public string Alphabet
        {
            get { return _Alphabet; }
            set { _Alphabet = value; }
        }

        /// <summary>
        /// Unix-Zeitstempel für SMS und Email
        /// </summary>
        public ulong SentTime
        {
            get { return _SentTime; }
            set { _SentTime = value; }
        }

        /// <summary>
        /// Für SMS und Email
        /// Begrenzt auf 255 Zeichen. Zeilenumbrüche werden durch Leerzeichen ersetzt.
        /// </summary>
        public string Content
        {
            get { return _Content; }
            set
            {
                if (value.Length > 255)
                {
                    _Content = value.Substring(0, 255).Replace(Environment.NewLine, " ") + "...";
                }
                else
                {
                    _Content = value.Replace(Environment.NewLine, " ");
                }
            }
        }

    }
    #endregion

    //#region Datentypen für Personen

    //public class PersonCollection : List<Person> { }

    ///// <summary>
    ///// Repräsentation einer Person (Nutzer, Kunde, Bereitschaftsnehmer..)
    ///// </summary>
    //public class Person
    //{
    //    public uint Id { get; set; }

    //    public DateTime CreationTime { get; set; }

    //    public string Name { get; set; }

    //    public uint CompanyId { get; set; }

    //    public string CompanyName { get; set; }

    //    private string _Email;
    //    /// <summary>
    //    /// Für Email
    //    /// </summary>
    //    public string EMail
    //    {
    //        get { return _Email; }
    //        set
    //        {
    //            if (HelperClass.IsValidEmailAddress(value))
    //            {
    //                _Email = value;
    //            }
    //        }
    //    }

    //    public ulong Cellphone { get; set; }

    //    public string KeyWord { get; set; }
    //    public uint MaxInactive { get; set; }
    //    public MessageType MessageWay { get; set; }
    //}

    //#endregion

    #region Datentypen für Bereitschaftsdienste

    public class NightShift : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        void OnNightShiftPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private DateTime entryTime;
        private string guardName;
        private DateTime startTime;
        private int startTimeHour;
        private DateTime endTime;
        private int endTimeHour;

        public uint ShiftsId { get; set; }

        public DateTime EntryTime
        {
            get
            {
                return entryTime;
            }
            set
            {
                entryTime = value;
                OnNightShiftPropertyChanged(nameof(EntryTime));
            }
        }

        public string GuardName
        {
            get
            {
                return guardName;
            }
            set
            {
                guardName = value;
                OnNightShiftPropertyChanged(nameof(GuardName));
            }
        }

        /// <summary>
        /// Gibt an, ob StartTime in der Vergangenheit liegt.
        /// Wird genutzt, um DataGrid-Reihen zu formatieren.
        /// </summary>
        public bool IsPastTime { get; set; } = false;

        public bool IsHoliday { get; set; } = false;

        public bool IsWeekend { get; set; } = false;

        public DateTime StartTime
        {
            get
            {
                return startTime;
            }
            set
            {
                startTime = value;
                StartTimeHour = value.Hour;
                if (DateTime.Compare(value, DateTime.Now.Date) < 0)
                {
                    //value liegt vor heute
                    IsPastTime = true;
                }
                else
                {
                    IsPastTime = false;
                }

                if (value.DayOfWeek == DayOfWeek.Saturday || value.DayOfWeek == DayOfWeek.Sunday)
                {
                    IsWeekend = true;
                }
                else
                {
                    IsWeekend = false;
                }

                List<DateTime> holidays = HelperClass.Feiertage(value);
                if (holidays.Contains(value.Date))
                {
                    IsHoliday = true;
                }
                else
                {
                    IsHoliday = false;
                }

                OnNightShiftPropertyChanged(nameof(StartTime));
            }
        }

        public int StartTimeHour
        {
            get
            {
                return startTimeHour;
            }
            set
            {
                startTimeHour = value;
                StartTime.AddHours(value - StartTime.Hour);
                OnNightShiftPropertyChanged(nameof(StartTimeHour));
            }
        }

        public DateTime EndTime
        {
            get
            {
                return endTime;
            }
            set
            {
                endTime = value;

                if (StartTime.Date.AddDays(1) == EndTime.Date)
                {
                    EndTimeHour = value.Hour;
                }
                else
                {
                    EndTimeHour = 24;
                }
                OnNightShiftPropertyChanged(nameof(EndTime));
            }
        }

        public int EndTimeHour
        {
            get
            {
                return endTimeHour;
            }
            set
            {
                endTimeHour = value;
                EndTime.AddHours(value - EndTime.Hour);
                OnNightShiftPropertyChanged(nameof(EndTimeHour));
            }
        }

        private bool sendViaSMS;

        public bool SendViaSMS
        {
            get { return sendViaSMS; }
            set
            {
                sendViaSMS = value;
                OnNightShiftPropertyChanged(nameof(SendViaSMS));
            }
        }

        private bool sendViaEmail;

        public bool SendViaEmail
        {
            get { return sendViaEmail; }
            set
            {
                sendViaEmail = value;
                OnNightShiftPropertyChanged(nameof(SendViaEmail));
            }
        }

        public string SendToEmail { get; set; }

        public ulong SendToCellphone { get; set; }
    }

    #endregion

    internal static class HelperClass
    {
        /// <summary>
        /// Prüft, ob der übergebene string eine gültige email-Adresse ist.
        /// </summary>
        /// <param name="mailAddress"></param>
        /// <returns></returns>
        internal static bool IsValidEmailAddress(string mailAddress)
        {
            Regex mailIDPattern = new Regex(@"[\w-]+@([\w-]+\.)+[\w-]+");

            if (!string.IsNullOrEmpty(mailAddress) && mailIDPattern.IsMatch(mailAddress))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Kovertiert einen String mit Zahlen und Zeichen in eine Telefonnumer als Zahl mit führender  
        /// Ländervorwahl z.B. +49 (0) 4201 123 456 oder 0421 123 456 wird zu 49421123456 
        /// </summary>
        /// <param name="str_phone">String, der eine Telefonummer enthält.</param>
        /// <returns>Telefonnumer als Zahl mit führender  
        /// Ländervorwahl (keine führende 00). Bei ungültigem str_phone Rückgabewert 0.</returns>
        internal static ulong ConvertStringToPhonenumber(string str_phone)
        {
            // Entferne (0) aus +49 (0) 421...
            str_phone = str_phone.Replace("(0)", string.Empty);

            // Entferne alles ausser Zahlen
            Regex regexObj = new Regex(@"[^\d]");
            str_phone = regexObj.Replace(str_phone, "");

            // Wenn zu wenige Zeichen übrigbleiben gebe 0 zurück.
            if (str_phone.Length < 2) return 0;

            // Wenn am Anfang 0 steht, aber nicht 00 ersetze führende 0 durch 49
            string firstTwoDigits = str_phone.Substring(0, 2);

            if (firstTwoDigits != "00" && firstTwoDigits[0] == '0')
            {
                str_phone = "49" + str_phone.Substring(1, str_phone.Length - 1);
            }

            ulong number = ulong.Parse(str_phone);

            if (number > 0)
            {
                return number;
            }
            else
            {
                return 0;
            }
        }

        // Aus VB konvertiert
        private static DateTime DateOsterSonntag(DateTime pDate)
        {
            int viJahr, viMonat, viTag;
            int viC, viG, viH, viI, viJ, viL;

            viJahr = pDate.Year;
            viG = viJahr % 19;
            viC = viJahr / 100;
            viH = (viC - viC / 4 - (8 * viC + 13) / 25 + 19 * viG + 15) % 30;
            viI = viH - viH / 28 * (1 - 29 / (viH + 1) * (21 - viG) / 11);
            viJ = (viJahr + viJahr / 4 + viI + 2 - viC + viC / 4) % 7;
            viL = viI - viJ;
            viMonat = 3 + (viL + 40) / 44;
            viTag = viL + 28 - 31 * (viMonat / 4);

            return new DateTime(viJahr, viMonat, viTag);
        }

        // Aus VB konvertiert
        public static List<DateTime> Feiertage(DateTime pDate)
        {
            int viJahr = pDate.Year;
            DateTime vdOstern = DateOsterSonntag(pDate);
            List<DateTime> feiertage = new List<DateTime>
            {
                new DateTime(viJahr, 1, 1),    // Neujahr
                new DateTime(viJahr, 5, 1),    // Erster Mai
                vdOstern.AddDays(-2),          // Karfreitag
                vdOstern.AddDays(1),           // Ostermontag
                vdOstern.AddDays(39),          // Himmelfahrt
                vdOstern.AddDays(50),          // Pfingstmontag
                new DateTime(viJahr, 10, 3),   // TagderDeutschenEinheit
                new DateTime(viJahr, 10, 31),  // Reformationstag
                new DateTime(viJahr, 12, 24),  // Heiligabend
                new DateTime(viJahr, 12, 25),  // Weihnachten 1
                new DateTime(viJahr, 12, 26),  // Weihnachten 2
                new DateTime(viJahr, 12, DateTime.DaysInMonth(viJahr, 12)) // Silvester
            };

            return feiertage;
        }

        /// <summary>
        /// Ermittelt aus dem übergebenen datum die aktuelle Kalenderwoche
        /// </summary>
        /// <param name="datum"></param>
        /// <returns></returns>
        public static int GetCalendarWeek(DateTime datum)
        {
            int kw = (datum.DayOfYear / 7) + 1;
            if (kw == 53)
            {
                kw = 1;
            }
            return kw;
        }

        /// <summary>
        /// Wandelt den übergebenen String in die Hexadezimaldarstellung
        /// </summary>
        /// <param name="Hexstring">der umzuwandelnde String</param>
        /// <returns>Hexadezimaldarstellung</returns>
        public static string StringToHex(string hexstring)
        {
            var sb = new StringBuilder();
            foreach (char t in hexstring)
                // ggf. IFormatProvider culture Entfernen!
                sb.Append(Convert.ToInt32(t).ToString("x") + " ");
            return sb.ToString();
        }

        public static MessageType GetMessageType(bool recEmail = false, bool recSms = false, bool sentEmail = false, bool sentSms = false)
        {
            MessageType type = 0;

            if (recSms) type += 4;    //Empfang von SMS
            if (sentSms) type += 8;    //Senden an SMS
            if (recEmail) type += 16;   //Empgang von Email
            if (sentEmail) type += 32;   //Senden an Email 
            
            return type;
        }

    }

}
