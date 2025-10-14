using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Didionysymus.DungeonGeneration.LSystem
{
    public class PropPlacer : MonoBehaviour
    {
        [Serializable]
        public class PropSet
        {
            public string Name;
            public List<GameObject> PropPrefabs;
            public RoomType TargetRoomType = RoomType.Standard;
            [Range(0f, 1f)] public float PlacemenDensity = 0.3f;
            public bool PlaceOnWalls = false;
            public bool PlaceInCenter = true;
        }
        
        [Header("Prop Sets")]
        [SerializeField] private List<PropSet> PropSets = new List<PropSet>();
        
        [Header("General Settings")]
        [SerializeField] private float WallOffset = 0.4f;
        [SerializeField] private float CenterSpacing = 0.8f;
        [SerializeField] private bool RandomRotation = true;
        
        [Header("References")]
        [SerializeField] private DungeonGenerator Generator;
        [SerializeField] private Transform Parent;

        private Random _random;

        private void Start()
        {
            // Get the generator component if it does not exist
            if(Generator == null) 
            {
                Generator = GetComponent<DungeonGenerator>();
            }
        }

        /// <summary>
        /// Places props across all rooms in the dungeon based on the current dungeon generator's configuration
        /// </summary>
        [ContextMenu("Place Props")]
        public void PlaceProps()
        {
            // Exit case - no DungeonGenerator component
            if (Generator == null)
            {
                Debug.LogError("DungeonGenerator component not found");
                return;
            }

            List<RoomData> rooms = Generator.Rooms;
            DungeonConfig config = Generator.Configuration;

            // Exit case - no rooms found
            if (rooms == null || rooms.Count == 0)
            {
                Debug.LogWarning("No rooms found. Generate a dungeon first!");
                return;
            }
            
            // Setup parent if it does not exist
            if(Parent == null) 
            {
                Parent = new GameObject("Props").transform;
            }
            
            // Clear existing props
            foreach (Transform child in Parent)
            {
                Destroy(child.gameObject);
            }
            
            // Initialize random with same seed as dungeon
            int seed = config.Seed == 0 
                ? UnityEngine.Random.Range(1, 100000) 
                : config.Seed;
            _random = new Random(seed + 1000);
            
            // Place props in each room
            foreach (RoomData room in rooms)
            {
                PlacePropsInRoom(room, config);
            }

            // If debugging, print number of props placed
            if (config.Debug)
            {
                Debug.Log($"Placed {Parent.childCount} props in {rooms.Count} rooms");
            }
        }

        /// <summary>
        /// Places props in a specific room based on the provided room data and dungeon configuration
        /// </summary>
        /// <param name="room">The data representing the room in which props are to be placed, including its type and layout</param>
        /// <param name="config">The configuration object containing global dungeon settings and parameters for prop placement</param>
        private void PlacePropsInRoom(RoomData room, DungeonConfig config)
        {
            // Find matching prop sets for this room type
            List<PropSet> matchingSets = PropSets.FindAll(set =>
                set.TargetRoomType == room.Type || set.TargetRoomType == RoomType.Standard);

            // Exit case - no matching sets for this room type found
            if (matchingSets.Count == 0) return;
            
            // Use first matching set
            PropSet propSet = matchingSets[0];

            // Exit case - no prop prefabs found for this set
            if (propSet.PropPrefabs == null || propSet.PropPrefabs.Count == 0) return;
            
            // Place props
            if (propSet.PlaceOnWalls) PlacePropsOnWalls(room, propSet, config);
            if(propSet.PlaceInCenter) PlacePropsInCenter(room, propSet, config);
        }

        /// <summary>
        /// Places props on the walls of a room based on the given prop set and dungeon configuration
        /// </summary>
        /// <param name="room">The room data containing details about the room where props are to be placed</param>
        /// <param name="propSet">The collection of props and their placement settings to be applied within the room</param>
        /// <param name="config">The dungeon configuration that defines global dungeon settings and parameters for placement</param>
        private void PlacePropsOnWalls(RoomData room, PropSet propSet, DungeonConfig config)
        {
            Dictionary<Vector2Int, DungeonCell> grid = Generator.Grid;
            
            // Iterate through room perimeter cells
            for (int x = 0; x < room.Size.x; x++)
            {
                for (int z = 0; z < room.Size.y; z++)
                {
                    Vector2Int cellPosition = room.GridPosition + new Vector2Int(x, z);
                    
                    // Check if this is a perimeter cell
                    bool isPerimeter = x == 0 || x == room.Size.x - 1 || 
                                       z == 0 || z == room.Size.y - 1;

                    // Skip if this is not a perimeter cell
                    if (!isPerimeter) continue;
                    
                    // Skip if the random roll fails
                    if (_random.NextDouble() > propSet.PlacemenDensity) continue;

                    // Get the DungeonCell at the position
                    DungeonCell cell = grid[cellPosition];
                    
                    // Find which wall to place against
                    if (cell.HasWallNorth && z == room.Size.y - 1)
                    {
                        PlacePropAtWall(cellPosition, Direction.North, propSet, config);
                    } 
                    else if (cell.HasWallSouth && z == 0)
                    {
                        PlacePropAtWall(cellPosition, Direction.South, propSet, config);
                    } 
                    else if (cell.HasWallEast && x == room.Size.x - 1)
                    {
                        PlacePropAtWall(cellPosition, Direction.East, propSet, config);
                    } 
                    else if (cell.HasWallWest && x == 0)
                    {
                        PlacePropAtWall(cellPosition, Direction.West, propSet, config);
                    }
                } 
            }
        }

        /// <summary>
        /// Places a prop at a specific wall edge of the given cell position based on the wall direction
        /// </summary>
        /// <param name="cellPosition">The position of the cell in the grid where the prop is to be placed.</param>
        /// <param name="wallDirection">The direction of the wall where the prop will be placed (e.g., North, South, East, West)</param>
        /// <param name="propSet">The collection of props and associated settings specifying which props are valid for placement</param>
        /// <param name="config">The dungeon configuration defining physical dimensions and global dungeon settings.</param>
        private void PlacePropAtWall(Vector2Int cellPosition, Direction wallDirection, PropSet propSet,
            DungeonConfig config)
        {
            // Calculate the prop position and rotation
            Vector3 cellWorldPosition = new Vector3(
                cellPosition.x * config.CellSize, 
                0, 
                cellPosition.y * config.CellSize
            );
            Vector3 propPosition = cellWorldPosition;
            Quaternion propRotation = Quaternion.identity;
            
            // Offset from wall and set rotation
            switch (wallDirection)
            {
                case Direction.North:
                    propPosition += new Vector3(0, 0, config.CellSize * 0.5f - WallOffset);
                    propRotation = Quaternion.Euler(0, 180, 0);
                    break;
                case Direction.South:
                    propPosition += new Vector3(0, 0, -config.CellSize * 0.5f + WallOffset);
                    propRotation = Quaternion.Euler(0, 0, 0);
                    break;
                case Direction.East:
                    propPosition += new Vector3(config.CellSize * 0.5f - WallOffset, 0, 0);
                    propRotation = Quaternion.Euler(0, 270, 0);
                    break;
                case Direction.West:
                    propPosition += new Vector3(-config.CellSize * 0.5f + WallOffset, 0, 0);
                    propRotation = Quaternion.Euler(0, 90, 0);
                    break;
            }
            
            // Select a random prop
            GameObject propPrefab = propSet.PropPrefabs[_random.Next(propSet.PropPrefabs.Count)];
            
            // Instantiate the prop
            GameObject prop = Instantiate(propPrefab, propPosition, propRotation, Parent);
            prop.name = $"{propSet.Name}_{cellPosition.x}_{cellPosition.y}";
        }

        /// <summary>
        /// Places props within the central area of a given room, avoiding the perimeter walls
        /// </summary>
        /// <param name="room">The room data object defining the properties of the room where props are being placed</param>
        /// <param name="propSet">The set of properties and prefabs used for prop placement</param>
        /// <param name="config">The dungeon configuration providing global settings for the dungeon generation process</param>
        private void PlacePropsInCenter(RoomData room, PropSet propSet, DungeonConfig config)
        {
            // Exit case - the room is smaller than 3x3
            if (room.Size.x <= 3 || room.Size.y <= 3) return;

            // Calculate how many props to palce based on room size and placement density
            int maxProps = Mathf.FloorToInt(room.Size.x * room.Size.y * propSet.PlacemenDensity * 0.1f);
            maxProps = Mathf.Clamp(maxProps, 0, 5);
            
            for (int i = 0; i < maxProps; i++)
            {
                // Random position within center area (avoiding perimeter)
                float xOffset = ((float)_random.NextDouble() - 0.5f) * (room.Size.x - 2) * config.CellSize;
                float zOffset = ((float)_random.NextDouble() - 0.5f) * (room.Size.y - 2) * config.CellSize;
                
                Vector3 propPosition = room.GetWorldCenter(config.CellSize, config.FloorHeightUnits);
                propPosition += new Vector3(xOffset, 0, zOffset);
                
                // Random rotation if enabled
                Quaternion propRotation = RandomRotation 
                    ? Quaternion.Euler(0, (float)_random.NextDouble() * 360f, 0)
                    : Quaternion.identity;
                
                // Select a random prop
                GameObject propPrefab = propSet.PropPrefabs[_random.Next(propSet.PropPrefabs.Count)];
                
                // Instantiate the prop
                GameObject prop = Instantiate(propPrefab, propPosition, propRotation, Parent);
                prop.name = $"Prop_Center_{propPrefab.name}_{i}";
            }
        }

        /// <summary>
        /// Clears all existing props from the dungeon by removing child objects from the assigned parent transform
        /// </summary>
        [ContextMenu("Clear Props")]
        public void ClearProps()
        {
            // Exit case - no Parent is assigned
            if (Parent == null) return;
            
            // Destroy each child object
            foreach (Transform child in Parent)
            {
                Destroy(child.gameObject);
            }
        }
    }
}