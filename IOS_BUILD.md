# iOS derlemesi (GitHub Actions)

Bu bilgisayarda iOS derlenmez. `.github/workflows/ios.yml` iki iş çalıştırır:

1. **unity** (ubuntu): `game-ci/unity-builder` ile `IOSBuilder.Build` çalışır, Xcode projesini üretir.
2. **archive** (macOS 26, Xcode 26): projeyi arşivler, App Store Connect API anahtarıyla otomatik imzalar, IPA'yı
   `app-store-connect` yöntemiyle dışa aktarır, `altool` ile App Store Connect'e (TestFlight) yükler. IPA ayrıca artifact olarak saklanır.

## Apple tarafında bir kerelik hazırlık

- Apple Developer hesabında `com.alpigame.mypawpal` paket kimliği (otomatik imza kendisi de oluşturabilir) ve App Store Connect'te bu
  kimlikle bir uygulama kaydı.
- Uygulama içi ürünler: `Assets/Scripts/Sim/Products.cs` içindeki id'lerle aynı (tüketilir / tüketilmez / otomatik yenilenen abonelik).
  Abonelikler tek bir abonelik grubunda olmalı.
- App Store Connect > Kullanıcılar ve Erişim > Entegrasyonlar > App Store Connect API: **Admin** rolünde bir anahtar oluştur
  (bulut yönetimli dağıtım sertifikasını otomatik imza oluşturabilsin diye). `.p8` dosyası yalnızca bir kez indirilir.
- Hesapta kayıtlı en az bir cihaz yoksa arşiv adımında "no devices" imza hatası çıkabilir; telefonunu Certificates, Identifiers &
  Profiles > Devices altına ekle.

## Sırlar

Değerleri komuta yazma; `gh` değeri sorar ya da dosyadan okur. Depo: `bansizhesap78-source/mypawpal`.

```
# Unity Personal lisansı (Unity Hub > Preferences > Licenses > Add > personal ile oluşan .ulf dosyasının içeriği)
gh secret set UNITY_LICENSE  -R bansizhesap78-source/mypawpal < "C:\ProgramData\Unity\Unity_lic.ulf"
gh secret set UNITY_EMAIL    -R bansizhesap78-source/mypawpal
gh secret set UNITY_PASSWORD -R bansizhesap78-source/mypawpal

# Apple
gh secret set APPLE_TEAM_ID  -R bansizhesap78-source/mypawpal      # 10 karakterli Team ID
gh secret set ASC_KEY_ID     -R bansizhesap78-source/mypawpal      # API anahtarının Key ID'si
gh secret set ASC_ISSUER_ID  -R bansizhesap78-source/mypawpal      # Issuer ID (UUID)
gh secret set ASC_KEY_P8     -R bansizhesap78-source/mypawpal < AuthKey_XXXXXXXXXX.p8
```

`ASC_KEY_P8` düz `.p8` içeriği (`-----BEGIN PRIVATE KEY-----` ile başlayan) ya da onun base64'ü olabilir; iş akışı ikisini de tanır.
Yukarıdaki komutlar dosya içeriğini düz metin olarak yükler.

## Çalıştırma

```
gh workflow run ios.yml -R bansizhesap78-source/mypawpal -f build_number=1 -f version=1.0.0 -f upload=true
gh run watch -R bansizhesap78-source/mypawpal
```

- `build_number` boşsa `github.run_number` kullanılır. App Store Connect her yüklemede aynı sürüm için daha büyük bir build numarası ister.
- `version` boşsa projedeki `bundleVersion` kullanılır.
- `upload=false` yalnızca IPA üretir (yüklemez). IPA'yı indirmek için:
  `gh run download <run-id> -R bansizhesap78-source/mypawpal -n MyPawPal-ipa-<build_number>`
- Yükleme bittikten sonra build'in App Store Connect'te "işleniyor" durumundan çıkması birkaç dakika sürer; sonra TestFlight'ta
  ya da uygulama sürümünün "Build" bölümünde seçilir.

## Notlar

- Unity sürümü `ProjectSettings/ProjectVersion.txt`'ten okunur (6000.6.3f1); imaj `unityci/editor:ubuntu-6000.6.3f1-ios-3`.
- `Library` önbelleği `actions/cache` ile saklanır; ilk çalışma uzun sürer.
- Uygulama yalnızca iPhone'dur (iPad desteği kapalı), iOS 15+, ARM64, IL2CPP.
- Development build bu hat tarafından reddedilir (sahte test mağazası içerir).
