namespace PT
{
    public static class Constants
    {
        // Logging
        public const string PT_LOG_FILE = "Pro-Trades.log";
        public const string ROOT_LINUX_DIR = "/root";
        public const string LOCAL_LOGGER_NAME = "localLog";
        public const string LOGGER_BASIC_TARGET_LAYOUT = "${longdate} | ${level:uppercase=true:padding=5} | ${callsite} | ${message} | ${exception:format=type,tostring}";
        public static readonly string LOGGER_DEFAULT_FILE_LOG_LEVEL = LogLevel.Debug.ToString();
        public const string TIME_TEMPLATE = "{0:00}:{1:00}:{2:00}.{3:00}";
        public const string LOGGER_INVALID_LOG_LEVEL = "Invalid log level {0} | Using default LogLevel {1} | Exception error {2}";
        public const string LOGGER_SETUP_VALUES = "Logging initialized with FileLoggingEnabled: {0}, FileLogLevel: {1}, LogFilePath: {2}";
        public const string LOG_TIMESTAMP = "{0} | {1}";

        // Encryption & Security
        public const string SESSION_EXP_DATE = "12/12/2028";
        public const string SESSION_KEY = "SessionKey";
        public const string SESSION_SALT = "SessionSalt";
        public const string PASSWORD_EXP_DATE = "12/12/2028";
        public const string PASSWORD_KEY = "PasswordKey";
        public const string PASSWORD_SALT = "PasswordSalt";
        public const string KEY_GENERATION_FAILED = "Encryption failed to generate key.";
        public const string AUTH_HEADER = "authorization";
        public const string PT_CORS = "PTCORS";

        // API identifiers and other constant values
        public const string ALPACA_KEY_ID = "APCA-API-KEY-ID";
        public const string ALPACA_SECRET_KEY = "APCA-API-SECRET-KEY";
        public static readonly string DEFAULT_RED = "Red";
        public static readonly string DEFAULT_GREEN = "Green";
        public const int PRIME_GATE = 83;
        public const int HEALTHY_SHORT_INTEREST_PCT = 17;
        public const int DEFAULT_HISTORY_DAYS = 375;
        public const int DEFAULT_LOOKBACK_DAYS = 7;
        public const int INVALID_COMPOSITE = -1; // Used when composite fails, i.e. Ratings Composite
        public const decimal SIGNAL_CONSTANT = 3.7M; // Min 0, Max 7 depending on overall market conditions (bullish 0, bearish 7)
        public const int THIRTY_DAYS = 30;
        public const int TEN_DAYS = 10;
        public const decimal HUNDRED = 100.0M;
        public const decimal THIRTY = 30.0M;
        public const decimal TEN = 10.0M;
        public const decimal FIVE = 5.0M;
        public const decimal THREE = 3.0M;
        public const decimal TWO = 2.0M;
        public const decimal FIB = 0.618M;
        public const decimal HALF = 0.5M;
        public const decimal BT_AVG_WEEK_DIFF_PERCENT = 0.03M;
        public const decimal PENALTY = (decimal) (-1 * Math.PI);
        public const decimal BONUS = (decimal) Math.PI;

        // Dollar volume and price disqualification limits
        public static readonly decimal DEFAULT_VOLUME_USD_1D_LIMIT = 900000.0M;
        public static readonly decimal DEFAULT_VOLUME_USD_10D_LIMIT = 600000.0M;
        public static readonly decimal DEFAULT_VOLUME_USD_30D_LIMIT = 300000.0M;
        public static readonly decimal DEFAULT_PENNY_PRICE_D_LIMIT = 2.5M;
        public static readonly int DEFAULT_MIN_PASS_30D_LIMIT = 24;
        public static readonly int DEFAULT_MIN_PASS_10D_LIMIT = 8;

        // Long messages
        public static readonly string RECOVER_PASSWORD_EMAIL_TITLE = "Pro-Trades: Recover password";
        public static readonly string RECOVER_PASSWORD_EMAIL_BODY = "Hello {0}, here's your one time passcode: {1}";
        public static readonly string INVALID_VERIFICATION_TOKEN = "Auth Error: invalid verification token.";
        public static readonly string TOKEN_EXPIRED = "Token has expired.";
        public static readonly string EMAIL_AUTH_ERROR = "Auth Error: Email not tied to an existing out.";

        // Prediction rankings
        public static readonly string RANK_DISQUALIFIED = "DISQUALIFIED";
        public static readonly string RANK_SHORT = "SHORT";
        public static readonly string RANK_BAD = "BAD";
        public static readonly string RANK_NEUTRAL = "NEUTRAL";
        public static readonly string RANK_FAIR = "FAIR";
        public static readonly string RANK_GOOD = "GOOD";
        public static readonly string RANK_PRIME = "PRIME";
        public static readonly string RANK_E = "-E";

        // HS Parameter Set Types
        public static readonly string HS1 = "HS1";
        public static readonly string HS1_SHORT_DESCRIPTION = "Pure Form";
        public static readonly string HS1_LONG_DESCRIPTION = "The oldest original prediction parameter set with the best recorded accuracy";
        public static readonly string HS1_SET = "adx_aroon_obv_macd_short_fund_hedge";

        public static readonly string HS2 = "HS2";
        public static readonly string HS2_SHORT_DESCRIPTION = "BBANDS OBV Swap";
        public static readonly string HS2_LONG_DESCRIPTION = "2nd generation expanded parameter set for Bollinger Bands signal plays";
        public static readonly string HS2_SET = "adx_aroon_bbands_macd_short_fund_hedge";

        public static readonly string HS3 = "HS3";
        public static readonly string HS3_SHORT_DESCRIPTION = "BBANDS AROON Swap";
        public static readonly string HS3_LONG_DESCRIPTION = "3rd generation expanded parameter set for Bollinger Bands signal plays";
        public static readonly string HS3_SET = "adx_bbands_obv_macd_short_fund_hedge";

        public static readonly string HS4 = "HS4";
        public static readonly string HS4_SHORT_DESCRIPTION = "Fundamentals Not Found";
        public static readonly string HS4_LONG_DESCRIPTION = "2nd generation parameter set for defensive case, not well studied";
        public static readonly string HS4_SET = "adx_aroon_obv_bbands_macd_short_hedge";

        public static readonly string HS5 = "HS5";
        public static readonly string HS5_SHORT_DESCRIPTION = "Financial Instruments";
        public static readonly string HS5_LONG_DESCRIPTION = "2nd generation parameter set usually for financial instruments like index funds or other defensive cases";
        public static readonly string HS5_SET = "adx_aroon_obv_bbands_macd_short_fund";

        // Indicator Composites
        public const string COMPOSITE_ADX = "ADX";
        public const string COMPOSITE_AROON = "AROON";
        public const string COMPOSITE_BBANDS = "BBANDS";
        public const string COMPOSITE_OBV = "OBV";
        public const string COMPOSITE_MACD = "MACD";
    }
}
