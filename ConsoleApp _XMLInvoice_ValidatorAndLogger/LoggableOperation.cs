using System;
using System.Collections.Generic;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace ConsoleApp__XMLInvoice_ValidatorAndLogger
{
    public abstract class LoggableOperation
    {
        public readonly List<string> SuccessLogBuffer = new List<string>();
        public readonly List<string> ErrorLogBuffer = new List<string>();
        public readonly List<string> TechnicalSuccessLogBuffer = new List<string>();
        public readonly List<string> TechnicalErrorLogBuffer = new List<string>();
        public readonly List<string> UserErrorLogBuffer = new List<string>();
        public bool OperationSuccessful = true;
        public readonly ILogger SuccessLogger;
        public readonly ILogger ErrorLogger;
        public readonly ILogger TechnicalSuccessLogger;
        public readonly ILogger TechnicalErrorLogger;
        public readonly ILogger UserErrorLogger;

        public LoggableOperation()
        {
            var successSink = new SuccessSink(SuccessLogBuffer);
            var errorSink = new ErrorSink(ErrorLogBuffer);
            var technicalSuccessSink = new TechnicalSuccessSink(TechnicalSuccessLogBuffer);
            var technicalErrorSink = new TechnicalErrorSink(TechnicalErrorLogBuffer);
         
            SuccessLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(successSink)
                .CreateLogger();

            ErrorLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(errorSink)
                .WriteTo.Console()
                .CreateLogger();

            TechnicalSuccessLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(technicalSuccessSink)
                .CreateLogger();

            TechnicalErrorLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(technicalErrorSink)
                .CreateLogger();


            Log.Logger = ErrorLogger;
        }

        public void WriteLogs(string result, TimeSpan elapsed)
        {
            string date = DateTime.Now.ToString("yyyyMMdd");
            string hoursMinutes = DateTime.Now.ToString("HHmm");
            string elapsedTime = $"{elapsed.Hours:D2}h{elapsed.Minutes:D2}m{elapsed.Seconds:D2}s";
            string execDir = AppDomain.CurrentDomain.BaseDirectory; // Directory where .exe is located
            Directory.CreateDirectory(Path.Combine(execDir, "logs"));
            Console.WriteLine($"Il percorso della directory dell'applicazione è: {execDir}");

            if (OperationSuccessful)
            {
                using (var successFileLogger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(Path.Combine(execDir, "logs", $"{date}_{result}_User_{hoursMinutes}_{elapsedTime}.txt"), rollingInterval: RollingInterval.Day)
                    .CreateLogger())
                {
                    foreach (var logEntry in SuccessLogBuffer)
                    {
                        successFileLogger.Information(logEntry);
                    }
                }

                using (var technicalSuccessFileLogger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(Path.Combine(execDir, "logs", $"{date}_{result}_Technical_{hoursMinutes}_{elapsedTime}.txt"), rollingInterval: RollingInterval.Day)
                    .CreateLogger())
                {
                    foreach (var logEntry in TechnicalSuccessLogBuffer)
                    {
                        technicalSuccessFileLogger.Information(logEntry);
                    }
                }
            }
            else
            {
                using (var errorFileLogger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(Path.Combine(execDir, "logs", $"{date}_{result}_User_{hoursMinutes}_{elapsedTime}.txt"), rollingInterval: RollingInterval.Day)
                    .CreateLogger())
                {
                    foreach (var logEntry in UserErrorLogBuffer)
                    {
                        errorFileLogger.Information(logEntry);
                    }
                }

                using (var technicalErrorFileLogger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(Path.Combine(execDir, "logs", $"{date}_{result}_TechnicalError_{hoursMinutes}_{elapsedTime}.txt"), rollingInterval: RollingInterval.Day)
                    .CreateLogger())
                {
                    foreach (var logEntry in TechnicalErrorLogBuffer)
                    {
                        technicalErrorFileLogger.Information(logEntry);
                    }
                }
            }
        }
    }
}