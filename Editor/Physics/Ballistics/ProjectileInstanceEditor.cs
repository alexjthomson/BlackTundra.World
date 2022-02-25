using BlackTundra.Foundation.Editor.Utility;
using BlackTundra.World.Ballistics;

using UnityEditor;

using UnityEngine;

namespace BlackTundra.World.Editor.Ballistics {

    [CustomEditor(typeof(ProjectileInstance))]
    public sealed class ProjectileInstanceEditor : CustomInspector {

        #region constant

        private static readonly GUIContent PropertiesGUIContent = new GUIContent("Properties", "Physical properties of the projectile itself.");

        private static readonly GUIContent SimulationFlagsGUIContent = new GUIContent("Simulation Flags", "Flags used to toggle projectile simulation features on/off.");

        #endregion

        #region logic

        #region DrawInspector

        protected sealed override void DrawInspector() {
            ProjectileInstance projectileInstance = (ProjectileInstance)target;
            Projectile projectile = projectileInstance.projectile;
            if (projectile == null) {
                projectile = new Projectile();
                projectileInstance.projectile = projectile;
                MarkAsDirty(projectileInstance);
            }
            EditorLayout.Title("Projectile Properties");
            ProjectileProperties properties = EditorLayout.ReferenceField(PropertiesGUIContent, projectile.properties, false);
            if (properties != projectile.properties) {
                projectile.properties = properties;
                MarkAsDirty(projectileInstance);
            }
            ProjectileSimulationFlags flags = EditorLayout.EnumFlagsField(SimulationFlagsGUIContent, projectile.simulationFlags);
            if (flags != projectile.simulationFlags) {
                projectile.simulationFlags = flags;
                MarkAsDirty(projectileInstance);
            }
        }

        #endregion

        #endregion

    }

}