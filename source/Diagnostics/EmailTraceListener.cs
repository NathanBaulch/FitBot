using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using FitBot.Properties;

namespace FitBot.Diagnostics
{
    public class EmailTraceListener : TraceListener
    {
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            TraceEvent(eventCache, source, eventType, id, message, new object[0]);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
            {
                return;
            }

            var client = new SmtpClient(Settings.Default.SmtpServer)
                {
                    Credentials = new NetworkCredential(Settings.Default.SmtpUsername, Settings.Default.SmtpPassword),
                    EnableSsl = true
                };
            var body = string.Format(format, args);
            var firstLine = new StringReader(body).ReadLine();
            if (firstLine.Length > 100)
            {
                firstLine = firstLine.Substring(0, 97).Trim() + "...";
            }
            var msg = new MailMessage
                {
                    From = new MailAddress(Settings.Default.NotificationFrom, "FitBot"),
                    To = {Settings.Default.NotificationTo},
                    Subject = $"{eventType} - {firstLine}",
                    Body = body
                };
            client.Send(msg);
        }

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
        }
    }
}