using NBitcoin;
using NBitcoin.Altcoins;
using Nethereum.HdWallet;
using Nethereum.Web3.Accounts;
using Nethereum.Signer;
using Nethereum.Util;
using Newtonsoft.Json;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace UniversalColdWallet
{
    /// <summary>
    /// Utility class for generating and verifying TRON addresses
    /// </summary>
    public class TronAddressGenerator
    {
        /// <summary>
        /// Generates a TRON address from a public key
        /// </summary>
        /// <param name="publicKey">The public key in byte array format</param>
        /// <returns>TRON address in Base58 format</returns>
        public static string GenerateTronAddress(byte[] publicKey)
        {
            // If public key starts with 0x04 (uncompressed), remove the prefix
            byte[] pubKeyRaw = publicKey[0] == 4 ? publicKey.Skip(1).ToArray() : publicKey;

            // Calculate Keccak256 hash
            var keccak = new Sha3Keccack();
            byte[] hash = keccak.CalculateHash(pubKeyRaw);

            // Take last 20 bytes (Ethereum address format)
            byte[] addressBytes = hash.Skip(12).ToArray();

            // Add TRON address prefix (0x41)
            byte[] tronAddress = new byte[21];
            tronAddress[0] = 0x41;
            Buffer.BlockCopy(addressBytes, 0, tronAddress, 1, 20);

            // Calculate checksum (double SHA256)
            byte[] checksum = CalculateDoubleSha256(tronAddress);
            byte[] checksumBytes = checksum.Take(4).ToArray();

            // Combine address and checksum
            byte[] finalBytes = new byte[25];
            Buffer.BlockCopy(tronAddress, 0, finalBytes, 0, 21);
            Buffer.BlockCopy(checksumBytes, 0, finalBytes, 21, 4);

            // Base58 encode the result
            return SimpleBase.Base58.Bitcoin.Encode(finalBytes);
        }

        /// <summary>
        /// Generates a TRON address from a private key hex string
        /// </summary>
        /// <param name="privateKeyHex">Private key in hex format (64 characters)</param>
        /// <returns>TRON address in base58 format</returns>
        public static string GenerateTronAddressFromPrivateKey(string privateKeyHex)
        {
            // Create EthECKey from private key
            var ecKey = new EthECKey(privateKeyHex);
            
            // Get the public key bytes
            byte[] publicKey = ecKey.GetPubKey();
            
            // Generate TRON address from public key
            return GenerateTronAddress(publicKey);
        }

        /// <summary>
        /// Calculates double SHA-256 hash (applies SHA-256 twice)
        /// </summary>
        /// <param name="data">Input data to hash</param>
        /// <returns>32-byte double SHA-256 hash</returns>
        private static byte[] CalculateDoubleSha256(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] firstHash = sha256.ComputeHash(data);
                return sha256.ComputeHash(firstHash);
            }
        }
    }

    // Implementing Sha3Keccack using Nethereum's Keccak implementation
    public class Sha3Keccack
    {
        public byte[] CalculateHash(byte[] input)
        {
            // Use Nethereum's implementation of Keccak256
            return new Nethereum.Util.Sha3Keccack().CalculateHash(input);
        }
    }

    public class UniversalColdWallet
    {
        private readonly string _mnemonic;
        private readonly SupportedCoins _supportedCoins;
        private string? _encryptedMnemonic;
        private string _lastUsedPassword = "";  // Son kullanılan şifreyi saklamak için
        private WalletExport? _currentExport;

        public UniversalColdWallet()
        {
            _mnemonic = GenerateMnemonic();
            _supportedCoins = new SupportedCoins();
        }

        public UniversalColdWallet(string existingMnemonic)
        {
            if (string.IsNullOrEmpty(existingMnemonic))
            {
                throw new ArgumentException("Mnemonic boş olamaz.", nameof(existingMnemonic));
            }
            _mnemonic = existingMnemonic;
            _supportedCoins = new SupportedCoins();
        }

        private string GenerateMnemonic()
        {
            var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
            return mnemonic.ToString();
        }

        public string GenerateAddress(string coinSymbol, int accountIndex = 0)
        {
            var coinInfo = _supportedCoins.GetCoinInfo(coinSymbol.ToUpper());

            switch (coinInfo.CoinType)
            {
                case CoinType.Bitcoin:
                    return GenerateBitcoinAddress(coinInfo.DerivationPath, accountIndex, coinSymbol);

                case CoinType.Ethereum:
                case CoinType.BinanceSmartChain:
                    return GenerateEthereumAddress(coinInfo.DerivationPath, accountIndex);

                case CoinType.Tron:
                    return GenerateTronAddress(coinInfo.DerivationPath, accountIndex);

                case CoinType.Generic:
                default:
                    return GenerateGenericAddress(coinInfo.DerivationPath, accountIndex, coinSymbol);
            }
        }

        private string GenerateBitcoinAddress(string derivationPath, int accountIndex, string coinType)
        {
            var mnemonic = new Mnemonic(_mnemonic);
            var hdRoot = mnemonic.DeriveExtKey();
            var keyPath = new NBitcoin.KeyPath(derivationPath + "/" + accountIndex);
            var derivedKey = hdRoot.Derive(keyPath);

            switch (coinType)
            {
                case "BTC":
                    // For BTC, we'll return the native segwit (bech32) address by default
                    // The other address types will be accessible through a dedicated method
                    return derivedKey.PrivateKey.PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main).ToString();

                case "LTC":
                    // LTC için kendi Bech32 implementasyonumuzu kullan
                    var pubKeyHash = derivedKey.PrivateKey.PubKey.Hash.ToBytes();
                    return GenerateLitecoinBech32Address(pubKeyHash);

                case "BCH":
                    var bchAddress = derivedKey.PrivateKey.PubKey.GetAddress(ScriptPubKeyType.Legacy, BCash.Instance.Mainnet);
                    var cashAddr = bchAddress.ToString().Replace("bitcoincash:", "");
                    return $"bitcoincash:{cashAddr}";

                case "DOGE":
                    var dogeNetwork = Dogecoin.Instance.Mainnet;
                    var dogePrivKey = derivedKey.PrivateKey;
                    var dogeAddress = dogePrivKey.PubKey.GetAddress(ScriptPubKeyType.Legacy, dogeNetwork);
                    return dogeAddress.ToString();

                case "XRP":
                    return GenerateXRPAddress(derivedKey);

                default:
                    return derivedKey.PrivateKey.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main).ToString();
            }
        }

        /// <summary>
        /// Generates a proper Litecoin Bech32 address according to BIP 173
        /// Based on reference implementation from bitcoin/bech32
        /// </summary>
        /// <param name="pubKeyHash">20-byte public key hash</param>
        /// <returns>Litecoin Bech32 address starting with "ltc1"</returns>
        private static string GenerateLitecoinBech32Address(byte[] pubKeyHash)
        {
            if (pubKeyHash.Length != 20)
                throw new ArgumentException("Public key hash must be 20 bytes", nameof(pubKeyHash));

            const string hrp = "ltc";
            const int witnessVersion = 0;

            // Convert the 20-byte hash to 5-bit groups
            var converted = ConvertBitsToBase32(pubKeyHash, 8, 5, true);
            
            // Create spec: version + converted data
            var spec = new List<int> { witnessVersion };
            spec.AddRange(converted);

            // Calculate bech32 checksum
            var checksum = CalculateBech32Checksum(hrp, spec);
            
            // Combine all parts
            var combined = spec.Concat(checksum).ToArray();

            // Encode to bech32 string
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

        /// <summary>
        /// Convert bits from one base to another (reference implementation)
        /// </summary>
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

        /// <summary>
        /// Calculate Bech32 checksum according to BIP 173 reference
        /// </summary>
        private static List<int> CalculateBech32Checksum(string hrp, List<int> data)
        {
            // HRP expansion: high bits + [0] + low bits
            var values = new List<int>();
            
            // High 3 bits of each character
            foreach (char c in hrp)
                values.Add(c >> 5);
            
            // Separator
            values.Add(0);
            
            // Low 5 bits of each character  
            foreach (char c in hrp)
                values.Add(c & 31);
            
            // Add the data
            values.AddRange(data);
            
            // Add 6 zero bytes for checksum space
            values.AddRange(Enumerable.Repeat(0, 6));
            
            // Calculate polymod
            uint polymod = CalculateBech32PolymodForLTC(values) ^ 1;
            
            // Extract 6 5-bit checksum values
            var checksum = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                checksum.Add((int)((polymod >> (5 * (5 - i))) & 31));
            }
            
            return checksum;
        }

        /// <summary>
        /// Bech32 polymod calculation (reference implementation)
        /// </summary>
        private static uint CalculateBech32PolymodForLTC(List<int> values)
        {
            // Generator polynomial constants for bech32
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

        /// <summary>
        /// Generates different Bitcoin address types for the same private key
        /// </summary>
        /// <param name="accountIndex">Account index to use (default 0)</param>
        /// <returns>A dictionary with address types and addresses</returns>
        public Dictionary<string, string> GetBitcoinAddressTypes(int accountIndex = 0)
        {
            var coinInfo = _supportedCoins.GetCoinInfo("BTC");
            var mnemonic = new Mnemonic(_mnemonic);
            var hdRoot = mnemonic.DeriveExtKey();
            var keyPath = new NBitcoin.KeyPath(coinInfo.DerivationPath + "/" + accountIndex);
            var derivedKey = hdRoot.Derive(keyPath);
            var pubKey = derivedKey.PrivateKey.PubKey;
            
            return new Dictionary<string, string>
            {
                { "Legacy (P2PKH)", pubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main).ToString() },
                { "Nested SegWit (P2SH-P2WPKH)", pubKey.GetAddress(ScriptPubKeyType.SegwitP2SH, Network.Main).ToString() },
                { "Native SegWit (Bech32, P2WPKH)", pubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main).ToString() }
            };
        }

        private string GenerateXRPAddress(ExtKey derivedKey)
        {
            // Get the public key bytes (secp256k1)
            var publicKey = derivedKey.PrivateKey.PubKey.ToBytes(true);

            // Calculate RIPEMD160(SHA256(publicKey))
            using var sha256 = SHA256.Create();
            var sha256Hash = sha256.ComputeHash(publicKey);
            
            // Use NBitcoin's Hash160 (RIPEMD160(SHA256(x)))
            var rippleKeyHash = derivedKey.PrivateKey.PubKey.Hash.ToBytes();

            // Prepend XRP address version byte (0x00)
            var addressBytes = new byte[21];
            addressBytes[0] = 0x00;
            Array.Copy(rippleKeyHash, 0, addressBytes, 1, 20);

            // Create double SHA256 checksum
            var checksum = sha256.ComputeHash(sha256.ComputeHash(addressBytes));

            // Combine address bytes and checksum
            var finalBytes = new byte[25];
            Array.Copy(addressBytes, 0, finalBytes, 0, 21);
            Array.Copy(checksum, 0, finalBytes, 21, 4);

            // Convert to Base58 using Ripple's alphabet
            const string ALPHABET = "rpshnaf39wBUDNEGHJKLM4PQRST7VWXYZ2bcdeCg65jkm8oFqi1tuvAxyz";
            var result = new StringBuilder();
            
            // Convert to BigInteger for proper Base58 encoding
            var bigInt = System.Numerics.BigInteger.Zero;
            for (int i = 0; i < finalBytes.Length; i++)
            {
                bigInt = (bigInt * 256) + finalBytes[i];
            }
            
            // Convert to Base58
            while (bigInt > 0)
            {
                var remainder = (int)(bigInt % 58);
                bigInt /= 58;
                result.Insert(0, ALPHABET[remainder]);
            }
            
            // Add leading zeros as appropriate characters
            for (int i = 0; i < finalBytes.Length && finalBytes[i] == 0; i++)
            {
                result.Insert(0, ALPHABET[0]);
            }
            
            var address = result.ToString();
            
            // XRP addresses must start with 'r'
            if (!address.StartsWith("r"))
            {
                throw new InvalidOperationException("Generated XRP address is invalid: does not start with 'r'");
            }

            return address;
        }

        private string GenerateTronAddress(string derivationPath, int accountIndex)
        {
            // Get the private key for TRON
            string privateKeyHex = GetPrivateKeyHex(derivationPath, accountIndex);

            // Generate the TRON address from the private key
            return TronAddressGenerator.GenerateTronAddressFromPrivateKey(privateKeyHex);
        }
        
        // Helper method to get a raw private key hex string from derivation path
        private string GetPrivateKeyHex(string derivationPath, int accountIndex)
        {
            var mnemonic = new Mnemonic(_mnemonic);
            var hdRoot = mnemonic.DeriveExtKey();
            var keyPath = new NBitcoin.KeyPath(derivationPath + "/" + accountIndex);
            var derivedKey = hdRoot.Derive(keyPath);
            return derivedKey.PrivateKey.ToHex();
        }

        private string GenerateEthereumAddress(string derivationPath, int accountIndex)
        {
            var wallet = new Wallet(_mnemonic, string.Empty);
            var account = wallet.GetAccount(accountIndex);
            return account.Address;
        }

        private static string Bech32Encode(string hrp, byte[] data)
        {
            const string CHARSET = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";

            var values = ConvertBits(data, 8, 5, true);
            var checksum = CreateChecksum(hrp, values);

            var combined = values.Concat(checksum).ToArray();

            var ret = new StringBuilder(hrp + "1");
            foreach (var b in combined)
            {
                ret.Append(CHARSET[b]);
            }

            return ret.ToString();
        }

        private static byte[] ConvertBits(byte[] data, int fromBits, int toBits, bool pad)
        {
            var acc = 0;
            var bits = 0;
            var maxv = (1 << toBits) - 1;
            var result = new List<byte>();

            foreach (var value in data)
            {
                if ((value >> fromBits) > 0)
                    throw new ArgumentException("Invalid data range");

                acc = (acc << fromBits) | value;
                bits += fromBits;

                while (bits >= toBits)
                {
                    bits -= toBits;
                    result.Add((byte)((acc >> bits) & maxv));
                }
            }

            if (pad)
            {
                if (bits > 0)
                    result.Add((byte)((acc << (toBits - bits)) & maxv));
            }
            else if (bits >= fromBits || (((acc << (toBits - bits)) & maxv) != 0))
            {
                throw new ArgumentException("Invalid padding");
            }

            return result.ToArray();
        }

        private static byte[] CreateChecksum(string hrp, byte[] data)
        {
            int[] values = hrp.Select(c => c >> 5).Concat(new[] { 0 })
                .Concat(hrp.Select(c => c & 31))
                .Concat(data.Select(b => (int)b))
                .ToArray();

            uint poly = PolyMod(values.Concat(new[] { 0, 0, 0, 0, 0, 0 }).ToArray()) ^ 1;
            byte[] ret = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                ret[i] = (byte)((poly >> (5 * (5 - i))) & 31);
            }
            return ret;
        }

        private static uint PolyMod(int[] values)
        {
            uint chk = 1;
            uint[] generator = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };

            foreach (var value in values)
            {
                uint top = chk >> 25;
                chk = (uint)(((chk & 0x1ffffff) << 5) ^ value);
                for (int i = 0; i < 5; i++)
                {
                    if (((top >> i) & 1) == 1)
                        chk ^= generator[i];
                }
            }
            return chk;
        }

        private static string Base58EncodeSolana(byte[] data)
        {
            const string ALPHABET = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            
            // Convert to BigInteger
            System.Numerics.BigInteger intData = 0;
            for (int i = 0; i < data.Length; i++)
            {
                intData = intData * 256 + data[i];
            }

            // Convert to base58 string
            var result = new StringBuilder();
            while (intData > 0)
            {
                int remainder = (int)(intData % 58);
                intData /= 58;
                result.Insert(0, ALPHABET[remainder]);
            }

            // Add leading zeros
            for (int i = 0; i < data.Length && data[i] == 0; i++)
            {
                result.Insert(0, ALPHABET[0]);
            }

            return result.ToString();
        }

        private string GenerateCardanoAddress(ExtKey derivedKey)
        {
            var paymentPubKey = derivedKey.PrivateKey.PubKey.ToBytes();
            var paymentKeyHash = SHA256.HashData(paymentPubKey);

            var stakingKeyPath = new NBitcoin.KeyPath("2/0");
            var stakingKey = derivedKey.Derive(stakingKeyPath);
            var stakingPubKey = stakingKey.PrivateKey.PubKey.ToBytes();
            var stakingKeyHash = SHA256.HashData(stakingPubKey);

            byte headerByte = 0x01;

            var addressBytes = new List<byte>
            {
                headerByte
            };
            addressBytes.AddRange(paymentKeyHash.Take(28));
            addressBytes.AddRange(stakingKeyHash.Take(28));

            return Bech32Encode("addr", addressBytes.ToArray());
        }

        private string GenerateSolanaAddress(ExtKey derivedKey)
        {
            // Get the seed bytes from derived key
            byte[] seedBytes = derivedKey.PrivateKey.ToBytes();
            
            // Create a unique seed for Solana derivation using HMAC-SHA512
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes("ed25519 seed"));
            var derivedSeed = hmac.ComputeHash(seedBytes);
            
            // We'll use the first 32 bytes for the private key
            var privateKeyBytes = derivedSeed.Take(32).ToArray();
            
            // Use SHA512 to derive the public key (Ed25519 spec)
            using var sha512 = SHA512.Create();
            var hash = sha512.ComputeHash(privateKeyBytes);
            
            // Clamp the private key according to Ed25519 spec
            hash[0] &= 248;
            hash[31] &= 127;
            hash[31] |= 64;
            
            // Use the first 32 bytes as public key
            var publicKeyBytes = hash.Take(32).ToArray();
            
            // In Solana, the public key IS the address (when Base58 encoded)
            return Base58EncodeSolana(publicKeyBytes);
        }

        private string GenerateGenericAddress(string derivationPath, int accountIndex, string coinType)
        {
            var mnemonic = new Mnemonic(_mnemonic);
            var hdRoot = mnemonic.DeriveExtKey();
            var keyPath = new NBitcoin.KeyPath(derivationPath + "/" + accountIndex);
            var derivedKey = hdRoot.Derive(keyPath);

            if (coinType == "ADA")
            {
                return GenerateCardanoAddress(derivedKey);
            }
            else if (coinType == "SOL")
            {
                return GenerateSolanaAddress(derivedKey);
            }

            var pubKeyHash = derivedKey.PrivateKey.PubKey.Hash.ToString();
            return $"{coinType.ToLower()}_{pubKeyHash.Substring(0, 20)}";
        }

        public string GetPrivateKey(string coinSymbol, int accountIndex = 0)
        {
            if (string.IsNullOrEmpty(coinSymbol))
                throw new ArgumentException("Coin sembolü boş olamaz.", nameof(coinSymbol));

            var coinInfo = _supportedCoins.GetCoinInfo(coinSymbol.ToUpper());

            // Get the correct mnemonic (decrypted if encrypted)
            string mnemonicToUse;
            if (HasPassword)
            {
                if (string.IsNullOrEmpty(_lastUsedPassword))
                    throw new InvalidOperationException("Cüzdan şifreli. Önce SetCurrentPassword ile şifreyi ayarlayın.");
                
                try
                {
                    mnemonicToUse = SetGetPassword.DecryptMnemonic(_encryptedMnemonic!, _lastUsedPassword);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Mnemonic çözülemedi: {ex.Message}");
                }
            }
            else
            {
                mnemonicToUse = _mnemonic;
            }

            // Create HD wallet from mnemonic
            var mnemonic = new Mnemonic(mnemonicToUse);
            var hdRoot = mnemonic.DeriveExtKey();
            var keyPath = new NBitcoin.KeyPath(coinInfo.DerivationPath + "/" + accountIndex);
            var derivedKey = hdRoot.Derive(keyPath);

            // Return private key based on coin type
            switch (coinSymbol.ToUpper())
            {
                case "ETH":
                case "USDT":
                case "SHIB":
                    var ethWallet = new Wallet(mnemonicToUse, string.Empty);
                    var ethAccount = ethWallet.GetAccount(accountIndex);
                    return ethAccount.PrivateKey;

                case "BNB_BSC":
                case "USDT_BEP20":
                    var bscWallet = new Wallet(mnemonicToUse, string.Empty);
                    var bscAccount = bscWallet.GetAccount(accountIndex);
                    return bscAccount.PrivateKey;

                case "USDT_TRC20":
                    // For TRC20, we need to derive the key and ensure it matches the address format
                    return derivedKey.PrivateKey.ToHex(); // Return just the hex without 0x prefix
                
                case "BTC":
                case "LTC":
                case "BCH":
                case "DOGE":
                case "XRP":
                case "ADA":
                case "SOL":
                default:
                    // Tüm coinler için hex formatında private key döndür
                    return derivedKey.PrivateKey.ToHex();
            }
        }

        public WalletExport ExportWallet()
        {
            // Eğer mevcut export varsa onu kullan
            if (_currentExport != null)
            {
                return _currentExport;
            }

            var addresses = new Dictionary<string, List<AddressInfo>>();
            
            // Get all supported coins in their original order from SupportedCoins
            var supportedSymbols = _supportedCoins.GetAllSymbols();
            
            // Add all addresses to the dictionary maintaining the order from SupportedCoins
            foreach (var coinSymbol in supportedSymbols)
            {
                var coinInfo = _supportedCoins.GetCoinInfo(coinSymbol);
                var coinAddresses = new List<AddressInfo>();

                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        var address = GenerateAddress(coinSymbol, i);
                        coinAddresses.Add(new AddressInfo
                        {
                            Index = i,
                            Address = address,
                            DerivationPath = coinInfo.DerivationPath + "/" + i
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{coinSymbol} için adres oluşturulamadı: {ex.Message}");
                    }
                }

                addresses[coinSymbol] = coinAddresses;
            }

            _currentExport = new WalletExport
            {
                Mnemonic = _mnemonic,
                CreatedAt = DateTime.UtcNow,
                Addresses = addresses,
                SupportedCoins = supportedSymbols.ToList()
            };

            _currentExport.UpdateTotalBalances();
            return _currentExport;
        }

        public void SaveToFile(string filePath, string? password = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Dosya yolu boş olamaz.", nameof(filePath));
            }

            string fullPath = Path.GetFullPath(filePath);
            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var export = ExportWallet();
            export.UpdateTotalBalances(); // TotalBalances'ı güncelle

            // Şifreli cüzdan kontrolü
            if (HasPassword)
            {
                if (string.IsNullOrEmpty(password))
                {
                    throw new ArgumentException("Bu cüzdan şifreli. Kaydetmek için şifre gerekli.");
                }
                
                export.Mnemonic = _encryptedMnemonic!;
                export.IsEncrypted = true;
                
                string jsonData = JsonConvert.SerializeObject(export, Formatting.Indented);
                string encryptedData = SetGetPassword.EncryptString(jsonData, password);
                File.WriteAllText(fullPath, encryptedData);
            }
            else
            {
                string jsonData = JsonConvert.SerializeObject(export, Formatting.Indented);
                File.WriteAllText(fullPath, jsonData);
            }

            Console.WriteLine($"Cüzdan güvenli şekilde kaydedildi: {fullPath}");
        }

        public static UniversalColdWallet LoadFromFile(string filePath, string? password = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Dosya yolu boş olamaz.", nameof(filePath));
            }

            string fullPath = Path.GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Cüzdan dosyası bulunamadı: {fullPath}");
            }

            string jsonData = File.ReadAllText(fullPath);
            WalletExport? export;

            try
            {
                export = JsonConvert.DeserializeObject<WalletExport>(jsonData);

                if (export == null)
                {
                    throw new InvalidOperationException("JSON verileri geçerli bir cüzdan formatında değil.");
                }

                if (export.IsEncrypted)
                {
                    if (string.IsNullOrEmpty(password))
                    {
                        throw new ArgumentException("Bu cüzdan şifreli. Şifre gerekli.");
                    }

                    try
                    {
                        var decryptedMnemonic = SetGetPassword.DecryptString(export.Mnemonic, password);
                        var wallet = new UniversalColdWallet(decryptedMnemonic);
                        wallet._currentExport = export; // Mevcut export'u sakla
                        wallet._encryptedMnemonic = export.Mnemonic; // Şifreli mnemonic'i sakla
                        return wallet;
                    }
                    catch (CryptographicException)
                    {
                        throw new ArgumentException("Girilen şifre yanlış!");
                    }
                }

                var unencryptedWallet = new UniversalColdWallet(export.Mnemonic);
                unencryptedWallet._currentExport = export; // Mevcut export'u sakla
                return unencryptedWallet;
            }
            catch (JsonReaderException)
            {
                if (string.IsNullOrEmpty(password))
                {
                    throw new ArgumentException("Bu cüzdan şifreli. Şifre gerekli.");
                }

                try
                {
                    var decryptedJson = SetGetPassword.DecryptString(jsonData, password);
                    export = JsonConvert.DeserializeObject<WalletExport>(decryptedJson)
                        ?? throw new InvalidOperationException("JSON verileri geçerli bir cüzdan formatında değil.");

                    var wallet = new UniversalColdWallet(export.Mnemonic);
                    wallet._currentExport = export; // Mevcut export'u sakla
                    wallet.EncryptMnemonic(password);
                    return wallet;
                }
                catch (CryptographicException)
                {
                    throw new ArgumentException("Girilen şifre yanlış!");
                }
            }
        }

        private string EncryptString(string text, string password)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(password);

            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("şifrelenecek metin boş olamaz.", nameof(text));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("şifre boş olamaz.", nameof(password));

            return SetGetPassword.EncryptString(text, password);
        }

        private string DecryptString(string encryptedText, string password)
        {
            ArgumentNullException.ThrowIfNull(encryptedText);
            ArgumentNullException.ThrowIfNull(password);

            if (string.IsNullOrEmpty(encryptedText))
                throw new ArgumentException("şifrelenmiş metin boş olamaz.", nameof(encryptedText));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("şifre boş olamaz.", nameof(password));

            return SetGetPassword.DecryptString(encryptedText, password);
        }

        public void EncryptMnemonic(string password)
        {
            _encryptedMnemonic = SetGetPassword.EncryptMnemonic(_mnemonic, password);
        }

        public bool VerifyPassword(string password)
        {
            return SetGetPassword.VerifyPassword(_encryptedMnemonic!, password, _mnemonic);
        }

        public void ChangePassword(string currentPassword, string newPassword)
        {
            _encryptedMnemonic = SetGetPassword.ChangePassword(_encryptedMnemonic!, currentPassword, newPassword, _mnemonic);
        }

        public bool HasPassword => !string.IsNullOrEmpty(_encryptedMnemonic);

        public string GetMnemonic() => _mnemonic;
        public List<string> GetSupportedCoins() => _supportedCoins.GetAllSymbols().ToList();

        public void UpdateBalance(string coinSymbol, int accountIndex, decimal balance)
        {
            ArgumentException.ThrowIfNullOrEmpty(coinSymbol, nameof(coinSymbol));

            var export = ExportWallet();
            
            // Normalize coin symbol
            var normalizedCoinSymbol = coinSymbol.ToUpper();
            
            // Special handling for TRX_TRC20 and TRX which should be treated the same
            if (normalizedCoinSymbol == "TRX")
            {
                normalizedCoinSymbol = "TRX_TRC20";
            }
            
            // Check if Addresses is null before accessing it
            if (export.Addresses == null)
            {
                throw new InvalidOperationException("Addresses collection is null in wallet export");
            }
            
            // Find the matching coin key in the Addresses dictionary
            var matchingCoinKey = export.Addresses.Keys
                .FirstOrDefault(k => k.Equals(normalizedCoinSymbol, StringComparison.OrdinalIgnoreCase));
            
            if (matchingCoinKey == null)
            {
                // More informative error message
                var supportedCoins = string.Join(", ", export.Addresses.Keys);
                throw new ArgumentException($"Coin bulunamadı: {coinSymbol}. Desteklenen coinler: {supportedCoins}");
            }

            // Adres ve bakiye güncellemesi
            var addresses = export.Addresses[matchingCoinKey];
            var address = addresses.FirstOrDefault(a => a.Index == accountIndex);

            if (address == null)
            {
                address = new AddressInfo
                {
                    Index = accountIndex,
                    Address = GenerateAddress(matchingCoinKey, accountIndex),
                    DerivationPath = $"m/44'/60'/0'/0/{accountIndex}"
                };
                addresses.Add(address);
            }

            address.Balance = balance;
            address.LastBalanceUpdate = DateTime.UtcNow;

            // TotalBalances'ı güncelle
            export.UpdateTotalBalances();

            // Son kullanılan şifreyi kullanarak kaydet
            if (HasPassword && !string.IsNullOrEmpty(_lastUsedPassword))
            {
                SaveToFile("cold_wallet.json", _lastUsedPassword);
            }
            else
            {
                SaveToFile("cold_wallet.json");
            }
        }

        public void UpdateAllBalances(Dictionary<string, Dictionary<int, decimal>> balances)
        {
            ArgumentNullException.ThrowIfNull(balances);

            var export = ExportWallet();
            if (export.Addresses == null)
                return;

            bool hasChanges = false;

            foreach (var coinBalance in balances)
            {
                var coinSymbol = coinBalance.Key;
                if (!export.Addresses.ContainsKey(coinSymbol))
                    continue;

                var addresses = export.Addresses[coinSymbol];
                foreach (var indexBalance in coinBalance.Value)
                {
                    var address = addresses.FirstOrDefault(a => a.Index == indexBalance.Key);
                    if (address != null)
                    {
                        if (address.Balance != indexBalance.Value)
                        {
                            hasChanges = true;
                            address.Balance = indexBalance.Value;
                            address.LastBalanceUpdate = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Yeni adres oluştur
                        address = new AddressInfo
                        {
                            Index = indexBalance.Key,
                            Address = GenerateAddress(coinSymbol, indexBalance.Key),
                            DerivationPath = $"m/44'/60'/0'/0/{indexBalance.Key}",
                            Balance = indexBalance.Value,
                            LastBalanceUpdate = DateTime.UtcNow
                        };
                        addresses.Add(address);
                        hasChanges = true;
                    }
                }
            }

            if (hasChanges)
            {
                export.UpdateTotalBalances();

                // Son kullanılan şifreyi kullanarak kaydet
                if (HasPassword && !string.IsNullOrEmpty(_lastUsedPassword))
                {
                    SaveToFile("cold_wallet.json", _lastUsedPassword);
                }
                else
                {
                    SaveToFile("cold_wallet.json");
                }

                // Define the order of coins for display
                string[] coinOrder = new string[] 
                { 
                    "BTC", "ETH", "LTC", "BCH", "DOGE", "ADA", "SOL", 
                    "USDT", "USDT_TRC20", "USDT_BEP20", "BNB_BSC", "TRX_TRC20",
                    "SHIB", "XRP" 
                };
                
                Console.WriteLine($"\nBakiyeler güncellendi:");
                
                // Create an ordered dictionary based on the defined order
                var orderedBalances = new Dictionary<string, decimal>();
                
                // First add coins in our defined order
                foreach (var coin in coinOrder)
                {
                    if (export.TotalBalances.TryGetValue(coin, out decimal balance))
                    {
                        orderedBalances[coin] = balance;
                        Console.WriteLine($"{coin}: {balance:N8}");
                    }
                }
                
                // Then add any remaining coins not in our predefined order
                foreach (var balance in export.TotalBalances)
                {
                    if (!orderedBalances.ContainsKey(balance.Key))
                    {
                        Console.WriteLine($"{balance.Key}: {balance.Value:N8}");
                    }
                }
            }
        }

        public void SetCurrentPassword(string password)
        {
            if (HasPassword && !string.IsNullOrEmpty(password))
            {
                _lastUsedPassword = password;
            }
        }

        public Dictionary<string, decimal> GetTotalBalances()
        {
            var export = ExportWallet();
            return export.TotalBalances;
        }

        public decimal GetBalance(string coinSymbol, int accountIndex = 0)
        {
            ArgumentException.ThrowIfNullOrEmpty(coinSymbol, nameof(coinSymbol));

            var export = ExportWallet();
            
            // Normalize coin symbol
            var normalizedCoinSymbol = coinSymbol.ToUpper();
            
            // Special handling for TRX_TRC20 and TRX which should be treated the same
            if (normalizedCoinSymbol == "TRX")
            {
                normalizedCoinSymbol = "TRX_TRC20";
            }
            
            // Check if Addresses is null before accessing it
            if (export.Addresses == null)
            {
                return 0;
            }
            
            // Find the matching coin key in the Addresses dictionary
            var matchingCoinKey = export.Addresses.Keys
                .FirstOrDefault(k => k.Equals(normalizedCoinSymbol, StringComparison.OrdinalIgnoreCase));
                
            if (matchingCoinKey == null)
            {
                return 0;
            }

            var addresses = export.Addresses[matchingCoinKey];
            var address = addresses.FirstOrDefault(a => a.Index == accountIndex);

            return address?.Balance ?? 0;
        }

        public DateTime? GetLastBalanceUpdate(string coinSymbol, int accountIndex = 0)
        {
            ArgumentException.ThrowIfNullOrEmpty(coinSymbol, nameof(coinSymbol));

            var export = ExportWallet();
            
            // Normalize coin symbol
            var normalizedCoinSymbol = coinSymbol.ToUpper();
            
            // Special handling for TRX_TRC20 and TRX which should be treated the same
            if (normalizedCoinSymbol == "TRX")
            {
                normalizedCoinSymbol = "TRX_TRC20";
            }
            
            // Check if Addresses is null before accessing it
            if (export.Addresses == null)
            {
                return null;
            }
            
            // Find the matching coin key in the Addresses dictionary
            var matchingCoinKey = export.Addresses.Keys
                .FirstOrDefault(k => k.Equals(normalizedCoinSymbol, StringComparison.OrdinalIgnoreCase));
                
            if (matchingCoinKey == null)
            {
                return null;
            }

            var addresses = export.Addresses[matchingCoinKey];
            var address = addresses.FirstOrDefault(a => a.Index == accountIndex);

            return address?.LastBalanceUpdate;
        }
    }
}