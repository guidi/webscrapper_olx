using System;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace OLXWebScraper.Service
{
    public static class EmailService
    {
        public static Boolean EnviarEmail(String assuntoEmail, String mensagem)
        {
            Boolean enviou = false;

            var destinatario = Environment.GetEnvironmentVariable("OLX_SCRAPPER_DESTINATARIO");
            var host = Environment.GetEnvironmentVariable("OLX_SCRAPPER_HOST_EMAIL");
            var porta = Environment.GetEnvironmentVariable("OLX_SCRAPPER_PORTA_EMAIL");
            Int32.TryParse(porta, out int PortaInt);
            var usuario = Environment.GetEnvironmentVariable("OLX_SCRAPPER_USUARIO_EMAIL");
            var remetente = Environment.GetEnvironmentVariable("OLX_SCRAPPER_REMETENTE_EMAIL");
            var senha = Environment.GetEnvironmentVariable("OLX_SCRAPPER_SENHA_EMAIL");

            using (SmtpClient client = new SmtpClient())
            {
                client.Port = PortaInt;
                client.Host = host;
                client.EnableSsl = true;
                client.Timeout = 15000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(usuario, senha);

                MailMessage mm = new MailMessage(remetente, destinatario, assuntoEmail, mensagem);
                mm.IsBodyHtml = true;
                mm.BodyEncoding = UTF8Encoding.UTF8;
                mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                client.Send(mm);
                enviou = true;
            }

            return enviou;
        }
    }
}
