#if USE_XR_TOOLKIT

namespace BlackTundra.World.XR.Experimental.Locomotion {

    /// <summary>
    /// Interface that describes an object that implements turning locomotion.
    /// </summary>
    public interface IXRTurnProvider {

        void Update(in float deltaTime);

    }

}

#endif