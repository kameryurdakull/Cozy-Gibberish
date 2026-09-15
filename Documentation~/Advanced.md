# Advanced Systems

## Phonotactics

Onset ve coda listeleri weighted selection kullanır. Immediate repetition engeli, coda probability, vowel harmony ve syllable pattern ayarları karakterin konuşma imzasını belirler.

Tokenizer:

- Unicode grapheme cluster kullanır
- Combining mark’ları tek karakter olarak değerlendirir
- Rich-text tag’lerini source mapping’den ayırır
- Fantasy modunda hyphen’ı kelime parçası kabul eder

## Style stack

Bir base style üzerine biome, emotion, status effect veya gameplay durumu katmanlanabilir. Multiplicative özellikler neutral `1`, additive özellikler neutral `0` üzerinden blend edilir.

## Hybrid voice bank

`GibberishHybridVoiceBank`, belirli consonant onset’lerine kısa mono/stereo `AudioClip` grain’leri bağlar. Capture sırasında klipler mono immutable buffer’a dönüştürülür; worker renderer grain’i procedural onset ile sample-rate uyumlu olarak harmanlar.

## Speech sequence

`GibberishSpeechSequence` string markup yerine typed segmentler kullanır. Her segment kendi style, expression, seed, priority ve tail pause değerini taşır.

## Profile migration

Voice, style ve phonotactic asset’leri stable ID ve schema version taşır. `Tools > Cozy Gibberish > Validate and Migrate Profiles` tüm project asset’lerini tarar ve eski veriyi current schema’ya yükseltir.
