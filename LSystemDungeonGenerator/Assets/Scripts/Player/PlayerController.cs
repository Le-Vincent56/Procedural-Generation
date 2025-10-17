using Didionysymus.DungeonGeneration.Input;
using UnityEngine;

namespace Didionysymus.DungeonGeneration.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputReader Input;
        
        [Header("Movement Settings")]
        [SerializeField] private float MoveSpeed = 5f;
        [SerializeField] private float Acceleration = 10f;
        [SerializeField] private float Deceleration = 10f;
        
        [Header("Mouse Look Settings")]
        [SerializeField] private float MouseSensitivity = 0.1f;
        [SerializeField] private float LookXLimit = 80f;
        [SerializeField] private Camera PlayerCamera;
        
        [Header("Physics Settings")]
        [SerializeField] private float GravityMultiplier = 2f;
        [SerializeField] private LayerMask GroundLayers = -1;

        private Rigidbody _rb;
        private Vector3 _moveVelocity;
        private float _rotationX = 0f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            // Configure the Rigidbody
            _rb.freezeRotation = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            // Find the camera if not serialized
            if (!PlayerCamera)
            {
                PlayerCamera = GetComponentInChildren<Camera>();
            }
            
            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleLook();
        }

        private void FixedUpdate()
        {
            HandleMovement();
            HandleGravity();
        }

        /// <summary>
        /// Handles the player's look functionality, including horizontal rotation of the player body
        /// and vertical rotation of the camera based on normalized input values and mouse sensitivity
        /// </summary>
        private void HandleLook()
        {
            // Exit case - cursor is not locked
            if(Cursor.lockState != CursorLockMode.Locked) return;
            
            // Apply horizontal rotation t othe body
            transform.Rotate(Vector3.up * (Input.NormLookX * MouseSensitivity));
            
            // Apply vertical rotation to the camera
            _rotationX -= Input.NormLookY * MouseSensitivity;
            _rotationX = Mathf.Clamp(_rotationX, -LookXLimit, LookXLimit);

            if (!PlayerCamera) return;

            PlayerCamera.transform.localRotation = Quaternion.Euler(_rotationX, 0f, 0f);
        }

        /// <summary>
        /// Handles the player's movement by calculating velocity based on input direction,
        /// movement speed, acceleration, and deceleration
        /// </summary>
        private void HandleMovement()
        {
            // Cache the fixed delta time
            float fixedDelta = Time.fixedDeltaTime;
            
            // Get movement directions
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            
            // Keep movement simple and horizontal
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            // Calculate the target velocity
            Vector3 inputDirection = (forward * Input.NormMoveY + right * Input.NormMoveX).normalized;
            Vector3 targetVelocity = inputDirection * MoveSpeed;
            
            _moveVelocity = inputDirection.magnitude > 0.01f ? 
                Vector3.Lerp(_moveVelocity, targetVelocity, Acceleration * fixedDelta) 
                : Vector3.Lerp(_moveVelocity, Vector3.zero, Deceleration * fixedDelta);
            
            // Apply movement
            _rb.linearVelocity = new Vector3(_moveVelocity.x, _rb.linearVelocity.y, _moveVelocity.z);
        }

        /// <summary>
        /// Applies additional gravity to the player by modifying the Rigidbody's velocity
        /// using a custom gravity multiplier
        /// </summary>
        private void HandleGravity()
        {
            // Apply custom gravity
            _rb.linearVelocity += Vector3.up * (Physics.gravity.y * GravityMultiplier * Time.fixedDeltaTime);
        }
    }
}
