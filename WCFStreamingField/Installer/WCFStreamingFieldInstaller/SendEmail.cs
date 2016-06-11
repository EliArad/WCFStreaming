using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;

namespace StreamingFieldInstaller
{
    public class SendEmail
    {
        public static void Send(string toEmailAddress, 
                                string toName, 
                                string subject, 
                                string body , 
                                string attachmentFile = "")
        {
            try
            {
                var fromAddress = new MailAddress("easp13@gmail.com", "Eli Arad");
                var toAddress = new MailAddress(toEmailAddress, toName);
                const string fromPassword = "sheep3628290123";
                const string userName = "easp13";

                SmtpClient smtp = new SmtpClient();

                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.EnableSsl = true;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(userName, fromPassword);
                System.Net.Mail.Attachment attachment = null;
                if (attachmentFile != string.Empty)
                {
                    attachment = new System.Net.Mail.Attachment(attachmentFile);
                }
                MailMessage message = new MailMessage(fromAddress, toAddress);

                message.Subject = subject;
                message.Body = body;
                if (attachmentFile != string.Empty)
                    message.Attachments.Add(attachment);
                smtp.Send(message);

            }
            catch (Exception err)
            {
                throw (new SystemException(err.Message));
            }
        }
    }
}
