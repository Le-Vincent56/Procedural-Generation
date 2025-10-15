using UnityEngine;

namespace Didionysymus.DungeonGeneration.LSystem
{
    public enum CellType
    {
        Empty,
        Room,
        Corridor,
        Door
    }

    /// <summary>
    /// Represents an individual cell within a dungeon grid
    /// </summary>
    public class DungeonCell
    {
        public Vector2Int GridPosition { get; set; }
        public CellType Type { get; set; }
        public bool IsOccupied { get; set; }
        public int RoomID { get; set; }
        
        public bool HasWallNorth { get; set; }
        public bool HasWallSouth { get; set; }
        public bool HasWallEast { get; set; }
        public bool HasWallWest { get; set; }

        public DungeonCell(Vector2Int position)
        {
            GridPosition = position;
            Type = CellType.Empty;
            IsOccupied = false;
            RoomID = -1;
            
            HasWallNorth = true;
            HasWallSouth = true;
            HasWallEast = true;
            HasWallWest = true;
        }

        /// <summary>
        /// Calculates the world position of the dungeon cell based on its grid position, floor level, cell size, and floor height
        /// </summary>
        /// <param name="cellSize">The size of the individual cell in world units; default is 1</param>
        /// <param name="floorHeight">The height of each floor in world units; default is 3</param>
        /// <returns>The world position of the dungeon cell as a <see cref="Vector3"/></returns>
        public Vector3 GetWorldPosition(float cellSize = 1f, float floorHeight = 3f)
        {
            return new Vector3(
                GridPosition.x * cellSize, 
                floorHeight, 
                GridPosition.y * cellSize
            );
        }
    }
}
