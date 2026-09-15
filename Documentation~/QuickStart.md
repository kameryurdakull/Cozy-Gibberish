# Quick Start

## Starter kit

`Tools > Cozy Gibberish > Setup Wizard` şu asset’leri üretir:

- Voice profile
- Cozy/energetic/mechanical/mysterious style preset’leri
- Phonotactic profile
- Prosody profile
- Style stack
- Output profile
- Çift AudioSource içeren player prefab

Mevcut asset’ler overwrite edilmez.

## Character tasarımı

1. Voice profile ile kalıcı ses kimliğini belirleyin.
2. Phonotactic profile ile karakterin hayali dil kurallarını oluşturun.
3. Style preset ile durumsal delivery tasarlayın.
4. Prosody curve ile konuşma boyunca enerji ve pitch akışını yönetin.
5. Output profile ile DSP, spatial, mixer, cache ve queue bütçelerini belirleyin.

`Tools > Cozy Gibberish > Voice Lab` penceresi tüm kombinasyonu runtime renderer üzerinden dinletir.

## Streaming

Output Profile backend değerini `Streaming` yapın. Player, gerektiğinde `GibberishStreamingAudioOutput` bileşenini oluşturur. Streaming backend tam `AudioClip` allocation’ını ortadan kaldırır; ring buffer kapasitesini en uzun hazırlanan repliğe göre ayarlayın.

## Network

`GibberishNetworkRequest` ile text, profile stable ID, style stable ID, expression ve seed taşınır. PCM gönderilmez. Remote tarafta `GibberishAssetRegistry` request’i yerel asset’lere çözer.
