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
| Namespace | Kök `HoldIt`, özellik klasörü `HoldIt.<Özellik>`. Asmdef `Game` kalır. Testler `Assets/Game/Tests/`'te (`Game.Tests`, namespace `HoldIt.Tests`). `Game.Editor` ilk editor script'iyle açılır. Geçiş M0'da yapılır | Template kontrol listesinin 7. adımı: asmdef adı `[SerializeReference]` verisine yazılır, sabit kalan ad veriyi kırmaz. Kodda görünen kimlik namespace'tir |

## UI (2026-11-02)
| Konu | Karar | Gerekçe |
|---|---|---|
| UI sistemi | UI Toolkit (UXML/USS) yalnız ekranlar ve HUD için kullanılır. Sabır barı ve konuşma balonu dünyadaki nesnelerdir (sprite/mesh + TMP) | Ekranlar ve HUD yerleşim işidir. Dünyadaki öğeler eğik kamerada karakteri izler ve karakterle birlikte havuzlanır. UI Toolkit'i öğrenmek portföy hedefinin parçası |

### M0'da doğrulanacaklar (template UI Toolkit'le hiç denenmedi)
- Panel input'u root EventSystem'den (UI map) alıyor mu? EventSystem yokken UI Toolkit DefaultInputActions'a düşer ve
  modları ile rebind'i atlar (`Input.md`).
- UI map'i olmayan modda (yükleme boyunca mod None) panel tepki vermiyor mu?
- UI üstünde başlayan bir basış nasıl anlaşılır? (GDD §5: "UI üstünde başlayan basış yok sayılır".)
- UXML'deki localization binding'ini `LocalizationValidator` görmez, çünkü yalnız serialize edilen `Localized*` alanlara
  bakar. Kırık anahtar sessiz kalır.
- Android geri tuşu panelde `UI/Cancel` olarak geliyor mu?

## Görsel ve asset (2026-11-02)
| Konu | Karar | Gerekçe |
|---|---|---|
| Sıra | Önce placeholder (primitive); gerçek modeller oynanış bitince | Oynanış değişirken model işi boşa gider |
| Ortam | Harita verisinden modüler kare parçalarıyla kurulur (1 birim = 1 kare: zemin, duvar, kapı, kabin, dar geçit). Parçalar değiştirilebilir prefab'lardır; harita elle modellenmez | Harita veridir: yeni harita model işi istemez, placeholder'dan gerçek modele geçiş bir prefab değişimidir |
| Lisans | Repo public: yalnız CC0 (Kenney, Quaternius vb.) ya da kendi modellerin. Ücretli paket (Asset Store, Synty) repoya girmez | Ücretli paket lisansları kaynak dosyaların public repoda dağıtılmasına izin vermez |
| Karakterler | Hazır bir CC0 paketten gelir. Thin/Normal/Fat aynı modelin yatay ölçeğiyle yapılır. Ortam kiti belki Blender'da bpy script'iyle yapılır | Tek model, üç beden. Prototip de böyle yapıyor (beden ölçeği 0.75/1.0/1.45) |

## Açık
- ~~**Dil:** GDD §11 "yalnız İngilizce" diyor, template'te Localization modülü var (en/tr örneği). Mobil doğrulama listesindeki
  "System seçicisi cihaz dilini buluyor mu" maddesi tek dille anlamsız kalır. Tek dil seçilirse kontrol listesinin 13.
  adımı modülü çıkarır (Addressables'ı çıkarmak denenmedi). Öneri: modül kalır, en + tr. → M0~~ (2026-11-02: en + tr, modül
  kalır → "Kimlik ve kaynaklar" → Oyun dilleri)
- **Android geri tuşu:** Esc hem `Gameplay/Pause`'a hem `UI/Cancel`'a bağlı (`README.md` → "Açık"). Geri tuşunun menüde,
  oyunda ve pause'da ne yapacağı → M0
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
