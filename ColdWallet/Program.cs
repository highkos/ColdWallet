using System;
using System.IO;
using System.Threading.Tasks;

namespace UniversalColdWallet
{
    class Program
    {
        private static UniversalColdWallet? _currentWallet;

        static async Task Main(string[] args)
        {
            await MainAsync(args);
        }

        static async Task MainAsync(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Evrensel Soğuk Cüzdan ===");
                Console.WriteLine("1. Yeni Cüzdan Oluştur");
                Console.WriteLine("2. Mevcut Cüzdanı Yükle");
                Console.WriteLine("3. Cüzdanı Tohum Cümlesinden Kurtar");
                Console.WriteLine("4. Şifre İşlemleri");
                Console.WriteLine("5. Bakiye Kontrolü");
                Console.WriteLine("6. Özel Anahtarları Göster");
                Console.WriteLine("7. Çıkış");
                Console.Write("\nSeçiminiz (1-7): ");

                var choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            _currentWallet = CreateNewWalletWithPassword();
                            PressAnyKeyToContinue();
                            break;

                        case "2":
                            LoadExistingWallet();
                            break;

                        case "3":
                            _currentWallet = SummonWallet.RecoverWalletInteractive();
                            DisplayWalletInfo(_currentWallet);
                            PressAnyKeyToContinue();
                            break;

                        case "4":
                            HandlePasswordOperations();
                            break;

                        case "5":
                            await HandleBalanceCheckAsync();
                            break;

                        case "6":
                            HandlePrivateKeys();
                            break;

                        case "7":
                            ExitProgram();
                            return;

                        default:
                            Console.WriteLine("\nGeçersiz seçim! Lütfen tekrar deneyin.");
                            PressAnyKeyToContinue();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nHata: {ex.Message}");
                    PressAnyKeyToContinue();
                }
            }
        }

        private static void HandlePrivateKeys()
        {
            if (_currentWallet == null)
            {
                Console.WriteLine("\nÖnce bir cüzdan yükleyin veya oluşturun!");
                PressAnyKeyToContinue();
                return;
            }

            PrivateKeyGene.ShowPrivateKeys(_currentWallet);
            PressAnyKeyToContinue();
        }

        private static UniversalColdWallet CreateNewWalletWithPassword()
        {
            var wallet = CreateWallet.CreateNewWallet();

            string filePath = "cold_wallet.json";
            
            Console.WriteLine("\nCüzdanı şifrelemek istiyor musunuz? (E/H): ");
            var shouldEncrypt = Console.ReadLine()?.Trim().ToUpper() == "E";
            string? password = null;

            if (shouldEncrypt)
            {
                Console.WriteLine("\nŞifre belirleme seçenekleri:");
                Console.WriteLine("1. Otomatik güvenli şifre oluştur");
                Console.WriteLine("2. Manuel şifre belirle");
                Console.Write("\nSeçiminiz (1-2): ");
                
                var choice = Console.ReadLine()?.Trim();
                
                if (choice == "1")
                {
                    password = SetGetPassword.GenerateSecurePassword();
                    SetGetPassword.DisplayNewPassword(password);
                }
                else if (choice == "2")
                {
                    bool validPassword = false;
                    while (!validPassword)
                    {
                        Console.Write("\nYeni şifre: ");
                        password = SetGetPassword.ReadPassword();

                        if (SetGetPassword.ValidatePasswordStrength(password))
                        {
                            Console.Write("Şifreyi tekrar girin: ");
                            var confirmPassword = SetGetPassword.ReadPassword();

                            if (password == confirmPassword)
                            {
                                validPassword = true;
                            }
                            else
                            {
                                Console.WriteLine("\nŞifreler eşleşmiyor! Tekrar deneyin.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("\nŞifre güvenlik kriterlerini karşılamıyor!");
                            Console.WriteLine("- En az 8 karakter uzunluğunda");
                            Console.WriteLine("- En az 1 büyük harf");
                            Console.WriteLine("- En az 1 küçük harf");
                            Console.WriteLine("- En az 1 rakam");
                            Console.WriteLine("- En az 1 özel karakter");
                        }
                    }
                }
            }

            try
            {
                wallet.SaveToFile(filePath, password);
                Console.WriteLine($"\nCüzdan {filePath} dosyasına kaydedildi.");
                
                if (shouldEncrypt && !string.IsNullOrEmpty(password))
                {
                    Console.WriteLine("NOT: Cüzdan belirlenen şifre ile şifrelenmiştir.");
                    Console.WriteLine("Lütfen bu şifreyi güvenli bir yerde saklayın!");
                }
                else
                {
                    Console.WriteLine("NOT: Cüzdan şifresiz olarak kaydedildi.");
                    Console.WriteLine("DİKKAT: Şifresiz cüzdanlar güvenlik riski taşır!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nHata: Cüzdan kaydedilemedi: {ex.Message}");
            }

            return wallet;
        }

        private static void LoadExistingWallet()
        {
            Console.Write("\nCüzdan dosya yolunu giriniz (varsayılan: cold_wallet.json): ");
            string? input = Console.ReadLine();
            string inputPath = string.IsNullOrWhiteSpace(input) ? "cold_wallet.json" : input;

            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string filePath;

                if (Path.IsPathFullyQualified(inputPath))
                {
                    filePath = inputPath;
                }
                else
                {
                    filePath = Path.Combine(baseDirectory, inputPath);
                }

                Console.WriteLine($"\nAranan dosya yolu: {filePath}");

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Cüzdan dosyası bulunamadı: {filePath}");
                }

                string jsonContent = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    throw new InvalidOperationException("Cüzdan dosyası boş!");
                }

                bool isEncrypted = false;
                try
                {
                    var export = Newtonsoft.Json.JsonConvert.DeserializeObject<WalletExport>(jsonContent);
                    isEncrypted = export?.IsEncrypted ?? false;
                }
                catch
                {
                    isEncrypted = true;
                }

                if (isEncrypted)
                {
                    Console.WriteLine("\nDosya şifreli görünüyor.");
                    Console.WriteLine("İpucu: Cüzdanı oluştururken otomatik oluşturulan şifreyi kullanın.");
                }

                Console.Write("Cüzdan şifresini giriniz" + (isEncrypted ? "" : " (şifresizse Enter)") + ": ");
                string password = ReadPassword();

                try
                {
                    _currentWallet = UniversalColdWallet.LoadFromFile(filePath, password);
                    Console.WriteLine("\nCüzdan başarıyla yüklendi!");
                    
                    // Cüzdan bilgilerini ve mnemonic'i otomatik göster
                    DisplayWalletInfo(_currentWallet, true);

                    // Şifreyi tekrar ayarla
                    if (!string.IsNullOrEmpty(password))
                    {
                        _currentWallet.EncryptMnemonic(password);
                    }

                    // Özel anahtarları göster
                    Console.WriteLine("\nÖzel anahtarlar otomatik olarak gösteriliyor...");
                    Console.WriteLine("Devam etmek için bir tuşa basın...");
                    Console.ReadKey(true);
                    PrivateKeyGene.ShowPrivateKeys(_currentWallet);
                }
                catch (ArgumentException ex) when (ex.Message.Contains("şifre"))
                {
                    Console.WriteLine($"\nHata: {ex.Message}");
                    if (isEncrypted)
                    {
                        Console.WriteLine("İpucu: Cüzdanı ilk oluşturduğunuzda gösterilen otomatik şifreyi kullanın.");
                        Console.WriteLine("Bu şifre cüzdan oluşturulduğunda ekranda gösterilmişti.");
                    }
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("JSON"))
                {
                    Console.WriteLine("\nHata: Cüzdan dosyası bozuk veya geçersiz formatta!");
                    Console.WriteLine("İpucu: Dosyanın düzgün bir cüzdan dosyası olduğundan emin olun.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nHata: {ex.Message}");
                if (ex is FileNotFoundException)
                {
                    Console.WriteLine("\nİpucu: Dosya yolunu kontrol edin. Tam yol kullanabilir veya sadece dosya adı girebilirsiniz.");
                    Console.WriteLine($"Uygulama dizini: {AppDomain.CurrentDomain.BaseDirectory}");
                    Console.WriteLine("Örnek kullanım:");
                    Console.WriteLine("1. Sadece dosya adı: cold_wallet.json");
                    Console.WriteLine("2. Tam yol: C:\\Users\\Username\\Documents\\cold_wallet.json");
                }
            }

            PressAnyKeyToContinue();
        }

        private static void DisplayWalletInfo(UniversalColdWallet wallet, bool showMnemonicAutomatically = false)
        {
            ArgumentNullException.ThrowIfNull(wallet);

            Console.WriteLine("\n=== Cüzdan Bilgileri ===");
            
            // Eğer otomatik gösterim isteniyorsa veya kullanıcı onay verirse mnemonic'i göster
            if (showMnemonicAutomatically)
            {
                var mnemonic = wallet.GetMnemonic();
                if (!string.IsNullOrEmpty(mnemonic))
                {
                    Console.WriteLine("\nMnemonic Kelimeleri:");
                    Console.WriteLine("===================");
                    var words = mnemonic.Split(' ');
                    Console.WriteLine(string.Join(" ", words));  // Kelimeleri aralarında bir boşlukla yan yana yazdır
                    Console.WriteLine("\nBu kelimeleri güvenli bir yerde saklayın!");
                    Console.WriteLine("ASLA dijital ortamda saklamayın veya başkalarıyla paylaşmayın!");
                }
            }
            else
            {
                Console.Write("\nMnemonic kelimeleri güvenlik açısından çok önemlidir!");
                Console.WriteLine("\nBu kelimeler cüzdanınızı kurtarmanızı sağlayan ana anahtardır.");
                Console.WriteLine("Güvenli bir ortamda olduğunuzdan emin olun.");
                Console.Write("\nMnemonic kelimelerini görmek istiyor musunuz? (E/H): ");
                
                var showMnemonic = Console.ReadLine()?.Trim().ToUpper();
                if (showMnemonic == "E")
                {
                    // Şifre gerekiyorsa iste
                    if (wallet.HasPassword)
                    {
                        Console.Write("\nCüzdan şifresini giriniz: ");
                        string password = ReadPassword();
                        wallet.SetCurrentPassword(password);
                    }

                    var mnemonic = wallet.GetMnemonic();
                    if (!string.IsNullOrEmpty(mnemonic))
                    {
                        Console.WriteLine("\nMnemonic Kelimeleri:");
                        Console.WriteLine("===================");
                        var words = mnemonic.Split(' ');
                        Console.WriteLine(string.Join(" ", words));  // Kelimeleri aralarında bir boşlukla yan yana yazdır
                        Console.WriteLine("\nBu kelimeleri güvenli bir yerde saklayın!");
                        Console.WriteLine("ASLA dijital ortamda saklamayın veya başkalarıyla paylaşmayın!");
                    }
                }
            }

            var export = wallet.ExportWallet();
            if (export != null)
            {
                Console.WriteLine($"\nOluşturulma Tarihi: {export.CreatedAt:dd.MM.yyyy HH:mm:ss}");
                
                if (export.TotalBalances?.Count > 0)
                {
                    // Define the order of coins for display
                    string[] coinOrder = new string[] 
                    { 
                        "BTC", "ETH", "LTC", "BCH", "DOGE", "ADA", "SOL", 
                        "USDT", "USDT_TRC20", "TRX_TRC20", "USDT_BEP20", "BNB_BSC",
                        "SHIB", "XRP" 
                    };
                    
                    Console.WriteLine("\nToplam Bakiyeler:");
                    Console.WriteLine("================");
                    
                    // Create an ordered dictionary based on the defined order
                    var orderedBalances = new Dictionary<string, decimal>();
                    
                    // First add coins in our defined order
                    foreach (var coin in coinOrder)
                    {
                        if (export.TotalBalances.TryGetValue(coin, out decimal balance))
                        {
                            orderedBalances[coin] = balance;
                        }
                    }
                    
                    // Then add any remaining coins not in our predefined order
                    foreach (var balance in export.TotalBalances)
                    {
                        if (!orderedBalances.ContainsKey(balance.Key))
                        {
                            orderedBalances[balance.Key] = balance.Value;
                        }
                    }
                    
                    // Display the balances in the ordered dictionary
                    foreach (var balance in orderedBalances)
                    {
                        Console.WriteLine($"{balance.Key,-10}: {balance.Value,15:N8}");
                    }
                }

                Console.WriteLine("\nDesteklenen Coinler ve Adresler:");
                Console.WriteLine("================================");

                if (export.Addresses != null)
                {
                    // Use the same coin order for displaying addresses
                    string[] coinOrder = new string[] 
                    { 
                        "BTC", "ETH", "LTC", "BCH", "DOGE", "ADA", "SOL", 
                        "USDT", "USDT_TRC20", "TRX_TRC20", "USDT_BEP20", "BNB_BSC",
                        "SHIB", "XRP" 
                    };

                    // First display the coins in our defined order
                    foreach (var coinSymbol in coinOrder)
                    {
                        if (export.Addresses.TryGetValue(coinSymbol, out var addresses) && addresses != null)
                        {
                            // Special handling for BTC to show all address types
                            if (coinSymbol == "BTC")
                            {
                                Console.WriteLine($"\nBitcoin (BTC) Adresleri ve Bakiyeleri:");
                            
                                foreach (var address in addresses)
                                {
                                    if (address != null)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"  Index {address.Index,2}:");
                                        Console.ResetColor();
                                        
                                        // Get all address types for this index
                                        var addressTypes = wallet.GetBitcoinAddressTypes(address.Index);
                                        
                                        Console.WriteLine($"    Legacy (P2PKH):        {addressTypes["Legacy (P2PKH)"]}");
                                        Console.WriteLine($"    Nested SegWit (P2SH):  {addressTypes["Nested SegWit (P2SH-P2WPKH)"]}");
                                        Console.WriteLine($"    Native SegWit (Bech32): {addressTypes["Native SegWit (Bech32, P2WPKH)"]}");
                                        Console.WriteLine($"    Derivation Path: {address.DerivationPath}");
                                        Console.WriteLine($"    Bakiye: {address.Balance,15:N8} {coinSymbol}");
                                        
                                        if (address.LastBalanceUpdate.HasValue)
                                        {
                                            Console.WriteLine($"    Son Güncelleme: {address.LastBalanceUpdate:dd.MM.yyyy HH:mm:ss}");
                                        }
                                        Console.WriteLine();
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"\n{coinSymbol} Adresleri ve Bakiyeleri:");
                                foreach (var address in addresses)
                                {
                                    if (address != null)
                                    {
                                        Console.WriteLine($"  Index {address.Index,2}: {address.Address}");
                                        Console.WriteLine($"    Derivation Path: {address.DerivationPath}");
                                        Console.WriteLine($"    Bakiye: {address.Balance,15:N8} {coinSymbol}");
                                        
                                        if (address.LastBalanceUpdate.HasValue)
                                        {
                                            Console.WriteLine($"    Son Güncelleme: {address.LastBalanceUpdate:dd.MM.yyyy HH:mm:ss}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    // Then display any remaining coins not in our predefined order
                    foreach (var coin in export.Addresses)
                    {
                        if (!coinOrder.Contains(coin.Key) && coin.Value != null)
                        {
                            Console.WriteLine($"\n{coin.Key} Adresleri ve Bakiyeleri:");
                            foreach (var address in coin.Value)
                            {
                                if (address != null)
                                {
                                    Console.WriteLine($"  Index {address.Index,2}: {address.Address}");
                                    Console.WriteLine($"    Derivation Path: {address.DerivationPath}");
                                    Console.WriteLine($"    Bakiye: {address.Balance,15:N8} {coin.Key}");
                                    
                                    if (address.LastBalanceUpdate.HasValue)
                                    {
                                        Console.WriteLine($"    Son Güncelleme: {address.LastBalanceUpdate:dd.MM.yyyy HH:mm:ss}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void CreateAndDisplayAddresses(UniversalColdWallet wallet)
        {
            ArgumentNullException.ThrowIfNull(wallet);

            Console.WriteLine("\n=== Coin Adresleri ===");
            var coins = wallet.GetSupportedCoins();
            if (coins != null)
            {
                foreach (var coin in coins)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(coin))
                        {
                            var address = wallet.GenerateAddress(coin, 0);
                            if (!string.IsNullOrEmpty(address))
                            {
                                Console.WriteLine($"{coin,-10}: {address}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{coin,-10}: Hata - {ex.Message}");
                    }
                }
            }
        }

        private static void HandlePasswordOperations()
        {
            if (_currentWallet == null)
            {
                Console.WriteLine("\nÖnce bir cüzdan yükleyin veya oluşturun!");
                PressAnyKeyToContinue();
                return;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Şifre İşlemleri ===");

                if (!_currentWallet.HasPassword)
                {
                    Console.WriteLine("1. Şifre Ekle");
                    Console.WriteLine("2. Ana Menüye Dön");
                    Console.Write("\nSeçiminiz (1-2): ");

                    var choice = Console.ReadLine();
                    string? newPassword;

                    switch (choice)
                    {
                        case "1":
                            Console.WriteLine("\nŞifre belirleme seçenekleri:");
                            Console.WriteLine("1. Otomatik güvenli şifre oluştur");
                            Console.WriteLine("2. Manuel şifre belirle");
                            Console.Write("\nSeçiminiz (1-2): ");
                            
                            var passwordChoice = Console.ReadLine()?.Trim();
                            
                            if (passwordChoice == "1")
                            {
                                newPassword = SetGetPassword.GenerateSecurePassword();
                                _currentWallet.EncryptMnemonic(newPassword);
                                _currentWallet.SaveToFile("cold_wallet.json", newPassword);
                                SetGetPassword.DisplayNewPassword(newPassword);
                                Console.WriteLine("Cüzdan yeni şifre ile güvenli şekilde kaydedildi.");
                            }
                            else if (passwordChoice == "2")
                            {
                                bool success = SetGetPassword.AddPassword(_currentWallet, out newPassword);
                                if (success && !string.IsNullOrEmpty(newPassword))
                                {
                                    _currentWallet.SaveToFile("cold_wallet.json", newPassword);
                                    Console.WriteLine("Cüzdan yeni şifre ile güvenli şekilde kaydedildi.");
                                }
                            }
                            PressAnyKeyToContinue();
                            break;

                        case "2":
                            return;

                        default:
                            Console.WriteLine("\nGeçersiz seçim! Lütfen tekrar deneyin.");
                            PressAnyKeyToContinue();
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("1. Şifre Değiştir (Elle)");
                    Console.WriteLine("2. Şifre Değiştir (Otomatik)");
                    Console.WriteLine("3. Ana Menüye Dön");
                    Console.Write("\nSeçiminiz (1-3): ");

                    var choice = Console.ReadLine();
                    string? newPassword;

                    switch (choice)
                    {
                        case "1":
                            bool success = SetGetPassword.ChangePasswordManually(_currentWallet, out newPassword);
                            if (success && !string.IsNullOrEmpty(newPassword))
                            {
                                _currentWallet.SaveToFile("cold_wallet.json", newPassword);
                                Console.WriteLine("Cüzdan yeni şifre ile güvenli şekilde kaydedildi.");
                            }
                            PressAnyKeyToContinue();
                            break;

                        case "2":
                            success = SetGetPassword.ChangePasswordAutomatically(_currentWallet, out newPassword);
                            if (success && !string.IsNullOrEmpty(newPassword))
                            {
                                _currentWallet.SaveToFile("cold_wallet.json", newPassword);
                                Console.WriteLine("\nCüzdan yeni şifre ile güvenli şekilde kaydedildi.");
                            }
                            PressAnyKeyToContinue();
                            break;

                        case "3":
                            return;

                        default:
                            Console.WriteLine("\nGeçersiz seçim! Lütfen tekrar deneyin.");
                            PressAnyKeyToContinue();
                            break;
                    }
                }
            }
        }

        private static async Task HandleBalanceCheckAsync()
        {
            if (_currentWallet == null)
            {
                Console.WriteLine("\nÖnce bir cüzdan yükleyin veya oluşturun!");
                PressAnyKeyToContinue();
                return;
            }

            if (_currentWallet.HasPassword)
            {
                Console.Write("\nCüzdan şifresini giriniz: ");
                string password = ReadPassword();
                _currentWallet.SetCurrentPassword(password);
            }

            Console.Clear();
            Console.WriteLine("=== Bakiye Kontrolü ===");
            Console.WriteLine("1. Tüm Bakiyeleri Güncelle");
            Console.WriteLine("2. Tek Coin Bakiyesini Güncelle");
            Console.WriteLine("3. Ana Menüye Dön");
            Console.Write("\nSeçiminiz (1-3): ");

            var choice = Console.ReadLine();
            var balanceChecker = new AccountBalance();

            try
            {
                switch (choice)
                {
                    case "1":
                        Console.WriteLine("\nBakiyeler güncelleniyor, lütfen bekleyin...");
                        
                        var oldBalances = new Dictionary<string, decimal>();
                        var coins = _currentWallet.GetSupportedCoins();
                        foreach (var coin in coins)
                        {
                            oldBalances[coin] = _currentWallet.GetBalance(coin, 0);
                        }

                        await balanceChecker.UpdateWalletBalancesAsync(_currentWallet);
                        
                        bool hasChanges = false;
                        Console.WriteLine("\nBakiye Değişiklikleri:");
                        Console.WriteLine("====================");
                        foreach (var coin in coins)
                        {
                            var newBalance = _currentWallet.GetBalance(coin, 0);
                            var oldBalance = oldBalances[coin];
                            var change = newBalance - oldBalance;
                            
                            if (change != 0)
                            {
                                hasChanges = true;
                                var changeSymbol = change > 0 ? "+" : "";
                                Console.WriteLine($"{coin,-10}: {oldBalance:N8} -> {newBalance:N8} ({changeSymbol}{change:N8})");
                            }
                        }

                        Console.WriteLine(hasChanges ? "\nBakiyeler başarıyla güncellendi!" : "\nGüncellenecek bakiye değişikliği yok.");
                        DisplayWalletInfo(_currentWallet, true); // showMnemonicAutomatically parametresi true olarak eklendi
                        break;

                    case "2":
                        await UpdateSingleCoinBalanceAsync(balanceChecker);
                        break;

                    case "3":
                        return;

                    default:
                        Console.WriteLine("\nGeçersiz seçim!");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nBakiye güncelleme hatası: {ex.Message}");
            }

            PressAnyKeyToContinue();
        }

        private static async Task UpdateSingleCoinBalanceAsync(AccountBalance balanceChecker)
        {
            var coins = _currentWallet!.GetSupportedCoins();
            
            Console.WriteLine("\nDesteklenen Coinler:");
            for (int i = 0; i < coins.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {coins[i]}");
            }

            Console.Write($"\nGüncellenecek coini seçin (1-{coins.Count}): ");
            if (int.TryParse(Console.ReadLine(), out int coinChoice) && coinChoice > 0 && coinChoice <= coins.Count)
            {
                var selectedCoin = coins[coinChoice - 1];
                Console.WriteLine($"\n{selectedCoin} bakiyesi güncelleniyor...");

                try
                {
                    Console.WriteLine("API'ye bağlanılıyor...");
                    
                    var address = _currentWallet.GenerateAddress(selectedCoin, 0);
                    Console.WriteLine($"Adres: {address}");
                    Console.WriteLine("Bakiye sorgulanıyor...");

                    var balance = await balanceChecker.GetBalanceAsync(selectedCoin, address);
                    var oldBalance = _currentWallet.GetBalance(selectedCoin, 0);

                    Console.WriteLine($"Eski Bakiye: {oldBalance:N8} {selectedCoin}");
                    _currentWallet.UpdateBalance(selectedCoin, 0, balance);

                    Console.WriteLine($"\n{selectedCoin} bakiyesi başarıyla güncellendi!");
                    Console.WriteLine($"Yeni Bakiye: {balance:N8} {selectedCoin}");

                    var change = balance - oldBalance;
                    if (change != 0)
                    {
                        var changeSymbol = change > 0 ? "+" : "";
                        Console.WriteLine($"Değişim: {changeSymbol}{change:N8} {selectedCoin}");
                    }

                    var lastUpdate = _currentWallet.GetLastBalanceUpdate(selectedCoin, 0);
                    if (lastUpdate.HasValue)
                    {
                        Console.WriteLine($"Son Güncelleme: {lastUpdate.Value:dd.MM.yyyy HH:mm:ss}");
                    }

                    // Tam cüzdan bilgilerini göster
                    Console.WriteLine("\nGüncel cüzdan bilgileri gösteriliyor...");
                    DisplayWalletInfo(_currentWallet, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nBakiye güncelleme hatası: {ex.Message}");
                    Console.WriteLine("Lütfen internet bağlantınızı kontrol edin ve tekrar deneyin.");
                }
            }
            else
            {
                Console.WriteLine("\nGeçersiz seçim! Lütfen listeden geçerli bir coin numarası seçin.");
            }
        }

        private static string ReadPassword()
        {
            var password = new System.Text.StringBuilder();
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

        private static void ExitProgram()
        {
            Console.WriteLine("\nProgramdan çıkılıyor...");
            PressAnyKeyToContinue();
        }

        private static void PressAnyKeyToContinue()
        {
            Console.WriteLine("\nDevam etmek için bir tuşa basın...");
            Console.ReadKey();
        }
    }
}