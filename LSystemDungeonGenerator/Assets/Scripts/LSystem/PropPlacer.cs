using System;
using System.Collections.Generic;
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
        [Serializable]
        public class PropSet
        {
            public string Name;
            public List<GameObject> PropPrefabs;
            public RoomType TargetRoomType = RoomType.Standard;
            [Range(0f, 1f)] public float PlacementDensity = 0.3f;
            public bool PlaceOnWalls = false;
            public bool PlaceInCenter = true;
            public bool PlaceInCorners = false;
            public float MinSpacing = 2f;
        }

        [Serializable]
        public class TorchSettings
        {
            public GameObject TorchPrefab;
            public float MinSpacing = 3f;
            public float MaxSpacing = 5f;
            public float WallOffset = 0.1f;
            public float HeightOffset = 1.5f;
            public bool PlaceInRooms = true;
            public bool PlaceInCorridors = true;
            [Range(0f, 1f)] public float CornerSkipChance = 0.5f;
        }

        [Header("Torch Settings")] 
        [SerializeField] private TorchSettings TorchConfig = new TorchSettings();

        [Header("General Props")] [SerializeField]
        private PropSet GeneralProps = new PropSet()
        {
            Name = "General",
            PlacementDensity = 0.2f,
            MinSpacing = 1.5f
        };
    }
}