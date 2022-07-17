using BlackTundra.Foundation;
using BlackTundra.Foundation.Utility;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace BlackTundra.World {

    /// <summary>
    /// Describes a <see cref="Volume"/> of space.
    /// </summary>
    [DisallowMultipleComponent]
    //[RequireComponent(typeof(Collider))]
#if UNITY_EDITOR
    [AddComponentMenu("World/Volume")]
#endif
    public sealed class Volume : MonoBehaviour {

        #region constant

        /// <summary>
        /// Every <see cref="Volume"/> instance.
        /// </summary>
        private static readonly List<Volume> VolumeList = new List<Volume>();

        private static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter(nameof(Volume));

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
        internal float _weight = 1.0f;

        /// <inheritdoc cref="blendDistance"/>
        [SerializeField]
        private float _blendDistance = 0.0f;

        /// <inheritdoc cref="tags"/>
        [SerializeField]
        private string[] _tags = new string[0];

        /// <summary>
        /// Layer converted into a layer flag.
        /// </summary>
        private int layerFlag;

        /// <summary>
        /// <see cref="_blendDistance"/> squared.
        /// </summary>
        internal float sqrBlendDistance;

        /// <summary>
        /// <code>1.0f / <see cref="sqrBlendDistance"/></code>.
        /// </summary>
        internal float inverseSqrBlendDistance;

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
            set {
                _blendDistance = value > 0.0f ? value : 0.0f;
                sqrBlendDistance = _blendDistance * _blendDistance;
                inverseSqrBlendDistance = 1.0f / sqrBlendDistance;
            }
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
            RecalculateTagHashCodes();
        }

        #endregion

        #region OnEnable

        private void OnEnable() {
            layerFlag = 1 << gameObject.layer;
            sqrBlendDistance = _blendDistance * _blendDistance;
            inverseSqrBlendDistance = 1.0f / sqrBlendDistance;
            VolumeList.Add(this); // add to the list of all volumes
        }

        #endregion

        #region OnDisable

        private void OnDisable() {
            VolumeList.Remove(this); // remove from the list of all volumes
        }

        #endregion

        #region OnDestroy
        /*
        private void OnDestroy() {
            VolumeList.Remove(this); // remove from the list of all volumes
        }
        */
        #endregion

        #region RecalculateTagHashCodes

        private void RecalculateTagHashCodes() {
            int tagCount = _tags.Length;
            if (tagHashCodes == null || tagHashCodes.Length != tagCount) tagHashCodes = new int[tagCount];
            for (int i = tagCount - 1; i >= 0; i--) tagHashCodes[i] = _tags[i].GetHashCode();
        }

        #endregion

        #region GetCollider

        private void GetCollider() {
            collider = GetComponent<Collider>();
            if (collider == null) ConsoleFormatter.Warning("Collider expected on non-global volume.");
            else if (!collider.isTrigger) {
                ConsoleFormatter.Warning($"Volume `{name}` collider is not a trigger; the collider will be converted to a trigger.");
#if UNITY_EDITOR
                Debug.LogWarning("Volume collider is not trigger.", collider);
#endif
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

        /// <param name="tags">Tags to check for.</param>
        /// <param name="any">When true, the method will return <c>true</c> if just one of the tags in the <paramref name="tags"/> parameter is present.</param>
        /// <returns>
        /// Returns <c>true</c> if the <see cref="Volume"/> has either all <paramref name="tags"/> (if <paramref name="any"/> is <c>false</c>) or just one of the
        /// <paramref name="tags"/> (if <paramref name="any"/> is <c>true</c>).
        /// </returns>
        private bool HasTag(in string[] tags, in bool any) {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            int hashCode;
            if (any) {
                for (int i = tags.Length - 1; i >= 0; i--) {
                    hashCode = tags[i].GetHashCode();
                    for (int j = tagHashCodes.Length - 1; j >= 0; j--) {
                        if (tagHashCodes[j] == hashCode) return true;
                    }
                }
                return false;
            } else {
                bool missing;
                for (int i = tags.Length - 1; i >= 0; i--) {
                    missing = true;
                    hashCode = tags[i].GetHashCode();
                    for (int j = tagHashCodes.Length - 1; j >= 0; j--) {
                        if (tagHashCodes[j] == hashCode) {
                            missing = false;
                            break;
                        }
                    }
                    if (missing) return false; // this tag is missing
                }
                return true; // all tags found
            }
        }

        private bool HasTag(in int[] tags, in bool any) {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            int hashCode;
            if (any) {
                for (int i = tags.Length - 1; i >= 0; i--) {
                    hashCode = tags[i];
                    for (int j = tagHashCodes.Length - 1; j >= 0; j--) {
                        if (tagHashCodes[j] == hashCode) return true;
                    }
                }
                return false;
            } else {
                bool missing;
                for (int i = tags.Length - 1; i >= 0; i--) {
                    missing = true;
                    hashCode = tags[i];
                    for (int j = tagHashCodes.Length - 1; j >= 0; j--) {
                        if (tagHashCodes[j] == hashCode) {
                            missing = false;
                            break;
                        }
                    }
                    if (missing) return false; // this tag is missing
                }
                return true; // all tags found
            }
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

        public static IEnumerator<VolumeHit> QueryPoint(Vector3 point, LayerMask layerMask, int tag) {
            Volume volume; // used to store a reference to the current volume
            for (int i = VolumeList.Count - 1; i >= 0; i--) { // iterate each volume
                volume = VolumeList[i]; // get the current volume
                if ((volume.layerFlag & layerMask) == 0 || !volume.HasTag(tag)) continue; // volume not in layermask or doesn't have any of the tags
                if (volume._global) {
                    yield return new VolumeHit(volume, point, 0.0f);
                } else {
                    Vector3 closestPoint = volume.collider.ClosestPoint(point); // get the closest point on the collider to the point
                    float sqrDistance = (closestPoint - point).sqrMagnitude; // calculate the square distance from the point to the closest point
                    /* Note:
                     * Volume doesn't do anything when `sqrDistance = sqrBlendDistance`, but we can't
                     * use a >= comparison as sqrBlendDistance could be set to 0, in which case, the
                     * volume would always have total influence.
                     */
                    if (sqrDistance > volume.sqrBlendDistance) continue; // volume has no influence
                    yield return new VolumeHit(volume, closestPoint, sqrDistance); // return that this volume was hit
                }
            }
        }

        public static IEnumerator<VolumeHit> QueryPoint(Vector3 point, LayerMask layerMask, params int[] tags) {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (tags.Length == 0) throw new ArgumentException(nameof(tags));
            Volume volume; // used to store a reference to the current volume
            for (int i = VolumeList.Count - 1; i >= 0; i--) { // iterate each volume
                volume = VolumeList[i]; // get the current volume
                if ((volume.layerFlag & layerMask) == 0 || !volume.HasTag(tags, true)) continue; // volume not in layermask or doesn't have any of the tags
                if (volume._global) {
                    yield return new VolumeHit(volume, point, 0.0f);
                } else {
                    Vector3 closestPoint = volume.collider.ClosestPoint(point); // get the closest point on the collider to the point
                    float sqrDistance = (closestPoint - point).sqrMagnitude; // calculate the square distance from the point to the closest point
                    /* Note:
                     * Volume doesn't do anything when `sqrDistance = sqrBlendDistance`, but we can't
                     * use a >= comparison as sqrBlendDistance could be set to 0, in which case, the
                     * volume would always have total influence.
                     */
                    if (sqrDistance > volume.sqrBlendDistance) continue; // volume has no influence
                    yield return new VolumeHit(volume, closestPoint, sqrDistance); // return that this volume was hit
                }
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
        public static float QueryTagAtPoint(in int tag, in Vector3 point, in LayerMask layermask) {
            Volume volume; // used to store a reference to the current volume
            float totalInfluence = 0.0f;
            for (int i = VolumeList.Count - 1; i >= 0; i--) { // iterate each volume
                volume = VolumeList[i]; // get the current volume
                if ((volume.layerFlag & layermask) == 0 || !volume.HasTag(tag)) continue; // volume is not in layermask or doesn't have the tag
                if (volume._global) {
                    if (volume.weight > totalInfluence)
                        totalInfluence = volume.weight;
                } else {
                    Vector3 closestPoint = volume.collider.ClosestPoint(point); // get the closest point on the collider to the point
                    float sqrDistance = (closestPoint - point).sqrMagnitude; // calculate the square distance from the point to the closest point

                    /* Note:
                     * Volume doesn't do anything when `sqrDistance = sqrBlendDistance`, but we can't
                     * use a >= comparison as sqrBlendDistance could be set to 0, in which case, the
                     * volume would always have total influence.
                     */
                    if (sqrDistance > volume.sqrBlendDistance) continue; // volume has no influence

                    // calculate the influence that the volume will have:
                    float influence = volume.sqrBlendDistance > 0.0f
                        ? volume._weight * (1.0f - (sqrDistance * volume.inverseSqrBlendDistance))
                        : volume._weight;

                    if (influence > totalInfluence)
                        totalInfluence = influence;
                }
            }
            return totalInfluence;
        }

        #endregion

        #region QueryTagInRange

        public static IEnumerator<VolumeHit> QueryTagInRange(Vector3 point, float range, LayerMask layerMask, int tag) {
            if (range < 0.0f) throw new ArgumentOutOfRangeException(nameof(range));
            if (range == 0.0f) yield break;
            range *= range; // square the range
            Volume volume;
            for (int i = VolumeList.Count - 1; i >= 0; i--) { // iterate each volume
                volume = VolumeList[i];
                if ((volume.layerFlag & layerMask) == 0 || !volume.HasTag(tag)) continue;
                if (volume._global) {
                    yield return new VolumeHit(volume, point, 0.0f);
                } else {
                    Vector3 closestPoint = volume.collider.ClosestPoint(point);
                    float sqrDistance = (volume.transform.position - point).sqrMagnitude;
                    if (sqrDistance > range) continue; // volume out of range
                    yield return new VolumeHit(volume, closestPoint, sqrDistance); // return that this volume was hit
                }
            }
        }

        public static IEnumerator<VolumeHit> QueryTagInRange(Vector3 point, float range, LayerMask layerMask, params int[] tags) {
            if (range < 0.0f) throw new ArgumentOutOfRangeException(nameof(range));
            if (range == 0.0f) yield break;
            range *= range; // square the range
            Volume volume;
            for (int i = VolumeList.Count - 1; i >= 0; i--) { // iterate each volume
                volume = VolumeList[i];
                if ((volume.layerFlag & layerMask) == 0 || !volume.HasTag(tags, true)) continue;
                if (volume._global) {
                    yield return new VolumeHit(volume, point, 0.0f);
                } else {
                    Vector3 closestPoint = volume.collider.ClosestPoint(point);
                    float sqrDistance = (volume.transform.position - point).sqrMagnitude;
                    if (sqrDistance > range) continue; // volume out of range
                    yield return new VolumeHit(volume, closestPoint, sqrDistance); // return that this volume was hit
                }
            }
        }

        #endregion

        #endregion

    }

}