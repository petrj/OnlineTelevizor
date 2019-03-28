using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NLog;
using NLog.Config;

namespace LoggerService
{
    public class BasicLoggingService : ILoggingService
    {
        private string _logFileName;
        private LoggingLevelEnum _minLevel;

        public BasicLoggingService(LoggingLevelEnum minLevel = LoggingLevelEnum.Debug)
        {
            MinLevel = minLevel;
        }

        public string LogFilename
        {
            get
            {
                return _logFileName;
            }
            set
            {
                _logFileName = value;
            }
        }

        public LoggingLevelEnum MinLevel { get => _minLevel; set => _minLevel = value; }

        private void WriteToLogFile(LoggingLevelEnum level, string message)
        {
            try
            {
                if ((int)level < (int)MinLevel)
                    return;

                string msg = $"[{DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss")}] {level} {message}";

                using (var fs = new FileStream(LogFilename, FileMode.Append, FileAccess.Write))
                {
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.WriteLine(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error while writing log to {LogFilename} ({ex})");
            }
        }

        public void Debug(string message)
        {
            WriteToLogFile(LoggingLevelEnum.Debug, message);
        }

        public void Warn(string message)
        {
            WriteToLogFile(LoggingLevelEnum.Warning, message);
        }

        public void Info(string message)
        {
            WriteToLogFile(LoggingLevelEnum.Info, message);
        }

        public void Error(Exception ex, string message)
        {
            WriteToLogFile(LoggingLevelEnum.Error, $"{message} {ex}");
        }
    }
}
