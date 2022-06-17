using System;

namespace BlackTundra.World {

    /// <summary>
    /// Describes a type of damage being inflicted.
    /// </summary>
    [Serializable]
    public enum DamageType : byte {

        /// <summary>
        /// Bludgeoning impact damage.
        /// </summary>
        BluntImpact = 0x01,

        /// <summary>
        /// Slashing damage which is performed by a sharp object.
        /// </summary>
        Slashing = 0x02,

        /// <summary>
        /// Piercing damage caused when a sharp object penetrates another.
        /// </summary>
        Piercing = 0x03,

        /// <summary>
        /// Environmental damage that may be caused by temperature, radiation, acid, or something else.
        /// </summary>
        Environment = 0x04,

        /// <summary>
        /// A non-environmental effect has caused damage. This could be poison, electrical, or something else.
        /// </summary>
        Effect = 0x05,

        /// <summary>
        /// A source of damage that is not defined by the pre-determind categories.
        /// </summary>
        Other = 0x00
    }

}