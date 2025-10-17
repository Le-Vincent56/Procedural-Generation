using System;
using UnityEngine;

namespace Didionysymus.DungeonGeneration.LSystem
{
    [Serializable]
    public class TorchSettings
    {
        public GameObject TorchPrefab;
        public float MinSpacing = 3f;
        public float MaxSpacing = 5f;
        public float WallOffset = 0.05f;
        public float HeightOffset = 1.5f;
        public bool PlaceInRooms = true;
        public bool PlaceInCorridors = true;
        [Range(0f, 1f)] public float CornerSkipChance = 0.5f;
    }
}