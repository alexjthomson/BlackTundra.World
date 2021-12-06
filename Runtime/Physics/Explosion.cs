using BlackTundra.World.CameraSystem;
using BlackTundra.World.Items;

using System;

using UnityEngine;

namespace BlackTundra.World {

    public static class Explosion {

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
                Rigidbody rigidbody;
                WorldItem item;
                for (int i = colliderCount - 1; i >= 0; i--) {
                    rigidbody = colliders[i].GetComponent<Rigidbody>();
                    if (rigidbody != null) {
                        item = rigidbody.GetComponent<WorldItem>();
                        if (item != null) item.EnablePhysics();
                        rigidbody.AddExplosionForce(force, point, radius);
                    }
                }
            }
            if (force > 1000.0f) {
                force -= 1000.0f;
                return CameraShakeSource.CreateAt(
                    point,
                    Mathf.Clamp(force * 0.005f, 0.0f, 1.0f),
                    Mathf.Clamp(force * 0.01f, 25.0f, 75.0f),
                    false,
                    Mathf.Clamp(force * 0.00025f, 0.5f, 1.0f),
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