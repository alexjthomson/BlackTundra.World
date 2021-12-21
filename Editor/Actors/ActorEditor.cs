using BlackTundra.Foundation.Editor.Utility;
using BlackTundra.World.Actors;

using UnityEditor;

using UnityEngine;
using UnityEngine.AI;

namespace BlackTundra.World.Editor.Actors {

    [CustomEditor(typeof(Actor))]
    public sealed class ActorEdtior : CustomInspector {

        #region variable

        private Actor actor = null;
        private ActorProfile profile = null;
        private NavMeshAgent agent = null;

        private bool profileEditor = false;
        private int toolbarIndex = 0;
        private float scrollHeight = 0.0f;

        #endregion

        #region logic

        private void OnEnable() {
            actor = (Actor)target;
            profile = actor.profile;
            agent = actor.GetComponent<NavMeshAgent>();
            if (agent == null) {
                agent = actor.gameObject.AddComponent<NavMeshAgent>();
                actor.SetupAgent();
                MarkAsDirty(agent);
            }
            //agent.hideFlags = UnityEngine.HideFlags.HideInInspector;
        }

        protected sealed override void DrawInspector() {
            if (Application.isPlaying) {
                EditorLayout.TextField("Behaviour", actor.Behaviour?.GetType().Name ?? "None");
            }
            CapsuleCollider collider = actor.GetComponent<CapsuleCollider>();
            bool oldUseCollider;
            if (collider != null) {
                //collider.hideFlags = HideFlags.HideInInspector;
                oldUseCollider = collider.enabled;
            } else {
                oldUseCollider = false;
            }
            bool useCollider = EditorLayout.BooleanField("Use Collider", oldUseCollider);
            if (useCollider != oldUseCollider) {
                actor.UseCollider = useCollider;
                if (useCollider) actor.SetupCollider();
                MarkAsDirty(agent.gameObject);
            }
            ActorProfile newProfile = EditorLayout.ReferenceField("Profile", profile, false);
            if (!Application.isPlaying) {
                if (newProfile != profile) {
                    profile = newProfile;
                    actor.profile = profile;
                    MarkAsDirty();
                }
                if (profile == null) {
                    EditorLayout.Error("An actor must have an actor profile assigned to it so it can function properly.");
                } else {
                    profileEditor = EditorLayout.Foldout("Profile Editor", profileEditor);
                    if (profileEditor && ActorProfileEditor.DrawProfile(profile, ref toolbarIndex, ref scrollHeight)) {
                        actor.SetupAgent();
                        if (useCollider) actor.SetupCollider();
                        MarkAsDirty(agent);
                    }
                }
            }
        }

        #endregion

    }

}