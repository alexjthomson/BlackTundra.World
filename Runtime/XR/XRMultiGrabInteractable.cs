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

        #region constant


        #endregion

        #region variable

        /// <summary>
        /// Additional grab points.
        /// </summary>
        [SerializeField]
        private XRBaseInteractable[] auxiliaryGrabPoints = new XRBaseInteractable[0];

        #endregion

        #region property

        public XRBaseInteractor PrimaryInteractor {
            get => primaryInteractor;
            private set {
                if (primaryInteractor == value) return;
                if (primaryInteractor != null) primaryInteractor.GetAttachTransform(this).localRotation = primaryInteractorAttachRotation;
                if (value != null) {
                    primaryInteractorAttachRotation = value.GetAttachTransform(this).localRotation;
                    primaryInteractable = value.GetOldestInteractableSelected() as XRBaseInteractable;
                } else {
                    secondaryInteractable = null;
                }
                primaryInteractor = value;
                if (value == secondaryInteractor) {
                    secondaryInteractor = null;
                }
            }
        }
        private XRBaseInteractor primaryInteractor = null;
        private XRBaseInteractable primaryInteractable = null;
        private Quaternion primaryInteractorAttachRotation = Quaternion.identity;

        public XRBaseInteractor SecondaryInteractor {
            get => SecondaryInteractor;
            private set {
                if (value != null && primaryInteractor == null) {
                    PrimaryInteractor = value;
                } else if (value != primaryInteractor && secondaryInteractor != value) {
                    secondaryInteractor = value;
                    if (value != null) {
                        secondaryInteractable = value.GetOldestInteractableSelected() as XRBaseInteractable;
                    } else {
                        primaryInteractor.GetAttachTransform(this).localRotation = primaryInteractorAttachRotation;
                        secondaryInteractable = null;
                    }
                }
            }
        }
        private XRBaseInteractor secondaryInteractor = null;
        private XRBaseInteractable secondaryInteractable = null;

        #endregion

        #region logic

        #region Awake

        protected sealed override void Awake() {
            base.Awake();
            // configure grab points:
            XRBaseInteractable grabPoint;
            for (int i = auxiliaryGrabPoints.Length - 1; i >= 0; i--) {
                grabPoint = auxiliaryGrabPoints[i];
                if (grabPoint != null) {
                    grabPoint.selectEntered.RemoveListener(OnSelectEntered);
                    grabPoint.selectEntered.AddListener(OnSelectEntered);
                    grabPoint.selectExited.RemoveListener(OnSelectExited);
                    grabPoint.selectExited.AddListener(OnSelectExited);
                }
            }
        }

        #endregion

        #region OnDestroy

        protected sealed override void OnDestroy() {
            base.OnDestroy();
            // configure grab points:
            XRBaseInteractable grabPoint;
            for (int i = auxiliaryGrabPoints.Length - 1; i >= 0; i--) {
                grabPoint = auxiliaryGrabPoints[i];
                if (grabPoint != null) {
                    grabPoint.selectEntered.RemoveListener(OnSelectEntered);
                    grabPoint.selectExited.RemoveListener(OnSelectExited);
                }
            }
            // reset primary interactor:
            if (primaryInteractor != null) {
                ResetPrimaryInteractor();
            }
        }

        #endregion

        #region ProcessInteractable

        public sealed override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase) {
            if (primaryInteractor != null && secondaryInteractor != null) {
                Transform pivotTransform = primaryInteractor.transform;
                Transform directionTransform = secondaryInteractor.transform;
                Vector3 handVector = directionTransform.position - pivotTransform.position; // pivot hand -> direction hand
                Vector3 upAxis = pivotTransform.up; // up axis to use for look rotation
                Quaternion interactorRotation = Quaternion.LookRotation(
                    handVector,
                    upAxis
                );
                primaryInteractor.GetAttachTransform(primaryInteractable).rotation = interactorRotation; // apply rotation
            }
            base.ProcessInteractable(updatePhase);
        }

        #endregion

        #region OnSelectEntered

        protected sealed override void OnSelectEntered(SelectEnterEventArgs args) {
            IXRSelectInteractor interactor = args.interactorObject;
            if (interactor != null && interactor is XRBaseInteractor baseInteractor) {
                if (primaryInteractor == null) PrimaryInteractor = baseInteractor;
                else if (primaryInteractor != baseInteractor) SecondaryInteractor = baseInteractor;
            }
            base.OnSelectEntered(args);
        }

        #endregion

        #region OnSelectExited

        protected sealed override void OnSelectExited(SelectExitEventArgs args) {
            IXRSelectInteractor interactor = args.interactorObject;
            if (interactor != null && interactor is XRBaseInteractor baseInteractor) {
                if (primaryInteractor == baseInteractor) ResetPrimaryInteractor();
                else if (primaryInteractor != baseInteractor) SecondaryInteractor = null;
            }
            base.OnSelectExited(args);
        }

        #endregion

        #region ResetPrimaryInteractor

        private void ResetPrimaryInteractor() {
            primaryInteractor.GetAttachTransform(this).localRotation = primaryInteractorAttachRotation;
            primaryInteractor = null;
        }

        #endregion

        #region IsSelectedBy

        public sealed override bool IsSelectableBy(IXRSelectInteractor interactor) {
            return interactor != null
                && interactor is XRBaseInteractor baseInteractor
                && (primaryInteractor == baseInteractor || secondaryInteractor == baseInteractor || base.IsSelectableBy(interactor));
        }

        #endregion

        #endregion

    }

}

#endif