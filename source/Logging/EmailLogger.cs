using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace FitBot.Logging
{
    public static class EmailLoggerExtensions
    {
        public static ILoggingBuilder AddEmailLogger(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, EmailLoggerProvider>());
            LoggerProviderOptions.RegisterProviderOptions<EmailLoggerOptions, EmailLoggerProvider>(builder.Services);
            return builder;
        }
    }

    [ProviderAlias("Email")]
    public sealed class EmailLoggerProvider : ILoggerProvider
    {
        private readonly EmailLoggerOptions _options;
        private readonly ConcurrentDictionary<string, EmailLogger> _loggers = new();

        public EmailLoggerProvider(IOptions<EmailLoggerOptions> options) => _options = options.Value;

        public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, _ => new EmailLogger(_options));

        public void Dispose() => _loggers.Clear();
    }

    public class EmailLogger : ILogger
    {
        private readonly EmailLoggerOptions _options;

        public EmailLogger(EmailLoggerOptions options) => _options = options;

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var client = new SmtpClient(_options.Server)
                {
                    Credentials = new NetworkCredential(_options.Username, _options.Password),
                    EnableSsl = true
                };
            var text = new StringReader(formatter(state, exception)).ReadLine();
            if (text?.Length > 100)
            {
                text = text.Substring(0, 97).Trim() + "...";
            }
            var msg = new MailMessage
                {
                    From = new MailAddress(_options.From, "FitBot"),
                    To = {_options.To},
                    Subject = $"{logLevel} - {text}",
                    Body = exception?.ToString() ?? string.Empty
                };
            try
            {
                client.Send(msg);
            }
            catch
            {
                // ignored
            }
        }
    }

    public class EmailLoggerOptions
    {
        public string Server { get; init; }
        public string Username { get; init; }
        public string Password { get; init; }
        public string From { get; init; }
        public string To { get; init; }
    }
}