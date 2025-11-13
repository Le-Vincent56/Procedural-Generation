using System.Collections.Generic;
using System.Text;
using Didionysymus.WaveFunctionCollapse.Core;
using UnityEngine;

namespace Didionysymus.WaveFunctionCollapse.Data
{
    /// <summary>
    /// Represents a collection of tiles used in a Wave Function Collapse algorithm,
    /// providing functionality for managing a set of tiles, applying weight multipliers,
    /// and retrieving tiles based on specific criteria
    /// </summary>
    [CreateAssetMenu(fileName = "New Tile Set", menuName = "WFC/Tile Set", order = 2)]
    public class TileSet : ScriptableObject
    {
        [Header("Tile Collection")]
        public List<TileData> Tiles = new List<TileData>();

        [Header("Set Configuration")] 
        public string SetName = "Medieval Town";
        public float DefaultTileSize = 5f;
        
        [Header("Weight Multipliers")]
        [Range(0.1f, 10f)]
        public float RoadWeightMultiplier = 2f;
        
        [Range(0.1f, 10f)]
        public float BuildingWeightMultiplier = 1f;
        
        [Range(0.1f, 10f)]
        public float EmptyWeightMultiplier = 1.5f;
        
        [Range(0.1f, 10f)]
        public float WallWeightMultiplier = 1f;

        /// <summary>
        /// Retrieves the complete collection of tile data available in the TileSet;
        /// only non-null tiles with a valid Prefab are included in the returned list
        /// </summary>
        /// <returns>
        /// A list of <c>TileData</c> objects that represent the valid tiles within the TileSet
        /// </returns>
        public List<TileData> GetAllTiles()
        {
            List<TileData> allTiles = new List<TileData>();
            
            foreach (TileData tile in Tiles)
            {
                // Skip null tiles
                if (!tile || !tile.Prefab) continue;
                
                allTiles.Add(tile);
            }
            
            return allTiles;
        }

        /// <summary>
        /// Retrieves a list of tiles from the TileSet that belong to the specified category;
        /// only non-null tiles with a valid Prefab and matching category will be included in the returned list
        /// </summary>
        /// <param name="category">The category of tiles to filter by</param>
        /// <returns>A list of <c>TileData</c> objects within the specified category</returns>
        public List<TileData> GetTilesByCategory(TileCategory category)
        {
            List<TileData> tiles = new List<TileData>();
            
            foreach (TileData tile in Tiles)
            {
                // Skip null tiles
                if (!tile || !tile.Prefab) continue;

                // Skip if the category does not match
                if (tile.Category != category) continue;
                
                tiles.Add(tile);
            }
            
            return tiles;
        }

        /// <summary>
        /// Validates all tiles in the TileSet to ensure their socket configurations are correct
        /// and identifies any null or invalid tiles within the collection
        /// </summary>
        [ContextMenu("Validate All Tiles")]
        public void ValidateAllTiles()
        {
            StringBuilder debugBuilder = new StringBuilder();
            
            int validCount = 0;
            int invalidCount = 0;

            foreach (TileData tile in Tiles)
            {
                // Skip if the tile is null
                if (!tile)
                {
                    debugBuilder.Clear();
                    debugBuilder.Append("TileSet ");
                    debugBuilder.Append(SetName);
                    debugBuilder.Append(" contains a null tile reference");
                    
                    Debug.LogWarning(debugBuilder.ToString());
                    invalidCount++;
                    continue;
                }

                // Track valid and invalid sockets
                if (tile.ValidateSockets()) validCount++;
                else invalidCount++;
            }
            
            debugBuilder.Clear();
            debugBuilder.Append("TileSet Validation Complete: ");
            debugBuilder.Append(validCount);
            debugBuilder.Append(" valid, ");
            debugBuilder.Append(invalidCount);
            debugBuilder.Append(" invalid");
                
            Debug.Log(debugBuilder.ToString());
        }

        /// <summary>
        /// Adjusts the weight values of all tiles in the TileSet based on the predefined weight multipliers
        /// for each category; this ensures that specific categories of tiles have their probability weights
        /// scaled proportionally according to the multipliers specified in the TileSet's configuration
        /// </summary>
        [ContextMenu("Apply Weight Multipliers")]
        public void ApplyWeightMultipliers()
        {
            foreach (TileData tile in Tiles)
            {
                // Skip null tiles
                if (!tile) continue;

                float multiplier = 1f;

                switch (tile.Category)
                {
                    case TileCategory.Road:
                        multiplier = RoadWeightMultiplier;
                        break;
                    
                    case TileCategory.ResidentialBuilding:
                    case TileCategory.CommercialBuilding:
                    case TileCategory.PublicBuilding:
                        multiplier = BuildingWeightMultiplier;
                        break;
                    
                    case TileCategory.OpenSpace:
                        multiplier = EmptyWeightMultiplier;
                        break;
                    
                    case TileCategory.Wall:
                        multiplier = WallWeightMultiplier;
                        break;
                }

                // Apply the weight
                tile.Weight = Mathf.Max(0.1f, tile.Weight * multiplier);
            }

            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.Append("Applied weight multipliers to ");
            debugBuilder.Append(Tiles.Count);
            debugBuilder.Append(" tiles");
            
            Debug.Log(debugBuilder.ToString());
        }
    }
}