using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelBox2_2
{
    class Sql
    {
        #region Felder
        public static string DbPath { get; set; } = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "DB", "MelBox2.db");
        private readonly string Datasource = "Data Source=" + DbPath;

        public static string DefaultGuardName = "Bereitschaftshandy";
        #endregion

        #region Helfer-Methoden
        public static ulong ConvertToUnixTime(DateTime datetime)
        {
            DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return (ulong)(datetime - sTime).TotalSeconds;
        }

        public static ulong ConvertToUnixTime(string datetime)
        {
            DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            if (DateTime.TryParse(datetime, out DateTime time)) 
            {
                return (ulong)(time - sTime).TotalSeconds;
            }
            else
            {
                return 0;
            }            
        }

        public static DateTime UnixTimeToDateTime(ulong unixtime)
        {
            DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return sTime.AddSeconds(unixtime);
        }
        #endregion

        #region SQL- Basismethoden
        public Sql()
        {
            if (!System.IO.File.Exists(DbPath))
            {
                CreateNewDataBase();
            }
        }
        
        /// <summary>
        /// Erzeugt eine neue Datenbankdatei, erzeugt darin Tabellen, Füllt diverse Tabellen mit Defaultwerten.
        /// </summary>
        private void CreateNewDataBase()
        {
            //Erstelle Datenbank-Datei
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DbPath));
            FileStream stream = File.Create(DbPath);
            stream.Close();

            //Erzeuge Tabellen in neuer Datenbank-Datei
            using (var con = new SQLiteConnection(Datasource))
            {
                con.Open();

                List<String> TableCreateQueries = new List<string>
                    {
                        "CREATE TABLE \"Log\"(\"ID\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,\"UnixTimeStamp\" INTEGER NOT NULL, \"Type\" TEXT ,  \"Message\" TEXT);",

                        "CREATE TABLE \"Companies\" (\"ID\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"Name\"  TEXT NOT NULL,	\"Address\" TEXT, " +
                        "\"ZipCode\" INTEGER,\"City\"  TEXT); ",

                        "INSERT INTO \"Companies\" (\"ID\", \"Name\", \"Address\", \"ZipCode\", \"City\") VALUES (0, \"_UNBEKANNT_\", \"Musterstraße 123\", 12345, \"Modellstadt\" );",

                        "INSERT INTO \"Companies\" (\"ID\", \"Name\", \"Address\", \"ZipCode\", \"City\") VALUES (1, \"Kreutzträger Kältetechnik GmbH & Co. KG\", \"Theodor-Barth-Str. 21\", 28307, \"Bremen\" );",

                        "CREATE TABLE \"Persons\"(\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE, \"UnixTimeStamp\" INTEGER NOT NULL, \"Name\" TEXT NOT NULL, " +
                        "\"CompanyID\" INTEGER, \"Email\" TEXT, \"Cellphone\" INTEGER, \"KeyWord\" TEXT, \"MaxInactive\" INTEGER, \"MessageType\" INTEGER );",

                        "INSERT INTO \"Persons\" (\"ID\", \"UnixTimeStamp\", \"Name\", \"MessageType\" ) VALUES (0, " + ConvertToUnixTime(DateTime.Now) + ", \"System_MelBox\", " + (ushort)MessageType.NoCategory + ");",

                        "INSERT INTO \"Persons\" (\"ID\", \"UnixTimeStamp\", \"Name\", \"CompanyID\", \"Email\", \"Cellphone\", \"MessageType\" ) VALUES (1, " + ConvertToUnixTime(DateTime.Now) + ", \"" +  DefaultGuardName + "\", 1, \"bereitschaftshandy@kreutztraeger.de\", 491728362586," + (ushort)MessageType.SentToSms + ");",

                        "INSERT INTO \"Persons\" (\"ID\", \"UnixTimeStamp\", \"Name\", \"CompanyID\", \"Email\", \"Cellphone\", \"MessageType\" ) VALUES (2, " + ConvertToUnixTime(DateTime.Now) + ", \"Henry Kreutzträger\", 1, \"henry.kreutztraeger@kreutztraeger.de\", 491727889419," + (ushort)MessageType.SentToEmailAndSMS + ");",

                        "INSERT INTO \"Persons\" (\"ID\", \"UnixTimeStamp\", \"Name\", \"CompanyID\", \"Email\", \"Cellphone\", \"MessageType\" ) VALUES (3, " + ConvertToUnixTime(DateTime.Now) + ", \"Bernd Kreutzträger\", 1, \"bernd.kreutztraeger@kreutztraeger.de\", 491727875067," + (ushort)MessageType.SentToEmailAndSMS + ");",

                        //Tabelle MessageTypes wird z.Zt. nicht verwendet! Dient als Dokumentation für die BitCodierung von MessageType.
                        "CREATE TABLE \"MessageTypes\" (\"ID\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"Description\" TEXT NOT NULL);",

                        "INSERT INTO \"MessageTypes\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.NoCategory + ", \"keine Zuordnung\");",
                        "INSERT INTO \"MessageTypes\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.RecievedFromUnknown + ", \"von unbekannt\");",
                        "INSERT INTO \"MessageTypes\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.SentToUnknown + ", \"an unbekannt\");",
                        "INSERT INTO \"MessageTypes\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.RecievedFromSms + ", \"von SMS\");",
                        "INSERT INTO \"MessageTypes\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.SentToSms + ", \"an SMS\");",
                        "INSERT INTO \"MessageTypes\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.RecievedFromEmail + ", \"von Email\");",
                        "INSERT INTO \"MessageTypes\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.SentToEmail + ", \"an Email\");",
                        "INSERT INTO \"MessageTypes\" (\"ID\", \"Description\") VALUES (" + (ushort)MessageType.SentToEmailAndSMS + ", \"an SMS und Email\");",

                        "CREATE TABLE \"Messages\"( \"ID\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"RecieveTime\" INTEGER NOT NULL, \"FromPersonID\" INTEGER NOT NULL, " +
                                            " \"SendTime\" INTEGER, \"ToPersonIDs\" TEXT,  \"Type\" INTEGER NOT NULL, \"Content\" TEXT);",

                        "INSERT INTO \"Messages\" (\"RecieveTime\", \"FromPersonID\", \"Type\", \"Content\") VALUES " +
                                            "(" + ConvertToUnixTime(DateTime.Now) + ",0,1,\"Datenbank neu erstellt.\");",

                        "CREATE TABLE \"Shifts\"( \"ID\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"EntryTime\" INTEGER NOT NULL, " +
                                            "\"PersonID\" INTEGER NOT NULL, \"StartTime\" INTEGER NOT NULL, \"EndTime\" INTEGER NOT NULL, \"SendType\" INTEGER NOT NULL );",

                        "INSERT INTO \"Shifts\" ( \"ID\", \"EntryTime\", \"PersonID\", \"StartTime\", \"EndTime\", \"SendType\" ) VALUES (0, " +
                         ConvertToUnixTime( DateTime.Now ) + ", 0," + ConvertToUnixTime( DateTime.Now.Date.AddHours(17) ) + ", " + ConvertToUnixTime( DateTime.Now.Date.AddDays(1).AddHours(7)  ) +", 0 );",

                        "CREATE TABLE \"BlockedMessages\"( \"ID\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"Content\" TEXT NOT NULL, \"StartHour\" INTEGER NOT NULL, " +
                        " \"EndHour\" INTEGER NOT NULL, \"WorkdaysOnly\" INTEGER NOT NULL CHECK (\"WorkdaysOnly\" < 2));"

                };

                foreach (string query in TableCreateQueries)
                {
                    SQLiteCommand sQLiteCommand = new SQLiteCommand(query, con);
                    using (SQLiteCommand cmd = sQLiteCommand)
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Liest aus der SQL-Datenbank und gibt ein DataTable-Object zurück.
        /// </summary>
        /// <param name="query">SQL-Abfrage mit Parametern</param>
        /// <param name="args">Parameter - Wert - Paare</param>
        /// <returns>Abfrageergebnis als DataTable.</returns>
        private DataTable ExecuteRead(string query, Dictionary<string, object> args)
        {
            if (string.IsNullOrEmpty(query.Trim()))
                return null;

            using (var con = new SQLiteConnection(Datasource))
            {
                con.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                {
                    if (args != null)
                    {
                        //set the arguments given in the query
                        foreach (var pair in args)
                        {
                            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                        }
                    }


                    var da = new SQLiteDataAdapter(cmd);

                    var dt = new DataTable();
                    da.Fill(dt);
                    da.Dispose();
                    return dt;
                }
            }
        }

        private DataTable ExecuteRead(string query)
        {
            if (string.IsNullOrEmpty(query.Trim()))
                return null;

            try
            {

                using (var con = new SQLiteConnection(Datasource))
                {
                    con.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        var da = new SQLiteDataAdapter(cmd);

                        var dt = new DataTable();
                        _ = da.Fill(dt);
                        da.Dispose();
                        return dt;
                    }
                }
            }
            catch (DataException data_ex)
            {
                Log.Write(Log.Type.General, "Lesen in SQL-Datenbank schlug fehl bei: " + query + "\r\n" + data_ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Führt Schreibaufgaben in DB aus. 
        /// </summary>
        /// <param name="query">SQL-Abfrage mit Parametern</param>
        /// <param name="args">Parameter - Wert - Paare</param>
        /// <returns>Anzahl betroffener Zeilen.</returns>
        private int ExecuteWrite(string query, Dictionary<string, object> args)
        {
            int numberOfRowsAffected = 0;

            try
            {
                //setup the connection to the database
                using (var con = new SQLiteConnection(Datasource))
                {
                    con.Open();

                    //open a new command
                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        //set the arguments given in the query
                        foreach (var pair in args)
                        {
                            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                        }

                        //execute the query and get the number of row affected
                        numberOfRowsAffected = cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                //nichts unternehmen
                Log.Write(Log.Type.Internal, "Schreiben in SQL-Datenbank fehlgeschlagen. " + query + " | " + ex.Message);
            }

            return numberOfRowsAffected;
        }

        #endregion

        #region SQL Allgemein

        /// <summary>
        /// Listet alle Tabellen in der Datenbank, bis auf übergebene Ausnahmen.
        /// /// Hinweis: Systemtabellen sind gelistet in Tabelle "sqlite_sequence"
        /// </summary>
        /// <param name="exceptions">Ausnamen: Tabellen, die nicht aufgelistet werden sollen</param>
        /// <returns>List von Tabellennamen in der Datenbank</returns>
        internal IEnumerable<string> GetAllTableNames(string[] exceptions)
        {
            string query = "SELECT name FROM sqlite_master WHERE type=\"table\"";

            DataTable dt = ExecuteRead(query, null);
            List<string> s = dt.AsEnumerable().Select(x => x[0].ToString()).ToList();

            if (exceptions.Length > 0)
            {
                foreach (string exception in  exceptions)
                {
                    s.Remove(exception);
                }                
            }

            return s;
        }

        /// <summary>
        /// Gibt den letzten Eintrag (letzte ID) der angegebenen Tabelle wieder.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        internal uint GetLastId(string tableName, string where = "1=1")
        {
            string query = "SELECT ID FROM \"" + tableName + "\" WHERE " + where + " ORDER BY ID DESC LIMIT 1";

            DataTable dt = ExecuteRead(query, null);
            string idString = dt.AsEnumerable().Select(x => x[0].ToString()).ToList().First();
            uint.TryParse(idString, out uint lastId);

            return lastId;
        }

        /// <summary>
        /// Ruft die ID bei dataEntry ab.
        /// SQL: [..] LIKE %dataEntry% [..]
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="colName"></param>
        /// <param name="dataEntry">Eintrag der in einer Zelle gefunden werden soll.</param>
        /// <returns></returns>
        internal uint GetIdFromEntry(string tableName, string colName, string dataEntry)
        {
            string query = "SELECT ID FROM \"" + tableName + "\" WHERE " + colName + " LIKE \"" + dataEntry + "\"";

            //Log.Write(Log.Type.Internal, query);

            DataTable dt = ExecuteRead(query, null);

            if (dt.Rows.Count < 1 || !uint.TryParse(dt.Rows[0][0].ToString(), out uint id))
            {
                id = 0;
            }

            return id;
        }

        /// <summary>
        /// Liest die letzten Einträge in der angegebenen Tabelle.
        /// Wandelt UNIX-Zeitstempel in lesbares Format um.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="numberOfRows"></param>
        /// <returns></returns>
        public DataTable GetLastEntries(string tableName, string sortingColName, int numberOfRows)
        {
            //BAUSTELLE!!!
            string query = "PRAGMA table_info(" + tableName + ")";
            DataTable dt = ExecuteRead(query);
            List<string> s = dt.AsEnumerable().Select(x => x["name"].ToString()).ToList();

            string cols = string.Empty;
            foreach (string col in s)
            {
                if (col != s.First()) cols += ", ";
                if (col.Contains("Time"))
                {
                    cols += "datetime(" + col + ", 'unixepoch') AS " + col;
                }
                else
                {
                    cols += col;
                }
            }

            query = "SELECT " + cols + " FROM " + tableName + " ORDER BY \"" + sortingColName + "\" DESC LIMIT " + numberOfRows;

            return ExecuteRead(query);
        }

        /// <summary>
        /// List eine LIste aus der Splate colName aus.
        /// </summary>
        /// <param name="tableName">Name der Tabelle.</param>
        /// <param name="colName">Name der abzufragenden Spalte.</param>
        /// <param name="where">(optional) Einschränkende Bedingung.</param>
        /// <returns></returns>
        public IEnumerable<string> GetListFromColumn(string tableName, string colName, string where = "1=1")
        {
            string query = "SELECT " + colName + " FROM " + tableName + " WHERE " + where;

            DataTable dt = ExecuteRead(query);
            List<string> s = dt.AsEnumerable().Select(x => x[0].ToString()).ToList();

            return s;
        }

        /// <summary>
        /// Erster Eintrag aus Splate colName.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="colName"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public string GetFirstEntryFromColumn(string tableName, string colName, string where = "1=1")
        {
            string query = "SELECT " + colName + " FROM " + tableName + " WHERE " + where;

            DataTable dt = ExecuteRead(query);
            return dt.AsEnumerable().Select(x => x[0].ToString()).ToList().First();
        }

        /// <summary>
        /// Liest die erste Zeile der Abfrage in ein Dictionary ab. 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal Dictionary<string, object> GetRowValues(string query, Dictionary<string, object> args)
        {
            DataTable dt = ExecuteRead(query, args);

            Dictionary<string, object> dict = new Dictionary<string, object>();

            if (dt.Rows.Count > 0)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    dict.Add(col.ColumnName, dt.Rows[0][col.ColumnName]);
                }
            }
            return dict;
        }

        /// <summary>
        /// Schreibt einen neuen Eintrag in die Tabelle 'Log'.
        /// </summary>
        /// <param name="message"></param>
        internal void CreateLogEntry(string type, string message)
        {
            const string query = "INSERT INTO Log(UnixTimeStamp, Type, Message) VALUES(@timeStamp, @type, @message)";

            var args = new Dictionary<string, object>
            {
                {"@timeStamp", ConvertToUnixTime( DateTime.Now ) },
                {"@type", type},
                {"@message", message}
            };

            ExecuteWrite(query, args);
        }

        /// <summary>
        /// Löscht die ältere Hälfte der Log-Einträge.
        /// </summary>
        /// <returns>Anzahl der gelöschten Einträge</returns>
        internal int DeleteHalfOfLogEntries()
        {
            string query = "DELETE FROM \"Log\" ORDER BY ID LIMIT (SELECT CAST(COUNT(ID)/ 2 AS INTEGER) FROM \"Log\" );";

            return ExecuteWrite(query, null);
        }

        /// <summary>
        /// Erstellt ein Backup der gesamten Datenbank.
        /// </summary>
        internal static void BackupDatabase()
        {            
            string destinationPath = Path.Combine(Path.GetDirectoryName(DbPath), DateTime.Now.Year.ToString() );
            Directory.CreateDirectory(destinationPath);
            destinationPath = Path.Combine(destinationPath, string.Format("{0}_KW{1:00}_", DateTime.Now.Year, HelperClass.GetCalendarWeek(DateTime.Now)) + Path.GetFileName(DbPath) );

            if (File.Exists(destinationPath)) return;

            Log.Write(Log.Type.Internal, "Erstelle Datenbank-Backup unter " + destinationPath);

            try
            {
                using (var source = new SQLiteConnection("Data Source =" + DbPath + "; Version=3;"))
                using (var destination = new SQLiteConnection("Data Source=" + destinationPath + "; Version=3;"))
                {
                    source.Open();
                    destination.Open();
                    source.BackupDatabase(destination, "main", "main", -1, null, 0);
                }
            }
            catch (IOException ex)
            {
                Log.Write(Log.Type.Internal, "Datenbank-Backup fehlgeschlagen: " + ex.GetType().ToString() + " | " + ex.Message);
            }
        }

        #endregion

        #region SQL Firmen

        /// <summary>
        /// Gibt eine Liste aller Firmennamen aus.
        /// </summary>
        /// <param name="where">EInschränkende Bedingung.</param>
        /// <returns>Liste von Firmennamen</returns>
        internal IEnumerable<string> GetListOfCompanies(string where = "1=1")
        {
            string query = "SELECT \"Name\" FROM \"Companies\" WHERE " + where;

            Sql sql = new Sql();
            DataTable dt = sql.ExecuteRead(query, null);
            List<string> s = dt.AsEnumerable().Select(x => x[0].ToString()).ToList();

            return s;
        }

        internal string GetCompanyNameFromId(uint CompanyId)
        {
            string query = "SELECT Name FROM Companies WHERE ID=" + CompanyId;

            DataTable dt = ExecuteRead(query, null);
            return dt.Rows[0][0].ToString();
        }

        /// <summary>
        /// Erzeugt einen neuen Eintrag in der Firmentabelle.
        /// </summary>
        /// <param name="companyName"></param>
        /// <param name="address"></param>
        /// <param name="zipCode"></param>
        /// <param name="city"></param>
        /// <returns>ID des letzten (=erstellten) Eintrags.</returns>
        internal uint CreateCompany(string companyName, string address, uint zipCode, string city)
        {
            string query = "INSERT OR REPLACE INTO \"Companies\" (Name, Address, ZipCode, City) VALUES (@Name, @Address, @ZipCode, @City) ";

            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@Name", companyName },
                { "@Address", address },
                { "@ZipCode", zipCode },
                { "@City", city }
            };

            ExecuteWrite(query, args);

            return GetLastId("Companies");
        }

        /// <summary>
        /// Aktualisiert einen Eintrag in der Firmentabelle.
        /// </summary>
        /// <param name="companyId">ID des EIntrags, der geändert werden soll.</param>
        /// <param name="companyName"></param>
        /// <param name="address"></param>
        /// <param name="zipCode"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        internal int UpdateCompany(uint companyId, string companyName, string address, int zipCode, string city)
        {
            string query = "UPDATE \"Companies\" SET Name = @Name, Address = @Address, ZipCode = @ZipCode, City = @City WHERE ID = @ID ;";

            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@ID", companyId },
                { "@Name", companyName },
                { "@Address", address },
                { "@ZipCode", zipCode },
                { "@City", city }
            };

            return ExecuteWrite(query, args);
        }

        /// <summary>
        /// Entfernt einen Eintrag in der Firmentabelle.
        /// </summary>
        /// <param name="companyId"></param>
        /// <returns></returns>
        internal int DeleteCompany(uint companyId)
        {
            string query = "DELETE FROM \"Companies\" WHERE ID = @ID ;";

            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@ID", companyId }
            };

            return ExecuteWrite(query, args);
        }

        #endregion

        #region SQL Personen

        /// <summary>
        /// 1. Finde Personen mit dem Nachnamen _UNBEKANNT_
        /// 2. 
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<string> GetUnknownPersons()
        {
            string query1 = "SELECT ID FROM Persons WHERE Name=\"_UNBEKANNT_\"";

            DataTable dt1 = ExecuteRead(query1);
            List<string> ids = dt1.AsEnumerable().Select(x => x[0].ToString()).ToList();

            string idCollection = "NULL";
            foreach (string id in ids)
            {
                idCollection += ", " + id;
            }

            string query2 = "SELECT KeyWord FROM Persons WHERE KeyWord IS NOT NULL AND ID IN (" + idCollection + ")" +
                            "UNION SELECT Email FROM Persons WHERE Email IS NOT NULL AND ID IN (" + idCollection + ")" +
                            "UNION SELECT Cellphone FROM Persons WHERE Cellphone IS NOT NULL AND ID IN(" + idCollection + ")";

            DataTable dt2 = ExecuteRead(query2);
            List<string> unknowns = dt2.AsEnumerable().Select(x => x[0].ToString()).ToList();

            return unknowns;
        }

        internal uint GetPersonID(Message message)
        {
            string email = message.EMail;
            string keyWord = message.CustomerKeyWord;

            if (email== null) email = String.Empty;
            if (keyWord == null) keyWord = String.Empty;

            Dictionary<string, object> personArgs = new Dictionary<string, object>
            {
                { "@phoneNumber", message.Cellphone },
                { "@email",  email.ToLower() },
                { "@keyWord", keyWord.ToLower() }
            };

            
            DataTable senderIDTable = ExecuteRead("SELECT \"ID\" FROM \"Persons\" WHERE " +
                                                    "( \"Cellphone\" > 0 AND \"Cellphone\" = @phoneNumber ) " +
                                                    "OR ( length(\"KeyWord\") > 2 AND \"KeyWord\" = @keyWord ) " +
                                                    "OR ( length(\"Email\") > 5 AND \"Email\" = @email )", personArgs);
            // Keine passende Person gefunden:
            if (senderIDTable.Rows.Count < 1)
            {
                Log.Write(Log.Type.Persons, string.Format("Kein Eintrag gefunden. Neue Person wird angelegt mit >{0}<, >{1}<, >{2}<", message.CustomerKeyWord, message.EMail, message.Cellphone));
                return CreatePerson(message);
            }
            else if (senderIDTable.Rows.Count > 1)
            {
                string entries = string.Empty;
                foreach (string item in senderIDTable.AsEnumerable().Select(x => x[0].ToString()).ToList())
                {
                    entries += item + ",";
                }
                Log.Write(Log.Type.Persons, string.Format("Es gibt meherer Einträge für eine Person mit KeyWord >{0}<, Email >{1}<, Mobilnummer >{2}< \r\nPersonen-IDs: {3}" , message.CustomerKeyWord, message.EMail, message.Cellphone, entries));
            }
            else
            {
                Log.Write(Log.Type.Persons, string.Format("Es gibt genau einen Eintrag für Keyword >{0}<, Email >{1}<, Mobilnummer >{2}<", message.CustomerKeyWord, message.EMail, message.Cellphone));
            }

            string idString = senderIDTable.AsEnumerable().Select(x => x[0].ToString()).ToList().First();

            if (!uint.TryParse(idString, out uint senderId))
            {
                Log.Write(Log.Type.Persons, "Der Eintrag >" + idString + "< konnte nicht als ID für eine Person interpretiert werden.");
                return CreatePerson(message);
            }

            return senderId;
        }

        /// <summary>
        /// Sucht Einträge mit mindestens einer Übereinstimmung der angegebenen Werte.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cellphone"></param>
        /// <param name="keyWord"></param>
        /// <returns></returns>
        internal uint GetPersonID(string personName, string email, ulong cellphone, string keyWord)
        {
           
            //if (email == null) email = String.Empty;
            //if (keyWord == null) keyWord = String.Empty;

            Dictionary<string, object> personArgs = new Dictionary<string, object>
            {
                { "@name", personName },
                { "@phoneNumber", cellphone },
                { "@email",  email },
                { "@keyWord", keyWord }
            };


            DataTable senderIDTable = ExecuteRead("SELECT \"ID\" FROM \"Persons\" WHERE " +
                                                    "( \"Name\" IS NOT NULL AND \"Name\" = @name ) " +
                                                    "OR ( \"Cellphone\" > 0 AND \"Cellphone\" = @phoneNumber ) " +
                                                    "OR ( length(\"KeyWord\") > 2 AND \"KeyWord\" = @keyWord ) " +
                                                    "OR ( length(\"Email\") > 5 AND \"Email\" = @email )", personArgs);
            // Keine passende Person gefunden:
            if (senderIDTable.Rows.Count < 1)
            {               
                return 0;
            }
                      
            if (senderIDTable.Rows.Count > 1)
            {
                Log.Write(Log.Type.Persons, string.Format("Es gibt meherer Einträge für eine Person mit KeyWord >{0}<, Email >{1}<, Mobilnummer >{2}<", keyWord, email, cellphone));                
            }

            string idString = senderIDTable.AsEnumerable().Select(x => x[0].ToString()).ToList().Last();

            if (!uint.TryParse(idString, out uint senderId))
            {
                Log.Write(Log.Type.Persons, "Der Eintrag >" + idString + "< konnte nicht als ID für eine Person interpretiert werden.");
                return 0;
            }

            return senderId;
        }

        internal string GetPersonNameFromId(uint PersonId)
        {
            string query = "SELECT Name FROM Persons WHERE ID=" + PersonId;

            DataTable dt = ExecuteRead(query, null);
            return dt.Rows[0][0].ToString();
        }

        /// <summary>
        /// Prüft ob das Bit messageType in der Datenbank gesetzt ist.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="personId"></param>
        /// <returns></returns>
        internal bool GetPersonContactPossibility(MessageType messageType, uint personId)
        {
            string query = "SELECT MessageType FROM Persons WHERE ID = " + personId ;

            DataTable dt = ExecuteRead(query);

            string strMessageType = dt.Rows[0][0].ToString();

            Enum.TryParse(strMessageType, out MessageType personMessageType);

            MessageType test = (messageType & personMessageType);

            return (test == messageType);
        }

        /// <summary>
        /// Fügt eine neue Person (Nutzer) in die Datenbank ein
        /// und gibt die ID des Eintrags wieder.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private uint CreatePerson(Message message)
        {
            //Schreibe neue Person in DB
            const string query = "INSERT INTO Persons( UnixTimeStamp, Name, Cellphone, Email, KeyWord ) VALUES ( @UnixTimeStamp, @Name, @Cellphone, @Email, @KeyWord )";

            //Telefonnumer nur eintragen wenn > 0
            object Cellphone = null;
            if (message.Cellphone > 0) Cellphone = message.Cellphone;

            string email = string.Empty;
            if (message.EMail != null) email = message.EMail.ToLower();

            string keyWord = null;
            if (message.CustomerKeyWord != null)
            {             
                keyWord = message.CustomerKeyWord.ToLower();
            }
            var args = new Dictionary<string, object>
                {
                    {"@UnixTimeStamp", ConvertToUnixTime(DateTime.Now) },
                    {"@Name", "_UNBEKANNT_"},
                    {"@Cellphone", Cellphone},
                    {"@Email", email},
                    {"@KeyWord", keyWord}
                };

            ExecuteWrite(query, args);

            # region Email-Benachrichtigung "neue unbekannte Telefonnummer / Emailadresse"
            DateTime sentTime = DateTimeOffset.FromUnixTimeSeconds((long)message.SentTime).UtcDateTime;

            StringBuilder body = new StringBuilder();
            body.Append("Es wurde ein neuer Absender in die Datenbank von MelBox2 eingetragen.\r\n\r\n");
            body.Append("Neue Nachricht empfangen am " + sentTime.ToShortDateString() + " um " + sentTime.ToLongTimeString() + " UTC \r\n\r\n");

            body.Append("Benutzerschlüsselwort ist\t\t\"" + message.CustomerKeyWord + "\"\r\n");
            body.Append("Empfangene Emailadresse war\t\t\"" + message.EMail + "\"\r\n");
            body.Append("Empfangene Telefonnummer war\t\"+" + message.Cellphone + "\"\r\n\r\n");
            if (keyWord != null && keyWord.Length > 0)
            {
                body.Append("Beginn der empfangenen Nachricht war\t\"" + message.CustomerKeyWord + "...\"\r\n");
            }
            else
            {
                body.Append("Empfangenen Nachricht war\t\t\"" + message.Content + "\"\r\n");
            }
            
            body.Append("\r\nBitte die Absenderdaten in MelBox2 im Reiter >Stammdaten< vervollständigen .\r\nDies ist eine automatische Nachricht von MelBox2");

            System.Net.Mail.MailAddressCollection sentTo = new System.Net.Mail.MailAddressCollection
            {
                MainWindow.MelBoxAdmin
            };

            MainWindow.SendEmail( sentTo , "Neuer Absender in MelBox2", body.ToString());
            #endregion

            return GetLastId("Persons");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="companyName"></param>
        /// <param name="email"></param>
        /// <param name="cellphone"></param>
        /// <param name="keyword"></param>
        /// <param name="maxinactive"></param>
        /// <param name="role"></param>
        /// <returns>die zuletzt erstellte Personen-ID</returns>
        internal uint CreatePerson(string Name, string companyName, string email, string cellphone, string keyword, string maxinactive, MessageType role)
        {
            //Schreibe/Aktualisiere Person in DB
            const string queryCreate = "INSERT INTO Persons ( UnixTimeStamp, Name, CompanyId, Cellphone, Email, KeyWord, MaxInactive, MessageType ) " +
                                        "VALUES ( @UnixTimeStamp, @Name, @CompanyId, @Cellphone, @Email, @KeyWord, @MaxInactive, @MessageType )";

            ulong unixTimeStamp = ConvertToUnixTime(DateTime.Now);

            uint CompanyId = GetIdFromEntry("Companies", "Name", companyName);

            ulong phonenumber = HelperClass.ConvertStringToPhonenumber(cellphone);

            if (!HelperClass.IsValidEmailAddress(email))
            {
                email = null;
            }

            if (!int.TryParse(maxinactive, out int maxInactiveInt))
            {
                maxInactiveInt = 0;
            }


            Dictionary<string, object> args = new Dictionary<string, object>()
            {
                    {"@UnixTimeStamp", unixTimeStamp },
                    {"@Name", Name},
                    {"@CompanyId", CompanyId},
                    {"@Cellphone", phonenumber},
                    {"@Email", email},
                    {"@KeyWord", keyword},
                    {"@MaxInactive", maxInactiveInt},
                    {"@MessageType", (uint)role}
            };

            ExecuteWrite(queryCreate, args);

            return GetLastId("Persons", "UnixTimeStamp = " + unixTimeStamp);
        }

        /// <summary>
        /// Ändert  einen Eintrag in der Personentabelle.
        /// </summary>
        /// <param name="personId">Identifikationsschlüssel</param>
        /// <param name="Name"></param>
        /// <param name="companyName"></param>
        /// <param name="email"></param>
        /// <param name="cellphone"></param>
        /// <param name="keyword"></param>
        /// <param name="maxinactive"></param>
        /// <param name="role"></param>
        /// <returns></returns>        
        internal int UpdatePerson(uint personId, string Name, string companyName, string email, string cellphone, string keyword, string maxinactive, MessageType role)
        {
            const string query =    "UPDATE Persons SET  " + 
                                    "UnixTimeStamp = @UnixTimeStamp, " +
                                    "Name = @Name, " +
                                    "CompanyId = @CompanyId, " +
                                    "Cellphone = @Cellphone, " +
                                    "Email = @Email, " +
                                    "KeyWord = @KeyWord," +
                                    "MaxInactive = @MaxInactive, " +
                                    "MessageType = @RoleId " +
                                    "WHERE ID = @ID";

            ulong unixTimeStamp = ConvertToUnixTime(DateTime.Now);

            uint CompanyId = GetIdFromEntry("Companies", "Name", companyName);

            ulong phonenumber = HelperClass.ConvertStringToPhonenumber(cellphone);

            if (!HelperClass.IsValidEmailAddress(email))
            {
                email = null;
            }

            if (!int.TryParse(maxinactive, out int maxInactiveInt))
            {
                maxInactiveInt = 0;
            }

            Dictionary<string, object> args = new Dictionary<string, object>()
            {
                    {"@ID", personId},
                    {"@UnixTimeStamp", unixTimeStamp },
                    {"@Name", Name},
                    {"@CompanyId", CompanyId},
                    {"@Cellphone", phonenumber},
                    {"@Email", email},
                    {"@KeyWord", keyword},
                    {"@MaxInactive", maxInactiveInt},
                    {"@RoleId", (int)role}
            };

            return ExecuteWrite(query, args);
        }

        /// <summary>
        /// Entfernt einen Eintrag in der Personentabelle.
        /// </summary>
        /// <param name="personId"></param>
        /// <returns></returns>
        internal int DeletePerson(uint personId)
        {
            string query = "DELETE FROM \"Persons\" WHERE ID = @ID ;";

            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@ID", personId }
            };

            return ExecuteWrite(query, args);
        }

        #endregion

        #region SQL Schichten

        /// <summary>
        /// Bereitschaftszeiten laden.
        /// </summary>
        /// <returns></returns>
        internal DataTable GetShifts(DateTime from, DateTime to)
        {
            string query = "SELECT Shifts.ID AS ID," +
                            "datetime(EntryTime, 'unixepoch') AS EntryTime, " +
                            "Persons.Name AS Name, " +
                            "datetime(StartTime, 'unixepoch') AS StartTime, " +
                            "datetime(EndTime, 'unixepoch') AS EndTime, " +
                            "Shifts.SendType AS SendType " +
                            "FROM Shifts JOIN Persons ON Shifts.PersonID = Persons.ID " +
                            "WHERE datetime(StartTime, 'unixepoch') >= @From AND datetime(EndTime, 'unixepoch') <= @To" +
                            "ORDER BY StartTime DESC LIMIT 1000";
            
            Dictionary<string, object> args = new Dictionary<string, object>()
            {
                    {"@From", from },
                    {"@To", to }
            };

            return ExecuteRead(query, args);
        }

        /// <summary>
        /// Bereitschaftszeiten laden.
        /// </summary>
        /// <returns></returns>
        internal DataTable GetShifts()
        {
            string query = "SELECT Shifts.ID AS ID," +
                            "datetime(EntryTime, 'unixepoch') AS EntryTime, " +
                            "Persons.Name AS Name, " +
                            "datetime(StartTime, 'unixepoch') AS StartTime, " +
                            "datetime(EndTime, 'unixepoch') AS EndTime, " +
                            "Shifts.SendType AS SendType " +
                            "FROM Shifts JOIN Persons ON Shifts.PersonID = Persons.ID " +
                            //"WHERE datetime(StartTime, 'unixepoch') >= @From AND datetime(EndTime, 'unixepoch') <= @To" +
                            "ORDER BY StartTime DESC LIMIT 1000";

            return ExecuteRead(query, null);
        }

        internal ObservableCollection<NightShift> GetCurrentShifts()
        {
            ObservableCollection<NightShift> nightShifts = new ObservableCollection<NightShift>();

            //Prüfe, ob es eine Schicht gibt, die heute beginnt                
            string todayShiftsCountStr = GetFirstEntryFromColumn("Shifts", "COUNT(ID)", "strftime('%d-%m-%Y', datetime(StartTime, 'unixepoch')) = strftime('%d-%m-%Y','now')");
            if (int.TryParse(todayShiftsCountStr, out int todayShiftsCount) && todayShiftsCount == 0)
            {
                //Erzeuge eine neue Schicht für heute mit Standardwerten (Bereitschaftshandy)
                CreateShiftDefault(DefaultGuardName);
            }

            string query = "SELECT Shifts.ID AS ID," +
                            "Persons.Name AS Name," +
                            "Persons.Email AS Email, " +
                            "Persons.Cellphone AS Cellphone, " +
                            "Shifts.SendType AS SendType, " +
                            "datetime(Shifts.StartTime , 'unixepoch') AS Begin, " +
                            "datetime(Shifts.EndTime , 'unixepoch') AS End " +
                            "FROM Shifts JOIN Persons ON Shifts.PersonID = Persons.ID " +
                            "WHERE strftime('%s','now') BETWEEN StartTime AND EndTime ";

            DataTable dt = ExecuteRead(query);

            if (dt.Rows.Count == 0 || dt == null)
            {
                // Wenn kein Bereitschaftsnehmer gefunden wurde:
                ObservableCollection<NightShift> silentListeners = MainWindow.GetSilentListeners();
                foreach (NightShift nightShift in silentListeners)
                {
                    nightShifts.Add(nightShift);
                }
            }
            else
            {
                foreach (DataRow row in dt.Rows)
                {
                    int sendType = int.Parse(row["SendType"].ToString());

                    ulong.TryParse(row["Cellphone"].ToString(), out ulong cellphone);

                    NightShift nightShift = new NightShift
                    {
                        ShiftsId = uint.Parse(row["ID"].ToString()),
                        GuardName = row["Name"].ToString(),
                        SendToEmail = row["Email"].ToString(),
                        SendToCellphone = cellphone,
                        SendViaSMS = (sendType == (int)MessageType.SentToSms) || (sendType == (int)MessageType.SentToEmailAndSMS),
                        SendViaEmail = (sendType == (int)MessageType.SentToEmail || sendType == (int)MessageType.SentToEmailAndSMS),
                        StartTime = DateTime.Parse( row["Begin"].ToString() ),
                        EndTime = DateTime.Parse( row["End"].ToString() )
                    };

                    nightShifts.Add(nightShift);
                }
            }
            return nightShifts;
        }


        /// <summary>
        /// Erstellt eine neue Schicht im Bereitschaftsplan.
        /// </summary>
        /// <param name="personId"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="sendingType"></param>
        /// <returns></returns>
        internal uint CreateShift(uint personId, DateTime startTime, DateTime endTime, MessageType sendingType)
        {
            ulong entryTime = ConvertToUnixTime(DateTime.Now);

            string query = "INSERT INTO Shifts ( EntryTime, PersonID, StartTime, EndTime, SendType ) " +
                           "VALUES ( @EntryTime, @PersonID, @StartTime, @EndTime, @SendType ); ";

            Dictionary<string, object> args = new Dictionary<string, object>()
            {
                    {"@EntryTime", entryTime},
                    {"@PersonID", personId},
                    {"@StartTime", ConvertToUnixTime(startTime) },
                    {"@EndTime", ConvertToUnixTime(endTime)},
                    {"@SendType", (int)sendingType},
            };

            ExecuteWrite(query, args);

            return GetLastId("Shifts", "EntryTime = " + entryTime);
        }
        /// <summary>
        /// Standard-Schicht, die erstellt wird, wenn kein Eintrag für den aktuellen Tag gefunden wurde.
        /// </summary>
        /// <param name="GuardName">Name des Bereitschaftnehmers, wie er in der Datenbank abgelegt ist.</param>
        /// <returns>ID der neu erstellten Schicht.</returns>
        internal uint CreateShiftDefault(string GuardName)
        {
            uint bereitschaftId = GetIdFromEntry("Persons", "Name", GuardName);
            MessageType type = (MessageType) int.Parse(GetFirstEntryFromColumn("Persons", "MessageType", "Name = \"" + GuardName + "\""));

            DateTime date = DateTime.Now.Date;
            List<DateTime> holidays = HelperClass.Feiertage(date);

            int startHour;
            if (holidays.Contains(date) || date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                startHour = MainWindow.NightShiftEndHour; //Start = Ende Vortag.
            }
            else
            {
                if (date.DayOfWeek == DayOfWeek.Friday)
                {
                    startHour = MainWindow.NightShiftStartHourFriday;
                }
                else
                {
                    startHour = MainWindow.NightShiftStartHour;
                }
            }

            DateTime StartTime = date.AddHours(startHour);
            DateTime EndTime = date.AddDays(1).AddHours(MainWindow.NightShiftEndHour);
            Log.Write(Log.Type.Calendar, "Habe automatische Standardschicht erstellt von " + StartTime.ToShortTimeString() + " bis " + EndTime.ToShortTimeString());

            return CreateShift(bereitschaftId, StartTime, EndTime, type);
        }

        /// <summary>
        /// Aktualisiert eine vorhanden Schicht im Bereitschaftsplan.
        /// </summary>
        /// <param name="shiftId"></param>
        /// <param name="personId"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        internal void UpdateShift(uint shiftId, uint personId, DateTime startTime, DateTime endTime, MessageType sendingType)
        {
            ulong entryTime = ConvertToUnixTime(DateTime.Now);

            string query = "UPDATE Shifts SET " +
                           "EntryTime = @EntryTime, PersonID = @PersonID, StartTime = @StartTime, EndTime = @EndTime, SendType = @SendType " +
                           "WHERE ID = @ID ; ";

            Dictionary<string, object> args = new Dictionary<string, object>()
            {
                    {"@ID", shiftId},
                    {"@EntryTime", entryTime},
                    {"@PersonID", personId},
                    {"@StartTime", ConvertToUnixTime(startTime) },
                    {"@EndTime", ConvertToUnixTime(endTime)},
                    {"@SendType", (int)sendingType}
            };

            ExecuteWrite(query, args);
        }

        /// <summary>
        /// Aktualisiert nur den Bereitschaftsnehmer in einer vorhanden Schicht im Bereitschaftsplan.
        /// </summary>
        /// <param name="shiftId">ID der Schicht die geändert werden soll.</param>
        /// <param name="personId">ID der Person, die diese Schicht übernimmt.</param>
        internal void UpdateShift(uint shiftId, uint personId, MessageType messageType)
        {            
            string query = "UPDATE Shifts SET " +
                           "PersonID = @PersonID, " +
                           "SendType = @SendType, " +
                           "WHERE ID = @ID ; ";

            Dictionary<string, object> args = new Dictionary<string, object>()
            {
                    {"@ID", shiftId},                   
                    {"@PersonID", personId},
                    {"@SendType", (uint)messageType}
            };

            ExecuteWrite(query, args);
        }

        /// <summary>
        /// Entfert eine Schicht aus dem Bereitschaftsplan.
        /// </summary>
        /// <param name="shiftId"></param>
        /// <returns></returns>
        internal int DeleteShift(uint shiftId)
        {
            //nur löschen, wenn mindestens eine Schicht vorhanden
            string query = "DELETE FROM \"Shifts\" WHERE ID = @ID;";

            Dictionary<string, object> args = new Dictionary<string, object>
            {
                { "@ID", shiftId }
            };

            return ExecuteWrite(query, args);
        }

        #endregion

        #region SQL Nachrichten

        /// <summary>
        /// Speichert eine neue Meldung und gibt die letzte ID der Tabelle Messages aus.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public uint CreateMessageEntry(Message message)
        {
            if (message.Content.Length < 2) message.Content = "- KEIN TEXT -";

            uint senderId = GetPersonID(message);

            //ist genau dieser Eintrag schon vorhanden?
            const string checkQuery = "SELECT ID FROM Messages WHERE RecieveTime = @RecieveTime AND FromPersonID = @FromPersonID AND Content = @Content";

            const string writeQuery = "INSERT INTO Messages(RecieveTime, FromPersonID, Type, Content) VALUES (@RecieveTime, @FromPersonID, @Type, @Content)";
                        
            var args = new Dictionary<string, object>
                {
                    {"@RecieveTime", message.SentTime },
                    {"@FromPersonID", senderId},
                    {"@Type", message.Type},
                    {"@Content", message.Content }               
                };

            DataTable dt = ExecuteRead(checkQuery, args);
            //Ist der Eintrag schon einmal vorhanden?
            if (dt.Rows.Count > 0)
            {                
                return 0;
            }

            //Wurde kein neuer Eintrag erzeugt?
            if (ExecuteWrite(writeQuery, args) == 0)
            {
                return 0;
            }

            MainWindow.Timer_LastMessages = GetLastMessagesForShow();

            return GetLastId("Messages");
        }

        /// <summary>
        /// Ergänzt den Eintrag einer Nachricht in der Datenbank, nachdem die Nachricht gesendet wurde.
        /// </summary>
        /// <param name="MessageId">Nachricht, die geändert werden soll.</param>
        /// <param name="messageType">Nachrichtentyp der hinzugefügt werdne soll.</param>
        /// <param name="recieverIds">IDs der Empfänger (Bereitschaftsnehmer)</param>
        internal void UpdateSentMessageEntry(uint MessageId, MessageType messageType, List<uint> recieverIds)
        {
            string query1 = "SELECT Type FROM Messages WHERE ID = " + MessageId;

            DataTable dt = ExecuteRead(query1, null);

            if (dt.Rows.Count < 1 || !uint.TryParse(dt.Rows[0][0].ToString(), out uint type))
            {
                return;
            }

            uint newType = type | (uint)messageType;

            string strReciverIds = string.Empty;

            foreach (uint id in recieverIds)
            {
                strReciverIds += id.ToString();

                if (id != recieverIds.Last()) 
                    strReciverIds += ",";
            }

            string query2 = "UPDATE Messages SET Type = " + newType + ", SendTime = " + ConvertToUnixTime(DateTime.Now) + ", ToPersonIDs = \"" + strReciverIds + "\" WHERE ID = " + MessageId;

            Dictionary<string, object> args = new Dictionary<string, object>
                {
                    {"@NewType ", newType },
                    {"@ID", MessageId}
                };

            ExecuteWrite(query2, args);
        }

        internal DataTable GetLastMessagesForShow()
        {
            string query = "SELECT " +
            "strftime('%d.%m.%Y %H:%M', datetime(Msg.RecieveTime, 'unixepoch')) AS Empfangen, " +
            "(CASE WHEN Msg.Type & " + (int)MessageType.RecievedFromSms + " > 0 THEN 'true' ELSE 'false' END) AS von_SMS, " +
            "(CASE WHEN Msg.Type & " + (int)MessageType.RecievedFromEmail + " > 0 THEN 'true' ELSE 'false' END) AS von_Email, " +
            "(CASE WHEN length(Persons.Name) > 0 THEN Persons.Name ELSE 'ID ' || Msg.FromPersonID END) AS von, " +
            "(CASE WHEN length(Msg.Content) > 255 THEN substr(Msg.Content, 0, 255) || '...' ELSE Msg.Content END) AS Inhalt, " +
            "strftime('%d.%m.%Y %H:%M', datetime(Msg.SendTime, 'unixepoch')) AS Gesendet, " +
            "(CASE WHEN Msg.Type & " + (int)MessageType.SentToSms + " > 0 THEN 'true' ELSE 'false' END) AS an_SMS, " +
            "(CASE WHEN Msg.Type & " + (int)MessageType.SentToEmail + " > 0 THEN 'true' ELSE 'false' END) AS an_Email, " +
            "(SELECT group_concat(Name) FROM Persons WHERE ID " +
            " IN( " +
            "  WITH split(word, str) AS( " +
            "     SELECT '', ToPersonIds || ',' FROM Messages WHERE ID = Msg.ID " +
            "     UNION ALL SELECT " +
            "     substr(str, 0, instr(str, ',')), " +
            "    substr(str, instr(str, ',') + 1) " +
            "   FROM split WHERE str != '' " +
            "  ) SELECT word FROM split WHERE word != '' " +
            " ) " +
            ") AS An, " +
            "(CASE WHEN(SELECT COUNT(ID) FROM BlockedMessages WHERE Msg.Content = BlockedMessages.Content) > 0 THEN 'ja' ELSE 'nein' END) AS Gesperrt " +
            "FROM Messages AS Msg " +
            "LEFT OUTER JOIN Persons ON Msg.FromPersonID = Persons.ID " +
            "LEFT JOIN BlockedMessages ON Msg.Content = BlockedMessages.Content " +
            "ORDER BY Msg.RecieveTime DESC LIMIT 1000 ";

            return ExecuteRead(query, null);
        }

        /// <summary>
        /// Zeigt an , ob die übergebene Nachricht zum aktuellen zeitpunkt gesperrt ist.
        /// </summary>
        /// <param name="message">NAchricht zur Überprüfung</param>
        /// <returns>true: Nachricht ist zum aktuellen Zeitpunkt gesperrt</returns>
        internal bool IsMessageBlocked(Message message)
        {
            DataTable dt = GetBlockedMessages( message.Content.Replace('"', '\'') , true);

            if (dt.Rows.Count > 0)
            {
                string strMatchCount = dt.AsEnumerable().Select(x => x[0].ToString()).ToList().First();
                uint.TryParse(strMatchCount, out uint matchCount);

                return (matchCount > 0);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gibt eine Liste gesperrter Nachrichten aus.
        /// </summary>
        /// <param name="filterString">Inhalt der Nachricht, nach der gesucht werden soll. (Platzhalter % möglich)</param>
        /// <param name="RegardTiming">true: nur zum aktuellen Zeitpunkt gesperrte Nachrichten</param>
        /// <returns></returns>
        internal DataTable GetBlockedMessages(string filterString = null, bool RegardTiming = false)
        {
            //Alle geperrten Nachrichten
            string query = "SELECT Content, StartHour, EndHour, WorkdaysOnly FROM BlockedMessages";

            //Nachrichten mit dem Inhalt..
            if (filterString != null)
            {
                query += " WHERE Content LIKE '" + filterString + "'";
            }

            //aktuell gesperrte Nachrichten (), wenn heute kein Feiertag ist
            if (filterString != null && RegardTiming && !HelperClass.Feiertage(DateTime.Now).Contains(DateTime.Now.Date) )
            {
                // Wenn jetzt später ist als StartHour UND jetzt früher ist als EndHour
                query += "AND StartHour < strftime('%H', 'now', 'localtime') AND EndHour > strftime('%H', 'now')";
            }
            
            return ExecuteRead(query);
        }

        internal void CreateBlockedMessage(string content, int startHour = 20, int endHour = 6, int workdaysOnly = 0)
        {
            string query = "INSERT INTO BlockedMessages(Content, StartHour, EndHour, WorkdaysOnly) VALUES (@Content, @StartHour, @EndHour, @WorkdaysOnly)";

            var args = new Dictionary<string, object>
                {
                    {"@Content", content },
                    {"@StartHour", startHour},
                    {"@EndHour", endHour},
                    {"@WorkdaysOnly", workdaysOnly }
                };

            //Wurde kein neuer Eintrag erzeugt?
            if (ExecuteWrite(query, args) == 0)
            {
                Log.Write(Log.Type.Internal, "Nachricht konnte nicht gesperrt werden: " + content);
            }

        }

        /// <summary>
        /// Entfernt die geblockte Nachricht aus der Sperrliste.
        /// </summary>
        /// <param name="MessageId">ID der Nachricht in der Sperrliste</param>
        internal void DeleteBlockedMessage(uint MessageId)
        {
            string query = "DELETE FROM BlockedMessages WHERE ID = @id";

            var args = new Dictionary<string, object>
                {
                    {"@id",  MessageId }
                };

            if (ExecuteWrite(query, args) == 0)
            {
                string content = GetFirstEntryFromColumn("BlockedMessages", "Content", "ID = " + MessageId);
                Log.Write(Log.Type.Internal, "Sperre der Nachricht [" + MessageId + "] konnte nicht aufgehoben werden. Inhalt:\r\n"  + content);
            }
        }

        internal void GetInactivePersons()
            
        #endregion


    }
}
