using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace UniversalColdWallet
{
    public static class PrivateKeyGene
    {
        public static void ShowPrivateKeys(UniversalColdWallet wallet)
        {
            if (wallet == null)
            {
                Console.WriteLine("Hata: Cüzdan bulunamadı!");
                return;
            }

            Console.Clear();
            Console.WriteLine("=== Özel Anahtar Gösterimi ===");
            Console.WriteLine("UYARI: Özel anahtarlarınız çok değerlidir!");
            Console.WriteLine("Bu bilgileri güvenli bir ortamda görüntülediğinizden emin olun.");
            Console.WriteLine("Özel anahtarları ASLA paylaşmayın veya kopyalamayın!\n");

            // Kullanıcıdan onay al
            Console.Write("Özel anahtarları görüntülemek istediğinize emin misiniz? (E/H): ");
            var confirm = Console.ReadLine()?.Trim().ToUpper();
            if (confirm != "E")
            {
                Console.WriteLine("\nİşlem iptal edildi.");
                return;
            }

            // Şifre gerekiyorsa iste
            if (wallet.HasPassword)
            {
                Console.Write("\nCüzdan şifresini giriniz: ");
                string password = ReadPassword();
                wallet.SetCurrentPassword(password);
            }

            var coins = wallet.GetSupportedCoins();
            Console.WriteLine("\nÖzel Anahtarlar:");
            Console.WriteLine("================");

            var privateKeys = new Dictionary<string, string>();
            var errors = new List<string>();

            // Önce tüm private keyleri topla
            foreach (var coin in coins)
            {
                try
                {
                    var privateKey = wallet.GetPrivateKey(coin);
                    if (!string.IsNullOrEmpty(privateKey))
                    {
                        privateKeys[coin] = privateKey;
                    }
                    else
                    {
                        errors.Add($"{coin}: Özel anahtar alınamadı");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{coin}: {ex.Message}");
                }
            }

            // Tüm private keyleri göster
            foreach (var kvp in privateKeys)
            {
                Console.WriteLine($"\n{kvp.Key} Özel Anahtarı:");
                Console.WriteLine(kvp.Value);

                // Her coin için format bilgisi göster
                switch (kvp.Key)
                {
                    case "BTC":
                    case "LTC":
                    case "BCH":
                    case "DOGE":
                        Console.WriteLine($"\nBu {kvp.Key} özel anahtarı WIF (Wallet Import Format) formatındadır.");
                        Console.WriteLine($"Çoğu {kvp.Key} cüzdanı bu formatı doğrudan destekler.");
                        break;

                    case "ETH":
                    case "BNB_BSC":
                    case "USDT":
                    case "USDT_BEP20":
                    case "SHIB":
                        Console.WriteLine($"\nBu {kvp.Key} özel anahtarı hexadecimal formatındadır (0x ile başlar).");
                        Console.WriteLine("MetaMask veya benzeri cüzdanlarda kullanılabilir.");
                        break;

                    case "XRP":
                        Console.WriteLine("\nBu XRP özel anahtarı özel format kullanır.");
                        Console.WriteLine("Ripple cüzdanlarında Secret Key olarak kullanılabilir.");
                        break;

                    case "ADA":
                        Console.WriteLine("\nBu Cardano özel anahtarı Ed25519 Extended formatındadır.");
                        Console.WriteLine("Daedalus veya Yoroi cüzdanlarında kullanılabilir.");
                        break;

                    case "SOL":
                        Console.WriteLine("\nBu Solana özel anahtarı Base58 formatındadır.");
                        Console.WriteLine("Phantom veya Solflare gibi cüzdanlarda kullanılabilir.");
                        break;

                    case "USDT_TRC20":
                        Console.WriteLine("\nBu TRON özel anahtarı hexadecimal formatındadır.");
                        Console.WriteLine("TronLink veya benzeri cüzdanlarda kullanılabilir.");
                        break;
                }
            }

            // Hataları göster
            if (errors.Count > 0)
            {
                Console.WriteLine("\nHatalar:");
                Console.WriteLine("========");
                foreach (var error in errors)
                {
                    Console.WriteLine($"- {error}");
                }
            }

            Console.WriteLine("\nÖnemli Güvenlik Uyarıları:");
            Console.WriteLine("1. Bu anahtarları güvenli bir yerde saklayın");
            Console.WriteLine("2. Asla dijital ortamda saklamayın");
            Console.WriteLine("3. Hiç kimseyle paylaşmayın");
            Console.WriteLine("4. Fotoğrafını çekmeyin");
        }

        private static string ReadPassword()
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
    }
}