# Integrations

Integration sample’ları Package Manager içinden ayrı ayrı import edilir. İlgili package kurulduktan sonra define eklenir.

## UniTask

Capture main thread’de, render bounded scheduler üzerinden worker thread’de, playback tekrar main thread’de çalışır. Cancellation token DSP loop’una kadar iletilir.

## VContainer

Planner, synthesizer ve event bus singleton; player component ise hem `IGibberishSpeechService` hem `IAdvancedGibberishSpeechService` olarak register edilir.

## DOTween

Mouth cue hedef DSP zamanı kadar delay alır ve equal-power audio schedule ile uyumlu konuşma hareketi üretir.

## TMP

Word source range event’leri `TMP_Text.maxVisibleCharacters` değerine çevrilir. Rich-text kaynak indeksleri korunur.

## Dialogue

`SpeakLine`, `SpeakYarnCommand` ve `SpeakInkLine` public entrypoint’leri framework registration sistemlerine bağlanabilir. Unity Localization bridge asenkron localized string sonucunu aynı facade’a iletir.

## Timeline

Timeline track bir `GibberishVoicePlayer` binding’i ve typed speech clip’leri kullanır.

## Addressables

Voice/style asset reference loader handle’ları owner lifecycle’ında serbest bırakır. Editor utility seçili bake asset’lerini default Addressables group’a taşır.

## Burst

Opsiyonel synthesizer `NativeArray`, `IJobParallelFor` ve Burst ile sample post-processing uygular; core renderer fallback olarak kalır.
