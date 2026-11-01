# Unity Oyun Şablonu

[English](README.md)

PC ve mobil oyunlar için bir Unity 6.3 (URP) proje şablonu. Her oyunda yeniden yazılan sistemler ilk günden hazır:
bağımlılık enjeksiyonu scope'ları, oyun durumları, sahne yükleme, kayıt, input modları, ses, nesne havuzları, oyuncu
ayarları, yerelleştirme, platform servisleri ve kontrollü bir build hattı. Yeni oyun GitHub'ın **Use this template**
düğmesiyle başlar ve bunun üstüne kurulur.

## Ne değildir

- **UI yok.** Menü, HUD ya da ayarlar ekranı yok. Sistemler bir UI'ın ihtiyaç duyduğu servisleri ve mesajları sunar.
- **Oynanış sistemi yok.** Karakter, savaş, envanter gibi sistemler her oyunun kendisine ait.
- **Paket değil.** Framework `Assets/Framework/` altında kaynak koddur ve her oyuna kopyalanır. Oyunlar arasında güncelleme
  akışı yok; bir oyunda yapılan düzeltme o tek klasör karşılaştırılarak şablona geri taşınır.

## Gereksinimler

Unity **6000.3.27f2** (6.3 LTS), Universal Render Pipeline 17.3. Build için platformun build desteği gerekir: Windows için
Windows Build Support (IL2CPP), Android için Android Build Support.

| Paket | Sürüm | Kaynak | Kullanım |
|---|---|---|---|
| VContainer | 1.19.0 | OpenUPM | Bağımlılık enjeksiyonu |
| UniTask | 2.5.11 | OpenUPM | Async |
| MessagePipe, MessagePipe.VContainer | 1.8.2 | OpenUPM | Mesajlar |
| PrimeTween | 1.5.2 | npmjs | Tween'ler |
| Newtonsoft Json | 3.2.2 | Unity | Kayıt dosyaları |
| Input System | 1.20.1 | Unity | Input |
| Localization (Addressables 2.10.3 ile gelir) | 1.5.13 | Unity | Diller |
| Mono.Cecil | 1.11.6 | Unity | Build kontrolleri (yalnız Editor) |
| Pipeline | 0.9.1-exp.1 (deneysel) | Unity | Unity CLI'ın Editor'a ve Development build'lere bağlanması |

OpenUPM paketleri imzasız: proje ilk açıldığında Unity eksik imza için soru sorar.

## Modüller

Her modül `Assets/Framework/` altında tek klasör ve kendi assembly'leri var (`AF.<Modül>`, ayrıca `.Editor` ve `.Tests`).

| Modül | Ne yapar | Gerektirir | Doküman (İngilizce) |
|---|---|---|---|
| Core | Root ve sahne scope'ları, oyun durumları, sahne yükleme, pause ve oyun saati, boot adımları, nesne havuzları, tween kuralları, log, doğrulama | — | [Core.md](Assets/Framework/Documentation~/Core.md) |
| Save | Migration'lı, sürümlü JSON kayıtlar; atomik yazma, yedek ve tek yazma kuyruğu; slotlar, global ve cihaz verisi | Core | [Save.md](Assets/Framework/Documentation~/Save.md) |
| Input | Action map'lerin ve cursor'ın tek sahibi olarak input modları, kontrol şeması takibi, tuş atama | Core, Save | [Input.md](Assets/Framework/Documentation~/Input.md) |
| Audio | Havuzlanmış kaynaklarda ses cue'ları, crossfade'li müzik, mixer kanal sesleri, susturma, tek AudioListener | Core | [Audio.md](Assets/Framework/Documentation~/Audio.md) |
| Settings | Ekran ayarları ve kanal seslerinin kaydı | Core, Save, Audio, Input | [Settings.md](Assets/Framework/Documentation~/Settings.md) |
| Localization | Unity Localization ile oyunun dili, global veriyle birlikte kaydedilir | Core, Save | [Localization.md](Assets/Framework/Documentation~/Localization.md) |
| Platform | Küçük arayüzlerin arkasında başarımlar ve mağaza overlay'i; Mock backend | Core | [Platform.md](Assets/Framework/Documentation~/Platform.md) |
| Build | Build öncesi ve sonrası kontrolleriyle tek build menüsü (yalnız Editor) | Core | [Build.md](Assets/Framework/Documentation~/Build.md) |

`Assets/Game/` yer tutucu oyundur: `Game` assembly'si, root scope prefab'ı, Intro, Menu ve Gameplay sahneleri, ses mixer'ı,
input action'ları ve örnek yerelleştirme asset'leri. **AF > Validate All** projeyi ve build sahnelerini kontrol eder; build
menüsü de onu çalıştırır.

## Yeni oyuna başlamak

1. Şablondan bir repo oluştur (**Use this template**) ve klonla.
2. Unity 6000.3.27f2 ile aç; OpenUPM paketleri için eksik imza diyaloğunu kabul et.
3. Gereken build desteğini kur (Windows IL2CPP; Android için SDK, NDK ve OpenJDK'siyle Android Build Support). Platformu
   **File > Build Profiles**'tan değiştir; build menüsü etkin platformu build eder, platformu kendisi değiştirmez.
4. **Player Settings**'te Company Name, Product Name ve Version'ı ayarla. `persistentDataPath` ve uygulama kimliği
   (`com.<Company>.<Product>`) bunlardan türer; ürün adında boşluk ya da simge varsa Player Settings'te görünen kimliği
   kontrol et.
5. **AF > Build > Check Release** çalıştır: yer tutucu ad hatası kalmamalı.
6. `Assets/Game/Scripts/` içindeki `Game` namespace'ini ve `Game` assembly'sinin Root Namespace'ini değiştir. Assembly adı
   aynı kalır.
7. Gerekmeyen modül varsa çıkar: `GameRootScope`'taki `Use…` satırı, klasörü ve `Game.asmdef`'teki referansı (Localization
   için ek adımlar [Localization.md](Assets/Framework/Documentation~/Localization.md)'de).
8. Sahneler: Intro (build index 0, Boot Target Manual), Menu (Boot Target Menu), Gameplay (Boot Target Playing). Sahne
   eklerken ya da adını değiştirirken build'in sahne listesini ve `Assets/Game/Scripts/Scenes.cs`'i birlikte güncelle:
   ikisini karşılaştıran bir kontrol yok, eksik ad ilk yüklemede hata verir. Sahne ekleme:
   [Core.md](Assets/Framework/Documentation~/Core.md).
9. Root prefab `Assets/Game/Prefabs/RootScope.prefab`: oyunun root scope'u ve tek EventSystem. Loading screen de buraya.
10. Ses: mixer'ın grupları (`Assets/Game/Audio/GameMixer.mixer`) ile `AudioChannels.cs` eşleşir.
11. Input: `Assets/Game/Input/Controls.inputactions`'ın map'leri ile `InputModes.cs`'teki modlar eşleşir. Şablonda Escape hem
    `Gameplay/Pause`'a hem `UI/Cancel`'a bağlı; oyunun ne istediğine karar ver ([Input.md](Assets/Framework/Documentation~/Input.md)).
12. Yerelleştirme: örnek dilleri (İngilizce, Türkçe) ve `UI` tablosundaki `menu.play` girdisini değiştir; Specific Locale
    Selector oyunun ana dilini göstersin. Tek dilli oyun modülü çıkarabilir.
13. Mobil: Default Orientation'ı ayarla (şablon dört yönü de açık bırakır). Android API seviyesi 25, ARM64, IL2CPP ve Minimal
    stripping ayarlı; imzalama ayarlı değil.
14. Kalite seviyeleri: Android ve iOS'ta varsayılan Mobile, Windows, macOS ve Linux'ta PC.
15. Console'da hata olmadığını, EditMode testlerinin Test Runner'da geçtiğini ve **AF > Validate All**'ın bir şey
    raporlamadığını kontrol et.
16. **AF > Build > Development** ile build al ve çalıştır, sonra **AF > Build > Release**. İlk build URP'nin pipeline
    asset'lerini yeniden yazabilir; bu değişiklikleri ayrı commit'le.

## Platform durumu

**Windows (IL2CPP, managed stripping Minimal): doğrulandı**, Development ve Release build'leriyle. Player'da: root scope ve
boot adımları, global ve cihaz kayıt verisinin okunup geri yazılması (kayıtlı tuş atamaları dahil), intro'dan menüye sahne
akışı, MessagePipe üzerinden struct mesajlar, ses mixer'ı ve ses kaynağı havuzunun kurulumu, pencere odakta değilken
susturma, kayıtlı ekran ayarlarının ve dilin açılışta uygulanması, Unity Localization içeriğinin build'de olması ve Release
build'de Pipeline runtime'ının olmaması. Şablonda UI olmadığı için player'da denenmeyenler: oynanıştan slot kaydı, ses
çalma, pause ve overlay mesajları. Exclusive fullscreen denenmedi.

**Mobil: doğrulanmadı.** Kodda mobil yollar var (dokunmatik kontrol şeması, mobil kare hızı varsayılanı, uygulama arka
plana geçerken kayıt, PlayerPrefs kayıt deposu, Android IL2CPP ayarları), ama hiç Android ya da iOS build'i alınmadı ve
hiçbir şey cihazda çalışmadı. Şablondan yapılacak ilk mobil oyun bunu deneyecek; düzeltmeler şablona geri gelir.

**macOS ve Linux:** build alınmadı.

**Steam:** henüz Steam modülü yok. Başarımlar ve overlay Mock backend'de çalışır; mağaza backend'i aynı arayüzlerin arkasına
takılır ([Platform.md](Assets/Framework/Documentation~/Platform.md)).

Development build, Pipeline paketinin kendisine gönderilen kodu çalıştıran yerel HTTP sunucusunu taşır. Kimseye verilmez;
test edenler Release build alır.

## Yazarlık

Mimari, kararlar ve inceleme: Ramazan Toy. Kodun büyük kısmı bu yönlendirmeyle Claude Code ile yazıldı; o commit'lerde
Co-Authored-By satırı var. `CLAUDE.md` ve `.claude/` bu çalışmanın uyduğu kuralları ve karar kaydını tutar.
