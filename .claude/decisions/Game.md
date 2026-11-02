# HoldIt (oyun kararları)

Oyunun kararları, doğrulananları ve açık soruları burada. Framework kararları diğer dosyalarda; bu dosya template'te
yok, bu yüzden cherry-pick'lerle çakışmaz. Oyunun "Açık" listesi de burada (`README.md`'deki liste framework'ün).
Karar değişirse satır silinmez; eskisinin üstü çizilir, yenisi tarihle eklenir.

## Kimlik ve kaynaklar (2026-11-02)
| Konu | Karar | Gerekçe |
|---|---|---|
| Amaç | Mağazaya çıkmayan portre Android oyunu. Repo public GitHub showcase'i olur. Oyuncuya giden tek şey GitHub Releases'teki Release APK'sı | Portföy: bitmiş bir oyun ve 10 dakikada okunabilen kod. APK git'e girmez (`*.apk` gitignored): binary git geçmişinde her klonda kalır ve LFS kotasını yer |
| Template'in sahada denenmesi | Framework burada yamanmaz. Sorun "Template'e gidenler"e yazılır, template'te düzeltilir, `git cherry-pick -x` ile alınır. HoldIt'e özgü kurallar CLAUDE.md'nin başında durur; framework bölümleri template'le aynı kalır | İki kopya ayrışırsa düzeltme geri taşınamaz. Bu düzende cherry-pick yalnız CLAUDE.md'nin baş bloğuyla çakışır |
| Fork noktası | Template `a949bbd` ("Validate the template as a whole and add the README"). Tree birebir aynı; `template` remote'unda bulunur | Cherry-pick'in temeli |
| Tasarım kaynakları | `Design/` gitignored. İçinde GDD (yol gösterir, değişir) ve HTML prototip (three.js; davranış ve tuning referansı, kodu port edilmez) var. Kalıcı karar buraya ya da CLAUDE.md'ye taşınır | Taslak bir GDD public repoda koddan farklı şey söyler. Portföyde tasarımı M10'un README'si gösterir |
| Çalışma dili | Türkçe (CLAUDE.md, `.claude/`, `Design/`). Kod, yorum, commit mesajı ve framework dokümanı İngilizce. README EN/TR, M10'da yazılır | Template'teki düzenle aynı. md dosyalarının public'teki dili "Açık"ta |
| Oyun dilleri | İngilizce + Türkçe. Template'in Localization modülü kalır; örnek tablolar (`UI`, `menu.play`) oyunun tablolarıyla değişir. Balon metinleri dahil bütün metinler `LocalizedString` ile bağlanır. Ayarlarda dil seçimi `ILocaleService` ile (M7). Specific (yedek) seçici M0'da belirlenir (öneri: en) | Kullanıcı kararı (2026-11-02). Mobil doğrulama listesindeki System seçicisi ancak iki dille denenebilir. Modülü çıkarmak (Addressables) template'te hiç denenmemiş bir yol |
| Namespace | Kök `HoldIt`, özellik klasörü `HoldIt.<Özellik>`. Asmdef `Game` kalır. Testler `Assets/Game/Tests/`'te (`Game.Tests`, namespace `HoldIt.Tests`). `Game.Editor` ilk editor script'iyle açılır. Geçiş M0'da yapılır (2026-11-02: yapıldı; klasörler `Intro`, `Menu`, `Gameplay`, `Input`, `UI`) | Template kontrol listesinin 7. adımı: asmdef adı `[SerializeReference]` verisine yazılır, sabit kalan ad veriyi kırmaz. Kodda görünen kimlik namespace'tir |
| Kimlik (M0) | Company `ramazantoy`, Product `HoldIt`, identifier `com.ramazantoy.holdit` (küçük harf için override açık; override global olduğu için Standalone ve iOS'unki de aynı), Version 0.1.0, bundle version code 1. Yön Portrait, otomatik döndürme kapalı | Repo `github.com/ramazantoy` altında: paket adı, GitHub adresi ve kayıt klasörü (`LocalLow/ramazantoy/HoldIt`) aynı adı taşır. Takma ad `afoolasfallen` framework'ün `AF` önekinde kalır |
| Lisans: font (M0) | ~~Asset yalnız CC0 ya da kendi işin~~ → (2026-11-02) fontlar SIL Open Font License (OFL) olabilir; `OFL.txt` fontun yanında durur | OFL public repoda dağıtıma izin verir (fontla lisans dosyası birlikte, tek başına satılmaz). Kural ücretli paketleri dışarıda tutmak içindi |

## UI (2026-11-02)
| Konu | Karar | Gerekçe |
|---|---|---|
| UI sistemi | UI Toolkit (UXML/USS) yalnız ekranlar ve HUD için kullanılır. Sabır barı ve konuşma balonu dünyadaki nesnelerdir (sprite/mesh + TMP) | Ekranlar ve HUD yerleşim işidir. Dünyadaki öğeler eğik kamerada karakteri izler ve karakterle birlikte havuzlanır. UI Toolkit'i öğrenmek portföy hedefinin parçası |

| Ekran düzeni (M0) | Ekran başına bir UXML (`Assets/Game/UI/`) ve sahnede bir UIDocument + view MonoBehaviour (elemanı adla bulur, tıkı event olarak verir). Ne olacağına saf C# flow karar verir (`MenuFlow`, `GameplayFlow`), view'ı interface'ten bilir. Bütün UIDocument'lar tek `GamePanelSettings`'i paylaşır (tek panel); gizleme `display: none` | UI Builder'da her ekran ayrı dosya. Mantık testli, view ince. Settings ekranı M7'de büyüyecek ve pause menüsünden de açılacak |
| Panel ölçeği (M0) | Scale With Screen Size, referans 1080×1920, eşleme genişlik (match 0). Tema `GameTheme.tss` (Unity varsayılanı), ortak stiller `Common.uss` (her UXML'de `<Style>`) | Canvas Scaler karşılığı. Portre telefonlarda en-boy farkı dikeyde çıkar. Stil UXML'de bağlı olunca UI Builder önizlemesi tema seçmeden doğru görünür |
| Güvenli alan (M0) | Her ekranın tam boy container'ı `screen` adını taşır; `SafeArea.Track` `Screen.safeArea`'yı padding olarak verir | `androidRenderOutsideSafeArea` açık: çentik ve kamera deliği altında düğme kalmasın |
| Metin (M0) | UXML'de `LocalizedString` binding'i (`table="GUID:…" entry="Id(…)"`, UI Builder'ın yazdığı biçim). Validator görmüyor → "Template'e gidenler" | UI Builder'da düzenlenir, dil değişince kendiliğinden güncellenir; anahtar adı değişse de id ile bağlı kalır |
| Font (M0) | Baloo 2 (Regular + Bold, OFL). SDF FontAsset (56 pt, 1024² atlas, multi-atlas açık), Latin + Latin-1 + Latin Extended-A + tipografik noktalama önceden atlasta (332 glyph). ~~Statik~~ (2026-11-02) **Dinamik**: Unity 6.3'te UI Toolkit metni Advanced Text sistemiyle çiziyor, o da statik FontAsset'i reddediyor ("Advanced text system cannot use static font asset"). Regular'ın ağırlık tablosunda 700 → Bold, yani `-unity-font-style: bold` gerçek Bold'u kullanır. `.screen`'de `-unity-font-definition` | Önceden doldurulan atlas, metinler en/tr'de kaldıkça asset'in Editor'da değişmesini (diff gürültüsü) azaltır; yeni harf gelirse asset değişir, diff'te ayrı tutulur. Eksik tek karakter `ſ`. Asset'ler 2,2 MB metin YAML, LFS'e alınmadı (YAML merge/diff kalsın) |
| UI üstünde başlayan basış (M0) | `IUiHitTest` → `EventSystem.current.RaycastAll`, basış anındaki konumla. Basış UI üstünde başlarsa parmak kalkana kadar yok sayılır. Tam ekran container'lar `picking-mode: Ignore` (HUD) | Senkron sorgu, kare sırasından bağımsız. `IsPointerOverGameObject` input callback'inde geçen karenin durumunu okur. UI Toolkit panelleri EventSystem varken `PanelRaycaster` ile raycast'e girer |

## Input (2026-11-02, M0)
| Konu | Karar | Gerekçe |
|---|---|---|
| Map'ler | `Gameplay`: `Point` (~~Value~~ → PassThrough/Vector2, `<Pointer>/position`) + `Press` (Button, `<Pointer>/press`), gruplar Keyboard&Mouse + Touch. Template'in `Move`, `Look`, `Pause`'u silindi. UI map'i template'inki | HoldIt yalnız işaretçi kullanır; gamepad yok. Point PassThrough (inceleme, 2026-11-02): Value birden çok işaretçide (fare + dokunuş) "en büyük" konumu seçerdi; `ControlsAssetTests` asset'in şeklini korur |
| `IPointerInput` | Gameplay sahne scope'unda (`UsePointerInput`). `IsPressed`, `Position` + `PointerDown/Moved/Up` mesajları. Callback'ler yalnız kaydeder (her basış/bırakış o anki cihaz konumuyla), `Tick` sırayla karar verir. Mod değişimiyle gelen canceled da Up olur | GDD §5'in Down/Move/Up'ı. Tek karede bas-bırak, aynı karede bırak + yeni basış (eski çizgi yeni konuma sürüklenmez) ve map kapanması testli |
| Geri tuşu | Tek "geri" action'ı `UI/Cancel` (Esc; Android Back Escape olarak gelir). Kök `BackButton` → `BackPressed`. Settings açıkken kök menüye; kök menüde mobilde çıkış, masaüstünde hiçbir şey; Gameplay'de Menu (M6'da pause'u açar, açıkken kapatır). Gameplay modu UI map'ini tutar | Tek action: tek basış iki iş yapamaz, template'in "pause'u açan basış kapatır" sorunu kalkar (`README.md` → Açık). Android'de kökte geri çıkıştır; masaüstünde Quit düğmesi var |
| Action erişimi | `FindAction("Map/Action", throwIfNotFound: true)` + private const string. Generate C# Class (`@PlayerControls`) ve tipli sarmalayıcı yok | Kullanıcı kararı (2026-11-02). Üretilen sınıf asset'in ayrı kopyasını kurar: mod servisi (yüklemede, menüde, pause'da map kapatma), UI modülü ve rebind onu görmez. Sarmalayıcı yerine const: yönetmesi basit, Editor'a bağlı değil |

## Intro (2026-11-02, M0)
| Konu | Karar | Gerekçe |
|---|---|---|
| Splash | `Assets/Game/Video/Splash.mp4` (kullanıcının kendi videosu: H.264 High, 1080×1920, 30 fps, 3,4 sn, ses yok). `VideoPlayer` kameranın arka düzleminde, FitInside, kamera arka planı siyah, unscaled zaman. `IntroFlow` video **ve** boot bitince Menu'yü yükler; video hatası loglanır ve bitmiş sayılır | Kullanıcı isteği. Template'in sabit bekleme yerine "gerçek intro" yolu. Ses kanalı yok, mixer/AudioSource kuralına girmiyor |

### M0'da doğrulanacaklar (template UI Toolkit'le hiç denenmedi) → sonuçlar (2026-11-02, Editor)
- Panel input'u root EventSystem'den (UI map) alıyor mu? → **Evet.** Play'de `UIElementsRuntimeUtility.useDefaultEventSystem`
  false, aktif EventSystem root'taki (`InputSystemUIInputModule`); `GamePanelSettings` için `PanelEventHandler` +
  `PanelRaycaster` DontDestroyOnLoad'da. Editor'da gerçek fare tıkı Menu → Gameplay → Menu geçişini yaptı.
- UI map'i olmayan modda (yükleme boyunca mod None) panel tepki vermiyor mu? → **Çıkarımla evet, tıkla denenmedi.** Panelin
  tek input yolu UI modülü, modül UI map'inin action'larını okuyor (aynı asset), mod None onları kapatıyor. `script run`'dan
  kuyruğa atılan olay odaksız Editor'da oyuna ulaşmadı (→ Template'e gidenler); cihazda bakılır.
- UI üstünde başlayan bir basış nasıl anlaşılır? → `EventSystem.RaycastAll` (Input → "UI üstünde başlayan basış"). Play'de
  Play düğmesi `PanelRaycaster`'a çarptı. UIDocument'ın kök elemanı varsayılan `picking-mode: Ignore`; tam ekran
  container'ın kendisi Ignore değilse her yeri UI sayar (HUD'da Ignore). Mantık `PointerInputTests`'te; uçtan uca cihazda.
- UXML'deki localization binding'ini `LocalizationValidator` görmez → **Doğrulandı (koddan):** yalnız sahne, prefab ve
  ScriptableObject'teki `Localized*` alanlara bakıyor, `VisualTreeAsset` taranmıyor → Template'e gidenler. Binding'lerin
  kendisi çalışıyor: sistem dili tr iken düğmeler "Oyna/Ayarlar/Çıkış", yedi binding de doğru anahtara çözülüyor.
- Android geri tuşu panelde `UI/Cancel` olarak geliyor mu? → **Açık:** Android cihaz yok (Açık → Cihaz testi).

## Görsel ve asset (2026-11-02)
| Konu | Karar | Gerekçe |
|---|---|---|
| Sıra | Önce placeholder (primitive); gerçek modeller oynanış bitince | Oynanış değişirken model işi boşa gider |
| Ortam | Harita verisinden modüler kare parçalarıyla kurulur (1 birim = 1 kare: zemin, duvar, kapı, kabin, dar geçit). Parçalar değiştirilebilir prefab'lardır; harita elle modellenmez | Harita veridir: yeni harita model işi istemez, placeholder'dan gerçek modele geçiş bir prefab değişimidir |
| Lisans | Repo public: yalnız CC0 (Kenney, Quaternius vb.) ya da kendi modellerin. Ücretli paket (Asset Store, Synty) repoya girmez. (2026-11-02) Fontlar OFL olabilir ("Kimlik ve kaynaklar" → Lisans: font) | Ücretli paket lisansları kaynak dosyaların public repoda dağıtılmasına izin vermez |
| Karakterler | Hazır bir CC0 paketten gelir. Thin/Normal/Fat aynı modelin yatay ölçeğiyle yapılır. Ortam kiti belki Blender'da bpy script'iyle yapılır | Tek model, üç beden. Prototip de böyle yapıyor (beden ölçeği 0.75/1.0/1.45) |

## Açık
- ~~**Dil:** GDD §11 "yalnız İngilizce" diyor, template'te Localization modülü var (en/tr örneği). Mobil doğrulama listesindeki
  "System seçicisi cihaz dilini buluyor mu" maddesi tek dille anlamsız kalır. Tek dil seçilirse kontrol listesinin 13.
  adımı modülü çıkarır (Addressables'ı çıkarmak denenmedi). Öneri: modül kalır, en + tr. → M0~~ (2026-11-02: en + tr, modül
  kalır → "Kimlik ve kaynaklar" → Oyun dilleri)
- ~~**Android geri tuşu:** Esc hem `Gameplay/Pause`'a hem `UI/Cancel`'a bağlı (`README.md` → "Açık"). Geri tuşunun menüde,
  oyunda ve pause'da ne yapacağı → M0~~ (2026-11-02: tek geri action'ı `UI/Cancel` → "Input" → Geri tuşu; cihazda gelişi
  aşağıdaki "Cihaz testi"nde)
- **UI sistemi** (2026-11-02, M0 sonu): kullanıcı UXML'i zor buldu ("HTML/CSS gibi ama anlamıyorum"). M0 UI Toolkit'le kaldı;
  kullanıcı tutorial izleyip devam edebilir ya da Canvas'a (uGUI) dönülür → sonraki oturumda karar. Dönülürse: flow'lar,
  testler ve input aynı kalır; yalnız üç view (`MainMenuView`, `SettingsView`, `GameplayHudView`) ve ekranlar değişir, metin
  TMP + `LocalizeStringEvent` (validator görür), font Baloo 2'nin TMP asset'i, `EventSystemUiHitTest` `GraphicRaycaster`'ı zaten
  sayıyor, güvenli alan RectTransform anchor'ıyla. TMP Essential Resources import'u ve UI Toolkit dosyalarının silinmesi onay ister
- **Cihaz testi** (2026-11-02): Android cihaz yok, M0'da build alınmadı ve aktif platform Android'e geçirilmedi. Cihaz
  bulununca: aktif platform Android, Development build, `adb install` + `adb logcat -s Unity`. Bakılacaklar: akış ve video,
  geri tuşunun Escape → `UI/Cancel` olarak gelmesi (`androidPredictiveBackSupport` açık; gelmezse kapatmak denenir), kökte
  geri ile çıkış, Gameplay'de dokunuş logu ve HUD'a basışın loglanmaması, mod None iken panelin tepkisizliği, portre kilit,
  Türkçe karakterler, çentik altında HUD, H.264 High'ın oynaması, ilk build'in yeniden yazdığı URP asset'leri (ayrı commit).
  Ardından `README.md` → "İlk mobil oyunda doğrulanacaklar" (M0b'nin cihaz kısmı)
- **Haptik** (GDD §7'deki ayar, M9): template'te yok. Açma/kapama oyunun kendi Global savable'ında durur. Titreşimin
  kendisinin oyunda mı template'te mi (Platform sözleşmesi olarak) yazılacağı → M9
- **En iyi skor** (harita başına): template'in Save'i slot odaklı. HoldIt slot kullanmaz, skor Global savable'da tutulur
  → M7'de teyit
- **Release APK imzası:** template Android'i imzalamıyor (debug keystore). Sideload edilen showcase APK'sı için yeterli
  → M10'da teyit
- **README:** kökteki README.md ve README.tr.md template'i anlatıyor. HoldIt'inki M10'da EN/TR yazılır. Repo ondan önce
  public olursa template README'si görünür.
- **Public'e geçmeden önce:** `decisions/`'ın framework dosyalarındaki "eski oyun" anlatımları ve takma ad satırı genel derse
  çevrilir (`README.md` kuralı). Düzeltme template'te yapılıp alınır. Ayrıca o zaman karar verilecek iki konu:
  - md dosyalarının dili: hedef yurt dışıysa İngilizce. Çeviri template'te yapılıp alınır.
  - `.claude/` (decisions dahil) public repoda kalacak mı? Önerim: kalsın. İçinde kalıcı karar kaydı var, ignore etmek
    template'ten gelen cherry-pick'leri bozar (değişti/silindi çakışması), ayrıca AI iş akışını göstermek bilerek alınmış bir
    karardı.
- **Ad:** GDD'deki `CharacterController` (presenter) Unity'nin bileşeniyle aynı adı taşıyor → M3
- **GDD ↔ prototip:** GDD "değerler prototipten" diyor ama ikisi ayrışmış. İlgili milestone'da kullanıcı karar verir:

  | Konu | Prototip | GDD | Milestone |
  |---|---|---|---|
  | İlerleme | 5 seviye; her seviyenin kalabalık sayısı ve spawn aralığı var, seviye sonunda yıldız | Harita başına sonsuz mod; spawn skorla sıklaşır (4.8 sn, skor başına −0.08, en az 2.1) | M6 |
  | Dolu kare | `BLOCK_TIMEOUT` sonra olduğu yerde durur ("Yol kapalı!"), yeniden çizilebilir | `BlockedWaitSeconds` sonra geri yürür | M4 |
  | Hız Thin/Normal/Fat (kare/sn) | 3.0 / 2.6 / 2.0 | 3.6 / 3.0 / 2.2 | M3 |
  | En fazla karakter | 14 | 9 | M6 |
  | Sabır | Seviyeye göre 30 → 22 sn | Sabit 30 sn | M6 |
  | Kare türleri | Bina (M/W, yürünemez) ve lobi (L) var | Yok | M1 |
  | Diğer | Su birikintisi 12 sn, beden ölçeği 0.75/1.0/1.45, kamera 22° eğim / FOV 40, metinler Türkçe | "Birkaç saniye" | M1–M6 |

## Template'e gidenler
Burada bulunup template'te düzeltilen framework sorunları.

| Tarih | Sorun | Template commit'i | HoldIt'e alındı |
|---|---|---|---|
| 2026-11-02 | `LocalizationValidator` UXML'deki `LocalizedString` binding'lerini taramıyor (yalnız sahne/prefab/ScriptableObject alanları): kırık tablo ya da anahtar sessiz kalır. Öneri: `VisualTreeAsset`'leri yükleyip binding'leri (`TryGetBinding` → `LocalizedReference`) aynı kontrole sokmak | — | — |
| 2026-11-02 | CLAUDE.md iş akışı: `.rsp` yolu sabit değil (CLAUDE.md `1900b0aEDbg.dag` diyor, bu makinede `1300b0aEDbg.dag`). Öneri: `Library/Bee/artifacts/*.dag/<asmdef>.rsp` | — | — |
| 2026-11-02 | CLAUDE.md iş akışı: `EditorSceneManager.OpenScene(Single)` başka referansı olmayan asset'leri bellekten atar; açmadan önce yüklenen asset referansı "yok edilmiş" null'a döner ve atama boş yazılır (hata vermez). Asset'i sahneyi açtıktan sonra yükle | — | — |
| 2026-11-02 | CLAUDE.md iş akışı: odaksız Editor'da Play. `Application.runInBackground = true` (yalnız o oturum, ayar değişmez) loop'u döndürür; `Step` ile duraklatılmış Play'de `VideoPlayer` ilerlemez. `script run`'dan `QueueStateEvent` olayı oyuna ulaşmaz (native fare de ezer): sanal cihaz + `InputSystem.Update()` ile cihaz durumu değişti, uçtan uca tık denenemedi. Test cihazı domain reload'dan sağ çıkıyor, elle silinmeli | — | — |
| 2026-11-02 | Template'in `Controls.inputactions`'ı Esc'i hem `Gameplay/Pause` hem `UI/Cancel`'a bağlıyor (`README.md` → Açık). HoldIt'in çözümü (tek geri action'ı + `BackPressed`, ekran başına geri davranışı) template'e örnek olarak taşınabilir. Kullanıcı kararı | — | — |
