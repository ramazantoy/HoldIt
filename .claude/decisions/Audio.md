# Audio (F6)

Bu modülün kararları, doğrulananları ve inceleme bulguları; çapraz kararlar ve "Açık" listesi `README.md`'de.
Karar değişirse satır silinmez; eskisinin üstü çizilir, yenisi tarihle eklenir.

> Havuz ve tween yaşam döngüsü (aynı faz, F6): `Core.md`. Kanal seslerinin kalıcılığı ve `AudioVolumeChanged` (F7): `Settings.md`.

## F6 kararları (2026-10-31)
Pool, tween yaşam döngüsü, Audio. Kapsam: Core (`Pooling/`, `Tweens/`, `EntryPointAudit`, `RootScope`, `ScopeFieldAudit`),
Core.Editor (`DirectCallScan`, `AwaitedTweenValidator`, `DirectInstantiateValidator`, `IValidator.ValidateAssets`), yeni Audio
modülü, Game (`AudioChannels.cs`, `GameRootScope`, `GameMixer.mixer`, üç sahne), `AF.Core` → PrimeTween referansı.
| Konu | Karar | Gerekçe |
|---|---|---|
| Modül yeri | Pool ve tween yardımcısı Core'da (yeni asmdef yok); Audio ayrı modül `AF.Audio` (+Editor, Tests), ~~`builder.UseAudio(mixer, options)`~~ (2026-11-01: F7'de `UseAudio(mixer, messagePipe, options)` → Settings.md "F7 kararları") | Pool'un yeni dış bağımlılığı yok, Audio onu kullanıyor; PrimeTween kilitli stack'te. Audio'nun kendi kurulumu ve validator'ı var |
| Ses API'si | `SoundCue` SO (klipler, çıkış grubu, ses, pitch aralığı, loop, `PlayWhilePaused`, 3D mesafe/rolloff). `ISounds.Play` (2D) / `PlayAt` (3D) → `SoundHandle` (`IsPlaying`, `Stop(fade)`; `PoolLife` üstünde). Her çalışta kaynağın tüm alanları yeniden | Yönlendirme tasarımcının asset'inde. Bilinen tuzaklar: null grup havuzdaki eski grubu bırakır, string grup araması sessiz bozulur |
| Ses sahipliği | `ISounds` root'ta Scoped (`ScopeSounds`): scope dispose'unda sahip olduğu sesler anında durur. Voice'lar tek root havuzunda (kodla kurulan kapalı şablon), çalarken `Audio` altında. `IGlobalSounds` (root singleton) sahneyi aşacak ses için (inceleme) | Loop'lar sahne değişiminde sızmaz, additive sahne kendininkileri götürür; unload loading screen altında, fade gereksiz |
| Fade-out | `Stop(fade)` voice'u "stopping" işaretler, `Tick` fade bitince (ya da öldürülünce) havuza alır; OnComplete yok (inceleme) | Tween'in bitişi havuza tek dönüş yolu olmaz (kendi kuralımız); dışarıdan `Tween.StopAll` sesi takılı bırakırdı |
| Pause | `AudioListener.pause` `IPauseService`'i yansıtır, tek yazıcı Audio (Editor + dev'de dışarıdan değişim `Log.Error`), dispose'da false. Müzik ve `PlayWhilePaused` `ignoreListenerPause` + unscaled fade; oyun sesi fade'i ölçekli. Bitiş kontrolü listener pause'undaki kaynakta atlanır | Varsayılan güvenli (unutulan kaynak da durur); timeScale tek-yazıcı deseni |
| Müzik | `IMusic {Play(clip, fade), Stop(fade), Current}`, root'ta A/B kaynak crossfade, unscaled, Music grubu, aynı klip no-op, son çağrı kazanır; fade sonu sahiplik kontrolü (güncel parçanın kaynağı susturulmaz). Ses cihazı değişiminde/`AudioSettings.Reset`'te güncel parça sonraki karede yeniden başlar (inceleme) | Tek "çalan parça" durumu; Unity cihaz değişiminde tüm kaynakları durdurur, callback reset'in içinden geliyor |
| Kanallar / mixer | `GameMixer.mixer`: Master/{Music, SFX, UI}, exposed `MasterVolume/MusicVolume/SfxVolume/UiVolume`. `AudioChannel(group, volumeParameter)`, oyunun listesi `AudioChannels.cs` (`AudioOptions { Music }.Add(...)`). `UseAudio` grubu tam adla ve parametreyi kurulumda doğrular (mevcutları sayan hata). `IAudioVolumes` lineer 0–1 → dB (≤0.0001 → -80), `Start`'ta ve sonra her `Set`'te | F7 kanal listesini dolaşır. Exposed parametre string; yanlışı build anında patlar. `SetFloat` Awake'te tutmuyor |
| Mute / odak | `Mute(reason)` handle → `AudioListener.volume`. `MuteWhenUnfocused` (varsayılan açık, yalnız player) `Application.focusChanged` ile handle. Mobil arka plan için kod yok | Reklam/video müziği de susturmalı. Editor CLI yüzünden çoğu zaman odaksız |
| AudioListener | Tek listener root'ta (`Audio/Listener`), `IPostLateTickable`'da hedefi (`IAudioListener.Follow`, yoksa `Camera.main`) izler. Üç sahnenin kamerasındaki listener silindi; `AudioValidator` sahne/prefab'da listener'ı ve çıkışsız AudioSource'u hata sayar, SoundCue'ları `ValidateAssets`'te | F5 sırasında eski sahne gidince yenisi gelene kadar listener yoktu; additive sahne ikinci listener getirirdi |
| Mixer'ın yaratılması | Scratchpad editor kodu: menünün `DoCreateAudioMixer.Action`'ı, `CreateNewGroup` + `AddChildToParent`, `AddExposedParameter` + ad, `views`'a "View" | Public API yok; elle YAML yasak. Sonuç menüden yaratılanla aynı yapıda |
| Yeni mesaj / asset | ~~Mesaj yok~~ (2026-11-01: F7'de `AudioVolumeChanged` → Settings.md "F7 kararları"); template'te ses dosyası, cue ve prefab yok, yalnız mixer | IL2CPP struct mesaj riski eklenmez; minimal |

## Doğrulananlar (F6, kaynaktan, Editor'da ve Windows IL2CPP dev build'inde)
| Konu | Sonuç → kural |
|---|---|
| Testler | 327/327 (+59). Sonrası 13 error + 2 warning (yeni beklenen: havuz tween sızıntısı, AudioListener dışarıdan değişti, klipsiz cue). ValidateAll 0, `Builder.Check(Development)` 0 error (placeholder uyarısı) |
| Play (Editor) | Intro'dan Play: tek listener `RootScope(Clone)/Audio/Listener`, 8 voice prewarm. Pause'da başlatılan ses pause boyunca havuza dönmedi, resume'da çaldı ve bitti; `PlayWhilePaused` ses pause'da bitti. Eski handle yeni yaşamı durdurmadı; fade'le stop havuza döndü. Ses 0.5 → `MusicVolume` -6.02 dB. Müzik crossfade sonunda eski kaynak susturuldu. `Load<PlayingState>(Gameplay)` kare kare: her karede 1 listener, Menu'nün loop'u unload'da durdu, root loop'u ve müzik sürdü. `Load(Gameplay)` reload: sahne havuzu instance'ları (DDOL'a taşınan dahil) yok, root instance'ı sahneyle öldü → sayaç 0 + uyarı. Token'a bağlı tween despawn'dan sonra yazmadı, bağlanmamış tween spawn'da hata verdi. `IGlobalSounds` sahneden root'unkini verdi. Play durdurma 0 error |
| `AudioSettings.Reset` (Editor) | Callback reset'in içinde senkron; kodla yaratılan klipler (stream'li dahil) reset'ten sonra çalmıyor, kaynak sağlam; geçici import edilmiş WAV'da müzik sonraki karede yeniden çaldı (asset sonra silindi) |
| Dev build (IL2CPP, 10:07) | Player.log: `Audio: mixer GameMixer, channels Master, Music, SFX, UI, 8 voices` (generic `PrefabPool<SoundVoice>`, Scoped resolve, `InjectGameObject`), Intro → Menu aynı sıra, exception/AOT yok; pencere odağı değişince `Audio muted: unfocused` / `Audio unmuted`; quit'te Menu scope temiz dispose; kayıt klasörü değişmedi. IL2CPP stack'inde `VoicePlayer:get_Count` görünen satır özdeş metot birleştirmesi (Info logunun stack'i). Build inceleme düzeltmelerinden önce alındı; düzeltmeler Editor'da doğrulandı |
| Doğrulanamayan | Player'da ses çalma (UI yok), gerçek cihaz değişimi, mobil arka plan |

## F6 incelemesi (bağımsız, git diff üzerinden, 2026-10-31)
| Bulgu | Düzeltme |
|---|---|
| Fade'le durdurulan loop'un tek dönüş yolu OnComplete'ti | `Stopping` + `Tick`; test (fade öldürülünce de biter) |
| Ses cihazı değişiminde müzik sessiz kalır | `OnAudioConfigurationChanged` → sonraki karede yeniden çal; Editor'da import edilmiş klipte doğrulandı |
| Audio.md sahne kodunun root `ISounds`'una ulaşabileceğini söylüyordu | `IGlobalSounds` (root singleton); scope testi gerçek kayıtlarla |
| Nit'ler: voice'lar DDOL kökünde; Core.md'de çift token, sahne token'ı, aktivasyon notu; yanlış yerde XML özeti; box mesajı; SoundCue taraması her Play'de; `For` farklı `T` | Voice'lar `Audio` altında; dokümanlar düzeltildi; özet taşındı; mesaj "boxing"i de söylüyor; cue taraması `ValidateAssets`'te; `For` dokümanı. `TweenLeakAudit` maliyeti ve farklı `T` "Açık"ta |

## Template doğrulaması (2026-11-01)
| Bulgu | Düzeltme |
|---|---|
| Odak susturması mobil player'da da açıktı; Unity dokümanına göre Android'de ekran klavyesi odağı alır → klavye, bildirim perdesi, izin/satın alma diyaloğu oyunu susturur | Odak yalnız masaüstü player'da izlenir (`AudioVolumes.FollowsFocus`); mobil arka planı Unity'nin kendi duraklatması, reklam `Mute(reason)`; test, Audio.md. Cihazda doğrulanmadı ("Açık") |
| `AudioListener.volume`'un dışarıdan değişimi ve boş klip slotu testsizdi | Testler (+1 beklenen error logu) |
| Audio.md "mixer'da olanları listeler" yalnız gruplar için doğru; runtime'da exposed parametre listesi için public API yok | Doküman |
| Kabul (değişiklik yok) | `AudioRig.cs:14` Unity objesinde `?.` (scope build anında canlı); `AF.Audio.asmdef`'in UniTask referansı kullanılmıyor; `VoicePlayer`'ın modül içinde somut inject'i |
