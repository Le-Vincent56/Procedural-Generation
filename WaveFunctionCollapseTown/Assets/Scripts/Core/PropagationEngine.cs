using System;
using System.Collections.Generic;
using System.Text;
using Didionysymus.WaveFunctionCollapse.Data;
using UnityEngine;

namespace Didionysymus.WaveFunctionCollapse.Core
{
    public class PropagationEngine
    {
        private GridCell[,] _grid;
        private int _gridWidth;
        private int _gridDepth;
        private Queue<Vector2Int> _propagationQueue;
        private HashSet<Vector2Int> _queuedCells;

        public Action<Vector2Int> OnCellPropagated = delegate { };

        private static readonly Vector2Int[] _directionVectors = new Vector2Int[]
        {
            new Vector2Int(0, 1), // North
            new Vector2Int(1, 0), // East
            new Vector2Int(0, -1), // South
            new Vector2Int(-1, 0) // West
        };

        public PropagationEngine(GridCell[,] grid, int width, int height)
        {
            _grid = grid;
            _gridWidth = width;
            _gridDepth = height;
            _propagationQueue = new Queue<Vector2Int>();
            _queuedCells = new HashSet<Vector2Int>();
        }

        /// <summary>
        /// Propagates constraints from the starting cell through the grid based on adjacent tiles and their relationships
        /// </summary>
        /// <param name="startCell">The starting grid cell from which propagation begins; must not be null</param>
        /// <returns>
        /// A boolean value indicating whether the propagation completed successfully;
        /// returns true if the propagation was successful, and false otherwise
        /// </returns>
        public bool Propagate(GridCell startCell)
        {
            // Exit case - the starting cell is null
            if (startCell == null)
            {
                Debug.LogError("[PropagationEngine] Attempted to propagate from null cell");
                return false;
            }

            // Prepare propagation
            _propagationQueue.Clear();
            _queuedCells.Clear();
            _propagationQueue.Enqueue(startCell.Position);
            _queuedCells.Add(startCell.Position);
            
            while (_propagationQueue.Count > 0)
            {
                Vector2Int currentPosition = _propagationQueue.Dequeue();
                _queuedCells.Remove(currentPosition);
                
                // Propgate to all four neighbors
                for (int direction = 0; direction < 4; direction++)
                {
                    Vector2Int neighborPosition = currentPosition + _directionVectors[direction];

                    // Exit case - if failed to propagate to the neighbor cell
                    if (!PropagateToNeighbor(currentPosition, neighborPosition, direction))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Attempts to propagate constraints from the current grid cell to a neighboring cell
        /// while validating and updating the neighboring cell's possible tiles based on adjacency rules
        /// </summary>
        /// <param name="currentPosition">The position of the current grid cell initiating propagation</param>
        /// <param name="neighborPosition">The position of the neighboring grid cell to propagate to</param>
        /// <param name="direction">The direction in which propagation is occurring, represented as an integer</param>
        /// <returns>
        /// A boolean value indicating whether the propagation was successful; returns true if the propagation completes
        /// without encountering contradictions or failures, otherwise false
        /// </returns>
        private bool PropagateToNeighbor(Vector2Int currentPosition, Vector2Int neighborPosition, int direction)
        {
            // Check bounds
            if (neighborPosition.x < 0 || neighborPosition.x >= _gridWidth 
                                       || neighborPosition.y < 0 || neighborPosition.y >= _gridDepth)
            {
                // Out of bounds is not a failure
                return true;
            }

            // Get the current and neighbor cells
            GridCell current = _grid[currentPosition.x, currentPosition.y];
            GridCell neighbor = _grid[neighborPosition.x, neighborPosition.y];
            
            // Skip if the neighbor is already collapsed
            if (neighbor.IsCollapsed) return true;
            
            // Calculate the oppsoite direction
            int oppositeDirection = (direction + 2) % 4;
            
            // Determine which tiles in the neighbor are still valid
            HashSet<TileData> validTiles = new HashSet<TileData>();

            foreach (TileData neighborTile in neighbor.PossibleTiles)
            {
                // Skip if the neighbor tile is not valid
                if (!IsNeighborTileValid(current, neighborTile, direction, oppositeDirection)) continue;

                validTiles.Add(neighborTile);
            }
            
            // Update the neighbor if possibilities changed
            if (validTiles.Count < neighbor.PossibleTiles.Count)
            {
                // Update the neighbor's possible tiles and entropy
                neighbor.PossibleTiles = validTiles;
                neighbor.UpdateEntropy();
                
                // Exit case - a contradiction was found
                if (validTiles.Count == 0)
                {
                    StringBuilder debugBuilder = new StringBuilder();
                    debugBuilder.Append("Contradiction detected at ");
                    debugBuilder.Append(neighborPosition);
                    debugBuilder.Append(" (no valid tiles remaining");
                    
                    Debug.LogWarning(debugBuilder.ToString());
                    return false;
                }
                
                // Add to the queue for further propagation (avoiding duplicates)
                if (!_queuedCells.Contains(neighborPosition))
                {
                    _propagationQueue.Enqueue(neighborPosition);
                    _queuedCells.Add(neighborPosition);
                    
                    // Notify for visualization
                    OnCellPropagated?.Invoke(neighborPosition);
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a neighboring tile is valid based on its properties,
        /// the current grid cell, and their respective relationships
        /// </summary>
        /// <param name="current">The current grid cell being evaluated; must not be null</param>
        /// <param name="neighborTile">The tile data of the neighboring tile; must not be null</param>
        /// <param name="direction">The direction from the current cell to the neighboring tile</param>
        /// <param name="oppositeDirection">The direction from the neighboring tile to the current cell</param>
        /// <returns>
        /// A boolean value indicating whether the neighboring tile is valid based on the conditions evaluated;
        /// returns true if the neighbor tile is valid, otherwise false
        /// </returns>
        private bool IsNeighborTileValid(GridCell current, TileData neighborTile, int direction, int oppositeDirection)
        {
            // Check all possible rotations of the neighbor tile
            int maxNeighborRotations = neighborTile.AllowRotation
                ? neighborTile.RotationSteps
                : 1;

            for (int neighborRotation = 0; neighborRotation < maxNeighborRotations; neighborRotation++)
            {
                int neighborRotationDegrees = neighborRotation * 90;
                
                // Check against all possible tiles in the current celll
                foreach (TileData currentTile in current.PossibleTiles)
                {
                    // If the current cell is collapsed, use its actual rotation
                    if (current.IsCollapsed)
                    {
                        TileSocket currentSocket = currentTile.GetSocketForDirection(direction, current.Rotation);
                        TileSocket neighborSocket = neighborTile.GetSocketForDirection(oppositeDirection, neighborRotationDegrees);

                        // Exit case - the sockets are compatible
                        if (currentSocket != null && neighborSocket != null &&
                            currentSocket.IsCompatible(neighborSocket))
                        {
                            return true;
                        }
                    }
                    // If not collapsed, check all rotation directions
                    else
                    {
                        int maxCurrentRotations = currentTile.AllowRotation
                            ? currentTile.RotationSteps
                            : 1;

                        for (int currentRotation = 0; currentRotation < maxCurrentRotations; currentRotation++)
                        {
                            // Get the rotation degrees
                            int currentRotationDegrees = currentRotation * 90;
                            
                            TileSocket currentSocket = currentTile.GetSocketForDirection(direction, currentRotationDegrees);
                            TileSocket neighborSocket = neighborTile.GetSocketForDirection(oppositeDirection, neighborRotationDegrees);
                            
                            // Exit case - the sockets are compatible
                            if (currentSocket != null && neighborSocket != null &&
                                currentSocket.IsCompatible(neighborSocket))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}