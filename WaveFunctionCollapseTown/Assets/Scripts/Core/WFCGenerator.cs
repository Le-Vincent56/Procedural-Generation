using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Didionysymus.WaveFunctionCollapse.Data;
using PrimeTween;
using UnityEngine;

namespace Didionysymus.WaveFunctionCollapse.Core
{
    public class WFCGenerator : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [Range(5, 20)]
        [SerializeField] private int gridWidth = 20;
        
        [Range(5, 20)]
        [SerializeField] private int gridDepth = 20;
        
        [SerializeField] private float tileSize = 5f;
        
        [Header("Generation Settings")]
        [SerializeField] private TileSet tileSet;
        [SerializeField] private int seed = -1;
        [SerializeField] private bool useRandomSeed = true;
        
        [Header("Algorithm Settings")]
        [SerializeField] private bool useBacktracking = true;
        [SerializeField] private int maxBacktrackAttempts = 1000;
        [SerializeField] private bool propagateImmediately = true;
        [SerializeField] private float visualizationDelay = 0.1f;
        
        [Header("Town Layout Constraints")]
        [SerializeField] private bool generateTownWalls = false;
        [SerializeField] private bool ensureConnectedRoads = true;
        [SerializeField] private bool createTownCenter = true;
        [SerializeField] private Vector2Int townCenterPosition = new Vector2Int(25, 25);
        [SerializeField] private int townCenterRadius = 2;
        
        [Header("Wall Data")]
        [SerializeField] private TileData wallStraightTile;
        [SerializeField] private TileData wallGateTile;
        [SerializeField] private TileData wallCornerTile;
        
        [Header("Debug Settings")] 
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool visualizeEntropy = false;
        [SerializeField] private bool pauseOnContradiction = false;
        [SerializeField] private bool showCurrentCollapse = true;
        [SerializeField] private bool showPropagation = true;
        [SerializeField] private bool colorByCategory = true;
        
        private GridCell[,] _grid;
        private Stack<GridState> _history;
        private PropagationEngine _propagationEngine;
        private bool _isGenerating = false;
        private int _backtrackCount = 0;
        private GameObject _tileContainer;

        private float _generationStartTime;
        private int _totalCollapses = 0;
        private int _contradictions = 0;

        private GridCell _currentCollapsingCell;
        private List<Vector2Int> _currentPropagatingCells = new List<Vector2Int>();
        private List<Vector2Int> _collapseOrder = new List<Vector2Int>();
        
        private void Start()
        {
            // Auto-generate on Start() if TileSet is assigned
            if (!tileSet || tileSet.GetAllTiles().Count == 0)
            {
                Debug.Log("No Tiles");
                return;
            }

            Debug.Log("Starting generation...");
            StartCoroutine(GenerateTownCoroutine());
        }

        private IEnumerator GenerateTownCoroutine()
        {
            // Exit case - already generating
            if (_isGenerating)
            {
                Debug.LogWarning("Generation already in progress!");
                yield break;
            }
            
            // Exit case - no TileSet assigned
            if(!tileSet || tileSet.GetAllTiles().Count == 0)
            {
                Debug.LogWarning("Cannot generate: No tileset assigned or tileset is empty!");
                yield break;
            }

            _isGenerating = true;
            _generationStartTime = Time.realtimeSinceStartup;
            
            StringBuilder debugBuilder = new StringBuilder();
            
            // Clean up previous generation
            CleanupPreviousGeneration();
            
            // Initialize
            InitializeSeed();
            InitializeGrid();
            
            // Apply initial constraints
            ApplyInitialConstraints();
            
            // Main WFC generation loop
            while (!IsFullyCollapsed())
            {
                // Find the cell with minimum entropy
                GridCell cellToCollapse = GetMinimumEntropyCell();

                // Check if the cell is valid
                if (cellToCollapse == null || !cellToCollapse.IsValid())
                {
                    // Contradiction detected
                    _contradictions++;

                    if (cellToCollapse != null)
                    {
                        LogContradictionAtCell(cellToCollapse.Position);
                    }

                    // Show debug info
                    if (showDebugInfo)
                    {
                        debugBuilder.Clear();
                        debugBuilder.Append("Contradiction detected (total: ");
                        debugBuilder.Append(_contradictions);
                        debugBuilder.Append(")");
                        
                        Debug.LogWarning(debugBuilder.ToString());
                    }

                    // Attempt to backtrack
                    if (useBacktracking && _backtrackCount < maxBacktrackAttempts)
                    {
                        bool backtrackSuccess = Backtrack();

                        // Exit case - if backtracking failed
                        if (!backtrackSuccess)
                        {
                            Debug.LogError("Generation failed: unable to backtrack");
                            break;
                        }

                        // Track backtracks
                        _backtrackCount++;

                        // Pause on contradiction
                        if (pauseOnContradiction)
                            yield return new WaitForSeconds(0.5f);

                        continue;
                    }
                    else
                    {
                        debugBuilder.Clear();
                        debugBuilder.Append("Generation failed: max backtracks (");
                        debugBuilder.Append(maxBacktrackAttempts);
                        debugBuilder.Append(") reached");
                        
                        Debug.LogError(debugBuilder.ToString());
                        break;
                    }
                }
                
                // Collapse the selected cell
                CollapseCell(cellToCollapse);
                _totalCollapses++;

                if (!cellToCollapse.IsCollapsed)
                {
                    // Collapse failed (no valid rotation) - treat as a contradiction
                    _contradictions++;

                    LogContradictionAtCell(cellToCollapse.Position);

                    if (showDebugInfo)
                    {
                        debugBuilder.Clear();
                        debugBuilder.Append("Collapse failed at ");
                        debugBuilder.Append(cellToCollapse.Position);
                        debugBuilder.Append(", backtracking");
                        Debug.LogWarning(debugBuilder.ToString());
                    }
                    
                    if (useBacktracking && _backtrackCount < maxBacktrackAttempts)
                    {
                        Backtrack();
                        _backtrackCount++;
                        continue;
                    }
                    else
                    {
                        debugBuilder.Clear();
                        debugBuilder.Append("Generation failed: collapse failed and cannot backtrack");
                        break;
                    }
                }
                
                // Propagate constraints
                if (propagateImmediately)
                {
                    // Clear the propagating cells list
                    _currentPropagatingCells.Clear();
                    
                    bool propagationSuccess = _propagationEngine.Propagate(cellToCollapse);

                    if (!propagationSuccess)
                    {
                        _contradictions++;

                        if (useBacktracking && _backtrackCount < maxBacktrackAttempts)
                        {
                            Backtrack();
                            _backtrackCount++;
                        }
                    }
                }
                
                // Visualization delay
                if(visualizationDelay > 0)
                    yield return new WaitForSeconds(visualizationDelay);
            }
            
            // Post-processing
            InstantiateTiles();

            // Ensure connected roads
            if (ensureConnectedRoads) ValidateRoadConnectivity();
            
            // Report statistics
            float generationTime = Time.realtimeSinceStartup - _generationStartTime;
            debugBuilder.Clear();
            debugBuilder.Append("Generation Complete!");
            debugBuilder.AppendLine("Time: ");
            debugBuilder.Append(generationTime.ToString("F2"));
            debugBuilder.Append("s | Collapses: ");
            debugBuilder.Append(_totalCollapses);
            debugBuilder.Append(" | Backtracks: ");
            debugBuilder.Append(_backtrackCount);
            debugBuilder.Append(" | Contradictions: ");
            debugBuilder.Append(_contradictions);
            Debug.Log(debugBuilder.ToString());

            _isGenerating = false;
        }

        /// <summary>
        /// Initializes the random seed used for procedural generation.
        /// If the useRandomSeed flag is enabled, a new random seed will be generated and assigned;
        /// otherwise, the preconfigured seed value is used; the initialized seed is then applied
        /// to the random number generator to ensure consistent generation
        /// </summary>
        private void InitializeSeed()
        {
            // Check if using a random seed
            if (useRandomSeed)
            {
                seed = Random.Range(0, int.MaxValue);
            }
            
            // Initialize the seed
            Random.InitState(seed);
            
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.Append("Seed: ");
            debugBuilder.Append(seed);
            
            Debug.Log(debugBuilder.ToString());
        }

        /// <summary>
        /// Initializes the grid structure for the Wave Function Collapse (WFC) algorithm;
        /// Creates and configures a two-dimensional grid of GridCell objects, where each cell
        /// is initialized with the full set of possible tiles retrieved from the assigned TileSet;
        /// resets relevant instance counts and sets up the propagation engine for constraint propagation
        /// </summary>
        private void InitializeGrid()
        {
            // Initialize components
            _grid = new GridCell[gridWidth, gridDepth];
            _history = new Stack<GridState>();
            _propagationEngine = new PropagationEngine(_grid, gridWidth, gridDepth);

            _propagationEngine.OnCellPropagated = (position) =>
            {
                // Exit case - not showing propagation
                if (!showPropagation) return;

                // Exit case - cell is already propagating
                if (_currentPropagatingCells.Contains(position)) return;
                
                _currentPropagatingCells.Add(position);
            };
            
            // Reset instance counts
            GridCell.ResetInstanceCounts();
            
            // Get all available tiles from the TileSet
            List<TileData> availableTiles = tileSet.GetAllTiles();

            // Exit case - there are no available tiles in the TileSet
            if (availableTiles.Count == 0)
            {
                Debug.LogError("TileSet contains no valid tiles!");
                return;
            }
            
            // Initialize each cell with all possibilities
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    _grid[x, z] = new GridCell(x, z, availableTiles);
                }
            }
            
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.Append("Grid initialized: ");
            debugBuilder.Append(gridWidth);
            debugBuilder.Append("x");
            debugBuilder.Append(gridDepth);
            debugBuilder.Append(" | ");
            debugBuilder.Append(availableTiles.Count);
            debugBuilder.Append(" tiles");
            
            Debug.Log(debugBuilder.ToString());
        }

        /// <summary>
        /// Applies initial constraints to the grid before the Wave Function Collapse
        /// process begins
        /// </summary>
        private void ApplyInitialConstraints()
        {
            ApplyDistanceConstraints();
            ApplyBorderConstraints();
            if(generateTownWalls) ApplyWallConstraints();
            if(createTownCenter) CreateTownCenter();
        }

        /// <summary>
        /// Enforces boundary constraints on the grid to simulate wall structures
        /// by constraining the outer edges to consist exclusively of wall tiles;
        /// additionally, gates are placed at specific midpoints to allow controlled
        /// access through the walls
        /// </summary>
        private void ApplyWallConstraints()
        {
            // Validate wall tiles are assigned
            if (!wallStraightTile || !wallCornerTile || !wallGateTile)
            {
                Debug.LogError("Wall tiles not assigned!");
                return;
            }

            // Get the mid points
            int midX = gridWidth / 2;
            int midZ = gridDepth / 2;

            // Handle corners
            GridCell southWest = _grid[0, 0];
            GridCell southEast = _grid[gridWidth - 1, 0];
            GridCell northWest = _grid[0, gridDepth - 1];
            GridCell northEast = _grid[gridWidth - 1, gridDepth - 1];
            
            southWest.Collapse(wallCornerTile, 270);
            _collapseOrder.Add(southWest.Position);
            
            southEast.Collapse(wallCornerTile, 180);
            _collapseOrder.Add(southEast.Position);
            
            northEast.Collapse(wallCornerTile, 90);
            _collapseOrder.Add(northEast.Position);
            
            northWest.Collapse(wallCornerTile, 0);
            _collapseOrder.Add(northWest.Position);           

            // Handle Gates
            GridCell midWest = _grid[midX, 0];
            GridCell midEast = _grid[midX, gridDepth - 1];
            GridCell midSouth = _grid[0, midZ];
            GridCell midNorth = _grid[gridWidth - 1, midZ];
            
            midWest.Collapse(wallGateTile, 0);
            _collapseOrder.Add(midWest.Position);
            
            midEast.Collapse(wallGateTile, 180);
            _collapseOrder.Add(midEast.Position);
            
            midSouth.Collapse(wallGateTile, 90);
            _collapseOrder.Add(midSouth.Position);
            
            midNorth.Collapse(wallGateTile, 270);
            _collapseOrder.Add(midNorth.Position);
            
            
            // Straight walls
            for (int x = 0; x < gridWidth; x++)
            {
                // Skip corners and gates
                if (x == 0 || x == gridWidth - 1 || x == midX) continue;

                if (!_grid[x, 0].IsCollapsed)
                {
                    // Get the grid cell
                    GridCell cell = _grid[x, 0];
                    
                    // Collapse the cell
                    cell.Collapse(wallStraightTile, 0);
                    _collapseOrder.Add(cell.Position);
                }

                if (!_grid[x, gridDepth - 1].IsCollapsed)
                {
                    // Get the grid cell
                    GridCell cell = _grid[x, gridDepth - 1];
                    
                    // Collapse the cell
                    cell.Collapse(wallStraightTile, 0);  
                    _collapseOrder.Add(cell.Position);
                }          
            }
            
            for(int z = 0; z < gridDepth; z++) 
            {
                // Skip corners and gates
                if (z == 0 || z == gridDepth - 1 || z == midZ) continue;

                if (!_grid[0, z].IsCollapsed)
                {
                    // Get the grid cell
                    GridCell cell = _grid[0, z];
                    
                    // Collapse the cell
                    cell.Collapse(wallStraightTile, 90);
                    _collapseOrder.Add(cell.Position);
                }
                
                if(!_grid[gridWidth - 1, z].IsCollapsed)
                {
                    // Get the grid cell
                    GridCell cell = _grid[gridWidth - 1, z];
                    
                    // Collapse the cell
                    cell.Collapse(wallStraightTile, 90);
                    _collapseOrder.Add(cell.Position);
                }       
            }
            
            // Propagate from all collapsed walls
            for (int x = 0; x < gridWidth; x++)
            {
                _propagationEngine.Propagate(_grid[x, 0]);
                _propagationEngine.Propagate(_grid[x, gridDepth - 1]);
            }

            for (int z = 0; z < gridDepth; z++)
            {
                _propagationEngine.Propagate(_grid[0, z]);
                _propagationEngine.Propagate(_grid[gridWidth - 1, z]);
            }
            
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.Append("Wall constraints applied for ");
            debugBuilder.Append(gridWidth);
            debugBuilder.Append("x");
            debugBuilder.Append(gridDepth);
            debugBuilder.Append(" grid: 4 corners, 4 gates, ");
            debugBuilder.Append((gridWidth + gridDepth) * 2 - 16);
            debugBuilder.Append(" straight walls");
            
            Debug.Log(debugBuilder.ToString());
        }

        /// <summary>
        /// Configures a centralized plaza-like area within the grid based on the specified
        /// center position and radius; ensures that the designated area is constrained
        /// to open space or plaza tiles, effectively creating a focal point for the
        /// generated town layout; the center position is clamped to remain within grid
        /// boundaries, and the radius determines the size of the area affected
        /// </summary>
        private void CreateTownCenter()
        {
            // Find suitable road tiles
            TileData crossTile = null;
            TileData straightTile = null;
            
            foreach (TileData tile in tileSet.GetAllTiles())
            {
                // Get the tile name
                string tileName = tile.TileName.ToLower();
            
                if (tileName.Contains("cross"))
                    crossTile = tile;
                
                if(tileName.Contains("straight"))
                    straightTile = tile;
            }
            
            // Fallback: use any road tile
            if(!crossTile || !straightTile) 
            {
                foreach (TileData tile in tileSet.GetAllTiles())
                {
                    if(tile.Category != TileCategory.Road) continue;
            
                    if (!crossTile) crossTile = tile;
                    if(!straightTile) straightTile = tile;
                }
            }
            
            if (!crossTile || !straightTile)
            {
                Debug.LogWarning("Not enough Road tiles found for town center");
                return;
            }
            
            int cellsCollapsed = 0;
            if (townCenterRadius == 0)
            {
                // Single crossroads at the center
                GridCell centerCell = _grid[townCenterPosition.x, townCenterPosition.y];
                centerCell.Collapse(crossTile, 0);
                _propagationEngine.Propagate(centerCell);
                
                // Track the cell
                _collapseOrder.Add(centerCell.Position);
                cellsCollapsed = 1;
            } else
            {
                Vector2Int[] directions =
                {
                    new Vector2Int(0, 1),   // North
                    new Vector2Int(1, 0),   // East
                    new Vector2Int(0, -1),  // South
                    new Vector2Int(-1, 0)   // West
                };
                
                // Place center crossroads
                GridCell centerCell = _grid[townCenterPosition.x, townCenterPosition.y];
                centerCell.Collapse(crossTile, 0);
                
                // Track the cell being collapsed
                _collapseOrder.Add(centerCell.Position);
                cellsCollapsed++;
                
                // Extend roads in 4 directions
                for(int direction = 0; direction < 4; direction++) 
                {
                    Vector2Int gridDirection = directions[direction];
                    int rotation = direction * 90;
                    
                    for(int distance = 1; distance <= townCenterRadius; distance++) 
                    {
                        int cellX = townCenterPosition.x + (gridDirection.x * distance);
                        int cellZ = townCenterPosition.y + (gridDirection.y * distance);
                        
                        // Check bounds
                        if(cellX < 0 || cellX >= gridWidth) continue;
                        if(cellZ < 0 || cellZ >= gridDepth) continue;
                        
                        // Get the grid cell
                        GridCell cell = _grid[cellX, cellZ];
            
                        // Skip if the cell is already collapsed
                        if (cell.IsCollapsed) continue;
                        
                        // Collapse the cell
                        cell.Collapse(straightTile, rotation);
                        
                        // Track the collapsed cell
                        _collapseOrder.Add(cell.Position);
                        cellsCollapsed++;
                    }
                }
                
                _propagationEngine.Propagate(centerCell);
            
                // Propagate from all collapsed road cells
                for(int direction = 0; direction < 4; direction++) 
                {
                    Vector2Int gridDirection = directions[direction];
                    int rotation = direction * 90;
                    
                    for(int distance = 1; distance <= townCenterRadius; distance++) 
                    {
                        int cellX = townCenterPosition.x + (gridDirection.x * distance);
                        int cellZ = townCenterPosition.y + (gridDirection.y * distance);
                        
                        // Check bounds
                        if(cellX < 0 || cellX >= gridWidth) continue;
                        if(cellZ < 0 || cellZ >= gridDepth) continue;
                        
                        // Get the grid cell
                        GridCell cell = _grid[cellX, cellZ];
                        
                        _propagationEngine.Propagate(_grid[cellX, cellZ]);
                    }
                }
            }
            
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.Append("Created road-centric town center at (");
            debugBuilder.Append(townCenterPosition.x);
            debugBuilder.Append(", ");
            debugBuilder.Append(townCenterPosition.y);
            debugBuilder.Append(") - Pre-collapsed ");
            debugBuilder.Append(cellsCollapsed);
            debugBuilder.Append(" road tiles (");
            if (townCenterRadius == 0)
            {
                debugBuilder.Append("crossroads");
            }
            else 
            {
                debugBuilder.Append("cross pattern, radius ");
                debugBuilder.Append(townCenterRadius);
            }
            debugBuilder.Append(")");
            
            Debug.Log(debugBuilder.ToString());
        }
        
        /// <summary>
        /// Applies distance-based constraints to the grid, limiting the possible tiles
        /// for each cell based on their distance from the defined town center; tiles
        /// that do not meet the minimum or maximum distance requirements are excluded
        /// from the cell's possibilities. Entropy is updated for each cell after constraints
        /// are applied
        /// </summary>
        private void ApplyDistanceConstraints()
        {
            Vector2 center = new Vector2(townCenterPosition.x, townCenterPosition.y);

            for (int x = 0; x < gridWidth; x++)
            {
                for(int z = 0; z < gridDepth; z++)
                {
                    GridCell cell = _grid[x, z];
                    
                    // Skip already-collapsed cells
                    if (cell.IsCollapsed) continue;
                    
                    // Calculate the distance from the center
                    float distance = Vector2.Distance(new Vector2(x, z), center);

                    // Remove possible tiles according to constraints
                    cell.PossibleTiles.RemoveWhere(tile => distance < tile.MinDistanceFromCenter
                        || distance > tile.MaxDistanceFromCenter);

                    cell.UpdateEntropy();
                }
            }
        }

        /// <summary>
        /// Enforces border constraints on the grid to ensure that only valid tiles
        /// are assigned to the outermost edges;a ny tiles that cannot align with
        /// border requirements are removed, maintaining consistency and coherence
        /// in the border regions of the grid
        /// </summary>
        private void ApplyBorderConstraints()
        {
            // Remove tiles that can't be placed at borders
            for (int x = 0; x < gridWidth; x++)
            {
                if(!_grid[x, 0].IsCollapsed)
                    ApplyBorderConstraintToCell(_grid[x, 0]);
                
                if(!_grid[x, gridDepth - 1].IsCollapsed)
                    ApplyBorderConstraintToCell(_grid[x, gridDepth - 1]);
            }

            for (int z = 0; z < gridDepth; z++)
            {
                if(!_grid[0, z].IsCollapsed)
                    ApplyBorderConstraintToCell(_grid[0, z]);
                
                if(!_grid[gridWidth - 1, z].IsCollapsed)
                    ApplyBorderConstraintToCell(_grid[gridWidth - 1, z]);
            }
        }

        /// <summary>
        /// Applies border constraints to a specific grid cell, removing tiles that are not suitable for placement
        /// at the border based on their category; updates the entropy of the cell after modification
        /// </summary>
        /// <param name="cell">The grid cell to which the border constraints will be applied</param>
        private void ApplyBorderConstraintToCell(GridCell cell)
        {
            cell.PossibleTiles.RemoveWhere(tile => !tile.CanPlaceAtBorder);
        }

        /// <summary>
        /// Checks whether the Wave Function Collapse (WFC) grid has been fully collapsed;
        /// the grid is considered fully collapsed if all cells have been resolved
        /// into a single possible tile configuration without any remaining entropy
        /// </summary>
        /// <returns>True if all cells in the grid are collapsed; otherwise, false</returns>
        private bool IsFullyCollapsed()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    // Exit case - grid is not fully collapsed
                    if (!_grid[x, z].IsCollapsed) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Identifies and retrieves the grid cell with the lowest entropy value that has not yet been collapsed;
        /// cells with lower entropy have fewer possible configurations and are prioritized for selection;
        /// if multiple cells have the same minimal entropy, one is chosen randomly; returns null if there are no valid uncollapsed cells available
        /// </summary>
        /// <returns>The grid cell with the minimum entropy, or null if no such cell exists</returns>
        private GridCell GetMinimumEntropyCell()
        {
            List<GridCell> uncollapsedCells = new List<GridCell>();
            
            // Collect all uncollapsed cells with valid possibilities
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    GridCell cell = _grid[x, z];
                    
                    if (!cell.IsCollapsed && cell.IsValid())
                        uncollapsedCells.Add(cell);
                }
            }

            // Exit case - no uncollapsed cells
            if (uncollapsedCells.Count == 0) return null;
            
            // Sort by entropy (ascending)
            uncollapsedCells.Sort((a, b) => a.Entropy.CompareTo(b.Entropy));
            
            // Get all cells with minimum entropy
            float minimumEntropy = uncollapsedCells[0].Entropy;
            List<GridCell> minimumEntropyCells = new List<GridCell>();

            for (int i = 0; i < uncollapsedCells.Count; i++)
            {
                // Skip if entropy is not equal to the minimum
                if (Mathf.Approximately(uncollapsedCells[i].Entropy, minimumEntropy))
                    minimumEntropyCells.Add(uncollapsedCells[i]);
                else
                    break;
            }
            
            // Randomly select one from the minimum entropy cells
            return minimumEntropyCells[Random.Range(0, minimumEntropyCells.Count)];
        }

        /// <summary>
        /// Collapses a given grid cell by selecting a tile based on its probabilities and assigns it to the cell;
        /// optionally saves the state for backtracking and logs debug information if enabled
        /// </summary>
        /// <param name="cell">The grid cell to be collapsed; it must be a valid, non-null cell that is not already collapsed</param>
        private void CollapseCell(GridCell cell)
        {
            // Exit case - the cell is null or collapsed
            if (cell == null || cell.IsCollapsed) return;
            
            // Track for visualization
            _currentCollapsingCell = cell;
            
            // Save state for potential backtracking
            if (useBacktracking) SaveState();
            
            // Select a tile based on weights
            TileData selectedTile = SelectTileByWeight(cell.PossibleTiles);

            StringBuilder debugBuilder = new StringBuilder();
            
            if (!selectedTile)
            {
                debugBuilder.Clear();
                debugBuilder.Append("Failed to select tile at ");
                debugBuilder.Append(cell.Position);
                
                Debug.LogError(debugBuilder.ToString());
                return;
            }

            int validRotation = SelectValidRotation(cell, selectedTile);

            // Exit case - no valid rotation was found
            if (validRotation == -1)
            {
                if (showDebugInfo)
                {
                    debugBuilder.Clear();
                    debugBuilder.Append("No valid rotation found for ");
                    debugBuilder.Append(selectedTile.TileName);
                    debugBuilder.Append(" at ");
                    debugBuilder.Append(cell.Position);
                    debugBuilder.Append(". Trying a different tile");
                
                    Debug.LogWarning(debugBuilder.ToString());
                }

                cell.PossibleTiles.Remove(selectedTile);
                
                // Try another tile if possible
                if (cell.PossibleTiles.Count > 0)
                {
                    CollapseCell(cell);
                    return;
                }
                else
                {
                    // No tiles left - this is a contradiction, let the main loop handle
                    // the backtracking
                    debugBuilder.Clear();
                    debugBuilder.Append("Contradiction at ");
                    debugBuilder.Append(cell.Position);
                    debugBuilder.Append(" - no valid tiles with valid rotations");
                    Debug.LogWarning(debugBuilder.ToString());

                    cell.Entropy = float.MaxValue;
                    _currentCollapsingCell = null;
                    return;
                }
            }
            
            // Collapse the cell
            cell.Collapse(selectedTile, validRotation);
            
            // Track collapse order for visualization
            _collapseOrder.Add(cell.Position);
            
            if (showDebugInfo)
            {
                debugBuilder.Clear();
                debugBuilder.Append("Collapsed cell at ");
                debugBuilder.Append(cell.Position);
                debugBuilder.Append(" to ");
                debugBuilder.Append(selectedTile.TileName);
                debugBuilder.Append(" (rotation: ");
                debugBuilder.Append(cell.Rotation);
                debugBuilder.Append(", Entropy: ");
                debugBuilder.Append(cell.Entropy.ToString("F2"));
                debugBuilder.Append(")");
                
                Debug.Log(debugBuilder.ToString());
            }
            
            // Clear after a frame
            _currentCollapsingCell = null;
        }

        /// <summary>
        /// Selects a valid rotation for a given tile in a grid cell;
        /// checks all possible rotations of the tile to determine which ones are valid
        /// based on the tile's properties and the grid cell's position
        /// </summary>
        /// <param name="cell">The grid cell for which a valid rotation is being selected</param>
        /// <param name="tile">The tile for which valid rotations are being checked</param>
        /// <returns>
        /// Returns an integer representing the selected rotation in degrees;
        /// returns -1 if no valid rotation is found
        /// </returns>
        private int SelectValidRotation(GridCell cell, TileData tile)
        {
            // Get possible rotations
            int maxRotations = tile.AllowRotation
                ? tile.RotationSteps
                : 1;

            List<int> validRotations = new List<int>();
            
            // Check each possible rotation
            for (int rotationStep = 0; rotationStep < maxRotations; rotationStep++)
            {
                int rotationDegrees = rotationStep * 90;

                // Skip if the rotation is not valid for the position of the cell
                if (!IsRotationValidForPosition(cell.Position, tile, rotationDegrees))
                    continue;
                
                validRotations.Add(rotationDegrees);
            }
            
            // No valid rotations found
            if (validRotations.Count == 0)
            {
                DiagnoseInvalidRotation(cell.Position, tile, 0);
                return -1;
            }
            
            // Randomly select from valid rotations
            return validRotations[Random.Range(0, validRotations.Count)];
        }

        /// <summary>
        /// Determines whether a given rotation is valid for the specified position and tile;
        /// validity is based on neighboring tiles' compatibility with the tile's sockets
        /// at the specified rotation
        /// </summary>
        /// <param name="position">The position of the cell in the grid</param>
        /// <param name="tile">The tile to check the rotation validity for</param>
        /// <param name="rotation">The rotation angle in degrees, typically in increments of 90</param>
        /// <returns>Returns true if the specified rotation is compatible with all neighboring tiles; otherwise, false.</returns>
        private bool IsRotationValidForPosition(Vector2Int position, TileData tile, int rotation)
        {
            // Check all four directions
            Vector2Int[] directions =
            {
                new Vector2Int(0, 1),  // North
                new Vector2Int(1, 0),   // East
                new Vector2Int(0, -1),   // South
                new Vector2Int(-1, 0)   // West
            };

            for (int direction = 0; direction < 4; direction++)
            {
                Vector2Int neighborPosition = position + directions[direction];
                
                // Skip out of bounds
                if (neighborPosition.x < 0 || neighborPosition.x >= gridWidth || neighborPosition.y < 0 ||
                    neighborPosition.y >= gridDepth)
                {
                    continue;
                }

                GridCell neighbor = _grid[neighborPosition.x, neighborPosition.y];
                
                // Only check collapsed neighbors (uncollapsed will be constrained later)
                if (!neighbor.IsCollapsed) continue;
                
                // Get sockets
                int oppositeDirection = (direction + 2) % 4;
                TileSocket thisSocket = tile.GetSocketForDirection(direction, rotation);
                TileSocket neighborSocket = neighbor.CollapsedTile.GetSocketForDirection(oppositeDirection, neighbor.Rotation);
                
                // Check compatibility
                if (thisSocket == null || neighborSocket == null) return false;
                if (!thisSocket.IsCompatible(neighborSocket)) return false;
            }

            // All neighbors are compatible with this rotation
            return true;
        }

        /// <summary>
        /// Selects a tile from the given set of possible tiles based on their respective weights;
        /// applies weighted random selection, taking into account constraints such as the maximum
        /// number of allowed instances per tile; if no valid weights are present, falls back to
        /// uniform selection or the last tile in the set
        /// </summary>
        /// <param name="tiles">A collection of TileData representing the possible tiles for selection</param>
        /// <returns>The selected TileData object based on the weighted random algorithm, or null if no tiles are available</returns>
        private TileData SelectTileByWeight(HashSet<TileData> tiles)
        {
            // Exit case - no tiles available
            if (tiles == null || tiles.Count == 0) return null;
            
            // Filter out tiles that have reached max instances
            List<TileData> availableTiles = new List<TileData>(tiles.Count);
            foreach (TileData tile in tiles)
            {
                if (tile.MaxInstancesPerTown < 0 || GridCell.GetInstanceCount(tile) < tile.MaxInstancesPerTown)
                {
                    availableTiles.Add(tile);
                }
            }

            // Check if the available tiles list is empty
            if (availableTiles.Count == 0)
            {
                // All tiles are maxed out, fall back to the original set
                availableTiles.Clear();
                foreach (TileData tile in tiles)
                {
                    availableTiles.Add(tile);
                }
            }

            // Add together all the weights
            float totalWeight = 0f;
            for (int i = 0; i < availableTiles.Count; i++)
            {
                totalWeight += availableTiles[i].Weight;
            }

            // Exit case - all weights are zero
            if (totalWeight == 0)
            {
                // Select uniformly
                return availableTiles[Random.Range(0, availableTiles.Count)];
            }
            
            // Weighted random selection
            float random = Random.Range(0f, totalWeight);
            float current = 0f;

            foreach (TileData tile in availableTiles)
            {
                current += tile.Weight;
                if (random <= current) return tile;
            }
            
            // Fallback
            return availableTiles[availableTiles.Count - 1];
        }

        /// <summary>
        /// Saves the current state of the grid to enable backtracking if necessary;
        /// the method captures the current arrangement and status of grid cells
        /// by pushing a snapshot of the grid onto the history stack; if the number
        /// of saved states exceeds the configured maximum depth, the oldest state
        /// is removed to manage memory usage
        /// </summary>
        private void SaveState()
        {
            // Push the current state onto the history stack
            _history.Push(new GridState(_grid));
            
            // Limit history depth to prevent memory issues
            int maxHistoryDepth = (gridWidth * gridDepth) * 2;
            if (_history.Count > maxHistoryDepth)
            {
                // Convert to a list, remove the oldest, then convert back
                GridState[] historyArray = _history.ToArray();
                _history.Clear();
                
                // Push back all except the oldest (skipping the last element)
                for (int i = 0; i < historyArray.Length - 1; i++)
                {
                    _history.Push(historyArray[i]);
                }
            }
        }

        /// <summary>
        /// Attempts to revert the grid to a previous valid state by restoring
        /// the most recent state from the history stack; this is used to resolve
        /// contradictions during the Wave Function Collapse process when a viable
        /// solution cannot be immediately found
        /// </summary>
        /// <returns>True if the backtracking operation successfully restored a previous state; otherwise, false if no history is available to backtrack</returns>
        private bool Backtrack()
        {
            if (_history.Count == 0)
            {
                Debug.LogWarning("Cannot backtrack: no history available");
                return false;
            }

            // Restore the previous state
            GridState previousState = _history.Pop();
            previousState.RestoreTo(_grid);

            if (showDebugInfo)
            {
                StringBuilder debugBuilder = new StringBuilder();
                debugBuilder.Append("Backtracked (history depth: ");
                debugBuilder.Append(_history.Count);
                debugBuilder.Append(")");
                
                Debug.Log(debugBuilder.ToString());
            }

            return true;
        }

        /// <summary>
        /// Instantiates the generated grid of tiles based on the wave function collapse results;
        /// this method creates a container GameObject to organize the tiles and iterates through the generated
        /// grid to instantiate appropriate tile prefabs at their designated positions; it also tracks and logs
        /// statistics about the number of instantiated tiles, distinguishing between buildings, roads, and empty tiles
        /// </summary>
        private void InstantiateTiles()
        {
            // Create a container for organization
            _tileContainer = new GameObject("Generated Town");
            _tileContainer.transform.position = Vector3.zero;

            StringBuilder tileNameBuilder = new StringBuilder();
            
            int instantiatedCount = 0;
            int buildingCount = 0;
            int roadCount = 0;
            int emptyCount = 0;
            for (int i = 0; i < _collapseOrder.Count; i++)
            {
                // Get the grid position
                Vector2Int gridPosition = _collapseOrder[i];
                
                // Extract the grid cell
                GridCell cell = _grid[gridPosition.x, gridPosition.y];

                // Skip if the cell is not collapsed
                if (!cell.IsCollapsed) continue;
                    
                // Skip if the cell's collapsed tile is null
                if (!cell.CollapsedTile) continue;
                    
                // Skip if the cell's collapsed tile prefab is null
                if (!cell.CollapsedTile.Prefab) continue;

                // Get the world position (place at the center of the grid cell)
                Vector3 position = new Vector3(
                    gridPosition.x * tileSize + (tileSize * 0.5f),
                    0,
                    gridPosition.y * tileSize + (tileSize * 0.5f)
                );

                // Get the rotation
                Quaternion rotation = Quaternion.Euler(0, cell.Rotation, 0);
                    
                // Instantiate the tile object
                GameObject tileObject = Instantiate(
                    cell.CollapsedTile.Prefab, 
                    position, 
                    rotation,
                    _tileContainer.transform
                );

                // Set the tile name
                tileNameBuilder.Clear();
                tileNameBuilder.Append(cell.CollapsedTile.TileName);
                tileNameBuilder.Append("_");
                tileNameBuilder.Append(gridPosition.x);
                tileNameBuilder.Append("_");
                tileNameBuilder.Append(gridPosition.y);
                tileObject.name = tileNameBuilder.ToString();

                // Start at scale 0 and bounce to scale 1
                tileObject.transform.localScale = Vector3.zero;
                
                // Stagger the animation based on collapse order
                float delay = i * 0.025f;

                Tween.Scale(
                    tileObject.transform,
                    Vector3.one,
                    duration: 0.75f,
                    ease: Ease.OutElastic,
                    startDelay: delay
                );
                
                instantiatedCount++;

                if (cell.CollapsedTile.Category is TileCategory.Road)
                    roadCount++;

                if (cell.CollapsedTile.Category is TileCategory.CommercialBuilding
                    or TileCategory.ResidentialBuilding or TileCategory.PublicBuilding)
                    buildingCount++;

                if (cell.CollapsedTile.Category is TileCategory.OpenSpace or TileCategory.Special)
                    emptyCount++;
            }
            
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.Append("Instantiated ");
            debugBuilder.Append(instantiatedCount);
            debugBuilder.Append(" tiles");
            debugBuilder.AppendLine();
            debugBuilder.AppendLine("Buildings: ");
            debugBuilder.Append(buildingCount);
            debugBuilder.Append(" | Roads: ");
            debugBuilder.Append(roadCount);
            debugBuilder.Append(" | Empty Tiles: ");
            debugBuilder.Append(emptyCount);
            
            Debug.Log(debugBuilder.ToString());
        }

        /// <summary>
        /// Validates the connectivity of all road tiles within the generated grid;
        /// this method ensures that all road tiles are part of a single connected component, enabling
        /// uninterrupted traversal across the road network; if any road tile is found to be disconnected,
        /// appropriate debug warnings or errors are logged; the check includes identifying all road tiles,
        /// performing a flood fill operation to traverse connected roads, and comparing the connected
        /// component size with the total number of road tiles.
        /// </summary>
        private void ValidateRoadConnectivity()
        {
            HashSet<Vector2Int> visitedRoads = new HashSet<Vector2Int>();
            Vector2Int? firstRoad = null;
            
            // Find the first road tile
            for (int x = 0; x < gridWidth && !firstRoad.HasValue; x++)
            {
                for (int z = 0; z < gridDepth && !firstRoad.HasValue; z++)
                {
                    // Skip if the cell is not collapsed
                    if (!_grid[x, z].IsCollapsed) continue;
                    
                    // Skip if the cell's collapsed tile is not a road
                    if(_grid[x, z].CollapsedTile.Category != TileCategory.Road) continue;
                    
                    firstRoad = new Vector2Int(x, z);
                }
            }

            // Exit case - still not road found
            if (!firstRoad.HasValue)
            {
                Debug.LogWarning("No roads found in the generated town!");
                return;
            }
            
            // Flood fill from the first road
            Queue<Vector2Int> toVisit = new Queue<Vector2Int>();
            toVisit.Enqueue(firstRoad.Value);

            while (toVisit.Count > 0)
            {
                Vector2Int current = toVisit.Dequeue();

                // Skip visited roads
                if (visitedRoads.Contains(current)) continue;

                // Visit the road
                visitedRoads.Add(current);
                
                // Check neighbors
                Vector2Int[] neighbors =
                {
                    current + Vector2Int.up,
                    current + Vector2Int.right,
                    current + Vector2Int.down,
                    current + Vector2Int.left
                };

                // Check neighbors to see if they are roads
                foreach (Vector2Int neighbor in neighbors)
                {
                    if (neighbor.x >= 0 && neighbor.x < gridWidth && neighbor.y >= 0 && neighbor.y < gridDepth)
                    {
                        GridCell cell = _grid[neighbor.x, neighbor.y];
                        if (cell.IsCollapsed && cell.CollapsedTile.Category == TileCategory.Road)
                        {
                            toVisit.Enqueue(neighbor);
                        }
                    }
                }
            }

            int totalRoads = 0;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    if (_grid[x, z].IsCollapsed && _grid[x, z].CollapsedTile.Category == TileCategory.Road)
                    {
                        totalRoads++;
                    }
                }
            }

            StringBuilder debugBuilder = new StringBuilder();
            
            if (visitedRoads.Count < totalRoads)
            {
                debugBuilder.Append("Disconnected roads detected! Connected: ");
                debugBuilder.Append(visitedRoads.Count);
                debugBuilder.Append("/");
                debugBuilder.Append(totalRoads);
                Debug.LogWarning(debugBuilder.ToString());
            }
            else
            {
                debugBuilder.Append("All roads connected: ");
                debugBuilder.Append(visitedRoads.Count);
                debugBuilder.Append(" road tiles");
                Debug.Log(debugBuilder.ToString());
            }
        }

        /// <summary>
        /// Cleans up the remnants of any previous generation processes; this method
        /// destroys any existing tile container and resets the statistics related
        /// to the Wave Function Collapse process, such as total collapses, contradictions,
        /// and backtrack count; it prepares the generator for a new procedural generation cycle
        /// </summary>
        private void CleanupPreviousGeneration()
        {
            // Destroy the tile container if it exists
            if (_tileContainer) DestroyImmediate(_tileContainer);
            
            // Reset collapse tracking
            _collapseOrder.Clear();
            
            // Reset statistics
            _totalCollapses = 0;
            _contradictions = 0;
            _backtrackCount = 0;
            
            Debug.Log("Cleaned up the previous generation");
        }
        
        /// <summary>
        /// Determines the color representation for a given tile category;
        /// each tile category is mapped to a specific color to visually
        /// distinguish the types of tiles within the grid during visualization
        /// </summary>
        /// <param name="category">The tile category for which the color is to be assigned</param>
        /// <returns>The color corresponding to the specified tile category. If the category is unrecognized, a default white color is returned</returns>
        private Color GetCategoryColor(TileCategory category)
        {
            switch (category)
            {
                case TileCategory.Road: return new Color(0.5f, 0.5f, 0.5f);
                case TileCategory.ResidentialBuilding: return new Color(0.8f, 0.6f, 0.4f);
                case TileCategory.CommercialBuilding: return new Color(0.4f, 0.6f, 0.8f);
                case TileCategory.PublicBuilding: return new Color(0.8f, 0.8f, 0.4f);
                case TileCategory.OpenSpace: return new Color(0.4f, 0.8f, 0.4f);
                case TileCategory.Wall: return new Color(0.6f, 0.4f, 0.4f);
                case TileCategory.Special: return new Color(0.8f, 0.4f, 0.8f);
                default: return Color.white;
            }
        }

        /// <summary>
        /// Diagnoses issues related to invalid rotations for a specific tile during Wave Function Collapse;
        /// it checks the compatibility of the tile's sockets with its neighbors and logs detailed information
        /// about the diagnosis, including position, tile information, attempted rotation, neighbor tiles,
        /// sockets, and compatibility results
        /// </summary>
        /// <param name="position">The grid position of the tile being diagnosed</param>
        /// <param name="tile">The tile data of the tile under validation</param>
        /// <param name="attemptedRotation">The attempted rotation of the tile causing the invalid state</param>
        private void DiagnoseInvalidRotation(Vector2Int position, TileData tile, int attemptedRotation)
        {
            StringBuilder debugBuilder = new StringBuilder();
            debugBuilder.Append("=== Invalid Rotation Diagnosis ===");
            debugBuilder.AppendLine("Position: ");
            debugBuilder.Append(position);
            debugBuilder.AppendLine("Tile: ");
            debugBuilder.Append(tile.TileName);
            debugBuilder.AppendLine("Attempted Rotation: ");
            debugBuilder.Append(attemptedRotation);
            debugBuilder.AppendLine("Neighbors: ");

            Vector2Int[] directions =
            {
                new Vector2Int(0, 1),  // North
                new Vector2Int(1, 0),   // East
                new Vector2Int(0, -1),   // South
                new Vector2Int(-1, 0)   // West
            };

            string[] directionNames =
            {
                "North",
                "East",
                "South",
                "West"
            };

            for (int direction = 0; direction < 4; direction++)
            {
                Vector2Int neighborPosition = position + directions[direction];

                if (neighborPosition.x < 0 || neighborPosition.x >= gridWidth || neighborPosition.y < 0 ||
                    neighborPosition.y >= gridDepth)
                {
                    debugBuilder.AppendLine("\t");
                    debugBuilder.Append(directionNames[direction]);
                    debugBuilder.Append(": Out of Bounds");
                    continue;
                }

                GridCell neighbor = _grid[neighborPosition.x, neighborPosition.y];

                if (!neighbor.IsCollapsed)
                {
                    debugBuilder.AppendLine("\t");
                    debugBuilder.Append(directionNames[direction]);
                    debugBuilder.Append(": Uncollapsed (");
                    debugBuilder.Append(neighbor.PossibleTiles.Count);
                    debugBuilder.Append(" possibilities)");
                    continue;
                }

                int oppositeDirection = (direction + 2) % 4;
                
                // Get the Tile Sockets
                TileSocket thisSocket = tile.GetSocketForDirection(direction, attemptedRotation);
                TileSocket neighborSocket = neighbor.CollapsedTile.GetSocketForDirection(oppositeDirection, neighbor.Rotation);
                
                // Check compatibility
                bool compatible = thisSocket != null && neighborSocket != null && thisSocket.IsCompatible(neighborSocket);

                debugBuilder.AppendLine("\t");
                debugBuilder.Append(directionNames[oppositeDirection]);
                debugBuilder.Append(": ");
                debugBuilder.Append(neighbor.CollapsedTile.TileName);
                debugBuilder.Append(" (rotation: ");
                debugBuilder.Append(neighbor.Rotation);
                debugBuilder.Append(")");

                debugBuilder.AppendLine("\t");
                debugBuilder.Append("This Socket: ");
                debugBuilder.Append(thisSocket?.SocketType);
                debugBuilder.Append(", Neighbor Socket: ");
                debugBuilder.Append(neighborSocket?.SocketType);

                debugBuilder.AppendLine("\t");
                debugBuilder.Append("Compatible: ");
                debugBuilder.Append(compatible);
            }
            
            Debug.LogWarning(debugBuilder.ToString());
        }

        /// <summary>
        /// Logs debug information about a detected contradiction at the specified grid cell position;
        /// the method provides contextual information about the cell, including its remaining possible tiles
        /// and the states of its neighboring cells, or notes if a neighbor is out of bounds.
        /// </summary>
        /// <param name="position">The position of the grid cell where the contradiction occurred</param>
        private void LogContradictionAtCell(Vector2Int position)
        {
            StringBuilder debug = new StringBuilder();
            debug.AppendLine($"\n=== CONTRADICTION CONTEXT AT {position} ===");

            Vector2Int[] directions =
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0)
            };
                
            string[] directionNames = {
                "North",
                "East",
                "South",
                "West"
            };
            
            GridCell cell = _grid[position.x, position.y];
            debug.AppendLine($"Cell has {cell.PossibleTiles.Count} possible tiles remaining");

            for (int i = 0; i < 4; i++)
            {
                Vector2Int nPos = position + directions[i];

                if (nPos.x >= 0 && nPos.x < gridWidth && nPos.y >= 0 && nPos.y < gridDepth)
                {
                    GridCell neighbor = _grid[nPos.x, nPos.y];

                    if (neighbor.IsCollapsed)
                    {
                        int oppositeDirection = (i + 2) % 4;
                        TileSocket neighborSocket =
                            neighbor.CollapsedTile.GetSocketForDirection(oppositeDirection, neighbor.Rotation);

                        debug.AppendLine(
                            $"\t{directionNames[i]}: {neighbor.CollapsedTile.TileName} (rotation: {neighbor.Rotation}, Socket Facing Here: {neighborSocket?.SocketType}");
                    }
                    else
                    {
                        debug.AppendLine($"\t{directionNames[i]}: Uncollapsed ({neighbor.PossibleTiles.Count} options");
                    }
                }
                else
                {
                    debug.AppendLine($"\t{directionNames[i]}: Out of bounds");
                }
            }
            
            Debug.LogError(debug.ToString());
        }

        private void OnDrawGizmos()
        {
            // Exit case - not visualizing, the grid is null, or not generating
            if (!visualizeEntropy || _grid == null || !_isGenerating) return;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    // Get the current grid cell
                    GridCell cell = _grid[x, z];
                    Vector3 cellPosition = new Vector3(
                        x * tileSize + (tileSize * 0.5f), 
                        0.5f, 
                        z * tileSize + (tileSize * 0.5f)
                    );

                    // // Show collapsed tiles
                    if (cell.IsCollapsed)
                    {
                        if (colorByCategory && cell.CollapsedTile != null)
                        {
                            Gizmos.color = GetCategoryColor(cell.CollapsedTile.Category);
                            Gizmos.DrawCube(cellPosition, Vector3.one * (tileSize * 0.3f));
                        }
                    } else if (visualizeEntropy && _isGenerating)
                    {
                        float normalizedEntropy = Mathf.Clamp01(cell.Entropy / 5f);
                        Gizmos.color = Color.Lerp(Color.green, Color.red, normalizedEntropy);
                        Gizmos.DrawCube(cellPosition, Vector3.one * (tileSize * 0.8f));
                        
#if UNITY_EDITOR
                        UnityEditor.Handles.Label(
                            cellPosition + Vector3.up * 2f,
                            cell.PossibleTiles.Count.ToString("F2")
                        );
#endif
                    }

                    if (showCurrentCollapse && _currentCollapsingCell != null &&
                        cell.Position == _currentCollapsingCell.Position)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(cellPosition, tileSize * 0.6f);
                    }

                    if (showPropagation && _currentPropagatingCells.Contains(cell.Position))
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireCube(cellPosition, Vector3.one * (tileSize * 0.9f));
                    }
                }
            }
        }
    }
}