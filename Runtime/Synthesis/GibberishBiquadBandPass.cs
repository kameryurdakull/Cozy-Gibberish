using System;
using UnityEngine;

namespace CozyGibberish
{
    internal struct GibberishBiquadBandPass
    {
        private readonly float _b0;
        private readonly float _b2;
        private readonly float _a1;
        private readonly float _a2;
        private float _x1;
        private float _x2;
        private float _y1;
        private float _y2;

        public GibberishBiquadBandPass(float frequency, float quality, int sampleRate)
        {
            var nyquistSafeFrequency = Mathf.Clamp(frequency, 40f, sampleRate * 0.46f);
            var omega = 2f * Mathf.PI * nyquistSafeFrequency / sampleRate;
            var alpha = Mathf.Sin(omega) / (2f * Mathf.Max(0.25f, quality));
            var inverseA0 = 1f / (1f + alpha);

            _b0 = alpha * inverseA0;
            _b2 = -alpha * inverseA0;
            _a1 = -2f * Mathf.Cos(omega) * inverseA0;
            _a2 = (1f - alpha) * inverseA0;
            _x1 = 0f;
            _x2 = 0f;
            _y1 = 0f;
            _y2 = 0f;
        }

        public float Process(float input)
        {
            var output = _b0 * input + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
            _x2 = _x1;
            _x1 = input;
            _y2 = _y1;
            _y1 = output;
            return output;
        }
    }
}
