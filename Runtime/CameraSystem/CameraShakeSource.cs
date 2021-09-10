using System;

using UnityEngine;

using BlackTundra.Foundation;
using BlackTundra.Foundation.Collections.Generic;

using Random = UnityEngine.Random;

namespace BlackTundra.World.CameraSystem {

    /// <summary>
    /// A source of camera shake that can be used to influence any active/enabled <see cref="CameraController"/> instance.
    /// </summary>
    public sealed class CameraShakeSource {

        #region constant

        /// <summary>
        /// Amount to shrink/expand the <see cref="SourceBuffer"/> by.
        /// </summary>
        private const int SourceBufferExpandSize = 16;

        /// <summary>
        /// <see cref="PackedBuffer{T}"/> containing every <see cref="CameraShakeSource"/> instance.
        /// </summary>
        private static readonly PackedBuffer<CameraShakeSource> SourceBuffer = new PackedBuffer<CameraShakeSource>(SourceBufferExpandSize);

        #endregion

        #region variable

        /// <inheritdoc cref="magnitude"/>
        internal float _magnitude;

        /// <inheritdoc cref="roughness"/>
        internal float _roughness;

        /// <summary>
        /// Position that the <see cref="CameraShakeSource"/> emits shake from.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// Scale of the <see cref="CameraShakeSource"/> on each local <see cref="CameraController"/> axis.
        /// </summary>
        public Vector3 scale;

        /// <summary>
        /// Total number of seconds the shake will last for.
        /// </summary>
        public readonly float lifetime;

        /// <summary>
        /// Time since the start of the <see cref="lifetime"/> that the fade in will end and the sustain period will begin.
        /// </summary>
        internal readonly float fadeInTime;

        /// <summary>
        /// Time since the start of the <see cref="lifetime"/> that the fade out will start.
        /// </summary>
        internal readonly float fadeOutTime;

        /// <summary>
        /// Seed used to generate the random shaking.
        /// </summary>
        /// <remarks>
        /// This ensures each shake instance is different.
        /// </remarks>
        public readonly float seed;

        /// <summary>
        /// Amount of time since the start of the shake.
        /// </summary>
        internal float phase;

        /// <inheritdoc cref="loop"/>
        private bool _loop;

        /// <summary>
        /// Offset due to looping.
        /// </summary>
        private float loopOffset;

        /// <inheritdoc cref="IsPlaying"/>
        private bool playing;

        /// <summary>
        /// <c>true</c> when the phase has changed.
        /// </summary>
        private bool changed;

        /// <summary>
        /// Cached shake from the last time the <see cref="Sample(in Vector3)"/> method was invoked.
        /// </summary>
        private Vector3 shake;

        #endregion

        #region property

        #region magnitude
        /// <summary>
        /// Magnitude of the shake caused by the <see cref="CameraShakeSource"/>.
        /// A higher value will result in more shake.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float magnitude {
#pragma warning restore IDE1006 // naming styles
            get => _magnitude;
            set {
                if (value < 0.0f) throw new ArgumentException($"{nameof(magnitude)} cannot be less than zero.");
                _magnitude = value;
            }
        }
        #endregion

        #region roughness
        /// <summary>
        /// Roughness of the shake caused by the <see cref="CameraShakeSource"/>.
        /// A higher value will result in a rougher shake.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float roughness {
#pragma warning restore IDE1006 // naming styles
            get => _roughness;
            set {
                if (value < 0.0f) throw new ArgumentException($"{nameof(roughness)} cannot be less than zero.");
                _roughness = value;
            }
        }
        #endregion

        #region loop
        /// <summary>
        /// When <c>true</c>, the <see cref="CameraShakeSource"/> will loop the sustain period (after fading in).
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public bool loop {
#pragma warning restore IDE1006 // naming styles
            get => _loop;
            set {
                if (value == _loop) return;
                _loop = value;
                if (value) { // loop enabled
                    if (phase >= fadeOutTime) { // camera shake finished, reset phase
                        phase = 0.0f;
                    } else if (phase > fadeOutTime - lifetime) { // current in fade-out
                        float lastPhase = phase;
                        phase = (phase - lifetime) / (fadeOutTime - lifetime) * fadeInTime; // fade back in
                        loopOffset += phase - lastPhase;
                    }
                } else { // loop disabled
                    if (phase > fadeOutTime) {
                        loopOffset += phase - fadeOutTime;
                        phase = fadeOutTime;
                    }
                }
            }
        }
        #endregion

        #region IsPlaying
        /// <summary>
        /// <c>true</c> if the <see cref="CameraShakeSource"/> is playing/active.
        /// </summary>
        /// <remarks>
        /// The <see cref="CameraShakeSource"/> can be used again by calling <see cref="Play"/>.
        /// </remarks>
        public bool IsPlaying => playing;
        #endregion

        #region IsComplete
        /// <summary>
        /// Returns <c>true</c> when the <see cref="CameraShakeSource"/> has completed shaking.
        /// </summary>
        public bool IsComplete => !_loop && phase >= lifetime;
        #endregion

        #endregion

        #region constructor

        /// <summary>
        /// Creates a blank <see cref="CameraShakeSource"/> that is by default not playing.
        /// </summary>
        public CameraShakeSource() {
            _magnitude = 1.0f;
            _roughness = 1.0f;
            _loop = false;
            position = Vector3.zero;
            scale = Vector3.one;
            lifetime = 1.0f;
            fadeInTime = 0.25f;
            fadeOutTime = 0.75f;
            seed = Random.Range(-5000.0f, 5000.0f);
            phase = 0.0f;
            loopOffset = 0.0f;
            playing = false;
            changed = true;
            shake = Vector3.zero;
        }

        #endregion

        #region destructor

        ~CameraShakeSource() {
            if (SourceBuffer.Remove(this) > 0) SourceBuffer.TryShrink(SourceBufferExpandSize);
        }

        #endregion

        #region logic

        #region Update

        [CoreUpdate]
        private static void Update(float deltaTime) {
            for (int i = SourceBuffer.Count - 1; i >= 0; i--) {
                SourceBuffer[i].InternalUpdate(deltaTime);
            }
        }

        private void InternalUpdate(in float deltaTime) {
            if (!playing) return; // not playing
            phase += deltaTime; // increment phase
            changed = true;
            if (!_loop && phase > lifetime) Stop();
        }

        #endregion

        #region HasSample

        /// <returns>
        /// Returns <c>true</c> if a sample should be taken.
        /// </returns>
        internal static bool HasSample() => !SourceBuffer.IsEmpty;

        #endregion

        #region Sample

        /// <param name="position">Point in world-space to sample camera shake.</param>
        /// <returns>
        /// Returns a <see cref="Vector3"/> offset to apply to a <see cref="CameraController"/> at the
        /// provided <paramref name="position"/>.
        /// </returns>
        internal static Vector3 Sample(in Vector3 position) {
            Vector3 shake = Vector3.zero;
            int sourceCount = SourceBuffer.Count;
            if (sourceCount > 0) {
                for (int i = sourceCount - 1; i >= 0; i--) {
                    shake += SourceBuffer[i].InternalSample(position);
                }
            }
            return shake;
        }

        private Vector3 InternalSample(in Vector3 point) {
            if (changed) {
                float shakeScale = _magnitude;
                if (phase < fadeInTime) shakeScale *= phase / fadeInTime;
                else if (!_loop && phase > fadeOutTime) shakeScale *= (phase - lifetime) / (fadeOutTime - lifetime);
                float shakePhase = _roughness * (phase + loopOffset);
                shake = new Vector3(
                    shakeScale * scale.x * (Mathf.PerlinNoise(shakePhase, seed) - 0.5f),
                    shakeScale * scale.y * (Mathf.PerlinNoise(seed, shakePhase) - 0.5f),
                    shakeScale * scale.z * (Mathf.PerlinNoise(shakePhase - seed, seed - shakePhase) - 0.5f)
                );
            }
            return shake / Mathf.Max(1.0f, (point - position).sqrMagnitude);
        }

        #endregion

        #region Play

        /// <summary>
        /// Resumes playing the <see cref="CameraShakeSource"/>.
        /// If the shake is complete, the shake will start again from the start.
        /// </summary>
        public void Play() {
            if (playing) return; // already playing, stop here
            if (SourceBuffer.IsFull) SourceBuffer.Expand(SourceBufferExpandSize); // check if the buffer is full and expand if required
            SourceBuffer.AddLast(this, true); // add the source to the buffer
            if (!_loop && phase >= lifetime) phase = 0.0f; // reset phase if required
            playing = true; // set as playing
        }

        #endregion

        #region Pause

        /// <summary>
        /// Pauses the <see cref="CameraShakeSource"/>.
        /// </summary>
        public void Pause() {
            if (!playing) return; // already not playing
            SourceBuffer.Remove(this); // remove the source to the buffer
            playing = false; // set as not playing
        }

        #endregion

        #region Stop

        /// <summary>
        /// Stops the <see cref="CameraShakeSource"/>.
        /// </summary>
        /// <remarks>
        /// This will complete the shake unless <see cref="loop"/> is enabled.
        /// </remarks>
        public void Stop() {
            phase = lifetime; // set as complete
            if (!playing) return; // already not playing
            if (SourceBuffer.Remove(this) > 0) // remove from the source buffer
                SourceBuffer.TryShrink(SourceBufferExpandSize); // try shrink the source buffer
            playing = false; // set as not playing
            shake = Vector3.zero;
            changed = false;
        }

        #endregion

        #endregion

    }

}