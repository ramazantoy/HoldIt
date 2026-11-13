# Kod stili: F1–F3 kodunun toplu dönüşümü

Bu modülün kararları, doğrulananları ve inceleme bulguları; çapraz kararlar ve "Açık" listesi `README.md`'de.
Karar değişirse satır silinmez; eskisinin üstü çizilir, yenisi tarihle eklenir.

## Stil dönüşümü (2026-10-31)
Kapsam: Core, Save, Input (Runtime/Editor/Tests/Fixtures) ve `Game/Scripts`, 67 .cs; 3 YAML; 3 doküman örneği.
| Konu | Karar | Gerekçe |
|---|---|---|
| Private static alanlar | `static`/`static readonly` private alan da `_camelCase` (`_keyPattern`, `_utf8`) | F4 emsali (`_definedTargets`); "private alan" kuralının istisnası olmasın |
| Sabitler | `private const` string **ve char** UPPER_SNAKE (`INDEX_SEPARATOR`, `SLOT_1`); sayısal ve enum (`BindingFlags`) PascalCase kalır | Char ayraçlar string key'le aynı rolde; enum sabiti "key" değil. En az churn |
| Belirteç | Belirteçsiz her üye ve iç içe tipe `private`, üst düzey tipe `internal` (varsayılanın kendisi; erişim değişmez). Interface/enum üyesi, explicit impl, static ctor hariç | "Erişim açık yazılır" kuralı tipler için de; F4 `internal sealed class` yazıyor |
| `this.` | Yeniden adlandırılan alanın önündeki `this.` silindi | Yalnız parametreyle ad çakışması için vardı |
| Dokunulmayan | Public/protected/internal üyeler, save/test data sınıflarının public camelCase alanları (JSON anahtarı), fixture'ların public `target` alanları, parametre/local adları, string değerleri | API ve kalıcı veri kırılmasın. Fixture alanları hiçbir asset'te yok ve audit/validator'ın public-serialize dalını test ediyor |
| Serialize alanlar | `SceneScope._bootTarget`, `GameRootScope._inputActions`: `[FormerlySerializedAs]` + YAML aynı commit'te yeni adla yeniden yazıldı. Attribute kalır | Repo YAML'ı kodla aynı adı taşısın (değer attribute'a gizli bağımlı kalmasın); attribute reserialize edilmemiş kopyaları korur |
| Dokümanlar | Alan bildirimi/ataması görünen örnekler yeni stile (`Input.md`, `Platform.md`, `Save.md`); çağrı parçaları (`pause.Request(...)`) kaldı | Örnekler kopyalanan kod |
| Yöntem | Roslyn aracı (Unity'nin Roslyn'i, Editor `.rsp`'leriyle asmdef başına derleme, ön koşul 0 error) semantik rename + belirteç; modül modül (Core → Save → Input → Game) her birinde derleme + modül testleri. Bağımsız token-diff doğrulayıcısı HEAD'e karşı yalnız izinli farkı kabul eder (kasıtlı 5 bozulmayı yakaladı) | ~1250 ad + 347 belirteç elle hataya açık; semantik bağlama aynı adlı parametreye dokunmaz |

### Doğrulananlar (stil dönüşümü)
| Konu | Sonuç → kural |
|---|---|
| Sonuç | 1250 ad (184 bildirim + 1066 referans), 347 belirteç, 52 `this.`, 2 attribute; token diff 0 izinsiz fark; console 0 error; 242/242 (sonrası 10 error + 2 warning, beklenen); ValidateAll 0; `Builder.Check(Development)` 0 error (placeholder uyarısı); Menu=`Menu`, Gameplay=`Playing`, root actions = `Controls` (guid ve globalId aynı), sahneler diskten yeniden açılarak |
| `FormerlySerializedAs` + domain reload | Açık sahnedeki değer reload'da korundu (attribute rename'le aynı refresh'te) |
| `AssetDatabase.ForceReserializeAssets` (6.3) | İki sahneyi yeniden yazdı (tek satır: anahtar adı), `RootScope.prefab`'ı yazmadı. Prefab için bileşene `SetDirty` + `SaveAssetIfDirty` → tek satır |
| Editor `.rsp` dosyaları | `Library/Bee/artifacts/*.dag/<asmdef>.rsp` Unity dışı Roslyn derlemesine yetiyor (11 assembly, 0 error) |
| `console status` | `counts` oturum boyu birikir; Editor'ın gerçek console'u `groundTruth.consoleErrors` |

### Stil dönüşümü incelemesi (bağımsız, git diff üzerinden, 2026-10-31)
Bug ya da risk yok. İnceleme kendi araçlarıyla bakmış: token diff, parametre/local kapsam analiziyle bağlama kontrolü, 107
dosyada stil taraması (sonra 0 ihlal, HEAD'de 519), serialize alan ve string referansları (`git grep`'te eski ad yok,
prefab override `propertyPath`'i yok), dokümanlar.
| Bulgu | Düzeltme |
|---|---|
| `ReferenceValidator` mesajı alan adını reflection'dan alıyor: boş actions artık `_inputActions is [Required] but empty` der | Rename'in doğal sonucu, testte metin yok; kalır |
| `Slot1` → `SLOT_1` (`SLOT1` de olabilirdi) | Kural iki yolu da kabul ediyor; kalır |
| CLAUDE.md "eski stilde" maddesi ve Durum satırı | İnceleme başladığında zaten güncelleniyordu; Durum satırı sonda güncellendi |
