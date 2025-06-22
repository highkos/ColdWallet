using System;
using System.Linq;

namespace UniversalColdWallet
{
    public static class CreateWallet
    {
        public static UniversalColdWallet CreateNewWallet()
        {
            Console.WriteLine("=== Yeni So�uk C�zdan Olu�turuluyor ===\n");

            var wallet = new UniversalColdWallet();

            Console.WriteLine($"Mnemonic: {wallet.GetMnemonic()}");
            Console.WriteLine($"Desteklenen coinler: {string.Join(", ", wallet.GetSupportedCoins())}");
            Console.WriteLine();

            var export = wallet.ExportWallet();
            Console.WriteLine("\nOlu�turulan Adresler:");
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

            Console.WriteLine("\n=== C�zdan ba�ar�yla olu�turuldu! ===");
            Console.WriteLine("�NEML�: Mnemonic kelimelerinizi ve adresleri g�venli bir yerde saklay�n!");
            Console.WriteLine("NOT: Her coin i�in birden fazla adres olu�turuldu. Index de�erlerini not al�n!");

            return wallet;
        }
    }
}