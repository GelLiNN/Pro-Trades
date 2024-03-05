using LogLevel = NLog.LogLevel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PT.Models.RequestModels;

namespace PT.Middleware
{
    public static class DecryptionHelper
    {
        private const string LICENSE_INVALID = "License from config.json is invalid: {0}";
        private const string LICENSE_EXPIRED = "License from config.json is Expired: {0}";
        private const string LICENSE_ERROR = "Unknown license error: {0}";
        private const string LICENSE_NEEDED = "You must add a valid License to config.json in order to run the application";
        private const string ARGUMENT_ERROR = "Encrypted argument does not match expected parameters";
        private const string NULL_KEY_ERROR = "Encrypted arguments are null or empty";

        public const string IV = "2195D9B8DF1EL117"; // 16 bytes

        /// <summary>
        /// Decrypts the passed encrypted token, checking for validity
        /// </summary>
        /// <param name="token">string encrypted user token</param>
        /// <param name="isPassword">true for password tokens, false for session tokens</param>
        /// <returns>true if license is valid, false otherwise</returns>
        /// <exception cref="ArgumentException">Provided license is not valid.</exception>
        /// <exception cref="FormatException">Format for license is not valid.</exception>
        public static DecryptedTokenItems DecryptUserToken(string token, bool isPassword)
        {
            try
            {
                var result = DecryptToken(token, isPassword);
                int dateDiff = DateTime.Compare(result.ExpireDate, DateTime.Now);

                // check if license is invalid
                if (DateTime.Compare(result.ExpireDate, new DateTime()) == 0)
                {
                    LogHelper.Log(LogLevel.Error, string.Format(LICENSE_INVALID, token));
                }

                // check if license is expired
                if (dateDiff < 0)
                {
                    LogHelper.Log(LogLevel.Error, string.Format(LICENSE_EXPIRED, token));
                }

                // check if license is gouda
                bool valid = dateDiff > 0;

                // print license expiration date
                LogHelper.Log(LogLevel.Info, $"Current License Expiration: {result.ExpireDate:MM/dd/yyyy}");
                result.DateValid = valid;
                return result;
            }
            catch (Exception e) when (e is ArgumentException || e is FormatException)
            {
                LogHelper.Log(LogLevel.Error, string.Format(LICENSE_ERROR, e.Message));
                LogHelper.Log(LogLevel.Error, LICENSE_NEEDED);
                return null;
            }
        }

        // Private helper to turn a Hex String into a byte[]
        private static byte[] HexStringToByteArray(this string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return Array.Empty<byte>();
            }

            var length = hex.Length;

            var bytes = new byte[length / 2];

            for (var i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        // Private helper to turn a string into a byte Array
        private static byte[] ToBytes(this string str) => string.IsNullOrEmpty(str) ? Array.Empty<byte>() : Encoding.ASCII.GetBytes(str);

        private static DecryptedTokenItems DecryptToken(string hexLicense, bool isPassword)
        {

            var cypherBytes = hexLicense.HexStringToByteArray();

            var temp = GetKeySaltPair(isPassword);
            if (!String.IsNullOrEmpty(temp.key) && !String.IsNullOrEmpty(temp.salt))
            {
                var license = DecryptRegistration(cypherBytes, temp.key.ToBytes(), IV.ToBytes());

                var date = license[^8..];
                DateTime dateObj = DateTime.ParseExact(date, "MMddyyyy", null, DateTimeStyles.None);
                String[] tokens = license.Split(".");

                DecryptedTokenItems items =  new DecryptedTokenItems
                {
                    Username = tokens[0],
                    ExpireDate = dateObj
                };
                items.Password = isPassword ? tokens[1] : String.Empty;
                return items;
            }
            else
            {
                // Error getting Key and Salt pair from config
                return new DecryptedTokenItems();
            }
        }

        // Private helper to Decrypt the AES byte[] 
        private static string DecryptRegistration(byte[] encrypted, byte[] key, byte[] iv)
        {
            // Check the inputs and throw exception if empty
            if (encrypted == null || key == null || iv == null)
            {
                throw new ArgumentException(NULL_KEY_ERROR);
            }

            if (encrypted.Length <= 0 || key.Length <= 0 || iv.Length <= 0)
            {
                throw new ArgumentException(ARGUMENT_ERROR);
            }

            string text;

            // Encrypt the string
            using (var aesDecrypt = Aes.Create())
            {
                if (aesDecrypt == null)
                {
                    throw new InvalidOperationException(Constants.KEY_GENERATION_FAILED);
                }

                aesDecrypt.Key = key;

                aesDecrypt.IV = iv;

                // Create a Decryptor
                var cryptoTransform = aesDecrypt.CreateDecryptor(aesDecrypt.Key, aesDecrypt.IV);

                // Stream for decryption
                using var memoryStream = new MemoryStream(encrypted);
                using var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream);

                // Read the stream data
                text = reader.ReadToEnd();
            }

            return text;
        }

        public static (string? key, string? salt) GetKeySaltPair(bool isPassword)
        {
            string key = string.Empty;
            string salt = string.Empty;
            if (isPassword)
            {
                key = Program.Config.GetValue<string>(Constants.PASSWORD_KEY);
                salt = Program.Config.GetValue<string>(Constants.PASSWORD_SALT);
            }
            else
            {
                key = Program.Config.GetValue<string>(Constants.SESSION_KEY);
                salt = Program.Config.GetValue<string>(Constants.SESSION_SALT);
            }
            return (key, salt);
        }
    }

    public partial class EncryptionHelper
    {
        protected EncryptionHelper() { }

        private const string DateSlashSeparator = "/";
        private const string DateDashSeparator = "-";

        // Input dates must match this regex
        [GeneratedRegex(@"^[0-9]{2}/[0-9]{2}/[0-9]{4}$")]
        private static partial Regex DateRegex();

        /// <summary>
        /// Uses the passed name string and desired expiration date to generate an encrypted AES key string
        /// </summary>
        /// <param name="username">Username token to encrypt with</param>
        /// <param name="pass">Password token to encrypt with</param>
        /// <param name="expDate">Date of expiration in format mm/dd/yyyy</param>
        /// <param name="isPassword">true for password tokens, false for session tokens</param>
        public static string CreateEncryptedKey(string username, string pass, string expDate, bool isPassword)
        {
            bool valid = ValidateInputs(username, pass, expDate);
            var temp = DecryptionHelper.GetKeySaltPair(isPassword);

            if (valid && !String.IsNullOrEmpty(temp.key) && !String.IsNullOrEmpty(temp.salt))
            {
                string token = isPassword ? $"{username}.{pass}." : $"{username}.";
                string key = GenerateKeyHelper(token, expDate, temp.key, temp.salt);
                return key;
            }
            return string.Empty;
        }

        /// <summary>
        /// Method to decompose key generation logic
        /// </summary>
        /// <param name="token"></param>
        /// <param name="expiration"></param>
        /// <param name="k"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        /// <exception cref="SystemException"></exception>
        private static string GenerateKeyHelper(string token, string expiration, string k, string s)
        {
            var time = CleanDate(expiration);

            var plainText = token + s + time;

            if (string.IsNullOrEmpty(plainText))
            {
                throw new InvalidOperationException("Invalid key generation arguments");
            }

            using var aes = Aes.Create();
            aes.Key = StrToBytes(k);
            aes.IV = StrToBytes(DecryptionHelper.IV);

            var cipherText = EncryptRegistration(plainText, aes.Key, aes.IV);
            return GetKeyFromCipher(cipherText);
        }

        /// <summary>
        /// Lambda to validate inputs
        /// </summary>
        /// <param name="username"></param>
        /// <param name="pass"></param>
        /// <param name="date"></param>
        /// <returns>True if the name is not empty and the Date matches the Regex</returns>
        private static bool ValidateInputs(string username, string pass, string date) =>
            !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(pass) && DateRegex().IsMatch(date);

        /// <summary>
        /// Lambda to encode string into byte array
        /// </summary>
        /// <param name="str"></param>
        /// <returns>An empty byte array if the string is empty. Else the byte[] for the encoded string</returns>
        private static byte[] StrToBytes(string str) => str is null ? Array.Empty<byte>() : Encoding.ASCII.GetBytes(str);

        /// <summary>
        /// Method to generate an Encrypted Registration Key for the DeepArmor SDK
        /// </summary>
        /// <param name="text"></param>
        /// <param name="Key"></param>
        /// <param name="IV"></param>
        /// <returns>byte[] for the encrypted registration</returns>
        /// <exception cref="ArgumentException"></exception>
        private static byte[] EncryptRegistration(string text, byte[] Key, byte[] IV)
        {
            // Check the inputs and throw exception if empty
            if (text is null || text.Length <= 0)
            {
                throw new ArgumentNullException(nameof(text), text);
            }

            if (Key is null || Key.Length <= 0)
            {
                throw new ArgumentNullException(nameof(Key));
            }

            if (IV is null || IV.Length <= 0)
            {
                throw new ArgumentNullException(nameof(IV));
            }

            byte[] encryptedBytes;

            // Encrypt the string
            using (var aesEncrypt = Aes.Create())
            {
                aesEncrypt.Key = Key;
                aesEncrypt.IV = IV;

                // Stream transform decryptor
                var cryptoTransform = aesEncrypt.CreateEncryptor(aesEncrypt.Key, aesEncrypt.IV);

                // Create the streams for encryption
                using var memoryStream = new MemoryStream();
                using var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write);
                using (var writer = new StreamWriter(cryptoStream))
                {
                    //Write all data to the stream.
                    writer.Write(text);
                }

                encryptedBytes = memoryStream.ToArray();
            }

            // Return the encrypted bytes from the memory stream.
            return encryptedBytes;
        }

        /// <summary>
        /// Get the key from the cipher
        /// </summary>
        /// <param name="ciphertext"></param>
        /// <returns>Cipher string</returns>
        private static string GetKeyFromCipher(byte[] ciphertext)
        {
            return ciphertext == null || ciphertext.Length == 0
                ? string.Empty
                : BitConverter.ToString(ciphertext).Replace(DateDashSeparator, string.Empty);
        }

        /// <summary>
        /// Clean up the input date. Removes "/" and "-" characters
        /// </summary>
        /// <param name="expiration"></param>
        /// <returns>A clean date string</returns>
        private static string CleanDate(string expiration)
        {
            if (!expiration.Contains(DateSlashSeparator))
            {
                return string.Empty;
            }

            var split = expiration.Split(DateSlashSeparator);

            var date = split.Aggregate(string.Empty, (current, element) => current + element.PadLeft(2, '0') + DateDashSeparator);

            date = date[..^1];

            return date.Replace(DateSlashSeparator, string.Empty).Replace(DateDashSeparator, string.Empty);
        }
    }
}
