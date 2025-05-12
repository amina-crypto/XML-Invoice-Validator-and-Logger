using System;
using System.Collections.Generic;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace ConsoleApp__XMLInvoice_ValidatorAndLogger
{
    public abstract class LoggableOperation
    {
        public readonly List<string> SuccessLogBuffer = new List<string>();
        public readonly List<string> ErrorLogBuffer = new List<string>();
        public bool OperationSuccessful = true;
        public readonly ILogger SuccessLogger;
        public readonly ILogger ErrorLogger;

        public LoggableOperation()
        {
            var successSink = new SuccessSink(SuccessLogBuffer);
            var errorSink = new ErrorSink(ErrorLogBuffer);

            SuccessLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(successSink)
                .CreateLogger();

            ErrorLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(errorSink)
                .WriteTo.Console()
                .CreateLogger();

            Log.Logger = ErrorLogger;
        }

        protected void WriteLogs()
        {
            if (OperationSuccessful)
            {
                using (var successFileLogger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/success-log-.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger())
                {
                    foreach (var logEntry in SuccessLogBuffer)
                    {
                        successFileLogger.Information(logEntry);
                    }
                }
            }
            else
            {
                using (var errorFileLogger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/error-log-.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger())
                {
                    foreach (var logEntry in ErrorLogBuffer)
                    {
                        errorFileLogger.Information(logEntry);
                    }
                }
            }
        }
    }
}
