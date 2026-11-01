# Localization (F7)

Bu modülün kararları, doğrulananları ve inceleme bulguları; çapraz kararlar ve "Açık" listesi `README.md`'de.
Karar değişirse satır silinmez; eskisinin üstü çizilir, yenisi tarihle eklenir.

> Ekran ve ses ayarları: `Settings.md`. Addressables içerik build'i ve build kontrolleri: `Build.md`.

## F7 kararları (2026-11-01)
Oyuncu ayarları ve dil. Kapsam: yeni `AF.Settings` ve `AF.Localization` modülleri (+Editor, Tests), Audio (`AudioVolumeChanged`,
`UseAudio` imzası), Input (`ConfineCursor`), Build (Addressables içerik adımı), Game (`GameRootScope`, `Game.asmdef`,
`Assets/Game/Localization/`), `Assets/AddressableAssetsData/`, paket, PlayerSettings, `.gitignore`.
| Konu | Karar | Gerekçe |
|---|---|---|
| Nerede durur | Device `settings.display` (çözünürlük, pencere modu, vsync, kare sınırı, kalite, confine) ve `settings.audio` (kanal sesleri, MuteWhenUnfocused); Global `localization.locale` ve `input.bindings`. Oynanış ayarı oyunun kendi Global savable'ı (Settings.md'de örnek, template'te kod yok) | Ekran ve ses donanıma bağlı (eski oyunda Deck/PC ezilmesi bu değerlerdi); dil ve tuş oyuncuya ait |
| Modül sınırı | `AF.Settings` (→ Core, Save, Audio, Input) ekran + ses kalıcılığı; `AF.Localization` (→ Core, Save, Unity.Localization) dil + dilin kalıcılığı; Settings Localization'ı bilmez | Tek dilli oyun Localization'ı ve paketi çıkarabilir (Localization.md "Removing the module"); Audio kalıcılıksız kalır (F6) |
| Yazma zamanı | Setter diske yazmaz; zamanlayıcı yok. Ekran kapanırken oyun `SaveGlobal()` (global + device), suspend/quit Save'in kendi yazımı | Suspend/quit yazımı F4'te doğrulandı; kapanış çağrısı yalnız çökme sigortası. Zamanlayıcı oyun sırasında yazar ve "yalnız değiştiyse" yolu isterdi |
| Boot | Ekran/ses için boot adımı yok: Device `Restore`'u Save'in boot adımında uygular (Audio `Start`'ı boot adımlarından önce). Localization boot adımı `UseSave`'den sonra | F2 notundaki "Settings boot adımı" yalnız async iş (tablolar) için gerekiyordu |
| Dil seçimi | Seçiciler CommandLine → System → Specific(en); `PlayerPrefLocaleSelector` yok (validator). Kayıtlı kod (`null` = sistem) boot adımında uygulanır; projede yoksa uyarı, tabloları yüklenemezse hata + başlangıç dili, boot düşmez (inceleme); seçim kayıtta kalır | Eski oyunda dil PlayerPrefs'teydi (cihaza bağlı); senkron seçim her cihazda boot'u düşürmesin |
| Locale servisi | `ILocaleService {Current, Available, Set}`, `SelectedLocale`'in tek yazıcısı. Geçişler sırayla: süren varken yeni hedef kaydedilir, en yeni kazanır, hepsi onunla tamamlanır; geçiş sürerken ve seçili olmayan locale'in event'i yok sayılır (inceleme). Dışarıdan değişim (Scene Controls) izlenir, kaydedilmez. `LocaleChanged` tablolar yüklenince | Paket süren yüklemenin üstüne yeni seçimde eski init'i bırakıp yenisini başlatıyor; eski init başarısız olabiliyor ve event'i geç geliyor (paket kaynağı) |
| Tablolar boot'ta | String tabloları Preload; Preload Behavior "Selected Locale And Fallbacks" (inceleme). Init hatası / locale yok → boot adımı fırlatır | `WaitReady` sonrası senkron okuma; fallback metni de hazır |
| Örnek içerik | en (proje dili) + tr (fallback en), `UI` koleksiyonu, `menu.play`. Asset'ler `Assets/Game/Localization/`; Addressables ayarları varsayılan `Assets/AddressableAssetsData/` | İki dil geçişi test etmek için; Addressables klasörü URP asset'leri gibi proje ayarı, `.gitignore` ve araçlar bu yolu bekliyor |
| Preloaded Assets | Localization ayar asset'i kalıcı olarak listede | Paket her build'de ekleyip bellekte çıkarıyor, disk değişik kalıyordu (build'de görüldü); listede olunca dokunmuyor |
| Addressables build | `AF.Build.Editor` (`versionDefines` → `AF_ADDRESSABLES`): checks'ten sonra, eski çıktı silinmeden önce `BuildPlayerContent`; hata → Failed. Ayar "build with player" Do not build değilse build hatası; ayar yoksa hata. Build sonrası `StreamingAssets/aa/settings.json` yoksa Failed (paket çıktısında aranmaz) | İçerik Addressables'ın işi; raporda görünür, başarısız içerik eski build'i silmez. Addressables'ın kendi kancası `BuildGuard`'dan önce çalışıyor |
| Validator'lar | `DirectDisplayWriteValidator` (`Screen.SetResolution`, `fullScreen`/`fullScreenMode`, kalite seviyesi, `vSyncCount`, `targetFrameRate`, `ConfineCursor`; `[DisplaySettingsExempt]`), `DirectLocaleChangeValidator` (`SelectedLocale`, `SetSelectedLocale`; `[LocaleChangeExempt]`), `LocalizationValidator` (ayar/locale/Addressables yok, PlayerPref seçicisi, son seçici Specific değil → hata; kırık ya da yarım `LocalizedString`/`LocalizedAsset` → hata; eksik çeviri → uyarı) | Tek yazıcı kuralları F3/F5 deseniyle; kırık referans kesin hata, eksik çeviri geliştirmenin normal hali |
| Kapsam dışı | Render scale, parlaklık, monitör ve refresh rate seçimi, yeniden boyutlanan pencere, `-screen-*` argümanları, CJK font değişimi, ses dili, RTL, "sistemi izle"ye dönüş, uzak Addressables içeriği | Tüketicisi yok ya da F8 |

## Doğrulananlar (F7, paket kaynağından, Editor'da ve Windows IL2CPP dev build'inde)
| Konu | Sonuç → kural |
|---|---|
| Localization 1.5.13 | `SetSelectedLocale` süren init'i bırakıp yenisini başlatır, `SelectedLocaleChanged` yeni init bitince (gerekirse coroutine ile) gelir; açılıştaki ilk seçim event üretmez. Seçici locale bulamazsa init "başarılı" biter, seçili locale null → boot adımı ayrıca kontrol eder. Varsayılan seçici listesi CommandLine/System/Specific. `LocalizationBuildPlayer` ayar asset'ini build'de Preloaded Assets'e ekler, sonra yalnız bellekte çıkarır |
| Addressables 2.10.3 | `AddressablesPlayerBuildProcessor.PrepareForBuild` "Do not build"da da `Library/com.unity.addressables/aa/<platform>`'u (varsa) `StreamingAssets/aa`'ya ekler, `link.xml`'i config klasörüne kopyalar (InitializeOnLoad siler, gitignored). Varsayılan ayar `DoNotBuildWithPlayer`. İlk içerik build'i `ProfileDataSourceSettings.asset` ve `ProjectSettings/ScriptableBuildPipeline.json` yaratır |
| Testler | 380/380 (+53). Sonrası 14 error + 8 warning (yeni beklenen: 1 kayıtlı dil yüklenemedi; uyarılar 4 geçersiz ekran ayarı, 1 bilinmeyen ses kanalı, 1 projede olmayan dil). ValidateAll 0; `Builder.Check(Release)` yalnız placeholder hatası + commit'lenmemiş değişiklik uyarısı |
| Play (Editor) | Boot: global → ekran/ses `Restore`'da uygulandı → device → `Localization: tr (system)` → ready. Ses 0.5, vsync kapalı + 30 FPS, confine, dil en: mesajlar (`AudioVolumeChanged`, 3 `DisplaySettingsChanged`, tek `LocaleChanged tr->en`), `targetFrameRate` 30, `Cursor` Confined, metin "Play". Quit'te `device.json`/`global.json` yazıldı (null alanlarla), sonraki Play'de hepsi geri geldi (`en (saved)`). `QualitySettings.asset` değişmedi. İnceleme sonrası üst üste `Set(en)`+`Set(tr)`: ikisi tamamlandı, mesaj yok (son hedef başlangıç diliydi); pencere listesi 1600x900'le başladı |
| Dev build (IL2CPP, 11:36) | Rapor: içerik 23 sn, 10 konum, `aa` çıktıda. Dosyasız açılış: Unity'nin hatırladığı 960x540 pencereden kenarlıksız 1920x1080'e geçti (stil popup, başlıksız), `tr (system)`, hiç dosya yazılmadı. Elle yazılan Windowed 1280x720 + `en`: istemci 1280x720 başlıklı pencere, `en (saved)`, müzik 0.5; quit'te eksik alanlar normalize edilip yazıldı, sonraki açılışta yazılmadı. Exception/AOT yok, struct broker'lar hatasız. Build inceleme düzeltmelerinden önce alındı; düzeltmeler Editor'da doğrulandı |
| Doğrulanamayan | Exclusive fullscreen (monitör modu değişir, denenmedi), mobil kare hızı, çok seviyeli kalite, gerçek UI ile değişiklik akışı |

## F7 incelemesi (bağımsız, git diff üzerinden, 2026-11-01)
| Bulgu | Düzeltme |
|---|---|
| Üst üste `Set`: paket süren init'i bırakıp yenisini başlatıyor; eski init "could not preload" ile düşebiliyor, eski event yeni seçimden sonra gelip yanlış dili commit ediyordu | Geçişler sırayla (tek geçiş, en yeni hedef kazanır); event geçiş sürerken ve seçili olmayan locale için yok sayılır; `_ready` kayıtlı geçişten sonra. Testler: üst üste set, geç event, iptal; sahte backend paket gibi davranıyor (aynı dile event yok, hata, iptal) |
| Kayıtlı dilin tabloları yüklenemezse boot kalıcı düşüyordu (senkron seçim, her cihaz) | Hata logu + başlangıç diline dönüş, seçim kalır; test |
| Fallback tabloları önyüklenmiyordu, doküman "hemen döner" diyordu | Preload Behavior "Selected Locale And Fallbacks"; doküman |
| `Restore(null)` hazırken başlangıç diline dönmüyordu; `UseLocalization` installer testi yoktu | Başlangıç diline geçer; installer testi (UseSave şartı, boot adımı sırası, savable) |
| Nit'ler: `ConfineCursor` dispose sonrası uygulanıyordu ve doğrudan yazılabiliyordu; kurulum testi Editor `targetFrameRate`'ini yazıyordu; doküman (15 altı, `-language`, Addressables dolaylı bağımlılık); içerik testi yanlış dosyada | Dispose koruması; validator `ConfineCursor` yazımını da yakalar; `targetFrameRate` yalnız yazıldıysa geri; dokümanlar; `AddressablesContentTests.cs` |

## Template doğrulaması (2026-11-01)
| Bulgu | Düzeltme |
|---|---|
| Hata mesajlarında `AF > Build` / `AF > Validate All` sabit yazılıydı (prefix yalnız constants dosyasında) | `FrameworkConstants.Prefix` |
| `LocalizationValidator`'ın bileşen taraması (`CheckObjects`) ve `LocalizedAsset` yolu testsizdi | Test (sahte tablo araması, `LocalizeStringEvent` + `LocalizeTextureEvent`); sahnede bellekte de denendi |
