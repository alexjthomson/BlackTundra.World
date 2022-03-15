using System.Collections.Generic;

using UnityEngine.XR.Interaction.Toolkit;

using XRRayInteractorBase = UnityEngine.XR.Interaction.Toolkit.XRRayInteractor;

namespace BlackTundra.World.XR {

    public sealed class XRRayInteractor : XRRayInteractorBase {

        #region variable

        public override bool CanSelect(IXRSelectInteractable interactable) {
            if (!base.CanSelect(interactable)) return false;
            // check if interactable is already being held by a ray interactor:
            List<IXRSelectInteractor> interactors = interactable.interactorsSelecting;
            int interactorCount = interactors.Count;
            if (interactorCount == 0) return true;
            for (int i = 0; i < interactorCount; i++) {
                if (interactors[i] is XRRayInteractor interactor && interactor != this) return false;
            }
            return true;
        }

        #endregion

    }

}