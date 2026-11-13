# Build: build menüsü ve kontroller (F4), Addressables içerik adımı (F7)

Bu modülün kararları, doğrulananları ve inceleme bulguları; çapraz kararlar ve "Açık" listesi `README.md`'de.
Karar değişirse satır silinmez; eskisinin üstü çizilir, yenisi tarihle eklenir.

## F4 kararları (2026-10-31)
| Konu | Karar | Gerekçe |
|---|---|---|
| Modüller | Build yalnız Editor: `Assets/Framework/Build/{Editor,Tests/Editor}`, `AF.Build.Editor` (→ AF.Core, AF.Core.Editor, `Mono.Cecil.dll`), `AF.Build.Tests`. Platform: `Assets/Framework/Platform/{Runtime,Tests/Editor}`, `AF.Platform` (→ Core, VContainer, UniTask, MessagePipe), `AF.Platform.Tests`; Mock aynı asmdef'te. `com.unity.nuget.mono-cecil` 1.11.6 doğrudan bağımlılık | Build'in dış bağımlılığı (Cecil) var, runtime'a girmiyor. Mock varsayılan backend, ayrı asmdef gerekçesi yok. Steam F8'de ayrı asmdef |
| Build türleri | `Development` = development player + `SHOW_LOGS` (+ Pipeline runtime, toggle'dan); `Release` = define yok. Aktif hedef build edilir, menü platform değiştirmez | Dev build yalnız yerel test (loopback HTTP + kod çalıştırma); test edenler Release alır |
| Çıktı | `Builds/<Platform>/<Kind>/` (gönderilecek klasör), `<Kind>-symbols/` (IL2CPP BackUp ve Burst DoNotShip klasörleri taşınır), `<Kind>-report.txt`. Sabit yol; rapor her build başında, klasör build'den hemen önce silinir; yalnız `Builds/` içi silinir; reddedilen build öncekini bırakır | Bilinen tuzaklar: eski "OK" raporu doğrulanmamış build'in yanında kalır; debug klasörü depoya girer |
| Build öncesi kontrol | Ortam görüntüsü (`BuildEnvironment`) üzerinde saf kurallar + ValidateAll + serialize layout. Hata: Play/derleme, ValidateAll hatası, kaydedilmemiş sahne/prefab, PlayerSettings/compiler arg/`csc.rsp` define'ı, özel Build Profile aktif, layout farkı, desteklenmeyen/kurulu olmayan hedef, enabled sahne yok, Release'te placeholder ad. Uyarı: kirli asset, Release'te Mono, stripping > Minimal, Release'te commit'lenmemiş değişiklik, Development'ta placeholder | Testler proje ayarına dokunmadan kötü kurulum verir. Kirli asset uyarısı: Play'de değişen materyal keyword'ü build'e girebilir |
| Serialize layout kontrolü | `PlayerBuildInterface.CompilePlayerScripts` build'in hedefi/seçeneği/`extraScriptingDefines`'ıyla `Temp/`'e; editor tarafı `Library/ScriptAssemblies`; ikisi Cecil ile okunur, `Assets/` assembly'lerinde UnityEngine.Object türevi ya da `[Serializable]` tiplerin serialize alanları (ad + tip) karşılaştırılır. ~30 sn | Unity bunu build sonunda alan adı vermeden yakalıyor. `#if UNITY_EDITOR` dahil |
| Build sonrası | Symbols taşınır; çıktıda Pipeline runtime aranır (IL2CPP `global-metadata.dat`'ta `RuntimePipelineServer`, Mono `Unity.Pipeline.dll`), Release'te bulunursa Failed; build öncesi/sonrası `git status` farkı rapora | Pipeline-release kararı artefakt düzeyinde korunur; build'in projeye bıraktığı çöp görünür |
| Tek kapı | `BuildGuard` (`IPreprocessBuildWithContext`, order -10000) `Builder`'ın SessionState bayrağı yoksa build'i durdurur; `IncludeTestAssemblies` geçer | Build Profiles penceresi ve Pipeline `build run` kontrolleri ve define'ları atlar |
| Unity 6 Build Profiles | Kullanılmıyor; classic platform profili. Özel profil aktifse build hatası. Flavor fazında yeniden bakılır | Profilin kendi define'ı, sahne listesi ve PlayerSettings override'ı menüyü atlar |
| Proje ayarları | Standalone IL2CPP; stripping Minimal (Standalone, Android, iOS); artık define'lar (`SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY`) silindi; identifier override kapalı, identifier'lar `com.DefaultCompany.TemplateProject` | IL2CPP doğrulaması Minimal'de; identifier Company/Product'tan türesin |
| IL2CPP kod üretimi / link.xml | VContainer source generator ve `link.xml` eklenmedi. Stripping yükselten oyun `[Preserve]`/`link.xml` ekler; build uyarır | Minimal'de kullanıcı ve paket assembly'leri strip edilmez; generator yalnız performans |

## Doğrulananlar (F4, Editor'da ve Windows IL2CPP build'lerinde)
| Konu | Sonuç → kural |
|---|---|
| Unity 6.3 API | `ScriptCompilationSettings.extraScriptingDefines` var. `IPreprocessBuildWithContext` (`BuildCallbackContext.Report/IsPlayerBuild`). `BuildProfile.GetActiveBuildProfile()` platform profilinde null. `PlayerSettings` değişikliği `SaveAssetIfDirty` ile yazılmaz. `SetApplicationIdentifier` override bayrağını açar. `AssemblyBuilder` obsolete (CS0618) ama çalışıyor; testte pragma ile |
| Pipeline 0.9.1 | Toggle'lar PlayerSettings define'ı (Standalone + Server). Build işlemcisi yalnız PlayerSettings'e bakar. Build sonunda boş `Assets/Settings/Pipeline/Resources` klasörü kalır → klasör gitignored. IL2CPP player'da `script eval` derleyemez; player komutları console, runtime status/quit/simulate_key/simulate_pointer/set_time_scale |
| Dev build (IL2CPP, ~12 dk, 259 MB + 3.2 GB symbols) | Player.log: `Root scope built` bir kez, global/device yüklendi, `Game state: BootState -> MenuState`, `Input mode: none -> menu` (struct `GameStateChanged` aboneye ulaştı), `App boot ready`, `Platform: Mock`; exception/AOT/MissingMethod yok, audit hatası yok (`RegistrationAudit` reflection IL2CPP'de çalışıyor) |
| Save + binding round-trip (IL2CPP) | Elle yazılan `global.json` (Gameplay/Pause override'ı, bayat `action` etiketi) → Newtonsoft okudu, `InputBindings` (internal, `[Inject]`siz ctor, VContainer reflection) override'ı id ile uyguladı (yetim uyarısı yok) → quit'te `AppSuspending` → capture → Newtonsoft → kuyruk/thread pool → `File.Replace` ile düzeltilmiş etiketle yazdı, eski dosya `.bak` oldu |
| Rendering Debugger | Dev build'de Ctrl+Backspace (odaklı pencerede simüle) ile açılıyor, kendi action'ları mod servisinin dışında. Release'de `DEVELOPMENT_BUILD`/`UNITY_EDITOR` guard'ı arkasında, yok |
| Release build (IL2CPP, ~5 dk, 132 MB) | Pipeline runtime çıktıda yok (artefakt kontrolü). Bozuk `device.json` → Player.log'da Save `Log.Error` var; `Root scope built`, `App boot ready`, `Game state:`, `Platform:` yok (Info derlenmedi) |
| Doğrulanamayan (IL2CPP) | `PauseChanged`/`PlatformOverlayChanged` yayın yolu player'da tetiklenemedi (UI yok, eval yok); abonelikleri boot'ta kuruldu, hata yok. Yayın EditMode testlerinde. Slot new/save/load/delete player'da çağrılamadı; aynı kuyruk ve format global'de doğrulandı |

## F4 incelemesi (bağımsız, git diff üzerinden, 2026-10-31)
| Bulgu | Düzeltme |
|---|---|
| Android'de çıktı klasör içinde paket; "tek dosya" dalı hiç çalışmıyor, Pipeline taraması zip içini görmüyor, sonuç "not included" | Pipeline kontrolü build öncesine de alındı: `SerializedLayoutCheck` derlediği player assembly listesini döndürür, `BuildChecks.EvaluatePlayerAssemblies` Release'te `Unity.Pipeline.dll` varsa reddeder (her platformda). Paket çıktısı `BuildPaths.IsPackage(target)` (Android) ile ayrılır, raporda "aranmadı" notu. İlk düzeltme `File.Exists(location)` idi, Windows'ta `.exe`'yi paket sandı (symbols taşınmadı); doğrulama build'inde yakalandı |
| Build ortasındaki exception (çalışan player'ın kilitlediği dosya, symbols taşıma hatası) raporsuz ve yarım klasörle biter | Silme → build → sonrası tek try/catch; Failed + not + `Log.Exception`, rapor her durumda yazılır |
| Başarısız Release gönderilecek klasörde kalıyor | Başarısız Release'in klasörü silinir (Development incelemek için kalır) |
| Dosya adı temizliği host'a bağlı (`GetInvalidFileNameChars` Mac/Linux'ta farklı) | Sabit Windows karakter kümesi + kontrol karakterleri |
| Layout kontrolünün "Unity serialize eder mi" süzgeci testsiz; assembly bulunamazsa her şeyi sessizce atlıyor | `IsUnitySerialized(Assembly)` test edildi (Serializable, MonoBehaviour, düz sınıf, enum, olmayan tip); editor kopyası yüklü değilse hata |
| "Build changed project files" önceden değişmiş dosyanın yeniden yazılmasını ve untracked klasör içini kaçırıyor | `--untracked-files=all` + build öncesi/sonrası içerik hash'i |
| `Git`: yönlendirilip okunmayan stderr kilitleyebilir; status başarısızken rapor temiz görünür | stderr yönlendirilmez, stdout async okunur, zaman aşımında süreç öldürülür; status okunamazsa commit "(working tree state unknown)", Release'te uyarı |
| `DefinesSymbol` yalnız satır başındaki define'ı görüyor (`-warnaserror -define:X` geçiyor) | Satır option'lara bölünür |
| Standalone Server subtarget'ı sessizce Player build ediliyor | Build.md'de yazılı (menü yalnız player build eder) |
| Platform.md mağaza backend'inin build menüsü define'ıyla seçildiğini söylüyor (öyle bir define yok); Build.md Pipeline satırı toggle'a bağlı olduğunu söylemiyor | İkisi düzeltildi |
| Commit hijyeni: URP asset'leri build yan etkisi; ProjectSettings'te `m_BuildTargetBatching` Standalone girdisi Unity'nin yazdığı varsayılan değerler | URP kararı kullanıcıda; batching girdisi zararsız, kalır. Build.md'deki `unity command` örnekleri Unity'nin genel CLI'ı, AI akışına özel değil, kalır |

## F7 kararları (2026-11-01)
Oyuncu ayarları ve dil. Kapsam: yeni `AF.Settings` ve `AF.Localization` modülleri (+Editor, Tests), Audio (`AudioVolumeChanged`,
`UseAudio` imzası), Input (`ConfineCursor`), Build (Addressables içerik adımı), Game (`GameRootScope`, `Game.asmdef`,
`Assets/Game/Localization/`), `Assets/AddressableAssetsData/`, paket, PlayerSettings, `.gitignore`.
| Konu | Karar | Gerekçe |
|---|---|---|
| Preloaded Assets | Localization ayar asset'i kalıcı olarak listede | Paket her build'de ekleyip bellekte çıkarıyor, disk değişik kalıyordu (build'de görüldü); listede olunca dokunmuyor |
| Addressables build | `AF.Build.Editor` (`versionDefines` → `AF_ADDRESSABLES`): checks'ten sonra, eski çıktı silinmeden önce `BuildPlayerContent`; hata → Failed. Ayar "build with player" Do not build değilse build hatası; ayar yoksa hata. Build sonrası `StreamingAssets/aa/settings.json` yoksa Failed (paket çıktısında aranmaz) | İçerik Addressables'ın işi; raporda görünür, başarısız içerik eski build'i silmez. Addressables'ın kendi kancası `BuildGuard`'dan önce çalışıyor |
| Content state | `addressables_content_state.bin` varsayılan yerinde (gitignored); platform klasörlerinin `.meta`'ları da `.gitignore`'da (Windows, OSX, Linux, Android, iOS, WebGL) (inceleme) | Temiz klonda yetim meta olmasın; uzak içerik yok |

## Doğrulananlar (F7, paket kaynağından, Editor'da ve Windows IL2CPP dev build'inde)
| Konu | Sonuç → kural |
|---|---|
| Addressables 2.10.3 | `AddressablesPlayerBuildProcessor.PrepareForBuild` "Do not build"da da `Library/com.unity.addressables/aa/<platform>`'u (varsa) `StreamingAssets/aa`'ya ekler, `link.xml`'i config klasörüne kopyalar (InitializeOnLoad siler, gitignored). Varsayılan ayar `DoNotBuildWithPlayer`. İlk içerik build'i `ProfileDataSourceSettings.asset` ve `ProjectSettings/ScriptableBuildPipeline.json` yaratır |
| Dev build (IL2CPP, 11:36) | Rapor: içerik 23 sn, 10 konum, `aa` çıktıda. Dosyasız açılış: Unity'nin hatırladığı 960x540 pencereden kenarlıksız 1920x1080'e geçti (stil popup, başlıksız), `tr (system)`, hiç dosya yazılmadı. Elle yazılan Windowed 1280x720 + `en`: istemci 1280x720 başlıklı pencere, `en (saved)`, müzik 0.5; quit'te eksik alanlar normalize edilip yazıldı, sonraki açılışta yazılmadı. Exception/AOT yok, struct broker'lar hatasız. Build inceleme düzeltmelerinden önce alındı; düzeltmeler Editor'da doğrulandı |

## F7 incelemesi (bağımsız, git diff üzerinden, 2026-11-01)
| Bulgu | Düzeltme |
|---|---|
| `Windows.meta` yetim kalacaktı | `.gitignore`'a platform klasör metaları |

## Template doğrulaması (2026-11-01)
| Bulgu | Düzeltme |
|---|---|
| Rapor silme, değişen dosya hash'i ve `NoteChangedFiles` try/catch dışındaydı: oradaki IO istisnası raporsuz bitirirdi (F4 "rapor her durumda") | Gövde `Run`'a alındı; `Build` try/catch/finally ile `Finish`'i her yolda çağırır. Değişen dosya hash'i IO hatasında fırlatmaz (inceleme: başarılı build sonrası "Failed" + silinmemiş Release klasörü). Windows Release build'iyle doğrulandı (hash düzeltmesinden önce; aşağıda) |
| Build.md: Pipeline dev define'ı yalnız Standalone/Server → Android/iOS Development build'inde CLI bağlantısı yok; Android paketinin yanında symbols taşıma yok; iOS çıktısı Xcode projesi; layout kontrolü `Temp/BuildLayoutCheck`'i siler | Doküman. Android'de IL2CPP BackUp klasörünün oluşup oluşmadığı ilk mobil build'de görülür |

### Doğrulananlar (template doğrulaması, Windows IL2CPP Release, 2026-11-01)
| Konu | Sonuç |
|---|---|
| Release build (`AllowPlaceholderIdentity`, 5:19, 138.7 MB) | Succeeded; Addressables içeriği 8 sn, çıktıda; BackUp ve Burst klasörleri `Release-symbols/`'a; Pipeline runtime yok. Rapor build sırasında elle düzenlenen dosyaları da "build changed project files" altında listeledi (önce/sonra karşılaştırması, beklenen). URP asset'leri bu build'de yeniden yazılmadı; `Assets/Resources/` build ortasında oluşup sonunda silindi |
