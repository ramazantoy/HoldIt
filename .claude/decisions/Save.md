# Save (F2)

Bu modülün kararları, doğrulananları ve inceleme bulguları; çapraz kararlar ve "Açık" listesi `README.md`'de.
Karar değişirse satır silinmez; eskisinin üstü çizilir, yenisi tarihle eklenir.

> Sahne değişimine bağlı Save kararları (menüye dönüşte `CloseSlot`, reload, PlayTime): `Core.md` → "F5 kararları".
> Ses ve ekran ayarlarının savable'ları: `Settings.md`; dilin savable'ı: `Localization.md`.

## Save
- Şema versiyonlu (`schemaVersion`); yüklemede migration zinciri, adım başına tek fonksiyon ve test.
- Enum'lar string yazılır (StringEnumConverter); TypeNameHandling kapalı.
- Global `JsonConvert.DefaultSettings` asla set edilmez; save kendi settings örneğini kullanır (global ayar Unity araçlarını da bozuyor).
- Savable kaydı otomatik; root/sahne scope sınırında sahne scope'u kendi savable'larını root registry'ye kaydeder; ~~kayıtsız kalanı validator yakalar~~ (2026-11-01: yanlış kayıt scope build'inde `SavableAudit`, data sınıfı `SaveValidator`; hiç kaydedilmeyen savable'ı yakalayan kontrol yok → "F2 kararları").
- Atomik yazma (tmp → yerine koy) + `.bak`; okuma hatasında slot asla otomatik silinmez.
- Tek yazıcı (kuyruk); gerçek sonuç çağırana döner.
- Global veri (ayarlar, achievement) ile slot verisi ayrı.
- Mobil: uygulama arka plana geçince (`OnApplicationPause(true)`) kaydetme kancası.
- (2026-10-30) Uygulama ayrıntıları: "F2 kararları".

## F2 kararları (2026-10-30)
| Konu | Karar | Gerekçe |
|---|---|---|
| Modül | `Assets/Framework/Save/{Runtime,Editor,Tests/Editor}`, asmdef `AF.Save`, `AF.Save.Editor`, `AF.Save.Tests`. Oyun: `builder.UseSave(options)` | Core Save'i bilmez; oyunda tek satır |
| Savable | Non-generic `ISavable` + `Savable<TData>` taban sınıfı (`Key`, `Document`, `Capture`, `Restore`, `CreateDefault`). Singleton + `AsImplementedInterfaces`. Tek restore yolu: `Restore(bölüm ?? CreateDefault())` | Generic interface'te default method'un IL2CPP davranışı doğrulanmadı; taban sınıf risksiz. Yeni oyun, yükleme, sahne açılışı aynı yol |
| Scope bağlantısı | Core'a `ISceneScopeExtension`; her `SceneScope` entry point'lerden önce çağırır. Save'in uzantısı `SavableScopeLink` ekler: build'de kendi savable'larını kaydeder (parent'ınkileri atlar), slot açıksa restore eder; dispose'da belleğe capture edip çıkarır. Root'u `UseSave` bağlar | Kayıt otomatik; restore sahnenin `Initialize`'ından önce; seviye değişiminde veri kaybolmaz |
| Yanlış kayıt | `SavableAudit` (scope build, Editor + dev): `ISavable` kontratı olmadan ya da singleton dışı kayıt hata. Reflection `RegistrationAudit`'e (Core) taşındı, `EntryPointAudit` de onu kullanır. `SaveValidator`: MonoBehaviour/SO savable ve data sınıfı kuralları | "Hata yok ama kaydedilmiyor" sınıfı |
| Dokümanlar | Slot, Global (cloud), Device (cloud dışı); her birinin kendi `SaveSchema` zinciri | Eski oyunda Deck/PC ayarları birbirini eziyordu |
| Dosya düzeni | `saves/global.json`, `saves/slot-<id>.json`, `local/device.json`; `.bak`, `.tmp`, `.corrupt`. Format `{schemaVersion, meta{savedUtc, appVersion, playTimeSeconds}, sections}`, indent'li UTF-8 | Auto-Cloud tek desen; elle okunur |
| Slot modeli | String id (`^[a-z0-9][a-z0-9_-]{0,31}$`), sayı sınırı yok. Bellekte tek oturum: `LoadSlot`/`NewSlot`/`SaveSlot(id)` ("save as")/`CloseSlot`/`DeleteSlot`/`ListSlots` (+`TryReadSection` özet için). Yüklü olmayan sahnelerin bölümleri korunur. Oynama süresi meta'da | Eski sistem 4 sabit slot ve global değişen `ActiveSlotId` idi |
| Depolama | Senkron `ISaveStorage` (`IsThreadSafe` threading'i belirler). `FileSaveStorage` varsayılan (tmp → `Flush(true)` → `File.Replace`, IO retry), `PlayerPrefsSaveStorage` opsiyonel (index key, main thread) | PlayerPrefs isteği; F8 bu araya takılır |
| Kuyruk | Tek `SaveQueue`: sırayla; thread-safe depoda thread pool, sonuç main thread'de; bekleyen aynı-entry yazması birleşir (ardında okuma/silme yoksa); çağıran iptali yazmayı düşürmez. Capture main thread'de, aynı karede | Eski sistem eşzamanlı istekleri düşürüyordu |
| Suspend/quit | Core'a `AppSuspending` (root: `OnApplicationPause(true)`, `Application.quitting`). Save değişen global/device'ı, `AutosaveSlotOnSuspend` açıksa aktif slotu kuyruğa alır, `FlushSync` ile o karede bitirir. Default'lar dosyaya yazılmaz | Process donabilir; sonraki sürümün yeni default'ları uygulanabilsin |
| Okuma hataları | `RecoveredFromBackup` (ana `.corrupt`'a taşınır), `Corrupt` (kopya, yerinde kalır), `TooNew`, `ReadFailed`, `Failed` (migration/data sınıfı hatası, dosyaya dokunulmaz, hiçbir savable restore edilmez), `NotFound`. Global/device hiçbirinde boot'u durdurmaz; TooNew/ReadFailed/Failed'da oturum boyunca salt okunur | Okunamayanı silen/ezen eski davranışın tersi |
| Sonuç | `SaveResult {Written, Skipped, Refused, Failed}`; capture hatası `SaveDataException` fırlatır | UI yalnız `IsWritten`'da "kaydedildi" der |
| Dev oturumu | Playing'e slot olmadan girilince bellekte, yazılmaz; build'de `Log.Error` | Her sahneden Play |
| Slotsuz oyun | `SaveOptions.UsesSlots = false`: dev oturumu ve build'deki `Log.Error` yok; `ListSlots`/`LoadSlot`/`NewSlot`/`SaveSlot`/`DeleteSlot` `InvalidOperationException` fırlatır, `CloseSlot` zararsız. Global ve device aynen | İlerlemesi Global'de tutulan oyunda (HoldIt) her seviye girişinde yanlış alarm olurdu |
| Şifreleme/sıkıştırma | Yok; isteyen `ISaveStorage` dekoratörü yazar | Tek oyunculuda koruma sağlamaz, debug/mod/desteği zorlaştırır |
| JSON | `SaveJson`: StringEnum, TypeNameHandling None, ObjectCreationHandling Replace, DateParseHandling None, invariant culture, Unity struct converter'ları | Newtonsoft tuzakları (liste ikilenmesi, tarih string'i bozulması, Vector3 döngüsü, tr-TR) |

## Doğrulananlar (F2, kaynaktan ve Editor'da)
| Konu | Sonuç → kural |
|---|---|
| VContainer child koleksiyon | `IReadOnlyList<T>` child scope'ta parent'ın kayıtlarını da döndürür (`CollectFromParentScopes`) → link zaten kayıtlıları atlar |
| VContainer build callback sırası | Kayıt sırasıyla çalışır; entry point dispatcher'ı ilk `RegisterEntryPoint` ekler, `RegisterEntryPointExceptionHandler` eklemez → sahne uzantıları handler'dan sonra, `RegisterEntryPoint`'ten önce |
| VContainer dispose | `CompositeDisposable` LIFO; scope'ta kaydedilen IDisposable singleton scope'la dispose olur |
| Unfocused Editor Play | `QueuePlayerLoopUpdate` kare ilerletmedi; `isPaused = true` + `EditorApplication.Step()` ilerletir |

## Template doğrulaması (2026-11-01)
| Bulgu | Düzeltme |
|---|---|
| `Restore` fırlatınca yüklemede (`Activate`) loglanıp devam ediliyor, sahne açılışında (`Register`) scope build'i düşüyordu | İkisi de loglar ve devam eder (`SaveRegistry.Restore`): bir bozuk savable sahneyi açılmaz yapmasın; test (+1 beklenen exception logu). Bölüm data sınıfına uymazsa sahne scope'u yine düşer (`ReadSection`) |
| `SaveValidator`: ScriptableObject savable, abstract ve `object` üye testsizdi | Testler |
| Save.md: key kuralı (ilk karakter harf/rakam, ≤64), slot id'nin ilk karakteri, `SaveGlobal`'da global capture fırlatınca device'ın yine yazılması, mobilde OS yedeğinin (iCloud, Android Auto Backup) `local/`'i taşıyabilmesi, masaüstünde odak kaybının da suspend olması | Doküman |
| Kabul (değişiklik yok) | `.corrupt`'ın üzerine yazılması (`ISaveStorage` yorumunda yazılı); `Corrupt` global/device yazılabilir kalır, ilk suspend'de varsayılanla yazılır (okunamayan kopya `.corrupt`'ta); `PlayerPrefsSaveStorage.Read` try'sız (sözleşme `ISaveStorage`'da); `SaveService`/`SaveRegistry`'nin modül içinde somut tipiyle inject'i |
