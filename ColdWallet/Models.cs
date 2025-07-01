using System;
using System.Collections.Generic;

namespace UniversalColdWallet
{
    public class AddressInfo
    {
        public required int Index { get; set; }
        public required string Address { get; set; }
        public required string DerivationPath { get; set; }
        public decimal Balance { get; set; }
        public DateTime? LastBalanceUpdate { get; set; }
        public Dictionary<string, decimal>? AddressTypeBalances { get; set; } // For storing balances of different address types (BTC)
    }

    public class WalletExport
    {
        public required string Mnemonic { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required Dictionary<string, List<AddressInfo>> Addresses { get; set; }
        public required List<string> SupportedCoins { get; set; }
        public bool IsEncrypted { get; set; }
        public Dictionary<string, decimal> TotalBalances { get; set; } = new();
        public Dictionary<string, Dictionary<string, decimal>> BtcAddressTypeBalances { get; set; } = new(); // For storing BTC address type balances

        public void UpdateTotalBalances()
        {
            TotalBalances = CalculateTotalBalances();
        }

        private Dictionary<string, decimal> CalculateTotalBalances()
        {
            var totals = new Dictionary<string, decimal>();
            
            if (Addresses == null) return totals;

            foreach (var coin in Addresses)
            {
                if (coin.Value == null) continue;
                
                decimal total = 0;
                foreach (var address in coin.Value)
                {
                    if (address != null)
                    {
                        total += address.Balance;
                    }
                }
                totals[coin.Key] = total;
            }

            return totals;
        }
    }

    public enum NetworkType
    {
        Mainnet,
        Testnet
    }
}