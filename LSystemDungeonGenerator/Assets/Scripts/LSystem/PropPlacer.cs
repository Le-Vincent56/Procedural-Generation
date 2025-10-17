using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

namespace Didionysymus.DungeonGeneration.LSystem
{
    /// <summary>
    /// The PropPlacer class is responsible for placing props in dungeon rooms generated
    /// by the DungeonGenerator; it provides functionality to clear and populate props
    /// in rooms based on configuration settings, such as room types and densities
    /// </summary>
    public class PropPlacer : MonoBehaviour
    {
        [Header("Torch Settings")] 
        [SerializeField] private TorchSettings TorchConfig = new TorchSettings();

        [Header("General Props")] [SerializeField]
        private PropSet GeneralProps = new PropSet()
        {
            Name = "General",
            PlacementDensity = 0.2f,
            MinSpacing = 1.5f
        };

        [Header("Special Props")]
        [SerializeField] private PropSet StartRoomProps = new PropSet() 
        { 
            Name = "Start",
            TargetRoomType = RoomType.Start,
            PlacementDensity = 0.1f,
            PlaceInCenter = true,
            MinSpacing = 1f
        };
        
        [SerializeField] private PropSet BossRoomProps = new PropSet()
        {
            Name = "Boss Room",
            TargetRoomType = RoomType.Boss,
            PlacementDensity = 0.4f,
            PlaceInCenter = true,
            MinSpacing = 2f
        };
        
        [SerializeField] private PropSet TreasureRoomProps = new PropSet() 
        { 
            Name = "Treasure",
            TargetRoomType = RoomType.Treasure,
            PlacementDensity = 0.6f,
            PlaceInCorners = true,
            MinSpacing = 1f
        };
        
        [SerializeField] private PropSet SafeRoomProps = new PropSet() 
        { 
            Name = "Safe",
            TargetRoomType = RoomType.Safe,
            PlacementDensity = 0.3f,
            MinSpacing = 2f
        };
        
        [Header("General Settings")]
        [SerializeField] private float WallOffset = 0.4f;
        [SerializeField] private float MinDistanceFromDoors = 2.5f;
        [SerializeField] private bool AvoidDoorways = true;

        private Transform _dungeonParent;
        private Transform _propsParent;
        private Transform _torchesParent;
        private DungeonGenerator _generator;
        private Random _random;
        private List<Vector3> _placedPropPositions = new List<Vector3>();
        private List<Vector3> _placedTorchPositions = new List<Vector3>();

        public void Initialize(DungeonGenerator generator, Transform dungeonParent)
        {
            _generator = generator;
            _dungeonParent = dungeonParent;
        }

        /// <summary>
        /// Places props, such as torches and other decorative or functional items, in all rooms and corridors of the dungeon
        /// </summary>
        public void PlaceProps()
        {
            if (!_generator)
            {
                Debug.LogError("DungeonGenerator component not found");
                return;
            }
            
            List<RoomData> rooms = _generator.Rooms;
            DungeonConfig config = _generator.Configuration;

            if (rooms == null || rooms.Count == 0)
            {
                Debug.LogWarning("No rooms found. Generate a dungeon first!");
                return;
            }

            SetupHierarchy();
            
            // Initialize random with the same seed as the dungeon
            int seed = config.Seed == 0
                ? UnityEngine.Random.Range(1, 100000)
                : config.Seed;
            _random = new Random(seed + 1000);
            
            // Clear tracking lists
            _placedPropPositions.Clear();
            _placedTorchPositions.Clear();
            
            // Place torches in all rooms and corridors
            PlaceTorches(rooms, config);

            // Place general props in all rooms
            foreach (RoomData room in rooms)
            {
                // Skip if it's a corridor
                if (room.Type == RoomType.Corridor) continue;

                PlaceGeneralProps(room, config);
            }
            
            // Place special props in special rooms
            foreach (RoomData room in rooms)
            {
                PlaceSpecialProps(room, config);
            }

            // Debug
            if (config.Debug)
            {
                Debug.Log($"Placed {_propsParent.childCount} props and " +
                          $"{_torchesParent.childCount} torches in {rooms.Count} rooms"
                );
            }
        }

        /// <summary>
        /// Sets up the hierarchy for prop and torch placement by organizing parent objects;
        /// clears any existing props and torches from the hierarchy to prepare for new placement;
        /// ensures dedicated parent objects exist for both props and torches under the dungeon parent.
        /// </summary>
        private void SetupHierarchy()
        {
            // Create the props parent if it doesn't exist
            if (!_propsParent)
            {
                GameObject props = new GameObject("Props");
                props.transform.position = new Vector3(0f, 0f, 0f);
                _propsParent = props.transform;
                props.transform.SetParent(_dungeonParent);
            }

            // Create the torches parent if it doesn't exist
            if (!_torchesParent)
            {
                GameObject torches = new GameObject("Torches");
                torches.transform.position = new Vector3(0f, 0f, 0f);
                _torchesParent = torches.transform;
                torches.transform.SetParent(_dungeonParent);
            }
            
            // Clear existing props
            foreach (Transform child in _propsParent)
            {
                DestroyImmediate(child.gameObject);
            }

            // Clear existing torches
            foreach (Transform child in _torchesParent)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        /// <summary>
        /// Places torches within specified rooms and corridors based on the dungeon configuration
        /// and torch settings
        /// </summary>
        /// <param name="rooms">A list of rooms in the dungeon where torches may be placed</param>
        /// <param name="config">The dungeon configuration containing settings such as seed,
        /// room types, and placement preferences</param>
        private void PlaceTorches(List<RoomData> rooms, DungeonConfig config)
        {
            // Skip if the config doesn't contain a torch prefab
            if (!TorchConfig.TorchPrefab) return;

            // Iterate through each room
            foreach (RoomData room in rooms)
            {
                // Check if torches should be placed in the room/corridor
                bool shouldPlace = (room.Type == RoomType.Corridor && TorchConfig.PlaceInCorridors) ||
                                   (room.Type != RoomType.Corridor && TorchConfig.PlaceInRooms);

                if (!shouldPlace) continue;

                List<WallSegment> wallSegments = GetWallSegments(room, config);

                // Iterate through each wall segment and place torches
                foreach (WallSegment segment in wallSegments)
                {
                    // Calculate segment length
                    float segmentLength = segment.Length * config.CellSize;

                    // Determine spacing
                    float spacing = UnityEngine.Random.Range(TorchConfig.MinSpacing, TorchConfig.MaxSpacing);
                    int torchCount = Mathf.FloorToInt(segmentLength / spacing);
                    
                    // Skip if there are no torches to place
                    if (torchCount <= 0) return;
                    
                    // Calculate the spacing of the torches
                    float actualSpacing = segmentLength / (torchCount + 1);

                    // Distribute torches evenly along the segment
                    for (int i = 1; i <= torchCount; i++)
                    {
                        // Get the current distance
                        float currentDistance = actualSpacing * i;
                        
                        // Calculate the position along the segment
                        float t = currentDistance / segmentLength;

                        Vector3 position = Vector3.Lerp(segment.StartWorld, segment.EndWorld, t);

                        // Skip if the position is too close to a door
                        if (AvoidDoorways && IsNearDoor(position, room, config))
                            continue;

                        // Check if it's a corner position and potentially skip
                        if (IsCornerPosition(position, room, config) &&
                            _random.NextDouble() < TorchConfig.CornerSkipChance)
                            continue;

                        // Skip if it is too close to other torches
                        if (IsTooCloseToOtherTorches(position))
                            continue;

                        // Calculate the final position with offsets
                        Vector3 wallOffset = GetWallOffsetVector(segment.Direction) * TorchConfig.WallOffset;
                        
                        // Need to position wall-mounted torches correctly based on direction
                        Vector3 finalPosition = position;
        
                        // Adjust position to be on the wall
                        switch (segment.Direction)
                        {
                            case Direction.North:
                                finalPosition.z = position.z + (config.CellSize * 0.5f) - TorchConfig.WallOffset;
                                break;
                            case Direction.South:
                                finalPosition.z = position.z - (config.CellSize * 0.5f) + TorchConfig.WallOffset;
                                break;
                            case Direction.East:
                                finalPosition.x = position.x + (config.CellSize * 0.5f) - TorchConfig.WallOffset;
                                break;
                            case Direction.West:
                                finalPosition.x = position.x - (config.CellSize * 0.5f) + TorchConfig.WallOffset;
                                break;
                        }
                        
                        // Adjust the final height
                        finalPosition.y += TorchConfig.HeightOffset;

                        // Place the torch
                        Quaternion rotation = GetWallRotation(segment.Direction);
                        GameObject torch = Instantiate(TorchConfig.TorchPrefab, finalPosition, rotation, _torchesParent);
                        torch.name = $"Torch_{room.RoomID}_{segment.Direction}_{i}";

                        _placedTorchPositions.Add(position);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the wall segments for a given room based on its position, size,
        /// and dungeon configuration.
        /// </summary>
        /// <param name="room">The data representing the room for which wall segments will be calculated</param>
        /// <param name="config">The dungeon configuration, including cell size and floor height, used to determine wall segment placement</param>
        /// <returns>A list of wall segments representing the start and end positions, direction, and length of each wall in the room</returns>
        private List<WallSegment> GetWallSegments(RoomData room, DungeonConfig config)
        {
            List<WallSegment> segments = new List<WallSegment>();
            
            // Get actual wall positions for the room
            float halfCell = config.CellSize * 0.5f;
            
            // North wall
            segments.Add(new WallSegment
            {
                StartWorld = new Vector3(room.GridPosition.x * config.CellSize, config.FloorHeightUnits, 
                    (room.GridPosition.y + room.Size.y - 1) * config.CellSize),
                EndWorld = new Vector3((room.GridPosition.x + room.Size.x) * config.CellSize, config.FloorHeightUnits, 
                    (room.GridPosition.y + room.Size.y - 1) * config.CellSize),
                Direction = Direction.North,
                Length = room.Size.x
            });
            
            // South wall
            segments.Add(new WallSegment
            {
                StartWorld = new Vector3(room.GridPosition.x * config.CellSize, config.FloorHeightUnits, 
                    room.GridPosition.y * config.CellSize),
                EndWorld = new Vector3((room.GridPosition.x + room.Size.x) * config.CellSize, config.FloorHeightUnits, 
                    room.GridPosition.y * config.CellSize),
                Direction = Direction.South,
                Length = room.Size.x
            });
            
            // East wall
            segments.Add(new WallSegment
            {
                StartWorld = new Vector3((room.GridPosition.x + room.Size.x - 1) * config.CellSize, config.FloorHeightUnits, 
                    room.GridPosition.y * config.CellSize),
                EndWorld = new Vector3((room.GridPosition.x + room.Size.x - 1) * config.CellSize, config.FloorHeightUnits, 
                    (room.GridPosition.y + room.Size.y) * config.CellSize),
                Direction = Direction.East,
                Length = room.Size.y
            });
            
            // West wall
            segments.Add(new WallSegment
            {
                StartWorld = new Vector3(room.GridPosition.x * config.CellSize, config.FloorHeightUnits, 
                    room.GridPosition.y * config.CellSize),
                EndWorld = new Vector3(room.GridPosition.x * config.CellSize, config.FloorHeightUnits, 
                    (room.GridPosition.y + room.Size.y) * config.CellSize),
                Direction = Direction.West,
                Length = room.Size.y
            });
            
            return segments;
        }

        /// <summary>
        /// Places general props, such as decorations or functional items, within a specified room of the dungeon using predefined configuration settings
        /// </summary>
        /// <param name="room">The room where the general props will be placed</param>
        /// <param name="config">Configuration settings that define how and where props are to be placed within the room</param>
        private void PlaceGeneralProps(RoomData room, DungeonConfig config)
        {
            // Exit case - no prop prefabs are assigned
            if (GeneralProps.PropPrefabs == null || GeneralProps.PropPrefabs.Count == 0) return;
            
            PlacePropSet(GeneralProps, room, config, "General");
        }

        /// <summary>
        /// Places special props in a room based on the room type and configuration parameters
        /// </summary>
        /// <param name="room">The room data where the props should be placed. Includes information such as the room type</param>
        /// <param name="config">The dungeon configuration providing relevant settings for prop placement</param>
        private void PlaceSpecialProps(RoomData room, DungeonConfig config)
        {
            PropSet propSet = null;

            switch (room.Type)
            {
                case RoomType.Boss:
                    propSet = BossRoomProps;
                    break;
                case RoomType.Treasure:
                    propSet = TreasureRoomProps;
                    break;
                case RoomType.Start:
                    propSet = StartRoomProps;
                    break;
                case RoomType.Safe:
                    propSet = SafeRoomProps;
                    break;
            }
            
            // Exit case - no prop prefabs were found
            if(propSet == null || propSet.PropPrefabs == null || propSet.PropPrefabs.Count == 0) return;
            
            PlacePropSet(propSet, room, config, $"Special_{room.Type}");
        }

        /// <summary>
        /// Places a set of props within a given room based on the configuration, placement density, and specified prefix for identification
        /// </summary>
        /// <param name="propSet">The set of props to be placed, including prefabs and placement density</param>
        /// <param name="room">The room where the props will be placed</param>
        /// <param name="config">The configuration settings for the dungeon, which may include constraints for prop placement</param>
        /// <param name="prefix">A string used as a prefix for identifying the props placed within the room</param>
        private void PlacePropSet(PropSet propSet, RoomData room, DungeonConfig config, string prefix)
        {
            List<Vector3> potentialPositions = GeneratePotentialPositions(propSet, room, config);

            // Shuffle positions
            for (int i = potentialPositions.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (potentialPositions[i], potentialPositions[j]) = (potentialPositions[j], potentialPositions[i]);
            }
            
            // Calculate how many props to place
            int maxProps = Mathf.CeilToInt(potentialPositions.Count * propSet.PlacementDensity);
            int placedCount = 0;
            
            foreach (Vector3 position in potentialPositions)
            {
                if (placedCount >= maxProps) break;
                
                // Check minimum spacing
                if (IsTooCloseToOtherProps(position, propSet.MinSpacing))
                    continue;
                
                // Check door proximity
                if (AvoidDoorways && IsNearDoor(position, room, config))
                    continue;
                
                // Select and place prop
                GameObject propPrefab = propSet.PropPrefabs[_random.Next(propSet.PropPrefabs.Count)];
                float randomRotation = _random.Next(0, 4) * 90f;
                
                GameObject prop = Instantiate(propPrefab, position, 
                    Quaternion.Euler(0, randomRotation, 0), _propsParent);
                prop.name = $"{prefix}_{propPrefab.name}_{room.RoomID}_{placedCount}";
                
                _placedPropPositions.Add(position);
                placedCount++;
            }
        }

        /// <summary>
        /// Generates a list of potential positions for placing props within a room based on the given configuration and prop set rules
        /// </summary>
        /// <param name="propSet">The set of properties defining the placement rules, such as whether to place on walls, in corners, or at the center</param>
        /// <param name="room">The room data specifying the size, grid position, and occupied cells of the room where props are to be placed</param>
        /// <param name="config">The dungeon configuration containing details such as the size of cells and the height of floors</param>
        /// <returns>A list of <c>Vector3</c> positions representing potential locations for props within the specified room</returns>
        private List<Vector3> GeneratePotentialPositions(PropSet propSet, RoomData room, DungeonConfig config)
        {
            // Create a list to store positions
            List<Vector3> positions = new List<Vector3>();
            
            // Generate center positions
            if (propSet.PlaceInCenter)
            {
                // Don't place too close to the walls (1-cell buffer)
                for (int x = 1; x < room.Size.x - 1; x++)
                {
                    for (int z = 1; z < room.Size.y - 1; z++)
                    {
                        // Store the position
                        Vector3 worldPos = new Vector3(
                            (room.GridPosition.x + x) * config.CellSize,
                            config.FloorHeightUnits,
                            (room.GridPosition.y + z) * config.CellSize
                        );
                        
                        positions.Add(worldPos);
                    }
                }
            }
            
            // Generate wall positions
            if (propSet.PlaceOnWalls)
            {
                foreach (Vector2Int cellPos in room.OccupiedCells)
                {
                    bool isPerimeter = IsPerimeterCell(cellPos, room);
                    if (!isPerimeter) continue;
                    
                    DungeonCell cell = _generator.Grid[cellPos];
                    List<Direction> wallDirs = GetWallDirections(cell);
                    
                    foreach (Direction dir in wallDirs)
                    {
                        Vector3 worldPos = new Vector3(
                            cellPos.x * config.CellSize,
                            config.FloorHeightUnits,
                            cellPos.y * config.CellSize
                        );
                        Vector3 offset = GetWallOffsetVector(dir) * (config.CellSize * 0.5f - WallOffset);
                        positions.Add(worldPos + offset);
                    }
                }
            }
            
            // Generate corner positions
            if (propSet.PlaceInCorners)
            {
                Vector3[] corners = new Vector3[]
                {
                    new Vector3(room.GridPosition.x * config.CellSize, config.FloorHeightUnits, 
                        room.GridPosition.y * config.CellSize),
                    new Vector3((room.GridPosition.x + room.Size.x - 1) * config.CellSize, config.FloorHeightUnits, 
                        room.GridPosition.y * config.CellSize),
                    new Vector3(room.GridPosition.x * config.CellSize, config.FloorHeightUnits, 
                        (room.GridPosition.y + room.Size.y - 1) * config.CellSize),
                    new Vector3((room.GridPosition.x + room.Size.x - 1) * config.CellSize, config.FloorHeightUnits, 
                        (room.GridPosition.y + room.Size.y - 1) * config.CellSize)
                };
                
                positions.AddRange(corners);
            }
            
            return positions;
        }

        /// <summary>
        /// Determines whether a given cell position in a room is part of the room's perimeter
        /// </summary>
        /// <param name="cellPos">The position of the cell to check</param>
        /// <param name="room">The room data that contains the cell</param>
        /// <returns>Returns true if the cell is on the perimeter of the room, otherwise false</returns>
        private bool IsPerimeterCell(Vector2Int cellPos, RoomData room)
        {
            Vector2Int relative = cellPos - room.GridPosition;
            return relative.x == 0 || relative.x == room.Size.x - 1 ||
                   relative.y == 0 || relative.y == room.Size.y - 1;
        }

        /// <summary>
        /// Identifies the directions of walls within the given dungeon cell
        /// </summary>
        /// <param name="cell">The dungeon cell to evaluate for wall directions</param>
        /// <returns>Returns a list of directions where walls are present within the specified cell</returns>
        private List<Direction> GetWallDirections(DungeonCell cell)
        {
            List<Direction> dirs = new List<Direction>();
            if (cell.HasWallNorth) dirs.Add(Direction.North);
            if (cell.HasWallSouth) dirs.Add(Direction.South);
            if (cell.HasWallEast) dirs.Add(Direction.East);
            if (cell.HasWallWest) dirs.Add(Direction.West);
            return dirs;
        }

        /// <summary>
        /// Calculates the offset vector based on the given wall direction
        /// </summary>
        /// <param name="direction">The direction of the wall for which the offset vector is needed</param>
        /// <returns>Returns a Vector3 representing the offset relative to the specified wall direction</returns>
        private Vector3 GetWallOffsetVector(Direction direction)
        {
            return direction switch
            {
                Direction.North => Vector3.forward,
                Direction.South => Vector3.back,
                Direction.East => Vector3.right,
                Direction.West => Vector3.left,
                _ => Vector3.zero
            };
        }

        /// <summary>
        /// Determines the wall rotation based on the given direction
        /// </summary>
        /// <param name="direction">The direction indicating which wall rotation to apply</param>
        /// <returns>Returns a Quaternion representing the rotation to align with the specified wall direction</returns>
        private Quaternion GetWallRotation(Direction direction)
        {
            return direction switch
            {
                Direction.North => Quaternion.Euler(0, 180, 0),
                Direction.South => Quaternion.Euler(0, 0, 0),
                Direction.East => Quaternion.Euler(0, 270, 0),
                Direction.West => Quaternion.Euler(0, 90, 0),
                _ => Quaternion.identity
            };
        }

        /// <summary>
        /// Determines whether a specified position is too close to any previously placed props
        /// </summary>
        /// <param name="position">The position to evaluate for proximity to other props</param>
        /// <param name="minSpacing">The minimum distance required between props</param>
        /// <returns>Returns true if the position is within the minimum spacing distance from any previously placed prop; otherwise, false</returns>
        private bool IsTooCloseToOtherProps(Vector3 position, float minSpacing)
        {
            foreach (Vector3 placedPos in _placedPropPositions)
            {
                if (Vector3.Distance(position, placedPos) < minSpacing)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether a specified position is too close to any previously placed torches
        /// </summary>
        /// <param name="position">The position to evaluate for proximity to other torches</param>
        /// <returns>Returns true if the position is within the minimum spacing distance from any previously placed torch; otherwise, false</returns>
        private bool IsTooCloseToOtherTorches(Vector3 position)
        {
            foreach (Vector3 torchPos in _placedTorchPositions)
            {
                if (Vector3.Distance(position, torchPos) < TorchConfig.MinSpacing)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether a given position is too close to any of the doors within a specified room
        /// </summary>
        /// <param name="position">The world position to evaluate</param>
        /// <param name="room">The room data containing the positions of the doors</param>
        /// <param name="config">The dungeon configuration defining properties such as cell size</param>
        /// <returns>Returns true if the position is within a minimum distance from any door; otherwise, false</returns>
        private bool IsNearDoor(Vector3 position, RoomData room, DungeonConfig config)
        {
            for (int i = 0; i < room.DoorPositions.Count; i++)
            {
                // Get the position of the door and its direction
                Vector2Int doorPos = room.DoorPositions[i];
                Direction doorDir = room.DoorDirections[i];
                
                // Get the center of the door
                Vector3 doorWorldPos = new Vector3(
                    doorPos.x * config.CellSize, 
                    0, 
                    doorPos.y * config.CellSize
                );
                
                // Create a buffer zone around the door (spanning 1.5 cells on each side)
                float doorBuffer = config.CellSize * 1.5f;
                
                // Check distance with buffer
                if (Vector3.Distance(
                        new Vector3(position.x, 0, position.z),
                        new Vector3(doorWorldPos.x, 0, doorWorldPos.z)) < 
                        doorBuffer + MinDistanceFromDoors
                )
                {
                    return true;
                }
                
                // Also check the adjacent cell for 2-cell wide doors
                switch (doorDir)
                {
                    case Direction.North:
                    case Direction.South:
                        // Door is horizontal, check cells to the left and right
                        for (int offset = -1; offset <= 1; offset++)
                        {
                            Vector3 checkPos = new Vector3(
                                (doorPos.x + offset) * config.CellSize,
                                0,
                                doorPos.y * config.CellSize
                            );
                            
                            // Verify distance
                            if (Vector3.Distance(new Vector3(position.x, 0, position.z), 
                                    new Vector3(checkPos.x, 0, checkPos.z)) < MinDistanceFromDoors)
                                return true;
                        }
                        break;
                    case Direction.East:
                    case Direction.West:
                        // Door is vertical, check cells above and below
                        for (int offset = -1; offset <= 1; offset++)
                        {
                            Vector3 checkPos = new Vector3(
                                doorPos.x * config.CellSize,
                                0,
                                (doorPos.y + offset) * config.CellSize
                            );
                            
                            // Verify distance
                            if (Vector3.Distance(new Vector3(position.x, 0, position.z), 
                                    new Vector3(checkPos.x, 0, checkPos.z)) < MinDistanceFromDoors)
                                return true;
                        } 
                        break;
                }
            }
            
            // Also check if there's a door in the grid at this position
            Vector2Int gridPos = new Vector2Int(
                Mathf.RoundToInt(position.x / config.CellSize),
                Mathf.RoundToInt(position.z / config.CellSize)
            );

            // Exit case - no door cell found
            if (!_generator.Grid.TryGetValue(gridPos, out DungeonCell cell)) return false;
            
            return cell.Type == CellType.Door;
        }

        /// <summary>
        /// Determines whether the specified position is a corner position within the given room
        /// </summary>
        /// <param name="position">The world position to evaluate</param>
        /// <param name="room">The room data that defines the bounds of the room</param>
        /// <param name="config">The dungeon configuration used for calculations such as cell size</param>
        /// <returns>Returns true if the position is within a predefined threshold of one of the room's corners; otherwise, false</returns>
        private bool IsCornerPosition(Vector3 position, RoomData room, DungeonConfig config)
        {
            // Store the corners of the room
            Vector3[] corners = new Vector3[]
            {
                new Vector3(room.GridPosition.x * config.CellSize, config.FloorHeightUnits, 
                    room.GridPosition.y * config.CellSize),
                new Vector3((room.GridPosition.x + room.Size.x) * config.CellSize, config.FloorHeightUnits, 
                    room.GridPosition.y * config.CellSize),
                new Vector3(room.GridPosition.x * config.CellSize, config.FloorHeightUnits, 
                    (room.GridPosition.y + room.Size.y) * config.CellSize),
                new Vector3((room.GridPosition.x + room.Size.x) * config.CellSize, config.FloorHeightUnits, 
                    (room.GridPosition.y + room.Size.y) * config.CellSize)
            };
            
            // Check if the position is within the threshold of any of the corners
            foreach (Vector3 corner in corners)
            {
                if (Vector3.Distance(position, corner) < config.CellSize * 0.7f)
                    return true;
            }
            return false;
        }
    }
}