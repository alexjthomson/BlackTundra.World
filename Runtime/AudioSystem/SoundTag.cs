using System;

namespace BlackTundra.World.Audio {

    // TODO: rewrite the descriptions to exclude any mention of channel

    /// <summary>
    /// Describes different well-defined <see cref="SoundTag">audio channels</see> that the audio
    /// system knows how to handle. All sounds should fit into one of these classifications.
    /// </summary>
    /// <remarks>
    /// The values of each <see cref="SoundTag"/> are NOT flags, they should increment by <c>1</c>
    /// each time.
    /// </remarks>
    public enum SoundTag : int {

        /// <summary>
        /// <para>Environmental Sound</para>
        /// <para>
        /// This channel is for sounds that should exist within the <see cref="Soundscape"/> and therefore
        /// sounds that can be heard by listeners within the <see cref="Soundscape"/>. Any sounds that are
        /// not music and do not exist in the <see cref="Soundscape"/> should be marked as
        /// <see cref="SFX"/>.
        /// </para>
        /// </summary>
        ENV = 0,

        /// <summary>
        /// <para>Sound Effect</para>
        /// <para>
        /// This channel is dedicated to sounds that do not belong to the environment and are not music.
        /// This includes things such as UI sounds and other sounds that do not actually exist within
        /// the <see cref="Soundscape"/> but should still be heard.
        /// </para>
        /// </summary>
        SFX = 1,

        /// <summary>
        /// <para>Music</para>
        /// <para>
        /// This channel is decicated for only music. This includes any sound effects that may be part of
        /// any music. This channel is NOT for music that is playing in the world/<see cref="Soundscape"/>;
        /// any sounds that are part of the world should be marked as <see cref="ENV"/>.
        /// </para>
        /// </summary>
        MUS = 2

    }

    public static class SoundTagUtility {

        #region constant

        public static readonly int SoundTagCount;

        #endregion

        #region constructor

        static SoundTagUtility() {
            SoundTagCount = Enum.GetValues(typeof(SoundTag)).Length;
        }

        #endregion

    }

}