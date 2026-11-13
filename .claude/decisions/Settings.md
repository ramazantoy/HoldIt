# Settings (F7)

Bu modülün kararları, doğrulananları ve inceleme bulguları; çapraz kararlar ve "Açık" listesi `README.md`'de.
Karar değişirse satır silinmez; eskisinin üstü çizilir, yenisi tarihle eklenir.

> Dil ve Unity Localization: `Localization.md`. Addressables içerik build'i: `Build.md`. Kanal sesleri API'si: `Audio.md`.

## F7 kararları (2026-11-01)
Oyuncu ayarları ve dil. Kapsam: yeni `AF.Settings` ve `AF.Localization` modülleri (+Editor, Tests), Audio (`AudioVolumeChanged`,
`UseAudio` imzası), Input (`ConfineCursor`), Build (Addressables içerik adımı), Game (`GameRootScope`, `Game.asmdef`,
`Assets/Game/Localization/`), `Assets/AddressableAssetsData/`, paket, PlayerSettings, `.gitignore`.
| Konu | Karar | Gerekçe |
|---|---|---|
| Nerede durur | Device `settings.display` (çözünürlük, pencere modu, vsync, kare sınırı, kalite, confine) ve `settings.audio` (kanal sesleri, MuteWhenUnfocused); Global `localization.locale` ve `input.bindings`. Oynanış ayarı oyunun kendi Global savable'ı (Settings.md'de örnek, template'te kod yok) | Ekran ve ses donanıma bağlı (Deck/PC ayarlarının birbirini ezmesi bu değerlerdi); dil ve tuş oyuncuya ait |
| Modül sınırı | `AF.Settings` (→ Core, Save, Audio, Input) ekran + ses kalıcılığı; `AF.Localization` (→ Core, Save, Unity.Localization) dil + dilin kalıcılığı; Settings Localization'ı bilmez | Tek dilli oyun Localization'ı ve paketi çıkarabilir (Localization.md "Removing the module"); Audio kalıcılıksız kalır (F6) |
| Ses durumu | `IAudioVolumes` tek sahip; `VolumeSettings` `Capture`'da okur, `Restore`'da yazar, kopyası yok. Grup adıyla anahtarlı; eksik kanal 1, bilinmeyen anahtar uyarıyla düşer. `AudioVolumeChanged {Channel, Volume}` yalnız değişince; `UseAudio(mixer, messagePipe, options)` | Kim `Set` çağırırsa kaydedilir; mesajı durum sahibi atar |
| Ekran modeli | `IDisplaySettings`: `Current` (`DisplayState`, yürürlükteki değerler), `Resolutions`/`WindowModes`/`QualityLevels`/`HasWindow`, setter'lar, `ResetToDefaults`. Kendi `WindowMode {Windowed, Borderless, Exclusive}`. Kalite adla | Liste tek kaynaktan (elle liste + index yok); kayıtta Unity enum'u yok |
| Varsayılan ↔ seçim | Data alanları nullable; `null` = varsayılanı izle; varsayılana eşit seçim `null` yazılır. Varsayılanlar `SettingsOptions` (PC Borderless/vsync/sınırsız, mobil 60) + ekranın kendi boyutu + başlangıç kalite seviyesi | Donanıma/sürüme bağlı varsayılan dosyada donmaz; eklenen alan eski dosyada `null` gelir, migration gerekmez |
| Uygulama | Tek `Apply`: kalite → `vSyncCount` → `targetFrameRate` (vsync açıkken -1, mobilde 0 = yenileme hızı) → `SetResolution` yalnız farkta → confine; her adım ayrı try/catch + `Log.Exception`. Değişince `DisplaySettingsChanged {Previous, Current}` | Kalite seviyesi vsync'i sıfırlıyor; mobilde -1 = 30 FPS; açılışta titreme yok; boş catch yok |
| Geçersiz kayıt | O oturum varsayılan + `Log.Warning`, seçim kayıtta kalır. Setter geçersiz değerde fırlatır | Monitör geri takılınca seçim döner; çağıran hatası sessiz kalmaz |
| Listeler | Çözünürlük W×H tekil, büyükten küçüğe, ekranın kendi boyutuna kadar; pencerede görev çubuğu alanına başlık payıyla (64 px) sığanlar (inceleme). Ekran boyutu `Screen.mainWindowDisplayInfo`, exclusive'de önceki değer / `Display.main` (inceleme). Exclusive yalnız Windows | `currentResolution` fullscreen'de oyunun çözünürlüğü (eski tuzak); 1080p'de 1680x1050 pencere sığmıyordu |
| Yazma zamanı | Setter diske yazmaz; zamanlayıcı yok. Ekran kapanırken oyun `SaveGlobal()` (global + device), suspend/quit Save'in kendi yazımı | Suspend/quit yazımı F4'te doğrulandı; kapanış çağrısı yalnız çökme sigortası. Zamanlayıcı oyun sırasında yazar ve "yalnız değiştiyse" yolu isterdi |
| Boot | Ekran/ses için boot adımı yok: Device `Restore`'u Save'in boot adımında uygular (Audio `Start`'ı boot adımlarından önce). Localization boot adımı `UseSave`'den sonra | F2 notundaki "Settings boot adımı" yalnız async iş (tablolar) için gerekiyordu |
| Confine | `IInputModeService.ConfineCursor`: Free/Hidden'da `Confined`, Locked'da değişmez; dispose sonrası uygulanmaz (inceleme). Değeri Settings verir; doğrudan yazım validator hatası (inceleme) | Cursor'ın tek yazıcısı mod servisi (F3) |
| Alt+Enter | PlayerSettings `allowFullscreenSwitch` kapalı | Modun ikinci yazıcısıydı (eski tuzak 11) |
| Editor | Pencere/çözünürlük ve `vSyncCount` Editor'da uygulanmaz; kalite Play sonunda başlangıç seviyesine, `targetFrameRate` yalnız yazıldıysa -1'e döner | `vSyncCount` aktif seviyenin alanı: Play'de yazmak `QualitySettings.asset`'i kirletirdi |
| Validator'lar | `DirectDisplayWriteValidator` (`Screen.SetResolution`, `fullScreen`/`fullScreenMode`, kalite seviyesi, `vSyncCount`, `targetFrameRate`, `ConfineCursor`; `[DisplaySettingsExempt]`), `DirectLocaleChangeValidator` (`SelectedLocale`, `SetSelectedLocale`; `[LocaleChangeExempt]`), `LocalizationValidator` (ayar/locale/Addressables yok, PlayerPref seçicisi, son seçici Specific değil → hata; kırık ya da yarım `LocalizedString`/`LocalizedAsset` → hata; eksik çeviri → uyarı) | Tek yazıcı kuralları F3/F5 deseniyle; kırık referans kesin hata, eksik çeviri geliştirmenin normal hali |
| Kapsam dışı | Render scale, parlaklık, monitör ve refresh rate seçimi, yeniden boyutlanan pencere, `-screen-*` argümanları, CJK font değişimi, ses dili, RTL, "sistemi izle"ye dönüş, uzak Addressables içeriği | Tüketicisi yok ya da F8 |

## Doğrulananlar (F7, paket kaynağından, Editor'da ve Windows IL2CPP dev build'inde)
| Konu | Sonuç → kural |
|---|---|
| Unity 6.3 | Editor Play'de `QualitySettings.names` platforma süzülür (`[PC]`, index 0); Edit Mode'da hepsi. `GetActiveQualityLevelsForPlatform` var. `Screen.mainWindowDisplayInfo` 1920x1080 @240 |
| Testler | 380/380 (+53). Sonrası 14 error + 8 warning (yeni beklenen: 1 kayıtlı dil yüklenemedi; uyarılar 4 geçersiz ekran ayarı, 1 bilinmeyen ses kanalı, 1 projede olmayan dil). ValidateAll 0; `Builder.Check(Release)` yalnız placeholder hatası + commit'lenmemiş değişiklik uyarısı |
| Play (Editor) | Boot: global → ekran/ses `Restore`'da uygulandı → device → `Localization: tr (system)` → ready. Ses 0.5, vsync kapalı + 30 FPS, confine, dil en: mesajlar (`AudioVolumeChanged`, 3 `DisplaySettingsChanged`, tek `LocaleChanged tr->en`), `targetFrameRate` 30, `Cursor` Confined, metin "Play". Quit'te `device.json`/`global.json` yazıldı (null alanlarla), sonraki Play'de hepsi geri geldi (`en (saved)`). `QualitySettings.asset` değişmedi. İnceleme sonrası üst üste `Set(en)`+`Set(tr)`: ikisi tamamlandı, mesaj yok (son hedef başlangıç diliydi); pencere listesi 1600x900'le başladı |
| Dev build (IL2CPP, 11:36) | Rapor: içerik 23 sn, 10 konum, `aa` çıktıda. Dosyasız açılış: Unity'nin hatırladığı 960x540 pencereden kenarlıksız 1920x1080'e geçti (stil popup, başlıksız), `tr (system)`, hiç dosya yazılmadı. Elle yazılan Windowed 1280x720 + `en`: istemci 1280x720 başlıklı pencere, `en (saved)`, müzik 0.5; quit'te eksik alanlar normalize edilip yazıldı, sonraki açılışta yazılmadı. Exception/AOT yok, struct broker'lar hatasız. Build inceleme düzeltmelerinden önce alındı; düzeltmeler Editor'da doğrulandı |
| Doğrulanamayan | Exclusive fullscreen (monitör modu değişir, denenmedi), mobil kare hızı, çok seviyeli kalite, gerçek UI ile değişiklik akışı |

## F7 incelemesi (bağımsız, git diff üzerinden, 2026-11-01)
| Bulgu | Düzeltme |
|---|---|
| Exclusive'de `mainWindowDisplayInfo` oyunun çözünürlüğünü verebilir, liste daralır | Exclusive dışında okunan boyut saklanır; exclusive'de açılışta `Display.main`. Canlı denenmedi ("Açık") |
| Pencere varsayılanı 1080p'de 1680x1050: görev çubuğu + başlıkla sığmıyor | Pencere boyutları `workArea`'ya başlık payıyla sığmalı; test |
| Kalite varsayılanı canlı seviye; player seviyeyi hatırlıyorsa reset yanlış | Template'te gözlenemedi; "Açık"a yazıldı |
| Nit'ler: `ConfineCursor` dispose sonrası uygulanıyordu ve doğrudan yazılabiliyordu; kurulum testi Editor `targetFrameRate`'ini yazıyordu; doküman (15 altı, `-language`, Addressables dolaylı bağımlılık); içerik testi yanlış dosyada | Dispose koruması; validator `ConfineCursor` yazımını da yakalar; `targetFrameRate` yalnız yazıldıysa geri; dokümanlar; `AddressablesContentTests.cs` |

## Template doğrulaması (2026-11-01)
| Bulgu | Düzeltme |
|---|---|
| `SettingsOptions.FrameRateLimit`/`MobileFrameRateLimit` doğrulanmıyordu: geçersiz varsayılan (ör. 10) olduğu gibi uygulanıyor, her açılışta uyarı veriyordu | `UseSettings` setter kuralıyla doğrular (0 ya da ≥15, değilse `ArgumentException`); test |
| `DirectDisplayWriteValidator` `ConfineCursor` yazımını da yakalıyor; Settings.md, `IDisplaySettings`, `DisplaySettingsExemptAttribute` ve validator mesajı söylemiyordu | Dördü güncellendi |
| Testsiz yazımlar: `Screen.fullScreen`, `QualitySettings.IncreaseLevel`/`DecreaseLevel`/`currentLevel` | Testler (`currentLevel` obsolete, pragma) |
| Mobil yol `Application.isMobilePlatform`'a bağlı: Editor (hedef Android olsa da) ve Device Simulator (`UnityEngine.Device.*`'ı taklit eder) masaüstü yolunu çalıştırır | Settings.md; davranış ilk mobil oyunda değerlendirilir ("Açık") |
| Settings.md oynanış ayarı örneğinde mesaj broker'ı ve savable kaydı yoktu | Doküman |
| Kabul (değişiklik yok) | `VolumeSettings` varsayılandaki (1) kanalı da yazar (test bunu istiyor; "varsayılana eşit seçim null" kuralı ekran ayarları için, CLAUDE.md daraltıldı); mobilde `Current.WindowMode` Borderless okunur, `vSyncCount` 0 yazılır (etkisiz) |
