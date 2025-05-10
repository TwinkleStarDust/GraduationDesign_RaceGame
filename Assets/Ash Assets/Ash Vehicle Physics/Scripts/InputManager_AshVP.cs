using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AshVP
{
    public class InputManager_AshVP : MonoBehaviour
    {
        public carController carController;

        // Axis values
        private float accelerationInput;
        private float steerInput;
        private float brakeInput;

        // Input Actions to be assigned via inspector or script
        public InputAction accelerateAction;
        public InputAction steerAction;
        public InputAction brakeAction;

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void Update()
        {
            // Get the inputs from input actions
            accelerationInput = accelerateAction.ReadValue<float>();
            steerInput = steerAction.ReadValue<float>();
            brakeInput = brakeAction.ReadValue<float>();

            // Provide the inputs to the car controller
            carController.ProvideInputs(accelerationInput, steerInput, brakeInput);
        }

        // Enable the input actions
        private void EnableInputActions()
        {
            if (accelerateAction != null) accelerateAction.Enable();
            if (steerAction != null) steerAction.Enable();
            if (brakeAction != null) brakeAction.Enable();
        }

        // Disable the input actions
        private void DisableInputActions()
        {
            if (accelerateAction != null) accelerateAction.Disable();
            if (steerAction != null) steerAction.Disable();
            if (brakeAction != null) brakeAction.Disable();
        }


        [ContextMenu("Add Default Keyboard Bindings")]
        public void AddDefaultKeyboardBindings()
        {
            // Add bindings for accelerate (W for positive, S for negative)
            if (accelerateAction != null)
            {
                accelerateAction.AddCompositeBinding("1DAxis")
                    .With("Positive", "<Keyboard>/w")
                    .With("Negative", "<Keyboard>/s");
            }

            // Add bindings for steer (D for positive, A for negative)
            if (steerAction != null)
            {
                steerAction.AddCompositeBinding("1DAxis")
                    .With("Positive", "<Keyboard>/d")
                    .With("Negative", "<Keyboard>/a");
            }

            // Add bindings for brake (Space for positive, no negative binding)
            if (brakeAction != null)
            {
                brakeAction.AddCompositeBinding("1DAxis")
                    .With("Positive", "<Keyboard>/space"); // Only positive input for braking
            }

            Debug.Log("Default keyboard bindings have been applied.");
        }

        [ContextMenu("Add Default Gamepad Bindings")]
        public void AddDefaultGamepadBindings()
        {
            // Add bindings for accelerate (Right Trigger for positive, Left Trigger for negative on Gamepad)
            if (accelerateAction != null)
            {
                accelerateAction.AddCompositeBinding("1DAxis")
                    .With("Positive", "<Gamepad>/rightTrigger") // Right trigger for acceleration
                    .With("Negative", "<Gamepad>/leftTrigger");  // Left trigger for reverse/braking
            }

            // Add bindings for steer (Left Stick X-axis on Gamepad)
            if (steerAction != null)
            {
                steerAction.AddCompositeBinding("1DAxis")
                    .With("Positive", "<Gamepad>/leftStick/right") // Right direction of left stick
                    .With("Negative", "<Gamepad>/leftStick/left"); // Left direction of left stick
            }

            // Add bindings for brake (B Button or South Button on Gamepad)
            if (brakeAction != null)
            {
                brakeAction.AddCompositeBinding("1DAxis")
                    .With("Positive", "<Gamepad>/buttonSouth"); // Button South (A or X, depending on controller)
            }

            Debug.Log("Default gamepad bindings have been applied.");
        }


    }
}
