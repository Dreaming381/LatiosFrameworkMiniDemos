using UnityEngine;
using UnityEngine.InputSystem;

namespace Testing
{
    public class FreeCameraController : MonoBehaviour
    {
        [Header("Input")]
        public InputActionAsset inputActionAsset;

        [Header("Movement Settings")]
        public float moveSpeed = 10f;
        public float rollSpeed = 45f;
        public float mouseSensitivity = 0.1f;

        private InputAction moveForwardBackAction;
        private InputAction moveStrafeAction;
        private InputAction moveUpDownAction;
        private InputAction rollAction;
        private InputAction lookAction;

        private float pitch = 0f;
        private float yaw = 0f;
        private float roll = 0f;

        void OnEnable()
        {
            if (inputActionAsset == null)
            {
                Debug.LogError("InputActionAsset is not assigned on FreeCameraController!");
                return;
            }

            var actionMap = inputActionAsset.FindActionMap("FreeCamera");
            if (actionMap == null)
            {
                Debug.LogError("FreeCamera action map not found in InputActionAsset!");
                return;
            }

            moveForwardBackAction = actionMap.FindAction("MoveForwardBack");
            moveStrafeAction = actionMap.FindAction("MoveStrafe");
            moveUpDownAction = actionMap.FindAction("MoveUpDown");
            rollAction = actionMap.FindAction("Roll");
            lookAction = actionMap.FindAction("Look");

            actionMap.Enable();
        }

        void OnDisable()
        {
            if (inputActionAsset != null)
            {
                var actionMap = inputActionAsset.FindActionMap("FreeCamera");
                actionMap?.Disable();
            }
        }

        void Update()
        {
            if (inputActionAsset == null) return;

            HandleMovement();
            HandleRotation();
        }

        void HandleMovement()
        {
            Vector3 movement = Vector3.zero;

            // Forward/Back (W/S)
            if (moveForwardBackAction != null)
            {
                float forwardBack = moveForwardBackAction.ReadValue<float>();
                movement += transform.forward * forwardBack;
            }

            // Strafe (A/D)
            if (moveStrafeAction != null)
            {
                float strafe = moveStrafeAction.ReadValue<float>();
                movement += transform.right * strafe;
            }

            // Up/Down (Space/C)
            if (moveUpDownAction != null)
            {
                float upDown = moveUpDownAction.ReadValue<float>();
                movement += transform.up * upDown;
            }

            transform.position += movement * moveSpeed * Time.deltaTime;
        }

        void HandleRotation()
        {
            // Mouse look
            if (lookAction != null)
            {
                Vector2 lookDelta = lookAction.ReadValue<Vector2>();
                yaw += lookDelta.x * mouseSensitivity;
                pitch -= lookDelta.y * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, -89f, 89f);
            }

            // Roll (Q/E)
            if (rollAction != null)
            {
                float rollInput = rollAction.ReadValue<float>();
                roll += rollInput * rollSpeed * Time.deltaTime;
            }

            transform.rotation = Quaternion.Euler(pitch, yaw, -roll);
        }
    }
}
