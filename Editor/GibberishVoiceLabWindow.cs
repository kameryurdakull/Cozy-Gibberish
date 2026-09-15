using System;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CozyGibberish.Editor
{
    public sealed class GibberishVoiceLabWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Cozy Gibberish/Voice Lab";
        private const string DefaultText =
            "Welcome home, little traveler. The fire is warm and the tea is ready.";
        private const string DefaultExportName = "CozyGibberish.wav";
        private const string PitchProperty = "_basePitch";
        private const string WarmthProperty = "_warmth";
        private const string BrightnessProperty = "_brightness";
        private const string BreathinessProperty = "_breathiness";
        private const string DurationProperty = "_syllableDuration";
        private const string PitchRangeProperty = "_pitchVariationSemitones";
        private const string PreviewClipName = "CozyGibberish_VoiceLab";

        private ObjectField _voiceAField;
        private ObjectField _voiceBField;
        private ObjectField _styleField;
        private ObjectField _phonotacticsField;
        private ObjectField _prosodyField;
        private TextField _textField;
        private IntegerField _seedField;
        private Slider _morphSlider;
        private Slider _energySlider;
        private Slider _warmthSlider;
        private Slider _paceSlider;
        private Slider _pitchSlider;
        private Toggle _lockPitch;
        private Toggle _lockTimbre;
        private Toggle _lockRhythm;
        private Label _diagnostics;
        private Label _styleDiff;
        private AudioVisualizationElement _visualization;
        private GibberishAudioData _lastAudio;

        [MenuItem(MenuPath, priority = 101)]
        public static void Open()
        {
            var window = GetWindow<GibberishVoiceLabWindow>("Gibberish Voice Lab");
            window.minSize = new Vector2(760f, 640f);
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.style.paddingLeft = 12f;
            rootVisualElement.style.paddingRight = 12f;
            rootVisualElement.style.paddingTop = 10f;
            rootVisualElement.style.paddingBottom = 10f;

            var scroll = new ScrollView();
            rootVisualElement.Add(scroll);

            var title = new Label("Cozy Gibberish Voice Lab");
            title.style.fontSize = 20f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 8f;
            scroll.Add(title);

            var assetRow = CreateRow();
            _voiceAField = CreateObjectField<GibberishVoiceProfile>("Voice A");
            _voiceBField = CreateObjectField<GibberishVoiceProfile>("Voice B");
            assetRow.Add(_voiceAField);
            assetRow.Add(_voiceBField);
            scroll.Add(assetRow);

            _morphSlider = new Slider("A / B Morph", 0f, 1f) { value = 0f };
            scroll.Add(_morphSlider);

            var modifierRow = CreateRow();
            _styleField = CreateObjectField<GibberishStylePreset>("Style");
            _phonotacticsField = CreateObjectField<GibberishPhonotacticProfile>("Phonotactics");
            _prosodyField = CreateObjectField<GibberishProsodyProfile>("Prosody");
            modifierRow.Add(_styleField);
            modifierRow.Add(_phonotacticsField);
            modifierRow.Add(_prosodyField);
            scroll.Add(modifierRow);

            _textField = new TextField("Preview Text")
            {
                value = DefaultText,
                multiline = true
            };
            _textField.style.minHeight = 62f;
            scroll.Add(_textField);

            var expressionRow = CreateRow();
            _energySlider = CreateExpressionSlider("Energy");
            _warmthSlider = CreateExpressionSlider("Warmth");
            _paceSlider = CreateExpressionSlider("Pace");
            _pitchSlider = CreateExpressionSlider("Pitch");
            expressionRow.Add(_energySlider);
            expressionRow.Add(_warmthSlider);
            expressionRow.Add(_paceSlider);
            expressionRow.Add(_pitchSlider);
            scroll.Add(expressionRow);

            var seedRow = CreateRow();
            _seedField = new IntegerField("Seed");
            seedRow.Add(_seedField);
            for (var seed = 1; seed <= 6; seed++)
            {
                var capturedSeed = seed * 17;
                seedRow.Add(new Button(() =>
                {
                    _seedField.value = capturedSeed;
                    Preview();
                })
                {
                    text = capturedSeed.ToString()
                });
            }

            scroll.Add(seedRow);

            var lockRow = CreateRow();
            _lockPitch = new Toggle("Lock Pitch");
            _lockTimbre = new Toggle("Lock Timbre");
            _lockRhythm = new Toggle("Lock Rhythm");
            lockRow.Add(_lockPitch);
            lockRow.Add(_lockTimbre);
            lockRow.Add(_lockRhythm);
            scroll.Add(lockRow);

            var actionRow = CreateRow();
            actionRow.Add(new Button(Preview) { text = "Preview" });
            actionRow.Add(new Button(GibberishEditorPreview.Stop) { text = "Stop" });
            actionRow.Add(new Button(RandomizeUnlocked) { text = "Randomize Unlocked" });
            actionRow.Add(new Button(() => SetMorph(0f)) { text = "A" });
            actionRow.Add(new Button(() => SetMorph(1f)) { text = "B" });
            actionRow.Add(new Button(ExportWav) { text = "Export WAV" });
            scroll.Add(actionRow);

            _visualization = new AudioVisualizationElement();
            _visualization.style.height = 250f;
            _visualization.style.marginTop = 10f;
            _visualization.style.marginBottom = 8f;
            scroll.Add(_visualization);

            _diagnostics = new Label("Render a preview to inspect quality and cost.");
            _diagnostics.style.whiteSpace = WhiteSpace.Normal;
            scroll.Add(_diagnostics);

            _styleDiff = new Label();
            _styleDiff.style.whiteSpace = WhiteSpace.Normal;
            scroll.Add(_styleDiff);
        }

        private void OnDisable()
        {
            GibberishEditorPreview.Stop();
        }

        private void Preview()
        {
            var voiceA = _voiceAField.value as GibberishVoiceProfile;
            var voiceB = _voiceBField.value as GibberishVoiceProfile;
            if (voiceA == null && voiceB == null)
            {
                _diagnostics.text = "Assign at least one voice profile.";
                return;
            }

            var expression = new GibberishExpression(
                _energySlider.value,
                _warmthSlider.value,
                _paceSlider.value,
                _pitchSlider.value);
            var style = _styleField.value as GibberishStylePreset;
            var prosodyProfile = _prosodyField.value as GibberishProsodyProfile;
            var prosody = prosodyProfile == null
                ? GibberishProsodySnapshot.Neutral
                : prosodyProfile.CreateSnapshot();
            var left = (voiceA != null ? voiceA : voiceB)
                .CreateSnapshot(GibberishStyleInfluence.From(style), expression, prosody);
            var right = (voiceB != null ? voiceB : voiceA)
                .CreateSnapshot(GibberishStyleInfluence.From(style), expression, prosody);
            var voice = GibberishVoiceSnapshot.Blend(left, right, _morphSlider.value);
            var phonotacticsProfile =
                _phonotacticsField.value as GibberishPhonotacticProfile;
            var phonotactics = phonotacticsProfile == null
                ? GibberishPhonotacticSnapshot.Default
                : phonotacticsProfile.CreateSnapshot();
            var planner = new GibberishUtterancePlanner();

            var timer = Stopwatch.StartNew();
            var utterance = planner.CreatePlan(
                _textField.value,
                voice,
                phonotactics,
                _seedField.value);
            var synthesizer = new ProceduralGibberishSynthesizer();
            _lastAudio = synthesizer.Render(utterance, voice);
            timer.Stop();

            _visualization.SetAudio(_lastAudio);
            var clip = GibberishAudioClipFactory.Create(PreviewClipName, _lastAudio);
            GibberishEditorPreview.Play(clip);
            UpdateDiagnostics(_lastAudio, utterance, timer.Elapsed.TotalMilliseconds);
            UpdateStyleDiff(style);
        }

        private void RandomizeUnlocked()
        {
            var voice = _voiceAField.value as GibberishVoiceProfile;
            if (voice == null)
            {
                _diagnostics.text = "Voice A is required for asset randomization.";
                return;
            }

            Undo.RecordObject(voice, "Randomize Gibberish Voice");
            var serialized = new SerializedObject(voice);

            if (!_lockPitch.value)
            {
                serialized.FindProperty(PitchProperty).floatValue = UnityEngine.Random.Range(90f, 360f);
                RandomizeRange(serialized.FindProperty(PitchRangeProperty), -6f, 6f);
            }

            if (!_lockTimbre.value)
            {
                serialized.FindProperty(WarmthProperty).floatValue = UnityEngine.Random.value;
                serialized.FindProperty(BrightnessProperty).floatValue = UnityEngine.Random.value;
                serialized.FindProperty(BreathinessProperty).floatValue =
                    UnityEngine.Random.Range(0f, 0.35f);
            }

            if (!_lockRhythm.value)
            {
                RandomizeRange(serialized.FindProperty(DurationProperty), 0.065f, 0.26f);
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(voice);
            Preview();
        }

        private void ExportWav()
        {
            if (_lastAudio == null)
            {
                Preview();
            }

            if (_lastAudio == null)
            {
                return;
            }

            var path = EditorUtility.SaveFilePanel(
                "Export Gibberish WAV",
                string.Empty,
                DefaultExportName,
                "wav");
            if (!string.IsNullOrWhiteSpace(path))
            {
                GibberishWavEncoder.Write(path, _lastAudio);
            }
        }

        private void UpdateDiagnostics(
            GibberishAudioData audio,
            GibberishUtterance utterance,
            double milliseconds)
        {
            var peak = 0f;
            for (var index = 0; index < audio.Samples.Length; index++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(audio.Samples[index]));
            }

            var memoryKilobytes = audio.Samples.LongLength * sizeof(float) / 1024f;
            var status = peak > 0.95f ? "Headroom warning" : "Headroom safe";
            _diagnostics.text =
                $"{status} | Peak {peak:0.000} | {utterance.SyllableCount} syllables | " +
                $"{audio.Duration:0.00}s | {memoryKilobytes:0.0} KB PCM | Render {milliseconds:0.0} ms";
        }

        private void UpdateStyleDiff(GibberishStylePreset style)
        {
            if (style == null)
            {
                _styleDiff.text = "Style: neutral";
                return;
            }

            _styleDiff.text =
                $"Style delta — Pace ×{style.PaceMultiplier:0.00}, " +
                $"Pitch {style.PitchOffsetSemitones:+0.0;-0.0;0.0} st, " +
                $"Warmth {style.WarmthOffset:+0.00;-0.00;0.00}, " +
                $"Brightness {style.BrightnessOffset:+0.00;-0.00;0.00}";
        }

        private void SetMorph(float value)
        {
            _morphSlider.value = value;
            Preview();
        }

        private static void RandomizeRange(
            SerializedProperty range,
            float minimum,
            float maximum)
        {
            var first = UnityEngine.Random.Range(minimum, maximum);
            var second = UnityEngine.Random.Range(minimum, maximum);
            range.FindPropertyRelative("_minimum").floatValue = Mathf.Min(first, second);
            range.FindPropertyRelative("_maximum").floatValue = Mathf.Max(first, second);
        }

        private static VisualElement CreateRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 6f;
            return row;
        }

        private static ObjectField CreateObjectField<T>(string label)
            where T : UnityEngine.Object
        {
            var field = new ObjectField(label)
            {
                objectType = typeof(T),
                allowSceneObjects = false
            };
            field.style.flexGrow = 1f;
            return field;
        }

        private static Slider CreateExpressionSlider(string label)
        {
            var slider = new Slider(label, -1f, 1f);
            slider.style.flexGrow = 1f;
            return slider;
        }

        private sealed class AudioVisualizationElement : VisualElement
        {
            private const int SpectrumBands = 64;
            private const int SpectrumWindow = 2048;

            private float[] _samples = Array.Empty<float>();
            private float[] _spectrum = Array.Empty<float>();

            public AudioVisualizationElement()
            {
                generateVisualContent += Draw;
                style.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
                style.borderTopLeftRadius = 6f;
                style.borderTopRightRadius = 6f;
                style.borderBottomLeftRadius = 6f;
                style.borderBottomRightRadius = 6f;
            }

            public void SetAudio(GibberishAudioData audio)
            {
                _samples = audio?.Samples ?? Array.Empty<float>();
                _spectrum = CalculateSpectrum(_samples);
                MarkDirtyRepaint();
            }

            private void Draw(MeshGenerationContext context)
            {
                var painter = context.painter2D;
                var rect = contentRect;
                var waveformHeight = rect.height * 0.58f;
                var center = rect.y + waveformHeight * 0.5f;
                painter.strokeColor = new Color(0.43f, 0.91f, 0.76f, 1f);
                painter.lineWidth = 1.5f;
                painter.BeginPath();

                var points = Mathf.Max(2, Mathf.RoundToInt(rect.width));
                for (var index = 0; index < points; index++)
                {
                    var sampleIndex = _samples.Length == 0
                        ? 0
                        : Mathf.Clamp(
                            Mathf.RoundToInt(index / (float)(points - 1) * (_samples.Length - 1)),
                            0,
                            _samples.Length - 1);
                    var sample = _samples.Length == 0 ? 0f : _samples[sampleIndex];
                    var point = new Vector2(
                        rect.x + index / (float)(points - 1) * rect.width,
                        center - sample * waveformHeight * 0.46f);

                    if (index == 0)
                    {
                        painter.MoveTo(point);
                    }
                    else
                    {
                        painter.LineTo(point);
                    }
                }

                painter.Stroke();

                if (_spectrum.Length == 0)
                {
                    return;
                }

                var spectrumTop = rect.y + waveformHeight + 8f;
                var spectrumHeight = rect.height - waveformHeight - 12f;
                var barWidth = rect.width / _spectrum.Length;
                painter.fillColor = new Color(0.48f, 0.58f, 0.98f, 0.72f);
                for (var index = 0; index < _spectrum.Length; index++)
                {
                    var height = Mathf.Clamp01(_spectrum[index]) * spectrumHeight;
                    var left = rect.x + index * barWidth;
                    var top = spectrumTop + spectrumHeight - height;
                    var right = left + Mathf.Max(1f, barWidth - 1f);
                    var bottom = top + height;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(left, top));
                    painter.LineTo(new Vector2(right, top));
                    painter.LineTo(new Vector2(right, bottom));
                    painter.LineTo(new Vector2(left, bottom));
                    painter.ClosePath();
                    painter.Fill();
                }
            }

            private static float[] CalculateSpectrum(float[] samples)
            {
                if (samples == null || samples.Length == 0)
                {
                    return Array.Empty<float>();
                }

                var window = Mathf.Min(SpectrumWindow, samples.Length);
                var start = samples.Length - window;
                var result = new float[SpectrumBands];
                var maximum = 0f;

                for (var band = 0; band < result.Length; band++)
                {
                    var real = 0d;
                    var imaginary = 0d;
                    var frequencyBin = band + 1;
                    for (var sample = 0; sample < window; sample++)
                    {
                        var hann = 0.5d -
                                   0.5d * Math.Cos(2d * Math.PI * sample / Math.Max(1, window - 1));
                        var angle = 2d * Math.PI * frequencyBin * sample / window;
                        var value = samples[start + sample] * hann;
                        real += value * Math.Cos(angle);
                        imaginary -= value * Math.Sin(angle);
                    }

                    result[band] = (float)Math.Sqrt(real * real + imaginary * imaginary);
                    maximum = Mathf.Max(maximum, result[band]);
                }

                if (maximum > 0f)
                {
                    for (var index = 0; index < result.Length; index++)
                    {
                        result[index] = Mathf.Sqrt(result[index] / maximum);
                    }
                }

                return result;
            }
        }
    }
}
