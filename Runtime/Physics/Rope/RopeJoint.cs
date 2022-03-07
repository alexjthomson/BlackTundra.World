using UnityEngine;

namespace BlackTundra.World {

    internal sealed class RopeJoint {

        #region variable

        internal readonly RopePoint p1;

        internal readonly RopePoint p2;

        internal readonly float length;

        #endregion

        #region property

        public Vector3 centre => (p1.position + p2.position) * 0.5f;

        public Vector3 direction => (p2.position - p1.position).normalized;

        public float actualLength => Vector3.Distance(p1.position, p2.position);

        #endregion

        #region constructor

        internal RopeJoint(in RopePoint p1, in RopePoint p2, in float length) {
            this.p1 = p1;
            this.p2 = p2;
            this.length = length;
        }

        #endregion

    }

}