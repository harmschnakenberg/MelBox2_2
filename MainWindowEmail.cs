using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MelBox2_2
{
    public partial class MainWindow : Window
    {
        #region Felder
        public static string SmtpServer { get; set; } = "192.168.165.29";
        public static string MelBoxAdminEmailAddress { get; set; } = "harm.schnakenberg@kreutztraeger.de";

       // private const string BccReciever = "harm.schnakenberg@kreutztraeger.de";

        public static MailAddress SMSCenter { get; set; } = new MailAddress("SMSZentrale@Kreutztraeger.de", "SMS Zentrale Kreutzträger Kältetechnik"); //"SMSZentrale@Kreutztraeger.de";
        public static MailAddress MelBoxAdmin { get; set; } = new MailAddress(MelBoxAdminEmailAddress, "MelBox2Admin");

        #endregion

        public static void SendEmail(MailAddressCollection to, string subject, string body)
        {
            //per SmtpClient Funktioniert.
            using (SmtpClient smtpClient = new SmtpClient())
            {
                smtpClient.Host = SmtpServer;
                smtpClient.Port = 25;
                smtpClient.Credentials = CredentialCache.DefaultNetworkCredentials;

                using (MailMessage message = new MailMessage())
                {
                    message.From = SMSCenter;

                    foreach (MailAddress toAddress in to)
                    {
                        message.To.Add(toAddress);
                    }

                    //BAUSTELLE SilentListeners hier eintragen?
                    //if (to.Where(x => x.Address == BccReciever).Count() == 0)
                    //{
                    //    message.Bcc.Add(BccReciever);
                    //}

                    //Betreff
                    int subjectLenth = 255;
                    if (subject.Length < 255) subjectLenth = subject.Length;
                    message.Subject = subject.Substring(0, subjectLenth).Replace("\r\n", string.Empty);

                    //Nachricht
                    message.Body = body;

                    try
                    {
                        smtpClient.Send(message);
                    }
                    catch (SmtpException ex)
                    {
                        StatusBarText = "Senden der Mail fehlgeschlagen: " + subject;
                        Log.Write(Log.Type.Email, "Senden der Mail fehlgeschlagen: " + ex.Message);
                    }
                }
            }
        }

        //public static void SendEmail(Message message, MailAddressCollection to)
        //{
        //    per SmtpClient Funktioniert.
        //    using (SmtpClient smtpClient = new SmtpClient())
        //    {
        //        smtpClient.Host = MainWindow.SmtpServer;
        //        smtpClient.Port = 25;
        //        smtpClient.Credentials = CredentialCache.DefaultNetworkCredentials;

        //        using (MailMessage mailMessage = new MailMessage())
        //        {
        //            mailMessage.From = SMSCenter;

        //            foreach (MailAddress toAddress in to)
        //            {
        //                mailMessage.To.Add(toAddress);
        //            }

        //            MailAddress BBCreciever = new MailAddress(BccReciever);

        //            if (to.Where(x => x.Address == BccReciever).Count() == 0)
        //            {
        //                mailMessage.Bcc.Add(BccReciever);
        //            }

        //            string body = string.Empty;

        //            if (message.CustomerKeyWord != null) body += message.CustomerKeyWord + " | ";
        //            body += message.Content;

        //            int subjectLenth = 255;
        //            if (body.Length < 255) subjectLenth = body.Length;

        //            mailMessage.Subject = body.Substring(0, subjectLenth).Replace("\r\n", string.Empty);
        //            mailMessage.Body = message.Content;

        //            try
        //            {
        //                smtpClient.Send(mailMessage);
        //            }
        //            catch (SmtpException ex)
        //            {
        //                MainWindow.StatusBarText = "Senden der Mail fehlgeschlagen: " + mailMessage.Subject;
        //                Log.Write(Log.Type.Email, "Senden der Mail fehlgeschlagen: " + ex.Message);
        //            }
        //        }
        //    }
        //}

        internal async static Task<MessageCollection> ReadEmailsAsync()
        {
            try
            {
                MessageCollection messages = new MessageCollection();

                //using (var client = new ImapClient(new ProtocolLogger("imap.log")))
                using (var client = new ImapClient())
                {

                    client.Connect("imap.gmx.net", 993, SecureSocketOptions.SslOnConnect);
                    client.Authenticate("harmschnakenberg@gmx.de", "Oyterdamm64!");

                    client.Inbox.Open(FolderAccess.ReadOnly);
                    var uids = client.Inbox.Search(SearchQuery.New); //SearchQuery.New / .All

                    foreach (var uid in uids)
                    {
                        CancellationTokenSource source = new CancellationTokenSource();
                        //Timeout nach 10 Sekunden:
                        source.CancelAfter(TimeSpan.FromSeconds(10));
                        Message msg = await ReadEmailAsync(client, uid, source.Token);

                        messages.Add(msg);
                    }

                    client.Disconnect(true);

                    return messages;
                }
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        static Task<Message> ReadEmailAsync(ImapClient client, UniqueId uid, CancellationToken token)
        {
            return Task.Run<Message>(() =>
            {               
                Message message = new Message();

                MimeKit.MimeMessage newEmail = client.Inbox.GetMessage(uid);

                message.SentTime = (ulong)newEmail.Date.ToUnixTimeSeconds();

                MimeKit.MailboxAddress fromAdress = newEmail.From.Mailboxes.FirstOrDefault();
                message.EMail = fromAdress.Address;

                string content = "KEIN INHALT";
                if (newEmail.TextBody != null && newEmail.TextBody.Length > 5)
                {
                    content = newEmail.TextBody;
                }

                message.Content = content;

                message.Type = (ushort)MessageType.RecievedFromEmail;

                token.ThrowIfCancellationRequested();
           
                return message;
            });
        }
    
    }
}
