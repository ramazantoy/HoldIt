# CLAUDE.md

HoldIt: Unity 6.3 LTS + URP ile portre bir Android oyunu. Yukarıdan bakışlı tuvalet telaşı: erkek ve kadın karakterler
(Thin/Normal/Fat) gelir, sabır barları dolar. Oyuncu parmağıyla karakterden kare kare yol çizer. Yanlış kapı, dar geçit
(Fat için) ya da dolu kare karakteri olduğu yerde durdurur. Doğru tuvalette karakter boş kabine girer ya da kapıda bekleyip kapıyı
tıkar. Bar dolarsa kaza olur, üç kazada oyun biter. Oyun mağazaya çıkmaz; amaç public GitHub portföyü ve template'in sahada
denenmesi. Framework template'ten gelir: DI context, GameManager, SceneManager, Save, Audio, Input, Pool, Settings,
Localization, Platform, Build. Framework kararları `.claude/decisions/`'ta (indeks `README.md`, modül başına dosya), oyunun
kararları `.claude/decisions/Game.md`'de.

**Durum:** 2026-11-02'de template'ten açıldı. Fork `a949bbd`: F1–F7 ve template doğrulaması; framework geçmişi
`.claude/decisions/README.md` → "Fazlar". M0 Editor'da bitti (2026-11-02); Android cihaz olmadığı için cihaz kısmı bekliyor
(Game.md → Açık → Cihaz testi). M0b da cihaz bulununca yapılır. M1 ve M1b (ortam kiti, toon shader) Editor'da bitti
(2026-11-03). Milestone'lar 2026-11-03'te yeniden sıralandı: yol çizimi M5'e gitti, eski M3–M5 birer öne kaydı
(Game.md → Karakterler ve yol). M2 (yol modeli + karakter yürür, placeholder) Editor'da bitti (2026-11-03). M3'te
çizimin input'u öne alındı, M5'e kare efekti kaldı (kullanıcı kararı). M3 (çizim, adım doğrulama, doluluk ve rezervasyon,
geri dönüş, balonlar, yürüme döngüsü) Editor'da bitti (2026-11-03). M4 (prototipteki gibi büyük kabinler, kabin turu ve
ışıkları, kabine yürüyüş, kapıda kuyruk; parçalar placeholder) Editor'da bitti (2026-11-03). Karakter modelleri ayrı
oturumda üretiliyor; gelince M2b. Kabin parçaları kitten gelecek (Game.md → Açık). Sıradaki iş M5. Milestone başlarken
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
  framework dosyaları ve bu dosyanın HoldIt dışı bölümleri. Bulunan sorun Game.md → "Template'e gidenler"e yazılır,
  template'te düzeltilir, sonra `git fetch template` + `git cherry-pick -x <sha>` ile alınır.
- **Faz = milestone:** template'in faz kuralları milestone'lar için de geçerli. Her milestone oynanabilir halde biter. Commit
  mesajı kısa, emir kipinde ve İngilizce olur.
- **Oyun kuralları:**
  - Kurallar (grid, yol, sabır, kabin, spawn) saf C# ile yazılır; MonoBehaviour yalnız view içindir.
  - Her kural sınıfının EditMode testi aynı milestone'da yazılır. Her test dosyasında kod bir kez bilerek bozulur; test yine
    geçiyorsa işe yaramıyordur.
  - Ayarlanabilir her sayı ScriptableObject config'de durur.
  - Özellikler birbirini interface ve MessagePipe mesajıyla tanır (aşağıdaki mimari kurallar).
  - Oynanış kodu yalnız `IPointerInput`'u bilir (Point/Press, `Gameplay` map'i); legacy `Input.*` kullanılmaz.
  - Dosyalar küçük ve tek amaçlı olur. Template API'si uydurulmaz; eksikse söylenir ve öneri yapılır.
  - Ekranlar ve HUD Canvas (uGUI) + TextMeshPro ile, her ekran bir prefab; tam ekranlar `ScreenView`'dan türer (fade,
    gizliyken raycast ve çizim yok). Sabır barı ve konuşma balonu dünyadaki nesnelerdir (Game.md → UI).
  - Repo public olacak: asset yalnız CC0 ya da kendi işin olur; font OFL olabilir, `OFL.txt` yanında durur. Ücretli paket
    (Asset Store, Synty) repoya girmez.

### Milestone'lar
| # | İçerik | Bitti sayılır |
|---|---|---|
| M0 | Namespace `HoldIt` geçişi, kimlik (Company/Product/identifier/Version), portre yön. Intro: splash videosu oynar, boot da bitince Menu'ye geçer. Oyun fontu (Baloo 2). Placeholder ana menü (UI Toolkit'le denendi, Canvas'a dönüldü): Play → Gameplay (şimdilik boş sahne), Settings (boş ekran), Quit (yalnız masaüstü); Gameplay'den Menu'ye dönüş. `IPointerInput`: Point/Press `Gameplay` map'inde, UI üstünde başlayan basış yok sayılır, Gameplay'de dokunuş logu. Android geri tuşu (Esc ↔ `Gameplay/Pause` + `UI/Cancel` kararı). Menü metinleri şimdilik mevcut Localization tablosuna eklenir | Editor'da Intro → Menu → Gameplay → Menu akışı; Game.md'deki "M0'da doğrulanacaklar" (UI Toolkit) cevaplanır. Telefonda Development build, geri tuşu ve `adb logcat`'teki dokunuşlar Android cihaz bulununca (Game.md → Açık → Cihaz testi) |
| M0b | Localization en + tr: örnek tablolar oyunun tablolarıyla değişir, Specific seçici belirlenir. "Yeni oyun kontrol listesi"nin kalan adımları ve "İlk mobil oyunda doğrulanacaklar"dan bu build'le bakılabilenler (ikisi de `decisions/README.md` → "Template doğrulaması") | Telefon diline göre en/tr açılır; kontrol listesi ve mobil doğrulama sonuçları decisions'a yazılır |
| M1 | Grid + harita asset'i + portre kamera sığdırma | Harita 1 farklı en-boy oranlarında doğru görünür |
| M1b | Ortam kiti (zemin, duvar, dar geçit, bina duvarı, kapı) kendi Blender kitimizden gelir; placeholder prefab'ların içi değişir (Game.md → Harita ve tahta → Parçalar) | Harita 1 gerçek parçalarla kurulur; M1'in en-boy kontrolleri yine geçer |
| M2 | Yol modeli (saf C#: çizim kuralları, geri alma, en kısa yol) + karakter modeli ve state machine (Idle, Walking) + placeholder karakter (SPEC hiyerarşisi, 6 prefab, havuz). Çizim yok; Editor/dev'de debug kaynağı (6 karakter + dokun → en kısa yolu yürü) | Editor'da bir karakter debug yolunu kare kare yürür, köşede döner, yol bitince Idle olur |
| M2b | Karakter modelleri gelince (zamanı modele bağlı): FBX import ayarları, 6 prefab'ın içi, UV renk/ifade varyantı (ofset hesabı saf C# + test), parça animasyonlarının gerçek modelle ayarı | 6 varyant gerçek modelle yürür; M2'nin kontrolleri yine geçer |
| M3 | Yol çizimi (input + geçici kare işaretleri) + adım doğrulama + doluluk ve rezervasyon + geri dönüş + balonlar + yürüme döngüsü. Debug: tıkla-yürü kalkar, girişten gelen karakterler eklenir | Editor'da karakter çizilen yolu yürür; yanlış kapı, dar geçit ve dolu kare çalışır; iki karakter aynı karede durmaz, ayrılmış kareye çizilemez |
| M4 | Tuvaletler, kabinler, kuyruk | Doğru giriş, meşgul ışıkları, kapı tıkama |
| M5 | Kare efektleri: M3'ün geçici işaretlerinin yerine oynanarak seçilen görsel (LineRenderer'dan vazgeçildi) | Cihazda çizilir ve geri alınır; efekt seçildi |
| M6 | Spawn, sabır, kaza, can, skor | Tam döngü oynanır, oyun sonu |
| M7 | Harita seçimi, ayarlar (ses, haptik, dil), en iyi skor kaydı | Tam oyun akışı |
| M8 | Harita 2 ve 3, tuning turu | Bütün haritalar baştan sona oynanır |
| M9 | Juice: PrimeTween animasyonları, sesler, haptik | Cihazda iyi hissettirir |
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
- **Köprü:** Unity CLI + `com.unity.pipeline`. Parametreler `--ad değer` biçiminde; `--result-only` yalnız sonucu verir.
  - Komut arama: `unity command --query <terim>`.
  - Durum: `unity command editor status` (`ready`, `compiling`, `domainReloadInProgress`).
  - Console: `unity command console status` (sayaçlar + `compilationFailed`), `unity command console get` (`--level warn`,
    `--tail N`; `--limit` yok). `get` geçmişi tutar: derleme düzeldikten sonra eski hata satırları `console clear`'a kadar kalır,
    güncel durum için `status`'un `groundTruth.consoleErrors`'ına bak (üstteki `counts` oturum boyu birikir, recompile'da
    sıfırlanmaz).
  - Testler: `unity command test run`, `unity command test status`. Tek assembly: `--filter AF.Save.Tests --filter_type assembly`.
  - Serialize alan okuma: `component get_fields --target <asset yolu | instanceId> --component <Tip> --field <ad>`. Git Bash
    `/` ile başlayan argümanı yola çevirir (`/SceneScope` → `C:/Program Files/Git/SceneScope`); sahne objesine instanceId ile
    (`scene get_hierarchy`) eriş.
  - Editor kodu: `unity command script run --file <yol.cs> --entry Tip.Metot`. Dosya proje dışında (scratchpad) durabilir;
    domain reload olmadan derlenip çalışır. Toplu sahne/asset/ayar işleri için yol bu. Argümanlar JSON dizisi:
    `--args '["Intro"]'` (`--args Intro` reddedilir).
  - Editor kapalıysa: `unity open <proje yolu>`.
- **CLI zaman aşımı alırsan önce modal diyalog ara.** HTTP sunucusu cevap verir ama main thread'deki komut bekler. Örnek: OpenUPM
  paketleri ilk kurulduğunda Unity 6.3 "Missing Signature" diyaloğu açar. Diyaloğu kullanıcı kapatır.
- Bekleme döngüsünde CLI istemcisini kendi `--timeout`'undan önce öldürme; yarıda kesilen istek console'a sahte pipeline hatası yazar.
- **Köprü yoksa** (Editor kapalı, bağlantı kopuk): `%LOCALAPPDATA%\Unity\Editor\Editor.log`. Değişiklikten önce byte boyutunu al,
  sonra yalnız o offset'ten sonrasını oku. Domain reload sonrası bağlantı koparsa bekle ve yeniden bağlan. Sonucu kullanıcıdan isteme.
- Editor script'lerinde `AssetDatabase.SaveAssets()` değil, hedef asset için `AssetDatabase.SaveAssetIfDirty(obj)`. `SaveAssets`,
  Unity'nin arka planda kirlettiği her asset'i de yazar ve diff'e beklenmedik dosyalar girer.
- **Unity YAML'ını (scene, prefab, asset) asla dosya olarak elle yazma.** CLI komutlarını ya da editor kodunu kullan. Elle yazılan
  YAML'da yanlış alan adı hata vermez, değer sessizce default'a düşer.
- Editor state'ini değiştirdiysen ne değiştiğini ve kaydedilmesi gerekip gerekmediğini söyle.
- Geçici script ve çıktıları proje köküne ya da `Assets/` altına bırakma.
- Editor odakta değilken dışarıdan yazılan dosyaları görmez: `script run` ile `AssetDatabase.Refresh()` çağır, sonra
  `editor status`'ta compiling/domainReload bitene kadar bekle (alan sırası: `domainReloadInProgress` `playMode`'dan önce gelir).
- Onaysız çalışsın diye unity komutları sade yazılır: `cd` öneki yok (cwd zaten proje), `python`'a pipe yok, elle bekleme
  döngüsü yok. Refresh + hazır olana kadar bekleme + console sayaçları: `bash .claude/scripts/wait-editor.sh --refresh`
  (yalnız beklemek için `--refresh`siz). Script `status: ready` bekler; Play'de durum `playing` olduğu için orada zaman aşımına
  düşer, Play'e girişi/çıkışı `editor status`'un `playMode` alanından izle.
- Editor odakta değilken Play'de player loop ilerlemez (`playerLoopTicking: false`); `UniTask.Yield`/thread pool dönüşü de
  bekler. `QueuePlayerLoopUpdate()` kare ilerletmedi (F2'de `Time.frameCount` sabit kaldı). Çalışan yol: `script run` ile
  `EditorApplication.isPaused = true; EditorApplication.Step();` (çağrı başına bir kare). Bitince Play'den çık ve
  `isPaused = false` yap, yoksa kullanıcının sonraki Play'i duraklamış başlar. Editor odaktaysa (F5'te öyleydi) loop ilerler;
  ilk karelere bakmak için Play'i duraklatılmış başlat (`isPaused = true`, sonra `isPlaying = true`) ve adımla.
- Play durdurma sırası (6000.3.27f2): `ExitingPlayMode` istek karesinde, Unity sonraki karede sahne yüklemesini reddeder
  (`LoadSceneAsync` null, kendi logu yok), `Application.quitting` ondan sonra. Play'i durdururken süren async iş için
  `quitting`'e güvenme.
- Run In Background kapalı (template): Unity dokümanına göre Editor odak kaybettiğinde Play'de de `OnApplicationPause(true)` →
  `AppSuspending(Background)`, Save değişen global/device'ı yazar (denenmedi). CLI'la sürülen odaksız Play'de beklenir.
- `script run` her çağrıda ayrı assembly derler; çağrılar arası static alan paylaşılmaz (gerekirse `AppDomain.SetData`).
- `EditorBuildSettings.scenes` ve `PlayerSettings` değişikliği `SaveAssetIfDirty` ile diske yazılmaz. Önce persistent dirty
  nesneleri listele (`Packages/` ve `Library/` yollarını ele); yalnız hedef ve gitignored olanlar varsa `SaveAssets` kabul
  edilir, sonra `git status` ile kontrol et. `PlayerSettings.SetApplicationIdentifier` `overrideDefaultApplicationIdentifier`'ı
  yeniden açar; kapatmak gerekirse ondan sonra `SerializedObject` ile.
- Inspector editor'ları hedefi yok olmuş halde kalırsa her domain reload'da `GameObjectInspector`/`TransformInspector`
  `OnEnable` hatası (3 error) basar. Çözüm: `script run` ile `Resources.FindObjectsOfTypeAll<Editor>()` içinde `target == null`
  olanları `DestroyImmediate`.
- **Build:** yalnız `AF.Build.Editor.Builder` (menü `AF/Build`). CLI: `.claude/scripts/BuildCli.cs` (`ClaudeBuild.Development`)
  + `script run --timeout 3600`, Bash arka planda (Windows IL2CPP ~12 dk; Editor bu sürede komut almaz). Pipeline `build run`
  ve Build Profiles penceresi `BuildGuard` ile reddedilir. Rapor `Builds/<Platform>/<Kind>-report.txt`. Release
  `ClaudeBuild.Release`; placeholder Company/Product ile reddedilir (HoldIt'in kimliği M0'da).
- Build URP'nin `PC_RPAsset` prefilter alanlarını ve global ayarlarını yeniden yazar (rapor "build changed project files"
  diye listeler); diff'e bak, kendiliğinden commit'e girmesin. Builder Addressables içeriğini kendisi kurar (~25 sn, rapora not);
  Addressables'ın "Build Addressables on Player Build" ayarı "Do not build" kalır (değilse build hatası). Localization ayar
  asset'i Preloaded Assets'te kalıcı durur (paket build'de ekleyip yalnız bellekte çıkarıyordu).
- **Dev build'e bağlanma:** `unity command --runtime-path Builds/Windows/Development <komut>`. IL2CPP player'da yalnız
  `console get/clear/status`, `runtime status/quit/simulate_key/simulate_pointer/set_time_scale` var; `script eval` derleyemez
  (referans assembly yok), capture/hierarchy yok. Doğrulama Player.log (`-logFile <scratchpad yolu>`) + dosya içeriğiyle;
  ekran için PowerShell `CopyFromScreen` ile player penceresi. `simulate_key` pencere odaktayken güvenilir.
- Editor ve Windows player aynı `persistentDataPath`'i kullanır (`%USERPROFILE%\AppData\LocalLow\<Company>\<Product>`);
  player testinden önce klasörü scratchpad'e yedekle, sonra geri koy.
- Editor-only asmdef'teki MonoBehaviour `AddComponent` ile eklenemez ("editor script"). Test bileşenleri
  `UNITY_INCLUDE_TESTS` kısıtlı runtime asmdef'te durur (`AF.Core.Tests.Fixtures`).
- VContainer, Editor'da oluşturulan ve adı `LifetimeScope.cs` ile biten script'in içeriğini kendi şablonuyla değiştirir; scope
  dosyalarına bu soneki verme.
- `test run` sonrası console'da `LogAssert.Expect`'li testlerden beklenen loglar kalır (template doğrulaması sonu: 16 error =
  1 AppBoot + 7 Save bozuk/eski/kilitli dosya senaryosu + 1 Save `Restore` fırlatan savable (exception) + 1 Input sapma denetimi
  + 1 Platform bilinmeyen başarım + 1 havuz tween sızıntısı + 2 Audio `AudioListener.pause`/`volume` dışarıdan değişti + 1 Audio
  klipsiz cue + 1 Localization kayıtlı dil yüklenemedi; 8 warning = 1 Save salt okunur + 1 Input yetim override + 4 Settings
  geçersiz ekran ayarı + 1 Settings bilinmeyen ses kanalı + 1 Localization projede olmayan dil). Tam EditMode koşusu 393 test.
- EditMode test koşusunda açık sahne kaydedilmemiş "Untitled": testte `EditorSceneManager.NewScene(Additive)` fırlatır.
  Sahne alan validator'ı, bileşen listesi alan internal bir metotla test et (`ScopeSetupValidator.CheckScene` deseni).
- Unity sarılmış exception'ı en içteki mesajla loglar: `LogAssert.Expect(LogType.Exception, ...)` regex'i iç mesaja yazılır.
  Console doğrulamasını testlerden önce yap ya da `console clear` sonrası tekrarla.
- Input System testleri `InputTestFixture`'dan türer (`Unity.InputSystem.TestFramework` derleniyor, `testables` gerekmez).
  Fixture'ın `runtime`'ı internal: native cihaz yaratılamaz, `AddDevice` cihazları `native == false`.
- Editor'da `AddComponent` ile eklenen `InputSystemUIInputModule` `Reset`'te Unity'nin DefaultInputActions'ını alır; aktif
  modüle action atamak Edit Mode'da da action'ları açar. Kurarken GameObject'i kapat, önce `UnassignActions()`, sonra asset ve
  referansları ata. `.inputactions`'ın her action için iki `InputActionReference` alt asset'i var: gizli olan eski uyumluluk
  kopyası (file id isimden), görünür olanı kullan (file id action id'sinden, rename'e dayanıklı).
- Yeni asmdef'e GUID ile referans vermek için önce asmdef'i yaz, refresh et, Unity'nin ürettiği `.meta`'dan GUID'i oku.
  `.meta`'yı elle yazma.
- Yeni `.cs` dosyası bazen derlemeye girmez (F7: refresh/`ImportAsset`/temiz derleme "up to date" dedi, dosya MonoScript ama
  assembly'de yok). Yeni dosyadan sonra `Library/Bee/artifacts/1900b0aEDbg.dag/<asmdef>.rsp`'deki `.cs` sayısını diskle
  karşılaştır; eksikse `script run` ile `AssetDatabase.MoveAsset(yol, yol_tmp)` + geri taşı, sonra refresh.
- PrimeTween Edit Mode'da `EditorApplication.update`'ten tick'lenir: tween davranışı `[UnityTest]` (yield) ile EditMode'da test
  edilir. Edit Mode'da `[ExecuteAlways]` olmayan bileşenin Awake/OnEnable/OnDestroy'u çalışmaz.
- Audio mixer'ın public oluşturma API'si yok (F6): `script run` + reflection ile `ProjectWindowCallback.DoCreateAudioMixer.Action`
  (menünün çağırdığı), grup `AudioMixerController.CreateNewGroup` + `AddChildToParent`, exposed parametre `AddExposedParameter`
  (`AudioGroupParameterPath(group, GetGUIDForVolume())`) + `exposedParameters` ile ad + `OnChangedExposedParameter`. Yeni mixer'da
  view yok, `AddGroupToCurrentView` patlar: `views`'a tüm grupları içeren "View" yazılır. Geri okuma `SerializedObject`.

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
- Save: `NewSlot` ve save-as dolu slotun üzerine yazar; oyuncuya sormak oyunun işi (`ListSlots`).
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
  Düzen `.claude/README.md`'de. HoldIt şimdilik private (2026-11-02). Public'e geçmeden önce CLAUDE.md ve `.claude/` kişisel
  yol ve eski ürüne atıf açısından gözden geçirilir; `decisions/`'ın framework dosyalarındakiler template'te düzeltilip alınır.
- Kalıcı kararlar: `.claude/decisions/` (önce `README.md`, sonra yalnız ilgili modül dosyası). Tek seferlik prompt'lar
  `.claude/prompts/`, gerektiğinde iş notları `.claude/notes/<konu>/` (ikisi de yerel), tekrar kullanılan editor betikleri `.claude/scripts/`.
- Framework sistem dokümanı commit'li (`Assets/Framework/Documentation~/`, modül başına bir dosya). İnsan için, İngilizce yazılır.
- Commit'li dosyalara (CLAUDE.md dahil) kişisel yol ya da başka projelere referans yazma; kod ve dokümanlara AI iş akışı
  notu da yazma.
