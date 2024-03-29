namespace PT
{
    public static class Constants
    {
        // Logging
        public const string PT_LOG = "Pro-Trades.log";
        public const string ROOT_LINUX_DIR = "/root";
        public const string LOCAL_LOGGER_NAME = "localLog";
        public const string LOGGER_BASIC_TARGET_LAYOUT = "${longdate} | ${level:uppercase=true:padding=5} | ${callsite} | ${message} | ${exception:format=type,tostring}";
        public static readonly string LOGGER_DEFAULT_FILE_LOG_LEVEL = LogLevel.Debug.ToString();
        public const string TIME_TEMPLATE = "{0:00}:{1:00}:{2:00}.{3:00}";
        public const string LOGGER_INVALID_LOG_LEVEL = "Invalid log level {0} | Using default LogLevel {1} | Exception error {2}";
        public const string LOGGER_SETUP_VALUES = "Logging initialized with FileLoggingEnabled: {0}, FileLogLevel: {1}, LogFilePath: {2}";
        public const string LOG_TIMESTAMP = "{0} | {1}";

        // Encryption & Security
        public const string SESSION_EXP_DATE = "12/12/2024";
        public const string SESSION_KEY = "SessionKey";
        public const string SESSION_SALT = "SessionSalt";
        public const string PASSWORD_EXP_DATE = "12/12/2024";
        public const string PASSWORD_KEY = "PasswordKey";
        public const string PASSWORD_SALT = "PasswordSalt";
        public const string KEY_GENERATION_FAILED = "Encryption failed to generate key.";
        public const string AUTH_HEADER = "authorization";
        public const string PT_CORS = "PTCORS";

        // API identifiers and other constant values
        public const string ALPACA_KEY_ID = "APCA-API-KEY-ID";
        public const string ALPACA_SECRET_KEY = "APCA-API-SECRET-KEY";
        public static readonly int DEFAULT_HISTORY_DAYS = 375;
        public static readonly decimal DEFAULT_VOLUME_USD_DISQUALIFYING_LIMIT = 1000000.0M;


        public static readonly string RECOVER_PASSWORD_EMAIL_TITLE = "Pro-Trades: Recover password";
        public static readonly string RECOVER_PASSWORD_EMAIL_BODY = "Hello {0}, here's your one time passcode: {1}";
        public static readonly string INVALID_VERIFICATION_TOKEN = "Auth Error: invalid verification token.";
        public static readonly string TOKEN_EXPIRED = "Token has expired.";
        public static readonly string EMAIL_AUTH_ERROR = "Auth Error: Email not tied to an existing out.";
    }
}
