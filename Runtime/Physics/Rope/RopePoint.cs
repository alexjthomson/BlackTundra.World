using UnityEngine;

namespace BlackTundra.World {

    internal sealed class RopePoint {

        #region variable

        internal Vector3 position;
        internal Vector3 lastPosition;
        internal readonly bool simulate;

        #endregion

        #region constructor

        internal RopePoint(in Vector3 position, in bool simulate) {
            this.position = position;
            lastPosition = position;
            this.simulate = simulate;
        }

        #endregion

    }

}