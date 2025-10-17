using System.Collections.Generic;
using UnityEngine;

namespace Didionysymus.DungeonGeneration.LSystem
{
    public enum RoomType
    {
        Standard,
        Start,
        Boss,
        Treasure,
        Safe,
        Corridor
    }
    
    public enum Direction
    {
        North,
        South,
        East,
        West,
        Up,
        Down
    }
    
    public class RoomData
    {
        public int RoomID { get; set; }
        public RoomType Type { get; set; }
        public Vector2Int GridPosition { get; set; }
        public Vector2Int Size { get; set; }
        public int FloorLevel { get; set; }
        
        public List<Vector2Int> DoorPositions { get; set; }
        public List<Direction> DoorDirections { get; set; }
        
        public List<Vector2Int> OccupiedCells { get; set; }

        public RoomData(int id, RoomType type, Vector2Int position, Vector2Int size, int floor = 0)
        {
            RoomID = id;
            Type = type;
            GridPosition = position;
            Size = size;
            FloorLevel = floor;
            DoorPositions = new List<Vector2Int>();
            DoorDirections = new List<Direction>();
            OccupiedCells = new List<Vector2Int>();

            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    OccupiedCells.Add(position + new Vector2Int(x, z));
                }
            }
        }

        /// <summary>
        /// Computes the center position of the room in grid coordinates
        /// </summary>
        /// <returns>A Vector2Int representing the center position of the room in grid units</returns>
        public Vector2Int GetCenter() => GridPosition + Size / 2;

        /// <summary>
        /// Calculates the world center of the room in global coordinates
        /// </summary>
        /// <param name="cellSize">The size of each grid cell in world units</param>
        /// <param name="floorHeight">The height of each floor level in world units</param>
        /// <returns>The world position of the room's center</returns>
        public Vector3 GetWorldCenter(float cellSize = 1f, float floorHeight = 3f)
        {
            Vector2Int center = GetCenter();
            return new Vector3(
                center.x * cellSize,
               floorHeight, 
                center.y * cellSize
            );
        }


        /// <summary>
        /// Checks if the specified position is within the bounds of the room
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <returns>True if the position is within the room's bounds; otherwise, false</returns>
        public bool ContainsPosition(Vector2Int position)
        {
            return position.x >= GridPosition.x && position.x < GridPosition.x + Size.x && 
                   position.y >= GridPosition.y && position.y < GridPosition.y + Size.y;
        }
    }
}