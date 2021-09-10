//#define WORLD_REPLACE_AUDIO_SOURCE_WITH_SOUND_SOURCE

using BlackTundra.Foundation;
using BlackTundra.Foundation.Collections.Generic;

using System;

using UnityEngine;

using Console = BlackTundra.Foundation.Console;

namespace BlackTundra.World.Audio {

    public sealed class SoundSource : MonoBehaviour {

        #region constant

        private const int BufferExpandSize = 8;

        #endregion

        #region variable

        private readonly PackedBuffer<SoundInstance> buffer = new PackedBuffer<SoundInstance>(BufferExpandSize);

        #endregion

        #region logic

        #region Validate
#if WORLD_REPLACE_AUDIO_SOURCE_WITH_SOUND_SOURCE

        [CoreValidate]
        private static void Validate() {
            AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
            int audioSourceCount = audioSources.Length;
            if (audioSourceCount > 0) {
                Debug.LogWarning($"{audioSourceCount} AudioSource components found. These should be replaced with {nameof(SoundSource)} components.");
                string warningMessage = $"Replace AudioSource component with {nameof(SoundSource)} component.";
                for (int i = audioSourceCount - 1; i >= 0; i--) Debug.LogWarning(warningMessage, audioSources[i]);
            }
        }

#endif
        #endregion

        #region Play

        public SoundInstance Play(in Sound sound) => Play(sound, 1.0f, 1.0f);

        public SoundInstance Play(in Sound sound, in float volume) => Play(sound, volume, 1.0f);

        public SoundInstance Play(in Sound sound, float volume, float pitch) {
            if (sound == null) throw new ArgumentNullException(nameof(sound));
            SoundInstance instance = sound.Play(volume, pitch, transform.position);
            if (buffer.IsFull) buffer.Expand(BufferExpandSize);
            buffer.AddLast(instance);
            return instance;
        }

        #endregion

        #region Update

        private void Update() {
            if (!buffer.IsEmpty) { // buffer is not empty
                int instanceCount = buffer.Count;
                SoundInstance instance;
                Vector3 position = transform.position;
                for (int i = instanceCount; i >= 0; i--) {
                    instance = buffer[i];
                    if (instance.disposed) buffer.RemoveAt(i);
                    else instance.position = position;
                }
                int remainingSpace = buffer.RemainingSpace;
                if (remainingSpace > BufferExpandSize) {
                    int shrinkOperations = remainingSpace / BufferExpandSize; // calculate the required number of shrink operations
                    if (!buffer.TryShrink(BufferExpandSize * shrinkOperations)) { // combine shrink operations into single shrink operation
                        Console.Error($"[SoundSource] Failed to shrink {nameof(SoundInstance)} {nameof(buffer)}.");
                    }
                }
            }
        }

        #endregion

        #endregion

    }

}