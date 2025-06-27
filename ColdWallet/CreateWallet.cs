using System;
using System.Collections.Generic;
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
                // Special handling for BTC to show all address types
                if (coinSymbol == "BTC")
                {
                    DisplayBitcoinAddresses(wallet, export.Addresses["BTC"]);
                }
                // Check if the coin exists in the export addresses
                else if (export.Addresses.TryGetValue(coinSymbol, out var addresses))
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
        
        /// <summary>
        /// Displays Bitcoin addresses with all address types (Legacy, Nested SegWit, Native SegWit)
        /// </summary>
        public static void DisplayBitcoinAddresses(UniversalColdWallet wallet, List<AddressInfo> addresses)
        {
            Console.WriteLine("\nBitcoin (BTC) Adresleri:");
            
            foreach (var address in addresses)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Index {address.Index}:");
                Console.ResetColor();
                
                // Get all address types for this index
                var addressTypes = wallet.GetBitcoinAddressTypes(address.Index);
                
                Console.WriteLine($"    Legacy (P2PKH):        {addressTypes["Legacy (P2PKH)"]}");
                Console.WriteLine($"    Nested SegWit (P2SH):  {addressTypes["Nested SegWit (P2SH-P2WPKH)"]}");
                Console.WriteLine($"    Native SegWit (Bech32): {addressTypes["Native SegWit (Bech32, P2WPKH)"]}");
                Console.WriteLine($"    Derivation Path: {address.DerivationPath}");
                Console.WriteLine();
            }
        }
    }
}