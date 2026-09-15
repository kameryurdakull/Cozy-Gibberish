# GitHub Publishing

Bu depo bir Unity projesi değil, doğrudan Unity Package Manager paketidir. `package.json`, `Runtime`, `Editor`, `Tests`, `Samples~` ve `Documentation~` depo kökünde tutulur.

## Sürüm yayınlama

1. `package.json` ve `CHANGELOG.md` sürümlerini birlikte güncelleyin.
2. `npm pack --dry-run` ile dağıtıma girecek dosyaları denetleyin.
3. Unity Test Framework ile EditMode ve PlayMode testlerini çalıştırın.
4. Değişiklikleri `main` dalına birleştirin.
5. Paket sürümüyle aynı etiketi oluşturun:

```bash
git tag -a v2.0.0 -m "Cozy Gibberish 2.0.0"
git push origin main
git push origin v2.0.0
```

## UPM kurulumu

Kararlı ve tekrar üretilebilir kurulum için kullanıcılar dal adı yerine sürüm etiketi kullanmalıdır:

```text
https://github.com/kameryurdakull/Cozy-Gibberish.git#v2.0.0
```

Yeni sürüm yayınlandığında SemVer etiketi ve `package.json` sürümü birebir aynı tutulmalıdır.

## Lisans

Mevcut `LICENSE.md`, tüm hakları saklı bir dağıtım tanımlar. Paket açık kaynak olarak yayınlanacaksa ilk public release öncesinde uygun lisans metni seçilmeli ve `package.json` alanı buna göre güncellenmelidir.
