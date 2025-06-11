using System;

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

            foreach (var coin in export.Addresses)
            {
                Console.WriteLine($"\n{coin.Key} Adresleri:");
                foreach (var address in coin.Value)
                {
                    Console.WriteLine($"  Index {address.Index,2}: {address.Address}");
                    Console.WriteLine($"    Derivation Path: {address.DerivationPath}");
                }
            }

            Console.WriteLine("\n=== Cüzdan baþarýyla oluþturuldu! ===");
            Console.WriteLine("ÖNEMLÝ: Mnemonic kelimelerinizi ve adresleri güvenli bir yerde saklayýn!");
            Console.WriteLine("NOT: Her coin için birden fazla adres oluþturuldu. Index deðerlerini not alýn!");

            return wallet;
        }
    }
}