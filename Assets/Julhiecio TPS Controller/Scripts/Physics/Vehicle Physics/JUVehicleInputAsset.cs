using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

namespace JUTPS.VehicleSystem.Inputs
{
    /// <summary>
    /// Used to stores all player inputs to control <see cref="JUWheeledVehicle"/>s.
    /// </summary>
    public class JUVehicleInputAsset : ScriptableObject
    {
        /// <summary>
        /// The throttle action, contains all inputs to accelerate the vehicle.
        /// </summary>
        public InputAction ThrottleAction;

        /// <summary>
        /// The steer action, constains all inputs to turn the vehicle to left or right.
        /// </summary>
        [Space]
        public InputAction SteerAction;

        /// <summary>
        /// The brake action, constains all input to brake the vehicle.
        /// </summary>
        [Space]
        public InputAction BrakeAction;

        /// <summary>
        /// The nitro action, constains all input to active vehicle nitro.
        /// </summary>
        [Space]
        public InputAction NitroAction;

        /// <summary>
        /// The throttle value, -1 to 1 from <see cref="ThrottleAction"/>.
        /// </summary>
        public float ThrottleAxis
        {
            get => ThrottleAction.ReadValue<float>();
        }

        /// <summary>
        /// The steer value, -1 to 1 from <see cref="SteerAction"/>.
        /// </summary>
        public float SteerAxis
        {
            get => SteerAction.ReadValue<float>();
        }

        /// <summary>
        /// The brake value, 0 to 1 from <see cref="BrakeAction"/>.
        /// </summary>
        public float BrakeAxis
        {
            get => Mathf.Clamp01(BrakeAction.ReadValue<float>());
        }

        /// <summary>
        /// Return true if <see cref="NitroAction"/> is pressed.
        /// </summary>
        public bool IsNitroPressed
        {
            get => NitroAction.inProgress;
        }

        /// <summary>
        /// Create an <see cref="ScriptableObject"/> that contains all inputs to control a <see cref="JUWheeledVehicle"/>.
        /// </summary>
        public JUVehicleInputAsset()
        {
            ThrottleAction = new InputAction("Throttle", InputActionType.Value);
            SteerAction = new InputAction("Steer", InputActionType.Value);
            BrakeAction = new InputAction("Brake", InputActionType.Value);
            NitroAction = new InputAction("Nitro", InputActionType.Value);
        }

        /// <summary>
        /// Set input actions as enabled or disabled.
        /// </summary>
        /// <param name="enabled"></param>
        public void SetInputEnabled(bool enabled)
        {
            switch (enabled)
            {
                case true:
                    ThrottleAction.Enable();
                    SteerAction.Enable();
                    BrakeAction.Enable();
                    NitroAction.Enable();
                    break;
                case false:
                    ThrottleAction.Disable();
                    SteerAction.Disable();
                    BrakeAction.Disable();
                    NitroAction.Disable();
                    break;
            }
        }

    }
}
