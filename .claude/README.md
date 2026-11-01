# .claude/

Claude Code'un bu repoda çalışırken kullandığı her şey. Kurallar kökteki `CLAUDE.md`'de; burası onların dayandığı kayıtlar ve
araçlar. İnsan için framework dokümanı `Assets/Framework/Documentation~/`'de, burada değil.

| Yol | İçerik | Kural |
|---|---|---|
| `decisions/` | Karar kaydı: `README.md` indeks, çapraz kararlar, fazlar ve "Açık" listesi; modül başına dosya (kararlar, doğrulananlar, inceleme bulguları) | Önce indeks, sonra yalnız ilgili modül dosyası. Satır silinmez, üstü çizilir; tarih yazılır |
| `scripts/` | `unity command script run` ile çalışan editor betikleri ve bekleme betiği | Tekrar kullanılan iş buraya; tek seferlik betik oturumun scratchpad'inde kalır |
| `prompts/` | Tek seferlik oturum başlangıç prompt'ları | Gitignored (geçici; template'ten açılan oyuna taşınmamalı). Tekrar kullanılan süreç CLAUDE.md'ye kural ya da `scripts/`'e betik olur |
| `notes/` | Bir işin plan ya da handoff notu, yalnız gerektiğinde (`notes/<konu>/<konu>-plan.md`, `<konu>-handoff.md`) | Gitignored. Kalıcı karar notta kalmaz, `decisions/`'a taşınır; iş bitince not silinir |
| `settings.json` | Ortak izinler: okuma komutları serbest, silme, paket ve ayar değişikliği soruya bağlı | Kişisel yol yazılmaz |
| `settings.local.json` | Bu makineye ait izinler | Gitignored |

## Betikler

| Dosya | Giriş noktaları |
|---|---|
| `scripts/wait-editor.sh` | `bash .claude/scripts/wait-editor.sh [--refresh] [saniye]`: Editor hazır olana kadar bekler, console sayaçlarını yazar |
| `scripts/Refresh.cs` | `ClaudeRefresh.Run`: `AssetDatabase.Refresh` (wait-editor `--refresh` çağırır) |
| `scripts/Validate.cs` | `ClaudeValidate.All` (Validate All), `.Release` / `.Development` (build öncesi kontroller, ~30 sn) |
| `scripts/BuildCli.cs` | `ClaudeBuild.Development`, `.Release`, `.ReleaseWithPlaceholderNames`: Bash arka planda, `--timeout 3600` |
| `scripts/PlayMode.cs` | `ClaudePlayMode.Enter`, `.EnterPaused`, `.Step` (`--args '["10"]'`), `.Exit` (pause'u da temizler) |

`script run` her çağrıda ayrı bir assembly derler: çağrılar arasında static alan paylaşılmaz.

## Bilgi nerede durur

| Tür | Yer |
|---|---|
| Kural (her oturum geçerli) | `CLAUDE.md` |
| Kilitli karar ve gerekçesi, doğrulanan davranış, inceleme bulgusu | `decisions/<Modül>.md`; kritik tuzağın tek satırı ayrıca `decisions/README.md` indeksinde |
| Sistemin nasıl çalıştığı ve nasıl kullanıldığı (source of truth) | `Assets/Framework/Documentation~/<Modül>.md` (insan için, İngilizce) |
| Yarıda kalan işin durumu | `notes/<konu>/<konu>-handoff.md` |
| Koddaki bir yorumla anlatılabilecek şey | Kodun kendisi; buraya yazılmaz |

## Handoff

Bir iş oturum içinde bitmeyecekse ya da bağlam dolmak üzereyse, durmadan önce `notes/<konu>/<konu>-handoff.md` yazılır. Yeni
oturum yalnız bu dosyayla devam edebilmeli:

```
# <Konu> handoff (tarih)
## Durum        hangi plan, hangi adımda; ağaç temiz mi, commit'lenmemiş ne var
## Biten        yapılan ve doğrulanan (komut ve sonucuyla)
## Kalan        sıralı adımlar
## Tuzaklar     bu işte bulunan, koddan görünmeyen davranışlar ve elenen yaklaşımlar
## Sıradaki adım ilk yapılacak tek şey
## Devam prompt'u yeni oturuma yapıştırılacak metin
```

İş bitince kalıcı olan (karar, tuzak, doğrulanan) `decisions/`'a taşınır, handoff dosyası silinir.

## Template'ten yeni oyun

Adım adım kontrol listesi: `decisions/README.md` → "Template doğrulaması" → "Yeni oyun kontrol listesi".
`CLAUDE.md` ve `.claude/` (prompt'lar ve notlar hariç) template'le birlikte gelir. `decisions/` framework'ün kararlarını taşır; oyunun kendi kararları
`decisions/Game.md` gibi ayrı bir dosyaya yazılır, framework dosyaları yalnız framework değişince güncellenir.
