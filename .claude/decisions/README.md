# Karar kaydı

Template'in mimari kararları ve gerekçeleri. Karar değişirse satır silinmez; eskisinin üstü çizilir, yenisi tarihle eklenir.

**Amaç:** sonraki oyunlara (PC ve mobil) hızlı başlangıç. Tek ölçüt: geliştirmeyi hızlandıran hazır sistemler.
UI ve oyuna özel oynanış sistemleri template dışında kalır.

## Dosyalar

| Dosya | Konu | ⚠ Kritik tuzaklar (ayrıntı dosyada) |
|---|---|---|
| `README.md` (bu dosya) | Motor ve paketler, dağıtım, genel mimari, isimlendirme, eski projeden dersler, paket sürümleri, fazlar, **Açık** listesi, repo görünürlüğü | ⚠ Framework prefix'i (`AF`) yalnız constants dosyasında; kalıcı veriye girmez · ⚠ `[SerializeReference]` tipi taşınırken `[MovedFrom]` |
| `Core.md` | DI scope'ları, mesajlar, GameManager, boot, pause ve oyun saati, validation (F1); sahne yükleme ve intro (F5); havuz ve tween yaşam döngüsü (F6) | ⚠ VContainer sahne objesine kendiliğinden inject etmez: scope'ta serialize alan + `RegisterComponent` ya da Auto Inject · ⚠ `ITickable`/`IStartable` yalnız `RegisterEntryPoint` ile çalışır; Scoped servis entry point olmaz · ⚠ Buffered pub/sub yok (struct'ta `default(T)` gelir); publish en erken `Start` · ⚠ Tween yalnız `ToUniTask(ct)` ile await (PrimeTween'inki `Stop()` sonrası normal döner); havuzda await sonrası `life.IsCurrent` · ⚠ Sahne yalnız `ISceneLoader`; state'ler sahne yüklemez · ⚠ Run In Background kapalıyken Editor'da Play'in ve pencereli masaüstü player'ın odak kaybı da `AppSuspending(Background)` (Save yazar) |
| `Save.md` | Save sistemi (F2) | ⚠ `Restore` fırlatmaz ve tüm state'i değiştirir; sahne savable'ının kaydı `Construct`'ta değil `Initialize`/`Start`'ta okunur · ⚠ Key sabit; enum ya da alan rename'i migration ister, yayınlanmış adım değişmez · ⚠ Hiçbir dosya otomatik silinmez; okunamayan global/device o oturum salt okunur · ⚠ Menüye girişte slot kapanır; kaydetmek isteyen load'dan önce `SaveSlot` |
| `Input.md` | Input mod servisi, rebind, cursor (F3) | ⚠ Map/action `Enable`/`Disable` ve `Cursor`'a yalnız mod servisi dokunur; kapalı action guard sayılmaz · ⚠ "Generate C# Class" kapalı: ayrı kopya üretir · ⚠ Binding silinip yeniden yaratılmaz (id değişir, kayıtlı override düşer) · ⚠ Yükleme boyunca mod koşulsuz None · ⚠ Touch şeması yalnız native touchscreen'le seçilir; Device Simulator / Unity Remote'ta hiç |
| `Audio.md` | Ses, müzik, kanallar, listener (F6) | ⚠ Tek AudioListener root'ta; sahnede/prefab'da listener ve çıkışsız AudioSource validator hatası · ⚠ `AudioListener.pause`/`volume`'a yalnız Audio yazar · ⚠ Sahnenin sesleri scope'la durur; sahneyi aşacak ses `IGlobalSounds` · ⚠ Koddan yazılan exposed parametre snapshot'lardan çıkar |
| `Settings.md` | Ekran ayarları, ses kalıcılığı (F7) | ⚠ Kalite seviyesi `vSyncCount`'u sıfırlar: önce kalite; mobilde `targetFrameRate -1` = 30 FPS · ⚠ Ekran, kalite, kare hızı ve confine'a yalnız `IDisplaySettings` yazar; Alt+Enter kapalı · ⚠ Setter diske yazmaz; `null` = varsayılan, geçersiz kayıt o oturum varsayılana düşer · ⚠ Editor'da pencere ve `vSyncCount` uygulanmaz (`QualitySettings.asset` kirlenirdi) · ⚠ Mobil yol `isMobilePlatform`: Editor ve Device Simulator masaüstü yolunu çalıştırır |
| `Localization.md` | Dil seçimi ve kalıcılığı, Unity Localization (F7) | ⚠ `SelectedLocale`'e yalnız `ILocaleService` yazar; `PlayerPrefLocaleSelector` yok · ⚠ Süren yüklemenin üstüne seçim paketin init'ini bozar: geçişler sırayla · ⚠ Seçici locale bulamazsa init "başarılı" biter, seçili locale null · ⚠ Addressables içeriği olmayan build'de dil yok, oyun Boot'ta kalır |
| `Platform.md` | Platform sözleşmeleri ve Mock (F4) | ⚠ Her backend her sözleşmeyi kaydeder (eksikse `UsePlatform` fırlatır); mağaza SDK tipleri yalnız backend asmdef'inde · ⚠ Başarım doğruluğu oyunun save'inde; yüklemede kazanılmışlar yeniden `Unlock` |
| `Build.md` | Build menüsü, build öncesi/sonrası kontroller, IL2CPP (F4); Addressables içerik adımı (F7) | ⚠ Build yalnız `Builder`; diğer yollar `BuildGuard`'da reddedilir · ⚠ Define yalnız `BuildKinds.Defines` (Pipeline dev toggle'ı hariç) · ⚠ Platform define'ının pozitif dalında serialize alan yok (`#if UNITY_EDITOR` dahil) · ⚠ Addressables "build with player" kapalı kalır; URP build'de pipeline asset'lerini yeniden yazar |
| `CodeStyle.md` | F1–F3 kodunun yeni stile toplu dönüşümü | ⚠ Serialize alan rename'inde `[FormerlySerializedAs]` + YAML'ın yeniden yazımı, yoksa değer sessizce default'a düşer |
| `Game.md` | HoldIt'in oyun kararları: kaynaklar, UI, görsel ve asset; oyunun "Açık" listesi ve template'e giden sorunlar | ⚠ Framework burada yamanmaz: template'te düzeltilir, cherry-pick · ⚠ Repo public: yalnız CC0 ya da kendi asset |

Kullanım:
- Faz ya da iş başlarken bu dosyayı oku, sonra yalnız ilgili modül dosyalarını aç; klasörün tamamını okuma.
- Tuzak sütunu, dosyayı açmadan bilinmesi gerekenler içindir: bilinmezse yanlış iş yaptıracak kural başına tek satır. Faz
  kapanışında yeni kritik tuzak buraya da eklenir; ayrıntı ve gerekçe modül dosyasında kalır.
- Yeni karar ilgili modül dosyasına, birden çok modülü kesen karar buraya yazılır. Faz kapanışında bölüm adları aynı
  kalır: "F<n> kararları (tarih)", "Doğrulananlar (F<n>, ...)", "F<n> incelemesi (...)".
- "Açık" listesi yalnız burada; kapanan madde üstü çizilip kararın yazıldığı dosyaya gönderme yapılır.
- Repo public olmadan önce: eski ürüne yapılan anlatımlar (ad geçmiyor, "eski oyun / eski proje" diye) genel derse
  çevrilir; kişisel yol kalmaz.

## Motor ve paketler
| Karar | Gerekçe |
|---|---|
| Unity 6.3 LTS, URP; PC ve Mobile RP asset'leri kalite seviyeleriyle birlikte kalır | Hem PC hem mobil oyun bu template'ten başlar |
| DI: VContainer · Mesaj: MessagePipe (+VContainer) · Async: UniTask · Tween: PrimeTween · JSON: Newtonsoft | Zenject'in bakımı yavaş, reflection ve GC maliyeti yüksek; DOTween yerine allocation'sız PrimeTween |
| UniTask kalır, `Awaitable` kullanılmaz | 6.3 `Awaitable`'da WhenAll/WhenAny yok; havuzlandığı için iki kez await tanımsız; sızıntı izleyici yok; VContainer ve MessagePipe async entegrasyonu UniTask üzerine |
| Pool: `UnityEngine.Pool.ObjectPool<T>` sarmalayıcısı | Built-in, ek bağımlılık yok |
| Editor köprüsü: Unity CLI (`unity command …`) + `com.unity.pipeline` | AI Assistant'ın MCP sunucusu 2.21'de deprecated, Unity CLI'ı öneriyor. AI Assistant paketi kaldırıldı |
| Pipeline runtime'ı release build'e girmez | Asmdef define kısıtı; `enableInReleaseBuilds` kapalı tutulur |

## Dağıtım ve yapı
| Karar | Gerekçe |
|---|---|
| Dağıtım: GitHub "Use this template" | Kopyala ve ilerle; oyunlar arasında güncelleme akışı gerekmiyor |
| ~~Framework kodu gömülü paket: `Packages/com.af.framework`~~ | ~~Framework/oyun çizgisi görünür; bir düzeltmeyi template'e geri taşımak tek klasör karşılaştırması; ileride git URL'e geçmek tek satır~~ |
| ~~Tek paket, modül başına asmdef (Core, Save, Input, Audio, Scene, Localization, Platform + Editor + Tests). Asmdef'lerde "Use GUIDs" açık~~ | ~~Bağımlılık yönü derleyiciyle korunur; isteğe bağlı modül projeden çıkarılabilir; oyun kodu değişince framework yeniden derlenmez. Yeni asmdef yalnız dış bağımlılık ya da çıkarılabilirlik varsa açılır~~ |
| Oyun katmanı kendi asmdef'inde, kendi namespace'iyle (template'te placeholder) | |
| (2026-10-29) Framework kodu `Assets/Framework/` altında, paket değil | Kullanıcı kodu editörde doğrudan görmek istedi. Framework/oyun sınırı klasör + asmdef ile korunuyor; package.json, `package resolve`, `testables` yükü yok. Düzeltmeyi template'e taşımak yine tek klasör karşılaştırması. Klasör adında AF yok: prefix değişse de yol sabit |
| (2026-10-29) Modül önce düzen: `Assets/Framework/<Modül>/{Runtime,Editor,Tests}`, modül başına asmdef (`AF.Core`, `AF.Core.Editor`, `AF.Core.Tests`, `AF.Core.Tests.Fixtures`). "Use GUIDs" açık | Bağımlılık yönü derleyiciyle korunur; modül çıkarmak tek klasör. Yeni asmdef yalnız dış bağımlılık ya da çıkarılabilirlik varsa açılır |
| (2026-10-29) Oyun katmanı `Assets/Game/`, asmdef `Game`, namespace placeholder `Game`. New Game Setup yalnız namespace'i değiştirir. `Game.Editor` ilk editor script'iyle açılır. Root prefab, VContainerSettings ve sahneler burada | Asmdef adı `[SerializeReference]` verisine yazılır; sabit ad veri kırmaz. Araçlar oyun katmanını sabit adla bulur |
| New Game Setup penceresi: hedef platform, Company/Product adı, namespace | persistentDataPath bu adlardan türer; ilk build'den önce bir kez ayarlanmalı |
| Kod platform-nötr; IL2CPP doğrulaması Windows IL2CPP build'iyle | Mobil sorunların çoğu IL2CPP/AOT kaynaklı; Windows IL2CPP modülü kurulu, Android modülü şart değil |

## Mimari
| Karar | Gerekçe |
|---|---|
| Bootstrap sahnesi yok; root LifetimeScope VContainerSettings'ten kurulur | Her sahneden Play çalışmalı; VContainer'ın ProjectContext karşılığı bu |
| Async başlangıç (global save, ayarlar, platform) root'taki AppBoot'ta; sahneler onun "hazır" sinyalini bekler | Root scope senkron kurulur, async işler ayrı bir kapı ister |
| Gameplay sahnesinden doğrudan Play'de slot yoksa bellekte dev oturumu (diske yazmaz) | Her sahneden Play'in gameplay tarafı |
| ~~Hazır sistemler: GameManager (Boot/Menu/Playing/Paused, state başına sınıf), SceneManager (loading ekranı kancası, opsiyonel additive), Save, Audio (havuzlu one-shot + fade handle, müzik), Input mod servisi, Pool, oyun saati, Settings (model + uygula), Localization (Unity Localization)~~ | ~~Her oyunda yeniden yazılan çekirdek~~ |
| (2026-10-29) Hazır sistemler: GameManager (Boot/Menu/Playing, state başına sınıf), pause servisi, SceneManager (loading ekranı kancası, opsiyonel additive), Save, Audio (havuzlu one-shot + fade handle, müzik), Input mod servisi, Pool, oyun saati, Settings (model + uygula), Localization (Unity Localization) | Her oyunda yeniden yazılan çekirdek. Paused state'ten ayrı servise çıktı, bkz. "F1 kararları" |
| Platform: küçük ayrık interface'ler + Mock + Steam modülü (isteğe bağlı asmdef) | Tek `IPlatformService` dev servis olur; Steam entegrasyonu her PC oyununda aynı. Epic ve mobil mağazalar aynı desenle sonra |
| ~~Build: tek build menüsü; define'lar yalnız `extraScriptingDefines`; build öncesi kontrol. Flavor ekseni sonra~~ | ~~PlayerSettings'te unutulan define build'e sızar~~ |
| (2026-10-31) Build: tek build menüsü (`AF/Build`, `Builder`; diğer yollar `BuildGuard`'da reddedilir); define'lar yalnız `extraScriptingDefines`. Tek istisna Pipeline'ın dev toggle'ı `ENABLE_RUNTIME_PIPELINE_DEV` (PlayerSettings'te; assembly kısıtı dev-only sembol istediği için release'e giremez). Build öncesi kontrol ValidateAll'ı çağırır. Flavor ekseni sonra | PlayerSettings'te unutulan define build'e sızar. Pipeline'ın build işlemcisi yalnız PlayerSettings'e bakıyor; sembolü extraScriptingDefines'tan vermek desteklenmeyen yol (runtime ayarı bake edilmez) |
| Template'te UI sistemi yok | Her oyunun UI'ı farklı |
| Hata sessiz kalmaz: validator'lar, OnValidate, tek "AF/Validate All"; build öncesi kontrol de onu çağırır | En pahalı bug'lar "hata yok ama çalışmıyor" tipindeydi |
| Oyuncu modları: mod başına state sınıfı | Dev switch/enum 45 modlu tek manager'a dönüşmüştü |
| Bind ve inject interface üzerinden; bağımlılıklar tek constructor'dan (MonoBehaviour'da tek Construct) | |
| ~~Log: tek wrapper. `Log.Exception`/`Log.Error` her zaman derlenir; geliştirme izi `[Conditional("SHOW_LOGS")]`~~ | ~~Yayındaki oyunda Player.log'daki exception en değerli veri~~ |
| (2026-10-29) Log: tek wrapper `AF.Core.Log`. `Error`/`Exception` her zaman derlenir; `Info`/`Warning` `[Conditional("UNITY_EDITOR"), Conditional("SHOW_LOGS")]` (OR). Entry-point ve UniTask unobserved exception'ları buraya | Yayındaki oyunda Player.log'daki exception en değerli veri. Editor'da define gerekmez; dev build'e SHOW_LOGS F4 build menüsünden (`extraScriptingDefines`) |
| ~~Menü kökü, namespace prefix'i, save klasörü ve save anahtarları tek bir constants dosyasında~~ | ~~Rename güvenliği~~ |
| (2026-11-01) Menü kökü, namespace prefix'i, save klasörleri, dosya adları ve PlayerPrefs index anahtarı tek constants dosyasında (`FrameworkConstants`). Savable bölüm key'i savable'ın kendisinde, sabit küçük harf string (Save.md) | Rename güvenliği; uygulamada F2'den beri key'ler savable'da (template doğrulaması) |
| EditMode testleri pahalı patlayan yerlerde: save şeması, migration, saf mantık | Hedef %100 kapsama değil |
| Domain reload açık kalır | Kapatmak static state'i Play oturumları arasında taşır |

## İsimlendirme ve rename güvenliği
- Framework: `AF.*` namespace ve asmdef'leri, editor menü kökü `AF/`.
- Prefix yalnız constants dosyasında geçer; kalıcı veriye (save key, PlayerPrefs key, dosya adı) girmez.
- `[SerializeReference]` tip adını serialize eder; namespace/sınıf taşınırken `[MovedFrom]` şart.

## Eski projeden dersler → kurallar
| Ders | Kural |
|---|---|
| VContainer sahne objelerine otomatik inject etmez | Kayıtsız `[Inject]` bileşeni validator'da hata |
| `.As<IFoo>()` ile kaydedilen ITickable/IStartable hiç çalışmaz | Scope kurulurken entry-point kontrolü |
| Awake'te gönderilen event kayboluyordu; MessagePipe da tamponsuz | Abonelik constructor/`Initialize`'da, publish en erken `Start`'ta. State servisten okunur, event yalnız "değişti" bildirimi |
| Komut event'ini dinleyen UI eski değeri okuyordu | Komut ve "değişti" event'leri ayrı; UI yalnız "değişti"yi dinler |
| Dispose edilmeyen abonelik sessizce sızar | Abonelikler `DisposableBag` ile |
| ~~Havuzlanan objede async görev sonraki hayata sızıyordu~~ | ~~Spawn başına CancellationTokenSource / nesil kontrolü; havuza dönüşte tween'ler durur; pool prefab kökü kapalı; abonelik OnEnable/OnDisable~~ |
| (2026-10-31) Havuzlanan objede async görev sonraki hayata sızıyordu; aktif prefab kökü spawn'da OnEnable'ı atlattı | Yaşam başına token + nesil (`PoolLife`); havuzdaki objenin tween'i token'a bağlı; havuz instance'ı kapalı konteynerde yaratır, prefab kökü önemsiz (kural değişti, "F6 kararları"); abonelik OnEnable/OnDisable, reset `IPoolable` kancalarında |
| OnComplete state'i koşulsuz geri yüklüyordu | Tween/await sonrası state'e dokunmadan önce sahiplik kontrolü; pause'da çalışan UI tween'i unscaled |
| Yok edilmiş MonoBehaviour interface alanında `!= null` true döner | Ortak `IsAlive()` |
| OCE yutan yardımcı çağıranı iptal edilemez yapar | Yardımcılar OCE yutmaz |
| Cache'lenen `Time.time` pause'u bozar | Pause'u bilen oyun saati servisi |
| Input map tuzakları (project-wide actions, UI modülü, map restore, tetik carry-over) | Project-wide actions None; map'lerin tek sahibi input mod servisi. Touch şeması da kapsanmalı (Input fazında) |
| Paylaşılan materyale runtime yazmak `.mat` asset'ini bozar | MaterialPropertyBlock ya da instance materyal |
| Platform define'ının pozitif dalındaki serialize alan player'da layout hatası verir | Serialize alanlar define dışında |
| ~~Placeholder Company/Product adıyla build~~ | ~~Build öncesi kontrol reddeder~~ |
| (2026-10-31) Placeholder Company/Product adıyla build | Release reddedilir, Development uyarı verir (çakışma yalnız oyuncuya giden build'de zarar verir). Template'in kendi adları placeholder olduğu için kendi Release doğrulaması yalnız koddan `AllowPlaceholderIdentity` ile (menüde yok, rapora uyarı) |

## Doğrulananlar (F0, kaynaktan okundu)
| Konu | Sonuç → kural |
|---|---|
| PrimeTween 1.5.2 await | UniTask entegrasyonu hazır (`ToUniTask()`, örtük dönüşüm). `SetCancellationToken(ct)` ile iptal edilen tween'in await'i OCE fırlatır; **`Stop()` ile durdurulan tween'in await'i normal döner.** → ~~Await edilen tween'e her zaman `SetCancellationToken`; durdurma token'la yapılır. Ayrı köprü yazılmaz~~ (2026-10-31: önceden iptal edilmiş token ve geri dönüştürülmüş tween de normal döndüğü için köprü yazıldı: `ToUniTask(ct)` + validator → "F6 kararları") |
| MessagePipe.VContainer 1.8.2 | Mesaj tipi başına `RegisterMessageBroker<T>(options)` şart (open generic kaydı yok). Kayıtsız tip resolve'da hata verir (sessiz değil). → Açık kayıt kararı aşağıda |
| MessagePipe buffered pub/sub | `IBufferedPublisher/IBufferedSubscriber` yeni aboneye son değeri verir → "değişti" tipi state mesajları için geç abonelik sorununu çözer |
| Unity 6.3 paket imzası | OpenUPM paketleri imzasız; Package Manager "Signature: Missing" uyarısı verir, ilk kurulumda modal diyalog açılır. Kabul edildi; uyarı beklenen |

## Paket sürümleri (2026-10-29)
VContainer 1.19.0 · UniTask 2.5.11 · MessagePipe 1.8.2 (+VContainer 1.8.2) — OpenUPM · PrimeTween 1.5.2 — npmjs (resmi kanal; OpenUPM'deki 1.3.3 geride) ·
Newtonsoft 3.2.2 · Input System 1.20.0 · URP 17.3.0 · Pipeline 0.9.1-exp.1. Kaldırılanlar: AI Assistant, AI Inference, Visual Scripting, Multiplayer Center, Collab Proxy.
(2026-10-31) Unity 6000.3.26f1 → 6000.3.27f2 (kullanıcı yükseltti): Input System 1.20.1, AI Navigation 2.0.16, Rider 3.1.0.
F5'ten önce temel çizgi aynı: 242/242 (CarryOverTests dahil), ValidateAll 0.
(2026-11-01, F7) Localization 1.5.13 (Unity registry) eklendi; getirdikleri Addressables 2.10.3, Scriptable Build Pipeline 2.6.2,
Profiling Core 1.0.3.

## Render
- Template'te global post-process yok: PC/Mobile RP asset'lerinin Volume Profile alanı None, örnek profil silindi. Görünüm her oyunun kararı.
  Global `DefaultVolumeProfile` (URP varsayılanları) kalır.

## Açık
- ~~Input: Touch şeması template'te mi hazır durur, mobil oyunda mı eklenir?~~ (2026-10-31: template'te hazır; oynanış dokunmatik
  kontrolleri oyunun işi → "F3 kararları")
- ~~IL2CPP (F4 Windows IL2CPP build'inde): VContainer reflection injection (gerekirse kod üretimi), `EntryPointAudit` dev build'de
  reflection, struct mesajlı MessagePipe broker'ları.~~ (2026-10-31: Windows IL2CPP'de doğrulandı → "Doğrulananlar (F4)")
- ~~`IObjectResolver.Instantiate` dışında oluşturulan `[Inject]`'li prefab'ları yakalayan kontrol yok (validator yalnız sahne ve root prefab'a bakıyor).~~
  (2026-10-31: `DirectInstantiateValidator` proje kodundaki GameObject/Component `Object.Instantiate`'ini ve `[Inject]`'li
  `AddComponent`'i yakalıyor; havuz inject ediyor → "F6 kararları")
- ~~F5'e not (F1 incelemesi, 2026-10-30): `GameManager.GoTo`'da Enter fırlatırsa `Current` yeni state'te kalır ama
  `GameStateChanged` gitmez, aboneler `Current` ile uyumsuz kalır. Play durdurulurken geçiş yarıdaysa OCE `SceneEntry`'den
  VContainer'ın handler'ına düşer (`Forget(handler)` OCE filtrelemiyor) ve `Log.Exception` olarak loglanır. F1'de state'ler boş,
  tetiklenmiyor; sahne yüklemesi Enter'a girince gerçek olur.~~ (2026-10-31: Enter fırlatınca yine yayınlanır; token'ı iptal
  edilmiş OCE handler'da loglanmaz → "F5 kararları")
- ~~IL2CPP'ye ek (F2, 2026-10-30): save data sınıflarının alan/ctor'larının stripping'den sağ çıkması (Newtonsoft reflection'la
  kurar; gerekirse `link.xml` ya da `[Preserve]`), `File.Replace`'in Android/iOS'ta davranışı, thread pool'da dosya IO.~~
  (2026-10-31: Windows IL2CPP + Minimal stripping'de doğrulandı.) Açık kalan: `File.Replace` ve thread pool IO'nun Android/iOS
  davranışı; Android modülü kurulunca ilk mobil build'de. (2026-11-01: hâlâ açık → ilk mobil oyun; "Template doğrulaması" → mobil liste.)
- ~~F5'e not (F2): menüye dönüşte `SaveSlot` + `CloseSlot` sırası SceneManager akışında netleşir; sahne kapanırken savable'lar
  belleğe capture edilir, diske yazmak oyunun kararı. Aynı sahne yeniden yüklenirken yeni scope eskisi dispose olmadan kurulursa
  aynı key'li savable çift key hatası verir: Unity'nin eski sahne `OnDestroy` / yeni sahne `Awake` sırası (Single ve async)
  F5'te doğrulanır (bağımsız F2 incelemesi, 2026-10-30).~~ (2026-10-31: `SaveSlot` load'dan önce oyunun işi, `CloseSlot`
  MenuState'e girişte otomatik; loader eski sahneleri yeni sahneden önce kaldırır, Unity'nin kendi sırası da ölçüldü →
  "F5 kararları", "Doğrulananlar (F5)")
- ~~F7'ye not (F2): ayarlar `Global` (cloud'a giden: oynanış, dil) ve `Device` (çözünürlük, kalite, ses çıkışı) savable'ları olarak
  ikiye bölünür. Settings boot adımı `UseSave`'den sonra kaydedilir.~~ (2026-11-01: ekran ve ses Device, dil ve tuşlar Global;
  ekran/ses Save'in boot adımındaki `Restore`'da uygulanır, async boot adımı yalnız Localization'da → "F7 kararları")
- F8'e not (F2): Steam Auto-Cloud kökü `persistentDataPath`, desen `saves/*.json`; `local/` ve `.bak/.tmp/.corrupt` dışarıda.
  Özel depolama gerekirse `ISaveStorage` uygulaması.
- ~~F5'e not (F3, 2026-10-31): `GameStateChanged` geçiş bitince gittiği için loading (Enter) boyunca önceki state'in modu açık
  kalır; SceneManager geçiş boyunca `InputMode.None` isteği tutmalı. Sahnede açılan mod istekleri sahne scope'uyla bırakılır.~~
  (2026-10-31: yükleme boyunca mod koşulsuz None, istekler sırasıyla bekler → "F5 kararları")
- Additive yükleme sürerken (`EnqueueParent` çok kareli) kendi parent'ı olmayan başka bir scope uyanırsa o da ana sahne
  scope'unun çocuğu olur (F5 incelemesi). Olasılık düşük, Core.md'de yazılı; gerekirse aktivasyonu (`allowSceneActivation`)
  ayırıp parent'ı yalnız aktivasyon karelerinde tutmak. (2026-11-01: açık kalır, template'e ait.)
- Build'de `Application.Quit` sırasında süren sahne yüklemesi `AppSuspending(Quit)` ile iptal edilir (EditMode testinde);
  player'da tetiklenmedi (UI yok). ~~`Manual` Boot Target'lı ama Boot'tan çıkmayan sahne uyarısız Boot'ta kalır.~~
  (2026-11-01: player tarafı ilk gerçek oyunda, UI ile. Manual uyarısı kabul: Boot'ta kalan intro anında görünür.)
- `SaveLifecycle`'ın PlayTime'ı `IsLoading`'de saymaması testsiz (`Time.unscaledDeltaTime` EditMode'da sabitlenemiyor).
  (2026-11-01: resume sonrası ilk karenin arka plan süresini ekleyip eklemediği de belirsiz → ilk mobil oyun.)
- F8'e not (F3): Steam Input / Deck'te tek pad iki cihaz görünebilir (eski oyunda PS5 + Steam), Deck'te kilitli cursor dokunuşu
  ekran ortasına düşürür. Şema takibi yalnız native cihazları sayar; bunlar Steam modülünde doğrulanır.
- ~~F7'ye not (F3): cursor confine (pencere içi) ayarı ve rebind ekranı Settings'te; `IRebindService` hazır.~~ (2026-11-01:
  confine `IInputModeService.ConfineCursor`, değeri Settings verir; rebind ekranı UI, template'te yok → "F7 kararları")
- ~~Input (F3): URP'nin Rendering Debugger'ı kendi "Debug Menu" map'ini açıyor (Editor/dev build, asset'e ait değil, mod servisi
  görmez). Release build'de etkin olup olmadığı F4 build'inde kontrol edilir.~~ (2026-10-31: dev build'de açılıyor, release'de
  derlenmiyor → "Doğrulananlar (F4)")
- F8'e not (F4, 2026-10-31): mağaza build'inde Mock backend'i reddeden build kontrolü (mağaza define'ı ↔ backend), Steam'in
  overlay-kapandı callback'inin Deck'te kaybolması (eski oyun: ilk gamepad basışı "kapandı" sayıldı), `steam_appid.txt`'nin
  çıktıda olmaması, mağaza yükleme adımı. Build menüsü mağaza define'ını flavor/mağaza ekseniyle verir.
- ~~URP build sırasında `PC_RPAsset` prefilter alanlarını ve global ayarların `m_RuntimeSettings` listesini yeniden yazıyor
  (2026-10-31; dev ve release build aynı değeri üretti). Commit edilirse sonraki build'ler kirletmez; kullanıcı kararı.~~
  (2026-11-01: yeni oyun kontrol listesinde "ilk build'in yeniden yazdığı URP asset'leri ayrı commit"; template'in
  doğrulama Release build'i yeniden yazmadı → Build.md "Template doğrulaması".)
- Pipeline 0.9.1 IL2CPP player'da `script eval` derleyemiyor (referans assembly yok) ve player'a capture/hierarchy komutu
  sunmuyor; runtime doğrulama Player.log + dosyayla. Player'da "Could not restrict permissions … whoami" uyarısı (paketin
  kendisi). Paket güncellenince yeniden bakılır. (2026-11-01: açık.)
- Input (F3, 2026-10-31): template'in Gameplay modu UI map'ini de açar. Esc hem `Gameplay/Pause` hem `UI/Cancel`'a bağlı,
  tek basış ikisini tetikler; `UI/Cancel` ile kapanan bir pause menüsü açıldığı basışla kapanabilir (mod geçişi olmadığı için
  basılı tuş filtresi devreye girmez). Template'te bilerek böyle bırakıldı; gerçek oyunda map tasarımına göre karar verilir
  (Gameplay modundan UI'ı çıkarmak ya da açan/kapatan tuşu ayırmak). (2026-11-01: ilk gerçek oyun; kontrol listesinde adım.)

- F6 (2026-10-31): mobil arka plana geçişte Audio kod çalıştırmıyor (Unity ses çıkışını kendisi duraklatıyor); ilk mobil
  build'de doğrulanır. Gerçek ses cihazı değişimi elle denenmedi (Editor'da `AudioSettings.Reset` ile import edilmiş klipte
  müzik bir kare sonra yeniden çaldı; kodla yaratılmış `AudioClip.Create` klipleri reset'ten sonra hiç çalmıyor). Cihaz
  değişiminde süren sesler biter, yeniden başlatılmaz (yalnız müzik). (2026-11-01: mobilde odak susturması kaldırıldı →
  Audio.md "Template doğrulaması"; arka planda ses ve klavye/overlay davranışı ilk mobil oyunda, cihaz değişimi ilk PC oyununda.)
- F6: OnDisable'dan despawn desteklenmiyor (Unity deaktivasyon sırasında taşımayı reddeder); dokümanda yazılı, yakalayan
  kontrol yok. `TweenLeakAudit` dev build'de spawn başına O(bileşen × canlı tween); ağır havuzda gerekirse yalnız Editor'a
  alınır. Aynı prefab bileşeni farklı `T` ile `For`'a verilirse ikinci havuz açılır (dokümanda). (2026-11-01: açık kalır.)
- ~~F7'ye not (F6): kanal sesleri `IAudioVolumes.Set` ile, `MuteWhenUnfocused` ayarı da orada; kalıcılık ve boot'ta geri
  yükleme Settings'te. Ses seviyesi "değişti" mesajı tüketicisiyle birlikte.~~ (2026-11-01: `VolumeSettings` savable'ı
  `IAudioVolumes`'tan okur/yazar; `AudioVolumeChanged` Audio'da → "F7 kararları")
- F8'e not (F7, 2026-11-01): Steam'in oyun dili (`GetCurrentGameLanguage`) startup seçicisi olarak Steam modülünde. Deck'te
  Exclusive sunulmamalı (eski oyun; gamescope); Proton altında Windows build'i `WindowsPlayer` görünür, `WindowModes`
  Exclusive'i verir → Steam modülü Deck'i tanıyınca listeden çıkarır.
- F7 (2026-11-01): ekranın kendi boyutu exclusive fullscreen'de önceden okunan değer, exclusive'de açılışta `Display.main`
  (inceleme); exclusive mod canlı denenmedi (monitör modunu değiştirir). Kalite varsayılanı "uygulamanın başladığı seviye":
  Standalone player'ın seviyeyi kendisi hatırlayıp hatırlamadığı template'te gözlenemedi (platform başına tek seviye, kayıt
  defterinde `UnityGraphicsQuality` yok); hatırlıyorsa `ResetToDefaults` son seviyeye döner. Birden çok seviyeli ilk oyunda
  bakılır, gerekirse `SettingsOptions`'a seviye adı. Addressables paketi çıkarılınca `AF.Build.Editor`'daki GUID referanslarının
  davranışı denenmedi. `LocalizationValidator` yerelleştirilmiş alanları tip adı "Localized" ile başlayanlarda arar (paketin
  hepsi öyle); farklı adlı alt sınıf atlanır. (2026-11-01: exclusive → ilk PC oyunu; kalite hatırlama → ilk çok seviyeli
  oyun; Addressables çıkarma → ilk tek dilli oyun; "Localized" öneki kabul.)
- (2026-11-01) Run In Background kapalıyken Editor'da Play ve pencereli masaüstü player odak kaybında `OnApplicationPause(true)`
  alır (Unity dokümanı) → `AppSuspending(Background)`, Save değişeni ve slotu yazar. Denenmedi → Editor'da bir sonraki Play
  doğrulamasında, player'da ilk PC oyununda Release + Windowed ile (Core.md "Template doğrulaması").
- F8'e not (2026-11-01): bilinmeyen achievement id kontrolü yalnız `MockAchievements`'ta; Steam backend'inin `IAchievements`'ı
  aynısını yapar ya da kontrol `UsePlatform`'da ortak sarmalayıcıya alınır (Platform.md "Template doğrulaması").
- (2026-11-01) İlk mobil oyunda doğrulanacaklar: "Template doğrulaması" → mobil liste.

## Mesajlaşma ve isimlendirme (2026-10-29)
| Karar | Gerekçe |
|---|---|
| MessagePipe mesaj tipleri **açık** kaydedilir: sahibi olan özelliğin installer'ında `builder.RegisterMessageBroker<T>(options)`. Otomatik tarama yok | Reflection ile üretilen generic (`MakeGenericMethod`) struct mesajlarda IL2CPP'de runtime'da patlayabilir (mobil). Kapsam (root/sahne) kaydın yerinden görünür. Unutulan kayıt resolve'da hata verir, sessiz değil. Eski `DeclareSignal` alışkanlığının karşılığı |
| Kod prefix'i `AF` kalır (namespace, asmdef, `AF/` menü). Kullanıcının takma adından (afoolasfallen) | Kısa, çakışma riski yok. Kişisel/marka prefix'i normal (cysharp, hadashikick) |
| ~~Paket kimliği önerisi: `com.afoolasfallen.framework` (F1 planında teyit edilecek; olmazsa `com.af.framework`)~~ | ~~UPM kimliği benzersiz olmalı; "af" tek başına belirsiz~~ (2026-10-29: önce `com.af.framework` seçildi, sonra paket kaldırıldı; soru düştü) |
| İleride ad değişirse repo üzerinde değiştirilir ya da bu template'ten ayrı bir public/free template türetilir | |

## Fazlar
F0 repo hijyeni + paketler + editor köprüsü (bitti) · F1 framework iskeleti (bitti) · F2 Save (bitti, 2026-10-30) · F3 Input (bitti, 2026-10-31) · F4 Build menüsü + platform
interface/Mock (+ Windows IL2CPP build) · F5 SceneManager (pause F1'e taşındı) + intro akışı: `BootTarget.Manual` (sahne Boot'tan
kendisi çıkar), build index 0'da boş Intro örneği, intro → Menu geçişi; intro boot adımları sürerken oynar, Editor'da başka sahneden
Play intro'yu atlar (2026-10-29) · F6 Pool + tween yaşam döngüsü + Audio ·
F7 Settings + Localization · F8 Steam modülü · New Game Setup penceresi uygun fazda.
(2026-11-01) F7 bitti (Settings, Localization, Builder'da Addressables içerik adımı).
(2026-11-01) F8 (Steam modülü) ertelendi: yalnız Steam'e bir oyun yapılırsa. Sıradaki iş template doğrulaması, bulguların
düzeltilmesi ve README (EN/TR); oturum prompt'u `.claude/prompts/template-validation.md`. Mobil, ayrı repo'da yapılacak gerçek
bir mobil oyunla (test + portfolyo) denenir; orada çıkan template sorunları oyun repo'sunda yamanmaz, template'te düzeltilip
oyuna taşınır. New Game Setup penceresi bekler: doğrulamada yeni oyun adımları kontrol listesi olarak çıkarılır, pencerenin
gerekip gerekmediği ilk gerçek oyunda görülür. Karar kaydı `.claude/decisions/` altında modül başına dosyalara bölündü.
(2026-11-01) Template doğrulaması ve README (EN/TR) bitti → "Template doğrulaması (2026-11-01)".
(2026-10-31) F6 bitti (Pool, tween yardımcısı + validator'lar, Audio).
(2026-10-31) F5 bitti (SceneManager → `ISceneLoader`, intro akışı). F4 bitti. F4'ten sonra, F5'ten önce: F1–F3 kodunun yeni kod stiline toplu dönüşümü (tek mekanik commit,
serialize alanlarda `[FormerlySerializedAs]`) (2026-10-31: uygulandı → "Stil dönüşümü"). New Game Setup penceresi F8'den sonra, ilk gerçek oyundan önce (adım listesi
o zaman tam: Steam app id, flavor, CLAUDE.md/decisions kopyası, namespace).

## Template doğrulaması (2026-11-01)
F0–F7'den sonra template'in bütün halinde doğrulanması. Kod ↔ karar ↔ doküman taraması modül grubu başına bağımsız ajanla
(Core+Build, Save+Input+Platform, Audio+Settings+Localization), iddiaları kodda tek tek doğrulandı. Modülün bulguları ve
düzeltmeleri kendi dosyasında ("Template doğrulaması (2026-11-01)"); burada temel çizgi, özet ve listeler.

| Kontrol | Önce | Sonra |
|---|---|---|
| EditMode | 380/380; sonrası 14 error + 8 warning | 393/393; sonrası 16 error + 8 warning (yeni beklenen: `AudioListener.volume` dışarıdan, `Restore` fırlatan savable) |
| ValidateAll | 0 | 0 |
| `Builder.Check(Release)` | placeholder hatası | placeholder hatası + commit'lenmemiş değişiklik uyarısı |
| Windows IL2CPP Release (`AllowPlaceholderIdentity`) | — | Succeeded 5:19 (Build.md "Template doğrulaması") |

Özet: 1 hata (`SettingsOptions` doğrulanmıyordu), 6 risk (masaüstünde alt-tab suspend, mobilde odak susturması, prefab'da
PlayerInput/EventSystem, `AddComponent(Type)`, raporsuz biten build yolu, achievement kontrolünün yalnız Mock'ta olması),
11 doküman grubu, 3 nit. Kod düzeltmeleri testli; R1 (alt-tab) ve R6 (achievement) kod değişmeden belgelendi.

Validator ↔ test: dokümanın "validation reports" dediği her kural için test arandı. Testsiz olanlar bellekte (kaydedilmemiş
geçici sahne, scratchpad) denendi ve çalıştı, sonra kalıcı teste çevrildi; IL validator'larının eksik çağrıları fixture
vakası olarak eklendi. Testsiz kalan: eksik script (bellekte üretilemiyor, Unity API'si), sahnede RootScope (fixture'da somut
RootScope yok; bellekte denendi).

Kabul edilenler (değişiklik yok, gerekçeleri modül dosyalarında): modül içi tiplerin somut inject'i, test için internal ikinci
ctor, `InternalsVisibleTo`, scope build anında Unity objesinde `?.`, tek abonelikli serviste `IDisposable` alanı.

### İnceleme (bağımsız, git diff üzerinden, 2026-11-01)
| Bulgu | Düzeltme |
|---|---|
| `Builder`: dış catch build sonrası `NoteChangedFiles`'ı da kapsıyordu; başarılı Release'ten sonra kilitli bir dosyanın hash'i fırlatırsa rapor "Failed" der ama klasör silinmezdi | Hash dosya başına IO/erişim hatasını yakalar (`(unreadable)`), build sonrası yol fırlatmaz; dış catch notu "hiçbir şey build edilmedi, önceki build yerinde" |
| Editor'da Play de Run In Background kapalıyken odak kaybında `OnApplicationPause(true)` alır (Unity 6000.3 dokümanı); yalnız pencereli player yazılmıştı | `SuspendReason.Background`, Core.md, Save.md, CLAUDE.md; "Açık" maddesi Editor'u da kapsıyor |
| `InputSetupValidator` prefab taraması muafiyetsiz: `Assets/` altındaki örnek/demo prefab'ı (EventSystem, PlayerInput) her build'i durdurur; root'a iç içe prefab olarak konan EventSystem de raporlanır | Kabul: AudioValidator/LocalizationValidator'la aynı politika ("hata sessiz kalmaz"); örnek içerik silinir ya da düzeltilir. Input.md: EventSystem root prefab'ın düz çocuğu, kendi prefab'ı değil |
| Input.md "Touch şeması hiç Editor'da seçilmez" fazla güçlü: dokunmatik Windows laptop'ta native touchscreen Editor'da da var | "Gerçek touchscreen ister; Simulator/Remote'unki seçtirmez" |
| Güncellenmemiş XML: `AudioOptions`/`IAudioVolumes` "players only", `DirectInstantiateValidator` ve `InstantiateExemptAttribute` `AddComponent(Type)`'ı söylemiyordu; CLAUDE.md kopya kuralı da | Hepsi |
| Platform.md "mağaza backend'i de aynısını yapar" yazılmamış kod hakkında olgu cümlesi | "must do the same" |
| `ILoadingScreen` token'ı quit'te ve Play dururken de iptal olur | Yorum |
| `LocalizeStringEvent`/`LocalizeTextureEvent` `[ExecuteAlways]`: testte `OnEnable` tablo yüklemesi başlatabilir | Test kökü bileşenlerden önce kapatılır |
| Nit'ler: `CheckSettings`/`CheckScene` özeti; `nameof(TState)` paramName; değişen paragraflar sarılmamıştı | Düzeltildi. İlk sarma betiği değişmeyen paragrafları da sarmıştı: değişmeyenler HEAD'in kırılımına geri alındı, C# yorumları elle |
| `VContainerSettings` testte `DestroyImmediate` → statik `Instance = null` | Kabul: Edit Mode'da zaten null, Play'e girişte yeniden kurulur |

### Yeni oyun kontrol listesi
New Game Setup penceresi yerine; pencerenin gerekip gerekmediği ilk gerçek oyunda görülür. README'de 2. madde olmadan.
1. GitHub "Use this template" → yeni repo, klonla. Template'te LFS nesnesi yok; kurallar `.gitattributes` ile gelir.
2. CLAUDE.md ve `.claude/` template'le gelir (`prompts/`, `notes/` hariç). CLAUDE.md'nin başı ve Durum satırı oyuna göre
   yeniden yazılır (template'in fazlarını anlatır); oyunun kararları `decisions/Game.md`, framework dosyaları yalnız framework
   değişince.
3. Unity 6000.3.27f2 ile aç; OpenUPM "Missing Signature" diyaloğunu kabul et.
4. Platform modülü (Windows IL2CPP; mobil için Hub'dan Android Build Support + SDK/NDK/JDK). Platform Build Profiles'tan değişir;
   build menüsü değiştirmez.
5. Company / Product / Version. Identifier override kapalı → `com.<Company>.<Product>`. ⚠ Product adında boşluk/simge varsa
   Player Settings'te görünen identifier'a bak; build kontrolü yalnız placeholder'a bakıyor.
6. **AF > Build > Check Release** placeholder'sız.
7. Namespace: `Assets/Game/Scripts/**` `namespace Game` + `Game.asmdef` Root Namespace; asmdef adı `Game` kalır.
8. İsteğe bağlı modül çıkarma: `Use*` satırı, klasör, `Game.asmdef` referansı (Localization için Localization.md).
9. Sahneler Intro (index 0, Manual) / Menu (Menu) / Gameplay (Playing). Ad değişirse build listesi + `Scenes.cs` birlikte.
   ⚠ İkisini eşleyen kontrol yok; eksik ad ilk `Load`'da `ArgumentException`.
10. Root prefab (GameRootScope + tek EventSystem), VContainerSettings Preloaded Assets'te; loading screen buraya.
11. Mixer grupları ↔ `AudioChannels.cs`.
12. `Controls.inputactions` map'leri ↔ `InputModes.cs`; Esc'in `Gameplay/Pause` + `UI/Cancel` çakışmasına karar.
13. Localization: örnek en/tr ve `UI/menu.play` değişir; Specific seçici oyunun ana dili; tek dilliyse modül çıkar.
14. Mobil: Default Orientation (template: Auto Rotation, dört yön); Android API 25 / ARM64 / IL2CPP / Minimal hazır; imzalama yok.
15. Kalite: Android/iOS Mobile, Standalone PC.
16. Console 0, testler, **AF > Validate All** 0.
17. **AF > Build > Development**, çalıştır; sonra Release. İlk build'in yeniden yazdığı URP asset'leri ayrı commit.

### İlk mobil oyunda doğrulanacaklar
Kod okumayla mobil yolu olan ama hiç çalışmamış şeyler; sorun çıkarsa template'te düzeltilip oyuna taşınır.
- Android Build Support ile ilk build: IL2CPP + Minimal'de VContainer reflection, struct MessagePipe, Newtonsoft (Windows'ta
  doğrulandı); Addressables içeriğinin pakette olması (build sonrası kontrol paketin içine bakmıyor); IL2CPP BackUp klasörü.
- Save: `File.Replace` ve thread pool IO (Android/iOS); arka plana geçişte yazım ve ardından process ölümü; PlayTime resume.
- OS yedeği (iCloud, Android Auto Backup) `local/device.json`'u ve PlayerPrefs'i yeni cihaza taşıyor mu; gerekirse yedek kuralı.
- Ses: arka planda Unity'nin çıkışı duraklatması; ekran klavyesi/overlay sırasında susmama (odak artık mobilde izlenmiyor).
- Ekran: 60 FPS varsayılanı, `0 = yenileme hızı`, uyarlamalı yenileme; Editor/Device Simulator mobil yolu çalıştırmadığı için
  yalnız cihazda.
- Input: native touchscreen'le Touch şeması ve gizli cursor; Android Back → Escape → `Gameplay/Pause` + `UI/Cancel`.
- Localization: System seçicisinin cihaz dilini bulması.
- Development build'de CLI bağlantısı yok (Pipeline dev define'ı yalnız Standalone); log `adb logcat`.

## Doğrulananlar (iş akışı, F7)
| Konu | Sonuç → kural |
|---|---|
| Editor `script run` | Yeni dosyalar bazen derleme listesine girmiyor (watcher kaçırıyor; `ImportAsset`, temiz derleme, içerik değişikliği yetmedi). `AssetDatabase.MoveAsset` ile taşıyıp geri almak düzeltti. Yeni dosyadan sonra `.rsp`'deki kaynak sayısına bakılır |

## Repo görünürlüğü (2026-10-29)
- ~~Repo iş arayışı için public olacak. `CLAUDE.md`, `decisions.md`, `.claude/` gitignored. Public'e geçerken README yazılır.~~
- ~~Bedeli: "Use this template" bu dosyaları taşımaz → New Game Setup'ta elle kopyalama adımı.~~
- (2026-11-01) `CLAUDE.md` gitignored'dan çıktı, commit'leniyor: template şimdilik private; AI'la nasıl çalışıldığını göstermek
  artık istenen bir şey. `decisions.md` ve `.claude/` yerel kalır. Public'e geçmeden CLAUDE.md kişisel yol ve eski ürün atfı
  için kontrol edilir; public repolarda eski ürüne hiçbir atıf olmaz. Push kuralı: kullanıcı "pushla" dediğinde. Commit'lere
  Co-Authored-By satırı eklenir (AI kullanımı gizlenmez; README'deki rol cümlesiyle dengelenir).
- (2026-11-01) `.claude/` de commit'leniyor; karar kaydı `.claude/decisions/`'a modül başına bölündü (tek `decisions.md` ~40 bin
  token'dı, her faz başında baştan okunuyordu). Yerel kalanlar `.claude/settings.local.json`, kilit dosyaları, `prompts/` ve
  `notes/` (geçici; template'ten açılan oyuna taşınmamalı). "Use this template" ile CLAUDE.md, kararlar ve betikler yeni
  oyuna taşınır. Public'e geçmeden `.claude/` da kişisel yol ve
  eski ürün atfı için kontrol edilir (karar dosyalarında ürün adı yok, "eski oyun / eski proje" anlatımları var).
