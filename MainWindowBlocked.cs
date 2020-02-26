using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MelBox2_2
{
    public partial class MainWindow : Window
    {
		private static DataTable _BlockedMessages;

		public static DataTable BlockedMessages
		{
			get { return _BlockedMessages; }
			set 
			{ 
				_BlockedMessages = value;
				NotifyStaticPropertyChanged(nameof(BlockedMessages));
			}
		}

		/// <summary>
		/// Filtert die Anzeige gesperrter Nachrichten
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Blocked_Button_Filter_Click(object sender, RoutedEventArgs e)
		{
			string filterstring = Blocked_TextBlock_Filter.Text;
			//Keinen Filter setzen
			if (filterstring.Length < 3) filterstring = null;

			BlockedMessages = sql.GetBlockedMessages(filterstring);
		}

		/// <summary>
		/// Entfernt eine Nachricht aus der Sperrliste
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Blocked_Button_Release_Click(object sender, RoutedEventArgs e)
		{
			
			DataRowView selectedRow = (DataRowView)Blocked_DataGrid.SelectedItems[0];
			if (selectedRow == null) return;

			string selectedContent = selectedRow.Row.Field<string>(0);
			uint MessageId = sql.GetIdFromEntry("BlockedMessages", "Content", selectedContent);

			if (MessageId == 0)
			{
				MessageBox.Show("Die Nachricht mit diesem Inhalt wurde nicht gefunden:\r\n\r\n" + selectedContent);
				return;
			}

		    MessageBoxResult boxResult = MessageBox.Show("Nachricht [" + MessageId + "] wieder scharfschalten?\r\n\r\n" + selectedContent, "Nachricht freigeben?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

			if (boxResult == MessageBoxResult.Yes)
			{
				sql.DeleteBlockedMessage(MessageId);
			}

			BlockedMessages = sql.GetBlockedMessages();

		}

	}
}
