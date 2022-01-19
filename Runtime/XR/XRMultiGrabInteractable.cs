#if USE_XR_TOOLKIT

using System;

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace BlackTundra.World.XR {

    /// <summary>
    /// Extends <see cref="XRGrabInteractable"/> by allowing multiple grab points.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu("XR/XR Multi-Grab Interactable")]
#endif
    [DisallowMultipleComponent]
    public sealed class XRMultiGrabInteractable : XRGrabInteractable {

        #region variable

        /// <summary>
        /// Additional interactable points on the <see cref="XRMultiGrabInteractable"/>.
        /// </summary>
        [SerializeField]
        private XRBaseInteractable[] additionalInteractables = new XRBaseInteractable[0];

        #endregion

        #region property

        public IXRSelectInteractor PrimaryInteractor {
            get => _primaryInteractor;
            private set {
                if (_primaryInteractor == value) return;
                if (_primaryInteractor != null) _primaryInteractor.GetAttachTransform(this).localRotation = _primaryInteractorAttachRotation;
                if (value != null) _primaryInteractorAttachRotation = value.GetAttachTransform(this).localRotation;
                _primaryInteractor = value;
            }
        }
        private IXRSelectInteractor _primaryInteractor = null;
        private Quaternion _primaryInteractorAttachRotation = Quaternion.identity;

        public IXRSelectInteractor SecondaryInteractor {
            get => _secondaryInteractor;
            private set => _secondaryInteractor = value;
        }
        private IXRSelectInteractor _secondaryInteractor = null;

        #endregion

        #region logic

        #region Awake

        protected sealed override void Awake() {
            base.Awake();
            int additionalGrabPointCount = additionalInteractables.Length;
            XRBaseInteractable grabPoint;
            for (int i = additionalGrabPointCount - 1; i >= 0; i--) {
                grabPoint = additionalInteractables[i];
                if (grabPoint != null) {
                    grabPoint.hoverEntered.AddListener(OnAdditionalGrabPointHoverEntered);
                    grabPoint.hoverExited.AddListener(OnAdditionalGrabPointHoverExited);
                    grabPoint.selectEntered.AddListener(OnAdditionalGrabPointSelectEntered);
                    grabPoint.selectExited.AddListener(OnAdditionalGrabPointSelectExited);
                    grabPoint.activated.AddListener(OnAdditionalGrabPointActivated);
                    grabPoint.deactivated.AddListener(OnAdditionalGrabPointDeactivated);
                }
            }
        }

        #endregion

        #region OnDestroy

        protected sealed override void OnDestroy() {
            base.OnDestroy();
            XRBaseInteractable grabPoint;
            for (int i = additionalInteractables.Length - 1; i >= 0; i--) {
                grabPoint = additionalInteractables[i];
                if (grabPoint != null) {
                    grabPoint.hoverEntered.RemoveListener(OnAdditionalGrabPointHoverEntered);
                    grabPoint.hoverExited.RemoveListener(OnAdditionalGrabPointHoverExited);
                    grabPoint.selectEntered.RemoveListener(OnAdditionalGrabPointSelectEntered);
                    grabPoint.selectExited.RemoveListener(OnAdditionalGrabPointSelectExited);
                    grabPoint.activated.RemoveListener(OnAdditionalGrabPointActivated);
                    grabPoint.deactivated.RemoveListener(OnAdditionalGrabPointDeactivated);
                }
            }
        }

        #endregion

        #region ProcessInteractable

        public sealed override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase) {
            if (_primaryInteractor != null && _secondaryInteractor != null) {
                Quaternion lookRotation = Quaternion.LookRotation(
                    _secondaryInteractor.transform.position - _primaryInteractor.transform.position,
                    _primaryInteractor.transform.up
                );
                lookRotation *= Quaternion.Euler(0.0f, 0.0f, _primaryInteractor.transform.eulerAngles.z);
                _primaryInteractor.GetAttachTransform(this).rotation = lookRotation;
            }
            base.ProcessInteractable(updatePhase);
        }

        #endregion

        #region OnAdditionalGrabPointHoverEntered

        private void OnAdditionalGrabPointHoverEntered(HoverEnterEventArgs args) {
            OnHoverEntered(args);
        }

        #endregion

        #region OnAdditionalGrabPointHoverExited

        private void OnAdditionalGrabPointHoverExited(HoverExitEventArgs args) {
            OnHoverExited(args);
        }

        #endregion

        #region OnAdditionalGrabPointSelectEntered

        private void OnAdditionalGrabPointSelectEntered(SelectEnterEventArgs args) {
            OnSelectEntered(args);
        }

        #endregion

        #region OnAdditionalGrabPointSelectExited

        private void OnAdditionalGrabPointSelectExited(SelectExitEventArgs args) {
            OnSelectExited(args);
        }

        #endregion

        #region OnAdditionalGrabPointActivated

        private void OnAdditionalGrabPointActivated(ActivateEventArgs args) {
            OnActivated(args);
        }

        #endregion

        #region OnAdditionalGrabPointDeactivated

        private void OnAdditionalGrabPointDeactivated(DeactivateEventArgs args) {
            OnDeactivated(args);
        }

        #endregion

        #region OnSelectEntered

        protected sealed override void OnSelectEntered(SelectEnterEventArgs args) {
            IXRSelectInteractor interactor = args.interactorObject;
            Debug.Log("EE");
            if (interactor != null) {
                if (_primaryInteractor == null) PrimaryInteractor = interactor;
                else if (_primaryInteractor != interactor) SecondaryInteractor = interactor;
            }
            base.OnSelectEntered(args);
        }

        #endregion

        #region OnSelectExited

        protected sealed override void OnSelectExited(SelectExitEventArgs args) {
            IXRSelectInteractor interactor = args.interactorObject;
            if (interactor != null) {
                if (_primaryInteractor == interactor) PrimaryInteractor = null;
                else if (_secondaryInteractor == interactor) SecondaryInteractor = null;
            }
            base.OnSelectExited(args);
        }

        #endregion

        #region IsSelectedBy

        public sealed override bool IsSelectableBy(IXRSelectInteractor interactor) {
            if (interactor == null) throw new ArgumentNullException(nameof(interactor));
            bool isAlreadyGrabbed = interactorsSelecting.Contains(interactor); // check if the interactable has already been grabbed
            return base.IsSelectableBy(interactor) && !isAlreadyGrabbed;
        }

        #endregion

        #endregion

    }

}

#endif