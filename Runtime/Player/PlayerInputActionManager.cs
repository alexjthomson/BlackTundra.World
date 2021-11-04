using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackTundra.World.Player {

    /// <summary>
    /// Automatically enables/disables any <see cref="InputActionAsset"/> that is required by the player.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu("Player/Player Input Action Manager")]
#endif
    [DisallowMultipleComponent]
    public sealed class PlayerInputActionManager : MonoBehaviour {

        #region variable

        /// <summary>
        /// <see cref="InputActionAsset"/> references that are used by the player.
        /// </summary>
        [SerializeField]
        //[Tooltip("Input action assets to affect when inputs are enabled or disabled.")]
        private List<InputActionAsset> actionAssets = new List<InputActionAsset>();

        #endregion

        #region logic

        #region OnEnable

        private void OnEnable() {
            EnableInput();
        }

        #endregion

        #region OnDisable

        private void OnDisable() {
            DisableInput();
        }

        #endregion

        #region EnableInput

        private void EnableInput() {
            if (actionAssets == null) return;
            InputActionAsset asset;
            for (int i = actionAssets.Count - 1; i >= 0; i--) {
                asset = actionAssets[i];
                if (asset != null) {
                    asset.Enable();
                }
            }
        }

        #endregion

        #region DisableInput

        private void DisableInput() {
            if (actionAssets == null) return;
            InputActionAsset asset;
            for (int i = actionAssets.Count - 1; i >= 0; i--) {
                asset = actionAssets[i];
                if (asset != null) {
                    asset.Disable();
                }
            }
        }

        #endregion

        #endregion

    }

}