#if USE_XR_TOOLKIT

namespace BlackTundra.World.XR.Experimental.Locomotion {

    /// <summary>
    /// Interface that describes an object that implements movement locomotion.
    /// </summary>
    public interface IXRMovementProvider {

        void Update(in float deltaTime);

        void FixedUpdate(in float deltaTime);

    }

}

#endif