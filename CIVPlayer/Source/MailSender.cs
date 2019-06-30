using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace CIVPlayer.Source
{
    public class MailSender
    {
        private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string subject;
        private SmtpClient smtp;
        private MailAddress fromAddress;

        public MailSender(string fromMail, string fromPassword, string subjectTemplate)
        {
            fromAddress = new MailAddress(fromMail);
            subject = subjectTemplate;
            smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword),
                Timeout = 20000
            };
            log.Info("MailSender created with sending address: " + fromMail);
        }

        public bool sendMail(MailAddress toAddress, String messageBody)
        {
            log.Info("Sending email to " + toAddress.Address);
            MailMessage ms = new MailMessage(fromAddress, toAddress);
            ms.Subject = subject;
            ms.Body = messageBody;
            try
            {
                smtp.Send(ms);
                log.Info("Email sent.");
            }
            catch (Exception e)
            {
                log.Error("Error while sending email", e);
                log.Info("Trying to resend");
                try
                {
                    smtp.Send(ms);
                    log.Info("Email sent.");
                }
                catch (Exception e2)
                {
                    log.Error("Email sending unsuccessful", e2);
                    return false;
                }
            }

            return true;
        }
    }
}
