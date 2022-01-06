using BlackTundra.Foundation.Collections.Generic;
using BlackTundra.Foundation.Control;
using BlackTundra.Foundation.Utility;

using System;

using UnityEngine;

namespace BlackTundra.World.CameraSystem {

    /// <summary>
    /// Manages advanced camera movement logic. The <see cref="CameraController"/> includes many gameplay
    /// enhancing features such as camera shake and camera zoom.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu("World/Camera/Camera Controller")]
#endif
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(AudioSource))]
    [DefaultExecutionOrder(-100)]
    public sealed class CameraController : MonoBehaviour {

        #region constant

        /// <summary>
        /// Default name of a <see cref="CameraController"/> <see cref="GameObject"/>.
        /// </summary>
        public const string DefaultCameraControllerName = "CameraController";

        /// <summary>
        /// Tag to apply to any <see cref="GameObject"/> with a <see cref="CameraController"/> component.
        /// </summary>
        public const string CameraControllerTag = "MainCamera";

        /// <summary>
        /// Camera shake scalar to apply to controller rumble.
        /// </summary>
        private const float CameraShakeRumbleFrequency = 0.125f;

        /// <summary>
        /// Lerp speed to use for the wind volume.
        /// </summary>
        /// <remarks>
        /// This also applies to the wind pitch since it is based off of the volume.
        /// </remarks>
        private const float WindVolumeLerpSpeed = 2.5f;

        /// <summary>
        /// Maximum velocity that the wind can blow at.
        /// </summary>
        private const float MaxWindVolumeVelocity = 40.0f;

        /// <summary>
        /// Coefficient to convert from the square speed of the wind to the relative volume of the wind.
        /// </summary>
        private const float SqrSpeedToWindVolume = 1.0f / (MaxWindVolumeVelocity * MaxWindVolumeVelocity);

        /// <summary>
        /// <see cref="PackedBuffer{T}"/> containing every available <see cref="CameraController"/>.
        /// </summary>
        private static readonly PackedBuffer<CameraController> CameraControllerBuffer = new PackedBuffer<CameraController>(1);

        #endregion

        #region variable

        /// <summary>
        /// ID of the <see cref="CameraController"/>.
        /// </summary>
        public readonly int id = unchecked(rollingCameraId++);

        /// <summary>
        /// Target <see cref="Transform"/> component to track.
        /// </summary>
        private Transform _target = null;

        /// <inheritdoc cref="TrackingFlags"/>
        private CameraTrackingFlags trackingFlags = CameraTrackingFlags.None;

        /// <inheritdoc cref="PositionalTrackingSpeed"/>
        private float positionTrackingSpeed = 1.0f;

        /// <inheritdoc cref="RotationalTrackingSpeed"/>
        private float rotationTrackingSpeed = 1.0f;

        /// <summary>
        /// Tracks the target field of view of the <see cref="camera"/> component.
        /// </summary>
        private float _fov = -1.0f;

        /// <summary>
        /// Zoom amount to apply to the <see cref="camera"/> field of view.
        /// </summary>
        private float _zoom = 0.0f;

        /// <inheritdoc cref="position"/>
        private Vector3 _position = Vector3.zero;

        /// <inheritdoc cref="rotation"/>
        private Quaternion _rotation = Quaternion.identity;

        /// <inheritdoc cref="MinTrackingDistance"/>
        private float minTrackingDistance;

        /// <inheritdoc cref="TargetTrackingDistance"/>
        private float midTrackingDistance;

        /// <inheritdoc cref="MaxTrackingDistance"/>
        private float maxTrackingDistance;

        /// <summary>
        /// Rolling ID for <see cref="CameraController"/> instances.
        /// </summary>
        private static int rollingCameraId = 0;

        /// <summary>
        /// Reference to the original <see cref="Transform"/> that the <see cref="CameraController"/> was a child of.
        /// </summary>
        private Transform originalParent = null;

        #endregion

        #region property

        /// <summary>
        /// Target <see cref="Transform"/> component to track.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public Transform target {
#pragma warning restore IDE1006 // naming styles
            get => _target;
            set {
                if (value == _target || value == transform) return;
                _target = value;
                if (_target != null && (trackingFlags | CameraTrackingFlags.Parent) != 0) {
                    transform.parent = _target;
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                } else {
                    transform.parent = originalParent;
                }
            }
        }

        #region position
        /// <summary>
        /// Actual position of the <see cref="CameraController"/> excluding any camera effects such as camera shake.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public Vector3 position => _position;
#pragma warning restore IDE1006 // naming styles
        #endregion

        #region rotation
        /// <summary>
        /// Actual rotation of the <see cref="CameraController"/> excluding any camera effects such as camera shake.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public Quaternion rotation => _rotation;
#pragma warning restore IDE1006 // naming styles
        #endregion

        #region velocity
        /// <summary>
        /// Velocity of the <see cref="CameraController"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public Vector3 velocity { get; private set; } = Vector3.zero;
#pragma warning restore IDE1006 // naming styles
        #endregion

        #region camera
        /// <summary>
        /// <see cref="Camera"/> component attached to the <see cref="CameraController"/> object.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
#pragma warning disable IDE1006 // naming styles
        public Camera camera { get; private set; } = null;
#pragma warning restore IDE1006 // naming styles
        #endregion

        #region fov
#pragma warning disable IDE1006 // naming styles
        public float fov {
#pragma warning restore IDE1006 // naming styles
            get => _fov == -1.0f ? camera.fieldOfView : _fov;
            set {
                if (value == _fov) return;
                const float MinFov = 0.1f;
                if (value < MinFov) throw new ArgumentException($"fov cannot be less than {MinFov}");
                const float MaxFov = 179.9f;
                if (value > MaxFov) throw new ArgumentException($"fov cannot be greater than {MaxFov}");
                _fov = value;
                UpdateFov();
            }
        }
        #endregion

        #region zoom
        /// <summary>
        /// Zoom power to apply to the <see cref="camera"/> field of view.
        /// </summary>
        /// <remarks>
        /// <para><c>0.0</c> zoom means no zoom will be applied.</para>
        /// <para><c>1.0</c> zoom means the zoom will half the fov.</para>
        /// <para>Zoom is calculated by <c>fov *= 1.0f / (zoom + 1.0f)</c>.</para>
        /// </remarks>
#pragma warning disable IDE1006 // naming styles
        public float zoom {
#pragma warning restore IDE1006 // naming styles
            get => _zoom;
            set {
                if (value == _zoom) return;
                _zoom = Mathf.Clamp(value, 0.0f, 10.0f);
                UpdateFov();
            }
        }
        #endregion

        #region farClipPlane
        /// <summary>
        /// Value of the <see cref="camera"/> far clip plane.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float farClipPlane { get => camera.farClipPlane; set => camera.farClipPlane = value; }
#pragma warning restore IDE1006 // naming styles
        #endregion

        #region nearClipPlane
        /// <summary>
        /// Value of the <see cref="camera"/> near clip plane.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float nearClipPlane { get => camera.nearClipPlane; set => camera.nearClipPlane = value; }
#pragma warning restore IDE1006 // naming styles
        #endregion

        #region audioSource
        /// <summary>
        /// <see cref="AudioSource"/> component associated with the <see cref="CameraController"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public AudioSource audioSource { get; private set; } = null;
#pragma warning restore IDE1006 // naming styles
        #endregion

        #region TargetTrackingDistance
        /// <summary>
        /// Middle (target) distance that the <see cref="CameraController"/> can be from the <see cref="target"/>.
        /// </summary>
        public float TargetTrackingDistance {
            get => -midTrackingDistance;
            set {
                if (value < 0.0f) throw new ArgumentException($"{nameof(TargetTrackingDistance)} cannot be less than zero.");
                value = -value;
                if (value > minTrackingDistance) throw new ArgumentException($"{nameof(TargetTrackingDistance)} cannot be less than {nameof(MinTrackingDistance)}");
                if (value < maxTrackingDistance) throw new ArgumentException($"{nameof(TargetTrackingDistance)} cannot be more than {nameof(MaxTrackingDistance)}");
                midTrackingDistance = value;
            }
        }
        #endregion

        #region MinTrackingDistance
        /// <summary>
        /// Minimum distance that the <see cref="CameraController"/> can be from the <see cref="target"/>.
        /// </summary>
        public float MinTrackingDistance {
            get => -minTrackingDistance;
            set {
                if (value < 0.0f) throw new ArgumentException($"{nameof(MinTrackingDistance)} cannot be less than zero.");
                value = -value;
                if (value < midTrackingDistance) throw new ArgumentException($"{nameof(MinTrackingDistance)} cannot be more than {nameof(TargetTrackingDistance)}");
                if (value < maxTrackingDistance) throw new ArgumentException($"{nameof(MinTrackingDistance)} cannot be more than {nameof(MaxTrackingDistance)}");
                minTrackingDistance = value;
            }
        }
        #endregion

        #region MaxTrackingDistance
        /// <summary>
        /// Maximum distance that the <see cref="CameraController"/> can be from the <see cref="target"/>.
        /// </summary>
        public float MaxTrackingDistance {
            get => -maxTrackingDistance;
            set {
                if (value < 0.0f) throw new ArgumentException($"{nameof(MaxTrackingDistance)} cannot be less than zero.");
                value = -value;
                if (value > minTrackingDistance) throw new ArgumentException($"{nameof(MaxTrackingDistance)} cannot be less than {nameof(MinTrackingDistance)}");
                if (value > midTrackingDistance) throw new ArgumentException($"{nameof(MaxTrackingDistance)} cannot be less than {nameof(TargetTrackingDistance)}");
                maxTrackingDistance = value;
            }
        }
        #endregion

        #region PositionalTrackingSpeed
        /// <summary>
        /// Tracking speed (in meters per second) for position tracking of the <see cref="target"/>.
        /// </summary>
        public float PositionalTrackingSpeed {
            get => positionTrackingSpeed;
            set {
                if (value < 0.0f) throw new ArgumentException($"{nameof(PositionalTrackingSpeed)} cannot be less than zero.");
                positionTrackingSpeed = value;
            }
        }
        #endregion

        #region RotationalTrackingSpeed
        /// <summary>
        /// Tracking speed (in radians) for rotational tracking of the <see cref="target"/>.
        /// </summary>
        public float RotationalTrackingSpeed {
            get => rotationTrackingSpeed;
            set {
                if (value < 0.0f) throw new ArgumentException($"{nameof(RotationalTrackingSpeed)} cannot be less than zero.");
                rotationTrackingSpeed = value;
            }
        }
        #endregion

        #region RenderDistance
        /// <summary>
        /// Render distance (in meters).
        /// </summary>
        /// <remarks>
        /// Setting this will scale the <see cref="nearClipPlane"/> against the provided value and set the
        /// <see cref="farClipPlane"/> directly to the provided value.
        /// </remarks>
        public float RenderDistance {
            get => camera.farClipPlane;
            set {
                if (value < 0.1f || value > 25000.0f) throw new ArgumentOutOfRangeException(nameof(RenderDistance));
                camera.farClipPlane = value; // set the far clip plane as the render distance
                camera.nearClipPlane = value * value * 0.0000001f; // automatically calculate a near clip plane
            }
        }
        #endregion

        #region TrackingFlags
        /// <summary>
        /// Describes how the <see cref="CameraController"/> will track the <see cref="target"/>.
        /// </summary>
        public CameraTrackingFlags TrackingFlags {
            get => trackingFlags;
            set {
                trackingFlags = value;
                if (target != null && (trackingFlags | CameraTrackingFlags.Parent) != 0) {
                    transform.parent = target;
                } else {
                    transform.parent = originalParent;
                }
            }
        }
        #endregion

        #region MainCameraPosition

        /// <summary>
        /// Position in world-space of the main camera.
        /// </summary>
        public static Vector3 MainCameraPosition {
            get {
                if (current != null) return current._position;
                else {
                    Camera mainCamera = Camera.main;
                    if (mainCamera != null) return mainCamera.transform.position;
                    else return Vector3.zero;
                }
            }
        }

        #endregion

        #region IsCurrent
        /// <summary>
        /// Returns <c>true</c> if this <see cref="CameraController"/> is the <see cref="current"/> <see cref="CameraController"/>.
        /// </summary>
        public bool IsCurrent {
            get {
                if (current == null) {
                    current = this;
                    return true;
                }
                return this == current;
            }
        }
        #endregion

        #region current
        /// <summary>
        /// Current instance of <see cref="CameraController"/> in the scene.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public static CameraController current { get; private set; } = null;
#pragma warning restore IDE1006 // naming styles
        #endregion

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            originalParent = transform.parent;

            #region setup camera
            camera = this.ForceGetComponent<Camera>();
            camera.cameraType = CameraType.Game;
            camera.orthographic = false;
            _fov = camera.fieldOfView;
            #endregion

            #region setup audio source
            audioSource = this.ForceGetComponent<AudioSource>();
            audioSource.Stop();
            audioSource.volume = 0.0f;
            audioSource.pitch = 1.0f;
            audioSource.loop = true;
            if (audioSource.clip == null) {
                if (current != null) audioSource.clip = current.audioSource.clip;
            }
            #endregion

            #region setup game object
            gameObject.tag = CameraControllerTag;
#if UNITY_EDITOR
            if (gameObject.isStatic) {
                Debug.LogWarning($"A {nameof(CameraController)} instance cannot be marked as static.", this);
            }
#endif
            #endregion

            #region register camera controller
            if (CameraControllerBuffer.IsFull) CameraControllerBuffer.Expand(1);
            CameraControllerBuffer.AddLast(this);
            if (current == null) current = this;
            #endregion

        }

        #endregion

        #region OnDestroy

        private void OnDestroy() {
            if (CameraControllerBuffer.Remove(this) > 0) CameraControllerBuffer.TryShrink(1);
            if (current == this) current = CameraControllerBuffer.First;
        }

        #endregion

        #region OnEnable

        private void OnEnable() {
            _position = transform.position;
            _rotation = transform.rotation;
            velocity = Vector3.zero;
        }

        #endregion

        #region OnDisable

        private void OnDisable() {
            velocity = Vector3.zero;
        }

        #endregion

        #region LateUpdate

        private void LateUpdate() {
            float deltaTime = Time.deltaTime;
            if (target != null && (trackingFlags | CameraTrackingFlags.Parent) != 0) { // parented to target
                Vector3 localPosition = Vector3.zero;
                if (CameraShakeSource.HasSample()) { // a sample can be taken for camera shake
                    Vector3 shake = CameraShakeSource.Sample(transform.position); // sample camera shake
                    float sqrMagnitude = shake.sqrMagnitude; // calculate the square magnitude of the camera shake
                    if (sqrMagnitude > Mathf.Epsilon) { // a significant amount of shake exists
                        localPosition = shake; // add the shake to the final camera position
                        ControlManager.SetMotorRumble(Mathf.Sqrt(sqrMagnitude) * CameraShakeRumbleFrequency); // calculate the controller rumble
                    }
                }
                transform.localPosition = localPosition;
                _position = transform.position;
                _rotation = transform.rotation;
            } else { // parented to original target
                bool updateTransform = false; // track if the transform position/rotation needs to be updated
                if (target != null) { // there is a target to track
                    Vector3 lastPosition = _position;

                    // apply smoothing:
                    if ((trackingFlags & CameraTrackingFlags.Smooth) != 0) {
                        _position = Vector3.Lerp(
                            _position,
                            target.position + (target.forward * midTrackingDistance),
                            positionTrackingSpeed * deltaTime
                        );
                    } else {
                        _position = target.position + (target.forward * midTrackingDistance);
                    }

                    // apply clamping:
                    if ((trackingFlags & (CameraTrackingFlags.MinClamp | CameraTrackingFlags.MaxClamp)) != 0) { // positional clamping is enabled
                        Vector3 localPosition = _position - target.position; // calculate the local position of the camera relative to the target position
                        float sqrDistanceToTarget = localPosition.sqrMagnitude;
                        if ((trackingFlags & CameraTrackingFlags.MinClamp) != 0 && sqrDistanceToTarget < (minTrackingDistance * minTrackingDistance)) {
                            _position = target.position + (target.forward * minTrackingDistance);
                        } else if ((trackingFlags & CameraTrackingFlags.MaxClamp) != 0 && sqrDistanceToTarget > (maxTrackingDistance * maxTrackingDistance)) {
                            _position = target.position + (target.forward * maxTrackingDistance);
                        }
                    }

                    // calculate velocity:
                    velocity = (lastPosition - _position) * (1.0f / deltaTime);

                    // apply rotation:
                    if ((trackingFlags & CameraTrackingFlags.Smooth) != 0) {
                        _rotation = Quaternion.Lerp(
                            _rotation,
                            target.rotation,
                            rotationTrackingSpeed * deltaTime
                        );
                    } else {
                        _rotation = target.rotation;
                    }
                    updateTransform = true;
                } else {
                    velocity = Vector3.zero;
                }

                Vector3 finalPosition = _position;

                if (CameraShakeSource.HasSample()) { // a sample can be taken for camera shake
                    Vector3 shake = CameraShakeSource.Sample(transform.position); // sample camera shake
                    float sqrMagnitude = shake.sqrMagnitude; // calculate the square magnitude of the camera shake
                    if (sqrMagnitude > Mathf.Epsilon) { // a significant amount of shake exists
                        finalPosition += _rotation * shake; // add the shake to the final camera position
                        updateTransform = true; // the transform needs updating
                        ControlManager.SetMotorRumble(Mathf.Sqrt(sqrMagnitude) * CameraShakeRumbleFrequency); // calculate the controller rumble
                    }
                }

                if (updateTransform) {
                    transform.SetPositionAndRotation(
                        finalPosition,
                        _rotation
                    );
                }

            }

            #region wind (audio)
            if (audioSource.clip != null) { // there is an audio clip (for wind audio)
                Vector3 relativeWindVelocity = -velocity; // when still, air is acting against the velocity of the camera controller
                // TODO: add environment manager wind here
                // if (EnvironmentManager.current != null) relativeWindVelocity += EnvionmentManager.WindVelocity;
                float windVolume = SqrSpeedToWindVolume * relativeWindVelocity.sqrMagnitude; // calculate wind volume
                if (windVolume > 1.0f) windVolume = 1.0f; // apply max clamp
                float volume = audioSource.volume.Lerp(windVolume, WindVolumeLerpSpeed * deltaTime);
                if (volume > 0.0f) { // wind should be audable
                    audioSource.volume = volume; // set wind volume
                    audioSource.pitch = 0.8f + (volume * 0.4f); // min pitch = 0.8, max pitch = 1.2
                    if (!audioSource.isPlaying) audioSource.Play(); // play wind audio
                } else if (audioSource.isPlaying) { // no wind volume, stop the audio source
                    audioSource.Stop(); // stop playing wind audio
                }
            }
            #endregion
        }

        #endregion

        #region UpdateFov
        /// <summary>
        /// Updates the <see cref="camera"/> field of view based off of the values of <see cref="_fov"/> and <see cref="_zoom"/>.
        /// </summary>
        private void UpdateFov() => camera.fieldOfView = _fov * (1.0f / (_zoom + 1.0f));
        #endregion

        #region Create

        /// <summary>
        /// Creates a new <see cref="CameraController"/> instance.
        /// </summary>
        /// <param name="name">Name to give the new <see cref="CameraController"/> <see cref="GameObject"/>.</param>
        /// <returns>Returns a reference to the newly created <see cref="CameraController"/>.</returns>
        public static CameraController Create(in string name = DefaultCameraControllerName) {
            return new GameObject(
                name,
                typeof(Camera),
                typeof(AudioSource),
                typeof(CameraController)
            ) {
                tag = CameraControllerTag
            }.GetComponent<CameraController>();
        }

        #endregion

        #region IsVisible

        /// <summary>
        /// Tests if a <paramref name="renderer"/> is visible to this <see cref="CameraController"/> instance.
        /// </summary>
        /// <returns>Returns <c>true</c> if the <paramref name="renderer"/> is visible to this <see cref="CameraController"/> instance.</returns>
        public bool IsVisible(in Renderer renderer) {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
        }

        /// <summary>
        /// Tests if a <paramref name="collider"/> is visible to this <see cref="CameraController"/> instance.
        /// </summary>
        /// <returns>Returns <c>true</c> if the <paramref name="collider"/> is visible to this <see cref="CameraController"/> instance.</returns>
        public bool IsVisible(in Collider collider) {
            if (collider == null) throw new ArgumentNullException(nameof(collider));
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, collider.bounds);
        }

        #endregion

        #endregion

    }

}