# Unity Editor ile çalışma

CLAUDE.md'den taşındı. Editor'da takılınca, CLI zaman aşımında ya da yeni tür bir Editor işinden önce okunur.
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

