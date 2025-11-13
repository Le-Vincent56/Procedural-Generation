using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

namespace Didionysymus.WaveFunctionCollapse.Data
{
    /// <summary>
    /// Represents a socket type used in the Wave Function Collapse algorithm
    /// for defining compatibility between different tiles in a grid-based system
    /// </summary>
    [Serializable]
    public class TileSocket
    {
        public SocketType SocketType;
        
        /// <summary>
        /// Determines if the given TileSocket is compatible with this TileSocket based on explicit or symmetric compatibility
        /// </summary>
        /// <param name="other">The TileSocket to check compatibility with</param>
        /// <returns>
        /// Returns true if the given TileSocket is compatible, either through explicit type matching or symmetric type matching;
        /// returns false if the TileSocket is null or not compatible.
        /// </returns>
        public bool IsCompatible(TileSocket other)
        {
            // Exit case - the other TileSocket is null
            if (other == null) return false;

            switch (SocketType)
            {
                case SocketType.Road:
                    return other.SocketType is SocketType.Road or SocketType.BuildingDoor or SocketType.TownGate;
                
                case SocketType.NoRoad:
                    return other.SocketType is SocketType.NoRoad or SocketType.Empty or SocketType.BuildingWall
                        or SocketType.TownWall or SocketType.BuildingDoor;
                
                case SocketType.Empty:
                    return other.SocketType is SocketType.Empty or SocketType.NoRoad or SocketType.BuildingWall
                        or SocketType.TownWall;
                
                case SocketType.BuildingWall:
                    return other.SocketType is SocketType.BuildingWall or SocketType.Empty or SocketType.NoRoad;
                
                case SocketType.BuildingDoor:
                    return other.SocketType is SocketType.Road or SocketType.NoRoad;
                
                case SocketType.TownWall:
                    return other.SocketType is SocketType.TownWall or SocketType.Empty or SocketType.NoRoad or SocketType.TownGate;
                    
                case SocketType.TownGate:
                    return other.SocketType is SocketType.TownWall or SocketType.Road;
            }
            
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.Append("Unknown socket type in compatibility check: ");
            debugBuilder.Append(SocketType);
            
            Debug.LogError(debugBuilder.ToString());
            return false;
        }
    }
}