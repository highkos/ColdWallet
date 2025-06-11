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
            // Seed phrase do�rulama
            ValidateSeedPhrase(seedPhrase);

            try
            {
                // Yeni c�zdan olu�tur
                var wallet = new UniversalColdWallet(seedPhrase);

                // E�er yeni �ifre belirlendiyse, c�zdan� �ifrele
                if (!string.IsNullOrEmpty(newPassword))
                {
                    if (!SetGetPassword.ValidatePasswordStrength(newPassword))
                    {
                        throw new ArgumentException("Yeni �ifre g�venlik kriterlerini kar��lam�yor.");
                    }

                    wallet.EncryptMnemonic(newPassword);
                }

                return wallet;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("C�zdan kurtarma ba�ar�s�z: " + ex.Message, ex);
            }
        }

        public static bool ValidateSeedPhrase(string seedPhrase)
        {
            if (string.IsNullOrWhiteSpace(seedPhrase))
                throw new ArgumentException("Tohum c�mlesi bo� olamaz.");

            // Kelime say�s�n� kontrol et (12, 15, 18, 21, 24 kelime olabilir)
            var words = seedPhrase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var validWordCounts = new[] { 12, 15, 18, 21, 24 };
            
            if (!Array.Exists(validWordCounts, x => x == words.Length))
            {
                throw new ArgumentException($"Ge�ersiz kelime say�s�. Tohum c�mlesi {string.Join(", ", validWordCounts)} kelimeden olu�mal�d�r.");
            }

            try
            {
                // BIP39 format�na uygunlu�unu kontrol et
                var mnemonic = new Mnemonic(seedPhrase, Wordlist.English);
                if (!mnemonic.IsValidChecksum)
                {
                    throw new ArgumentException("Tohum c�mlesi ge�erli bir BIP39 format�nda de�il.");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Ge�ersiz tohum c�mlesi: " + ex.Message, ex);
            }
        }

        public static void DisplayRecoveryInstructions()
        {
            Console.WriteLine("\n=== C�zdan Kurtarma Talimatlar� ===");
            Console.WriteLine("1. Tohum c�mlenizi (12-24 kelime) haz�rlay�n");
            Console.WriteLine("2. Kelimelerin do�ru s�rada oldu�undan emin olun");
            Console.WriteLine("3. Kelimeler aras�nda sadece bir bo�luk olmal�");
            Console.WriteLine("4. T�m kelimeler k���k harflerle yaz�lmal�");
            Console.WriteLine("5. Yaz�m hatas� olmad���ndan emin olun");
            Console.WriteLine("\n�rnek format:");
            Console.WriteLine("word1 word2 word3 ... word12");
            Console.WriteLine("\nUYARI: Tohum c�mlenizi asla ba�kalar�yla payla�may�n!");
        }

        public static string? GetSeedPhraseFromUser()
        {
            DisplayRecoveryInstructions();

            Console.WriteLine("\nTohum c�mlesini giriniz:");
            Console.WriteLine("(Her kelime aras�nda bir bo�luk olacak �ekilde)");
            Console.Write("> ");

            var seedPhrase = Console.ReadLine()?.Trim().ToLower();
            
            // Fazla bo�luklar� temizle
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
                    Console.WriteLine("Tohum c�mlesi bo� olamaz. Tekrar deneyin.");
                    continue;
                }

                try
                {
                    Console.WriteLine("\nTohum c�mlesi do�rulan�yor...");
                    ValidateSeedPhrase(seedPhrase);
                    
                    Console.WriteLine("\nYeni bir �ifre belirlemek ister misiniz? (E/H)");
                    var setPassword = Console.ReadLine()?.Trim().ToUpper() == "E";

                    string? newPassword = null;
                    if (setPassword)
                    {
                        Console.WriteLine("\n�ifre belirleme se�enekleri:");
                        Console.WriteLine("1. Otomatik g�venli �ifre olu�tur");
                        Console.WriteLine("2. Manuel �ifre belirle");
                        Console.Write("\nSe�iminiz (1-2): ");
                        
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
                                Console.Write("\nYeni �ifre: ");
                                newPassword = SetGetPassword.ReadPassword();

                                if (SetGetPassword.ValidatePasswordStrength(newPassword))
                                {
                                    Console.Write("�ifreyi tekrar girin: ");
                                    var confirmPassword = SetGetPassword.ReadPassword();

                                    if (newPassword == confirmPassword)
                                    {
                                        validPassword = true;
                                    }
                                    else
                                    {
                                        Console.WriteLine("\n�ifreler e�le�miyor! Tekrar deneyin.");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("\n�ifre g�venlik kriterlerini kar��lam�yor!");
                                    Console.WriteLine("- En az 8 karakter uzunlu�unda");
                                    Console.WriteLine("- En az 1 b�y�k harf");
                                    Console.WriteLine("- En az 1 k���k harf");
                                    Console.WriteLine("- En az 1 rakam");
                                    Console.WriteLine("- En az 1 �zel karakter");
                                }
                            }
                        }
                    }

                    Console.WriteLine("\nC�zdan kurtar�l�yor...");
                    var recoveredWallet = RecoverFromSeedPhrase(seedPhrase, newPassword);
                    
                    // C�zdan� kaydet
                    recoveredWallet.SaveToFile("cold_wallet.json", newPassword);
                    
                    Console.WriteLine("\nC�zdan ba�ar�yla kurtar�ld� ve kaydedildi!");
                    return recoveredWallet;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nHata: {ex.Message}");
                    Console.WriteLine("Tekrar denemek i�in Enter'a bas�n veya ��kmak i�in 'q' tu�una bas�n.");
                    
                    if (Console.ReadLine()?.Trim().ToLower() == "q")
                        throw new OperationCanceledException("C�zdan kurtarma i�lemi iptal edildi.");
                }
            }
        }
    }
}