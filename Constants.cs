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

        // Long messages
        public static readonly string RECOVER_PASSWORD_EMAIL_TITLE = "Pro-Trades: Recover password";
        public static readonly string RECOVER_PASSWORD_EMAIL_BODY = "Hello {0}, here's your one time passcode: {1}";
        public static readonly string INVALID_VERIFICATION_TOKEN = "Auth Error: invalid verification token.";
        public static readonly string TOKEN_EXPIRED = "Token has expired.";
        public static readonly string EMAIL_AUTH_ERROR = "Auth Error: Email not tied to an existing account.";

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
        public static readonly string DEFAULT_DATA_PROVIDERS = "YahooFinance, Alpaca, FINRA, TipRanks";
        public static readonly string FORMAT_ROUND_2 = "0.00";
        public static readonly string FORMAT_CURRENCY = "C2";
        public const int DEFAULT_HISTORY_DAYS = 300; // Was 277 before 12.22.2025, 375 before 7.20.2025, trying 300
        public const int DEFAULT_LOOKBACK_DAYS = 7;
        public const decimal TARGET_AVG_WEEK_DIFF_PERCENT = 0.03M;

        // Numbers
        public const decimal TEN_THOUSAND = 10000.0M;
        public const decimal THIRTY_THOUSAND = 30000.0M;
        public const decimal FIFTY_THOUSAND = 50000.0M;
        public const decimal TEN_BILLION = 10000000000.0M;
        public const decimal ONE_BILLION = 1000000000.0M;
        public const decimal MILLION = 1000000.0M;
        public const decimal ONE_HUNDRED = 100.0M;
        public const decimal TWO_HUNDRED = 200.0M;
        public const decimal FIFTY = 50.0M;
        public const decimal THIRTY = 30.0M;
        public const decimal TWENTY = 20.0M;
        public const decimal TEN = 10.0M;
        public const decimal FIVE = 5.0M;
        public const decimal THREE = 3.0M;
        public const decimal TWO = 2.0M;
        public const decimal ONE = 1.0M;
        public const decimal FIB = 0.618M;
        public const decimal HALF = 0.5M;
        public const decimal THIRD = 0.3333333M;
        public const decimal FIFTH = 0.2M;

        #region Core Model Constants
        //TODO: if 82.9 or higher round up to 83.0 Prime
        public const decimal CORE_PENALTY = (decimal)(-1 * Math.PI);
        public const decimal CORE_BONUS = (decimal)Math.PI;
        public const int CORE_INVALID_COMP = -1; // To denote GRU composites which resulted in error
        public const decimal CORE_PRIME_GATE = 83.0M;
        public const decimal CORE_PRIME_RND_LIMIT = 82.75M;
        public const decimal CORE_SIGNAL_MOD = 3.7M; // Uber bullish macros 1, Uber bearish macros 7
        public const decimal CORE_HS1_MOD = ONE; // 0, 0.333, 0.5, 1.0 default, handicapped mode BONUS * half
        public const decimal CORE_HS2_MOD = CORE_BONUS - 1; // -.5, 0, 0.5 default, handicapped mode 1
        //public const decimal CORE_HS2_MOD = CORE_BONUS * TWO - 1.5M; // CORE_EXP_MODE
        public const decimal CORE_HS3_MOD = CORE_BONUS - 2; // BONUS * HALF, BONUS - 1 default, handicapped mode BONUS - half
        //public const decimal CORE_HS3_MOD = CORE_BONUS * TWO - 1.5M; // CORE_EXP_MODE

        public const decimal CORE_HS4_MOD = 0.0M;
        public const decimal CORE_HS5_MOD = CORE_BONUS * HALF - HALF; // BONUS * 0.5 default, handicapped mode BONUS - 1
        public const decimal CORE_HS6_MOD = CORE_PENALTY * 2;
        public const decimal CORE_EXP_MOD_SNAP = 84.0M;
        public const bool CORE_EXP_MOD_ENABLED = false; // Experimental HS1/HS2/HS3 extra post GRU composite mods mode

        // GRU composite gate constants
        public const decimal SHORT_HEALTHY_VOL_PERCENT = 17.0M;
        public const int OBV_LOOKBACK_DAYS = 42; // Was 42 before 7.20.2025, tried 37 until 9.4.2025
        public const decimal BBANDS_COMP_MID_LIMIT = 70 + (CORE_BONUS * HALF);
        public const decimal FUND_NER_INVERSE_MULTIPLIER_PERCENT = 0.07M;
        public const decimal FUND_NER_MAJOR_LIMIT_PERCENT = 0.5M;
        public const decimal FUND_NER_MINOR_LIMIT_PERCENT = 0.2M;
        public const decimal FUND_EPS_MOD_UPPER_LIMIT = 37.0M;
        public const decimal FUND_PE_MOD_UPPER_LIMIT = 33.0M;
        public const decimal FUND_PE_MOD_LOWER_LIMIT = -20.0M;
        public const int FUND_HANDICAP = 21; // For when YahooQuotesApi is broken

        // Dollar volume and price disqualification limits
        public static readonly decimal DEFAULT_VOLUME_USD_1D_LIMIT = 875000.0M;
        public static readonly decimal DEFAULT_VOLUME_USD_10D_LIMIT = 650000.0M;
        public static readonly decimal DEFAULT_VOLUME_USD_30D_LIMIT = 375000.0M;
        public static readonly decimal DEFAULT_PENNY_PRICE_D_LIMIT = 2.5M;
        public static readonly decimal DEFAULT_MCAP_D_LIMIT = .075M; // 75 million (in billions)
        public static readonly int DEFAULT_MIN_PASS_30D_LIMIT = 24;
        public static readonly int DEFAULT_MIN_PASS_10D_LIMIT = 8;

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
        public static readonly string HS1_LONG_DESCRIPTION = "The 1st generation original prediction parameter set with the most historical data";
        public static readonly string HS1_SET = "adx_aroon_obv_macd_short_fund_hedge";

        public static readonly string HS2 = "HS2";
        public static readonly string HS2_SHORT_DESCRIPTION = "BBANDS OBV Swap";
        public static readonly string HS2_LONG_DESCRIPTION = "2nd generation expanded parameter set for Bollinger Bands signal setups instead of OBV";
        public static readonly string HS2_SET = "adx_aroon_bbands_macd_short_fund_hedge";

        public static readonly string HS3 = "HS3";
        public static readonly string HS3_SHORT_DESCRIPTION = "BBANDS AROON Swap";
        public static readonly string HS3_LONG_DESCRIPTION = "3rd generation expanded parameter set for Bollinger Bands signal setups instead of Aroon";
        public static readonly string HS3_SET = "adx_bbands_obv_macd_short_fund_hedge";

        public static readonly string HS4 = "HS4";
        public static readonly string HS4_SHORT_DESCRIPTION = "Institution Driven";
        public static readonly string HS4_LONG_DESCRIPTION = "3rd generation parameter set for fundamentals defensive cases, not well studied";
        public static readonly string HS4_SET = "adx_aroon_obv_bbands_macd_short_hedge";

        public static readonly string HS5 = "HS5";
        public static readonly string HS5_SHORT_DESCRIPTION = "Financial Instruments";
        public static readonly string HS5_LONG_DESCRIPTION = "2nd generation parameter set usually for financial instruments, like index funds or hedge funds";
        public static readonly string HS5_SET = "adx_aroon_obv_bbands_macd_short_fund";

        public static readonly string HS6 = "HS6";
        public static readonly string HS6_SHORT_DESCRIPTION = "Raw Signals";
        public static readonly string HS6_LONG_DESCRIPTION = "4th generation parameter set, Fundamentals and Ratings error case for signals-only, not well studied";
        public static readonly string HS6_SET = "adx_aroon_obv_bbands_macd_short";

        // Indicator Composites
        public const string COMPOSITE_ADX = "ADX";
        public const string COMPOSITE_AROON = "AROON";
        public const string COMPOSITE_BBANDS = "BBANDS";
        public const string COMPOSITE_OBV = "OBV";
        public const string COMPOSITE_MACD = "MACD";
        #endregion
    }
}
