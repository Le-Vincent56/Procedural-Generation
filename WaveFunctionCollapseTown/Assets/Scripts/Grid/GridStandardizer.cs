using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Didionysymus.WaveFunctionCollapse.Grid
{
    /// <summary>
    /// The GridStandardizer class provides functionality to standardize prefabs
    /// by aligning them to a consistent grid layout; this process involves recalculating bounds,
    /// adjusting pivot points, centering, and optionally creating colliders and metadata.
    /// It also allows saving the standardized prefabs as new assets for reuse
    /// </summary>
    public class GridStandardizer : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private float gridSize = 10f;
        [SerializeField] private bool centerPivot = true;
        [SerializeField] private bool createColliderIfMissing = true;
        
        [Header("Processing")]
        [SerializeField] private GameObject[] prefabsToStandardize;

        /// <summary>
        /// Standardizes an array of prefabs to align with a consistent grid layout;
        /// this involves recalculating the bounds, adjusting the pivot, centering,
        /// and optionally creating colliders and metadata, then saving the standardized version as a new prefab
        /// </summary>
        [ContextMenu("Standardize Selected Prefabs")]
        public void StandardizePrefabs()
        {
            foreach (GameObject original in prefabsToStandardize)
            {
                // Skip if the prefab is null
                if (!original) continue;

#if UNITY_EDITOR
                // Create a copy object to work on
                GameObject prefab = PrefabUtility.InstantiatePrefab(original) as GameObject;
                
                // Exit case - the prefab is null
                if (!prefab) return;
                
                // Calculate the current bounds
                Bounds bounds = CalculateBounds(prefab);
                
                // Determine the grid size (1x1, 2x2, etc.)
                int gridSizeX = Mathf.RoundToInt(bounds.size.x / gridSize);
                int gridSizeZ = Mathf.RoundToInt(bounds.size.z / gridSize);

                // Default to 1x1 if the grid size is 0
                if (gridSizeX == 0) gridSizeX = 1;
                if (gridSizeZ == 0) gridSizeZ = 1;
                
                // Calculate the target size
                Vector3 targetSize = new Vector3(
                    gridSizeX * gridSize,
                    bounds.size.y,
                    gridSizeZ * gridSize
                );
                
                // Calculate the scale factor needed
                Vector3 scaleFactor = new Vector3(
                    targetSize.x / bounds.size.x,
                    1f,
                    targetSize.z / bounds.size.z
                );
                
                // Create a container with the proper pivot
                GameObject standardized = new GameObject($"{original.name}_Standardized");
                standardized.transform.position = Vector3.zero;
                
                // Add the original as a child
                prefab.transform.SetParent(standardized.transform);
                
                // Apply scaling to the prefab
                prefab.transform.localScale = Vector3.Scale(prefab.transform.localScale, scaleFactor);
                
                // Recalculate the bounds after scaling
                bounds = CalculateBounds(prefab);
                
                // Adjust the position to align with a pivot
                Vector3 offset = new Vector3(
                    centerPivot 
                        ? -bounds.center.x
                        : -bounds.min.x,
                    -bounds.min.y,
                    centerPivot 
                        ? -bounds.center.z
                        : -bounds.min.z
                );

                // Set the local position
                prefab.transform.localPosition = offset;
                
                // Add a box collider at the correct size
                if (createColliderIfMissing && !standardized.GetComponent<Collider>())
                {
                    // Add the Box Collider component
                    BoxCollider boxCollider = standardized.AddComponent<BoxCollider>();
                    
                    // Calculate the collider size and center
                    boxCollider.size = new Vector3(
                        gridSizeX * gridSize, 
                        bounds.size.y, 
                        gridSizeZ * gridSize
                    );
                    boxCollider.center = new Vector3(
                        gridSizeX * gridSize * 0.5f, 
                        bounds.size.y * 0.5f, 
                        gridSizeZ * gridSize * 0.5f
                    );
                }
                
                // Add metadata
                TileMetadata metadata = standardized.AddComponent<TileMetadata>();
                metadata.GridSizeX = gridSizeX;
                metadata.GridSizeZ = gridSizeZ;
                metadata.ActualSize = new Vector3(
                    gridSizeX * gridSize,
                    bounds.size.y,
                    gridSizeZ * gridSize
                );
                
                // Save as a new prefab
                StringBuilder pathBuilder = new StringBuilder();
                pathBuilder.Append("Assets/Prefabs/Standardized/");
                pathBuilder.Append(original.name);
                pathBuilder.Append("_Grid.prefab");
                PrefabUtility.SaveAsPrefabAsset(standardized, pathBuilder.ToString());
                
                // Clean up
                DestroyImmediate(standardized);
                
                StringBuilder debugBuilder = new StringBuilder();
                debugBuilder.Append("Standardized ");
                debugBuilder.Append(original.name);
                debugBuilder.Append(" to ");
                debugBuilder.Append(gridSizeX);
                debugBuilder.Append("x");
                debugBuilder.Append(gridSizeZ);
                debugBuilder.Append(" tiles");
                
                Debug.Log(debugBuilder.ToString());
#endif
            }
        }
        
        /// <summary>
        /// Calculates the bounds of a given prefab by combining the bounds of all child renderers;
        /// if no renderers are present, returns a default bounds size centered around the prefab's position
        /// </summary>
        /// <param name="prefab">The GameObject prefab for which the bounds will be calculated</param>
        /// <returns>The combined bounds of all renderers in the prefab, or a default bounds if no renderers are found</returns>
        private Bounds CalculateBounds(GameObject prefab)
        {
            // Get all the renderers
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
            
            // Exit case - there are no renderers
            if (renderers.Length == 0)
                return new Bounds(prefab.transform.position, Vector3.one);

            // Sum together the bounds to calculate the total
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderComponent in renderers)
            {
                bounds.Encapsulate(renderComponent.bounds);
            }

            return bounds;
        }
    }
}