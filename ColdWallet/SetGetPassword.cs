using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UniversalColdWallet
{
    public class SetGetPassword
    {
        private const int SALT_SIZE = 32;
        private const int ITERATION_COUNT = 210000;
        private static readonly byte[] DEFAULT_SALT = new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };

        public static string GenerateSecurePassword()
        {
            const string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";
            var password = new StringBuilder();

            using var rng = RandomNumberGenerator.Create()
                ?? throw new InvalidOperationException("Güvenli rastgele sayı üreteci oluşturulamadı.");

            byte[] randomBytes = new byte[16];
            rng.GetBytes(randomBytes);

            for (int i = 0; i < 16; i++)
            {
                password.Append(allowedChars[randomBytes[i] % allowedChars.Length]);
            }

            var result = password.ToString();
            if (!ValidatePasswordStrength(result))
            {
                return GenerateSecurePassword();
            }

            return result;
        }

        public static string EncryptMnemonic(string mnemonic, string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(mnemonic, nameof(mnemonic));
            ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));

            if (!ValidatePasswordStrength(password))
            {
                throw new ArgumentException("şifre güvenlik kriterlerini karşılamıyor.", nameof(password));
            }

            return EncryptString(mnemonic, password);
        }

        public static bool VerifyPassword(string? encryptedMnemonic, string? password, string? originalMnemonic)
        {
            if (string.IsNullOrEmpty(encryptedMnemonic) ||
                string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(originalMnemonic))
            {
                return false;
            }

            try
            {
                var decryptedMnemonic = DecryptString(encryptedMnemonic, password);
                return decryptedMnemonic == originalMnemonic;
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string ChangePassword(string currentEncryptedMnemonic, string currentPassword,
            string newPassword, string originalMnemonic)
        {
            ArgumentException.ThrowIfNullOrEmpty(currentEncryptedMnemonic, nameof(currentEncryptedMnemonic));
            ArgumentException.ThrowIfNullOrEmpty(currentPassword, nameof(currentPassword));
            ArgumentException.ThrowIfNullOrEmpty(newPassword, nameof(newPassword));
            ArgumentException.ThrowIfNullOrEmpty(originalMnemonic, nameof(originalMnemonic));

            if (!VerifyPassword(currentEncryptedMnemonic, currentPassword, originalMnemonic))
            {
                throw new ArgumentException("Mevcut şifre yanlış.");
            }

            if (!ValidatePasswordStrength(newPassword))
            {
                throw new ArgumentException("Yeni şifre güvenlik kriterlerini karşılamıyor.");
            }

            return EncryptString(originalMnemonic, newPassword);
        }

        public static void DisplayNewPassword(string? password)
        {
            ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));

            Console.WriteLine("\n=== Otomatik Oluşturulan Güvenli Şifre ===");
            Console.WriteLine($"şifreniz: {password}");
            Console.WriteLine("Bu şifreyi güvenli bir yerde saklayın!");
            Console.WriteLine("NOT: Bu şifre sadece bir kez gösterilecektir.");
            Console.WriteLine("\nşifre güvenlik kriterleri:");
            Console.WriteLine("- En az 8 karakter uzunluğunda");
            Console.WriteLine("- En az 1 büyük harf");
            Console.WriteLine("- En az 1 küçük harf");
            Console.WriteLine("- En az 1 rakam");
            Console.WriteLine("- En az 1 özel karakter");
        }

        public static bool ValidatePasswordStrength(string? password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            bool hasNumber = false;
            bool hasUpper = false;
            bool hasLower = false;
            bool hasSpecial = false;

            foreach (char c in password)
            {
                if (char.IsDigit(c)) hasNumber = true;
                else if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (!char.IsLetterOrDigit(c)) hasSpecial = true;
            }

            return hasNumber && hasUpper && hasLower && hasSpecial;
        }

        public static string EncryptString(string text, string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(text, nameof(text));
            ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));

            byte[] data = Encoding.UTF8.GetBytes(text);

            using var aes = Aes.Create()
                ?? throw new InvalidOperationException("AES şifreleme sağlayıcısı oluşturulamadı.");

            using var key = new Rfc2898DeriveBytes(password, DEFAULT_SALT, ITERATION_COUNT, HashAlgorithmName.SHA256);
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using var encryptor = aes.CreateEncryptor()
                ?? throw new InvalidOperationException("şifreleme sağlayıcısı oluşturulamadı.");
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public static string DecryptString(string encryptedText, string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(encryptedText, nameof(encryptedText));
            ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));

            byte[] data;
            try
            {
                data = Convert.FromBase64String(encryptedText);
            }
            catch (FormatException)
            {
                throw new ArgumentException("şifrelenmiş metin geçerli bir formatta değil.", nameof(encryptedText));
            }

            using var aes = Aes.Create()
                ?? throw new InvalidOperationException("AES şifreleme sağlayıcısı oluşturulamadı.");

            using var key = new Rfc2898DeriveBytes(password, DEFAULT_SALT, ITERATION_COUNT, HashAlgorithmName.SHA256);
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using var decryptor = aes.CreateDecryptor()
                ?? throw new InvalidOperationException("şifre çzme sağlayıcısı oluşturulamadı.");
            using var ms = new MemoryStream(data);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs);

            try
            {
                return reader.ReadToEnd();
            }
            catch (CryptographicException)
            {
                throw new ArgumentException("şifre yanlış veya veri bozuk.");
            }
        }

        public static string ReadPassword()
        {
            var password = new StringBuilder();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Length--;
                    Console.Write("\b \b");
                }
            }
            while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password.ToString();
        }

        public static bool ChangePasswordManually(UniversalColdWallet wallet, out string? newPassword)
        {
            ArgumentNullException.ThrowIfNull(wallet);
            newPassword = null;

            Console.WriteLine("\n=== Elle şifre Değiştirme ===");
            Console.Write("Mevcut şifre: ");
            var currentPassword = ReadPassword();

            if (string.IsNullOrEmpty(currentPassword))
            {
                Console.WriteLine("\nHata: şifre boş olamaz!");
                return false;
            }

            Console.Write("\nYeni şifre: ");
            newPassword = ReadPassword();

            if (string.IsNullOrEmpty(newPassword))
            {
                Console.WriteLine("\nHata: Yeni şifre boş olamaz!");
                return false;
            }

            if (!ValidatePasswordStrength(newPassword))
            {
                Console.WriteLine("\nHata: Yeni şifre yeterince güçlü değil!");
                Console.WriteLine("şifre en az 8 karakter uzunluğunda olmalı ve");
                Console.WriteLine("büyük harf, küçük harf, rakam ve özel karakter içermelidir.");
                return false;
            }

            Console.Write("Yeni şifre (tekrar): ");
            var confirmPassword = ReadPassword();

            if (string.IsNullOrEmpty(confirmPassword))
            {
                Console.WriteLine("\nHata: Onay şifresi boş olamaz!");
                return false;
            }

            if (newPassword != confirmPassword)
            {
                Console.WriteLine("\nHata: Yeni şifreler eşleşmiyor!");
                return false;
            }

            try
            {
                wallet.ChangePassword(currentPassword, newPassword);
                Console.WriteLine("\nşifre başarıyla değiştirildi!");
                return true;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"\nHata: {ex.Message}");
                return false;
            }
        }

        public static bool ChangePasswordAutomatically(UniversalColdWallet wallet, out string? newPassword)
        {
            ArgumentNullException.ThrowIfNull(wallet);
            newPassword = null;

            Console.WriteLine("\n=== Otomatik şifre Değiştirme ===");
            Console.Write("Mevcut şifre: ");
            var currentPassword = ReadPassword();

            if (string.IsNullOrEmpty(currentPassword))
            {
                Console.WriteLine("\nHata: Mevcut şifre boş olamaz!");
                return false;
            }

            try
            {
                newPassword = GenerateSecurePassword();
                if (string.IsNullOrEmpty(newPassword))
                {
                    throw new InvalidOperationException("Güvenli şifre oluşturulamadı!");
                }

                wallet.ChangePassword(currentPassword, newPassword);
                DisplayNewPassword(newPassword);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nHata: {ex.Message}");
                return false;
            }
        }

        public static bool AddPassword(UniversalColdWallet wallet, out string? newPassword)
        {
            ArgumentNullException.ThrowIfNull(wallet);
            newPassword = null;

            if (wallet.HasPassword)
            {
                Console.WriteLine("\nCzdan zaten şifreli!");
                return false;
            }

            Console.Write("\nYeni şifre: ");
            newPassword = ReadPassword();

            if (string.IsNullOrEmpty(newPassword))
            {
                Console.WriteLine("\nHata: şifre boş olamaz!");
                return false;
            }

            if (!ValidatePasswordStrength(newPassword))
            {
                Console.WriteLine("\nHata: şifre yeterince güçlü değil!");
                Console.WriteLine("şifre en az 8 karakter uzunluğunda olmalı ve");
                Console.WriteLine("büyük harf, küçük harf, rakam ve özel karakter içermelidir.");
                return false;
            }

            try
            {
                wallet.EncryptMnemonic(newPassword);
                Console.WriteLine("şifre başarıyla eklendi!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nHata: {ex.Message}");
                return false;
            }
        }

        public static string DecryptMnemonic(string encryptedMnemonic, string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(encryptedMnemonic, nameof(encryptedMnemonic));
            ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));

            return DecryptString(encryptedMnemonic, password);
        }
    }
}