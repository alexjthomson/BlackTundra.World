
using System;

using UnityEngine;

using Console = BlackTundra.Foundation.Console;

namespace BlackTundra.World.Player {

    /// <summary>
    /// Manages player physics, movement, and collisions.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(CharacterController))]
    public abstract class LocomotionBase : MonoBehaviour {

        #region variable

        /// <summary>
        /// <see cref="CapsuleCollider"/> used for player collisions.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
        private CapsuleCollider collider = null;

        /// <summary>
        /// <see cref="CharacterController"/> used for player movement and collisions with objects in the world.
        /// </summary>
        private CharacterController character = null;

        /// <summary>
        /// Movement velocity.
        /// </summary>
        private Vector3 motiveVelocity = Vector3.zero;

        /// <summary>
        /// Velocity acted upon by physics.
        /// </summary>
        private Vector3 physicsVelocity = Vector3.zero;

        /// <inheritdoc cref="IsGrounded"/>
        private bool grounded = false;

        /// <summary>
        /// When <c>true</c>, the yaw rotation is taken into account when applying movement velocity.
        /// </summary>
        public bool applyRotation = true;

        #region _mass

        /// <inheritdoc cref="mass"/>
        [SerializeField]
#if UNITY_EDITOR
        internal
#else
        private
#endif
        float _mass = 80.0f;

        #endregion

        #region _gravity

        /// <inheritdoc cref="gravity"/>
        [SerializeField]
#if UNITY_EDITOR
        internal
#else
        private
#endif
        float _gravity = -9.81f;

        #endregion

        #region _drag

        /// <inheritdoc cref="drag"/>
        [SerializeField]
#if UNITY_EDITOR
        internal
#else
        private
#endif
        float _drag = 0.005f;

        #endregion

        #region groundedVelocity

        /// <inheritdoc cref="GroundedStickVelocity"/>
        [SerializeField]
#if UNITY_EDITOR
        internal
#else
        private
#endif
        Vector3 groundedVelocity = new Vector3(0.0f, -1.0f, 0.0f);

        #endregion

        #region _layermask

        /// <inheritdoc cref="layerMask"/>
        [SerializeField]
#if UNITY_EDITOR
        internal
#else
        private
#endif
        LayerMask _layerMask = -1;

        #endregion

        #region frictionCoefficient

        /// <inheritdoc cref="FrictionCoefficient"/>
        [SerializeField]
#if UNITY_EDITOR
        internal
#else
        private
#endif
        float frictionCoefficient = 0.3f;

        #endregion

        #region slideCoefficient

        /// <inheritdoc cref="SlideCoefficient"/>
        [SerializeField]
#if UNITY_EDITOR
        internal
#else
        private
#endif
        float slideCoefficient = 20.0f;

        #endregion

        #endregion

        #region property

        /// <summary>
        /// Height of the <see cref="LocomotionBase"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float height {
#pragma warning restore IDE1006 // naming styles
            get => collider.height;
            set {
                float skinWidth = character.skinWidth;
                float height = Mathf.Max(value, collider.radius * 2.0f, skinWidth * 2.0f);
                collider.height = height;
                character.height = height - (2.0f * skinWidth);
                Vector3 center = new Vector3(0.0f, height * 0.5f, 0.0f);
                collider.center = center;
                character.center = center;
            }
        }

        /// <summary>
        /// Radius of the <see cref="LocomotionBase"/>.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float radius {
#pragma warning restore IDE1006 // naming styles
            get => collider.radius;
            set {
                float skinWidth = character.skinWidth;
                float radius = Mathf.Max(value, skinWidth);
                collider.radius = radius;
                character.radius = radius - skinWidth;
            }
        }

        /// <summary>
        /// Width of the skin for the <see cref="LocomotionBase"/>. The thicker the skin is, the better quality
        /// collisions are.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float skinWidth {
#pragma warning restore IDE1006 // naming styles
            get => character.skinWidth;
            set {
                float skinWidth = Mathf.Max(value, 0.0f);
                character.skinWidth = skinWidth;
                character.height = collider.height - (skinWidth * 2.0f);
                character.radius = collider.radius - skinWidth;
            }
        }

        /// <summary>
        /// <see cref="LayerMask"/> used for physics collisions etc.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public LayerMask layerMask {
#pragma warning restore IDE1006 // naming styles
            get => _layerMask;
            set => _layerMask = value;
        }

        /// <summary>
        /// Mass of the <see cref="LocomotionBase"/> in kg.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float mass {
#pragma warning restore IDE1006 // naming styles
            get => _mass;
            set => _mass = Mathf.Max(value, 0.1f);
        }

        /// <summary>
        /// Acceleration applied to the global y axis due to gravity.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float gravity {
#pragma warning restore IDE1006 // naming styles
            get => _gravity;
            set => _gravity = value;
        }

        /// <summary>
        /// Drag coefficient applied to physical velocities acting upon this <see cref="LocomotionBase"/>. This can be
        /// thought as the air-resistance coefficient.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float drag {
#pragma warning restore IDE1006 // naming styles
            get => _drag;
            set => _drag = Mathf.Max(value, 0.0f);
        }

        /// <summary>
        /// Coefficient of friction to use.
        /// </summary>
        public float FrictionCoefficient {
            get => frictionCoefficient;
            set => frictionCoefficient = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Coefficient applied to slide velocity.
        /// </summary>
        public float SlideCoefficient {
            get => slideCoefficient;
            set => slideCoefficient = Mathf.Max(value, 0.0f);
        }

        /// <summary>
        /// Static acceleration downwards when the player is stood on the ground. This helps the player stick to surfaces.
        /// </summary>
        public float GroundedStickVelocity {
            get => -groundedVelocity.y;
            set => groundedVelocity.y = Mathf.Min(-value, 0.0f);
        }

        /// <summary>
        /// <c>true</c> while the <see cref="LocomotionBase"/> is grounded.
        /// </summary>
        public bool IsGrounded => grounded;

        #endregion

        #region event

        /// <summary>
        /// Event invoked when the <see cref="LocomotionBase"/> impacts the ground. The first event argument is the
        /// velocity that the <see cref="LocomotionBase"/> impacted the ground with.
        /// </summary>
        public event Action<Vector3> OnImpactGround;

        /// <summary>
        /// Event invoked when the <see cref="LocomotionBase"/> leaves the ground.
        /// </summary>
        public event Action OnLeaveGround;

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            collider = GetComponent<CapsuleCollider>();
            Console.AssertReference(collider);
            character = GetComponent<CharacterController>();
            Console.AssertReference(character);
        }

        #endregion

        #region OnDestroy

        private void OnDestroy() {
#if UNITY_EDITOR
            collider.hideFlags = HideFlags.None;
            character.hideFlags = HideFlags.None;
#endif
        }

        #endregion

        #region OnEnable

        private void OnEnable() {
            collider.enabled = true;
            character.enabled = true;
            grounded = false;
        }

        #endregion

        #region OnDisable

        private void OnDisable() {
            collider.enabled = false;
            character.enabled = false;
            grounded = false;
        }

        #endregion

        #region Update

        private void Update() {

        }

        #endregion

        #region FixedUpdate

        private void FixedUpdate() {
            float deltaTime = Time.fixedDeltaTime;

            #region update ground state
            bool groundedState = character.isGrounded;
            if (groundedState != grounded) {
                grounded = groundedState;
                if (grounded) {
                    OnImpactGround?.Invoke(character.velocity);
                } else {
                    OnLeaveGround?.Invoke();
                }
            }
            #endregion

            Vector3 totalVelocity = applyRotation ? transform.rotation * motiveVelocity : motiveVelocity; // total velocity calculated this update
            if (grounded) {
                totalVelocity += groundedVelocity; // if grounded, apply the grounded velocity to the total velocity
                float groundFriction = frictionCoefficient * -gravity * deltaTime;
                physicsVelocity -= new Vector3(
                    physicsVelocity.x * groundFriction,
                    0.0f,
                    physicsVelocity.z * groundFriction
                );
                if (Physics.Raycast(transform.position + collider.center, Vector3.down, out RaycastHit hit, (collider.height * 0.5f) + character.stepOffset, _layerMask, QueryTriggerInteraction.UseGlobal)) { // cast grounded ray
                    Vector3 normal = hit.normal;
                    float slopeAngle = Vector3.Angle(normal, Vector3.up); // calculate the angle of the slope
                    float slopeLimit = character.slopeLimit; // get the slope limit
                    float slideAmount = slopeAngle / slopeLimit; // scale the slide amount with the slope limit
                    if (slideAmount > frictionCoefficient) { // prevent sliding unless more than the friction coefficient
                        slideAmount = (slideAmount - frictionCoefficient) * slideCoefficient * -gravity * deltaTime; // recalculate slide amount
                        physicsVelocity += new Vector3(
                            normal.x * slideAmount,
                            -physicsVelocity.y,
                            normal.z * slideAmount
                        );
                    }
                }
            } else { // not grounded
                float drag = -_drag; // calculate drag coeffcient
                physicsVelocity += new Vector3( // apply physics velocity changes
                    physicsVelocity.x * drag,
                    physicsVelocity.y * drag + (gravity * deltaTime),
                    physicsVelocity.z * drag
                );
            }
            totalVelocity += physicsVelocity; // apply physics velocity to the total velocity
            character.Move(totalVelocity * deltaTime); // move the character
        }

        #endregion

        #region SetMotiveVelocity

        public void SetMotiveVelocity(in Vector3 velocity) {
            motiveVelocity = velocity;
        }

        #endregion

        #region AddForce

        /// <summary>
        /// Adds a force to the <see cref="LocomotionBase"/>.
        /// The force is applied instantly and takes into account mass.
        /// </summary>
        public void AddForce(in Vector3 force) {
            physicsVelocity += force / mass;
        }

        #endregion

        #region AddVelocity

        /// <summary>
        /// Adds velocity to the <see cref="LocomotionBase"/>.
        /// This does not take into account mass and the velocity is applied instantly.
        /// </summary>
        public void AddVelocity(in Vector3 velocity) {
            physicsVelocity += velocity;
        }

        #endregion

        #endregion

    }

}