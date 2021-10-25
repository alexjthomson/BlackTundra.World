using BlackTundra.Foundation;
using BlackTundra.Foundation.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;

using Object = UnityEngine.Object;

namespace BlackTundra.World.Audio {

    /// <summary>
    /// Manages a pool of <see cref="AudioSource"/> instances.
    /// </summary>
    internal static class AudioSourceManager {

        #region constant

        private static readonly AudioMixerGroup[] SoundTagMixerGroups = new AudioMixerGroup[SoundTagUtility.SoundTagCount];

        private const int AudioSourcePoolExpandSize = 32;
        private const int AudioSourcePoolInitialCapacity = AudioSourcePoolExpandSize * 8;
        private const int AudioSourcePoolMaximumCapacity = AudioSourcePoolInitialCapacity * 4;
        private const int AudioSourcePoolInitialObjectCount = AudioSourcePoolInitialCapacity;

        private static readonly ObjectPool<AudioSource> AudioSourcePool = new ObjectPool<AudioSource>(
            AudioSourcePoolInitialCapacity,
            AudioSourcePoolMaximumCapacity,
            AudioSourcePoolExpandSize,
            AudioSourcePoolInitialObjectCount,
            CreateAudioSourcePoolObject,
            ReturnAudioSourceToPool,
            RemoveAudioSourceFromPool
        );

        private const string AudioSourceNamePrefix = "ASPOBJ_"; // Audio Source Pool OBJect

        private const string SharedAudioSourceNamePrefix = "SASOBJ_"; // Shared Audio Source OBJect

        private const string AudioSourceParentName = "AudioSourceManager";

        private static readonly AudioSource[] SharedAudioSources = new AudioSource[SoundTagUtility.SoundTagCount];

        private const string MixerResourcePath = "Settings/AudioMixer";

        #endregion

        #region variable

        private static AudioMixer audioMixer = null;

        private static Transform audioSourceParent = null;

        #endregion

        #region logic

        #region Initialise

        [CoreInitialise(int.MinValue)]
        private static void Initialise() {
            // load audio mixer:
            audioMixer = Resources.Load<AudioMixer>(MixerResourcePath);
            if (audioMixer == null) {
                Core.Quit(QuitReason.FatalCrash, $"No AudioMixer resource found at \"Resources/{MixerResourcePath}\"", null, true);
                return;
            }
            AudioMixerGroup[] groups;
            for (int i = 0; i < SoundTagUtility.SoundTagCount; i++) {
                groups = audioMixer.FindMatchingGroups(((SoundTag)i).ToString());
                if (groups.Length != 1) { // should only be one mixer group
                    Core.Quit(QuitReason.FatalCrash, $"AudioMixer at \"Resources/{MixerResourcePath}\" does not contain a mixer group at \"Master/{(SoundTag)i}\".", null, true);
                    return;
                }
                SoundTagMixerGroups[i] = groups[0];
            }
            // create audio source parent GameObject:
            audioSourceParent = new GameObject(AudioSourceParentName) {
                hideFlags = HideFlags.HideInHierarchy,
                isStatic = true
            }.transform;
            Object.DontDestroyOnLoad(audioSourceParent.gameObject);
            // create shared audio sources:
            AudioSource instance;
            for (int i = SharedAudioSources.Length - 1; i >= 0; i--) {
                instance = new GameObject(string.Concat(SharedAudioSourceNamePrefix, i), typeof(AudioSource)) {
                    hideFlags = HideFlags.HideInHierarchy
                }.GetComponent<AudioSource>();
                instance.outputAudioMixerGroup = SoundTagMixerGroups[i];
                instance.transform.parent = audioSourceParent;
            }
        }

        #endregion

        #region GetSingle

        internal static AudioSource GetSingle(in SoundTag tag) {
            AudioSource audioSource = AudioSourcePool.GetObject();
            audioSource.outputAudioMixerGroup = SoundTagMixerGroups[(int)tag];
            return audioSource;
        }

        #endregion

        #region ReturnSingle

        internal static void ReturnSingle(in AudioSource audioSource) => AudioSourcePool.ReturnToPool(audioSource);

        #endregion

        #region GetShared

        internal static AudioSource GetShared(in SoundTag tag) => SharedAudioSources[(int)tag];

        #endregion

        #region CreateAudioSourcePoolObject

        private static AudioSource CreateAudioSourcePoolObject(in int index) {
            AudioSource audioSource = new GameObject(string.Concat(AudioSourceNamePrefix, index), typeof(AudioSource)) {
                hideFlags = HideFlags.HideInHierarchy
            }.GetComponent<AudioSource>();
            audioSource.transform.parent = audioSourceParent;
            return audioSource;
        }

        #endregion

        #region ReturnAudioSourceToPool

        /// <summary>
        /// Invoked when an <see cref="AudioSource"/> is returned to its pool.
        /// </summary>
        private static void ReturnAudioSourceToPool(in AudioSource audioSource) {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.gameObject.SetActive(false);
        }

        #endregion

        #region ReturnAudioSourceFromPool

        /// <summary>
        /// Invoked when an <see cref="AudioSource"/> is requested from its pool.
        /// </summary>
        private static void RemoveAudioSourceFromPool(in AudioSource audioSource) {
            audioSource.gameObject.SetActive(true);
        }

        #endregion

        #endregion

    }

}