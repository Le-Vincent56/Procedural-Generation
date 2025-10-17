using Didionysymus.DungeonGeneration.Environment;
using Didionysymus.DungeonGeneration.Input;
using UnityEngine;

namespace Didionysymus.DungeonGeneration.Player
{
    public class InteractionSystem : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputReader Input;
        
        [Header("Settings")]
        [SerializeField] private float InteractionRange = 3f;
        [SerializeField] private LayerMask InteractionLayers = -1;
        
        [Header("Camera")]
        [SerializeField] private Camera PlayerCamera;
        
        private IInteractable currentInteractable;
        private GameObject currentTarget;

        private void Awake()
        {
            if (PlayerCamera) return;
            
            PlayerCamera = GetComponentInChildren<Camera>();
            if (!PlayerCamera)
            {
                PlayerCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            Input.Interact += OnInteract;
        }

        private void OnDisable()
        {
            Input.Interact -= OnInteract;
        }

        private void Update() => CheckForInteractable();

        /// <summary>
        /// Performs a raycast from the player's camera to detect potential interactable objects
        /// within a specified interaction range and layer mask
        /// </summary>
        private void CheckForInteractable()
        {
            // Cast ray from the camera center
            Ray ray = new Ray(PlayerCamera.transform.position, PlayerCamera.transform.forward);
            
            if (Physics.Raycast(ray, out RaycastHit hit, InteractionRange, InteractionLayers))
            {
                // Check if hit object has IInteractable
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                
                if (interactable != null && interactable.CanInteract(gameObject))
                {
                    // Exit case - seeing the same interactable
                    if (interactable == currentInteractable) return;
                    
                    currentInteractable = interactable;
                    currentTarget = hit.collider.gameObject;
                    
                    Debug.Log($"Detecting Interactable: {currentTarget.gameObject.name}");
                }
                else
                {
                    // Hit something but it's not interactable
                    ClearInteractable();
                }
            }
            else
            {
                // Nothing in range
                ClearInteractable();
            }
        }

        /// <summary>
        /// Handles interaction input, allowing the player to interact with the currently targeted interactable object
        /// </summary>
        /// <param name="pressed">Indicates whether the interact button is being pressed</param>
        private void OnInteract(bool pressed)
        {
            // Exit case - the button is being released
            if (!pressed) return;

            // Exit case - no interactable is targeted
            if (currentInteractable == null || currentTarget == null) return;
            
            currentInteractable.Interact(gameObject);

            // Clear the interactable if the player cannot interact with it
            if (!currentInteractable.CanInteract(gameObject)) ClearInteractable();
        }

        /// <summary>
        /// Clears the currently targeted interactable object and resets associated references
        /// </summary>
        private void ClearInteractable()
        {
            currentInteractable = null;
            currentTarget = null;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!PlayerCamera) return;
                
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(PlayerCamera.transform.position, 
                PlayerCamera.transform.forward * InteractionRange);
        }
    }
}