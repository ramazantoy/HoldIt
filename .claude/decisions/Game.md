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
| UI sistemi | ~~UI Toolkit (UXML/USS) yalnız ekranlar ve HUD için kullanılır. Sabır barı ve konuşma balonu dünyadaki nesnelerdir (sprite/mesh + TMP)~~ | ~~Ekranlar ve HUD yerleşim işidir. Dünyadaki öğeler eğik kamerada karakteri izler ve karakterle birlikte havuzlanır. UI Toolkit'i öğrenmek portföy hedefinin parçası~~ |
| UI sistemi (2026-11-02, M0 sonu) | **Canvas (uGUI) + TextMeshPro** ekranlar ve HUD için. Sabır barı ve konuşma balonu dünyadaki nesnelerdir (sprite/mesh + TMP) | Kullanıcı kararı: UXML'i öğrenmek bu projede istenmiyor, Canvas'ı zaten biliyor. View'lar ince ve flow'lar interface'ten bildiği için yalnız üç view ve ekranlar değişti; flow'lar, testler ve input aynı kaldı |
| Ekran düzeni (M0) | ~~Ekran başına bir UXML (`Assets/Game/UI/`) ve sahnede bir UIDocument + view MonoBehaviour (elemanı adla bulur, tıkı event olarak verir). Ne olacağına saf C# flow karar verir (`MenuFlow`, `GameplayFlow`), view'ı interface'ten bilir. Bütün UIDocument'lar tek `GamePanelSettings`'i paylaşır (tek panel); gizleme `display: none`~~ | ~~UI Builder'da her ekran ayrı dosya. Mantık testli, view ince. Settings ekranı M7'de büyüyecek ve pause menüsünden de açılacak~~ |
| Ekran düzeni (M0, Canvas) | Her ekran bir prefab (`Assets/Game/UI/MainMenuScreen`, `SettingsScreen`, `GameplayHud`): kök (tam boy, arka plan `Image`) → `SafeArea` (`SafeAreaFitter`) → içerik (`VerticalLayoutGroup`). View kökte, düğmeleri `[Required]` serialize alanla tutar, dinleyiciyi `Awake`'te ekler; göster/gizle `SetActive`. Sahnede tek `Canvas` (Screen Space Overlay + `GraphicRaycaster`), ekranlar onun altında prefab instance'ı. Ne olacağına saf C# flow karar verir | Prefab modunda düzenlenir; Settings M7'de pause menüsünden de açılacak. Mantık testli, view ince |
| Panel ölçeği (M0) | ~~Scale With Screen Size, referans 1080×1920, eşleme genişlik (match 0). Tema `GameTheme.tss` (Unity varsayılanı), ortak stiller `Common.uss` (her UXML'de `<Style>`)~~ | ~~Canvas Scaler karşılığı. Portre telefonlarda en-boy farkı dikeyde çıkar. Stil UXML'de bağlı olunca UI Builder önizlemesi tema seçmeden doğru görünür~~ |
| Ekran geçişi (M0) | Tam ekran ekranlar `ScreenView`'dan (`HoldIt.UI`) türer: kökte nested `Canvas` + `GraphicRaycaster` + `CanvasGroup`. Göster/gizle `interactable` ve `blocksRaycasts`'ı hemen değiştirir, alpha'yı PrimeTween'le fade eder (unscaled, `ToUniTask(ct)`), gizlenince fade bitince `Canvas.enabled = false`. Yeni çağrı süren fade'i token'la keser. Sahne yüklendikten sonraki ilk çağrı fade'siz. Süre `ScreenTransitionConfig` (`Assets/Game/UI/`, 0,2 sn). HUD türemez | Kullanıcı önerisi (base class + CanvasGroup fade). `SetActive` açılışta Canvas'ı yeniden kurar; yalnız alpha 0 ise ekranı çizmeye ve tıkı tutmaya devam eder. Gizli ekranın raycast tutması `IUiHitTest`'te oyun dokunuşlarını yutardı. Pause'da fade sürsün diye unscaled |
| Ölçek (M0, Canvas) | `CanvasScaler` Scale With Screen Size, referans 1080×1920, eşleme genişlik (0) | Portre telefonlarda en-boy farkı dikeyde çıkar; genişlik sabit kalınca yatayda taşma olmaz |
| Güvenli alan (M0) | ~~Her ekranın tam boy container'ı `screen` adını taşır; `SafeArea.Track` `Screen.safeArea`'yı padding olarak verir~~ | ~~`androidRenderOutsideSafeArea` açık: çentik ve kamera deliği altında düğme kalmasın~~ |
| Güvenli alan (M0, Canvas) | `SafeAreaFitter` (`HoldIt.UI`): RectTransform anchor'ını `Screen.safeArea`'ya oturtur, her karede değişimi kontrol eder. Arka plan kökte kalır, içerik `SafeArea`'nın altında | `androidRenderOutsideSafeArea` açık: çentik ve kamera deliği altında düğme kalmasın; Android inset'leri geç bildirebilir |
| Metin (M0) | ~~UXML'de `LocalizedString` binding'i (`table="GUID:…" entry="Id(…)"`, UI Builder'ın yazdığı biçim). Validator görmüyor → "Template'e gidenler"~~ | ~~UI Builder'da düzenlenir, dil değişince kendiliğinden güncellenir; anahtar adı değişse de id ile bağlı kalır~~ |
| Metin (M0, Canvas) | `TextMeshProUGUI` + `LocalizeStringEvent` (Localization'ın "Localize" menüsüyle aynı kurulum), tablo ve anahtar id ile. Validator `LocalizeStringEvent`'i görür | Kırık referans Validate All'da yakalanır; anahtar adı değişse de id ile bağlı kalır |
| Font (M0) | ~~Baloo 2 (Regular + Bold, OFL). SDF FontAsset (56 pt, 1024² atlas, multi-atlas açık), Latin + Latin-1 + Latin Extended-A + tipografik noktalama önceden atlasta (332 glyph). ~~Statik~~ (2026-11-02) **Dinamik**: Unity 6.3'te UI Toolkit metni Advanced Text sistemiyle çiziyor, o da statik FontAsset'i reddediyor ("Advanced text system cannot use static font asset"). Regular'ın ağırlık tablosunda 700 → Bold, yani `-unity-font-style: bold` gerçek Bold'u kullanır. `.screen`'de `-unity-font-definition`~~ | ~~Önceden doldurulan atlas, metinler en/tr'de kaldıkça asset'in Editor'da değişmesini (diff gürültüsü) azaltır; yeni harf gelirse asset değişir, diff'te ayrı tutulur. Eksik tek karakter `ſ`. Asset'ler 2,2 MB metin YAML, LFS'e alınmadı (YAML merge/diff kalsın)~~ |
| Font (M0, Canvas) | Baloo 2 (Regular + Bold, OFL). TMP font asset'i (`Assets/Game/UI/Fonts/*.asset`, SDF 56 pt, 1024² atlas, dinamik, multi-atlas), Latin + Latin-1 + Latin Extended-A + tipografik noktalama önceden atlasta. Regular'ın ağırlık tablosunda 700 → Bold. TMP Settings'in varsayılan fontu Baloo2-Regular | Yeni TMP metni kendiliğinden Baloo 2 alır. Atlas önceden dolu: metinler en/tr'de kaldıkça asset'in Editor'da değişmesi azalır |
| UI üstünde başlayan basış (M0) | ~~`IUiHitTest` → `EventSystem.current.RaycastAll`, basış anındaki konumla. Basış UI üstünde başlarsa parmak kalkana kadar yok sayılır. Tam ekran container'lar `picking-mode: Ignore` (HUD)~~ | ~~Senkron sorgu, kare sırasından bağımsız. `IsPointerOverGameObject` input callback'inde geçen karenin durumunu okur. UI Toolkit panelleri EventSystem varken `PanelRaycaster` ile raycast'e girer~~ |
| TMP Essential Resources (M0) | `Assets/TextMesh Pro/` (Unity'nin ugui paketinden: TMP ayarları, SDF shader'lar, LiberationSans + `OFL.txt`). EmojiOne örnek sprite'ı ve sprite asset'i silindi, TMP Settings'in varsayılan sprite asset'i boş | TMP çalışmak için ayar ve shader'ları ister. EmojiOne'ın lisansı belirsiz ("lisans koşullarına sitelerinden bakın"), CC0 kuralına uymuyor ve kullanılmıyor |
| UI üstünde başlayan basış (M0) | `IUiHitTest` → `EventSystem.current.RaycastAll`, basış anındaki konumla; yalnız `GraphicRaycaster` (Canvas) ve `PanelRaycaster` (UI Toolkit) sonuçları sayılır. Basış UI üstünde başlarsa parmak kalkana kadar yok sayılır. HUD kökünde Graphic yok, metinlerde Raycast Target kapalı | Senkron sorgu, kare sırasından bağımsız. Dünyaya eklenecek bir `PhysicsRaycaster` oyun alanını UI yapmaz |

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
UI Toolkit'le yapılan denemenin sonuçları; HoldIt sonra Canvas'a döndü ("UI" → UI sistemi). Template'e ders olarak kalır.
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
| Sıra | ~~Önce placeholder (primitive); gerçek modeller oynanış bitince~~ | ~~Oynanış değişirken model işi boşa gider~~ |
| Sıra (2026-11-02, M1) | Ortam kiti (zemin, duvar, dar geçit, bina duvarı, kapı) kendi Blender kitimizden gelir: M1'le paralel üretilir, M1b'de alınır. Kabin, klozet, lamba (M5), su birikintisi (M6) ~~ve karakterler (CC0 paket)~~ oynanıştan sonra. (2026-11-03) Karakterler aşağıdaki "Karakterler (2026-11-03)" satırında | Kullanıcı kararı. Parçaların şekli ve ölçüsü grid'le M1'de sabitlendi ("Harita ve tahta" → Parçalar); placeholder'dan gerçek modele geçiş prefab'ın içinde kalır |
| Ortam | Harita verisinden modüler kare parçalarıyla kurulur (1 birim = 1 kare: zemin, duvar, kapı, kabin, dar geçit). Parçalar değiştirilebilir prefab'lardır; harita elle modellenmez | Harita veridir: yeni harita model işi istemez, placeholder'dan gerçek modele geçiş bir prefab değişimidir |
| Lisans | Repo public: yalnız CC0 (Kenney, Quaternius vb.) ya da kendi modellerin. Ücretli paket (Asset Store, Synty) repoya girmez. (2026-11-02) Fontlar OFL olabilir ("Kimlik ve kaynaklar" → Lisans: font) | Ücretli paket lisansları kaynak dosyaların public repoda dağıtılmasına izin vermez |
| Stil (2026-11-02) | Stilize, el boyaması görünümlü ve yumuşak. Low-poly siluet ve hafif pahlı kenarlar. Renk dokuda durur: üstten alta açıktan koyuya gradyan, kenarlarda açık vurgu, iç köşelerde AO, hafif fırça ve noise varyasyonu. Yönlü ışık ve gölge dokuya basılmaz. Bütün kit tek atlas ve tek materyal kullanır. Unity'de URP Shader Graph ile toon shader (yumuşak 2–3 bant, isteğe bağlı rim) M1b'de yapılır. Palet aynı kalır | Kullanıcı kararı; GDD §8'deki "düz renk"in yerine geçer. Ekranda bir kare ~90 px: ince detay okunmaz, gradyan ve kenar vurgusu okunur. Boyama script'le bake edilir (AO + curvature + gradyan + noise), ışığı shader verir. Tek materyal batching'i korur |
| Stil: toon shader (2026-11-02, M1b) | Kitin önizlemesiyle aynı formül. Lambert = max(N·L, 0) × ana ışığın gölge zayıflaması. Bant: Lambert < 0.2 → 0.60, 0.2–0.6 → 0.82, > 0.6 → 1.0; eşiklerde ±0.04 smoothstep. Rim = (1 − N·V)^4 × 0.15, yalnız Lambert > 0.2'de (sert eşik). Sonuç = base × min(1, bant × 0.8 + 0.2 + rim). Işığın rengi ve şiddeti kullanılmaz. Property'ler: `_BandLow` 0.2, `_BandHigh` 0.6, `_BandSoftness` 0.04, `_ShadeDark` 0.60, `_ShadeMid` 0.82, `_ShadeLit` 1.0, `_LightScale` 0.8, `_Ambient` 0.2, `_RimPower` 4, `_RimStrength` 0.15, `_RimGate` 0.2 | Kullanıcı mevcut ramp'ı seçti; güçlü ramp (×0.50/0.70/1.0) seçilmedi. Tam ışık alan üst yüz paletin hex'ini birebir gösterir; güney ve batı yüzü ×0.856, kuzey ve doğu yüzü ×0.68 (rim hariç). Renk dokuda durur, ışık yalnız tonu değiştirir |
| Karakterler | ~~Hazır bir CC0 paketten gelir. Thin/Normal/Fat aynı modelin yatay ölçeğiyle yapılır. Ortam kiti belki Blender'da bpy script'iyle yapılır~~ | ~~Tek model, üç beden. Prototip de böyle yapıyor (beden ölçeği 0.75/1.0/1.45)~~ |
| Karakterler (2026-11-03) | Karakterler de kendi Blender kitimizden gelir. Ortam kiti gibi ayrı oturumda, HoldIt'le paralel üretilir; M3'te alınır, o zamana kadar kapsül yürür. HoldIt'in sözleşmesi: pivot ayakta, +Z ileri; Thin/Normal/Fat (Fat 0.64'lük dar geçide gözle de sığmaz); erkek mavi, kadın pembe; ~100 px'te okunur; rig yok, parçalı model, Unity'de PrimeTween ile prosedürel animasyon (öneri); tek atlas ve HoldIt/Toon shader. Kesin ölçü ve parça listesi kitin SPEC onayıyla belli olur | Kullanıcı kararı. Kendi modelimiz kitle aynı stilde durur ve public repoda lisans sorunu çıkarmaz. Rig'siz parçalı model, animasyonu kodda ve testlenebilir tutar |

## Harita ve tahta (2026-11-02, M1)
| Konu | Karar | Gerekçe |
|---|---|---|
| Level = harita | Her level kendi harita asset'idir (`MapDefinition`, `Assets/Game/Board/Maps/`). Harita 1 `Lobby` (prototipin haritası). Level'ın kalabalık, spawn ve sabır ayarları ile ilerleme M6'da (Açık → GDD ↔ prototip) | Kullanıcı isteği: her level'da farklı grid/labirent. Yeni level model işi istemez |
| Format | Satır satır metin, üst satır kameradan en uzak: `.` zemin, `L` lobi, `E` giriş, `#` duvar, `n` dar geçit, `M`/`W` erkek/kadın binası, `m`/`w` kapıları (`MapParser`). Boyut veriden gelir; parmakla çizim için genişlik ~10 kare kalır. Haritalar elle yapılır, prosedürel üretim yok | YAML diff'inde her satır ayrı görünür, testler haritayı string olarak yazar. Üretilen haritanın oynanabilirliği ve zorluğu kontrol edilemez; ileride bir üretici gelirse aynı formatı üretir |
| Kare türleri | Prototipinkiler kalır: bina (yürünemez; kabinler M5'te içinde) ve lobi (spawn'ın beklediği yer) | GDD'nin döngüsü lobiyi zaten anıyor; bina kabinlerin ve ışıkların yeri |
| Oynanabilirlik | `MapCheck`: en az bir giriş; her cinsiyete tek kapı; bina, kapısıyla birlikte dolu bir dikdörtgen; kapı dışarıda tam bir yürünebilir kareye açılır; iki kapı yan yana değil; girişten kapılardan geçmeden bir lobi karesine ve iki kapıya yol var, Fat (dar geçitsiz) için de. `MapValidator` (`Game.Editor`) bütün haritaları Validate All'da kontrol eder | Bozuk harita build'den önce yakalanır. Dikdörtgen bina M5'te kabin dizilimini basit tutar; yan yana kapıların kasaları aynı yere düşer |
| Koordinat | `Cell(Col, Row)`, satır 0 en üstte. 1 kare = 1 birim, tahtanın merkezi dünya orijininde, zemin y = 0, satırlar −Z'ye doğru (`BoardSpace`). Kare seçimi ekran ışınını y = 0 düzlemiyle keser (`IBoardPicker`) | Collider gerekmez; parçalar değişse de seçim aynı kalır |
| Parçalar | ~~`BoardLayout` haritayı parçalara çevirir, `TileSet` her parçanın prefab'ını verir. Parçanın kökü kare merkezinde, y = 0'da; kenar parçaları karenin kuzey (+Z) kenarında modellenip 0/90/180/270 döndürülür, zeminler dönmez. Zeminler 0.1 kalın. Ölçüler prototipten: WallBlock 1×1, y −0.1..0.9; NarrowPosts zemin parçasının üstünde, 0.18×0.81×0.96 iki dikme, x=±0.41 (açıklık 0.64), kuzey ve güneyi kapalı geçitte 90°; BuildingWall 1.0×0.7×0.14, z 0.36..0.5, köşelerde üst üste biner; Doorway kapı karesinin kendi zemini + açıklık kenarında 0.12×0.92×0.22 dikmeler (x=±0.5) + piktogram (başı bina içine). Kaide ölçeklenmiş küp: haritayı 0.4 taşar, 0.6 derin. Placeholder'lar collider'sız küpler (`Assets/Game/Board/Pieces/`), materyaller prototip paletinden~~ | ~~Harita veridir ("Görsel ve asset" → Ortam); kit gelince yalnız prefab'ların içi değişir~~ |
| Parçalar (2026-11-02, M1b) | `BoardLayout` haritayı parçalara çevirir, `TileSet.Prefab(PiecePlacement)` her parçanın prefab'ını verir (`Assets/Game/Board/Pieces/`). Parçanın kökü kare merkezinde, y = 0'da; kenar parçaları karenin kuzey (+Z) kenarında modellenip 0/90/180/270 döndürülür, zeminler dönmez. Ölçüler M1'deki gibi: zeminler y −0.1..0; WallBlock y −0.1..0.9; NarrowPosts zemin parçasının üstünde, dikmeler x=±0.41 (açıklık 0.64), kuzey ve güneyi kapalı geçitte 90°; BuildingWall z 0.36..0.5, ~~+X ucu 1 mm içeride~~ (2026-11-03: kitte içe alma kaldırıldı; tek kutu, x −0.5..0.5, köşedeki çakışma kolonun içinde kalıyor); Doorway kendi zemini + açıklık kenarında dikmeler (x=±0.5, z 0.43) + piktogram (başı bina içine). Mesh'ler kendi Blender kitimizden: `Assets/Game/Board/Models/HoldIt_<Parça>.fbx` (18) ve `Models/textures/HoldIt_Kit_BaseColor.png` (1024², sRGB, mipmap, Clamp). Import ayarları: scale 1, Normals Import; tangent, rig, animasyon ve collider yok; Read/Write kapalı. FBX'teki `HoldIt_Kit` slotu `AddRemap` ile aynı adlı materyale bağlı. Prefab = kök + `Mesh` çocuğu; MeshFilter'da FBX'in mesh'i, MeshRenderer'da `HoldIt_Kit`, gölge açık. FBX prefab'a iç içe konmaz. Kaide ölçeklenmiş küp kalır (haritayı 0.4 taşar, 0.6 derin). Collider yok | Dosya adları kitle aynı: yeni export üstüne kopyalanır, `.meta`'daki ayarlar ve prefab'ların mesh referansları kalır. Mesh doğrudan kullanıldığı için FBX'in materyali ve transform'u prefab'a sızmaz. Kare seçimi zemin düzlemiyle yapıldığı için collider gerekmez ("Koordinat"; kit SPEC'indeki "collider prefab'da kalır" burada geçerli değil). Kitin bounds'u editor koduyla ölçüldü: 18/18 SPEC'le ±1e-3 içinde, kökler rot 0 / scale 1 |
| Duvar bloğu şekli (2026-11-02, M1b) | WallBlock 6 modelden biridir (`WallShape`). Yaw 0'daki bağlantılar: Single yok, End K, Straight K+G, Corner K+D, T K+D+G, Cross dördü. `WallAutotile.Pick` yalnız `#` komşuların maskesine bakar (K1 D2 G4 B8). Maske çeyrek tur geri döndürülür; yaw 0 tablosuna ilk oturduğu tur yönü verir (Straight K+G 0°, D+B 90°). Dar geçit, bina ve harita dışı bağlanmaz. Şekil `PiecePlacement.Shape`'te taşınır; `TileSet`'te şekil başına bir prefab vardır, şekilsiz WallBlock fırlatır | Kullanıcı kararı (kitte B: varyantlar; "tek parça + her yerde pah" reddedildi). Bağlı kenarlar pahsız olduğu için yan yana bloklar tek kütle okunur, açık kenarlar bitmiş yüz olur. 16 maske `WallAutotileTests`'te |
| Bina köşe kolonu (2026-11-02, M1b) | `PieceKind.BuildingCorner` (cinsiyetli), KD köşede modellenmiş. Facing North KD, East GD, South GB, West KB: d ile saat yönündeki sonraki kenarın köşesi. Kolon dikdörtgenin 4 dış köşesine, köşede buluşan iki kenar da duvarlıysa konur. Kapının açıklık kenarına değen köşeye kolon gelmez, orada kapı dikmesi durur. Yan yana binaların ortak köşesine iki kolon gelir (W kitte 1 mm büyük) | Duvar uçları kolonun içinde kalır; kolonsuz köşede pahsız uç başlık gibi görünüyordu. Ortak köşede aynı düzlemde yüz kalmaz, z-fighting yok (Unity'de yakın planda bakıldı) |
| Shader ve materyal (2026-11-02, M1b) | `Assets/Game/Rendering/Toon.shadergraph` (`HoldIt/Toon`, Universal Unlit). Ayarlar: Cast Shadows, Keep Lighting Variants, Default SSAO kapalı. Işık hesabı `ToonLighting.hlsl`'de (Custom Function, File) ("Görsel ve asset" → Stil: toon shader). Kitin tek materyali `HoldIt_Kit` (Base Map = atlas). Kaide `BoardBase`, `HoldIt_Kit`'in Material Variant'ı: yalnız Base Map boş ve Base Color #5a6b7d override. Toon değerleri materyal property'si; shader'daki varsayılanlar seçilen değerler. Graph editor koduyla Shader Graph'ın kendi sınıfları ve serializer'ıyla kuruldu (elle JSON yok); Shader Graph penceresinde düzenlenir | Config kuralının karşılığı: ayarlanabilir sayılar kodda değil asset'te durur, Inspector'dan değişir. ScriptableObject kopyası runtime'da paylaşılan materyale yazmayı ya da iki doğruluk kaynağını getirirdi. Variant sayesinde değerler tek yerde. Kitin formülünde SSAO yok, telefonun renderer'ında da yok; açık kalsa Editor görüntüsü telefondan ayrılırdı. SRP Batcher uyumlu (iki subshader'da da kod 0). Blender render'ıyla aynı kamerada 11 örnek noktada fark ≤ 1/255, kaide formülle birebir |
| Kamera | Perspektif, dikeyden 22°, FOV 40 (`BoardCameraConfig`). `CameraFit` tahtanın renderer bounds köşelerini güvenli alan eksi HUD üst barı eksi paylar (yanlar %2, alt %2, üst %1) dikdörtgenine ortalayıp bir eksende dayanacak şekilde sığdırır. `BoardCamera` ekran, safe area, HUD, bounds ya da config değişince yeniden sığdırır. Telefonlarda genişlik sınırlar (9:16'da tahta merkezine ~31 m); uzun ekranlarda üstte ve altta boşluk kalır | Prototipin değerleri. Bounds'tan sığdırma gerçek modellerle de doğru kalır |
| HUD üst barı | `GameplayHud` → `SafeArea/TopBar` (tam genişlik, 184 referans px); menü düğmesi içinde, skor ve can M6'da buraya girer. `GameplayHudView` alt kenarını `IBoardInsets` olarak verir | Tahta HUD'un altında kalır; kamera HUD'u değil interface'i bilir |

## Açık
- ~~**Dil:** GDD §11 "yalnız İngilizce" diyor, template'te Localization modülü var (en/tr örneği). Mobil doğrulama listesindeki
  "System seçicisi cihaz dilini buluyor mu" maddesi tek dille anlamsız kalır. Tek dil seçilirse kontrol listesinin 13.
  adımı modülü çıkarır (Addressables'ı çıkarmak denenmedi). Öneri: modül kalır, en + tr. → M0~~ (2026-11-02: en + tr, modül
  kalır → "Kimlik ve kaynaklar" → Oyun dilleri)
- ~~**Android geri tuşu:** Esc hem `Gameplay/Pause`'a hem `UI/Cancel`'a bağlı (`README.md` → "Açık"). Geri tuşunun menüde,
  oyunda ve pause'da ne yapacağı → M0~~ (2026-11-02: tek geri action'ı `UI/Cancel` → "Input" → Geri tuşu; cihazda gelişi
  aşağıdaki "Cihaz testi"nde)
- ~~**UI sistemi** (2026-11-02, M0 sonu): UI Toolkit'le devam mı, Canvas'a dönüş mü~~ (2026-11-02: Canvas → "UI" → UI sistemi)
- **Cihaz testi** (2026-11-02): Android cihaz yok, M0'da build alınmadı ve aktif platform Android'e geçirilmedi. Cihaz
  bulununca: aktif platform Android, Development build, `adb install` + `adb logcat -s Unity`. Bakılacaklar: akış ve video,
  geri tuşunun Escape → `UI/Cancel` olarak gelmesi (`androidPredictiveBackSupport` açık; gelmezse kapatmak denenir), kökte
  geri ile çıkış, Gameplay'de dokunuş logu ve HUD'a basışın loglanmaması, mod None iken panelin tepkisizliği, portre kilit,
  Türkçe karakterler, çentik altında HUD, H.264 High'ın oynaması, ilk build'in yeniden yazdığı URP asset'leri (ayrı commit).
  Ardından `README.md` → "İlk mobil oyunda doğrulanacaklar" (M0b'nin cihaz kısmı). (2026-11-02) M0b'nin tamamı
  cihaz bulununca yapılır (kullanıcı kararı; bitti şartı telefon istiyor), M1 ondan önce. (2026-11-02, M1b) Aktif platform
  artık Android: atlas ASTC 6x6'ya sıkıştırılıyor, Editor'da Play Android'in Mobile kalite seviyesinde çalışıyor (PC
  seviyesi Android'de kapalı). Cihazda ayrıca bakılacaklar: tahtanın ekranda ASTC + mipmap'le görünüşü
- ~~**Telefon gölgesi** (2026-11-02, M1b): Mobile URP asset'inin gölge ayarları: 1024 harita, tek cascade, sert gölge,
  depth/normal bias 1/1. Bunlarla gölge kenarları dalgalı çıkıyor, gölgeler kitin render'ındakinden ince, içlerinde yer
  yer ışık noktaları var. Shader ve kit doğru: gölgesiz yüzlerde fark ≤ 1/255. Önerim: Mobile_RPAsset'te çözünürlük 2048,
  soft shadows açık, bias'ı düşürmek, shadow distance'ı tahtayı kapsayan en kısa değere çekmek. Proje ayarı olduğu için
  kullanıcının onayını bekliyor~~ (2026-11-03, kullanıcı onayladı) `Mobile_RPAsset`: gölge haritası 2048, soft shadows
  açık (kalite Medium), depth/normal bias 0.3/0.3. Shadow distance 50 ve tek cascade kaldı (9:21'de tahtanın uzak köşesi
  ~43 m). Sonuç: kenarlar yumuşak, ışık noktaları yok, acne yok; gölge kitin render'ına yakın, biraz daha dar. Soft
  shadow'un telefondaki maliyetine cihazda bakılacak
- **Kitte izlenecekler** (2026-11-02, M1b; kit oturumuna bildirildi, kit v1 kilidi sonuca bağlı):
  - ~~BuildingWall düz sırasında ek yerindeki 1 mm basamak (fırıldak), MSAA'sız telefonda ek yerinin uçlarında tek piksel
    koyu nokta bırakıyor.~~ (2026-11-03) Kitte düzeltildi: içe alma kaldırıldı. Yeni export alındı (18 FBX ve atlas);
    ek yeri MSAA'sız da görünmüyor, ölçüler 18/18 ve ton farkı ≤ 1/255.
  - Bina zemininde noise her karede aynı; yakın planda seçiliyor, oyun ölçeğinde göze batmıyor. ~~Kullanıcı kararı
    bekliyor (önerim: olduğu gibi kalsın).~~ (2026-11-03, kullanıcı kararı) Olduğu gibi kalır; prototip bitince genel
    polish turunda yeniden bakılır.
  - Zeminler komşu duvarın AO'sunu almıyor; toon shader'da SSAO kapalı
- **Yol çizimi** (2026-11-03, kullanıcı kararı): çizim (input + görsel) bilerek bekletiliyor. Görselde LineRenderer'dan
  vazgeçildi ("dummy hissettirdi"); kare efektleri düşünülüyor, oynayınca seçilecek. Yol modeli (saf C#, hücre listesi +
  kurallar) önce yapılabilir, M3 onu girdi olarak alır. Milestone tablosu (M2'nin tanımı ve sırası) sıradaki planda
  güncellenir. Çizim en geç M6'dan önce gelir (M6'nın "tam döngü" şartı)
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
  | ~~Kare türleri~~ | ~~Bina (M/W, yürünemez) ve lobi (L) var~~ | ~~Yok~~ | M1: prototipinki ("Harita ve tahta") |
  | Diğer | Su birikintisi 12 sn, beden ölçeği 0.75/1.0/1.45, ~~kamera 22° eğim / FOV 40~~ (M1'de alındı), metinler Türkçe | "Birkaç saniye" | M3–M6 |

## Template'e gidenler
Burada bulunup template'te düzeltilen framework sorunları.

| Tarih | Sorun | Template commit'i | HoldIt'e alındı |
|---|---|---|---|
| 2026-11-02 | (HoldIt Canvas'a döndü, oyunu artık etkilemiyor) `LocalizationValidator` UXML'deki `LocalizedString` binding'lerini taramıyor (yalnız sahne/prefab/ScriptableObject alanları): kırık tablo ya da anahtar sessiz kalır. Öneri: `VisualTreeAsset`'leri yükleyip binding'leri (`TryGetBinding` → `LocalizedReference`) aynı kontrole sokmak | — | — |
| 2026-11-02 | CLAUDE.md iş akışı: `.rsp` yolu sabit değil (CLAUDE.md `1900b0aEDbg.dag` diyor, bu makinede `1300b0aEDbg.dag`). Öneri: `Library/Bee/artifacts/*.dag/<asmdef>.rsp` | — | — |
| 2026-11-02 | CLAUDE.md iş akışı: `EditorSceneManager.OpenScene(Single)` başka referansı olmayan asset'leri bellekten atar; açmadan önce yüklenen asset referansı "yok edilmiş" null'a döner ve atama boş yazılır (hata vermez). Asset'i sahneyi açtıktan sonra yükle | — | — |
| 2026-11-02 | CLAUDE.md iş akışı: odaksız Editor'da Play. `Application.runInBackground = true` (yalnız o oturum, ayar değişmez) loop'u döndürür; `Step` ile duraklatılmış Play'de `VideoPlayer` ilerlemez. `script run`'dan `QueueStateEvent` olayı oyuna ulaşmaz (native fare de ezer): sanal cihaz + `InputSystem.Update()` ile cihaz durumu değişti, uçtan uca tık denenemedi. Test cihazı domain reload'dan sağ çıkıyor, elle silinmeli | — | — |
| 2026-11-02 | Template'in `Controls.inputactions`'ı Esc'i hem `Gameplay/Pause` hem `UI/Cancel`'a bağlıyor (`README.md` → Açık). HoldIt'in çözümü (tek geri action'ı + `BackPressed`, ekran başına geri davranışı) template'e örnek olarak taşınabilir. Kullanıcı kararı | — | — |
