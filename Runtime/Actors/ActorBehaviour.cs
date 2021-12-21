using BlackTundra.World.Audio;

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

namespace BlackTundra.World.Actors {

    /// <summary>
    /// Base class used to implement how an <see cref="Actor"/> should behave when it recieves different inputs.
    /// </summary>
    public abstract class ActorBehaviour {

        #region property

        /// <summary>
        /// <see cref="Actor"/> that the behaviour is applied to. This will be <c>null</c> until just before
        /// <see cref="OnBehaviourStarted(in ActorBehaviour)"/> is invoked.
        /// </summary>
        /// <seealso cref="Actor.SetBehaviour(in ActorBehaviour)"/>
#pragma warning disable IDE1006 // naming styles
        public Actor actor { get; internal set; } = null;
#pragma warning restore IDE1006 // naming styles

        /// <summary>
        /// <see cref="NavMeshAgent"/> associated with the <see cref="actor"/> property. Like the <see cref="actor"/>
        /// property, this will have a <c>null</c> value until just before <see cref="OnBehaviourStarted(in ActorBehaviour)"/>
        /// is invoked.
        /// </summary>
        /// <seealso cref="actor"/>
#pragma warning disable IDE1006 // naming styles
        public NavMeshAgent agent { get; internal set; } = null;
#pragma warning restore IDE1006 // naming styles

        #endregion

        #region logic

        #region OnBehaviourStarted

        /// <summary>
        /// Invoked when this <see cref="ActorBehaviour"/> instance has started to be used by the <see cref="actor"/>.
        /// </summary>
        /// <param name="previousBehaviour">
        /// Previous <see cref="ActorBehaviour"/> that controlled the <see cref="Actor"/> before this <see cref="ActorBehaviour"/>.
        /// </param>
        protected internal abstract void OnBehaviourStarted(in ActorBehaviour previousBehaviour);

        #endregion

        #region OnBehaviourChanged

        /// <summary>
        /// Invoked when this <see cref="ActorBehaviour"/> instance has stopped being used by the <see cref="actor"/> and
        /// a new behaviour is assigned (<paramref name="nextBehaviour"/>).
        /// </summary>
        /// <param name="nextBehaviour">
        /// New <see cref="ActorBehaviour"/> that the <see cref="actor"/> will be using to determind how it should behave.
        /// </param>
        protected internal abstract void OnBehaviourChanged(in ActorBehaviour nextBehaviour);

        #endregion

        #region OnActorUpdated

        /// <summary>
        /// Invoked when the <see cref="actor"/> is updated. At most, this can be called once every frame. Otherwise it
        /// will be called in batches with other <see cref="Actor"/> instances to prevent the application from lagging
        /// too much.
        /// </summary>
        /// <param name="deltaTime">Number of seconds since the last time the <see cref="Actor"/> was updated.</param>
        protected internal abstract void OnActorUpdated(in float deltaTime);

        #endregion

        #region OnActorTargetUpdated

        /// <summary>
        /// Invoked when the <see cref="actor"/> target is updated. This may be because the <paramref name="collider"/>
        /// has moved a significant distance from the last known position and the <paramref name="position"/> has been
        /// updated to reflect that. It could also be because the target position was simply updated, this will be the
        /// case if <paramref name="collider"/> is <c>null</c>.
        /// </summary>
        /// <param name="collider"><see cref="Collider"/> component that the <see cref="actor"/> is tracking.</param>
        /// <param name="position">Position that the <see cref="actor"/> wants to get to.</param>
        /// <seealso cref="Actor.SetTargetCollider(in Collider)"/>
        /// <seealso cref="Actor.SetTargetPosition(in Vector3)"/>
        protected internal abstract void OnActorTargetUpdated(in Collider collider, in Vector3 position);

        #endregion

        #region OnActorDamaged

        /// <summary>
        /// Invoked when the <see cref="actor"/> is damaged.
        /// </summary>
        /// <param name="sender">Sender/invoker of the damage.</param>
        /// <param name="damage">Total damage done to the <see cref="actor"/> by the <paramref name="sender"/>.</param>
        /// <param name="data">Any data sent with the damage, if no data is sent this will be <c>null</c>.</param>
        /// <returns>
        /// Returns the total damage actually delt to the <see cref="actor"/>. If the <see cref="actor"/> has something
        /// like a damage reduction multiplier, it can be applied to the <paramref name="damage"/> and returned to
        /// indicate not all the supplied <paramref name="damage"/> was actually delt to the <see cref="actor"/>.
        /// </returns>
        /// <remarks>
        /// If the <see cref="actor"/> cannot be damaged, return <c>0.0f</c> from this method.
        /// </remarks>
        protected internal abstract float OnActorDamaged(in object sender, in float damage, in object data);

        #endregion

        #region OnColliderVisible

        /// <summary>
        /// Invoked when a <see cref="Collider"/> is discovered to be visible to the <see cref="actor"/>.
        /// </summary>
        protected abstract void OnColliderVisible(in Collider collider);

        #endregion

        #region OnSoundHeard

        /// <summary>
        /// Invoked when a <see cref="SoundSample"/> is heard by the <see cref="actor"/>.
        /// </summary>
        protected abstract void OnSoundHeard(in SoundSample soundSample);

        #endregion

        /*
        #region EvaluateObject

        /// <summary>
        /// Evaluates the importance of a <paramref name="gameObject"/> to the <see cref="actor"/>.
        /// </summary>
        public void EvaluateObject(in GameObject gameObject) {
            if (gameObject == null) return;
            EvaluateObject(gameObject.GetCollider(actor.Profile.visualPerceptionLayerMask));
        }

        /// <summary>
        /// Evaluates the importance of a <see cref="collider"/> to the <see cref="actor"/>.
        /// </summary>
        public void EvaluateObject(in Collider collider) {
            if (collider == null) return;
            OnEvaluateObject(collider);
        }

        #endregion
        #region EvaluateObjects

        /// <summary>
        /// Evaluates the importance of an array of <paramref name="gameObjects">GameObjects</paramref> to
        /// the <see cref="actor"/>.
        /// </summary>
        public void EvaluateObjects(in GameObject[] gameObjects) {
            if (gameObjects == null) return;
            LayerMask layermask = actor.Profile.visualPerceptionLayerMask;
            GameObject gameObject;
            for (int i = gameObjects.Length - 1; i >= 0; i--) {
                gameObject = gameObjects[i];
                if (gameObject != null) EvaluateObject(gameObject.GetCollider(layermask));
            }
        }

        /// <summary>
        /// Evaluates the importance of a collection of <paramref name="gameObjects">GameObjects</paramref> to
        /// the <see cref="actor"/>.
        /// </summary>
        /// <param name="gameObjects">
        /// Collection of <see cref="GameObject"/> instances. This <see cref="IEnumerator{T}"/> will be disposed
        /// automatically at the end of this method.
        /// </param>
        public void EvaluateObjects(in IEnumerator<GameObject> gameObjects) {
            LayerMask layermask = actor.Profile.visualPerceptionLayerMask;
            GameObject gameObject;
            try {
                while (gameObjects.MoveNext()) {
                    gameObject = gameObjects.Current;
                    if (gameObject != null) EvaluateObject(gameObject.GetCollider(layermask));
                }
            } finally {
                gameObjects.Dispose();
            }
        }

        /// <summary>
        /// Evaluates the importance of an array of <paramref name="colliders"/> to the <see cref="actor"/>.
        /// </summary>
        public void EvaluateObjects(in Collider[] colliders) {
            if (colliders == null) return;
            LayerMask layermask = actor.Profile.visualPerceptionLayerMask;
            Collider collider;
            for (int i = colliders.Length - 1; i >= 0; i--) {
                collider = colliders[i];
                if (collider != null && layermask.ContainsLayer(collider.gameObject.layer))
                    OnEvaluateObject(collider);
            }
        }

        /// <summary>
        /// Evaluates the importance of a collection of <see cref="colliders"/> to the <see cref="actor"/>.
        /// </summary>
        /// <param name="colliders">
        /// Collection of <see cref="Collider"/> instances. This <see cref="IEnumerator{T}"/> will be disposed
        /// automatically at the end of this method.
        /// </param>
        public void EvaluateObjects(in IEnumerator<Collider> colliders) {
            LayerMask layermask = actor.Profile.visualPerceptionLayerMask;
            Collider collider;
            try {
                while (colliders.MoveNext()) {
                    collider = colliders.Current;
                    if (collider != null && layermask.ContainsLayer(collider.gameObject.layer))
                        OnEvaluateObject(collider);
                }
            } finally {
                colliders.Dispose();
            }
        }

        #endregion
        */

        #region QueryVisionSensor

        /// <summary>
        /// Queries the visual perception of the <see cref="actor"/>. This invokes <see cref="OnEvaluateObject(in Collider)"/> when an object is spotted.
        /// </summary>
        protected void QueryVisionSensor() {
            using (IEnumerator<Collider> visibleColliders = actor.QueryVisionSensor()) {
                while (visibleColliders.MoveNext()) {
                    OnColliderVisible(visibleColliders.Current);
                }
            }
        }

        #endregion

        #region QuerySoundSensor

        protected void QuerySoundSensor() {
            using (IEnumerator<SoundSample> soundSamples = actor.QuerySoundSensor()) {
                while (soundSamples.MoveNext()) {
                    OnSoundHeard(soundSamples.Current);
                }
            }
        }

        #endregion

        #endregion

    }

}