using System;
using System.Collections.Generic;

namespace UniversalColdWallet
{
    public class SupportedCoins
    {
        private readonly Dictionary<string, WalletInfo> _coins;

        public SupportedCoins()
        {
            _coins = InitializeCoins();
        }

        public WalletInfo GetCoinInfo(string symbol)
        {
            var upperSymbol = symbol.ToUpper();
            if (!_coins.ContainsKey(upperSymbol))
                throw new ArgumentException($"Desteklenmeyen coin: {symbol}");

            return _coins[upperSymbol];
        }

        public bool IsSupported(string symbol)
        {
            return _coins.ContainsKey(symbol.ToUpper());
        }

        public IReadOnlyList<string> GetAllSymbols()
        {
            return new List<string>(_coins.Keys);
        }

        private static Dictionary<string, WalletInfo> InitializeCoins()
        {
            return new Dictionary<string, WalletInfo>
            {
                ["BTC"] = new WalletInfo { Name = "Bitcoin", DerivationPath = "m/44'/0'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.Bitcoin },
                ["ETH"] = new WalletInfo { Name = "Ethereum", DerivationPath = "m/44'/60'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.Ethereum },
                ["LTC"] = new WalletInfo { Name = "Litecoin", DerivationPath = "m/44'/2'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.Bitcoin },
                ["BCH"] = new WalletInfo { Name = "Bitcoin Cash", DerivationPath = "m/44'/145'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.Bitcoin },
                ["DOGE"] = new WalletInfo { Name = "Dogecoin", DerivationPath = "m/44'/3'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.Bitcoin },
                ["ADA"] = new WalletInfo { Name = "Cardano", DerivationPath = "m/44'/1815'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.Generic },
                ["SOL"] = new WalletInfo { Name = "Solana", DerivationPath = "m/44'/501'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.Generic },
                ["USDT"] = new WalletInfo { Name = "Tether (ERC-20)", DerivationPath = "m/44'/60'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.Ethereum },
                ["USDT_TRC20"] = new WalletInfo { Name = "Tether (TRC-20)", DerivationPath = "m/44'/195'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.Tron },
                ["TRX_TRC20"] = new WalletInfo { Name = "TRON (TRX)", DerivationPath = "m/44'/195'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.Tron },
                ["USDT_BEP20"] = new WalletInfo { Name = "Tether (BEP-20)", DerivationPath = "m/44'/60'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.BinanceSmartChain },
                ["SHIB"] = new WalletInfo { Name = "Shiba Inu (ERC-20)", DerivationPath = "m/44'/60'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.Ethereum },
                ["BNB_BSC"] = new WalletInfo { Name = "Binance Coin (BEP20)", DerivationPath = "m/44'/60'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.BinanceSmartChain },
                ["XRP"] = new WalletInfo { Name = "Ripple", DerivationPath = "m/44'/144'/0'/0", NetworkType = NetworkType.Mainnet, CoinType = CoinType.Bitcoin }
            };
        }
    }
}