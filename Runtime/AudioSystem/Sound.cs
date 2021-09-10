using BlackTundra.Foundation.Utility;

using System;
using System.Collections.Generic;

using UnityEngine;

using Console = BlackTundra.Foundation.Console;

namespace BlackTundra.World.Audio {

    /// <summary>
    /// Defines and describes a <see cref="Sound"/> that can be played.
    /// </summary>
    public sealed class Sound {

        #region constant

        /// <summary>
        /// Table containing every sound.
        /// </summary>
        private static readonly Dictionary<string, Sound>[] SoundTable;

        #endregion

        #region variable

        /// <summary>
        /// Name of the <see cref="Sound"/>.
        /// </summary>
        public readonly string name;

        /// <summary>
        /// <see cref="SoundTag"/> that describes what type of sound this is.
        /// </summary>
        public readonly SoundTag tag;

        /// <summary>
        /// <see cref="AudioClip"/> associated with this sound.
        /// </summary>
        public readonly AudioClip clip;

        /// <summary>
        /// Value between <c>0.0</c> and <c>1.0</c> that determinds how much the sound has the ability to
        /// penetrate materials.
        /// </summary>
        public readonly float penetration;

        #endregion

        #region constructor

        /// <summary>
        /// Static constructor for the <see cref="Sound"/> class.
        /// </summary>
        static Sound() {
            int tagCount = Enum.GetValues(typeof(SoundTag)).Length;
            SoundTable = new Dictionary<string, Sound>[tagCount];
            for (int i = tagCount - 1; i >= 0; i--) {
                SoundTable[i] = new Dictionary<string, Sound>();
            }
        }

        private Sound() => throw new NotSupportedException();

        /// <summary>
        /// Constructs a new <see cref="Sound"/> and adds it to the <see cref="SoundTable"/>.
        /// </summary>
        /// <param name="name">
        /// Name of the sound. This should be all lower-case alphanumerics with underscores, dashes, or full stops only.
        /// This is simply to ensure consistency and convention.
        /// </param>
        /// <param name="tag"><see cref="SoundTag"/> associated with the <see cref="Sound"/>.</param>
        /// <param name="clip"><see cref="AudioClip"/> associated with the <see cref="Sound"/>.</param>
        /// <param name="penetration">
        /// Value between <c>0.0</c> and <c>1.0</c> that determinds how much the sound has the ability to penetrate
        /// through materials. A value of <c>1.0</c> means the sound passes through materials with no problems. A value
        /// of <c>0.0</c> means the sound has no ability to penetrate materials.
        /// </param>
        internal Sound(in string name, in SoundTag tag, in AudioClip clip, in float penetration) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (penetration < 0.0f || penetration > 1.0f) throw new ArgumentOutOfRangeException(nameof(penetration));
            Dictionary<string, Sound> dictionary = SoundTable[(int)tag];
            if (dictionary.ContainsKey(name)) throw new ArgumentException($"A sound with the \"{tag}\" tag named \"{name}\" already exists.");
            this.name = name;
            this.clip = clip;
            this.tag = tag;
            this.penetration = penetration;
            dictionary.Add(name, this);
        }

        #endregion

        #region logic

        #region Find

        /// <summary>
        /// Searches for a sound based off of the <paramref name="name"/> of the sound and the <paramref name="tag"/>
        /// the sound is expected to have.
        /// </summary>
        /// <param name="name">Name of the <see cref="Sound"/>.</param>
        /// <param name="tag"><see cref="SoundTag"/> the sound has.</param>
        /// <returns>
        /// Returns a reference to the <see cref="Sound"/> if one was found with the correct <paramref name="name"/>
        /// and <paramref name="tag"/>; otherwise <c>null</c> is returned.
        /// </returns>
        public static Sound Find(in string name, in SoundTag tag) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            Dictionary<string, Sound> dictionary = SoundTable[(int)tag];
            return dictionary.TryGetValue(name, out Sound sound) ? sound : null;
        }

        #endregion

        #region Play

        public SoundInstance Play() => Play(1.0f, 1.0f);

        public SoundInstance Play(in float volume) => SoundInstance.CreateGlobal(this, volume, 1.0f);

        public SoundInstance Play(in float volume, in float pitch) => SoundInstance.CreateGlobal(this, volume, pitch);

        public SoundInstance Play(in float volume, in float pitch, in Vector3 point) => SoundInstance.CreateWorld(this, volume, pitch, point);

        #endregion

        #region PlayOnce

        public void PlayOnce() => AudioSourceManager.GetShared(tag).PlayOneShot(clip, 1.0f);

        public void PlayOnce(in float volume) => AudioSourceManager.GetShared(tag).PlayOneShot(clip, volume);

        #endregion

        #region VolumeAtTime

        /// <summary>
        /// Queries the volume of this <see cref="Sound"/> at a <paramref name="time"/> into the <see cref="Sound"/>.
        /// </summary>
        /// <param name="time">Time to query the <see cref="Sound"/> at. If this is more than the length of the clip, the value is wrapped back around.</param>
        /// <returns>Returns a value between <c>0.0f</c> (silent) and <c>1.0f</c> (max volume).</returns>
        /// <remarks>
        /// This method cannot find the volume of an <see cref="AudioClip"/> that is either not loaded into memory or uses compression.
        /// </remarks>
        public float VolumeAtTime(float time) {
            if (clip.loadState != AudioDataLoadState.Loaded) {
                Console.Warning($"Failed to get clip volume of audio clip \"{clip.name}\" because the clip is not loaded (\"{clip.loadState}\").");
                return 0.0f;
            }
            if (time < 0.0f) return 0.0f;
            float clipLength = clip.length; // number of seconds the clip is long
            if (time >= clipLength) time = MathsUtility.Wrap(time, clipLength); // wrap time if over clip length
            int sampleCount = clip.samples; // number of samples in the clip
            float sampleRate = sampleCount / clipLength; // calculate the number of samples each second in the clip
            int sampleIndex = Mathf.FloorToInt(sampleRate * time); // calculate the index to take a sample at
            int channelCount = clip.channels; // number of channels in the clip (mono, stereo, etc)
            float[] samples = new float[channelCount];
            if (clip.GetData(samples, sampleIndex)) {
                float maxIntensity = samples[0];
                float intensity;
                for (int i = channelCount - 1; i >= 1; i--) {
                    intensity = samples[i];
                    if (intensity < 0.0f) intensity = -intensity; // convert to absolute value
                    if (intensity > maxIntensity) maxIntensity = intensity;
                }
                return maxIntensity;
            } else {
                Console.Warning($"Failed to get clip volume of audio clip \"{clip.name}\" for an unknown reason; perhaps the clip uses compression.");
                return 0.0f; // failed to get clip data
            }
        }

        #endregion

        #region VolumeAtSample

        /// <summary>
        /// Queries the volume of this <see cref="Sound"/> at a <paramref name="sampleIndex"/>.
        /// </summary>
        /// <param name="sampleIndex">PCM sample index to query the volume of the <see cref="Sound"/> at.</param>
        /// <returns>Returns a value between <c>0.0f</c> (silent) and <c>1.0f</c> (max volume).</returns>
        /// <remarks>
        /// This method cannot find the volume of an <see cref="AudioClip"/> that is either not loaded into memory or uses compression.
        /// </remarks>
        public float VolumeAtSample(in int sampleIndex) {
            if (clip.loadState != AudioDataLoadState.Loaded) {
                Console.Warning($"Failed to get clip volume of audio clip \"{clip.name}\" because the clip is not loaded (\"{clip.loadState}\").");
                return 0.0f;
            }
            if (sampleIndex < 0) throw new ArgumentOutOfRangeException(nameof(sampleIndex));
            int sampleCount = clip.samples; // number of samples in the clip
            if (sampleIndex >= sampleCount) throw new ArgumentOutOfRangeException(nameof(sampleIndex));
            int channelCount = clip.channels; // number of channels in the clip (mono, stereo, etc)
            float[] samples = new float[channelCount];
            if (clip.GetData(samples, sampleIndex)) {
                float maxIntensity = samples[0];
                float intensity;
                for (int i = channelCount - 1; i >= 1; i--) {
                    intensity = samples[i];
                    if (intensity < 0.0f) intensity = -intensity; // convert to absolute value
                    if (intensity > maxIntensity) maxIntensity = intensity;
                }
                return maxIntensity;
            } else {
                Console.Warning($"Failed to get clip volume of audio clip \"{clip.name}\" for an unknown reason; perhaps the clip uses compression.");
                return 0.0f; // failed to get clip data
            }
        }

        #endregion

        #endregion

    }

}
