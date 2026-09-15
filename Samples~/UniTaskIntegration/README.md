# UniTask Integration

Prerequisites:

1. Install UniTask in the host project.
2. Add `COZY_GIBBERISH_UNITASK` to **Project Settings > Player > Scripting Define Symbols**.

`SpeakAsync` captures every Unity object on the main thread, renders the sample buffer on the thread pool, then returns to the main thread to create and play the `AudioClip`.

```csharp
var request = new GibberishSpeechRequest("Good morning!", voice, cozyStyle);
await player.SpeakAndWaitAsync(request, destroyCancellationToken);
```
