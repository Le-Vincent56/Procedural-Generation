using UnityEngine;

namespace Didionysymus.DungeonGeneration.LSystem
{
    public struct WallSegment
    {
        public Vector3 StartWorld;
        public Vector3 EndWorld;
        public Direction Direction;
        public float Length;
    }
}