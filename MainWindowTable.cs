using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MelBox2_2
{
    public partial class MainWindow : Window
    {

        private static DataTable _Tab_DataTable;
        public static DataTable Tab_DataTable
        {
            get { return _Tab_DataTable; }
            set
            {
                _Tab_DataTable = value;
                NotifyStaticPropertyChanged(nameof(Tab_DataTable));
            }
        }


        private void Tab_ComboBoxTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ( int.TryParse( Tab_TextBox_NumRows.Text, out int numRows ) )
            {
                Tab_DataTable = sql.GetLastEntries(e.AddedItems[0].ToString(), "ID", numRows);
            }
            
        }

    }
}
