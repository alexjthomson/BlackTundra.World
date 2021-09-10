using UnityEngine;

namespace BlackTundra.WorldSystem.Paths {

    public static class PathUtility {

        #region AverageScale

        private static float AverageScale(this Transform transform) {
            Vector3 scale = transform.lossyScale;
            return (scale.x + scale.y + scale.z) * 0.33333333333333333333f;
        }

        #endregion

        #region ConstrainPositionRotation

        private static void ConstrainPositionRotation(ref Vector3 position, ref Quaternion rotation, in PathSpace space) {

            switch (space) {

                case PathSpace.xy: {

                    Vector3 eulerAngles = rotation.eulerAngles;
                    if (eulerAngles.x != 0.0f || eulerAngles.y != 0.0f) rotation = Quaternion.AngleAxis(eulerAngles.z, Vector3.forward);
                    position = new Vector3(position.x, position.y);
                    break;

                }

                case PathSpace.xz: {

                    Vector3 eulerAngles = rotation.eulerAngles;
                    if (eulerAngles.x != 0.0f || eulerAngles.z != 0.0f) rotation = Quaternion.AngleAxis(eulerAngles.y, Vector3.up);
                    position = new Vector3(position.x, 0.0f, position.z);
                    break;

                }

            }

        }

        #endregion

        #region ConstrainRotation

        private static void ConstrainRotation(ref Quaternion rotation, in PathSpace space) {

            switch (space) {

                case PathSpace.xy: {

                    Vector3 eulerAngles = rotation.eulerAngles;
                    if (eulerAngles.x != 0.0f || eulerAngles.y != 0.0f) rotation = Quaternion.AngleAxis(eulerAngles.z, Vector3.forward);
                    break;

                }

                case PathSpace.xz: {

                    Vector3 eulerAngles = rotation.eulerAngles;
                    if (eulerAngles.x != 0.0f || eulerAngles.z != 0.0f) rotation = Quaternion.AngleAxis(eulerAngles.y, Vector3.up);
                    break;

                }

            }

        }

        #endregion

        #region TransformPoint

        public static Vector3 TransformPoint(this Transform transform, in Vector3 point, in PathSpace space) {

            float scale = transform.AverageScale();
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            ConstrainPositionRotation(ref position, ref rotation, space);
            return rotation * point * scale + position;

        }

        #endregion

        #region InverseTransformPoint

        public static Vector3 InverseTransformPoint(this Transform transform, in Vector3 point, in PathSpace space) {

            float scale = transform.AverageScale();
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            ConstrainPositionRotation(ref position, ref rotation, space);
            return Quaternion.Inverse(rotation) * (point - position) * (1.0f / scale);

        }

        #endregion

        #region TransformDirection

        public static Vector3 TransformDirection(this Transform transform, in Vector3 direction, in PathSpace space) {

            Quaternion rotation = transform.rotation;
            ConstrainRotation(ref rotation, space);
            return rotation * direction;

        }

        #endregion

        #region InverseTransformDirection

        public static Vector3 InverseTransformDirection(this Transform transform, in Vector3 direction, in PathSpace space) {
            Quaternion rotation = transform.rotation;
            ConstrainRotation(ref rotation, space);
            return Quaternion.Inverse(rotation) * direction;
        }

        #endregion

        #region TransformVector

        public static Vector3 TransformVector(this Transform transform, in Vector3 vector, in PathSpace space) {
            float scale = transform.AverageScale();
            Quaternion rotation = transform.rotation;
            ConstrainRotation(ref rotation, space);
            return rotation * vector * scale;
        }

        #endregion

        #region InverseTransformVector

        public static Vector3 InverseTransformVector(this Transform transform, in Vector3 vector, in PathSpace space) {
            float scale = transform.AverageScale();
            Quaternion rotation = transform.rotation;
            ConstrainRotation(ref rotation, space);
            return Quaternion.Inverse(rotation) * vector * (1.0f / scale);
        }

        #endregion

    }

}