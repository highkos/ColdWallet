using System;

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

            foreach (var coin in export.Addresses)
            {
                Console.WriteLine($"\n{coin.Key} Adresleri:");
                foreach (var address in coin.Value)
                {
                    Console.WriteLine($"  Index {address.Index,2}: {address.Address}");
                    Console.WriteLine($"    Derivation Path: {address.DerivationPath}");
                }
            }

            Console.WriteLine("\n=== C�zdan ba�ar�yla olu�turuldu! ===");
            Console.WriteLine("�NEML�: Mnemonic kelimelerinizi ve adresleri g�venli bir yerde saklay�n!");
            Console.WriteLine("NOT: Her coin i�in birden fazla adres olu�turuldu. Index de�erlerini not al�n!");

            return wallet;
        }
    }
}