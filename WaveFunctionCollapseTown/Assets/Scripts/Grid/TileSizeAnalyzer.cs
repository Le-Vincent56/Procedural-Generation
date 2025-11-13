using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Didionysymus.WaveFunctionCollapse.Grid
{
    /// <summary>
    /// A utility class to analyze the tile sizes of prefabs in relation to a target grid size
    /// </summary>
    public class TileSizeAnalyzer : MonoBehaviour
    {
        [Serializable]
        public class TileMeasurement
        {
            public GameObject Prefab;
            public Vector3 BoundsSize;
            public Vector3 PivotPosition;
            public bool FitsGrid;
            public string Notes;
        }
        
        [Header("Analysis Settings")]
        [SerializeField] private float targetGridSize = 10f;
        [SerializeField] private float tolerance = 0.1f;
        
        [Header("Prefabs to Analyze")]
        [SerializeField] private GameObject[] prefabsToCheck;

        [Header("Results")] 
        [SerializeField] private List<TileMeasurement> measurements = new List<TileMeasurement>();

        /// <summary>
        /// Analyzes a list of prefab GameObjects to determine their bounds, pivot positions,
        /// and compatibility with a predetermined grid size; stores the results in a measurement list
        /// and logs individual analysis results to the console
        /// </summary>
        [ContextMenu("Analyze Prefab Sizes")]
        public void AnalyzePrefabs()
        {
            // Clear the current measurements
            measurements.Clear();

            foreach (GameObject prefab in prefabsToCheck)
            {
                // Skip if the prefab is null
                if (!prefab) continue;

                // Create the Tile Measurement and set the prefab
                TileMeasurement measurement = new TileMeasurement
                {
                    Prefab = prefab
                };
                
                // Get bounds
                Bounds bounds = CalculateBounds(prefab);
                measurement.BoundsSize = bounds.size;
                measurement.PivotPosition = prefab.transform.position;
                
                // Check if the tile fits the grid
                bool fitsX = Mathf.Abs(bounds.size.x % targetGridSize) < tolerance ||
                             Mathf.Abs(bounds.size.x / targetGridSize -  Mathf.Round(bounds.size.x / targetGridSize)) < tolerance;
                bool fitsZ = Mathf.Abs(bounds.size.z % targetGridSize) < tolerance ||
                             Mathf.Abs(bounds.size.z / targetGridSize - Mathf.Round(bounds.size.z / targetGridSize)) < tolerance;
                measurement.FitsGrid = fitsX && fitsZ;
                
                // Generate notes
                float tilesX = bounds.size.x / targetGridSize;
                float tilesZ = bounds.size.z / targetGridSize;

                
                StringBuilder noteBuilder = new StringBuilder();
                noteBuilder.Append("Occupied ");
                noteBuilder.Append(tilesX.ToString("F1"));
                noteBuilder.Append("x");
                noteBuilder.Append(tilesZ.ToString("F1"));
                noteBuilder.Append(" tiles");

                if (!measurement.FitsGrid)
                    noteBuilder.Append(" - Needs adjustment");
                
                measurement.Notes = noteBuilder.ToString();
                
                // Add the measurement to the list
                measurements.Add(measurement);
            }
            
            // Report results
            Debug.Log($"Analyzed {measurements.Count} prefabs: ");
            StringBuilder debugBuilder = new StringBuilder();
            foreach (TileMeasurement measurement in measurements)
            {
                // Reset the string builder
                debugBuilder.Clear();
                
                // Get the status message
                string status = measurement.FitsGrid ? "[SUCCESS]" : "[FAILURE]";

                // Build the debug message
                debugBuilder.Append(status);
                debugBuilder.Append(" ");
                debugBuilder.Append(measurement.Prefab.name);
                debugBuilder.Append(": ");
                debugBuilder.Append(measurement.BoundsSize);
                debugBuilder.Append(" - ");
                debugBuilder.Append(measurement.Notes);
                
                Debug.Log(debugBuilder.ToString());
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
                return new Bounds(prefab.transform.position, Vector3.one * targetGridSize);

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
