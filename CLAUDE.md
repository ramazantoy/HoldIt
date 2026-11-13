# CLAUDE.md

HoldIt: Unity 6.3 LTS + URP ile portre bir Android oyunu. Yukarıdan bakışlı tuvalet telaşı: erkek ve kadın karakterler
(Thin/Normal/Fat) gelir, sabır barları dolar. Oyuncu parmağıyla karakterden kare kare yol çizer. Yanlış kapı, dar geçit
(Fat için) ya da dolu kare karakteri olduğu yerde durdurur. Doğru tuvalette karakter boş kabine girer ya da kapıda bekleyip kapıyı
tıkar. Bar dolarsa kaza olur, üç kazada oyun biter. Oyun mağazaya çıkmaz; amaç public GitHub portföyü ve template'in sahada
denenmesi. Framework template'ten gelir: DI context, GameManager, SceneManager, Save, Audio, Input, Pool, Settings,
Localization, Platform, Build. Framework kararları `.claude/decisions/`'ta (indeks `README.md`, modül başına dosya), oyunun
kararları `.claude/decisions/Game.md`'de.

**Durum:** template'ten fork `a949bbd` (framework geçmişi `.claude/decisions/README.md` → "Fazlar"). M0–M9 Editor'da bitti
(M5 M9'a katıldı); ayrıntı git'te ve Game.md'de. Bekleyenler Game.md → Açık'ta: cihaz testleri ve M0b (Android cihaz yok), M2b
(karakter modelleri), kitin kabin parçaları, kalibrasyon, ses cue'ları. Sıradaki iş M10. Milestone başlarken
plan moduna geç.

**Yapı:** framework `Assets/Framework/` (`AF.*` namespace ve asmdef'ler, modül başına `<Modül>/{Runtime,Editor,Tests}`).
Oyun `Assets/Game/`: asmdef `Game`, namespace `HoldIt`, özellik klasörü `HoldIt.<Özellik>`.
Testler `Assets/Game/Tests/` (`Game.Tests`), editor kodu ilk editor script'iyle `Game.Editor`. Framework oyuna referans
vermez. İnsan için doküman: `Assets/Framework/Documentation~/`. Tasarım `Design/`'da (gitignored, yalnız yerel).

## HoldIt
Oyuna özel her şey bu bölümde ve dosyanın başında durur. Aşağıdaki bölümler template'le aynı kalır; template'ten gelen
cherry-pick'ler çakışmasın diye.
- **Dil:** kullanıcıyla Türkçe konuşulur. CLAUDE.md, `.claude/` ve `Design/` Türkçe yazılır. Kod, yorum, commit mesajı ve
  framework dokümanı İngilizce.
- **Kaynaklar:** `Design/GDD.md` yol gösterir, parçalarından vazgeçilebilir. `Design/prototype/index.html` (three.js)
  davranış ve tuning referansıdır, kodu port edilmez. İkisi ayrışırsa kullanıcıya sor (bilinen farklar: Game.md → Açık).
  Çelişkide CLAUDE.md ve decisions GDD'nin önüne geçer.
- **Template:** framework burada yamanmaz. Kapsam: `Assets/Framework/`, `.claude/scripts/`, `.claude/decisions/`'ın
  framework dosyaları, bu dosyanın HoldIt dışı bölümleri ve `.claude/unity-editor.md` (template'teki CLAUDE.md'nin "Unity
  Editor ile çalışma" bölümü; template'ten o bölüme gelen cherry-pick buraya elle aktarılır). Bulunan sorun Game.md → "Template'e gidenler"e yazılır,
  template'te düzeltilir, sonra `git fetch template` + `git cherry-pick -x <sha>` ile alınır.
- **Faz = milestone:** template'in faz kuralları milestone'lar için de geçerli. Her milestone oynanabilir halde biter. Commit
  mesajı kısa, emir kipinde ve İngilizce olur.
- **Oyun kuralları:**
  - Kurallar (grid, yol, sabır, kabin, spawn) saf C# ile yazılır; MonoBehaviour yalnız view içindir.
  - Her kural sınıfının EditMode testi aynı milestone'da yazılır. Her test dosyasında kod bir kez bilerek bozulur; test yine
    geçiyorsa işe yaramıyordur.
  - Testleri kullanıcı Test Runner'da koşar ve kırılanları söyler. Claude derlemeyi (`console status`) kontrol eder, yeni
    yazdığı test dosyasını gerekirse filtreyle koşar; tam koşu yapmaz.
  - Ayarlanabilir her sayı ScriptableObject config'de durur.
  - Özellikler birbirini interface ve MessagePipe mesajıyla tanır (aşağıdaki mimari kurallar).
  - Oynanış kodu yalnız `IPointerInput`'u bilir (Point/Press, `Gameplay` map'i); legacy `Input.*` kullanılmaz.
  - Dosyalar küçük ve tek amaçlı olur. Template API'si uydurulmaz; eksikse söylenir ve öneri yapılır.
  - Ekranlar ve HUD Canvas (uGUI) + TextMeshPro ile, her ekran bir prefab; tam ekranlar `ScreenView`'dan türer (fade,
    gizliyken raycast ve çizim yok). Sabır barı dünyadaki nesnedir; konuşma balonu ve "+1" HUD altındaki dünya UI katmanında, ekran uzayındadır (Game.md → UI).
  - Repo public olacak: asset yalnız CC0 ya da kendi işin olur; font OFL olabilir, `OFL.txt` yanında durur. Ücretli paket
    (Asset Store, Synty) repoya girmez.

### Milestone'lar
| # | İçerik | Bitti sayılır |
|---|---|---|
| M0 | Kimlik, portre, Intro, font, Canvas menü, `IPointerInput`, Android geri tuşu | Bitti; cihaz kısmı Game.md → Açık → Cihaz testi |
| M0b | Localization en + tr: örnek tablolar oyunun tablolarıyla değişir, Specific seçici belirlenir. "Yeni oyun kontrol listesi"nin kalan adımları ve "İlk mobil oyunda doğrulanacaklar"dan bu build'le bakılabilenler (ikisi de `decisions/README.md` → "Template doğrulaması") | Telefon diline göre en/tr açılır; kontrol listesi ve mobil doğrulama sonuçları decisions'a yazılır |
| M1 | Grid, harita asset'i, portre kamera sığdırma | Bitti |
| M1b | Ortam kiti (kendi Blender kitimiz), toon shader | Bitti |
| M2 | Yol modeli, karakter state machine, placeholder karakter (6 prefab, havuz) | Bitti |
| M2b | Karakter modelleri gelince (zamanı modele bağlı): FBX import ayarları, 6 prefab'ın içi, UV renk/ifade varyantı (ofset hesabı saf C# + test), parça animasyonlarının gerçek modelle ayarı | 6 varyant gerçek modelle yürür; M2'nin kontrolleri yine geçer |
| M3 | Çizim input'u, adım doğrulama, doluluk ve rezervasyon, geri dönüş, balonlar | Bitti |
| M4 | Tuvaletler, kabinler, ışıklar, kapıda kuyruk (parçalar placeholder) | Bitti |
| M5 | M9'a katıldı: M3'ün kare işaretleri kalır, cilası M9'da | — |
| M6 | SO seviyeler, spawn, sabır, kaza, can, seviye sonu, pause | Bitti |
| M7 | Seviye seçimi, ayarlar, ilerleme kaydı | Bitti; cihaz kısmı Game.md → Açık |
| M8 | Seviye başına harita, üretici, harita aracı, simülasyonla tuning | Bitti; kalibrasyon Game.md → Açık |
| M9 | Juice: PrimeTween animasyonları, kare işaretlerinin cilası (eski M5), sesler, haptik | Bitti (kod ve doğrulama Editor'da); ses cue'ları kullanıcıda (Game.md → Açık → Ses cue'ları), haptik ve his kontrolü cihazda (Game.md → Açık → Cihaz testi) |
| M10 | Eksik kalan testler, README EN/TR (GIF + diyagram), Release APK GitHub Releases'te (APK git'e girmez) | Public repo hazır |

## Çalışma şekli
- Fazlarla ilerliyoruz. Her seferinde yalnız bir faz planlanır; onaylanınca uygulanır, bitince durulur.
- İş oturumda bitmeyecekse ya da bağlam dolmak üzereyse durmadan önce handoff notu yazılır (`.claude/README.md` → "Handoff").
- "Bence X kötü değil mi?" tipi sorular tartışma sorusudur, kod isteği değil. Net onay gelmeden dosyaya dokunma.
- Katılmadığın yerde gerekçesiyle itiraz et. Kilitli kararlar güçlü gerekçeyle tartışılabilir ama sessizce değiştirilmez.
- Seçenek listesi dökme; bir öneri yap ve nedenini söyle.

## Git
- Push yalnız kullanıcı açıkça istediğinde ("pushla"). Commit ve stage yalnız istenince. Commit mesajının sonuna Claude Code'un
  `Co-Authored-By: Claude … <noreply@anthropic.com>` satırı eklenir.
- İlk binary asset'ten önce `.gitattributes` ve LFS hazır; yeni binary türü eklenirse `.gitattributes`'a ekle.

## Unity Editor ile çalışma
Ayrıntılar ve bilinen tuzaklar `.claude/unity-editor.md`'de: köprü komutları, Play sürme, build, test, Input/Audio/VContainer
tuzakları. Editor'da takılınca, CLI zaman aşımında ya da yeni tür bir Editor işinden önce oku.
- Köprü: Unity CLI (`unity command ...`, parametre `--ad değer`). Komut arama `--query <terim>`; durum `editor status`;
  console `console status` (güncel hata `groundTruth.consoleErrors`).
- Editor kodu: `unity command script run --file <yol.cs> --entry Tip.Metot` (`--args '["x"]'`). Toplu sahne/asset işi bu yoldan.
- **Unity YAML'ını (scene, prefab, asset) asla dosya olarak elle yazma.** Hedef asset için `SaveAssetIfDirty(obj)`, `SaveAssets()` değil.
- Refresh + bekleme + sayaçlar: `bash .claude/scripts/wait-editor.sh --refresh`. Unity komutları sade (`cd` öneki, pipe, elle döngü yok).
- CLI zaman aşımında önce modal diyalog ara. Geçici script/çıktı proje köküne ya da `Assets/`'e bırakılmaz. Editor state'ini
  değiştirdiysen söyle. Build yalnız `AF.Build.Editor.Builder`.

### Onay sınırı
- Serbest: okuma (status, console, hierarchy, component okuma, test, recompile).
- Serbest ama raporlanır: onaylı faz kapsamındaki sahne/prefab/asset oluşturma ve düzenleme.
- Her zaman onaylı: silme, paket ekleme/çıkarma, proje ayarı değişikliği, build. Onaylı faz planında adıyla yazılanlar onaylı sayılır.

## Kod stili
- Private ve `[SerializeField]` alanlar `_camelCase` (alt çizgi önekli), `static`/`static readonly` private alanlar dahil;
  erişim belirteci açık yazılır (`private`). Örnek: `[SerializeField] private float _moveSpeed;`,
  `private readonly IPauseService _pause;`, `private static readonly Regex _keyPattern`.
- Belirteçsiz üye ya da tip yok; varsayılan da yazılır: üye ve iç içe tipte `private`, üst düzey tipte `internal`.
- `private const`: string, char ve key sabitleri `UPPER_SNAKE_CASE`; sayısal tuning ve enum (`BindingFlags`) sabitleri `PascalCase`.
- Tip, metot, property, event ve public üye `PascalCase`; parametre ve local `camelCase`. İstisna: save data sınıflarının
  public alanları `camelCase` (JSON anahtarı olurlar; rename migration ister).
- Tüm kod bu stilde (F1–F3 2026-10-31'de dönüştürüldü). Serialize alan rename'inde `[FormerlySerializedAs("eskiAd")]` şart,
  yoksa sahne/prefab'daki değer sessizce default'a düşer (`SceneScope._bootTarget`, `GameRootScope._inputActions`). Attribute
  kalır; YAML aynı commit'te yeni adla yeniden yazılır: sahne `AssetDatabase.ForceReserializeAssets` ile, prefab'ı o yazmadı →
  bileşene `EditorUtility.SetDirty` + `AssetDatabase.SaveAssetIfDirty`. Sonra `unity vcs diff` ile yalnız ad değişmiş mi bak.
- Toplu mekanik refactor: Editor'ın `Library/Bee/artifacts/1900b0aEDbg.dag/<asmdef>.rsp` dosyası Unity dışında aynı derlemeyi
  kurmaya yetiyor (referans + define + kaynak listesi; Roslyn `Editor/Data/DotNetSdkRoslyn`'de). Semantik model ile rename
  parametre/local'e dokunmaz; ayrıca HEAD'e karşı token diff ile "yalnız izinli fark" kontrolü.

## Mimari kurallar
- DI: VContainer. Bind ve inject interface üzerinden; bağımlılıklar tek constructor'dan (MonoBehaviour'da tek `Construct`).
- Scope'lar: `RootScope` (ProjectContext; oyunda `GameRootScope`, prefab'ı VContainerSettings → Preloaded Assets) ve her sahnede
  tek `SceneScope` (SceneContext). Kayıt `ConfigureGame` / `ConfigureScene`'de. Bootstrap sahnesi yok, her sahneden Play çalışır.
- VContainer sahne objelerine otomatik inject etmez: `[Inject]` alan bileşen scope'ta serialize alan + `RegisterComponent` ile ya da
  `autoInjectGameObjects` altında olmalı. `RegisterComponentInHierarchy` kullanılmaz (validator göremez). Serialize alan
  kaydedilmezse scope build'inde `ScopeFieldAudit` hata loglar (prefab asset tutan alan hariç: onu kopyalayan inject eder).
- `ITickable`/`IStartable` uygulayan sınıf `RegisterEntryPoint` ile kaydedilir; yalnız `.As<IFoo>()` ile kaydedilirse hiç çalışmaz
  (scope build'inde `EntryPointAudit` hata loglar).
- Mesaj: MessagePipe normal `IPublisher/ISubscriber` (SignalBus karşılığı). Abonelik constructor/`Initialize`'da, publish en erken
  `Start`'ta. "Değişti" mesajı state servise yazıldıktan sonra atılır ve yeni değeri taşır; durumu yansıtan abone başlarken
  servisten bir kez okur. Komut ve "değişti" mesajları ayrı; UI yalnız "değişti"yi dinler. Her abonelik dispose edilir: birden
  çoksa `DisposableBag`, tek abonelik `IDisposable` alanında.
- Buffered pub/sub kullanılmaz: struct mesajda abone olunca hiç publish olmasa da `default(T)` gelir.
- Pause GameManager state'i değil: `IPauseService.Request(reason)` handle döner, en az bir handle yaşadıkça oyun durur.
- `Time.timeScale`'e yalnız `IGameClock` yazar (pause → 0, değilse `Scale`). Slow-mo `clock.Scale` ile.
- Async: UniTask. Yardımcılar `OperationCanceledException` yutmaz.
- PrimeTween: tween yalnız `ToUniTask(ct)` (Core) ile await edilir; iptal tween'i olduğu yerde durdurur, await OCE atar.
  PrimeTween'in kendi await'i `Stop()` sonrası ve önceden iptal edilmiş token'la normal döner, await sonrası kod "tamamlandı"
  sanır. Diğer await yolları (`await tween`, `ToUniTask()`, UniTask'e dönüşüm, `yield return tween`, IEnumerator
  `WithCancellation`) validator hatası (`[TweenAwaitExempt]` istisna). Await edilen tween token'la durdurulur, `Stop()` ile değil;
  `defaultCancellationMode` Stop kalır.
- MessagePipe.VContainer'da her mesaj tipi, sahibi olan özelliğin installer'ında `RegisterMessageBroker<T>(options)` ile açıkça
  kaydedilir. Reflection'la otomatik kayıt yok (struct mesajlarda IL2CPP riski).
- Havuz: `IPools.For(prefab)` (root'ta Scoped: her scope kendi havuzu, kendi kayıtlarıyla inject; scope bitince spawn'lılar dahil
  hepsi yok). Instance kapalı konteynerde yaratılır, prefab kökünün durumu önemsiz. Abonelik OnEnable/OnDisable'da; reset ve çocuk
  `SetActive` `IPoolable.OnSpawned/OnDespawned`'da. Yaşamın async işi ve tween'i `life.Token`'a bağlı (despawn iptal eder;
  bağlanmamış tween Editor/dev'de spawn'da hata). Tween iptali PrimeTween'in sonraki update'inde (değer yazmadan): await
  sonrası kod (`catch`/`finally` dahil) instance'a dokunmadan önce `life.IsCurrent`'a bakar, instance o arada yeni yaşamda
  olabilir. Yaşamı aşabilecek referans instance değil `PoolLife` tutar. Awake/OnEnable'dan istenen despawn aktivasyon bitince
  uygulanır; OnDisable'dan despawn yok (Unity reddeder). Tween'in bitişi havuza tek dönüş yolu olmaz.
- Kopya: GameObject/Component `IObjectResolver.Instantiate` ya da havuzla; `Object.Instantiate`/`InstantiateAsync`, `[Inject]`'li
  bileşene `AddComponent` ve `AddComponent(Type)` validator hatası (`[InstantiateExempt]` istisna). Material/ScriptableObject serbest.
- Root'ta `Lifetime.Scoped` kayıt her scope'ta ayrı instance verir; Scoped servis entry point olmaz (yalnız kaydeden scope'ta
  çalışır; `EntryPointAudit` hata loglar).
- Tween/await bittikten sonra state'e dokunmadan önce sahipliği kontrol et; koşulsuz geri yükleme yok. Pause'da çalışan UI tween'i
  unscaled time kullanır.
- Interface tipli alandaki Unity objesi için `!= null` değil `IsAlive()`.
- Settings: ekran ayarları `IDisplaySettings` (Device `settings.display`), kanal sesleri `IAudioVolumes`'tan `VolumeSettings`
  ile (Device `settings.audio`), dil `ILocaleService` (Global `localization.locale`), tuşlar Input (Global). Oyuna özel ayar
  oyunun kendi Global savable'ı. `Screen.SetResolution`/`fullScreen(Mode)`, kalite seviyesi, `vSyncCount`, `targetFrameRate`
  ve `ConfineCursor`'a yalnız `IDisplaySettings` yazar (validator; `[DisplaySettingsExempt]`). Ekran ayarında data alanı
  `null` = varsayılan, varsayılana eşit seçim `null` yazılır (kanal sesleri her zaman yazılır). Setter diske yazmaz; kayıt ekran kapanırken oyunun `SaveGlobal()`'ı (global + device)
  ve suspend/quit. Kapanış çağrısı `OnDisable`'a değil ekranın hide metoduna. Alt+Enter kapalı (`allowFullscreenSwitch`).
- Localization: `LocalizationSettings.SelectedLocale`'e yalnız `ILocaleService` yazar (validator; `[LocaleChangeExempt]`).
  `PlayerPrefLocaleSelector` kullanılmaz; seçiciler CommandLine → System → Specific. Dil listesi `Available`'dan, yerel adla.
  Metne `LocalizedString` alanıyla bağlan (validator kırık referansı yakalar, string anahtarı yakalayamaz); sık kullanılan
  tablolar Preload. `UseLocalization` `UseSave`'den sonra (boot adımı kayıtlı dili uygular).
- Audio: ses `SoundCue` asset'i + `ISounds` (Scoped: sahnenin sesleri sahnenin scope'u bitince durur, root'unkiler sürer;
  sahneyi aşması gereken ses `IGlobalSounds`); kodda mixer grubu adı yok. Müzik `IMusic` (tek parça, crossfade, pause'da ve sahne değişiminde sürer). Kanal sesi `IAudioVolumes`
  (lineer 0–1; kalıcılık Settings'te, değişince `AudioVolumeChanged`), susturma `Mute(reason)` handle. `AudioListener.pause/volume`'a yalnız Audio yazar. Tek
  AudioListener root'ta (`IAudioListener.Follow`); sahne ve prefab'da AudioListener ve çıkışsız AudioSource validator hatası.
  Kanallar `Game/Scripts/AudioChannels.cs`, mixer `Game/Audio/GameMixer.mixer`.
- Zaman: `Time.time` cache'leyip karşılaştırma yok; `IGameClock.Time`.
- Log: `AF.Core.Log`. `Info/Warning` Editor'da ve `SHOW_LOGS` build'lerinde, `Error/Exception` her zaman.
- Zorunlu serialize referansa `[Required]`; yeni kontrol `IValidator` (editor asmdef'te, otomatik bulunur).
- Paylaşılan materyale runtime'da yazma; MaterialPropertyBlock ya da instance materyal.
- Global `JsonConvert.DefaultSettings` asla set edilmez. Save kendi ayarını kullanır (`SaveJson`).
- Save: savable düz C# servis, `Savable<TData>`'dan türer, singleton + `AsImplementedInterfaces()` ile kaydedilir; scope bağlar
  (elle liste yok). MonoBehaviour/ScriptableObject savable yok. `Capture` yalnız kendi alanını okur (sahne kapanırken de çalışır),
  `Restore` tüm state'i değiştirir ve fırlatmaz. Key sabit küçük harf string. Data sınıfı: public alan, parametresiz ctor,
  Unity objesi/interface/abstract üye yok (validator yakalar). Enum ya da alan rename'i migration adımı ister. Sahne
  savable'ı scope build'inde restore edilir: `[Inject] Construct`'tan sonra, entry point'lerden önce → kayıtlı state
  `Construct`'ta okunmaz, `Initialize`/`Start`'ta okunur.
- Save: `NewSlot` ve save-as dolu slotun üzerine yazar; oyuncuya sormak oyunun işi (`ListSlots`). Slot kullanmayan oyun
  `SaveOptions.UsesSlots = false` verir (dev oturumu ve build'deki hata gider, slot işlemleri fırlatır).
- Save: hiçbir dosya otomatik silinmez; kilit/IO hatası bozuk sayılmaz. Migration adımı tek fonksiyon + önce/sonra testi;
  yayınlanmış adım değiştirilmez. "Kaydedildi" geri bildirimi yalnız `SaveResult.IsWritten`'da.
- Arka plana geçiş/çıkış işi `AppSuspending` aboneliğinde senkron biter; await edilen hiçbir şey tamamlanmayabilir.
- Input: map/action `Enable`/`Disable` ve `Cursor.lockState`/`visible`'a yalnız input mod servisi dokunur. Kod
  `IInputModeService.Request(mode)` ile mod ister, handle'ı işi bitince (scope dispose, state `Exit`) bırakır. Etkin mod = en
  yeni canlı istek, yoksa game state'in modu; pause'da `InputOptions.Paused` o ana kadarki isteklerin üstüne biner. Hep açık map
  yok; her mod değişiminde modun map'leri tam açılır, kapalı action guard sayılmaz. Callback'te pause/mod guard'ı yazılmaz;
  `canceled` her zaman gelir. Validator proje kodundaki doğrudan çağrıyı yakalar (`[InputModeExempt]` istisna).
- Input: import edilmiş `InputActionAsset` kullanılır (root scope alanı); "Generate C# Class" kapalı (ayrı kopya üretir).
  Action'lar constructor'da `FindAction(..., throwIfNotFound: true)` ya da `InputActionReference` ile alınır. Ayrık komut
  Button, sürekli değer Value, PassThrough yalnız pointer. Callback abonelikleri `action.Subscribe(...)` + `DisposableBag`.
- Input: EventSystem yalnız root prefab'ta; sahnede EventSystem/input modülü, `PlayerInput` yok. UI input'u engellemek için modül
  kapatılmaz, UI map'i olmayan mod istenir.
- Input: binding silinip yeniden yaratılmaz (id değişir, kayıtlı override düşer); yolu yerinde değiştirilir. Rebind sonrası
  kayıt oyunun işi (`ISaveService.SaveGlobal`); suspend/quit değişen global'i zaten yazar.
- Framework modülü her sahneye kayıt eklemek isterse `ISceneScopeExtension` (root'a kaydedilir); sahne scope'u modülü bilmez.
- Sahne değişimi yalnız `ISceneLoader`: `Load(scene)` (state aynı), `Load<TState>(scene)` (state + sahne), `LoadAdditive`/
  `UnloadAdditive`. `SceneManager.LoadScene*`/`UnloadScene*` doğrudan çağrılmaz (validator; `[SceneLoadExempt]` istisna). State'ler
  sahne yüklemez. Token almaz; eski sahneden çağıran await sonrası kendine dokunmaz. Yükleme boyunca input modu koşulsuz None.
- Menüye girişte (`MenuState`) slot otomatik kapanır; kaydetmek isteyen load'dan önce `SaveSlot` çağırır. Sahne adları
  `Game/Scripts/Scenes.cs`'te. Intro build index 0, Boot Target `Manual` (sahne Boot'tan kendisi çıkar).
- Platform define'ının pozitif dalına serialize alan konmaz (`#if UNITY_EDITOR` dahil); build öncesi layout kontrolü yakalar.
- Build yalnız `AF/Build` (`Builder`); diğer yollar `BuildGuard`'da reddedilir. Player define'ı yalnız `BuildKinds.Defines`
  (`extraScriptingDefines`); PlayerSettings, compiler argümanı ya da `csc.rsp` define'ı build hatası, tek istisna Pipeline
  toggle'ının `ENABLE_RUNTIME_PIPELINE_DEV`'i. Release placeholder Company/Product ile reddedilir. Development build (Pipeline
  runtime + log) kimseye verilmez.
- Platform: oyun `IAchievements`/`IPlatformOverlay` kullanır; mağaza SDK tipleri yalnız backend asmdef'inde. Her backend her
  sözleşmeyi kaydeder (eksikse `UsePlatform` fırlatır); achievement id'leri `PlatformOptions.AchievementIds`'te. Overlay
  pause'u `UsePlatform` bağlar; doğruluk oyunun save'inde, yüklemede kazanılmış başarımlar yeniden `Unlock` edilir.
- `[SerializeReference]` kullanan tip taşınırken `[MovedFrom]` şart.
- Framework prefix'i (`AF`) yalnız constants dosyasında geçer; save key, PlayerPrefs key ve dosya adlarına girmez.

## Doğrulama
- Her değişiklikten sonra: `unity command console status` → 0 error, compile failure yok.
- Testler: `unity command test run`.
- Validation: `unity command script run --file .claude/scripts/Validate.cs --entry ClaudeValidate.All` (Edit Mode; build
  sahnelerini additive açıp kapatır).
- Build kontrolü: aynı dosyada `ClaudeValidate.Release` (~30 sn; template'te placeholder hatası beklenen, ağaç kirliyse
  commit'lenmemiş değişiklik uyarısı).
- Play: `.claude/scripts/PlayMode.cs` (`ClaudePlayMode.Enter` / `EnterPaused` / `Step` / `Exit`).

## Notlar
- `CLAUDE.md` ve `.claude/` commit'li; `.claude/settings.local.json`, kilit dosyaları, `prompts/` ve `notes/` yerel.
  Düzen `.claude/README.md`'de. HoldIt şimdilik private. Public'e geçmeden önce CLAUDE.md ve `.claude/` kişisel
  yol ve eski ürüne atıf açısından gözden geçirilir; `decisions/`'ın framework dosyalarındakiler template'te düzeltilip alınır.
- Kalıcı kararlar: `.claude/decisions/` (önce `README.md`, sonra yalnız ilgili modül dosyası). Tek seferlik prompt'lar
  `.claude/prompts/`, gerektiğinde iş notları `.claude/notes/<konu>/` (ikisi de yerel), tekrar kullanılan editor betikleri `.claude/scripts/`.
- Framework sistem dokümanı commit'li (`Assets/Framework/Documentation~/`, modül başına bir dosya). İnsan için, İngilizce yazılır.
- Commit'li dosyalara (CLAUDE.md dahil) kişisel yol ya da başka projelere referans yazma; kod ve dokümanlara AI iş akışı
  notu da yazma.
