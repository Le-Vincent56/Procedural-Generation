using UnityEngine;

namespace Didionysymus.DungeonGeneration.LSystem
{
    [CreateAssetMenu(fileName = "DungeonConfig", menuName = "L-System/Dungeon Config")]
    public class DungeonConfig : ScriptableObject
    {
        [Header("Generation Settings")] 
        [Tooltip("Random seed for generation (0 = random)")]
        public int Seed = 0;
        public int Iterations = 3;
        
        [Tooltip("Whether to place a 1-cell buffer around rooms and corridors")]
        public bool OneCellBuffer = true;
        
        [Header("Dungeon Size")]
        public Vector2Int MaxGridSize = new Vector2Int(50, 50);
        public int NumberOfFloors = 1;
        
        [Header("Room Settings")]
        public Vector2Int MinRoomSize = new Vector2Int(3, 3);
        public Vector2Int MaxRoomSize = new Vector2Int(7, 7);
        public float SmallRoomChance = 0.5f;
        public float MediumRoomChance = 0.35f;
        public float LargeRoomChance = 0.15f;
        
        [Header("Special Rooms")]
        public int BossRoomCount = 1;
        public int TreasureRoomCount = 2;
        public int SafeRoomCount = 2;
        
        [Header("Corridor Settings")]
        public int MinCorridorLength = 3;
        public int MaxCorridorLength = 6;
        public int CorridorWidth = 1;

        [Header("Density")]
        [Tooltip("Desired fill percentage of the dungeon (higher = more full)")]
        [Range(0f, 1f)]
        public float DesiredFill = 0.65f;
        
        [Tooltip("How densely packed the dungeon should be (higher = more cramped")]
        [Range(0f, 1f)]
        public float DungeonDensity = 0.6f;
        
        [Tooltip("Chance of branching during generation")]
        [Range(0f, 1f)]
        public float BranchingChance = 0.4f;
        
        [Header("Physical Dimensions")]
        [Tooltip("The size of one grid cell in world units")]
        public float CellSize = 1f;
        public float FloorHeightCells = 3f;
        public int Floors = 3;
        public float FloorHeightUnits => FloorHeightCells * CellSize;
        
        [Header("L-System Grammar")]
        [Tooltip("Custom Axiom (starting symbol) - leave empty for default")]
        public string CustomAxiom = "";
        
        [Tooltip("Enable Debug Visualization")]
        public bool Debug = true;
    }
}
