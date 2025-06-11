using NBitcoin;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UniversalColdWallet
{
    public static class SummonWallet
    {
        public static UniversalColdWallet RecoverFromSeedPhrase(string seedPhrase, string? newPassword = null)
        {
            // Seed phrase doðrulama
            ValidateSeedPhrase(seedPhrase);

            try
            {
                // Yeni cüzdan oluþtur
                var wallet = new UniversalColdWallet(seedPhrase);

                // Eðer yeni þifre belirlendiyse, cüzdaný þifrele
                if (!string.IsNullOrEmpty(newPassword))
                {
                    if (!SetGetPassword.ValidatePasswordStrength(newPassword))
                    {
                        throw new ArgumentException("Yeni þifre güvenlik kriterlerini karþýlamýyor.");
                    }

                    wallet.EncryptMnemonic(newPassword);
                }

                return wallet;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Cüzdan kurtarma baþarýsýz: " + ex.Message, ex);
            }
        }

        public static bool ValidateSeedPhrase(string seedPhrase)
        {
            if (string.IsNullOrWhiteSpace(seedPhrase))
                throw new ArgumentException("Tohum cümlesi boþ olamaz.");

            // Kelime sayýsýný kontrol et (12, 15, 18, 21, 24 kelime olabilir)
            var words = seedPhrase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var validWordCounts = new[] { 12, 15, 18, 21, 24 };
            
            if (!Array.Exists(validWordCounts, x => x == words.Length))
            {
                throw new ArgumentException($"Geçersiz kelime sayýsý. Tohum cümlesi {string.Join(", ", validWordCounts)} kelimeden oluþmalýdýr.");
            }

            try
            {
                // BIP39 formatýna uygunluðunu kontrol et
                var mnemonic = new Mnemonic(seedPhrase, Wordlist.English);
                if (!mnemonic.IsValidChecksum)
                {
                    throw new ArgumentException("Tohum cümlesi geçerli bir BIP39 formatýnda deðil.");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Geçersiz tohum cümlesi: " + ex.Message, ex);
            }
        }

        public static void DisplayRecoveryInstructions()
        {
            Console.WriteLine("\n=== Cüzdan Kurtarma Talimatlarý ===");
            Console.WriteLine("1. Tohum cümlenizi (12-24 kelime) hazýrlayýn");
            Console.WriteLine("2. Kelimelerin doðru sýrada olduðundan emin olun");
            Console.WriteLine("3. Kelimeler arasýnda sadece bir boþluk olmalý");
            Console.WriteLine("4. Tüm kelimeler küçük harflerle yazýlmalý");
            Console.WriteLine("5. Yazým hatasý olmadýðýndan emin olun");
            Console.WriteLine("\nÖrnek format:");
            Console.WriteLine("word1 word2 word3 ... word12");
            Console.WriteLine("\nUYARI: Tohum cümlenizi asla baþkalarýyla paylaþmayýn!");
        }

        public static string? GetSeedPhraseFromUser()
        {
            DisplayRecoveryInstructions();

            Console.WriteLine("\nTohum cümlesini giriniz:");
            Console.WriteLine("(Her kelime arasýnda bir boþluk olacak þekilde)");
            Console.Write("> ");

            var seedPhrase = Console.ReadLine()?.Trim().ToLower();
            
            // Fazla boþluklarý temizle
            if (!string.IsNullOrEmpty(seedPhrase))
            {
                seedPhrase = Regex.Replace(seedPhrase, @"\s+", " ");
            }

            return seedPhrase;
        }

        public static UniversalColdWallet RecoverWalletInteractive()
        {
            while (true)
            {
                var seedPhrase = GetSeedPhraseFromUser();
                if (string.IsNullOrEmpty(seedPhrase))
                {
                    Console.WriteLine("Tohum cümlesi boþ olamaz. Tekrar deneyin.");
                    continue;
                }

                try
                {
                    Console.WriteLine("\nTohum cümlesi doðrulanýyor...");
                    ValidateSeedPhrase(seedPhrase);
                    
                    Console.WriteLine("\nYeni bir þifre belirlemek ister misiniz? (E/H)");
                    var setPassword = Console.ReadLine()?.Trim().ToUpper() == "E";

                    string? newPassword = null;
                    if (setPassword)
                    {
                        Console.WriteLine("\nÞifre belirleme seçenekleri:");
                        Console.WriteLine("1. Otomatik güvenli þifre oluþtur");
                        Console.WriteLine("2. Manuel þifre belirle");
                        Console.Write("\nSeçiminiz (1-2): ");
                        
                        var choice = Console.ReadLine()?.Trim();
                        
                        if (choice == "1")
                        {
                            newPassword = SetGetPassword.GenerateSecurePassword();
                            SetGetPassword.DisplayNewPassword(newPassword);
                        }
                        else if (choice == "2")
                        {
                            bool validPassword = false;
                            while (!validPassword)
                            {
                                Console.Write("\nYeni þifre: ");
                                newPassword = SetGetPassword.ReadPassword();

                                if (SetGetPassword.ValidatePasswordStrength(newPassword))
                                {
                                    Console.Write("Þifreyi tekrar girin: ");
                                    var confirmPassword = SetGetPassword.ReadPassword();

                                    if (newPassword == confirmPassword)
                                    {
                                        validPassword = true;
                                    }
                                    else
                                    {
                                        Console.WriteLine("\nÞifreler eþleþmiyor! Tekrar deneyin.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("\nÞifre güvenlik kriterlerini karþýlamýyor!");
                                    Console.WriteLine("- En az 8 karakter uzunluðunda");
                                    Console.WriteLine("- En az 1 büyük harf");
                                    Console.WriteLine("- En az 1 küçük harf");
                                    Console.WriteLine("- En az 1 rakam");
                                    Console.WriteLine("- En az 1 özel karakter");
                                }
                            }
                        }
                    }

                    Console.WriteLine("\nCüzdan kurtarýlýyor...");
                    var recoveredWallet = RecoverFromSeedPhrase(seedPhrase, newPassword);
                    
                    // Cüzdaný kaydet
                    recoveredWallet.SaveToFile("cold_wallet.json", newPassword);
                    
                    Console.WriteLine("\nCüzdan baþarýyla kurtarýldý ve kaydedildi!");
                    return recoveredWallet;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nHata: {ex.Message}");
                    Console.WriteLine("Tekrar denemek için Enter'a basýn veya çýkmak için 'q' tuþuna basýn.");
                    
                    if (Console.ReadLine()?.Trim().ToLower() == "q")
                        throw new OperationCanceledException("Cüzdan kurtarma iþlemi iptal edildi.");
                }
            }
        }
    }
}