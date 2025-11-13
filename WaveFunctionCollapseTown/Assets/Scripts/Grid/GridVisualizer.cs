using Unity.VisualScripting;
using UnityEngine;

namespace Didionysymus.WaveFunctionCollapse.Grid
{
    [ExecuteInEditMode]
    public class GridVisualizer : MonoBehaviour
    {
        [SerializeField] private float gridSize = 10f;
        [SerializeField] private int gridWidth = 50;
        [SerializeField] private int gridDepth = 50;
        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private bool showGrid = true;
        [SerializeField] private bool showLabels = false;

        private void OnDrawGizmos()
        {
            // Exit case - not showing the grid
            if (!showGrid) return;

            Gizmos.color = gridColor;
            
            // Draw grid lines
            for(int x = 0; x < gridWidth; x++)
            {
                Vector3 start = transform.position + new Vector3(x * gridSize, 0, 0);
                Vector3 end = start + new Vector3(0, 0, gridDepth * gridSize);
                Gizmos.DrawLine(start, end);
            }

            for (int z = 0; z < gridDepth; z++)
            {
                Vector3 start = transform.position + new Vector3(0, 0, z * gridSize);
                Vector3 end = start + new Vector3(gridWidth * gridSize, 0, 0);
                Gizmos.DrawLine(start, end);
            }
            
            // Draw cell centers
            if (showLabels)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int z = 0; z < gridDepth; z++)
                    {
                        Vector3 center = transform.position + new Vector3(
                            (x + 0.5f) * gridSize, 
                            0f, 
                            (z + 0.5f) * gridSize
                        );
                        Gizmos.DrawWireCube(center, Vector3.one);
                    }
                }
            }
        }
    }
}