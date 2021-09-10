using System;

using UnityEngine;

namespace BlackTundra.World.Audio {

    /// <summary>
    /// Describes an instance of a sound.
    /// </summary>
    public sealed class SoundInstance : IDisposable {

        #region variable

        /// <summary>
        /// <see cref="Sound"/> that the <see cref="SoundInstance"/> is.
        /// </summary>
        internal readonly Sound sound;

        /// <summary>
        /// <see cref="AudioSource"/> used to play the sound.
        /// </summary>
        internal readonly AudioSource source;

        #endregion

        #region property

        /// <inheritdoc cref="AudioSource.clip"/>
#pragma warning disable IDE1006 // naming styles
        public AudioClip clip => sound.clip;
#pragma warning restore IDE1006 // naming styles

        /// <inheritdoc cref="AudioSource.dopplerLevel"/>
#pragma warning disable IDE1006 // naming styles
        public float dopplerLevel {
#pragma warning restore IDE1006 // naming styles
            get => source.dopplerLevel;
            set => source.dopplerLevel = value;
        }

        /// <inheritdoc cref="AudioSource.loop"/>
#pragma warning disable IDE1006 // naming styles
        public bool loop {
#pragma warning restore IDE1006 // naming styles
            get => source.loop;
            set => source.loop = value;
        }

        /// <inheritdoc cref="AudioSource.pitch"/>
#pragma warning disable IDE1006 // naming styles
        public float pitch {
#pragma warning restore IDE1006 // naming styles
            get => source.pitch;
            set => source.pitch = value;
        }

        /// <inheritdoc cref="AudioSource.priority"/>
#pragma warning disable IDE1006 // naming styles
        public int priority {
#pragma warning restore IDE1006 // naming styles
            get => source.priority;
            set => source.priority = priority;
        }

        /// <inheritdoc cref="AudioSource.spatialBlend"/>
#pragma warning disable IDE1006 // naming styles
        public float spatialBlend {
#pragma warning restore IDE1006 // naming styles
            get => source.spatialBlend;
            set => source.spatialBlend = value;
        }

        /// <inheritdoc cref="AudioSource.spatialize"/>
#pragma warning disable IDE1006 // naming styles
        public bool spatialize {
#pragma warning restore IDE1006 // naming styles
            get => source.spatialize;
            set => source.spatialize = value;
        }

        /// <inheritdoc cref="AudioSource.time"/>
#pragma warning disable IDE1006 // naming styles
        public float time {
#pragma warning restore IDE1006 // naming styles
            get => source.time;
            set => source.time = value;
        }

        /// <inheritdoc cref="AudioSource.volume"/>
#pragma warning disable IDE1006 // naming styles
        public float volume {
#pragma warning restore IDE1006 // naming styles
            get => source.volume;
            set => source.volume = volume;
        }

        /// <summary>
        /// Volume of the <see cref="clip"/> at the instant this property is queried. This will be equal
        /// to the sampled volume/intensity of the clip at the current instant multiplied by the
        /// <see cref="volume"/> of the <see cref="SoundInstance"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float clipVolume => source.volume * sound.VolumeAtSample(source.timeSamples);
#pragma warning restore IDE1006 // naming styles

        /// <summary>
        /// Position in world-space of the <see cref="SoundInstance"/>.
        /// </summary>
        /// <remarks>
        /// This will only have an effect on the <see cref="SoundInstance"/> if <see cref="spatialize"/>
        /// is <c>true</c>.
        /// </remarks>
#pragma warning disable IDE1006 // naming styles
        public Vector3 position {
#pragma warning restore IDE1006 // naming styles
            get => source.transform.position;
            set => source.transform.position = value;
        }

        /// <summary>
        /// <see cref="SoundTag"/> that this <see cref="SoundInstance"/> is marked with.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public SoundTag tag => sound.tag;
#pragma warning restore IDE1006 // naming styles

        /// <summary>
        /// <c>true</c> if the <see cref="SoundInstance"/> has been disposed.
        /// </summary>
        /// <seealso cref="Dispose"/>
#pragma warning disable IDE1006 // naming styles
        public bool disposed { get; private set; } = false;
#pragma warning restore IDE1006 // naming styles

        #endregion

        #region constructor

        private SoundInstance() => throw new NotSupportedException();

        /// <summary>
        /// Constructs a <see cref="SoundInstance"/>.
        /// </summary>
        /// <param name="sound"><see cref="Sound"/> to play.</param>
        /// <param name="source"><see cref="AudioSource"/> to play the sound from.</param>
        private SoundInstance(in Sound sound, in AudioSource source) {
            this.sound = sound;
            this.source = source;
            switch (sound.tag) {
                case SoundTag.ENV: {
                    Soundscape.TrackedSoundInstances.Add(this);
                    break;
                }
                case SoundTag.SFX: {
                    break;
                }
                case SoundTag.MUS: {
                    //MusicManager.RegisterSoundInstance(this);
                    break;
                }
            }
        }

        #endregion

        #region logic

        #region CreateGlobal

        /// <summary>
        /// Creates a global (not bound to a position) <see cref="SoundInstance"/>.
        /// </summary>
        internal static SoundInstance CreateGlobal(in Sound sound, in float volume, in float pitch) {
            AudioSource source = AudioSourceManager.GetSingle(sound.tag);
            source.volume = volume;
            source.pitch = pitch;

            source.spatialize = false;
            source.spatialBlend = 0.0f;

            source.loop = false;
            source.priority = 127;
            source.dopplerLevel = 1.0f;

            return new SoundInstance(sound, source);
        }

        #endregion

        #region CreateWorld

        /// <summary>
        /// Creates a world (bound to a <paramref name="point"/>) <see cref="SoundInstance"/>.
        /// </summary>
        internal static SoundInstance CreateWorld(in Sound sound, in float volume, in float pitch, in Vector3 point) {
            AudioSource source = AudioSourceManager.GetSingle(sound.tag);
            source.transform.position = point;
            source.volume = volume;
            source.pitch = pitch;

            source.spatialBlend = 1.0f;
            source.spatialize = true;

            source.loop = false;
            source.priority = 127;
            source.dopplerLevel = 1.0f;

            return new SoundInstance(sound, source);
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Disposes of the <see cref="SoundInstance"/> properly.
        /// </summary>
        public void Dispose() {
            disposed = true;
            AudioSourceManager.ReturnSingle(source);
            switch (sound.tag) {
                case SoundTag.ENV: {
                    Soundscape.TrackedSoundInstances.Remove(this);
                    break;
                }
                case SoundTag.SFX: {
                    break;
                }
                case SoundTag.MUS: {
                    //MusicManager.RemoveSoundInstance(this);
                    break;
                }
            }
        }

        #endregion

        #endregion

    }

}