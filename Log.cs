using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MelBox2_2
{
    public static class Log
    {
        [Flags]
        public enum Type
        {
            General,
            Persons,
            Calendar,
            Email,
            SMS,
            Internal
        }

        #region Felder
        public static int DebugWord { get; set; } = 255;

        private static readonly string TextLogPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Log", string.Format("Log{0:000}.txt", DateTime.Now.DayOfYear));
        #endregion

        public static void Write(Type type, string message)
        {
            MainWindow.StatusBarText = message;

            if (IsBitSet(DebugWord, (int)type))
            {
                Sql sql = new Sql();
                sql.CreateLogEntry(type.ToString(), message);

               //MainWindow.DataTableShow = sql.GetLastEntries("Log", "ID", 10);
            }
        }

        public static bool IsBitSet(int b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }

        public static void Text(Type type, string message)
        {
            
            using (System.IO.StreamWriter file = System.IO.File.AppendText(TextLogPath))
            {
                file.WriteLine(DateTime.Now.ToShortTimeString() + " - " + type + " - " + message);
            }
        }

    }

}
