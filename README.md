
# 🌟 Universal Cold Wallet - Evrensel Soğuk Cüzdan 🌟

> *"Your keys, your crypto, your peace of mind!"* 🔐

## 🎯 What is Universal Cold Wallet?

Universal Cold Wallet is a secure, offline-first cryptocurrency wallet that puts **YOU** in control of your digital assets. No more trusting third parties with your precious coins! 🚫

### 🎁 Key Features

- 🏰 **Multi-Coin Support**: One wallet to rule them all!
  - BTC, ETH, BNB (BSC), XRP, ADA, SOL, DOGE, LTC, and more! 
  - USDT support across multiple chains (ERC20, BEP20, TRC20)

- 🔒 **Military-Grade Security**:
  - Encrypted storage with AES-256
  - Optional password protection
  - Automatic secure password generation
  - Offline private key generation

- 🌈 **User-Friendly Interface**:
  - Clear menu-driven operations
  - Detailed balance tracking
  - Easy backup and restore options
  - Helpful error messages and guides

- 💎 **Advanced Features**:
  - HD Wallet support (BIP39/44)
  - Multiple address generation per coin
  - Real-time balance checking
  - Secure mnemonic phrase management

## 🚀 Getting Started

1. Clone this repository:
   ```bash
   git clone https://github.com/yourusername/UniversalColdWallet.git
   ```

2. Build the project:
   ```bash
   dotnet build
   ```

3. Run the wallet:
   ```bash
   dotnet run
   ```

## 🛠️ Requirements

- .NET 9.0 or higher
- Windows/Linux/macOS
- Internet connection (only for balance checking)

## 🎮 Basic Usage

1. 🆕 Create a new wallet
2. 📝 Save your mnemonic phrase (VERY IMPORTANT!)
3. 🔐 Set up encryption (optional but recommended)
4. 💰 Start managing your crypto!

## ⚠️ Security Warnings

- 🚨 NEVER share your private keys or mnemonic phrase
- 🚫 Don't store keys digitally (use paper/metal backup)
- 🔍 Always verify addresses before sending funds
- 🏠 Use in a secure, offline environment when possible

## 🤝 Contributing

Found a bug? Want to add a feature? We love contributions! 

1. 🍴 Fork the repository
2. 🌿 Create your feature branch
3. 💻 Make your changes
4. 🎯 Submit a pull request

## 🎭 Funny Side Note

Why did the Bitcoin maximalist bring a ladder to the crypto meetup? 
Because they heard the prices were going through the roof! 📈 

## ⚖️ License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Thanks to the crypto community for inspiration
- Built with love and lots of ☕
- Special thanks to all the pizza 🍕 that fueled this development

## 🐛 Known Issues

- Sometimes the wallet gets too secure and won't let even YOU access it! (Just kidding, but always remember your password!)
- May cause addiction to checking crypto balances
- Still can't make you a crypto millionaire (working on it! 😉)

## 🤔 FAQ

**Q**: Is this really free?
**A**: Yes! As free as your private keys should be! 🆓

**Q**: Can I store a million different coins?
**A**: You can, but maybe start with one and work your way up? 😅

---

Made with ❤️ by H. İlker KÖSELİ

*Remember: Not your keys, not your coins! 🔑*

> "In code we trust, but passwords we must not forget!" - Ancient Crypto Proverb
```



# Universal Cold Wallet v2.0

Önemli kripto para birimleri için soğuk cüzdan (cold wallet) oluşturup yönetmenizi sağlayan .NET 9 tabanlı bir masaüstü uygulaması.

## Genel Bakış

Universal Cold Wallet, çeşitli kripto para birimleri için güvenli bir şekilde adres oluşturmanızı ve özel anahtarlarınızı yönetmenizi sağlar. Uygulama çevrimdışı olarak çalışır ve hassas verileriniz (özel anahtarlar, tohum cümleleri) şifreli olarak saklanır.

## Yeni Özellikler (v2.0)

Bu sürüm, önceki sürüme göre çeşitli iyileştirmeler ve yeni özellikler içerir:

- **TRX (TRON) Coin Desteği**: TRON blockchain'inin yerel tokeni TRX için destek eklendi.
- **Tüm Adreslerin Özel Anahtarlarını Görüntüleme**: Her kripto para birimi için oluşturulan 5 adresin tümünün özel anahtarlarını görüntüleme özelliği eklendi.
- **USDT_TRC20 Özel Anahtar Gösterimi**: TRC20 token özel anahtarlarının doğru şekilde görüntülenmesi için hata düzeltmesi yapıldı.
- **Gelişmiş Hata Yönetimi**: TRON tabanlı token ve adresler için hata yönetimi iyileştirildi.
- **.NET 9 Uyumluluğu**: Codebase .NET 9'a yükseltildi, daha iyi performans ve güvenlik sağlanıyor.

## Düzeltilen Hatalar

- Özel anahtarların görüntülenmesinde yalnızca ilk adresin görüntülenmesi sorunu düzeltildi
- USDT_TRC20 için özel anahtar görüntülemede oluşan "The input string 'TRC20' was not in a correct format" hatası çözüldü
- Underscorelar içeren (örn: USDT_TRC20) coinlerin özel anahtarlarını görüntülerken oluşan ayrıştırma hataları düzeltildi
- Tüm adresler için özel anahtar gösterme opsiyonu eklenerek kullanıcı deneyimi iyileştirildi

## Desteklenen Kripto Para Birimleri

- Bitcoin (BTC)
- Ethereum (ETH)
- Litecoin (LTC)
- Bitcoin Cash (BCH)
- Dogecoin (DOGE)
- Cardano (ADA)
- Solana (SOL)
- Ripple (XRP)
- Tether (USDT) - ERC-20, BEP-20, TRC-20 formatında
- Shiba Inu (SHIB) - ERC-20 formatında
- Binance Coin (BNB) - BEP-20 formatında
- TRON (TRX) - TRC-20 formatında (YENİ!)

## Özellikler

- **Güvenli Adres Üretimi**: BIP-39, BIP-44 standartlarına göre HD (Hierarchical Deterministic) cüzdan üretimi
- **Özel Anahtar Yönetimi**: Her kripto para birimi için güvenli özel anahtar oluşturma ve görüntüleme
- **Çoklu Adres Desteği**: Her kripto para birimi için aynı tohum cümlesinden 5 adres türetme
- **Gelişmiş Şifreleme**: AES-256 şifreleme ile cüzdan verilerinin güvenli saklanması
- **Çevrimdışı İşlem**: İnternet bağlantısı gerekmeden cüzdan oluşturma ve özel anahtarları görüntüleme
- **Bakiye Kontrolü**: İsteğe bağlı olarak çevrimiçi bakiye kontrolü yap