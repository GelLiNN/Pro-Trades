using NLog;
using NLog.Config;
using NLog.Targets;
using System.Diagnostics;
using LogLevel = NLog.LogLevel;

namespace PT.Middleware
{
    public static class LogHelper
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static LogLevel FileLoggingLevel = LogLevel.Debug;
        private static bool LoggerInitialized;
        public static TimerHelper LogTimer { get; set; } = new();

        public static string LogFilePath { get; set; } =
            Path.Combine(Program.ExecutingPath, Constants.PT_LOG);

        /// <summary>
        /// Initialize the Logger target to a local file and to the syslog system / server based on the config settings
        /// </summary>
        /// <exception cref="ArgumentException">Argument from method is not valid.</exception>
        public static void InitializeLogger(IConfiguration config)
        {
            LogTimer.StartTimer();
            var loggingConfig = new LoggingConfiguration();
            LogFilePath = Path.Combine(Program.ExecutingPath, Constants.PT_LOG);
            try
            {
                FileLoggingLevel = LogLevel.FromString(config.GetValue<string>("FileLoggingLevel"));
            }
            catch (ArgumentException ex)
            {
                var errorMessage = string.Format(Constants.LOGGER_INVALID_LOG_LEVEL, FileLoggingLevel,
                    Constants.LOGGER_DEFAULT_FILE_LOG_LEVEL, ex.Message);
                FileLoggingLevel = LogLevel.FromString(Constants.LOGGER_DEFAULT_FILE_LOG_LEVEL);
                Log(LogLevel.Error, errorMessage);
            }

            // If the config has fileLogging enabled
            if (config.GetValue<bool>("FileLoggingEnabled"))
            {
                var fileTarget = new FileTarget(Constants.LOCAL_LOGGER_NAME)
                {
                    FileName = LogFilePath,
                    Layout = Constants.LOGGER_BASIC_TARGET_LAYOUT
                };
                loggingConfig.AddRule(FileLoggingLevel, LogLevel.Fatal, fileTarget);
            }


            LogManager.Configuration = loggingConfig;

            Log(LogLevel.Debug, string.Format(Constants.LOGGER_SETUP_VALUES,
                config.GetValue<bool>("FileLoggingEnabled"), FileLoggingLevel.ToString(), LogFilePath));
            LoggerInitialized = true;
        }

        /// <summary>
        /// Log function to correctly log to all the targets that are enabled. 
        /// Also write out to the console
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        public static void Log(LogLevel logLevel, string message, bool stdOut = true)
        {
            message = string.Format(Constants.LOG_TIMESTAMP, LogTimer.GetTime(), message);
            if (LoggerInitialized)
            {
                logger.Log(logLevel, message);
            }

            // Also write out to the console
            if (stdOut && logLevel.Ordinal >= FileLoggingLevel.Ordinal)
            {
                Console.WriteLine(message);
            }
        }
    }

    public class TimerHelper
    {
        private readonly Stopwatch stopWatch;

        public TimerHelper()
        {
            stopWatch = new Stopwatch();
        }

        /// <summary>
        /// Start the internal timer
        /// </summary>
        public void StopTimer() => stopWatch.Stop();

        /// <summary>
        /// Stop the internal timer
        /// </summary>
        public void StartTimer() => stopWatch.Start();

        /// <summary>
        /// Get the timer delta in the following format {0:00}:{1:00}:{2:00}.{3:00}
        /// </summary>
        /// <returns></returns>
        public string GetTime()
        {
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = string.Format(Constants.TIME_TEMPLATE,
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            return elapsedTime;
        }
    }
}