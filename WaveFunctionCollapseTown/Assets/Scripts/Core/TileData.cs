using System.Text;
using Didionysymus.WaveFunctionCollapse.Data;
using UnityEngine;

namespace Didionysymus.WaveFunctionCollapse.Core
{
    /// <summary>
    /// Defines the properties and behaviors for a tile used in the Wave Function Collapse system;
    /// the tile includes visual representation, rotation settings, socket configurations,
    /// and constraints for generation within a town-like structure
    /// </summary>
    [CreateAssetMenu(fileName = "New Tile", menuName = "WFC/Tile Data", order = 1)]
    public class TileData : ScriptableObject
    {
        [Header("Basic Properties")]
        public string TileName;
        public GameObject Prefab;
        
        [Tooltip("Probability Weight; higher values appear more frequently")]
        [Range(0.1f, 10f)] 
        public float Weight = 1.0f;
        
        [Header("Rotation Settings")]
        public bool AllowRotation = true;

        [Tooltip("Number of 90-degree rotation steps allowed (usually 4)")]
        [Range(1, 4)]
        public int RotationSteps = 4;

        [Header("Socket Configuration")]
        [Tooltip("Sockets for each face: [0] = North, [1] = East, [2] = South, [3] = West, [4] = Top, [5] = Bottom")]
        public TileSocket[] Sockets = new TileSocket[6];
        
        [Header("Town Properties")]
        public TileCategory Category;
        public bool IsCornerPiece = false;
        public bool RequiresRoadAccess = false;
        public Vector2Int TileSize = Vector2Int.one;
        
        [Header("Generation Constraints")]
        public int MaxInstancesPerTown = -1;
        public float MinDistanceFromCenter = 0f;
        public float MaxDistanceFromCenter = 100f;
        public bool CanPlaceAtBorder = true;

        /// <summary>
        /// Retrieves the TileSocket associated with a specified direction, considering a given rotation
        /// </summary>
        /// <param name="direction">The direction index for which the socket is required; valid values are typically in the range of 0 to 5, representing tile faces (e.g., North, East, South, West, Top, Bottom)</param>
        /// <param name="rotation">The rotation, in degrees (must be a multiple of 90), to apply when determining the rotated direction</param>
        /// <returns>The TileSocket for the specified direction after applying the rotation. Returns null if the direction is invalid or sockets are not defined</returns>
        public TileSocket GetSocketForDirection(int direction, int rotation)
        {
            // Exit case - there are no sockets or the direction is invalid
            if (Sockets == null || direction < 0 || direction >= 6)
            {
                StringBuilder debugBuilder = new StringBuilder();
                debugBuilder.Append("Invalid socket direction ");
                debugBuilder.Append(direction);
                debugBuilder.Append(" for tile ");
                debugBuilder.Append(name);
                
                Debug.LogError(debugBuilder.ToString());
                return null;
            }
            
            // Exit case - there are no rotations or vertical faces
            if (!AllowRotation || rotation == 0 || direction >= 4)
                return Sockets[direction];
            
            // Calculate the rotated direction (clockwise)
            int rotationSteps = rotation / 90;
            int rotatedDirection = (direction - rotationSteps + 4) % 4;

            return Sockets[rotatedDirection];
        }

        /// <summary>
        /// Validates the configuration of sockets for the tile, ensuring the socket array is properly initialized
        /// and has appropriate values for each direction. Logs errors if the configuration is invalid
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the socket configuration is valid; otherwise, returns <c>false</c>
        /// </returns>
        public bool ValidateSockets()
        {
            StringBuilder debugBuilder = new StringBuilder();
            
            if (Sockets is not { Length: 6 })
            {
                debugBuilder.Clear();
                debugBuilder.Append("Tile ");
                debugBuilder.Append(TileName);
                debugBuilder.Append(" has invalid socket array. Expected 6 sockets, but has ");
                debugBuilder.Append(Sockets?.Length ?? 0);
                debugBuilder.Append(" sockets.");
                
                Debug.LogError(debugBuilder.ToString());
                return false;
            }
            
            // Check horizontal faces (0-3)
            for (int i = 0; i < 4; i++)
            {
                if (Sockets[i] == null)
                {
                    debugBuilder.Clear();
                    debugBuilder.Append("Tile ");
                    debugBuilder.Append(TileName);
                    debugBuilder.Append(" has a missing socket for direction ");
                    debugBuilder.Append(i);
                    debugBuilder.Append("(0 = North, 1 = East, 2 = South, 3 = West)");
                    
                    Debug.LogError(debugBuilder.ToString());
                    return false;
                }
            }

            return true;
        }
    }
}