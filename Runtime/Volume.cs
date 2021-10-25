using BlackTundra.Foundation.Utility;

using System;
using System.Collections.Generic;

using UnityEngine;

using Console = BlackTundra.Foundation.Console;

namespace BlackTundra.World {

    /// <summary>
    /// Describes a <see cref="Volume"/> of space.
    /// </summary>
    [DisallowMultipleComponent]
    //[RequireComponent(typeof(Collider))]
#if UNITY_EDITOR
    [AddComponentMenu("Physics/Volume")]
#endif
    public sealed class Volume : MonoBehaviour {

        #region constant

        /// <summary>
        /// Every <see cref="Volume"/> instance.
        /// </summary>
        private static readonly List<Volume> VolumeList = new List<Volume>();

        #endregion

        #region variable

        /// <summary>
        /// <see cref="Collider"/> attached to the <see cref="Volume"/> used as a trigger.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
        private Collider collider = null;

        /// <inheritdoc cref="global"/>
        [SerializeField]
        private bool _global = false;

        /// <inheritdoc cref="weight"/>
        [SerializeField]
        private float _weight = 1.0f;

        /// <inheritdoc cref="blendDistance"/>
        [SerializeField]
        private float _blendDistance = 0.0f;

        /// <inheritdoc cref="tags"/>
        [SerializeField]
        private string[] _tags = new string[0];

        /// <summary>
        /// Hash codes generate from the <see cref="_tags"/> array.
        /// </summary>
        private int[] tagHashCodes = null;

        #endregion

        #region property

        public bool global {
            get => _global;
            set {
                if (value == _global) return;
                _global = value;
                if (!_global) GetCollider();
            }
        }

        /// <summary>
        /// Maximum influence that this <see cref="Volume"/> will have on a point inside the volume.
        /// </summary>
        public float weight {
            get => _weight;
            set => _weight = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Distance from the <see cref="Volume"/> that the volume will begin to have an effect.
        /// </summary>
        public float blendDistance {
            get => _blendDistance;
            set => _blendDistance = value > 0.0f ? value : 0.0f;
        }

        /// <summary>
        /// Tags associated with the <see cref="Volume"/>.
        /// This can be used to describe properties of the <see cref="Volume"/>.
        /// </summary>
        public string[] tags {
            get {
                int tagCount = _tags.Length;
                string[] tagBuffer = new string[tagCount];
                Array.Copy(_tags, 0, tagBuffer, 0, tagCount);
                return tagBuffer;
            }
            set {
                if (value == null) {
                    _tags = new string[0];
                    return;
                }
                int tagCount = value.Length;
                if (_tags == null || _tags.Length != tagCount) _tags = new string[tagCount];
                Array.Copy(value, 0, _tags, 0, tagCount);
                RecalculateTagHashCodes();
            }
        }

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            GetCollider();
            VolumeList.Add(this);
            RecalculateTagHashCodes();
        }

        #endregion

        #region RecalculateTagHashCodes

        private void RecalculateTagHashCodes() {
            int tagCount = _tags.Length;
            if (tagHashCodes == null || tagHashCodes.Length != tagCount) tagHashCodes = new int[tagCount];
            for (int i = tagCount - 1; i >= 0; i--) tagHashCodes[i] = _tags[i].GetHashCode();
        }

        #endregion

        #region OnDestroy

        private void OnDestroy() {
            VolumeList.Remove(this); // remove from the list of all volumes
        }

        #endregion

        #region GetCollider

        private void GetCollider() {
            collider = GetComponent<Collider>();
            if (collider == null) Console.Warning("Collider expected on non-global volume.");
            else if (!collider.isTrigger) {
                Console.Warning($"Volume \"{name}\" collider is not a trigger; the collider will be converted to a trigger.");
                collider.isTrigger = true;
            }
        }

        #endregion

        #region HasTag

        public bool HasTag(in string tag) {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            int hash = tag.GetHashCode();
            for (int i = tagHashCodes.Length - 1; i >= 0; i--) {
                if (hash == tagHashCodes[i]) return true;
            }
            return false;
        }

        private bool HasTag(in int hash) {
            for (int i = tagHashCodes.Length - 1; i >= 0; i--) {
                if (hash == tagHashCodes[i]) return true;
            }
            return false;
        }

        #endregion

        #region IndexOfTag

        private int IndexOfTag(in string tag) {
            int hash = tag.GetHashCode();
            for (int i = tagHashCodes.Length - 1; i >= 0; i--) {
                if (hash == tagHashCodes[i]) return i;
            }
            return -1;
        }

        #endregion

        #region AddTag

        public bool AddTag(in string tag) {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (HasTag(tag)) return false;
            _tags = _tags.AddLast(tag);
            tagHashCodes = tagHashCodes.AddLast(tag.GetHashCode());
            return true;
        }

        #endregion

        #region RemoveTag

        public bool RemoveTag(in string tag) {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            int index = IndexOfTag(tag);
            if (index != -1) {
                _tags = _tags.RemoveAt(index);
                tagHashCodes = tagHashCodes.RemoveAt(index);
                return true;
            }
            return false;
        }

        #endregion

        #region QueryPoint

        /// <summary>
        /// Queries the <see cref="Volume"/> instances that have influence on the provided <paramref name="point"/>.
        /// </summary>
        /// <param name="point">Point in world-space to query.</param>
        /// <param name="layermask"><see cref="LayerMask"/> to use to find <see cref="Volume"/> instances.</param>
        /// <returns>Returns every <see cref="VolumeHit"/> that occurs during the query.</returns>
        public static IEnumerator<VolumeHit> QueryPoint(Vector3 point, LayerMask layermask) {
            Volume volume; // used to store a reference to the current volume
            for (int i = VolumeList.Count - 1; i >= 0; i--) { // iterate each volume
                volume = VolumeList[i]; // get the current volume
                if (volume == null) { // null reference (shoud not happen)
                    VolumeList.RemoveAt(i); // remove from list
                    continue; // continue
                }
                if (!volume.enabled || (volume.gameObject.layer & layermask) == 0) continue; // volume is not enabled
                Vector3 closestPoint = volume.collider.ClosestPoint(point); // get the closest point on the collider to the point
                float sqrDistance = (closestPoint - point).sqrMagnitude; // calculate the square distance from the point to the closest point
                float sqrBlendDistance = volume._blendDistance * volume._blendDistance;

                /* Note:
                 * Volume doesn't do anything when `sqrDistance = sqrBlendDistance`, but we can't
                 * use a >= comparison as sqrBlendDistance could be set to 0, in which case, the
                 * volume would always have total influence.
                 */
                if (sqrDistance > sqrBlendDistance) continue; // volume has no influence

                // calculate the influence that the volume will have:
                float influence = sqrBlendDistance > 0.0f
                    ? 1.0f - (sqrDistance / sqrBlendDistance)
                    : 1.0f;

                yield return new VolumeHit(volume, point, influence * volume._weight); // return that this volume was hit
            }
        }

        #endregion

        #region QueryTagAtPoint

        /// <summary>
        /// Queries the influence value at a <paramref name="point"/> of a specified <paramref name="tag"/>.
        /// </summary>
        /// <returns>
        /// Returns a value between <c>0.0</c> and <c>1.0</c> that indicates the influence of a tag at a <paramref name="point"/>.
        /// </returns>
        public static float QueryTagAtPoint(in string tag, in Vector3 point, in LayerMask layermask) => QueryTagAtPoint(tag.GetHashCode(), point, layermask);
        public static float QueryTagAtPoint(in int hash, in Vector3 point, in LayerMask layermask) {
            Volume volume; // used to store a reference to the current volume
            float totalInfluence = 0.0f;
            for (int i = VolumeList.Count - 1; i >= 0; i--) { // iterate each volume
                volume = VolumeList[i]; // get the current volume
                if (volume == null) { // null reference (shoud not happen)
                    VolumeList.RemoveAt(i); // remove from list
                    continue; // continue
                }
                if (!volume.enabled || (volume.gameObject.layer & layermask) == 0 || !volume.HasTag(hash)) continue; // volume is not enabled
                Vector3 closestPoint = volume.collider.ClosestPoint(point); // get the closest point on the collider to the point
                float sqrDistance = (closestPoint - point).sqrMagnitude; // calculate the square distance from the point to the closest point
                float sqrBlendDistance = volume._blendDistance * volume._blendDistance;

                /* Note:
                 * Volume doesn't do anything when `sqrDistance = sqrBlendDistance`, but we can't
                 * use a >= comparison as sqrBlendDistance could be set to 0, in which case, the
                 * volume would always have total influence.
                 */
                if (sqrDistance > sqrBlendDistance) continue; // volume has no influence

                // calculate the influence that the volume will have:
                float influence = sqrBlendDistance > 0.0f
                    ? 1.0f - (sqrDistance / sqrBlendDistance)
                    : 1.0f;

                if (influence > totalInfluence) totalInfluence = influence;
            }
            return totalInfluence;
        }

        #endregion

        #endregion

    }

}