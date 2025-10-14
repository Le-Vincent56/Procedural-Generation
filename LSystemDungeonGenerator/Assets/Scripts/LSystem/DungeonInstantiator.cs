using System.Collections.Generic;
using UnityEngine;

namespace Didionysymus.DungeonGeneration.LSystem
{
    /// <summary>
    /// Handles the instantiation of dungeon prefabs in the scene
    /// </summary>
    public class DungeonInstantiator : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private GameObject FloorPrefab;
        [SerializeField] private GameObject WallPrefab;
        [SerializeField] private GameObject DoorPrefab;
        [SerializeField] private GameObject ArchPrefab;
        [SerializeField] private GameObject StairsPrefab;
        [SerializeField] private GameObject StairsSideCoverPrefab;
        
        [Header("Organization")]
        [SerializeField] private Transform Parent;

        private Transform _floorsParent;
        private Transform _wallsParent;
        private Transform _doorsParent;

        private DungeonConfig _config;
        private DungeonTurtle _turtle;

        /// <summary>
        /// Initializes the dungeon instantiator with the provided configuration and turtle instance
        /// </summary>
        /// <param name="config">The dungeon configuration object containing generation parameters</param>
        /// <param name="turtle">The dungeon turtle instance responsible for interpreting dungeon layouts</param>
        public void Initialize(DungeonConfig config, DungeonTurtle turtle)
        {
            _config = config;
            _turtle = turtle;
            SetupHierarchy();
        }

        /// <summary>
        /// Sets up the hierarchical structure for organizing different components of the dungeon within the scene
        /// </summary>
        /// <remarks>
        /// This involves creating a parent object if it does not exist, clearing any pre-existing children,
        /// and setting up dedicated child transforms for floors, walls, and doors
        /// </remarks>
        private void SetupHierarchy()
        {
            // Create the parent object if it doesn't exist
            if (Parent == null)
            {
                Parent = new GameObject("Dungeon").transform;
            }
            
            // Clear existing children
            foreach (Transform child in Parent)
            {
                DestroyImmediate(child.gameObject);
            }
            
            // Create organized parent objects
            _floorsParent = new GameObject("Floors").transform;
            _floorsParent.SetParent(Parent);
            _wallsParent = new GameObject("Walls").transform;
            _wallsParent.SetParent(Parent);
            _doorsParent = new GameObject("Doors").transform;
            _doorsParent.SetParent(Parent);
        }

        /// <summary>
        /// Instantiates the dungeon by placing floors, walls, and doors based on the provided grid and room data
        /// </summary>
        /// <param name="grid">A dictionary mapping grid positions to DungeonCell objects, representing the dungeon layout</param>
        /// <param name="rooms">A list of RoomData objects containing information about the rooms in the dungeon</param>
        public void InstantiateDungeon(Dictionary<Vector2Int, DungeonCell> grid, List<RoomData> rooms)
        {
            // First pass: place the floors
            foreach (KeyValuePair<Vector2Int, DungeonCell> kvp in grid)
            {
                // Extract the Dungeon Cell
                DungeonCell cell = kvp.Value;

                // Skip if the cell is not occupied
                if (!cell.IsOccupied) continue;

                // Place a floor at the cell
                PlaceFloor(cell);
            }
            
            // Compute wall flags from occupancy
            _turtle.BuildWalls();
            
            // Second pass: Place doors at room entrances
            foreach (RoomData room in rooms)
            {
                PlaceDoors(room, grid);
            }
            
            // Third pass: Place walls
            foreach (KeyValuePair<Vector2Int, DungeonCell> kvp in grid)
            {
                // Extract the Dungeon Cell
                DungeonCell cell = kvp.Value;
                
                // Skip if the cell is not occupied
                if (!cell.IsOccupied) continue;
                
                // Place a wall at the cell
                PlaceWalls(cell);
            }
            
            // Third pass: Place stairs
            
        }

        /// <summary>
        /// Places a floor at the specified DungeonCell's position based on its configuration and properties
        /// </summary>
        /// <param name="cell">The DungeonCell containing grid position, floor level, and other relevant attributes for determining the floor placement</param>
        private void PlaceFloor(DungeonCell cell)
        {
            // Exit case - a floor prefab does not exist
            if (FloorPrefab == null) return;
            
            // Get the world position of the cell
            Vector3 worldPosition = cell.GetWorldPosition(_config.CellSize, _config.FloorHeightUnits);

            // Instantiate the floor object
            GameObject floor = Instantiate(FloorPrefab, worldPosition, Quaternion.identity, _floorsParent);
            floor.name = $"Floor_{cell.GridPosition.x}_{cell.GridPosition.y}";
            
            // Scale to match cell size
            // Prefabs at scale (1,1,1) = 2 world units, so scale factor = CellSize / 2
            float scaleFactor = _config.CellSize / 2f;
            floor.transform.localScale = Vector3.one * scaleFactor;
        }

        /// <summary>
        /// Places walls around the specified DungeonCell based on its wall properties and orientation;
        /// handles the placement of each wall in the respective direction where a wall is required
        /// </summary>
        /// <param name="cell">The DungeonCell containing position, wall configuration, and other relevant properties</param>
        private void PlaceWalls(DungeonCell cell)
        {
            // Exit case - a wall prefab does not exist
            if (WallPrefab == null) return;
            
            Vector3 worldPosition = cell.GetWorldPosition(_config.CellSize, _config.FloorHeightUnits);
            
            if(cell.HasWallNorth) PlaceWall(worldPosition, Direction.North, cell);
            if(cell.HasWallSouth) PlaceWall(worldPosition, Direction.South, cell);
            if(cell.HasWallEast) PlaceWall(worldPosition, Direction.East, cell);
            if(cell.HasWallWest) PlaceWall(worldPosition, Direction.West, cell);
        }

        /// <summary>
        /// Places a wall at the specified position and orientation based on the cell's properties and the given direction;
        /// handles instantiation of the wall prefab, setting its position, rotation, scale, and parent in the scene hierarchy
        /// </summary>
        /// <param name="cellWorldPosition">The world position of the DungeonCell where the wall will be placed</param>
        /// <param name="direction">The direction in which the wall should be placed relative to the cell</param>
        /// <param name="cell">The DungeonCell containing grid position and other relevant wall information</param>
        private void PlaceWall(Vector3 cellWorldPosition, Direction direction, DungeonCell cell)
        {
            Vector3 wallPosition = cellWorldPosition;
            Quaternion wallRotation = Quaternion.identity;
            float halfCell = _config.CellSize * 0.5f;

            // Rotate the wall based on the direction
            switch (direction)
            {
                case Direction.North:
                    wallPosition += new Vector3(0, 0, halfCell);
                    wallRotation = Quaternion.Euler(0, 0, 0);
                    break;
                
                case Direction.South:
                    wallPosition += new Vector3(0, 0, -halfCell);
                    wallRotation = Quaternion.Euler(0, 180, 0);
                    break;
                    
                case Direction.East:
                    wallPosition += new Vector3(halfCell, 0, 0);
                    wallRotation = Quaternion.Euler(0, 90, 0);
                    break;
                    
                case Direction.West:
                    wallPosition += new Vector3(-halfCell, 0, 0);
                    wallRotation = Quaternion.Euler(0, -90, 0);
                    break;
            }
            
            // Instantiate the wall object
            GameObject wall = Instantiate(WallPrefab, wallPosition, wallRotation, _wallsParent);
            wall.name = $"Wall_{cell.GridPosition.x}_{cell.GridPosition.y}_{direction}";
            
            // Scale to match cell size
            // Prefabs at scale (1,1,1) = 2 world units, so scale factor = CellSize / 2
            // Keep walls thin by using reduced Z scale
            float scaleFactor = _config.CellSize / 2f;
            wall.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor * 0.5f);
        }

        /// <summary>
        /// Places doors and arches at the specified positions within the given room using the dungeon grid;
        /// handles the instantiation of door and arch prefabs, properly positioning and orienting them
        /// based on the room configuration; also removes walls in the neighboring grid cells where doors are created
        /// </summary>
        /// <param name="room">The room containing door position and direction information</param>
        /// <param name="grid">A dictionary representing the dungeon grid, where the key is the grid position
        /// and the value is a <c>DungeonCell</c> object containing information about the cell</param>
        private void PlaceDoors(RoomData room, Dictionary<Vector2Int, DungeonCell> grid)
        {
            // Exit case - the arch or door prefab does not exist
            if (ArchPrefab == null || DoorPrefab == null) return;

            for (int i = 0; i < room.DoorPositions.Count; i++)
            {
                // Get the door position and direction
                Vector2Int outsidePosition = room.DoorPositions[i];
                Direction doorDirection = room.DoorDirections[i];

                // Skip if the door position is not defined on the grid
                if (!grid.TryGetValue(outsidePosition, out DungeonCell outsideCell)) continue;

                Vector2Int insideOffset = doorDirection switch
                {
                    Direction.North => Vector2Int.up,
                    Direction.South => Vector2Int.down,
                    Direction.East => Vector2Int.right,
                    Direction.West => Vector2Int.left,
                    _ => Vector2Int.zero
                };

                Vector2Int insidePos = outsidePosition + insideOffset;
                
                // Exit case - the inside door position is not defined on the grid
                if (!grid.TryGetValue(insidePos, out DungeonCell insideCell)) continue;
                
                // Check if the connection is valid for a door
                bool okEdge = (outsideCell.Type == CellType.Corridor && insideCell.Type == CellType.Room) ||
                              (outsideCell.Type == CellType.Room && insideCell.Type == CellType.Corridor) ||
                              (outsideCell.Type == CellType.Room && insideCell.Type == CellType.Room);

                // Skip if the connection is not valid
                if (!okEdge) continue;
                
                // World midpoint on the shared edge
                Vector3 a = outsideCell.GetWorldPosition(_config.CellSize, _config.FloorHeightUnits);
                Vector3 b = insideCell.GetWorldPosition(_config.CellSize, _config.FloorHeightUnits);
                Vector3 midpoint = 0.5f * (a + b);

                Quaternion rotation = doorDirection switch
                {
                    Direction.North => Quaternion.Euler(0, 0, 0),
                    Direction.South => Quaternion.Euler(0, 180, 0),
                    Direction.East => Quaternion.Euler(0, 90, 0),
                    Direction.West => Quaternion.Euler(0, 270, 0),
                    _ => Quaternion.identity
                };
                
                // Scale factor for prefabs (at (1,1,1) = 2 world units)
                float scaleFactor = _config.CellSize / 2f;
                
                // Place the arch
                GameObject arch = Instantiate(ArchPrefab, midpoint, rotation, _doorsParent);
                arch.name = $"Arch_{outsidePosition.x}_{outsidePosition.y}";
                arch.transform.localScale = Vector3.one * scaleFactor;
                
                // Place door inside arch
                GameObject door = Instantiate(DoorPrefab, midpoint, rotation, _doorsParent);
                door.name = $"Door_{outsidePosition.x}_{outsidePosition.y}";
                door.transform.localScale = Vector3.one * scaleFactor;
                
                // Remove walls where the door is placed
                RemoveWallsForDoor(outsideCell, doorDirection, grid);
            }
        }

        /// <summary>
        /// Removes the walls in the specified direction for the given door cell and updates the neighboring cell accordingly;
        /// this creates a doorway in the dungeon grid
        /// </summary>
        /// <param name="doorCell">The DungeonCell where the door is being created</param>
        /// <param name="doorDirection">The direction in which the door is placed relative to the door cell</param>
        /// <param name="grid">A dictionary representing the dungeon grid, where the key is the grid position and the value
        /// is a <c>DungeonCell</c> object containing information about the cell</param>
        private void RemoveWallsForDoor(
            DungeonCell doorCell,
            Direction doorDirection,
            Dictionary<Vector2Int, DungeonCell> grid
        )
        {
            switch (doorDirection)
            {
                case Direction.North:
                    doorCell.HasWallNorth = false;
                    if (grid.TryGetValue(doorCell.GridPosition + Vector2Int.up, out DungeonCell cellNorth))
                    {
                        cellNorth.HasWallSouth = false;
                    }
                    break;
                
                case Direction.South:
                    doorCell.HasWallSouth = false;
                    if (grid.TryGetValue(doorCell.GridPosition + Vector2Int.down, out DungeonCell cellSouth))
                    {
                        cellSouth.HasWallNorth = false;   
                    }
                    break;
                
                case Direction.East:
                    doorCell.HasWallEast = false;
                    if (grid.TryGetValue(doorCell.GridPosition + Vector2Int.right, out DungeonCell cellEast))
                    {
                        cellEast.HasWallWest = false;
                    }
                    break;
                
                case Direction.West:
                    doorCell.HasWallWest = false;
                    if (grid.TryGetValue(doorCell.GridPosition + Vector2Int.left, out DungeonCell cellWest))
                    {
                        cellWest.HasWallEast = false;
                    }
                    break;
            }
        }

        /// <summary>
        /// Places a stair structure at the specified dungeon cell position based on the provided dungeon configuration
        /// </summary>
        /// <param name="cell">The dungeon cell where the stairs should be instantiated; includes information about grid position and floor level</param>
        private void PlaceStairs(DungeonCell cell)
        {
            // Exit case - the stair prefab does not exist
            if (StairsPrefab == null) return;

            // Get the world position of the cell
            Vector3 worldPosition = cell.GetWorldPosition(_config.CellSize, _config.FloorHeightUnits);
            
            // Stack segments vertically
            int segments = Mathf.RoundToInt(_config.FloorHeightCells);
            for (int i = 0; i < segments; i++)
            {
                Vector3 segmentPosition = worldPosition + new Vector3(0, i * _config.CellSize, 0);
                GameObject segment = Instantiate(StairsPrefab, segmentPosition, Quaternion.identity, Parent);
                segment.name = $"Stairs_{cell.GridPosition.x}_{cell.GridPosition.y}_seg{i}";
                segment.transform.localScale = Vector3.one * (_config.CellSize / 2f);
            }
        }

        /// <summary>
        /// Visually debugs the dungeon grid by drawing rays in the scene to represent the types and states of the cells
        /// </summary>
        /// <param name="grid">A dictionary representing the dungeon grid, where the key is the grid position and the value
        /// is a <c>DungeonCell</c> object containing information about the cell</param>
        public void DebugDrawGrid(Dictionary<Vector2Int, DungeonCell> grid)
        {
            foreach (KeyValuePair<Vector2Int, DungeonCell> kvp in grid)
            {
                // Extract the Dungeon Cell
                DungeonCell cell = kvp.Value;

                // Skip over un-occupied cells
                if (!cell.IsOccupied) continue;
                
                Vector3 worldPosition = cell.GetWorldPosition(_config.CellSize, _config.FloorHeightUnits);

                Color cellColor = cell.Type switch
                {
                    CellType.Room => Color.green,
                    CellType.Corridor => Color.blue,
                    CellType.Door => Color.yellow,
                    _ => Color.white
                };
                
                Debug.DrawRay(worldPosition, Vector3.up, cellColor, 10f);
            }
        }
    }
}