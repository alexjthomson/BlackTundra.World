using BlackTundra.Foundation.Utility;

using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Events;

namespace BlackTundra.World {

    /// <summary>
    /// Acts as a wrapper that adds a physics layer ontop of the existing <see cref="CharacterController"/>. This class controls a hidden
    /// <see cref="CharacterController"/> component.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
#if UNITY_EDITOR
    [AddComponentMenu("Physics/Character Controller (Physics-Based)")]
#endif
    public class PhysicsCharacterController : MonoBehaviour, IPhysicsObject {

        #region constant

        /// <summary>
        /// Minimum value that the <see cref="radius"/> can have.
        /// </summary>
        public const float MinimumRadius = 0.05f;

        /// <summary>
        /// Percentage between 0% and 100% that describes how much of the total radius is made up by the actual radius. The remaining amount is
        /// equal to the width of the skin.
        /// </summary>
        private const float RadiusToSkinRatio = 0.85f;

        /// <summary>
        /// Velocity that the <see cref="PhysicsCharacterController"/> is forced into the ground with when on the ground. This velocity helps to
        /// stick the <see cref="PhysicsCharacterController"/> to the ground. In order to leave the ground while on the ground, the
        /// <see cref="PhysicsCharacterController"/> must be travelling down a slope at a velocity greater than this velocity. If this is achieved
        /// then a vap will appear between the bottom of the <see cref="PhysicsCharacterController"/> and the ground.
        /// </summary>
        private static readonly Vector3 GroundStickVelocity = new Vector3(0.0f, -10.0f, 0.0f);

        #endregion

        #region variable

        /// <summary>
        /// <see cref="CharacterController"/> component attached to the same <see cref="GameObject"/> as the <see cref="PhysicsCharacterController"/>.
        /// </summary>
#if UNITY_EDITOR
        internal
#else
        private
#endif
        CharacterController _characterController = null;

        /// <summary>
        /// <see cref="PhysicsCharacterControllerFlags"/> that toggle features of the <see cref="PhysicsCharacterController"/> simulation.
        /// </summary>
#if UNITY_EDITOR
        [HideInInspector]
#endif
        [SerializeField]
        private PhysicsCharacterControllerFlags _flags = 0;

        /// <summary>
        /// Mass of the <see cref="PhysicsCharacterController"/>.
        /// </summary>
#if UNITY_EDITOR
        [HideInInspector]
#endif
        [SerializeField]
        private float _mass = 80.0f;

        /// <summary>
        /// Inverse of the <see cref="_mass"/>.
        /// </summary>
        private float _inverseMass = 1.0f / 80.0f;

        /// <summary>
        /// Centre of mass of the <see cref="PhysicsCharacterController"/>.
        /// </summary>
#if UNITY_EDITOR
        [HideInInspector]
#endif
        [SerializeField]
        private Vector3 _centreOfMass = Vector3.zero;

        /// <summary>
        /// <see cref="LayerMask"/> containing the layers that are considered to be solid.
        /// </summary>
#if UNITY_EDITOR
        [HideInInspector]
#endif
        [SerializeField]
        private LayerMask _solidMask = 0;

        /// <summary>
        /// Actual radius of the <see cref="_characterController"/>. This is calculated by adding together the <see cref="_radius"/> and <see cref="_skinWidth"/>.
        /// </summary>
#if UNITY_EDITOR
        [HideInInspector]
#endif
        [SerializeField]
        private float _actualRadius = 0.15f;

        /// <summary>
        /// Radius of the <see cref="_characterController"/>.
        /// </summary>
        /// <seealso cref="_characterController"/>
        private float _radius = 0.1f;

        /// <summary>
        /// Skin width/radius of the <see cref="_characterController"/>.
        /// </summary>
        /// <seealso cref="_characterController"/>
        private float _skinWidth = 0.05f;

        /// <summary>
        /// Actual height of the <see cref="_characterController"/>. This is measured between the base of the <see cref="_characterController"/> and the top
        /// of the <see cref="_characterController"/>.
        /// </summary>
        /// <seealso cref="_height"/>
        /// <seealso cref="_characterController"/>
#if UNITY_EDITOR
        [HideInInspector]
#endif
        [SerializeField]
        private float _actualHeight = 1.8f;

        /// <summary>
        /// Height of the <see cref="_characterController"/> including <c>(2 * <see cref="_actualRadius"/>)</c>.
        /// </summary>
        /// <seealso cref="_actualHeight"/>
        /// <seealso cref="_actualRadius"/>
        /// <seealso cref="_characterController"/>
        private float _height = 1.8f;

        /// <summary>
        /// Centre of the <see cref="_characterController"/>. This is calculated by halfing the y component of the <see cref="_actualHeight"/>.
        /// </summary>
        private Vector3 _centre = Vector3.zero;

        /// <summary>
        /// World-space final velocity of the <see cref="PhysicsCharacterController"/>. This is updated when the exact final velocity of the controller is known
        /// at the end of each <see cref="FixedUpdate"/>. This is the value returned by <see cref="GetVelocity"/>.
        /// </summary>
        private Vector3 _finalVelocity = Vector3.zero;

        /// <summary>
        /// World-space velocity of the <see cref="PhysicsCharacterController"/>.
        /// </summary>
        private Vector3 _velocity = Vector3.zero;

        /// <summary>
        /// Drag coefficient to use for the <see cref="PhysicsCharacterController"/>.
        /// </summary>
#if UNITY_EDITOR
        [HideInInspector]
#endif
        [SerializeField]
        private float _dragCoefficient = 1.2f;

        /// <summary>
        /// An amount of velocity waiting (pending) to be applied to the <see cref="_velocity"/>. Before this velocity can be added to the <see cref="_velocity"/>
        /// it must be multiplied by <see cref="Time.fixedDeltaTime"/>. This is done in the <see cref="FixedUpdate"/>. After the velocity has been added to the
        /// <see cref="_velocity"/>, it is set to <see cref="Vector3.zero"/>.
        /// </summary>
        private Vector3 _pendingTimeDependantVelocity = Vector3.zero;

        /// <summary>
        /// An amount of velocity waiting (pending) to be applied but not directly to the <see cref="_velocity"/>. This velocity is applied seperately and is used
        /// to artificially apply velocity such as velocity forcing the <see cref="PhysicsCharacterController"/> into the floor when grounded so that there is an
        /// instant sticking force to the floor. This velocity is entirely internally controlled on a <see cref="FixedUpdate"/> basis. This velocity is not
        /// cleared at the end of each <see cref="FixedUpdate"/> and is persistant.
        /// </summary>
        private Vector3 _pendingInstantVelocity = Vector3.zero;

        /// <summary>
        /// World-space translation vector constructed from calling <see cref="Move(in Vector3)"/>. This vector will be used to calculate the target amount that
        /// the controller should move. Movement is applied every time <see cref="FixedUpdate"/> is called; not when <see cref="Move(in Vector3)"/> is called.
        /// </summary>
        /// <seealso cref="Move(in Vector3)"/>
        private Vector3 _pendingMovementVector = Vector3.zero;

        /// <summary>
        /// Caches if the <see cref="PhysicsCharacterController"/> was grounded on the last <see cref="FixedUpdate"/>.
        /// </summary>
        private bool _isGrounded = false;

        /// <summary>
        /// <see cref="PhysicMaterial"/> for the material that the <see cref="PhysicsCharacterController"/> is contacting while <see cref="_isGrounded"/> is
        /// <c>true</c>. If <see cref="_isGrounded"/> is <c>false</c>, this will be <c>null</c>.
        /// </summary>
        private PhysicMaterial _groundPhysicMaterial = null;

        /// <summary>
        /// Invoked when the <see cref="PhysicsCharacterController"/> impacts the ground. The velocity that the
        /// <see cref="PhysicsCharacterController"/> impacted the ground with is provided.
        /// </summary>
        /// <remarks>
        /// This is invoked after <see cref="OnGroundedChanged"/>.
        /// </remarks>
        public UnityEvent<Vector3> OnImpactGround = null;

        /// <summary>
        /// Invoked when the <see cref="PhysicsCharacterController"/> leaves the ground.
        /// </summary>
        /// <remarks>
        /// This is invoked after <see cref="OnGroundedChanged"/>.
        /// </remarks>
        public UnityEvent OnLeaveGround = null;

        /// <summary>
        /// Invoked when the <see cref="PhysicsCharacterController"/> <see cref="isGrounded"/> state changes.
        /// </summary>
        /// <remarks>
        /// This is invoked before <see cref="OnImpactGround"/> and <see cref="OnLeaveGround"/>.
        /// </remarks>
        public UnityEvent<bool> OnGroundedChanged = null;

        #endregion

        #region property

        /// <summary>
        /// Velocity of the <see cref="PhysicsCharacterController"/> in world-space.
        /// </summary>
        /// <seealso cref="GetVelocity"/>
        /// <seealso cref="SetVelocity(in Vector3)"/>
        public Vector3 velocity {
            get => GetVelocity();
            set => SetVelocity(value);
        }

        /// <summary>
        /// World-space position of the <see cref="PhysicsCharacterController"/>.
        /// </summary>
        /// <seealso cref="GetPosition"/>
        /// <seealso cref="SetPosition(in Vector3)"/>
        public Vector3 position {
            get => GetPosition();
            set => SetPosition(value);
        }

        /// <summary>
        /// World-space rotation of the <see cref="PhysicsCharacterController"/>.
        /// </summary>
        /// <seealso cref="GetRotation"/>
        /// <seealso cref="SetRotation(in Quaternion)"/>
        public Quaternion rotation {
            get => GetRotation();
            set => SetRotation(value);
        }

        /// <summary>
        /// Centre of the <see cref="PhysicsCharacterController"/>.
        /// </summary>
        public Vector3 centre => _centre;

        /// <summary>
        /// Height of the <see cref="PhysicsCharacterController"/>. This is measured between the base and top of the controller.
        /// </summary>
        public float height {
            get => _actualHeight;
            set {
                float actualRadius2 = _actualRadius + _actualRadius;
                _actualHeight = Mathf.Max(value, actualRadius2);
                _height = _actualHeight - actualRadius2;
                _centre = new Vector3(_centre.x, _height * 0.5f, _centre.z);
                _characterController.height = _height;
                _characterController.center = _centre;
            }
        }

        /// <summary>
        /// Radius of the <see cref="PhysicsCharacterController"/>. This is measured from the centre of the controller to the outside
        /// edge of the controller.
        /// </summary>
        public float radius {
            get => _actualRadius;
            set {
                _actualRadius = Mathf.Max(value, MinimumRadius);
                _radius = _actualRadius * RadiusToSkinRatio;
                _skinWidth = _actualRadius - _radius;
                _actualHeight = _height + _actualRadius + _actualRadius;
                _characterController.radius = _radius;
                _characterController.skinWidth = _skinWidth;
            }
        }

        /// <summary>
        /// <see cref="PhysicsCharacterControllerFlags"/> that toggle features of the <see cref="PhysicsCharacterController"/> simulation.
        /// </summary>
        public PhysicsCharacterControllerFlags flags {
            get => _flags;
            set => _flags = value;
        }

        /// <summary>
        /// Mass of the <see cref="PhysicsCharacterController"/> in kg.
        /// </summary>
        /// <seealso cref="GetMass"/>
        /// <seealso cref="SetMass(in float)"/>
        public float mass {
            get => GetMass();
            set => SetMass(value);
        }

        /// <summary>
        /// Reciprocal of the <see cref="mass"/>.
        /// </summary>
        /// <seealso cref="mass"/>
        public float inverseMass => _inverseMass;

        /// <summary>
        /// Centre of mass of the <see cref="PhysicsCharacterController"/>.
        /// </summary>
        /// <seealso cref="GetCentreOfMass"/>
        /// <seealso cref="SetCentreOfMass(in Vector3)"/>
        public Vector3 centreOfMass {
            get => GetCentreOfMass();
            set => SetCentreOfMass(value);
        }

        /// <summary>
        /// Drag coefficient to use for the <see cref="PhysicsCharacterController"/>.
        /// </summary>
        public float dragCoefficient {
            get => _dragCoefficient;
            set {
                if (value < 0.0f || !float.IsNormal(value)) throw new ArgumentException("Drag coefficient must be greater than zero.");
                _dragCoefficient = value;
            }
        }

        /// <summary>
        /// <see cref="LayerMask"/> containing layers that contain solid objects.
        /// </summary>
        public LayerMask solidLayerMask {
            get => _solidMask;
            set => _solidMask = value;
        }

        /// <summary>
        /// <c>true</c> if the <see cref="PhysicsCharacterController"/> is touching the ground.
        /// </summary>
        /// <seealso cref="groundMaterialDescriptor"/>
        public bool isGrounded => _isGrounded;

        /// <summary>
        /// <see cref="PhysicMaterial"/> associated with the surface that the <see cref="PhysicsCharacterController"/> is stood on. This will only ever be assigned
        /// while <see cref="isGrounded"/> is <c>true</c>; otherwise, it will be <c>null</c>.
        /// </summary>
        /// <seealso cref="isGrounded"/>
        public PhysicMaterial groundMaterialDescriptor => _groundPhysicMaterial;

        #endregion

        #region logic

        #region OnEnable

#if UNITY_EDITOR
        internal
#endif
        protected virtual void OnEnable() {
            // setup character controller:
            _characterController = gameObject.ForceGetComponent<CharacterController>();
#if UNITY_EDITOR
            _characterController.hideFlags = HideFlags.HideInInspector;
#endif
            ResetController();
            // align to transform:
            SetPosition(transform.position);
            SetRotation(transform.rotation);
        }

        #endregion

        #region OnDestroy

        protected virtual void OnDestroy() {
            // remove character controller:
            _characterController = GetComponent<CharacterController>();
            if (_characterController != null) {
                Destroy(_characterController);
            }
        }

        #endregion

        #region FixedUpdate

        protected virtual void FixedUpdate() {
            // get timing information:
            float deltaTime = Time.fixedDeltaTime;
            if (deltaTime < Mathf.Epsilon) return; // no time has passed, do not perform an update
            float inverseDeltaTime = 1.0f / deltaTime;
            // update simulation:
            UpdateGroundedState();
            ApplyTimeBasedVelocity(deltaTime);
            ApplyGravityForce(deltaTime);
            ApplyEnvironmentalForces(deltaTime);
            ApplyGroundForces(deltaTime);
            UpdateCharacterController(deltaTime, inverseDeltaTime);
        }

        #endregion

        #region UpdateGroundedState

        /// <summary>
        /// Updates the <see cref="_isGrounded"/> state and invokes any related callbacks if changes in the state are detected.
        /// </summary>
        protected virtual void UpdateGroundedState() {
            // get current grounded state:
            bool currentlyGrounded = _characterController.isGrounded;
            // compare to last grounded state:
            if (currentlyGrounded != _isGrounded) { // grounded state has changed
                // update the grounded state:
                _isGrounded = currentlyGrounded;
                if (OnGroundedChanged != null) {
                    OnGroundedChanged.Invoke(_isGrounded);
                }
                // check if the controller impacted the ground or left the ground:
                if (_isGrounded) { // the controller impacted the ground
                    // apply instant stick force:
                    if ((_flags & (PhysicsCharacterControllerFlags.SimulateGravity | PhysicsCharacterControllerFlags.UseStickForce)) != 0) { // the controller is using gravity and stick force
                        _pendingInstantVelocity = GroundStickVelocity; // try to stick to the ground
                    } else { // no stick force should be applied
                        _pendingInstantVelocity = Vector3.zero;
                    }
                    // invoke impact ground callback:
                    if (OnImpactGround != null) {
                        OnImpactGround.Invoke(GetVelocity());
                    }
                } else { // the controller left the ground
                    // clear the ground material that the controller was impacting:
                    ClearGroundPhysicMaterial();
                    // apply no sticking force:
                    _pendingInstantVelocity = Vector3.zero; // apply no stick velocity (reset the instant velocity)
                    // invoke leave ground callback:
                    if (OnLeaveGround != null) {
                        OnLeaveGround.Invoke();
                    }
                }
            }
            // check if grounded:
            if (_isGrounded) {
                // find the ground physic material for the current position that the controller is in:
                FindGroundPhysicMaterial();
            }
        }

        #endregion

        #region ApplyTimeBasedVelocity

        /// <summary>
        /// Applys the <see cref="_pendingTimeDependantVelocity"/> to the <see cref="_velocity"/> vector and clears the <see cref="_pendingTimeDependantVelocity"/>.
        /// </summary>
        protected virtual void ApplyTimeBasedVelocity(in float deltaTime) {
            _velocity += _pendingTimeDependantVelocity * deltaTime; // apply the time-dependant velocity to the main velocity vector
            _pendingTimeDependantVelocity = Vector3.zero; // clear the pending time-dependant velocity
        }

        #endregion

        #region ApplyGravityForce

        /// <summary>
        /// Applies the gravitational force to the <see cref="PhysicsCharacterController"/>.
        /// </summary>
        protected virtual void ApplyGravityForce(in float deltaTime) {
            if (!_isGrounded || (_flags & PhysicsCharacterControllerFlags.SimulateGravity) == 0) return;
            _velocity.y -= Environment.gravity * deltaTime;
        }

        #endregion

        #region ApplyEnvironmentalForces

        /// <summary>
        /// Applies environmental drag forces to the <see cref="PhysicsCharacterController"/>.
        /// </summary>
        protected virtual void ApplyEnvironmentalForces(in float deltaTime) {
            if ((_flags & PhysicsCharacterControllerFlags.SimulateDrag | PhysicsCharacterControllerFlags.SimulateWind) == 0) return;
            // calculate relative velocity (controller velocity combined with wind velocity/force):
            Vector3 relativeVelocity;
            if ((_flags & PhysicsCharacterControllerFlags.SimulateWind) != 0) {
                relativeVelocity = -Environment.WindForce * _inverseMass;
                if ((_flags & PhysicsCharacterControllerFlags.SimulateDrag) != 0) {
                    relativeVelocity += _velocity;
                }
            } else {
                relativeVelocity = _velocity;
            }
            // get the position of the controller:
            Vector3 position = GetPosition();
            // find rho at the base of the controller:
            float rhoLower = Environment.RhoAt(position);
            // find rho at the top of the controller:
            float rhoUpper = Environment.RhoAt(new Vector3(position.x, position.y + _actualHeight, position.z));
            // average the two values for rho to a final value:
            float rho = (rhoUpper + rhoLower) * 0.5f;
            // calculate the vertical (top-down) (y) area of the controller:
            float verticalArea = _actualRadius * _actualRadius * Mathf.PI;
            // calculate the horizontal (front) (xz) area of the controller (same from any angle about the y-axis for a capsule):
            float horizontalArea = verticalArea + (_actualRadius * Mathf.Max(_actualHeight - _actualRadius - _actualRadius, 0.0f));
            // combine drag coefficient with rho and delta time:
            float combinedDragCoefficient = _dragCoefficient * -0.5f * rho * deltaTime;
            // calculate the vertical (y) drag coefficient:
            float verticalDragCoefficient = combinedDragCoefficient * verticalArea;
            // calculate the horizontal (xz) drag coefficient:
            float horizontalDragCoefficient = combinedDragCoefficient * horizontalArea;
            // apply drag:
            _velocity += new Vector3(
                Mathf.Sign(relativeVelocity.x) * relativeVelocity.x * relativeVelocity.x * horizontalDragCoefficient,
                Mathf.Sign(relativeVelocity.y) * relativeVelocity.y * relativeVelocity.y * verticalDragCoefficient,
                Mathf.Sign(relativeVelocity.z) * relativeVelocity.z * relativeVelocity.z * horizontalDragCoefficient
            );
        }

        #endregion

        #region ApplyGroundForces

        /// <summary>
        /// Applies forces created by the ground to the <see cref="PhysicsCharacterController"/>.
        /// </summary>
        protected virtual void ApplyGroundForces(in float deltaTime) {
            if (_isGrounded) { // the controller is grounded
                // remove any pending instant velocity if an overall upwards velocity is desired:
                if (_velocity.y > 0.0f && _pendingInstantVelocity.y < 0.0f) {
                    _pendingInstantVelocity.y = 0.0f;
                }
                // calculate the friction force from the ground:
                if (_groundPhysicMaterial != null) {
                    float frictionCoefficient = 1.0f - Mathf.Clamp01(_groundPhysicMaterial.dynamicFriction * deltaTime);
                    _velocity *= frictionCoefficient;
                }
                // clear negative y velocity:
                if (_velocity.y < 0.0f) { // the velocity has a negative y component
                    _velocity.y = 0.0f; // clear the y component, negative y velocity is not allowed / required while grounded
                }
            }
        }

        #endregion

        #region UpdateCharacterController

        /// <summary>
        /// Updates the <see cref="_characterController"/> component based on the physics forces applied.
        /// </summary>
        protected virtual void UpdateCharacterController(in float deltaTime, in float inverseDeltaTime) {
            // find the original position (before any translations are applied):
            Vector3 originalPosition = GetPosition(), finalPosition;
            // update the character controller height:
            UpdateCharacterControllerHeight(originalPosition, out finalPosition, deltaTime);
            // apply the pending movement vector:
            ApplyPendingMovement(finalPosition, out finalPosition, deltaTime);
            // apply the calulated velocity vector:
            ApplyPendingVelocity(finalPosition, out finalPosition, deltaTime, inverseDeltaTime);
            // calculate the final velocity:
            _finalVelocity = (finalPosition - originalPosition) * inverseDeltaTime;
        }

        #endregion

        #region UpdateCharacterControllerHeight

        /// <summary>
        /// Invoked during the <see cref="FixedUpdate"/> loop. This method should configure the current height of the controller.
        /// </summary>
        /// <param name="originalPosition">
        /// Initial position of the <see cref="PhysicsCharacterController"/> just before <see cref="UpdateCharacterControllerHeight(in Vector3, out Vector3, in float)"/>
        /// was invoked.
        /// </param>
        /// <param name="finalPosition">
        /// Final position expected to be reported by the <see cref="UpdateCharacterControllerHeight(in Vector3, out Vector3, in float)"/> method after the method has
        /// completed any transformations to the <see cref="PhysicsCharacterController"/>.
        /// </param>
        /// <param name="deltaTime"><see cref="Time.fixedDeltaTime"/></param>
        protected virtual void UpdateCharacterControllerHeight(in Vector3 originalPosition, out Vector3 finalPosition, in float deltaTime) {
            // this method should be overridden to apply a custom height to the controller
            finalPosition = originalPosition;
        }

        #endregion

        #region ApplyPendingMovement

        /// <summary>
        /// Applies the <see cref="_pendingMovementVector"/> translation to the <see cref="_characterController"/> and clears the <see cref="_pendingMovementVector"/>.
        /// </summary>
        protected virtual void ApplyPendingMovement(in Vector3 originalPosition, out Vector3 finalPosition, in float deltaTime) {
            // apply pending movement vector:
            _characterController.Move(_pendingMovementVector); // apply pending movement vector
            _pendingMovementVector = Vector3.zero; // reset pending movement vector
            // find the new position:
            finalPosition = GetPosition();
            // calculate the actual movement vector:
            Vector3 deltaPosition = new Vector3(
                finalPosition.x - originalPosition.x,
                0.0f, // do not calculate the delta position y component, it is not used
                finalPosition.z - originalPosition.z
            );
            // undo the movement by translating backwards by the actual translation vector that was applied:
            transform.position -= deltaPosition;
            // safely recalculate the centre of the character controller using the difference between the translation vector:
            _centre += transform.InverseTransformDirection(deltaPosition);
            _characterController.center = _centre; // update the centre of the character controller
            // recalculate the final position:
            finalPosition = GetPosition();
        }

        #endregion

        #region ApplyPendingVelocity

        protected virtual void ApplyPendingVelocity(in Vector3 originalPosition, out Vector3 finalPosition, in float deltaTime, in float inverseDeltaTime) {
            // calculate the movement vector to move the controller by:
            Vector3 movementVector = (_velocity + _pendingInstantVelocity) * deltaTime;
            // move the controller by the movement vector:
            _characterController.Move(movementVector);
            // update the final position:
            finalPosition = GetPosition();
            // calculate the actual movement vector:
            Vector3 actualMovementVector = finalPosition - originalPosition;
            // calculate the actual velocity moved:
            Vector3 actualVelocity = actualMovementVector * inverseDeltaTime;
            // extract the actual value of the physics velocity from the actual velocity:
            Vector3 newVelocity = actualVelocity - (_velocity - _pendingInstantVelocity);
            // transfer negative y component (if any) since it is used by the stick force:
            if (_velocity.y < Mathf.Epsilon) newVelocity.y = _velocity.y;
            // update the controller velocity:
            _velocity = newVelocity;
        }

        #endregion

        #region OnDrawGizmos
#if UNITY_EDITOR

        protected virtual void OnDrawGizmosSelected() {
            Vector3 com = transform.TransformPoint(_centreOfMass);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                new Vector3(com.x - 0.05f, com.y, com.z),
                new Vector3(com.x + 0.05f, com.y, com.z)
            );
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                new Vector3(com.x, com.y - 0.05f, com.z),
                new Vector3(com.x, com.y + 0.05f, com.z)
            );
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                new Vector3(com.x, com.y, com.z - 0.05f),
                new Vector3(com.x, com.y, com.z + 0.05f)
            );
            Handles.color = Color.yellow;
            Handles.Label(com, _mass.ToString("0.00kg"));
        }

#endif
        #endregion

        #region Move

        /// <summary>
        /// Tells the <see cref="PhysicsCharacterController"/> to move by the <paramref name="translation"/> when <see cref="FixedUpdate"/> is called next.
        /// </summary>
        /// <param name="translation">World-space movement vector to add to the <see cref="PhysicsCharacterController"/> movement vector.</param>
        public virtual void Move(in Vector3 translation) {
            if (float.IsNaN(translation.x) || float.IsNaN(translation.y) || float.IsNaN(translation.z)) {
                throw new ArgumentException("At least one component of the translation is NaN.");
            }
            _pendingMovementVector += translation;
        }

        #endregion

        #region GetVelocity

        /// <returns>
        /// World-space velocity of the <see cref="PhysicsCharacterController"/>.
        /// </returns>
        public virtual Vector3 GetVelocity() => _finalVelocity;

        #endregion

        #region SetVelocity

        /// <summary>
        /// Sets the velocity of the <see cref="PhysicsCharacterController"/> to the <paramref name="newVelocity"/>.
        /// </summary>
        /// <param name="newVelocity">New world-space velocity of the <see cref="PhysicsCharacterController"/>.</param>
        public virtual void SetVelocity(in Vector3 newVelocity) {
            if (float.IsNaN(newVelocity.x) || float.IsNaN(newVelocity.y) || float.IsNaN(newVelocity.z)) {
                throw new ArgumentException("At least one component of the new velocity is NaN.");
            }
            _finalVelocity = newVelocity;
            _velocity = newVelocity;
            _pendingTimeDependantVelocity = Vector3.zero;
        }

        #endregion

        #region GetPosition

        /// <returns>
        /// Position of the <see cref="PhysicsCharacterController"/> in world-space.
        /// </returns>
        public virtual Vector3 GetPosition() => transform.TransformPoint(_centre.x, 0.0f, _centre.z);

        #endregion

        #region SetPosition

        /// <summary>
        /// Sets the position of the <see cref="PhysicsCharacterController"/> to the <paramref name="newWorldPosition"/>.
        /// </summary>
        /// <param name="newWorldPosition">World-space position of the <see cref="PhysicsCharacterController"/>.</param>
        public virtual void SetPosition(in Vector3 newWorldPosition) {
            if (float.IsNaN(newWorldPosition.x) || float.IsNaN(newWorldPosition.y) || float.IsNaN(newWorldPosition.z)) {
                throw new ArgumentException("At least one component of the new position is NaN.");
            }
            transform.position = newWorldPosition;
            _centre = new Vector3(0.0f, _height * 0.5f, 0.0f);
            _characterController.center = _centre;
        }

        #endregion

        #region GetRotation

        /// <returns>
        /// Rotation of the <see cref="PhysicsCharacterController"/> in world-space.
        /// </returns>
        public virtual Quaternion GetRotation() => transform.rotation;

        #endregion

        #region SetRotation

        /// <summary>
        /// Sets the rotation of the <see cref="PhysicsCharacterController"/> to the <paramref name="newWorldRotation"/>.
        /// </summary>
        /// <param name="newWorldRotation">World-space rotation of the <see cref="PhysicsCharacterController"/>.</param>
        public virtual void SetRotation(in Quaternion newWorldRotation) {
            if (float.IsNaN(newWorldRotation.x) || float.IsNaN(newWorldRotation.y) || float.IsNaN(newWorldRotation.z) || float.IsNaN(newWorldRotation.w)) {
                throw new ArgumentException("At least one component of the new rotation is NaN.");
            }
            transform.rotation = newWorldRotation;
        }

        #endregion

        #region GetMass

        /// <returns>
        /// Mass of the <see cref="PhysicsCharacterController"/> in kg.
        /// </returns>
        public virtual float GetMass() => _mass;

        #endregion

        #region SetMass

        /// <summary>
        /// Sets the mass of the <see cref="PhysicsCharacterController"/> to the <paramref name="newMass"/>.
        /// </summary>
        /// <param name="newMass">New mass of the <see cref="PhysicsCharacterController"/> in kg.</param>
        public virtual void SetMass(in float newMass) {
            if (newMass < 0.0f || !float.IsNormal(newMass)) throw new ArgumentException("New mass must be a positive number.");
            // calculate mass constants:
            _mass = newMass;
            _inverseMass = 1.0f / newMass;
        }

        #endregion

        #region GetCentreOfMass

        /// <returns>
        /// Centre of mass of the <see cref="PhysicsCharacterController"/> in kg.
        /// </returns>
        /// <seealso cref="GetCentreOfMassWorldSpace"/>
        public virtual Vector3 GetCentreOfMass() => _centreOfMass;

        #endregion

        #region SetCentreOfMass

        /// <summary>
        /// Sets the centre of mass of the <see cref="PhysicsCharacterController"/> to the <paramref name="newCentreOfMass"/>.
        /// </summary>
        /// <param name="newCentreOfMass">New centre of mass in local-space relative to the <see cref="transform"/>.</param>
        public virtual void SetCentreOfMass(in Vector3 newCentreOfMass) {
            if (float.IsNaN(newCentreOfMass.x) || float.IsNaN(newCentreOfMass.y) || float.IsNaN(newCentreOfMass.z)) {
                throw new ArgumentException("At least one component of the new centre of mass is NaN.");
            }
            _centreOfMass = newCentreOfMass;
        }

        #endregion

        #region GetCentreOfMassWorldSpace

        /// <returns>
        /// World-space centre of mass.
        /// </returns>
        /// <seealso cref="GetCentreOfMass"/>
        public Vector3 GetCentreOfMassWorldSpace() {
            Vector3 centreOfMass = GetCentreOfMass();
            return transform.TransformPoint(
                centreOfMass.x + _centre.x,
                centreOfMass.y,
                centreOfMass.z + _centre.z
            );
        }

        #endregion

        #region AddForce

        /// <summary>
        /// Adds a <paramref name="force"/> to the <see cref="XRLocomotionController"/>.
        /// </summary>
        public virtual void AddForce(in Vector3 force, in ForceMode forceMode) {
            switch (forceMode) {
                case ForceMode.Force: {
                    _pendingTimeDependantVelocity += force * _inverseMass;
                    break;
                }
                case ForceMode.Impulse: {
                    _velocity += force * _inverseMass;
                    break;
                }
                case ForceMode.Acceleration: {
                    _pendingTimeDependantVelocity += force;
                    break;
                }
                case ForceMode.VelocityChange: {
                    _velocity += force;
                    break;
                }
            }
        }

        #endregion

        #region AddForceAtPosition

        /// <summary>
        /// Adds a <paramref name="force"/> at a specific <paramref name="position"/>.
        /// </summary>
        public virtual void AddForceAtPosition(in Vector3 force, in Vector3 position, in ForceMode forceMode) {
            AddForce(force, forceMode);
        }

        #endregion

        #region AddExplosionForce

        /// <summary>
        /// Adds an explosion <paramref name="explosionForce"/> to the <see cref="PhysicsCharacterController"/> at a specific <paramref name="explosionPosition"/>.
        /// </summary>
        /// <param name="explosionForce">Maximum magnitude of the force that can be created from the explosion.</param>
        /// <param name="explosionPosition">World-space point that the explosion propogates from.</param>
        /// <param name="explosionRadius">Radius of the explosion measured from the <paramref name="explosionPosition"/> to the edge of the explosion.</param>
        /// <param name="upwardsModifier">Adjustment to the apparent position of the explosion to make it seem to lift objects.</param>
        /// <param name="forceMode"><see cref="ForceMode"/> to apply the explosion force with.</param>
        public virtual void AddExplosionForce(in float explosionForce, in Vector3 explosionPosition, in float explosionRadius, float upwardsModifier, in ForceMode forceMode) {
            if (explosionRadius < 0.0f) throw new ArgumentException(nameof(explosionRadius) + " cannot be negative.");
            // calculate the vector from the point to the centre of mass:
            Vector3 explosionVector = GetCentreOfMassWorldSpace() - explosionPosition;
            // calculate the square distance between the point and the centre of mass:
            float sqrDistance = explosionVector.sqrMagnitude;
            if (sqrDistance > explosionRadius * explosionRadius) return; // the distance is greater than the explosion radius, do not do anything.
            // apply the explosion upwards modifier:
            explosionVector.y += upwardsModifier;
            // calculate and add the explosion force:
            AddForce(explosionForce * (1.0f - (1.0f / explosionRadius)) * explosionVector, forceMode);
        }

        #endregion

        #region FindGroundPhysicMaterial

        /// <summary>
        /// Finds the <see cref="PhysicMaterial"/> for the surface that the <see cref="PhysicsCharacterController"/> is in contact with and
        /// assigns it to the <see cref="_groundPhysicMaterial"/>.
        /// </summary>
        protected void FindGroundPhysicMaterial() {
            // calculate the cast distance:
            float castDistance = _actualRadius + _skinWidth + 0.0001f; // the sphere-cast needs to travel the radius of the controller plus the skin radius since a collider could have penetrated slightly into the skin of the controller
            // calculate the sphere-cast start position (in world-space):
            Vector3 castStart = GetPosition();
            castStart.y += castDistance;
            // begin the sphere-cast:
            if (Physics.SphereCast(castStart, castDistance + 0.0001f, Vector3.down, out RaycastHit hit, _actualRadius + 0.01f, _solidMask, QueryTriggerInteraction.Ignore)) {
                _groundPhysicMaterial = hit.collider.material;
            } else { // nothing was hit
                _groundPhysicMaterial = null;
            }
        }

        #endregion

        #region ClearGroundPhysicMaterial

        /// <summary>
        /// Clears the <see cref="_groundPhysicMaterial"/>.
        /// </summary>
        protected void ClearGroundPhysicMaterial() {
            _groundPhysicMaterial = null;
        }

        #endregion

        #region ResetController

        private void ResetController() {
            height = _actualHeight;
            radius = _actualRadius;
            ResetControllerState();
        }

        #endregion

        #region ResetControllerState

        private void ResetControllerState() {
            SetVelocity(Vector3.zero);
            _isGrounded = false;
            ClearGroundPhysicMaterial();
        }

        #endregion

        #endregion

    }

}