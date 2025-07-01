using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace UniversalColdWallet
{
    class Program
    {
        private static UniversalColdWallet? _currentWallet;

        static async Task Main(string[] args)
        {
            await MainAsync(args).ConfigureAwait(false);
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
                            await LoadExistingWalletAsync().ConfigureAwait(false);
                            break;

                        case "3":
                            _currentWallet = SummonWallet.RecoverWalletInteractive();
                            await DisplayWalletInfoAsync(_currentWallet).ConfigureAwait(false);
                            PressAnyKeyToContinue();
                            break;

                        case "4":
                            HandlePasswordOperations();
                            break;

                        case "5":
                            await HandleBalanceCheckAsync().ConfigureAwait(false);
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

        private static async Task LoadExistingWalletAsync()
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

                    await DisplayWalletInfoAsync(_currentWallet, true).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(password))
                    {
                        _currentWallet.EncryptMnemonic(password);
                    }

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

        private static async Task DisplayWalletInfoAsync(UniversalColdWallet wallet, bool showMnemonicAutomatically = false)
        {
            ArgumentNullException.ThrowIfNull(wallet);

            Console.WriteLine("\n=== Cüzdan Bilgileri ===");

            if (showMnemonicAutomatically)
            {
                var mnemonic = wallet.GetMnemonic();
                if (!string.IsNullOrEmpty(mnemonic))
                {
                    Console.WriteLine("\nMnemonic Kelimeleri:");
                    Console.WriteLine("===================");
                    var words = mnemonic.Split(' ');
                    Console.WriteLine(string.Join(" ", words));
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
                        Console.WriteLine(string.Join(" ", words));
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
                    string[] coinOrder = new string[]
                    {
                        "BTC", "ETH", "LTC", "BCH", "DOGE", "ADA", "SOL",
                        "USDT", "USDT_TRC20", "TRX_TRC20", "USDT_BEP20", "BNB_BSC",
                        "SHIB", "XRP"
                    };

                    Console.WriteLine("\nToplam Bakiyeler:");
                    Console.WriteLine("================");

                    var orderedBalances = new Dictionary<string, decimal>();

                    foreach (var coin in coinOrder)
                    {
                        if (export.TotalBalances.TryGetValue(coin, out decimal balance))
                        {
                            orderedBalances[coin] = balance;
                        }
                    }

                    foreach (var balance in export.TotalBalances)
                    {
                        if (!orderedBalances.ContainsKey(balance.Key))
                        {
                            orderedBalances[balance.Key] = balance.Value;
                        }
                    }

                    foreach (var balance in orderedBalances)
                    {
                        if (balance.Key == "BTC")
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"{balance.Key,-10}: {balance.Value,15:N8}");
                            Console.ResetColor();

                            try
                            {
                                var btcAddressTypes = wallet.GetBitcoinAddressTypes(0);
                                var balanceChecker = new AccountBalance();

                                decimal nativeBalance = 0;
                                decimal nestedBalance = 0;

                                foreach (var addressType in btcAddressTypes)
                                {
                                    try
                                    {
                                        var address = export.Addresses["BTC"].FirstOrDefault(a => a.Index == 0);
                                        decimal addressBalance = 0;

                                        if (address?.AddressTypeBalances != null &&
                                            address.AddressTypeBalances.TryGetValue(addressType.Key, out decimal storedBalance))
                                        {
                                            addressBalance = storedBalance;
                                        }
                                        else
                                        {
                                            addressBalance = await balanceChecker.GetBtcBalanceAsync(addressType.Value).ConfigureAwait(false);
                                        }

                                        if (addressType.Key.Contains("Native"))
                                        {
                                            nativeBalance = addressBalance;
                                        }
                                        else if (addressType.Key.Contains("Nested"))
                                        {
                                            nestedBalance = addressBalance;
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }

                                Console.WriteLine($"BTC Nested: {nestedBalance,15:N8}");
                                Console.WriteLine($"BTC Native: {nativeBalance,15:N8}");
                            }
                            catch
                            {
                            }
                        }
                        else if (balance.Key == "LTC")
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"{balance.Key,-10}: {balance.Value,15:N8}");
                            Console.ResetColor();

                            try
                            {
                                var ltcAddressTypes = GetLitecoinAddressTypes(wallet, 0);
                                var balanceChecker = new AccountBalance();

                                decimal nativeBalance = 0;
                                decimal nestedBalance = 0;

                                foreach (var addressType in ltcAddressTypes)
                                {
                                    try
                                    {
                                        var address = export.Addresses["LTC"].FirstOrDefault(a => a.Index == 0);
                                        decimal addressBalance = 0;

                                        if (address?.AddressTypeBalances != null &&
                                            address.AddressTypeBalances.TryGetValue(addressType.Key, out decimal storedBalance))
                                        {
                                            addressBalance = storedBalance;
                                        }
                                        else
                                        {
                                            // LTC için actual balance sorgulaması yapalım
                                            try
                                            {
                                                addressBalance = await balanceChecker.GetLtcBalanceAsync(addressType.Value).ConfigureAwait(false);
                                            }
                                            catch
                                            {
                                                addressBalance = 0; // API hatası varsa 0 olarak ayarla
                                            }
                                        }

                                        if (addressType.Key.Contains("Native"))
                                        {
                                            nativeBalance = addressBalance;
                                        }
                                        else if (addressType.Key.Contains("Nested"))
                                        {
                                            nestedBalance = addressBalance;
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }

                                Console.WriteLine($"LTC Nested: {nestedBalance,15:N8}");
                                Console.WriteLine($"LTC Native: {nativeBalance,15:N8}");
                            }
                            catch
                            {
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{balance.Key,-10}: {balance.Value,15:N8}");
                        }
                    }
                }

                Console.WriteLine("\nDesteklenen Coinler ve Adresler:");
                Console.WriteLine("================================");

                if (export.Addresses != null)
                {
                    string[] coinOrder = new string[]
                    {
                        "BTC", "ETH", "LTC", "BCH", "DOGE", "ADA", "SOL",
                        "USDT", "USDT_TRC20", "TRX_TRC20", "USDT_BEP20", "BNB_BSC",
                        "SHIB", "XRP"
                    };

                    foreach (var coinSymbol in coinOrder)
                    {
                        if (export.Addresses.TryGetValue(coinSymbol, out var addresses) && addresses != null)
                        {
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

                                        var addressTypes = wallet.GetBitcoinAddressTypes(address.Index);

                                        Console.WriteLine($"    Legacy (P2PKH):        {addressTypes["Legacy (P2PKH)"]}");
                                        Console.WriteLine($"    Nested SegWit (P2SH):  {addressTypes["Nested SegWit (P2SH-P2WPKH)"]}");
                                        Console.WriteLine($"    Native SegWit (Bech32): {addressTypes["Native SegWit (Bech32, P2WPKH)"]}");
                                        Console.WriteLine($"    Derivation Path: {address.DerivationPath}");
                                        Console.WriteLine($"    Toplam Bakiye: {address.Balance,15:N8} {coinSymbol}");

                                        if (address.AddressTypeBalances != null && address.AddressTypeBalances.Count > 0)
                                        {
                                            foreach (var typeBalance in address.AddressTypeBalances)
                                            {
                                                if (typeBalance.Value > 0)
                                                {
                                                    string typeName = typeBalance.Key.Split(' ')[0];
                                                    Console.WriteLine($"      • {typeName,-6} Bakiye: {typeBalance.Value,10:N8} BTC");
                                                }
                                            }
                                        }

                                        if (address.LastBalanceUpdate.HasValue)
                                        {
                                            Console.WriteLine($"    Son Güncelleme: {address.LastBalanceUpdate:dd.MM.yyyy HH:mm:ss}");
                                        }
                                        Console.WriteLine();
                                    }
                                }
                            }
                            else if (coinSymbol == "LTC")
                            {
                                Console.WriteLine($"\nLitecoin (LTC) Adresleri ve Bakiyeleri:");
                                foreach (var address in addresses)
                                {
                                    if (address != null)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine($"  Index {address.Index,2}:");
                                        Console.ResetColor();
                                        var ltcAddressTypes = GetLitecoinAddressTypes(wallet, address.Index);
                                        Console.WriteLine($"    Legacy (P2PKH):        {ltcAddressTypes["Legacy (P2PKH)"]}");
                                        Console.WriteLine($"    Nested SegWit (P2SH):  {ltcAddressTypes["Nested SegWit (P2SH-P2WPKH)"]}");
                                        Console.WriteLine($"    Native SegWit (Bech32): {ltcAddressTypes["Native SegWit (Bech32, P2WPKH)"]}");
                                        Console.WriteLine($"    Derivation Path: {address.DerivationPath}");
                                        Console.WriteLine($"    Toplam Bakiye: {address.Balance,15:N8} {coinSymbol}");
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

                        if (_currentWallet.GetSupportedCoins().Contains("BTC"))
                        {
                            Console.WriteLine("\nBitcoin adresleri kontrol ediliyor...");
                            var btcAddressTypes = _currentWallet.GetBitcoinAddressTypes(0);

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("\nBitcoin Adresleri Bakiyeleri:");
                            Console.ResetColor();

                            decimal totalBtcBalance = 0;
                            var addressBalances = new Dictionary<string, decimal>();

                            foreach (var item in btcAddressTypes)
                            {
                                string addressType = item.Key;
                                string btcAddress = item.Value;
                                string shortType = addressType.Split(' ')[0];

                                try
                                {
                                    decimal addressBalance = await balanceChecker.GetBtcBalanceAsync(btcAddress).ConfigureAwait(false);
                                    totalBtcBalance += addressBalance;
                                    addressBalances[addressType] = addressBalance;
                                    Console.WriteLine($"{shortType,-10}: {addressBalance,15:N8} BTC  ({btcAddress})");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"{shortType,-10}: Bakiye sorgulanamadı - {ex.Message.Split('.')[0]}");
                                    addressBalances[addressType] = 0;
                                }
                            }

                            var btcAddressInfo = _currentWallet.ExportWallet().Addresses["BTC"].FirstOrDefault(a => a.Index == 0);
                            if (btcAddressInfo != null)
                            {
                                if (btcAddressInfo.AddressTypeBalances == null)
                                    btcAddressInfo.AddressTypeBalances = new Dictionary<string, decimal>();

                                foreach (var balance in addressBalances)
                                {
                                    btcAddressInfo.AddressTypeBalances[balance.Key] = balance.Value;
                                }
                            }

                            Console.WriteLine($"\nToplam BTC: {totalBtcBalance,15:N8} BTC");
                            var dictionary = new Dictionary<int, decimal>
                            {
                                { 0, totalBtcBalance }
                            };

                            var btcBalances = new Dictionary<string, Dictionary<int, decimal>>
                            {
                                { "BTC", dictionary }
                            };

                            _currentWallet.UpdateAllBalances(btcBalances);
                            Console.WriteLine("Bitcoin bakiyeleri güncellendi.");
                        }

                        await balanceChecker.UpdateWalletBalancesAsync(_currentWallet).ConfigureAwait(false);

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
                        await DisplayWalletInfoAsync(_currentWallet, true).ConfigureAwait(false);
                        break;

                    case "2":
                        await UpdateSingleCoinBalanceAsync(balanceChecker).ConfigureAwait(false);
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

                    if (selectedCoin.ToUpper() == "BTC")
                    {
                        var btcAddressTypes = _currentWallet.GetBitcoinAddressTypes(0);
                        var oldBalance = _currentWallet.GetBalance(selectedCoin, 0);
                        decimal totalBalance = 0;

                        Console.WriteLine("\nBitcoin bakiyeleri kontrol ediliyor...");

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\nBitcoin Adresleri Bakiyeleri:");
                        Console.ResetColor();

                        var addressBalances = new Dictionary<string, decimal>();

                        foreach (var item in btcAddressTypes)
                        {
                            string addressType = item.Key;
                            string btcAddress = item.Value;
                            string shortType = addressType.Split(' ')[0];

                            try
                            {
                                decimal addressBalance = await balanceChecker.GetBtcBalanceAsync(btcAddress).ConfigureAwait(false);
                                totalBalance += addressBalance;
                                addressBalances[addressType] = addressBalance;
                                Console.WriteLine($"{shortType,-10}: {addressBalance,15:N8} BTC  ({btcAddress})");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"{shortType,-10}: Bakiye sorgulanamadı - {ex.Message.Split('.')[0]}");
                                addressBalances[addressType] = 0;
                            }
                        }

                        Console.WriteLine($"\nToplam BTC: {totalBalance,15:N8} BTC");

                        var change = totalBalance - oldBalance;
                        if (change != 0)
                        {
                            var changeSymbol = change > 0 ? "+" : "";
                            Console.WriteLine($"Değişim: {changeSymbol}{change:N8} BTC");
                        }

                        var btcAddressInfo = _currentWallet.ExportWallet().Addresses["BTC"].FirstOrDefault(a => a.Index == 0);
                        if (btcAddressInfo != null)
                        {
                            if (btcAddressInfo.AddressTypeBalances == null)
                                btcAddressInfo.AddressTypeBalances = new Dictionary<string, decimal>();

                            foreach (var balance in addressBalances)
                            {
                                btcAddressInfo.AddressTypeBalances[balance.Key] = balance.Value;
                            }
                        }

                        _currentWallet.UpdateBalance(selectedCoin, 0, totalBalance);

                        Console.WriteLine($"\n{selectedCoin} bakiyesi başarıyla güncellendi!");
                    }
                    else if (selectedCoin.ToUpper() == "LTC")
                    {
                        var ltcAddressTypes = GetLitecoinAddressTypes(_currentWallet, 0);
                        var oldBalance = _currentWallet.GetBalance(selectedCoin, 0);
                        decimal totalBalance = 0;

                        Console.WriteLine("\nLitecoin bakiyeleri kontrol ediliyor...");

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("\nLitecoin Adresleri Bakiyeleri:");
                        Console.ResetColor();

                        var addressBalances = new Dictionary<string, decimal>();

                        foreach (var item in ltcAddressTypes)
                        {
                            string addressType = item.Key;
                            string ltcAddress = item.Value;
                            string shortType = addressType.Split(' ')[0];

                            try
                            {
                                decimal addressBalance = await balanceChecker.GetLtcBalanceAsync(ltcAddress).ConfigureAwait(false);
                                totalBalance += addressBalance;
                                addressBalances[addressType] = addressBalance;
                                Console.WriteLine($"{shortType,-10}: {addressBalance,15:N8} LTC  ({ltcAddress})");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"{shortType,-10}: Bakiye sorgulanamadı - {ex.Message.Split('.')[0]}");
                                addressBalances[addressType] = 0;
                            }
                        }

                        Console.WriteLine($"\nToplam LTC: {totalBalance,15:N8} LTC");

                        var change = totalBalance - oldBalance;
                        if (change != 0)
                        {
                            var changeSymbol = change > 0 ? "+" : "";
                            Console.WriteLine($"Değişim: {changeSymbol}{change:N8} LTC");
                        }

                        var ltcAddressInfo = _currentWallet.ExportWallet().Addresses["LTC"].FirstOrDefault(a => a.Index == 0);
                        if (ltcAddressInfo != null)
                        {
                            if (ltcAddressInfo.AddressTypeBalances == null)
                                ltcAddressInfo.AddressTypeBalances = new Dictionary<string, decimal>();

                            foreach (var balance in addressBalances)
                            {
                                ltcAddressInfo.AddressTypeBalances[balance.Key] = balance.Value;
                            }
                        }

                        _currentWallet.UpdateBalance(selectedCoin, 0, totalBalance);

                        Console.WriteLine($"\n{selectedCoin} bakiyesi başarıyla güncellendi!");
                    }
                    else
                    {
                        var balance = await balanceChecker.GetBalanceAsync(selectedCoin, address).ConfigureAwait(false);
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
                    }

                    var lastUpdate = _currentWallet.GetLastBalanceUpdate(selectedCoin, 0);
                    if (lastUpdate.HasValue)
                    {
                        Console.WriteLine($"Son Güncelleme: {lastUpdate.Value:dd.MM.yyyy HH:mm:ss}");
                    }

                    Console.WriteLine("\nGüncel cüzdan bilgileri gösteriliyor...");
                    await DisplayWalletInfoAsync(_currentWallet, true).ConfigureAwait(false);
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

        private static Dictionary<string, string> GetLitecoinAddressTypes(UniversalColdWallet wallet, int accountIndex)
        {
            var coinInfo = new SupportedCoins().GetCoinInfo("LTC");
            var mnemonic = new NBitcoin.Mnemonic(wallet.GetMnemonic());
            var hdRoot = mnemonic.DeriveExtKey();
            var keyPath = new NBitcoin.KeyPath(coinInfo.DerivationPath + "/" + accountIndex);
            var derivedKey = hdRoot.Derive(keyPath);
            var pubKey = derivedKey.PrivateKey.PubKey;

            var pubKeyHash = pubKey.Hash.ToBytes();
            var nativeBech32 = GenerateLitecoinBech32Address(pubKeyHash);

            return new Dictionary<string, string>
            {
                { "Legacy (P2PKH)", pubKey.GetAddress(NBitcoin.ScriptPubKeyType.Legacy, NBitcoin.Altcoins.Litecoin.Instance.Mainnet).ToString() },
                { "Nested SegWit (P2SH-P2WPKH)", pubKey.GetAddress(NBitcoin.ScriptPubKeyType.SegwitP2SH, NBitcoin.Altcoins.Litecoin.Instance.Mainnet).ToString() },
                { "Native SegWit (Bech32, P2WPKH)", nativeBech32 }
            };
        }

        private static string GenerateLitecoinBech32Address(byte[] pubKeyHash)
        {
            if (pubKeyHash.Length != 20)
                throw new ArgumentException("Public key hash must be 20 bytes", nameof(pubKeyHash));

            const string hrp = "ltc";
            const int witnessVersion = 0;

            var converted = ConvertBitsToBase32(pubKeyHash, 8, 5, true);

            var spec = new List<int> { witnessVersion };
            spec.AddRange(converted);

            var checksum = CalculateBech32Checksum(hrp, spec);

            var combined = spec.Concat(checksum).ToArray();

            const string charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
            var result = new StringBuilder(hrp + "1");

            foreach (var value in combined)
            {
                if (value < 0 || value >= charset.Length)
                    throw new InvalidOperationException($"Invalid character value: {value}");
                result.Append(charset[value]);
            }

            return result.ToString();
        }

        private static List<int> ConvertBitsToBase32(byte[] data, int fromBits, int toBits, bool pad)
        {
            var result = new List<int>();
            int acc = 0;
            int bits = 0;
            int maxv = (1 << toBits) - 1;
            int maxAcc = (1 << (fromBits + toBits - 1)) - 1;

            foreach (byte value in data)
            {
                if (value < 0 || (value >> fromBits) != 0)
                    throw new ArgumentException("Invalid input data for base conversion");

                acc = ((acc << fromBits) | value) & maxAcc;
                bits += fromBits;

                while (bits >= toBits)
                {
                    bits -= toBits;
                    result.Add((acc >> bits) & maxv);
                }
            }

            if (pad)
            {
                if (bits > 0)
                    result.Add((acc << (toBits - bits)) & maxv);
            }
            else if (bits >= fromBits || ((acc << (toBits - bits)) & maxv) != 0)
            {
                throw new ArgumentException("Invalid padding bits");
            }

            return result;
        }

        private static List<int> CalculateBech32Checksum(string hrp, List<int> data)
        {
            var values = new List<int>();

            foreach (char c in hrp)
                values.Add(c >> 5);

            values.Add(0);

            foreach (char c in hrp)
                values.Add(c & 31);

            values.AddRange(data);

            values.AddRange(Enumerable.Repeat(0, 6));

            uint polymod = CalculateBech32Polymod(values) ^ 1;

            var checksum = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                checksum.Add((int)((polymod >> (5 * (5 - i))) & 31));
            }

            return checksum;
        }

        private static uint CalculateBech32Polymod(List<int> values)
        {
            uint[] generator = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };
            uint chk = 1;

            foreach (int value in values)
            {
                uint top = chk >> 25;
                chk = (chk & 0x1ffffff) << 5 ^ (uint)value;

                for (int i = 0; i < 5; i++)
                {
                    if (((top >> i) & 1) == 1)
                        chk ^= generator[i];
                }
            }

            return chk;
        }
    }
}