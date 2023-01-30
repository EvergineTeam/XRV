// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.IO;
using Evergine.Framework.Services;
using Evergine.Xrv.Core.Utils;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Evergine.Xrv.Core.Services.Logging
{
    internal class SerilogService : Service, ILogger
    {
        private const string LogTemplate = "[{Timestamp:HH:mm:ss.fff} {Level:u3}] ({ThreadID}) {Message:lj} {Properties:j}{NewLine}{Exception}";
        private readonly ILogger logger;
        private readonly LoggingConfiguration configuration;

        public SerilogService(LoggingConfiguration configuration)
        {
            this.configuration = configuration;
            if (this.configuration == null)
            {
                return;
            }

            var serilogConfiguration = new LoggerConfiguration()
                .Enrich.With(new ThreadIdEnricher())
                .MinimumLevel.Is(this.ConvertToSerilogLevel(this.configuration.LogLevel))
                .WriteTo.Debug(outputTemplate: LogTemplate);
            serilogConfiguration = this.ConfigureFileLogging(serilogConfiguration);

            var serilogLogger = serilogConfiguration.CreateLogger();
            var serilogFactory = new SerilogLoggerFactory(serilogLogger);

            this.logger = serilogFactory.CreateLogger("XRV");
            Serilog.Log.Logger = serilogLogger;
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull =>
            this.logger?.BeginScope(state);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => this.logger?.Log(logLevel, eventId, state, exception, formatter);

        bool ILogger.IsEnabled(LogLevel logLevel) =>
            this.logger?.IsEnabled(logLevel) ?? false;

        private LoggerConfiguration ConfigureFileLogging(LoggerConfiguration serilogConfiguration)
        {
            if (!this.configuration.EnableFileLogging)
            {
                return serilogConfiguration;
            }

            var fileOptions = this.configuration.FileOptions;
            if (fileOptions.MaxFileSize.HasValue)
            {
                var filePath = Path.Combine(DeviceHelper.GetLocalApplicationFolderPath(), "logs", fileOptions.FileName);
                serilogConfiguration = serilogConfiguration
                    .WriteTo.File(
                        filePath,
                        outputTemplate: LogTemplate,
                        fileSizeLimitBytes: fileOptions.MaxFileSize,
                        rollOnFileSizeLimit: true,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 10);
            }

            return serilogConfiguration;
        }

        private LogEventLevel ConvertToSerilogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return LogEventLevel.Verbose;
                case LogLevel.Debug:
                    return LogEventLevel.Debug;
                case LogLevel.Information:
                    return LogEventLevel.Information;
                case LogLevel.Warning:
                    return LogEventLevel.Warning;
                case LogLevel.Error:
                    return LogEventLevel.Error;
                case LogLevel.Critical:
                    return LogEventLevel.Fatal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }
    }
}
