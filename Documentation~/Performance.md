# Performance

## Önerilen yapı

- Diyalog açılmadan önce `PrewarmAsync`
- Aynı replikler için LRU cache
- Uzun akışlar için streaming backend
- Mobile için `Mobile` quality tier
- Çoklu NPC için 1–3 concurrency render scheduler

## Memory

Mono PCM yaklaşık olarak `sampleCount × 4 byte` kullanır. Output Profile hem entry sayısı hem toplam byte bütçesi uygular. Bütçeyi aşan en eski cache kaydı silinir.

## Audio thread

Streaming callback allocation, lock ve Unity object erişimi yapmaz. Main thread tek producer, audio thread tek consumer’dır. Buffer yetersizse callback kalan alanı silence ile doldurur.

## Test kapsamı

- Determinism
- Unicode/rich-text source mapping
- Phonotactic repetition ve archetype davranışı
- DSP sample safety ve spectral energy
- Cancellation
- Cache eviction
- Network codec
- Quality tiers
- Render budget
- PlayMode pause/resume/seek/interrupt/queue lifecycle
