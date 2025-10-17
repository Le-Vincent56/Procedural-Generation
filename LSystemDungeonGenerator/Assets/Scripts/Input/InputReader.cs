using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Didionysymus.DungeonGeneration.Input
{
    [CreateAssetMenu(fileName = "Input Reader", menuName = "Input/Input Reader", order = 0)]
    public class InputReader : ScriptableObject, GameplayActions.IPlayerActions
    {
        public event Action<Vector2, bool> Move = delegate { };
        public event Action<bool> Interact = delegate { };
        public event Action<Vector2> Look = delegate { };

        private GameplayActions _inputActions;
        
        public int NormMoveX { get; private set; }
        public int NormMoveY { get; private set; }
        
        public int NormLookX { get; private set; }
        public int NormLookY { get; private set; }

        private void OnEnable() => Enable();
        private void OnDisable() => Disable();

        /// <summary>
        /// Enable the input actions
        /// </summary>
        public void Enable()
        {
            if (_inputActions == null)
            {
                _inputActions = new GameplayActions();
                _inputActions.Player.SetCallbacks(this);
            }

            _inputActions.Enable();
        }

        /// <summary>
        /// Disable the input actions
        /// </summary>
        public void Disable() => _inputActions.Disable();

        public void OnMove(InputAction.CallbackContext context)
        {
            Vector2 rawMovementInput = context.ReadValue<Vector2>();
            
            Move?.Invoke(rawMovementInput, context.started);
            
            NormMoveX = (int)(rawMovementInput * Vector2.right).normalized.x;
            NormMoveY = (int)(rawMovementInput * Vector2.up).normalized.y;
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            // Check the context phase
            switch (context.phase)
            {
                // If starting, invoke with true
                case InputActionPhase.Started:
                    Interact?.Invoke(true);
                    break;
                // If canceled, invoke with false
                case InputActionPhase.Canceled:
                    Interact?.Invoke(false);
                    break;
            }
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            Vector2 rawLookInput = context.ReadValue<Vector2>();
            
            Look?.Invoke(rawLookInput);
            
            NormLookX = (int)(rawLookInput * Vector2.right).normalized.x;
            NormLookY = (int)(rawLookInput * Vector2.up).normalized.y;
        }
    }
}
