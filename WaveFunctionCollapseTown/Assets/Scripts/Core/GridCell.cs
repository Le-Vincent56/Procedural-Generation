using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Didionysymus.WaveFunctionCollapse.Core
{
    /// <summary>
    /// Represents a single cell in a grid used for the Wave Function Collapse algorithm; each GridCell contains
    /// information about its position, its current collapsed or uncollapsed state, possible tiles, and entropy value
    /// </summary>
    public class GridCell
    {
        public Vector2Int Position;
        public bool IsCollapsed = false;
        public TileData CollapsedTile;
        public int Rotation = 0;

        public HashSet<TileData> PossibleTiles;
        public float Entropy;
        
        private static Dictionary<TileData, int> _instanceCounts = new Dictionary<TileData, int>();

        public GridCell(int x, int y, List<TileData> allTiles)
        {
            Position = new Vector2Int(x, y);
            PossibleTiles = new HashSet<TileData>(allTiles);
            UpdateEntropy();
        }

        /// <summary>
        /// Collapses the GridCell to a specified tile, setting the selected tile as the cell's final state,
        /// clearing all other possible tiles, and updating the collapsed state;
        /// the rotation of the tile is randomly selected if allowed;
        /// this also tracks the instance count of the specified tile
        /// </summary>
        /// <param name="tile">The tile to collapse the GridCell into; must not be null</param>
        /// <param name="rotation">The rotation of the tile to collapse the GridCell into; must be a multiple of 90</param>
        public void Collapse(TileData tile, int rotation)
        {
            // Exit case - if the tile does not exist
            if (!tile)
            {
                StringBuilder debugBuilder = new StringBuilder();
                debugBuilder.Append("Attempted to collapse cell at ");
                debugBuilder.Append(Position);
                debugBuilder.Append(" with null tile");
                
                Debug.LogError(debugBuilder.ToString());
                return;
            }

            // Collapse the tile
            CollapsedTile = tile;
            PossibleTiles.Clear();
            PossibleTiles.Add(tile);
            IsCollapsed = true;
            
            // Use provided rotation
            Rotation = rotation;
            
            // Track the instance count of the tile
            IncrementInstanceCount(tile);
        }

        /// <summary>
        /// Removes a specified tile from the possible tiles of the current GridCell and updates its entropy
        /// if the tile is successfully removed; this process reduces the uncertainty of the GridCell
        /// by limiting its potential tile options
        /// </summary>
        /// <param name="tile">The tile to be removed from the possible tiles of the GridCell</param>
        /// <returns>
        /// Returns a boolean indicating whether the specified tile was successfully removed
        /// from the possible tiles; true if removal is successful, otherwise false
        /// </returns>
        public bool RemovePossibility(TileData tile)
        {
            // Check if the tile was removed
            bool removed = PossibleTiles.Remove(tile);
            
            // Update the entropy if the tile was removed
            if (removed) UpdateEntropy();

            return removed;
        }

        /// <summary>
        /// Updates the entropy of the current GridCell based on the possible tiles that
        /// can be assigned to it; the entropy reflects the uncertainty or randomness,
        /// calculated using the weights of the possible tiles and applying Shannon entropy.
        /// </summary>
        public void UpdateEntropy()
        {
            // Exit case - there are no possible tiles
            if (PossibleTiles.Count == 0)
            {
                // Invalid state
                Entropy = float.MaxValue;
                return;
            }

            // Check if there's only one possible tile
            if (PossibleTiles.Count == 1)
            {
                // Fully determined
                Entropy = 0f;
                return;
            }
            
            // Calculate the total weight
            float totalWeight = 0f;
            foreach (TileData tile in PossibleTiles)
            {
                totalWeight += tile.Weight;
            }

            // Exit case - the total weight equals 0
            if (totalWeight == 0f)
            {
                // Fallback to count-based entropy
                Entropy = PossibleTiles.Count;
                return;
            }

            // Shannon Entropy
            Entropy = 0f;
            foreach (TileData tile in PossibleTiles)
            {
                float probability = tile.Weight / totalWeight;

                if (probability > 0f)
                {
                    Entropy -= probability * Mathf.Log(probability, 2);
                }
            }
            
            // Add tiny random noise to break ties
            Entropy += Random.Range(0f, 0.001f);
        }

        /// <summary>
        /// Determines whether the current GridCell is in a valid state by checking
        /// if there are any possible tiles that can still be assigned to it
        /// </summary>
        /// <returns>True if the GridCell has at least one possible tile; otherwise, false</returns>
        public bool IsValid() => PossibleTiles.Count > 0;

        /// <summary>
        /// Creates a deep copy of the current GridCell, including its properties
        /// such as possible tiles, position, rotation, collapsed state,
        /// and associated entropy
        /// </summary>
        /// <returns>A new GridCell instance, identical to the original</returns>
        public GridCell Clone()
        {
            GridCell clone = new GridCell(Position.x, Position.y, new List<TileData>());
            clone.PossibleTiles = new HashSet<TileData>(PossibleTiles);
            clone.IsCollapsed = IsCollapsed;
            clone.CollapsedTile = CollapsedTile;
            clone.Rotation = Rotation;
            clone.Entropy = Entropy;
            return clone;
        }

        /// <summary>
        /// Retrieves the number of instances that have been created for the specified tile
        /// </summary>
        /// <param name="tile">The tile for which to get the instance count</param>
        /// <returns>The number of instances of the specified tile; returns 0 if the tile has no instances</returns>
        public static int GetInstanceCount(TileData tile) => _instanceCounts.GetValueOrDefault(tile, 0);

        /// <summary>
        /// Increments the instance count of the specified tile in the internal tracking dictionary;
        /// if the tile is not already in the dictionary, its count is initialized to zero before incrementing
        /// </summary>
        /// <param name="tile">The tile for which to increment the instance count; must not be null</param>
        public static void IncrementInstanceCount(TileData tile)
        {
            _instanceCounts.TryAdd(tile, 0);
            _instanceCounts[tile]++;
        }

        /// <summary>
        /// Retrieves a snapshot of the current instance counts for all tiles, providing a copy of the internal dictionary
        /// that tracks how many instances of each tile have been used
        /// </summary>
        /// <returns>A dictionary containing the snapshot of the instance counts, where the keys are tile data objects and the values are their respective counts</returns>
        public static Dictionary<TileData, int> GetInstanceCountsSnapshot() =>
            new Dictionary<TileData, int>(_instanceCounts);

        /// <summary>
        /// Restores the instance counts of tiles to the provided snapshot, ensuring that the tracked instance
        /// counts reflect the specified state; this operation clears the current instance counts and replaces them
        /// with the values in the provided snapshot
        /// </summary>
        /// <param name="snapshot">A dictionary snapshot containing the tile data and corresponding instance counts; must not be null</param>
        public static void RestoreInstanceCounts(Dictionary<TileData, int> snapshot)
        {
            _instanceCounts.Clear();
            foreach (KeyValuePair<TileData, int> entry in snapshot)
            {
                _instanceCounts[entry.Key] = entry.Value;
            }
        }
        
        /// <summary>
        /// Resets the instance count tracking for all TileData objects;
        ///clears the internal dictionary that maintains the counts of how many times each tile has been used
        /// </summary>
        public static void ResetInstanceCounts() => _instanceCounts.Clear();
    }
}