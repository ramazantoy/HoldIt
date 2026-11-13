# Platform (F4)

Bu modülün kararları, doğrulananları ve inceleme bulguları; çapraz kararlar ve "Açık" listesi `README.md`'de.
Karar değişirse satır silinmez; eskisinin üstü çizilir, yenisi tarihle eklenir.

> F4'ün build tarafı: `Build.md`. Steam modülü (F8) ertelendi; notları `README.md` → "Açık".

## F4 kararları (2026-10-31)
| Konu | Karar | Gerekçe |
|---|---|---|
| Modüller | Build yalnız Editor: `Assets/Framework/Build/{Editor,Tests/Editor}`, `AF.Build.Editor` (→ AF.Core, AF.Core.Editor, `Mono.Cecil.dll`), `AF.Build.Tests`. Platform: `Assets/Framework/Platform/{Runtime,Tests/Editor}`, `AF.Platform` (→ Core, VContainer, UniTask, MessagePipe), `AF.Platform.Tests`; Mock aynı asmdef'te. `com.unity.nuget.mono-cecil` 1.11.6 doğrudan bağımlılık | Build'in dış bağımlılığı (Cecil) var, runtime'a girmiyor. Mock varsayılan backend, ayrı asmdef gerekçesi yok. Steam F8'de ayrı asmdef |
| Platform | `IAchievements {Unlock, IsUnlocked}`, `IPlatformOverlay {IsOpen}` + `PlatformOverlayChanged`. `IPlatformBackend.Install`; `builder.UsePlatform(messagePipe, options, backend)` null → `MockPlatform`, sözleşme eksikse adını veren hata. `OverlayPause` açıkken `platform-overlay` pause handle'ı. Bilinmeyen achievement id `Log.Error`. Mock'lar kendi tipleriyle de kayıtlı (`MockPlatformOverlay.SetOpen`) | Bilinen tuzaklar: eksik case sessizce Mock'a düşer, unlock Steam hazır değilken düşer. Kimlik, DLC, presence, stat, bulut sonra (tüketicisi yok) |

## Template doğrulaması (2026-11-01)
| Bulgu | Düzeltme |
|---|---|
| Bilinmeyen achievement id kontrolü yalnız `MockAchievements`'ta; mağaza backend'i yazmazsa yazım hatası sessiz kalır | Kod yok (backend yok); Platform.md "Mock yapar, mağaza backend'i de yapmalı"; "Açık"ta F8 notu |
