namespace BlackTundra.World.XR.Experimental.Locomotion {

    public interface IXRMovementProvider {

        void Update(in float deltaTime);

        void FixedUpdate(in float deltaTime);

    }

}