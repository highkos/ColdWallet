namespace UniversalColdWallet
{
    public class WalletInfo
    {
        public required string Name { get; set; }
        public required string DerivationPath { get; set; }
        public required NetworkType NetworkType { get; set; }
        public required CoinType CoinType { get; set; }
    }
}