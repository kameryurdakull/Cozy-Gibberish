# VContainer Integration

Prerequisites:

1. Install VContainer in the host project.
2. Add `COZY_GIBBERISH_VCONTAINER` to **Project Settings > Player > Scripting Define Symbols**.
3. Assign the scene's `GibberishVoicePlayer` to `GibberishLifetimeScope`.

The composition root injects one planner, synthesizer and typed event bus. Gameplay systems should depend on `IGibberishSpeechService`, not on the player component.
