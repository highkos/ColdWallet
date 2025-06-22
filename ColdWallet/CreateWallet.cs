using System;
using System.Linq;

namespace UniversalColdWallet
{
    public static class CreateWallet
    {
        public static UniversalColdWallet CreateNewWallet()
        {
            Console.WriteLine("=== Yeni Soðuk Cüzdan Oluþturuluyor ===\n");

            var wallet = new UniversalColdWallet();

            Console.WriteLine($"Mnemonic: {wallet.GetMnemonic()}");
            Console.WriteLine($"Desteklenen coinler: {string.Join(", ", wallet.GetSupportedCoins())}");
            Console.WriteLine();

            var export = wallet.ExportWallet();
            Console.WriteLine("\nOluþturulan Adresler:");
            Console.WriteLine("=====================");

            // Get the supported symbols in their original order
            var supportedSymbols = wallet.GetSupportedCoins();
            
            // Display coins in the order they appear in SupportedCoins
            foreach (var coinSymbol in supportedSymbols)
            {
                // Check if the coin exists in the export addresses
                if (export.Addresses.TryGetValue(coinSymbol, out var addresses))
                {
                    Console.WriteLine($"\n{coinSymbol} Adresleri:");
                    foreach (var address in addresses)
                    {
                        Console.WriteLine($"  Index {address.Index,2}: {address.Address}");
                        Console.WriteLine($"    Derivation Path: {address.DerivationPath}");
                    }
                }
            }

            Console.WriteLine("\n=== Cüzdan baþarýyla oluþturuldu! ===");
            Console.WriteLine("ÖNEMLÝ: Mnemonic kelimelerinizi ve adresleri güvenli bir yerde saklayýn!");
            Console.WriteLine("NOT: Her coin için birden fazla adres oluþturuldu. Index deðerlerini not alýn!");

            return wallet;
        }
    }
}