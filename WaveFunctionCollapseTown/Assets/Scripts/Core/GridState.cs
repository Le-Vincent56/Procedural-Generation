using System.Collections.Generic;

namespace Didionysymus.WaveFunctionCollapse.Core
{
    /// <summary>
    /// Represents a snapshot of the grid state in the Wave Function Collapse system, allowing storage and restoration
    /// of grid configurations for undo or reversion purposes
    /// </summary>
    public class GridState
    {
        private readonly GridCell[,] _gridSnapshot;
        private readonly Dictionary<TileData, int> _instanceCountsSnapshot;
        
        public GridState(GridCell[,] grid)
        {
            int width = grid.GetLength(0);
            int depth = grid.GetLength(1);
            _gridSnapshot = new GridCell[width, depth];
            
            // Deep copy all the cells
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    _gridSnapshot[x, z] = grid[x, z].Clone();
                }
            }

            _instanceCountsSnapshot = GridCell.GetInstanceCountsSnapshot();
        }

        /// <summary>
        /// Restores the state of the given grid to match the previously saved state snapshot
        /// </summary>
        /// <param name="grid">The grid to restore, represented as a 2D array of <see cref="GridCell"/> objects</param>
        public void RestoreTo(GridCell[,] grid)
        {
            int width = grid.GetLength(0);
            int depth = grid.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    grid[x, z] = _gridSnapshot[x, z].Clone();
                }
            }
            
            // Restore instance counts
            GridCell.RestoreInstanceCounts(_instanceCountsSnapshot);
        }
    }
}