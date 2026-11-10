using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;

namespace CoreSkybandECR
{
    public class LogFile
    {
        /// <summary>
        /// This method is used to set log level and log details
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="isLogsEnabled"></param>
        /// <param name="logPath"></param>
        /// <param name="daysToRetainLogs"></param>
        public static void SetLogOptions(int logLevel, bool isLogsEnabled, string logPath, int daysToRetainLogs)
        {
            Log.Debug("Enter SetLogOptions method");

            if (isLogsEnabled)
            {
                if (!Directory.Exists(logPath))
                {
                    try
                    {
                        Directory.CreateDirectory(logPath);
                        Log.Information("Log path created: " + logPath);
                    }
                    catch (Exception e)
                    {
                        HandleLogError(e);
                        return;
                    }
                }
            }

            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            string fileName = $"NamiPayECRNet_{currentDate}.log";
            string filePath = Path.Combine(logPath, fileName);
            Log.Information($"File path: {filePath}");

            LogEventLevel logEventLevel = GetLogLevel(logLevel);
            Log.Information("Log Level: " + logEventLevel.ToString());

            Log.CloseAndFlush();

            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.File(filePath, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}]  [{Level:u3}] [{SourceContext}]  {Message}{NewLine}{Exception}");

            if (logLevel == 1)
            {
                loggerConfiguration.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error);
            }
            else if (logLevel == 3)
            {
                loggerConfiguration.MinimumLevel.Information();
            }
            else
            {
                var ls = new LoggingLevelSwitch { MinimumLevel = LogEventLevel.Fatal };
                loggerConfiguration.MinimumLevel.ControlledBy(ls);
            }

            Log.Logger = loggerConfiguration.CreateLogger();

            try
            {
                DateTime expirationDate = DateTime.Now.AddDays(-daysToRetainLogs);
                DirectoryInfo logDirectory = new DirectoryInfo(logPath);

                foreach (FileInfo file in logDirectory.GetFiles("NamiPayECRNet_*.log"))
                {
                    if (file.LastWriteTime < expirationDate)
                    {
                        file.Delete();
                        Log.Information("Logs Deleted successfully: " + file);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error | Failed to manage log files: " + e.Message);
            }
        }

        private static void HandleLogError(Exception e)
        {
            Log.Error($"Error creating log directory: {e.Message}");
        }

        public static bool deleteFileifNeeded(string logPath, string fileToDelete, string expirationDate)
        {
            bool isDeleted = false;
            try
            {
                DirectoryInfo logDirectory = new DirectoryInfo(logPath);

                if (DateTime.Now.Date > DateTime.Parse(expirationDate))
                {
                    foreach (FileInfo file in logDirectory.GetFiles(fileToDelete))
                    {
                        Log.CloseAndFlush();
                        file.Delete();
                        Log.Information("Logs Deleted successfully: " + file);
                        isDeleted = true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error | Failed to manage log files: " + e.Message);
            }

            return isDeleted;
        }

        private static LogEventLevel GetLogLevel(int logLevel)
        {
            switch (logLevel)
            {
                case 0: return LogEventLevel.Fatal;
                case 1: return LogEventLevel.Error;
                case 2: return LogEventLevel.Warning;
                case 3: return LogEventLevel.Information;
                case 4: return LogEventLevel.Debug;
                case 5: return LogEventLevel.Verbose;
                default: return LogEventLevel.Information;
            }
        }
    }
}
