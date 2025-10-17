using System;
using System.Collections.Generic;
using UnityEngine;

namespace Didionysymus.DungeonGeneration.LSystem
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
}