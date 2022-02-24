using UnityEngine;

namespace BlackTundra.World {

    [RequireComponent(typeof(Rigidbody))]
    public sealed class CentreOfMass : MonoBehaviour {

        #region variable

        /// <summary>
        /// Local centre of mass.
        /// </summary>
        [SerializeField]
        private Vector3 localPosition = Vector3.zero;

        #endregion

        #region logic

        private void Awake() {
            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody != null) {
                rigidbody.centerOfMass = localPosition;
            }
            enabled = false;
            Destroy(this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.TransformPoint(localPosition), 0.1f);
        }
#endif

        #endregion

    }

}