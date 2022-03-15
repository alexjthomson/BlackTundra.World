using BlackTundra.Foundation.Utility;
using BlackTundra.World.Items;
using BlackTundra.World.Pooling;

using System;

using UnityEngine;

namespace BlackTundra.World.Ballistics {

    /// <summary>
    /// Models a projectile.
    /// </summary>
    [Serializable]
    public sealed class Projectile : IObjectPoolable {

        #region constant

        /// <summary>
        /// Rate to take velocity when drag forces cause the projectile change direction extremely quickly.
        /// </summary>
        private const float VelocityDragDampenCoefficient = 0.25f;

#if UNITY_EDITOR
        /// <summary>
        /// Time in seconds that debug graphics will persist.
        /// </summary>
        private const float DebugGraphicsPersistTime = 0.1f;
#endif

        /// <summary>
        /// Coefficient used to convert the ratio of kinetic energy to material density to a penetration distance.
        /// </summary>
        private const float PenetrationDistanceCoefficient = 0.0125f;

        /// <summary>
        /// Percentage of energy that is transferred into kinetic energy.
        /// The rest of the energy is assumed to be turned into heat and sound.
        /// </summary>
        private const float ProjectileEnergyTransferEfficiency = 0.1f;

        #endregion

        #region variable

        /// <summary>
        /// <see cref="ProjectileProperties"/> that describe the <see cref="Projectile"/>.
        /// </summary>
        [SerializeField]
#if UNITY_EDITOR
        internal
#else
        private
#endif
        ProjectileProperties _properties;

        /// <summary>
        /// <see cref="ProjectileSimulationFlags"/> used to toggle <see cref="Projectile"/> simulation features on or off.
        /// </summary>
        [SerializeField]
        public ProjectileSimulationFlags simulationFlags;

        [NonSerialized]
        internal float _lifetime;

        [NonSerialized]
        internal Vector3 _position;

        [NonSerialized]
        internal Vector3 _velocity;

        /// <summary>
        /// <see cref="ObjectPool"/> that manages the <see cref="Projectile"/>.
        /// </summary>
        [NonSerialized]
        private ObjectPool parentPool;

        #endregion

        #region property

        public Vector3 position => _position;

        public Vector3 velocity => _velocity;

        public Vector3 forward => _velocity.normalized;

        /// <summary>
        /// Kinetic energy that the <see cref="Projectile"/> has.
        /// </summary>
        public float kineticEnergy => 0.5f * _properties.mass * _velocity.sqrMagnitude;

        /// <inheritdoc cref="_properties"/>
        public ProjectileProperties properties {
            get => _properties;
            set {
                if (value == null) throw new ArgumentNullException(nameof(properties));
                _properties = value;
            }
        }

        #endregion

        #region constructor

        public Projectile() {
            _properties = null;
            simulationFlags = ProjectileSimulationFlags.Gravity
                | ProjectileSimulationFlags.EnvironmentalDrag
                | ProjectileSimulationFlags.EnvironmentalForce
                | ProjectileSimulationFlags.Penetrate
                | ProjectileSimulationFlags.Ricochet
                | ProjectileSimulationFlags.TransferMomentum;
            _position = Vector3.zero;
            _velocity = Vector3.zero;
            _lifetime = -1.0f;
            parentPool = null;
        }

        #endregion

        #region logic

        #region SetStartParameters

        public void SetStartParameters(in Vector3 position, in Vector3 direction, in float kineticEnergy) {
            _properties.Initialise();
            _position = position;
            _velocity = CalculateVelocity(
                kineticEnergy,
                _properties._inverseMass,
                direction.normalized
            );
            _lifetime = 0.0f;
        }

        #endregion

        #region Simulate

        public void Simulate(in float deltaTime) {
            // lifetime calculation:
            _lifetime += deltaTime;
            if (_lifetime > _properties.lifetime) {
                OnLifetimeExpired();
                return;
            }
            // calculate speed and direction:
            float sqrSpeed = _velocity.sqrMagnitude; // calculate the square magnitude of the velocity
            float speed = Mathf.Sqrt(sqrSpeed); // calculate the speed of the projectile
            Vector3 direction = _velocity * (1.0f / speed); // calculate the direction that the projectile is currently moving in
            // create new velocity variable:
            Vector3 newVelocity = _velocity;
            // calculate environmental drag:
            if ((simulationFlags & ProjectileSimulationFlags.EnvironmentalDrag) != 0) {
                float rho = Environment.RhoAt(_position);
                Vector3 dragDeltaVelocity = (_properties._dragCoefficient * rho * sqrSpeed * deltaTime) * direction;
                newVelocity += dragDeltaVelocity;
            }
            // calculate environmental force:
            if ((simulationFlags & ProjectileSimulationFlags.EnvironmentalForce) != 0) {
                Vector3 environmentalForce = Environment.EnvironmentalForceAt(_position);
                newVelocity += environmentalForce * (_properties._inverseMass * deltaTime);
            }
            // calculate gravity:
            if ((simulationFlags & ProjectileSimulationFlags.Gravity) != 0) {
                float gravity = Environment.gravity;
                newVelocity.y -= gravity * deltaTime;
            }
            // recalculate direction:
            Vector3 lastDirection = direction;
            sqrSpeed = newVelocity.sqrMagnitude;
            speed = Mathf.Sqrt(sqrSpeed);
            direction = newVelocity * (1.0f / speed);
            // detect significant change in direction:
            if (Vector3.Dot(lastDirection, direction) < -Mathf.Epsilon) {
                newVelocity = _velocity * VelocityDragDampenCoefficient; // apply breaking (stops significant changes in direction of projectile due to external forces)
                sqrSpeed *= VelocityDragDampenCoefficient * VelocityDragDampenCoefficient;
                speed *= VelocityDragDampenCoefficient;
            }
            // apply velocity:
            _velocity = newVelocity;
            Vector3 lastPosition = _position;
            Vector3 deltaPosition = _velocity * deltaTime;
            _position += deltaPosition;
            RaycastHit hit;
            bool hitSuccess;
            if ((simulationFlags & ProjectileSimulationFlags.SphereCast) != 0) {
                hitSuccess = Physics.SphereCast(
                    lastPosition,
                    _properties.radius,
                    deltaPosition,
                    out hit,
                    deltaPosition.magnitude,
                    _properties.layerMask,
                    QueryTriggerInteraction.Ignore
                );
            } else {
                hitSuccess = Physics.Linecast(
                    lastPosition,
                    _position,
                    out hit,
                    _properties.layerMask,
                    QueryTriggerInteraction.Ignore
                );
            }
            if (hitSuccess) { // the projectile hit something
                Vector3 point = hit.point;
                Vector3 normal = hit.normal;
                float collisionAngle = Vector3.Angle(normal, lastPosition - point); // get the angle between the direction of travel and the normal vector
                float kineticEnergy = 0.5f * _properties.mass * sqrSpeed; // calculate the kinetic energy of the projectile
#if UNITY_EDITOR
                Debug.DrawLine(lastPosition, point, Color.red, DebugGraphicsPersistTime); // delta position line
                Debug.DrawLine(point, point + normal, Color.blue, DebugGraphicsPersistTime); // normal line
#endif
                if ((simulationFlags & ProjectileSimulationFlags.Ricochet) != 0 && collisionAngle > _properties.ricochetThresholdAngle) { // the projectile should ricochet
                    Collider collider = hit.collider;
                    MaterialDescriptor material = MaterialDatabase.GetMaterialDescriptor(collider.sharedMaterial);
                    float thresholdRicochetEnergy = (1.0f - material.hardness) * _properties.ricochetMaxEnergy;
                    if (kineticEnergy > thresholdRicochetEnergy) { // projectile has too much energy and will disintegrate
                        RegisterHit(hit, direction, ProjectileHitType.Disintegrate, kineticEnergy); // register the disentegration
                        OnLifetimeExpired();
                    } else { // projectile will ricochet off of the surface of the hit object
                        _position = point; // move the projectile to the point that it impacted
                        lastDirection = direction;
                        direction = Vector3.Reflect(direction, normal).normalized; // reflect the direciton that the projectile is travelling in based off of how it impacted
                        PhysicMaterial physicMaterial = material.material;
                        float dynamicFriction = physicMaterial != null ? physicMaterial.dynamicFriction : 0.5f;
                        float energyLossCoefficient = Mathf.Clamp(dynamicFriction, 0.01f, 1.0f);
                        float energyTransfer = kineticEnergy * energyLossCoefficient; // calculate the amount of energy to transfer to the hit object
                        float newKineticEnergy = kineticEnergy - energyTransfer; // calculate the amount of kinetic energy that the projectile has left
                        _velocity = CalculateVelocity(newKineticEnergy, _properties._inverseMass, direction); // calculate the new velocity of the projectile based on the amount of energy transferred
                        RegisterHit(hit, lastDirection, ProjectileHitType.Ricochet, energyTransfer); // register the ricochet
                    }
                } else if ((simulationFlags & ProjectileSimulationFlags.Penetrate) != 0 && _properties.penetrationPower > 0.0f) { // the projectile should not ricochet
                    Collider collider = hit.collider;
                    MaterialDescriptor material = MaterialDatabase.GetMaterialDescriptor(collider.sharedMaterial);
                    float density = material.density; // density of the material being impacted
                    float maximumPenetrationDistance = _properties.penetrationPower * PenetrationDistanceCoefficient * (kineticEnergy / density); // maximum penetration distance that the projectile can penetrate into the material
                    Vector3 penetrationDirection = (direction + (-0.2f * normal)).normalized; // penetration direction of the projectile
#if UNITY_EDITOR
                    Debug.DrawLine(point, point + (penetrationDirection * maximumPenetrationDistance), Color.magenta, DebugGraphicsPersistTime); // draw penetration line
#endif
                    float exitRayDistance = Mathf.Min(collider.bounds.max.LargestComponent(), maximumPenetrationDistance); // calculate the distance that the exit ray should use
                    RaycastHit[] penetrationHits = Physics.RaycastAll( // cast ray into material
                        point + (penetrationDirection * exitRayDistance), // find the end point of the exit ray, this is where the ray should originate from since it will be casting in reverse
                        -penetrationDirection, // use the reverse of the penetration direction since the ray origin is at the exit point
                        exitRayDistance - Mathf.Epsilon, // use the exit ray distance (take away epsilon just incase backfaces are detected)
                        _properties.layerMask // use layer mask
                    );
                    int hitCount = penetrationHits.Length;
                    if (hitCount > 0) {
                        float furthestDistance = float.MinValue; // value used to store the hit point furthest from the penetration ray origin
                        int furthestHit = -1; // store the index of the raycast hit that's the closest to the projectile (and furthest fromt he penetration ray origin)
                        RaycastHit currentHit;
                        for (int i = 0; i < hitCount; i++) { // iterate each hit
                            currentHit = penetrationHits[i];
                            if (collider == currentHit.collider) { // compare colliders since only the originally hit collider should be considered
                                float distance = currentHit.distance; // get the distance from the start of the penetration ray origin
                                if (distance > furthestDistance) { // distance is further than the current furthest distance
                                    furthestDistance = distance; // update the furthest distance
                                    furthestHit = i; // store the index of the raycast hit
                                }
                            }
                        }
                        if (furthestHit != -1) { // found exit point
                            float exitPointDistance = exitRayDistance - furthestHit; // calculate how far the exit point is from the entry point along the penetration direction
#if UNITY_EDITOR
                            Debug.DrawLine(point, point + (penetrationDirection * exitPointDistance), Color.green, DebugGraphicsPersistTime); // draw penetration line
#endif
                            if (exitPointDistance > maximumPenetrationDistance) { // projectile does not have enough energy to penetrate the hit object
                                RegisterHit(hit, penetrationDirection, ProjectileHitType.PenetratePartial, kineticEnergy);
                                OnLifetimeExpired();
                            } else { // projectile does have enough energy to penetrate the hit object fully
                                currentHit = penetrationHits[furthestHit]; // get the hit point at the exit point
#if UNITY_EDITOR
                                Debug.DrawRay(currentHit.point, currentHit.normal, Color.blue, DebugGraphicsPersistTime); // draw exit normal
#endif
                                float energyTransferPercent = exitPointDistance / maximumPenetrationDistance; // calculate the percetage of energy that should be transferred to the hit object
                                float energyTransferred = kineticEnergy * energyTransferPercent; // calculate the amount of energy transferred to the hit object
                                float exitEnergy = kineticEnergy - energyTransferred; // calculate the remaining energy of the projectile
                                _position = currentHit.point; // move the projectile to the exit point
                                direction = (penetrationDirection + (0.2f * currentHit.normal)).normalized; // calculate the exit direction of the projectile
                                _velocity = CalculateVelocity(exitEnergy, _properties._inverseMass, direction); // calculate the exit velocity of the projectile
                                RegisterHit(hit, penetrationDirection, ProjectileHitType.PenetrateFull, energyTransferred); // register the hit
                            }
                        } else { // failed to find exit point, therefore the projectile got embedded into the hit object
                            RegisterHit(hit, penetrationDirection, ProjectileHitType.PenetratePartial, kineticEnergy); // projectile is embedded into hit object
                            OnLifetimeExpired();
                        }
                    } else { // no penetration hits found, therefore no exit points were found, therefore the projectile get embedded into the hit object
                        RegisterHit(hit, penetrationDirection, ProjectileHitType.PenetratePartial, kineticEnergy); // projectile is embedded into hit object
                        OnLifetimeExpired();
                    }
                } else { // projectile cannot penetrate the target material
                    RegisterHit(hit, direction, ProjectileHitType.Disintegrate, kineticEnergy); // projectile cannot penetrate the surface or ricochet, therefore it must disintegrate upon impact
                    OnLifetimeExpired();
                }
            } else { // nothing was hit
#if UNITY_EDITOR
                Debug.DrawLine(lastPosition, _position, Color.white, DebugGraphicsPersistTime); // draw projectile path
#endif
            }
        }

        #endregion

        #region CalculateVelocity

        /// <param name="direction">Normalized direction that the velocity should travel in.</param>
        private static Vector3 CalculateVelocity(in float kineticEnergy, in float inverseMass, in Vector3 direction)
            => direction * Mathf.Sqrt(2.0f * kineticEnergy * inverseMass);

        #endregion

        #region RegisterHit

        private void RegisterHit(in RaycastHit hit, in Vector3 direction, in ProjectileHitType hitType, in float energyTransferred) {
            Collider collider = hit.collider;
            // impact:
            IImpactable impactable = collider.GetComponentInParent<IImpactable>(); // check if the object is impactable
            if (impactable != null) { // object is impactable
                impactable.OnImpact( // impact the object
                    _velocity,
                    hit.point,
                    energyTransferred
                );
            }
            // transfer momentum:
            if ((simulationFlags & ProjectileSimulationFlags.TransferMomentum) != 0) { // flag set
                IPhysicsObject physicsObject = collider.GetComponentInParent<IPhysicsObject>(); // check if the hit collider is a physics object
                if (physicsObject != null) { // target collider is an item
                    Vector3 impactForce = CalculateVelocity(
                        energyTransferred * ProjectileEnergyTransferEfficiency,
                        1.0f / physicsObject.mass,
                        direction
                    );
                    physicsObject.AddForceAtPosition(impactForce, hit.point, ForceMode.VelocityChange);
                } else { // target collider is not an item
                    Rigidbody rigidbody = collider.GetComponentInParent<Rigidbody>(); // check if the target item has a rigidbody
                    if (rigidbody != null && !rigidbody.isKinematic) { // the target item does have a rididbody
                        Vector3 impactForce = CalculateVelocity(
                            energyTransferred * ProjectileEnergyTransferEfficiency,
                            1.0f / rigidbody.mass,
                            direction
                        );
                        rigidbody.AddForceAtPosition(impactForce, hit.point, ForceMode.VelocityChange);
                    }
                }
            }
        }

        #endregion

        #region OnLifetimeExpired

        private void OnLifetimeExpired() {
            _lifetime = -1.0f;
            if (parentPool != null) {
                parentPool.ReturnToPool(this);
                parentPool = null;
            }
        }

        #endregion

        #region IsAvailable

        public bool IsAvailable(in ObjectPool objectPool) {
            if (objectPool == null) throw new ArgumentNullException(nameof(objectPool));
            return parentPool == null && _lifetime < 0.0f;
        }

        #endregion

        #region OnPoolUse

        public void OnPoolUse(in ObjectPool objectPool) {
            if (objectPool == null) throw new ArgumentNullException(nameof(objectPool));
            if (parentPool != null && parentPool != objectPool) {
                parentPool.ReturnToPool(this);
            }
            parentPool = objectPool;
        }

        #endregion

        #region OnPoolRelease

        public void OnPoolRelease(in ObjectPool objectPool) {
            if (objectPool == null) throw new ArgumentNullException(nameof(objectPool));
            if (parentPool == objectPool) {
                parentPool = null;
            }
        }

        #endregion

        #region OnPoolDispose

        public void OnPoolDispose(in ObjectPool objectPool) {
            if (objectPool == null) throw new ArgumentNullException(nameof(objectPool));
            if (parentPool == objectPool) {
                parentPool = null;
            }
        }

        #endregion

        #endregion

    }

}