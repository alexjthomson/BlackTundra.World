using BlackTundra.World.CameraSystem;

using System;

using UnityEngine;

namespace BlackTundra.World {

    public static class Explosion {

        #region constant

        /// <summary>
        /// Minimum force amount before camera shake is created from an explosion.
        /// </summary>
        private const float ThresholdCameraShakeForce = 2.5f;

        /// <summary>
        /// Force amount that has the most amount of camera shake applied when reached.
        /// </summary>
        private const float MaxCameraShakeForce = 15.0f;

        /// <summary>
        /// Coefficient used to scale down a force into a camera shake amount.
        /// </summary>
        private const float CameraShakeScalingCoefficient = 1.0f / (MaxCameraShakeForce - ThresholdCameraShakeForce);

        #endregion

        #region logic

        /// <summary>
        /// Creates an <see cref="Explosion"/> with a specified <paramref name="force"/> and <paramref name="radius"/> at a <paramref name="point"/>.
        /// </summary>
        public static CameraShakeSource CreateAt(in Vector3 point, in float radius, float force, in LayerMask layerMask) {
            if (radius <= 0.0f) throw new ArgumentOutOfRangeException(nameof(radius));
            if (force <= 0.0f) throw new ArgumentOutOfRangeException(nameof(force));
            Collider[] colliders = Physics.OverlapSphere(point, radius, layerMask);
            int colliderCount = colliders.Length;
            if (colliderCount > 0) {
                Collider collider;
                Rigidbody rigidbody;
                IPhysicsObject physicsObject;
                for (int i = colliderCount - 1; i >= 0; i--) {
                    collider = colliders[i];
                    physicsObject = collider.GetComponent<IPhysicsObject>();
                    if (physicsObject != null) {
                        physicsObject.AddExplosionForce(force, point, radius, 0.0f, ForceMode.Impulse);
                    } else {
                        rigidbody = collider.GetComponent<Rigidbody>();
                        if (rigidbody != null) {
                            rigidbody.AddExplosionForce(force, point, radius, 0.0f, ForceMode.Impulse);
                        }
                    }
                }
            }
            if (force > ThresholdCameraShakeForce) {
                float cameraShakeAmount = (force - ThresholdCameraShakeForce) * CameraShakeScalingCoefficient;
                return CameraShakeSource.CreateAt(
                    point,
                    Mathf.Lerp(cameraShakeAmount, 0.05f, 0.25f),
                    Mathf.Lerp(cameraShakeAmount, 25.0f, 75.0f),
                    false,
                    Mathf.Lerp(cameraShakeAmount, 0.5f, 1.0f),
                    0.0f,
                    0.25f
                );
            } else {
                return null;
            }
        }

        #endregion

    }

}