using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Didionysymus.DungeonGeneration.Environment
{
    public interface IInteractable
    {
        void Interact(GameObject interactor);
        bool CanInteract(GameObject interactor);
    }
    
    public class Door : MonoBehaviour, IInteractable
    {
        [Header("Door Settings")]
        [SerializeField] private bool IsOpen = false;

        /// <summary>
        /// Attempts to interact with an object by invoking its interaction logic
        /// if the object can be interacted with and is not in an already interacted state.
        /// </summary>
        /// <param name="interactor">The GameObject attempting the interaction.</param>
        public void Interact(GameObject interactor)
        {
            // Exit case - cannot interact
            if (!CanInteract(interactor)) return;
            
            // Exit case - the door is already open
            if (IsOpen) return;
            
            OpenDoor();
        }

        /// <summary>
        /// Open the door by deactivating it
        /// </summary>
        private void OpenDoor() => gameObject.SetActive(false);

        /// <summary>
        /// Check if the player can interact with the door (always true)
        /// </summary>
        /// <param name="interactor"></param>
        /// <returns></returns>
        public bool CanInteract(GameObject interactor) => true;
    }
}
