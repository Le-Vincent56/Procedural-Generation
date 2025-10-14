using System.Collections.Generic;
using UnityEngine;

namespace Didionysymus.DungeonGeneration.LSystem
{
    /// <summary>
    /// Main dungeon generation controller: orchestrates the L-system grammar,
    /// turtle walkthrough, and prefab instantiation
    /// </summary>
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private DungeonConfig Config;
        
        [Header("Components")]
        [SerializeField] private DungeonInstantiator Instantiator;
        
        [Header("Runtime Controls")]
        [SerializeField] private bool GenerateOnStart = false;

        private LSystemGrammar _grammar;
        private DungeonTurtle _turtle;
        private string generatedString;

        public DungeonConfig Configuration => Config;
        public Dictionary<Vector2Int, DungeonCell> Grid { get; private set; }
        public List<RoomData> Rooms { get; private set; }
        

        private void Start()
        {
            // Exit case - not generating on Start
            if (!GenerateOnStart) return;

            GenerateDungeon();
        }

        /// <summary>
        /// Generates a dungeon by utilizing L-System procedural generation rules;
        /// configures the dungeon generation based on the provided settings, constructs the spatial structure,
        /// and instantiates the final representation in the scene. Includes debugging information if enabled.
        /// </summary>
        [ContextMenu("Generate Dungeon")]
        public void GenerateDungeon()
        {
            // Exit case - no DungeonConfig is assigned
            if (Config == null)
            {
                Debug.LogError("DungeonConfig is not assigned");
                return;
            }

            // Check if a DungeonInstantiator is serialized
            if (Instantiator == null)
            {
                // Attempt to find the DungeonInstantiator component
                Instantiator = GetComponent<DungeonInstantiator>();
                
                // Exit case - no DungeonInstantiator component found
                if (Instantiator == null)
                {
                    Debug.LogError("DungeonInstantiator component not found");
                    return;
                }
            }

            float startTime = Time.realtimeSinceStartup;
            
            // Generate the L-System string
            int seed = Config.Seed == 0 
                ? Random.Range(1, 100000) 
                : Config.Seed;

            // Create generation components
            _grammar = new LSystemGrammar(Config, seed);
            _turtle = new DungeonTurtle(Config, _grammar);
            
            // Initialize Instantiator
            Instantiator.Initialize(Config, _turtle);
            
            // Generate the L-system string
            generatedString = _grammar.Generate();

            // If debugging, log the generated string
            if (Config.Debug)
            {
                Debug.Log($"Generated L-System String:" +
                          $"\n\tSeed: {seed}" +
                          $"\n\t{generatedString}");
            }
            
            // Interpret the string into spatial data
            _turtle.Walkthrough(generatedString);
            
            // Set the grid and room data
            Grid = _turtle.Grid;
            Rooms = _turtle.Rooms;

            // If debugging, log the generated grid and room data
            if (Config.Debug)
            {
                Debug.Log($"Generated {Rooms.Count} rooms and {Grid.Count} total cells!");
                PrintRoomStatistics();
            }
            
            // Instantiate dungeon in scene
            Instantiator.InstantiateDungeon(Grid, Rooms);

            // If debugging, draw the debug grid
            if (Config.Debug)
            {
                Instantiator.DebugDrawGrid(Grid);
            }
            
            float endTime = Time.realtimeSinceStartup;
            Debug.Log($"Dungeon generation took {endTime - startTime} seconds");
        }

        /// <summary>
        /// Logs the statistics of the rooms generated in the dungeon;
        /// this includes the count of each room type, such as Start Rooms,
        /// Standard Rooms, Boss Rooms, Treasure Rooms, and Safe Rooms.
        /// </summary>
        private void PrintRoomStatistics()
        {
            int standardRooms = 0;
            int bossRooms = 0;
            int treasureRooms = 0;
            int safeRooms = 0;
            int startRooms = 0;
            
            foreach (RoomData room in Rooms)
            {
                switch (room.Type)
                {
                    case RoomType.Standard:
                        standardRooms++;
                        break;
                    case RoomType.Boss:
                        bossRooms++;
                        break;
                    case RoomType.Treasure:
                        treasureRooms++;
                        break;
                    case RoomType.Safe:
                        safeRooms++;
                        break;
                    case RoomType.Start:
                        startRooms++;
                        break;
                }
            }
            
            Debug.Log($"Room Statistics:\n" +
                      $"  Start Rooms: {startRooms}\n" +
                      $"  Standard Rooms: {standardRooms}\n" +
                      $"  Boss Rooms: {bossRooms}\n" +
                      $"  Treasure Rooms: {treasureRooms}\n" +
                      $"  Safe Rooms: {safeRooms}");
        }
    }
}