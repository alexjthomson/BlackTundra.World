using BlackTundra.Foundation.Utility;
using BlackTundra.World.Items;

using UnityEngine;

namespace BlackTundra.World.XR {

    /// <summary>
    /// Tracks if the bounds of two groups of <see cref="Collider"/> components intersects.
    /// </summary>
    internal sealed class XRHandCollisionTracker {

        #region constant

        /// <summary>
        /// Number of seconds without intersection before the tracker will be disabled.
        /// </summary>
        private const float DisableTime = 0.1f;

        #endregion

        #region variable

        internal readonly WorldItem item;
        internal readonly Collider[] group1;
        internal readonly Collider[] group2;
        internal float timer;

        #endregion

        #region constructor

        internal XRHandCollisionTracker(in WorldItem item, in Collider[] group1, in Collider[] group2) {
            this.item = item;
            this.group1 = group1;
            this.group2 = group2;
            timer = DisableTime;
        }

        #endregion

        #region logic

        internal void ResetTimer() {
            timer = DisableTime;
        }

        internal bool IsIntersecting() {
            Bounds b1 = group1.CalculateBounds();
            Bounds b2 = group2.CalculateBounds();
            return b1.Intersects(b2);
        }

        internal void EnableCollisions() {
            Collider c1;
            Collider c2;
            int g2l = group2.Length - 1;
            for (int i = group1.Length - 1; i >= 0; i--) {
                c1 = group1[i];
                for (int j = 0; j < g2l; j++) {
                    c2 = group2[j];
                    Physics.IgnoreCollision(c1, c2, false);
                }
            }
        }

        #endregion

    }

}