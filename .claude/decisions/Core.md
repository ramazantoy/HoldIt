# Core: scope'lar, mesajlar, oyun akışı, boot, pause ve saat (F1); sahne yükleme ve intro (F5); havuz ve tween (F6)

Bu modülün kararları, doğrulananları ve inceleme bulguları; çapraz kararlar ve "Açık" listesi `README.md`'de.
Karar değişirse satır silinmez; eskisinin üstü çizilir, yenisi tarihle eklenir.

> Save, Input ve Audio modüllerinin kendi kararları ayrı dosyalarda; F5 ve F6 burada başlar.

## F1 kararları (2026-10-29)
| Konu | Karar | Gerekçe |
|---|---|---|
| "Değişti" mesajları | Normal `IPublisher/ISubscriber` (SignalBus karşılığı). Mesaj state servise yazıldıktan sonra atılır, yeni değeri taşır. Durumu yansıtan abone başlarken servisten bir kez okur. Buffered kullanılmaz | Struct mesajda buffered abone hiç publish olmadan `default(T)` alır (Doğrulananlar F1) |
| Pause | Ayrı `IPauseService`: `Request(reason)` handle döner, en az bir handle yaşadıkça pause; `PauseChanged` yalnız kenarda. GameManager'da Paused yok | Pause Playing'in üstüne biner (state olsa her pause'da Exit/Enter çalışır); çok kaynaklı istekte ilk resume diğerini ezmez |
| `Time.timeScale` | Tek yazan `IGameClock`: pause → 0, değilse `Scale`. Root dispose'da 1 | Pause ve slow-mo birbirini ezmez; resume slow-mo'yu korur |
| GameManager | `GoTo<T>()`: Exit → Enter (async, manager ömür token'ı) → `GameStateChanged{Previous, Current}`. Aynı state no-op; kayıtsız state ya da geçiş sırasında GoTo `InvalidOperationException`. Durumlar `IReadOnlyList<IGameState>`'ten tipe göre | Geçişler sahne yüklemeyi (F5) kapsayacağı için baştan async |
| Boot | `AppBoot` (`IAsyncStartable`) `IBootStep`'leri kayıt sırasıyla çalıştırır; hata bir kez loglanır, `WaitReady` aynı exception'ı verir. Boot'tan çıkışı ilk sahnenin `SceneScope._bootTarget`'ı (Menu/Playing) belirler; sonraki sahneler dokunmaz | Root senkron kurulur; async iş ayrı kapı ister. Her sahneden Play |
| Scope'lar | `RootScope` (abstract, sealed Configure → `ConfigureGame`) ve `SceneScope` (sealed Configure → `ConfigureScene`). İkisi de exception handler'ı `RegisterEntryPoint`'ten önce kaydeder ve `EntryPointAudit` bağlar (Editor + dev build) | Çekirdek kayıtlar ve denetimler atlanamaz |
| Sahne bileşeni kaydı | Scope'ta serialize alan + `RegisterComponent`, ya da `autoInjectGameObjects`. `RegisterComponentInHierarchy` yok. Validator serialize alanı kayıt sayar; alanın kodda gerçekten kaydedildiğini scope build'inde `ScopeFieldAudit` denetler (Editor + dev build, `builder.Exists`, tip bazlı; autoInject altındakiler atlanır) (2026-10-30) | Validator kaydı statik görebilsin; kodu göremediği kısmı build anı kapatır |
| MessagePipe handler hatası | Filtre yok. Senkron handler hatası publisher'a yükselir (fail-fast, loglanır); async handler hatası UniTask unobserved → Log | Global filtre runtime `MakeGenericType` ister (struct + IL2CPP); tip başına filtre scope sınırında çözülmüyor |
| Validation | `AF/Validate All` (proje + etkin build sahneleri) ve Play'e girerken açık sahneler (engellemez, sonucu "Clear on Play"den sonra loglar). Validator'lar: scope kurulumu, kayıtsız `[Inject]`, eksik script / Missing referans / boş `[Required]`. Yeni kontrol = `IValidator` | En pahalı bug'lar "hata yok ama çalışmıyor" tipindeydi |
| Test bileşenleri | MonoBehaviour fixture'ları `AF.Core.Tests.Fixtures` (runtime, `UNITY_INCLUDE_TESTS`), `[AddComponentMenu("")]` ile menüden gizli. `FieldScope` scope'ların Parent listesinde görünür (VContainer `TypeCache` ile dolduruyor), kozmetik, kabul (2026-10-30) | Editor asmdef'teki MonoBehaviour `AddComponent` ile eklenemez |

## Doğrulananlar (F1, kaynaktan ve Editor'da)
| Konu | Sonuç → kural |
|---|---|
| MessagePipe buffered | `BufferedMessageBrokerCore.Subscribe`: `if (IsValueType \|\| lastMessage != null) handler.Handle(lastMessage)` → struct'ta publish olmadan `default(T)`. `RegisterMessageBroker<T>` buffered'ı da kaydeder; kullanılmaz |
| MessagePipe publish | Handler'lar try/catch'siz döngüde; bir handler'ın exception'ı kalanları atlar ve publisher'a yükselir. Global open-generic filtre `MakeGenericType(messageType)` ile kapanır |
| MessagePipe child scope | Options root'ta `RegisterMessagePipe`'tan; child scope `Parent.Container.Resolve<MessagePipeOptions>()` ile alır (çalışıyor) |
| VContainer root | `VContainerSettings.OnEnable` (BeforeSceneLoad) → ilk sahne yüklenince ya da ilk scope Awake'inde root bir kez instantiate + DontDestroyOnLoad. Editor'da doğrulandı: Menu'den Play'de 1 root, Gameplay yüklenince aynı instance, ikinci build yok |
| VContainer tick sırası | `VContainerUpdate`, `ScriptRunBehaviourUpdate`'ten önce → `IGameClock` MonoBehaviour.Update'ten önce ilerler |
| VContainer exception handler | `EnsureDispatcherRegistered` scope'ta handler yoksa `Debug.LogException` handler'ı kaydeder; child parent'ınkine bakmaz → handler her scope'ta, `RegisterEntryPoint`'ten önce |
| VContainer `RegisterComponent<TInterface>` | Force-inject için `Resolve<TInterface>`; `MonoBehaviour` tipli değişkenle döngüde çağrılırsa hepsi `MonoBehaviour` olarak kaydedilir, yalnız sonuncusu inject edilir → somut tipli serialize alanla çağır |
| VContainer boş koleksiyon | `IReadOnlyList<T>` kayıt yokken boş liste döner (`TryFallbackToSingleElementCollection`) |
| VContainer `RegistrationBuilder` | `ImplementationType`/`InterfaceTypes` `protected internal` → `EntryPointAudit` reflection kullanır; alan adı değişirse `EntryPointAuditTests` kırılır, runtime'da audit hata loglayıp atlar, oyunu durdurmaz (2026-10-30) |
| VContainer script şablonu | Editor'da oluşturulan `*LifetimeScope.cs` içeriği şablonla değişir (`ScriptTemplateProcessor`) |
| Unity 6.3 | Editor-only asmdef'teki MonoBehaviour `AddComponent` ile eklenmez. `EditorBuildSettings` `SaveAssetIfDirty` ile diske yazılmaz. Odakta olmayan Editor Play'de player loop'u ilerletmez |

## F5 kararları (2026-10-31)
Sahne değişimi ve intro. Kapsam: Core (`Scenes/`, GameManager, scope'lar, AppBoot), Save (`SaveLifecycle`), Input (mod servisi),
Core.Editor (`DirectCallScan`, `DirectSceneLoadValidator`), Game (`Scenes.cs`, `Intro/`, `Intro.unity`), build sahne listesi.
| Konu | Karar | Gerekçe |
|---|---|---|
| Sahneyi kim yükler | `ISceneLoader`/`SceneLoader` (Core, `Core/Runtime/Scenes/`, yeni asmdef yok). State değiştiren yükleme `Load<TState>(scene)`: loader içeride `GoTo` çağırır. State'ler sahne yüklemez. Ad `SceneManager` değil | `PlayingState.Enter` hangi seviyeyi yükleyeceğini bilemez (payload yok); seviye → seviye geçişi state değiştirmez. F1'de GoTo'yu async yapan gerekçe (Enter'da yükleme) düştü, GoTo async kalır (oyunun state'leri). `UnityEngine.SceneManagement.SceneManager` ile çakışmasın |
| `Load` sırası | `IsLoading` + `SceneLoadingChanged(true)` → loading screen `Show` → (`Load<TState>`) `GoTo` → eski sahneler kaldırılır (boş geçici sahne aktif, hepsi en yeniden başlayarak `UnloadSceneAsync` ile beklenir) → hedef `LoadSceneAsync(Single)` → `Hide` → `IsLoading=false` + mesaj | Exit eski sahneyi görür, yeni sahnenin entry point'leri yeni state'i görür; dev oturumu yeni sahnenin savable'larından önce açılır; menüye dönüşte `CloseSlot` unload'dan önce (kapanmış slota capture yok). Unload-önce sırası Unity sürümünden bağımsız: aynı sahnenin reload'unda çift key yok. Single son adım: yeni sahne ilk `Awake`'inden aktif. Additive çocuklar ana sahneden önce (inceleme) |
| Input | Yükleme boyunca mod koşulsuz `None` (`InputModeService._loading`; `SceneLoadingChanged` + `Start`'ta `IsLoading`). Bu arada gelen istekler (yeni sahnenin `Start`'ı, overlay pause) sırasını korur, bitince uygulanır | Core Input'u bilemez. Bayrak GameStateChanged'den sonra iner: eski state'in modu araya girmez (Play: `menu -> none -> gameplay`). İlk sürüm sıradan bir istekti; yeni sahnenin `Start`'taki isteği loading ekranı altında map açıyordu (inceleme) |
| Menüye dönüş | `SaveLifecycle`: `GameStateChanged.Current is MenuState` ve oturum varken `CloseSlot`; `LoadSlot` sürüyorsa `Log.Error`, slot açık kalır. `SaveSlot` oyunun kararı, load'dan önce | Unutulan `CloseSlot`, "kaydetmeden çık"ılan ilerlemeyi suspend autosave'iyle yazardı. Handler'dan fırlatmak yayını diğer abonelerden (input) önce keserdi (inceleme) |
| GoTo hata semantiği | Exit fırlatırsa state değişmez, yayın yok; Enter fırlatırsa `Current = next`, GameStateChanged yine yayınlanır, exception çağırana. Ömür token'ı iptalinde yayın yok. Dispose idempotent | Aboneler `Current` ile uyumlu, hata kaybolmaz; önceki state zaten çıktı, geri alma anlamsız |
| İptal logu | Root ve sahne scope handler'ı ortak `EntryPointErrors.Report`: token'ı iptal edilmiş OCE loglanmaz, token'sızı loglanır. `AppBoot` iptalde `TrySetCanceled(cancellation)` | "Yardımcılar OCE yutmaz" korunur: yutma yalnız tepede, yalnız gerçek iptalde |
| Play durdurma / quit | Loader Editor'da `ExitingPlayMode`'da (`#if UNITY_EDITOR`, serialize alan yok), build'de `AppSuspending(Quit)`'te kendi ömür token'ını iptal eder. Unity'nin reddi (`AsyncOperation` null) iptal edilmiş token'la OCE olur; bitiş mesajı yayınlanmaz | Play'de ölçüldü: Unity yüklemeyi `ExitingPlayMode`'dan sonraki karede reddediyor, `Application.quitting` bir kare sonra. Yalnız quit'e bağlanan ilk düzeltme hatayı gidermedi |
| Token | `Load*` token almaz, loader'ın ömür token'ı | Eski sahnedeki butonun `destroyCancellationToken`'ı tam unload'da iptal olurdu. GoTo ile aynı |
| Eşzamanlılık / hata | Tek işlem; çakışma `InvalidOperationException`, build'de olmayan sahne `ArgumentException` (çağrıda, hiçbir şey başlamadan). Unload'dan önceki hata (Show, state değişimi, kayıtsız state): ekran geri kalkar. Sonraki hata: ekran kalır. İkisinde de input geri gelir, exception çağırana; `Current` gerçek aktif sahne | Eski sahneler duruyorsa tutarlı duruma dönülür (inceleme); gittiyse yarım sahneyi açmak yerine görünür takılma, yeni `Load` toparlar |
| Loading screen | `ILoadingScreen {Show(ct), SetProgress(float), Hide(ct)}`, root prefab bileşeni, `RegisterComponent(field).As<ILoadingScreen>()`; 0 → yok, >1 → kurulum hatası; erişim `IsAlive()`. İlerleme `progress / 0.9` | Template'te UI yok, yalnız sözleşme; root'ta yaşar |
| Additive | `LoadAdditive`/`UnloadAdditive`: loading screen ve input bloğu yok; scope parent'ı ana sahnenin `SceneScope`'u (`EnqueueParent`); `SceneEntry` yalnız parent root ise kaydedilir; ana sahne `UnloadAdditive` ile kaldırılamaz | HUD/alt bölge seviyenin servislerini inject eder; serialize parent boş kalır (validator kuralı). Additive sahnenin Boot Target'ı Boot sırasında da yok sayılır (inceleme) |
| `BootTarget.Manual` | Enum sonuna (Menu=0, Playing=1 korunur); `SceneEntry` hiçbir şey yapmaz | Intro Boot'tan kendisi çıkar |
| Intro | `Assets/Game/Scenes/Intro.unity` build index 0 (Intro, Menu, Gameplay): siyah kamera + `IntroScope` (Manual, `_minimumSeconds` 2). `IntroFlow`: `WhenAll(unscaled delay, boot.WaitReady)` → `Load<MenuState>(Scenes.Menu)`; boot hatasında intro'da kalır. Sahne adları `Game/Scripts/Scenes.cs`. Başka sahneden Play intro'yu atlar (ek kod yok) | Boş örnek; gerçek oyun logo/video koyar |
| Doğrudan yükleme | `DirectSceneLoadValidator`: proje IL'inde `SceneManager.LoadScene/LoadSceneAsync/UnloadScene/UnloadSceneAsync` hata; `[SceneLoadExempt]` muaf (`UnitySceneOperations` taşır; AF.Core hariç tutulmaz). IL tarayıcısı Input'tan Core.Editor'a `DirectCallScan` olarak taşındı, Input onu kullanır | "Hata sessiz kalmaz"; F3 tekniği |
| Reload ve savable | Davranış değişmedi, dokümanda: reload unload'da capture edilen state'le döner; son kayda dönüş `LoadSlot(ActiveSlot)` + `Load` | F2 modeli |
| PlayTime | `IsLoading`'de sayılmaz | Gerçek oynama süresi |
| Log (Info) | `Scene scope built/disposed: <sahne>`, `Scene loaded/added/removed`, `Game state: A -> B (Enter failed)`, `Save: slot closed on returning to the menu` | Sıra Editor console'undan ve Player.log'dan okunur |

## Doğrulananlar (F5, Editor 6000.3.27f2'de ve Windows IL2CPP dev build'inde)
| Konu | Sonuç → kural |
|---|---|
| Testler | 268/268 (+26; sonrası yine 10 error + 2 warning, yeni beklenen log yok); ValidateAll 0; `Builder.Check(Development)` 0 error (placeholder uyarısı) |
| Intro'dan Play | `Scene scope built: Intro` → boot → `BootState -> MenuState` → `disposed: Intro` → `built: Menu` → `Scene loaded: Menu` → `none -> menu`. Boot 2 sn'den uzun sürdü, intro onu bekledi; Play duraklatılmış başlatılıp adımlanınca boot hazırken intro 2 sn dolmadan geçmedi |
| Menu/Gameplay'den Play | Intro yok; Menu → MenuState, Gameplay → PlayingState + dev oturumu |
| Akışlar | `NewSlot` + `Load<PlayingState>`: başlangıçta `loading True, input none`, bitince `gameplay`, slot açık. `Load(Gameplay)` reload: `disposed` `built`'ten önce. `Load<MenuState>`: `slot closed`, sonra unload. Additive Menu'nün scope parent'ı Gameplay `SceneScope`'u; menüye dönüşte additive Menu, Gameplay'den önce dispose |
| Unity'nin kendi Single sırası (6000.3.27f2) | `LoadSceneAsync(Single)` ve senkron `LoadScene` aynı sahneyi yeniden yüklerken eski scope yeni scope kurulmadan dispose → bu sürümde doğrudan yükleme de çift key vermez; loader yine kendi sırasını uygular |
| Play'i durdurma | `ExitingPlayMode` istek karesinde gelir, Unity sonraki karede `LoadSceneAsync`'i null döndürür (kendi logu yok), `Application.quitting` ondan sonra. Düzeltmeden sonra yükleme ortasında ve intro beklerken durdurma: 0 error |
| Dev build (IL2CPP, 11:35) | Player.log'da Intro index 0, aynı sıra; `SceneLoadingChanged` struct broker'ı hatasız; exception/AOT yok; quit'te Menu scope temiz dispose; kayıt klasörü değişmedi (değişmeyen veri yazılmaz). Build inceleme düzeltmelerinden önce alındı; düzeltmeler Editor'da doğrulandı |
| VContainer `EnqueueParent` | Statik stack'e push, `Peek` ile okunur, `using` bitince pop: yükleme süresince parent'sız her scope'a uygulanır |

## F5 incelemesi (bağımsız, git diff üzerinden, 2026-10-31)
| Bulgu | Düzeltme |
|---|---|
| `UnloadAll` sahneleri yükleme sırasıyla kaldırıyor: ana sahne önce, additive çocuklar en az bir kare dispose edilmiş parent'ın servisleriyle çalışıyor | Ters sırayla (en yeni önce); Play'de additive Menu Gameplay'den önce dispose |
| Yükleme `None`'u sıradan istek: yeni sahnenin `Start`'ındaki istek loading ekranı altında map açıyor; dokümanlar "input bloklu" diyor | Mod servisinde `_loading` bayrağı mutlak; istekler sırasıyla bekler. Test: istek + pause yüklemede None, bitince sırayla |
| `_loadingScreen` interface alanında `!= null` / `?.` (kural: `IsAlive()`) | `IsAlive()` |
| Unload'dan önceki hata (Exit fırlatır, kayıtsız state, Show sırasında GoTo) ekranı sonsuza dek açık bırakıyor | Unload başlamadıysa (ve iptal değilse) `Hide`, sonra yeniden fırlat. Test: kayıtsız state |
| `EnqueueParent` tek seferlik değil; yorum yanlış | Yorum düzeltildi, Core.md'de uyarı, "Açık"a not |
| `CloseSlot` `LoadSlot` sürerken handler'da fırlatıyor, GameStateChanged yayınını kesiyor | `SaveService.IsLoadingSlot` (internal) ile korunur, `Log.Error`. Test yazılmadı: `MemoryStorage` senkron, yükleme yarıda tutulamıyor |
| Eksik testler | Kayıtsız state ve additive hata testleri eklendi. PlayTime ve "kapalı slota capture yok" testsiz (ilki "Açık"ta); Unity tarafı Play'de doğrulandı ("Doğrulananlar (F5)") |
| Additive sahnenin Boot Target'ı Boot sırasında yok sayılmıyor (intro sırasında additive → `GoTo<PlayingState>`); Manual sahne uyarısız Boot'ta kalabilir | `SceneEntry` yalnız parent root ise kaydedilir. Manual uyarısı eklenmedi, "Açık"ta |

## F6 kararları (2026-10-31)
Pool, tween yaşam döngüsü, Audio. Kapsam: Core (`Pooling/`, `Tweens/`, `EntryPointAudit`, `RootScope`, `ScopeFieldAudit`),
Core.Editor (`DirectCallScan`, `AwaitedTweenValidator`, `DirectInstantiateValidator`, `IValidator.ValidateAssets`), yeni Audio
modülü, Game (`AudioChannels.cs`, `GameRootScope`, `GameMixer.mixer`, üç sahne), `AF.Core` → PrimeTween referansı.
| Konu | Karar | Gerekçe |
|---|---|---|
| Modül yeri | Pool ve tween yardımcısı Core'da (yeni asmdef yok); Audio ayrı modül `AF.Audio` (+Editor, Tests), ~~`builder.UseAudio(mixer, options)`~~ (2026-11-01: F7'de `UseAudio(mixer, messagePipe, options)` → Settings.md "F7 kararları") | Pool'un yeni dış bağımlılığı yok, Audio onu kullanıyor; PrimeTween kilitli stack'te. Audio'nun kendi kurulumu ve validator'ı var |
| Havuz sahibi | `IPools` root'ta bir kez `Lifetime.Scoped`: her scope kendi instance'ını kendi resolver'ıyla kurar, scope'la dispose olur. `For(prefab)` scope'ta prefab başına tek havuz. Konteyner: scope GameObject'i altında kapalı `[Pools]/<Prefab>` | Root'tan tek fabrika sahne prefab'larına sahne servisini inject edemezdi. Sahne kaydı/uzantı gerekmez; yanlış scope'un havuzu alınamaz |
| Sahne kapanınca | Sahne havuzu dispose'da spawn'lılar dahil (DDOL'a taşınmış olsa da) her instance'ı yok eder. Root instance'ı başka sahnede ölürse havuz bırakır + `Log.Warning` (quit'te yok) | Tek kural: sahnenin şeyi sahneyle gider; root sızıntısı sessiz kalmasın |
| `ObjectPool<T>` | Kalır (kilitli karar): havuzdaki yığın + `maxSize` (10000). Spawn'lılar ayrı sözlük (referans karşılaştırıcısı: yok edilmiş Unity objeleri birbirine eşit), yok edilmiş eleman `Get`'te atlanır, çift despawn kendi kontrolümüz (her build). Prefab adı ve transform'u kurulumda saklanır | Unity'nin havuzu eleman çıkaramaz ve yalnız havuzdakileri görür; eksik sarmalayıcıda kapanıyor. Play'de prefab olarak verilen sahne objesi yok olunca uyarı `_prefab.name`'de patlıyordu |
| ~~Pool prefab'ının kökü kapalı~~ → havuz instance'ı kapalı tutar | Kopya kapalı konteynerin altında (`Object.Instantiate` + `InjectGameObject`, `[InstantiateExempt]`): Construct Awake'ten önce, Awake/OnEnable ilk Spawn'da. Prefab kökünün durumu önemsiz | Eski tuzağın düzeltmesi "havuz `SetActive(false)`'u kendisi zorlar" idi; kapalı kök prefab'ı düzenlemeyi zorlaştırıyor. VContainer'ın `Instantiate`'i prefab asset'ini açıp kapatıyor |
| Yaşam | `PoolLife` (token + nesil; `IsCurrent`, eski yaşamda no-op `Despawn`), `IPoolable.OnSpawned(life)/OnDespawned()` Unity aktivasyon callback'lerinin dışında. Despawn: token iptal → `OnDespawned` → `SetActive(false)` → konteyner. Spawn: konum → yeni yaşam → `SetActive(true)` → `OnSpawned`. Awake/OnEnable'dan istenen despawn aktivasyon bitince | Token önce: await edilen tween OCE alır. `OnEnable` içinde çocuk `SetActive` Unity'de düşüyor; reset ayrı kancada. Nesil, dışarıdan tutulan referansı korur (eski one-shot handle tuzağı) |
| Havuza dönüşte tween | Mekanizma yaşam token'ı (PrimeTween sonraki update'inde, değer yazmadan önce durdurur; await sonrası kod `life.IsCurrent`'a bakar). `TweenLeakAudit` (Editor + dev): spawn'da önceki despawn'dan ≥2 kare ve ≥1 fixed step geçmişse hiyerarşide canlı tween → `Log.Error` | `StopAll(target)` tam referans ve Sequence içini durdurmuyor, hata logluyor. Kare/fixed koşulu bağlı tween'lerin henüz işlenmediği yanlış pozitifi önler; pause'da kendiliğinden atlanır |
| Await edilen tween (F0 kararı değişti) | Core `ToUniTask(ct)` (Tween/Sequence): ölü tween + iptal → OCE, `SetCancellationToken(ct, Stop)`, await, sonra yine `ThrowIfCancellationRequested`. `AwaitedTweenValidator`: `GetAwaiter`, parametresiz `ToUniTask()`, UniTask'e dönüşüm, `ToYieldInstruction`, Tween/Sequence box'lanması (`yield return`, IEnumerator `WithCancellation`) hata; `[TweenAwaitExempt]` | PrimeTween'in await'i `Stop()` sonrası ve önceden iptal/geri dönüşümde normal döner. `Complete` modu iptalde bitiş değerini yazar (açıkça Stop). Box tespiti için `DirectCallScan` box operand'ını okur |
| Sahiplik / unscaled | Validator yok; Core.md'de kural ve örnek. Audio fade'leri kendisi uygular | Statik olarak bilinemez |
| Instantiate (Açık notu kapandı) | `DirectInstantiateValidator`: GameObject/Component `Object.Instantiate`/`InstantiateAsync` (genel `Object` overload'ları ve `T : Object` generic parametresi muhafazakâr), `[Inject]`'li `AddComponent<T>`. Material/ScriptableObject serbest. `[InstantiateExempt]` | Hangi prefab'ın verildiği statik bilinemez; tam yakalama tek kapıyla (sahne yükleme, input deseni) |
| Doğrulama maliyeti | `DirectCallScan` metot IL'ini domain ömrü boyunca önbelleğe alır. `IValidator.ValidateAssets` (varsayılan boş): ağır asset taraması yalnız Validate All ve build kontrolünde | Play'e her basışta `ValidateProject` çalışıyor; dört IL validator'ı tek okuma |
| Scoped entry point | `EntryPointAudit`: Scoped kayıt entry point arayüzüyle kayıtlıysa hata | VContainer entry point'leri kaydeden scope'ta çalıştırır; child'ın instance'ı sessizce çalışmazdı |
| `ScopeFieldAudit` | Sahnede olmayan (prefab asset) bileşen tutan alan atlanır (inceleme) | Havuz prefab'ı scope alanında durur; "RegisterComponent" önerisi prefab asset'ini inject ettirirdi |

## Doğrulananlar (F6, kaynaktan, Editor'da ve Windows IL2CPP dev build'inde)
| Konu | Sonuç → kural |
|---|---|
| VContainer 1.19 Scoped | Root'taki Scoped kayıt child'dan resolve edilince child'da ayrı instance, child'ın resolver'ı ve `ApplicationOrigin`'i; child dispose'da dispose. Root dispose'da Scoped'lar singleton'lardan önce. Entry point'ler `ContainerLocal` |
| PrimeTween 1.5.2 | `StopAll(target)` tam referans, Sequence içinde hata; kapalı objedeki tween sürer; token iptali sonraki update'te değer yazmadan önce, pause'da da; FixedUpdate tween'leri FixedUpdate'te (timeScale 0'da Update'te). Edit Mode'da `EditorApplication.update`'ten tick'lenir → tween testleri `[UnityTest]` |
| Testler | 327/327 (+59). Sonrası 13 error + 2 warning (yeni beklenen: havuz tween sızıntısı, AudioListener dışarıdan değişti, klipsiz cue). ValidateAll 0, `Builder.Check(Development)` 0 error (placeholder uyarısı) |
| Play (Editor) | Intro'dan Play: tek listener `RootScope(Clone)/Audio/Listener`, 8 voice prewarm. Pause'da başlatılan ses pause boyunca havuza dönmedi, resume'da çaldı ve bitti; `PlayWhilePaused` ses pause'da bitti. Eski handle yeni yaşamı durdurmadı; fade'le stop havuza döndü. Ses 0.5 → `MusicVolume` -6.02 dB. Müzik crossfade sonunda eski kaynak susturuldu. `Load<PlayingState>(Gameplay)` kare kare: her karede 1 listener, Menu'nün loop'u unload'da durdu, root loop'u ve müzik sürdü. `Load(Gameplay)` reload: sahne havuzu instance'ları (DDOL'a taşınan dahil) yok, root instance'ı sahneyle öldü → sayaç 0 + uyarı. Token'a bağlı tween despawn'dan sonra yazmadı, bağlanmamış tween spawn'da hata verdi. `IGlobalSounds` sahneden root'unkini verdi. Play durdurma 0 error |
| Dev build (IL2CPP, 10:07) | Player.log: `Audio: mixer GameMixer, channels Master, Music, SFX, UI, 8 voices` (generic `PrefabPool<SoundVoice>`, Scoped resolve, `InjectGameObject`), Intro → Menu aynı sıra, exception/AOT yok; pencere odağı değişince `Audio muted: unfocused` / `Audio unmuted`; quit'te Menu scope temiz dispose; kayıt klasörü değişmedi. IL2CPP stack'inde `VoicePlayer:get_Count` görünen satır özdeş metot birleştirmesi (Info logunun stack'i). Build inceleme düzeltmelerinden önce alındı; düzeltmeler Editor'da doğrulandı |

## F6 incelemesi (bağımsız, git diff üzerinden, 2026-10-31)
| Bulgu | Düzeltme |
|---|---|
| Despawn tween'i hemen durdurmuyor (PrimeTween sonraki update'te); havuz LIFO, aynı instance aynı karede yeniden spawn olabiliyor; yorum ve doküman "hemen" diyordu | Yorum, Core.md ve CLAUDE.md: değer yazılmaz, ama await sonrası kod (`catch`/`finally` dahil) `life.IsCurrent`'a bakar. Test: aynı karede yeniden spawn'da eski yaşamın tween'i yeni yaşamı oynatmıyor |
| `OnSpawned`'da despawn eden kancadan sonra diğer kancalar ölü yaşamla `OnSpawned` alıyordu | Döngüde her kanca öncesi `life.IsCurrent`; test |
| Awake/OnEnable'dan despawn: Unity aktivasyon sırasında `SetActive(false)`/`SetParent`'ı reddediyor, instance aktif kalıp havuza giriyordu | `Activating` bayrağı, istek aktivasyon bitince uygulanır, `OnSpawned` çağrılmaz; test. OnDisable'dan despawn dokümanda yasak ("Açık") |
| decisions.md F0 satırı eski | Üstü çizildi, bu bölüm |
| `ScopeFieldAudit` prefab asset tutan alanı "kayıtsız" sayıyordu | Sahnede olmayan bileşen atlanır; test (geçici prefab asset) |
| Nit'ler: voice'lar DDOL kökünde; Core.md'de çift token, sahne token'ı, aktivasyon notu; yanlış yerde XML özeti; box mesajı; SoundCue taraması her Play'de; `For` farklı `T` | Voice'lar `Audio` altında; dokümanlar düzeltildi; özet taşındı; mesaj "boxing"i de söylüyor; cue taraması `ValidateAssets`'te; `For` dokümanı. `TweenLeakAudit` maliyeti ve farklı `T` "Açık"ta |
| Performance Testing paketinin build'de yazdığı `Assets/Resources/PerformanceTestRun*.json` | Build sonunda silinmiş, diff'te yok |

## F1 girdileri (kapandı, 2026-10-29 → "F1 kararları")
Kapsam: `Packages/com.af…` paket iskeleti ve asmdef'leri (AF.Core, AF.Core.Editor, EditMode tests; Use GUIDs) · constants ·
Log (+UniTask unobserved / MessagePipe handler hata kancası) · `IsAlive()` · validation altyapısı + "AF/Validate All" (sahne scope'u var mı,
kayıtsız `[Inject]` bileşeni, yanlış kaydedilmiş entry point) · root LifetimeScope prefab + VContainerSettings, sahne scope tabanı,
MessagePipe kayıt düzeni · AppBoot "hazır" kapısı · GameManager (Boot/Menu/Playing/Paused state sınıfları) · pause'u bilen oyun saati · ilk EditMode testleri.
Bitti ölçütü: iki farklı sahneden Play'de root bir kez kurulur; kasıtlı eksik ref / kayıtsız `[Inject]` Validate All'da hata; `unity command test run` yeşil.
Konuşulacak: (a) "değişti" tipi state mesajlarında buffered abonelik kural mı? (b) oyun katmanının klasör/asmdef adı (VContainerSettings ve root
prefab orada) (c) pause GameManager state'i mi, ayrı servis mi? (d) paket kimliği teyidi.
Riskler listesi ve F0 planı: `~/.claude/plans/hepsini-tekte-yapmak-zorunda-buzzing-steele.md`.

## Template doğrulaması (2026-11-01)
Kod ↔ karar ↔ doküman taraması (bağımsız ajan, bulgular kodda doğrulandı); özet ve temel çizgi `README.md` → "Template doğrulaması".
| Bulgu | Düzeltme |
|---|---|
| `DirectInstantiateValidator` yalnız generic `AddComponent<T>`'yi yakalıyordu; `AddComponent(typeof(X))` geçiyordu (Core.md "her AddComponent") | Generic olmayan `AddComponent` muhafazakâr raporlanır (düz `Object` Instantiate'le aynı politika); test |
| `ILoadingScreen` yorumu await edilen tween'e `SetCancellationToken` diyordu: buna uyan kod `AwaitedTweenValidator` hatası alır | `ToUniTask(cancellation)` |
| Run In Background kapalıyken Editor'da Play ve pencereli masaüstü player odak kaybında `OnApplicationPause(true)` alır (Unity dokümanı; Editor kısmı bağımsız incelemede) → `AppSuspending(Background)`; dokümanlar "mobil" diyordu | Davranış kalır (Save yalnız değişen veriyi yazar, quit'le aynı anlam); `SuspendReason.Background` yorumu, Core.md, Save.md. Player'da denenmedi ("Açık") |
| `ScopeSetupValidator`'ın hiç testi yoktu | `CheckSettings` (VContainerSettings listesi) ve `CheckScene` (scope listeleri) ayrıldı, testler. Sahnede RootScope vakası testsiz: fixture'larda somut RootScope yok (bellekte `GameRootScope` ile denendi) |
| `ReferenceValidator` Missing referansı ve `DirectSceneLoadValidator` `UnloadScene` testsizdi | Testler (`UnloadScene` obsolete, `#pragma warning disable CS0618`) |
| Core.md: `UnloadScene` listede yok; quit sırasında yükleme "hatasız" değil (await OCE alır, loglanmaz); ScopeSetupValidator satırında iki hata eksik; Play validation yalnız error loglar; "her scope" → RootScope/SceneScope; havuz prefab + bileşen tipi başına; bekleyen despawn'a ikinci despawn sessiz; "nasıl yapılır" yok (yeni sahne, `LifetimeScope` soneki, oyun kodunun yeri) | Doküman |
| Kabul (değişiklik yok) | Dispose sonrası `GoTo` `ObjectDisposedException` atar (`SceneLoader` da öyle); test için internal ikinci ctor (`GameClock`, `SceneLoader`, `Pools`); `InternalsVisibleTo("AF.…")`; `Pools.cs:26` Unity objesinde `?.` (scope build anında canlı); tek abonelikli serviste `IDisposable` alanı; `RegistrationAudit.FindMissingContract` çağıransız ama Core.md'de modüllere sunulan yardımcı |

### Doğrulananlar (template doğrulaması)
| Konu | Sonuç → kural |
|---|---|
| Test Runner | EditMode koşusunda açık sahne kaydedilmemiş "Untitled": testte `EditorSceneManager.NewScene(Additive)` fırlatır → validator'ın sahne kısmı listeyle test edilir |
| Unity `Debug.LogException` | Sarılmış exception'ı en içteki mesajla loglar; `LogAssert` regex'i iç mesaja yazılır |
