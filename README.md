# Cozy Gibberish 2

Cozy Gibberish; örnek ses dosyası zorunluluğu olmadan, karaktere özgü ve deterministik gibberish konuşma üreten production odaklı Unity paketidir. Prosedürel formant sentezini phonotactic gramer, prosody curve, sample grain, DSP-time playback ve güçlü authoring araçlarıyla birleştirir.

## Temel yetenekler

- Weighted onset–vowel–coda phonotactic gramer
- Unicode grapheme, Türkçe karakter ve rich-text tokenizer
- Vowel harmony, tekrar engelleme ve farklı dil karakterleri
- Phrase boyunca pitch, energy ve pace prosody curve’leri
- Weighted `GibberishStyleStack` ve Voice A/B morph
- PolyBLEP oscillator, formant bank, oversampling ve hybrid sample grain
- Cancellation-aware worker-thread rendering
- DSP-time `PlayScheduled` ve equal-power dual-source crossfade
- DSP timestamp taşıyan syllable, word, phrase ve mouth-shape event’leri
- Pause, resume, seek, priority queue ve overflow politikaları
- Thread-safe LRU PCM cache ve prewarm API
- Audio-thread-safe streaming ring buffer backend
- AudioMixer routing, ducking, reverb ve 3D spatial ayarlar
- Network için seed/profile kimliği tabanlı deterministik request codec
- Voice Lab, batch WAV bake ve profile migration araçları

Core assembly’nin üçüncü parti bağımlılığı yoktur.

## Gereksinimler

- Unity 2022.3 veya daha yeni
- Core paket için ek dependency gerekmez
- Opsiyonel integration sample’ları için ilgili framework host projede kurulu olmalıdır

## Kurulum

Unity Package Manager’da `+ > Install package from git URL...` seçeneğini açın ve kararlı sürüm etiketini kullanın:

```text
https://github.com/kameryurdakull/Cozy-Gibberish.git#v2.0.0
```

Alternatif olarak `Packages/manifest.json` dosyasına ekleyebilirsiniz:

```json
{
  "dependencies": {
    "com.kamer.cozygibberish": "https://github.com/kameryurdakull/Cozy-Gibberish.git#v2.0.0"
  }
}
```

Yerel geliştirme için `Add package from disk...` ile depo kökündeki `package.json` dosyasını seçebilirsiniz. `.tgz` dağıtımları da `Add package from tarball...` ile desteklenir.

Ardından:

1. `Tools > Cozy Gibberish > Setup Wizard` açın.
2. Voice archetype ve style seçeneklerini belirleyin.
3. Voice, phonotactics, prosody, style stack, output profile ve player prefab oluşturun.
4. `Tools > Cozy Gibberish > Voice Lab` ile sesi tasarlayın.

## Runtime kullanımı

```csharp
var request = new GibberishSpeechRequest(
    text: "Welcome home, little traveler.",
    voice: cozyVoice,
    style: cozyStyle,
    expression: new GibberishExpression(
        energy: -0.1f,
        warmth: 0.45f,
        pace: -0.15f,
        pitch: 0f),
    seed: 42,
    priority: 5,
    playbackPolicy: GibberishPlaybackPolicy.Enqueue,
    phonotactics: cozyLanguage,
    prosody: gentleEnding);

var handle = player.Speak(request);
```

Playback kontrolü:

```csharp
player.Pause();
player.Seek(0.5f);
player.Resume();
player.Cancel(handle);
```

Önden hazırlama:

```csharp
var prepared = player.Prewarm(request);
player.PlayPrepared(prepared);
```

UniTask sample’ı kurulduğunda:

```csharp
var prepared = await player.PrewarmAsync(request, destroyCancellationToken);
player.PlayPrepared(prepared);
```

## DSP-time event’leri

```csharp
private IDisposable _mouthSubscription;

private void OnEnable()
{
    _mouthSubscription = player.Events.Subscribe<GibberishMouthCueEvent>(
        cue => ScheduleMouthPose(
            cue.Shape,
            cue.Intensity,
            cue.DspTime));
}

private void OnDisable()
{
    _mouthSubscription?.Dispose();
}
```

Event, callback anını değil hedef `AudioSettings.dspTime` değerini taşır. Böylece görsel sistemler event lookahead’i kullanarak audio ile senkron animasyon planlayabilir.

## Authoring araçları

- **Setup Wizard:** Tam starter kit ve iki kaynaklı prefab üretir.
- **Voice Lab:** Waveform, spectrum, A/B morph, seed galerisi, expression preview ve maliyet ölçümü.
- **Batch Bake:** `GibberishBakeManifest` içeriğini WAV dosyalarına dönüştürür.
- **Validate and Migrate Profiles:** Stable ID ve schema sürümlerini günceller.

## Opsiyonel integration define’ları

| Integration | Define |
|---|---|
| UniTask | `COZY_GIBBERISH_UNITASK` |
| VContainer | `COZY_GIBBERISH_VCONTAINER` |
| DOTween | `COZY_GIBBERISH_DOTWEEN` |
| TextMeshPro | `COZY_GIBBERISH_TMP` |
| Unity Localization | `COZY_GIBBERISH_LOCALIZATION` |
| Timeline | `COZY_GIBBERISH_TIMELINE` |
| Addressables | `COZY_GIBBERISH_ADDRESSABLES` |
| Burst/Jobs | `COZY_GIBBERISH_BURST` |

Yarn Spinner ve Ink için dependency-free `GibberishDialogueBridge` public command metotları sağlanır.

## Kalite profilleri

- `Mobile`: En fazla 32 kHz ve oversampling kapalı
- `Balanced`: Voice profile ayarlarını kullanır
- `Cinematic`: En az 48 kHz ve 2x oversampling

Yoğun NPC kullanımında UniTask scheduler, prewarm ve cache birlikte kullanılmalıdır. Kesintisiz uzun yayınlar için streaming backend tercih edilebilir.

Detaylar için:

- [Quick Start](Documentation~/QuickStart.md)
- [Architecture](Documentation~/Architecture.md)
- [Advanced Systems](Documentation~/Advanced.md)
- [Integrations](Documentation~/Integrations.md)
- [Performance](Documentation~/Performance.md)
- [GitHub Publishing](Documentation~/Publishing.md)
