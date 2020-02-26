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

        private void FillTabMasterData(Dictionary<string, object> dict)
        {
            if (dict.Count == 0) return;

            string company = "_UNBEKANNT_";
            if (uint.TryParse(dict["CompanyID"].ToString(), out uint companyId))
            {
                company = sql.GetCompanyNameFromId(companyId);
            }

            if (!uint.TryParse(dict["MessageType"].ToString(), out uint roleId))
            {
                roleId = 1;
            }

            Mast_TextBlock_Person_Id.Text = dict["ID"].ToString();
            Mast_TextBox_Name.Text = dict["Name"].ToString();
            Mast_ComboBox_Company.SelectedValue = company;
            Mast_TextBox_Email.Text = dict["Email"].ToString();
            Mast_TextBox_Cellphone.Text = "+" + dict["Cellphone"].ToString();
            Mast_TextBox_KeyWord.Text = dict["KeyWord"].ToString();
            Mast_TextBox_MaxInactivity.Text = dict["MaxInactive"].ToString();
            //string messageTypeName = Enum.GetName(typeof(MessageType), roleId);
            //Mast_ComboBox_Person_Role.SelectedValue = messageTypeName;

            Mast_CheckBox_RecievesEmail.IsChecked = IsEmailReciever(roleId);
            Mast_CheckBox_RecievesSMS.IsChecked = IsSMSReciever(roleId);

        }

        #region Persons

        /// <summary>
        /// Liest Personendaten ein, wenn die Combobox mit unbekannten Benutzern geändetr wird.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Mast_ComboBox_UnknownPersons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string identifier = "_UNBEKANNT_";
            if (Mast_ComboBox_UnknownPersons.SelectedIndex != -1)
            {
                identifier = Mast_ComboBox_UnknownPersons.SelectedValue.ToString();
            }

            string query = "SELECT ID, Name, CompanyID, Email, Cellphone, KeyWord, MaxInactive, MessageType FROM Persons WHERE Email =@email OR KeyWord = @keyword OR Cellphone = @cellphone";

            Dictionary<string, object> dictArgs = new Dictionary<string, object>
            {
                { "@email", identifier },
                { "@keyword", identifier },
                { "@cellphone", identifier }
            };

            Dictionary<string, object> dict = sql.GetRowValues(query, dictArgs);

            if (dict.Count == 0) return;

            string company = "_UNBEKANNT_";
            if (uint.TryParse(dict["CompanyID"].ToString(), out uint companyId))
            {
                company = sql.GetCompanyNameFromId(companyId);
            }

            if (!uint.TryParse(dict["MessageType"].ToString(), out uint roleId))
            {
                roleId = 1;
            }

            //string messageTypeName = Enum.GetName(typeof(MessageType), roleId);

            Mast_TextBox_Name.Text = dict["Name"].ToString();
            Mast_ComboBox_Company.SelectedValue = company;
            Mast_TextBox_Email.Text = dict["Email"].ToString();
            Mast_TextBox_Cellphone.Text = "+" + dict["Cellphone"].ToString();
            Mast_TextBox_KeyWord.Text = dict["KeyWord"].ToString();
            Mast_TextBox_MaxInactivity.Text = dict["MaxInactive"].ToString();            
            Mast_CheckBox_RecievesEmail.IsChecked = IsEmailReciever(roleId);
            Mast_CheckBox_RecievesSMS.IsChecked = IsSMSReciever(roleId);


            FillTabMasterData(dict);
        }

        /// <summary>
        /// Sucht die Adressaten aus der Datenbank, für die kein Name gefunden wurde.
        /// </summary>
        internal void GetUnknownPersons()
        {
            Mast_ComboBox_UnknownPersons.ItemsSource = sql.GetUnknownPersons();

            if (Mast_ComboBox_UnknownPersons.HasItems)
            {
                Mast_ComboBox_UnknownPersons.IsEnabled = true;
                Mast_ComboBox_UnknownPersons.SelectedIndex = 0;
                Mast_Button_CreatePerson.IsEnabled = false;
            }
            else
            {
                Mast_ComboBox_UnknownPersons.IsEnabled = false;
                Mast_Button_CreatePerson.IsEnabled = true;
            }            
        }

        private void Mast_Button_SearchName_Click(object sender, RoutedEventArgs e)
        {
            string searchstring = Mast_TextBox_Name.Text;
            string query = "SELECT ID, Name, CompanyID, Email, Cellphone, KeyWord, MaxInactive, MessageType FROM Persons WHERE Name LIKE \"" + searchstring + "\" LIMIT 1";

            Dictionary<string, object> dict = sql.GetRowValues(query, null);

            FillTabMasterData(dict);
        }
        
        private void Mast_Button_CreatePerson_Click(object sender, RoutedEventArgs e)
        {
            if (Mast_ComboBox_Company.SelectedItem == null)
            {
                MessageBox.Show("Wähle eine Firmenzugehörigkeit aus!", "MelBox2", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (Mast_TextBox_Name.Text.Length < 3)
            {
                MessageBox.Show("Der Name muss mindestens drei Zeichen lang sein!", "MelBox2", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            string name = Mast_TextBox_Name.Text;
            string company = Mast_ComboBox_Company.SelectedValue.ToString();
            string email = Mast_TextBox_Email.Text;
            ulong cellphone = HelperClass.ConvertStringToPhonenumber( Mast_TextBox_Cellphone.Text );
            string KeyWord = Mast_TextBox_KeyWord.Text;
            string MaxInactive = Mast_TextBox_MaxInactivity.Text;
            bool recEmailChecked = (bool)Mast_CheckBox_RecievesEmail.IsChecked;
            bool recSmsChecked = (bool)Mast_CheckBox_RecievesSMS.IsChecked;

            if (recEmailChecked && !HelperClass.IsValidEmailAddress(email))
            {
                MessageBox.Show("E-Mails können nur mit gültiger Email-Adresse gesendet / empfangen werden!", "MelBox2", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (recSmsChecked && cellphone == 0)
            {
                MessageBox.Show("SMS können nur mit gültiger Telefonnummer gesendet / empfangen werden!", "MelBox2", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            MessageType role = MessageType.NoCategory; 

            if (recEmailChecked)
            {
                role = MessageType.SentToEmail; 
            }

            if (recSmsChecked)
            {
                role |= MessageType.SentToSms;
            }
            
            uint oldPersonId = sql.GetPersonID(null, email, cellphone , KeyWord);
         
            if (oldPersonId != 0 )
            {
                string oldName =  sql.GetPersonNameFromId(oldPersonId);
                MessageBox.Show(string.Format("Die Person mit \r\nEmail\t{0}\r\nMobilnummer\t{1}\r\nKeyWord\t{2}\r\n gibt es schon unter dem Namen\r\n\t\t{3}\r\n\r\nEs wird keine neue Person erstellt.", email, cellphone, KeyWord, oldName), "MelBox2 - Diese Person gibt es schon.", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
          
            uint newPersonId = sql.CreatePerson(name, company, email, cellphone.ToString() , KeyWord, MaxInactive, role);

            Mast_TextBlock_Person_Id.Text = newPersonId.ToString();

            if (newPersonId > 0)
            {
                _ = MessageBox.Show("Person neu erstellt:\r\n" + name + "\r\nvon Firma " + company, "MelBox2 - Person [" + newPersonId + "] neu erstellt.", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Write(Log.Type.Persons, "Person [" + newPersonId + "] neu erstellt: " + name + " von Firma " + company);
            }

            //Auswahl verfügbarerer Personen im Kalender aktualisieren.
            Cal_AvailablePersonal = sql.GetListFromColumn("Persons", "Name", "1=1 LIMIT 50");
        }

        private void Mast_Button_UpdatePerson_Click(object sender, RoutedEventArgs e)
        {
            if (uint.TryParse(Mast_TextBlock_Person_Id.Text, out uint personId))
            {
                if (Mast_ComboBox_Company.SelectedItem == null)
                {
                    MessageBox.Show("Wähle eine Firmenzugehörigkeit aus!", "MelBox2", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                if (Mast_TextBox_Name.Text.Length < 3)
                {
                    MessageBox.Show("Der Name muss mindestens drei Zeichen lang sein!", "MelBox2", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                string name = Mast_TextBox_Name.Text;
                string company = Mast_ComboBox_Company.SelectedValue.ToString();
                string email = Mast_TextBox_Email.Text;
                ulong cellphone = HelperClass.ConvertStringToPhonenumber(Mast_TextBox_Cellphone.Text);
                string keyWord = Mast_TextBox_KeyWord.Text;
                string maxInactive = Mast_TextBox_MaxInactivity.Text;
                bool recEmailChecked = (bool)Mast_CheckBox_RecievesEmail.IsChecked;
                bool recSmsChecked = (bool)Mast_CheckBox_RecievesSMS.IsChecked;

                if (recEmailChecked && !HelperClass.IsValidEmailAddress(email))
                {
                    MessageBox.Show("E-Mails können nur mit gültiger Email-Adresse gesendet / empfangen werden!", "MelBox2", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                if (recSmsChecked && cellphone == 0)
                {
                    MessageBox.Show("SMS können nur mit gültiger Telefonnummer gesendet / empfangen werden!", "MelBox2", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                MessageType role = MessageType.NoCategory;

                if (recEmailChecked)
                {
                    role = MessageType.SentToEmail;
                }

                if (recSmsChecked)
                {
                    role |= MessageType.SentToSms;
                }

                int r = sql.UpdatePerson(personId, name, company, email, cellphone.ToString(), keyWord, maxInactive, role);

                if (r > 0)
                {
                    _ = MessageBox.Show("Person geändert:\r\n" + name + "\r\nvon Firma " + company, "MelBox2 - Person [" + personId + "] geändert.", MessageBoxButton.OK, MessageBoxImage.Information);
                    Log.Write(Log.Type.Persons, "Person [" + personId + "] geändert: " + name + " von Firma " + company);
                }

                GetUnknownPersons();
            }
        }

        private void Mast_Button_DeletePerson_Click(object sender, RoutedEventArgs e)
        {
            string name = Mast_TextBox_Name.Text;

            MessageBoxResult result = MessageBox.Show("Wirklich Person " + name + " löschen?\r\nEmpfangene Nachrichrten können danach nicht mehr zugeordnet werden.", "MelBox2 - Wirklich Person löschen?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes && uint.TryParse(Mast_TextBlock_Person_Id.Text, out uint personId))
            {
                int r = sql.DeletePerson(personId);

                if (  r > 0 )
                {
                    _ = MessageBox.Show("Person " + name +" gelöscht.", "Person gelöscht.", MessageBoxButton.OK, MessageBoxImage.Information);
                    Log.Write(Log.Type.Persons, "Person [" + personId + "] " + name + " gelöscht.");
                }
            }
        }

        internal bool IsSMSReciever(uint roleId)
        {
            return (roleId == (uint)MessageType.SentToSms || roleId == (uint)MessageType.SentToEmailAndSMS);
        }

        internal bool IsEmailReciever(uint roleId)
        {
            return (roleId == (uint)MessageType.SentToEmail || roleId == (uint)MessageType.SentToEmailAndSMS);
        }

        #endregion

        #region Companies
        private void Mast_Button_DeleteCompany_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult r = MessageBox.Show("Wirklich Firmeninformation dauerhaft löschen?", "MelBox2 - Firmenadresse löschen?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r != MessageBoxResult.Yes) return;

            if (uint.TryParse(Mast_TextBox_Company_Id.Text, out uint companyId))
            {

                int affectedRows = sql.DeleteCompany(companyId);

                Mast_ComboBox_Company.ItemsSource = sql.GetListOfCompanies();

                if (affectedRows > 0)
                {
                    _ = MessageBox.Show("Eintrag bei ID " + companyId + " gelöscht aus Tabelle Companies", "MelBox2 - Tabelleneintrag gelöscht", MessageBoxButton.OK, MessageBoxImage.Information);
                    Log.Write(Log.Type.Persons, "Eintrag gelöscht: ID = " + companyId + " in Tabelle Companies");
                }
            }

            Mast_ComboBox_Company.ItemsSource = sql.GetListOfCompanies();
        }

        private void Mast_Button_UpdateCompany_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult r = MessageBox.Show("Wirklich Firmeninformation ändern?", "MelBox2 - Firmenadresse ändern?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r != MessageBoxResult.Yes) return;

            if (uint.TryParse(Mast_TextBox_Company_Id.Text, out uint companyId))
            {

                string name = Mast_TextBox_Company_Name.Text;
                string address = Mast_TextBox_Company_Address.Text;

                if (!int.TryParse(Mast_TextBox_Company_ZipCode.Text, out int zipCode))
                {
                    zipCode = 0;
                }

                string city = Mast_TextBox_Company_City.Text;

                int affectedRows = sql.UpdateCompany(companyId, name, address, zipCode, city);

                if (affectedRows > 0)
                {
                    _ = MessageBox.Show("Firmeneintrag bei [" + companyId + "] " + name + " geändert.", "MelBox2 -  Firmenadresse geändert.", MessageBoxButton.OK, MessageBoxImage.Information);
                    Log.Write(Log.Type.Persons, "Eintrag geändert bei [" + companyId + "] " + name + " in Tabelle Companies");
                }
            }
        }

        private void Mast_Button_CreateCompany_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult r = MessageBox.Show("Wirklich neue Firmeninformation erstellen?", "MelBox2 - Neue Firmenadresse anlegen?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r != MessageBoxResult.Yes) return;
                        
            string name = Mast_TextBox_Company_Name.Text;
            string address = Mast_TextBox_Company_Address.Text;

            if (!uint.TryParse(Mast_TextBox_Company_ZipCode.Text, out uint zipCode))
            {
                zipCode = 0;
            }

            string city = Mast_TextBox_Company_City.Text;

            uint affectedRows = sql.CreateCompany(name, address, zipCode, city);

            if (affectedRows > 0)
            {
                _ = MessageBox.Show("Neuer Firmeneintrag " + name + " erstellt.", "MelBox2 -  Firmenadresse neu erstelt.", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Write(Log.Type.Persons, "Neuer Eintrag für " + name + " in Tabelle Companies");
            }

            Mast_ComboBox_Company.ItemsSource = sql.GetListOfCompanies();

        }

        private void Mast_ComboBox_Company_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object selectedCompany = Mast_ComboBox_Company.SelectedValue;

            string companyName = "_UNBEKANNT_";
            if (selectedCompany != null)
            {
                companyName = selectedCompany.ToString();
            }

            string query = "SELECT * FROM Companies WHERE Name = \"" + companyName + "\"";

            Sql sql = new Sql();
            Dictionary<string, object> dict = sql.GetRowValues(query, null);

            if (dict == null) return; // Rausnehmen?
            uint.TryParse(dict["ID"].ToString(), out uint id);

            Mast_TextBox_Company_Id.Text = id.ToString();
            Mast_TextBox_Company_Name.Text = dict["Name"].ToString();
            Mast_TextBox_Company_Address.Text = dict["Address"].ToString();
            Mast_TextBox_Company_ZipCode.Text = dict["ZipCode"].ToString();
            Mast_TextBox_Company_City.Text = dict["City"].ToString();
        }

        #endregion
    }
}
