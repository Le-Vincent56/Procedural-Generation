using System.Text;
using Didionysymus.WaveFunctionCollapse.Core;
using Didionysymus.WaveFunctionCollapse.Data;
using UnityEngine;

namespace Didionysymus.WaveFunctionCollapse.Utilities
{
    public class DirectionTester : MonoBehaviour
    {
        [Header("Testing")]
        [SerializeField] private TileData tileToTest;
        [SerializeField] private int testRotation = 0;
        
        [Header("Visualization")]
        [SerializeField] private float arrowLength = 5f;
        [SerializeField] private bool showLabels = true;

        private static readonly string[] _directionNames =
        {
            "North",
            "East",
            "South",
            "West"
        };

        private static readonly Color[] _directionColors =
        {
            Color.blue,
            Color.red,
            Color.yellow,
            Color.green
        };

        void OnDrawGizmos()
        {
            // Exit case - the tile is null
            if (!tileToTest) return;
            
            // Draw arrows for each direction
            for (int direction = 0; direction < 4; direction++)
            {
                Vector3 worldDirection = GetWorldDirection(direction);
                TileSocket socket = tileToTest.GetSocketForDirection(direction, testRotation);

                // Skip if the socket is null
                if (socket == null) continue;
                
                // Color by socket type
                Gizmos.color = _directionColors[direction];
                
                // Draw an arrow from the center
                Vector3 start = transform.position;
                Vector3 end = start + worldDirection * arrowLength;
                
                Gizmos.DrawLine(start, end);
                DrawArrowHead(start, end, 1f);
                
                // Draw label
                if (showLabels)
                {
#if UNITY_EDITOR
                    StringBuilder labelBuilder = new StringBuilder();
                    labelBuilder.AppendLine(_directionNames[direction]);
                    labelBuilder.AppendLine(socket.SocketType.ToString());
                    
                    UnityEditor.Handles.Label(
                        end + Vector3.up * 0.5f, 
                        labelBuilder.ToString()
                    );
#endif
                }
            }
        }
        private Vector3 GetWorldDirection(int direction)
        {
            return direction switch
            {
                0 => Vector3.forward,   // North = +Z
                1 => Vector3.right,     // East = +X
                2 => Vector3.back,      // South = -Z
                3 => Vector3.left,      // West = -X
                _ => Vector3.zero
            };
        }

        /// <summary>
        /// Draws the triangular arrowhead for a line, indicating its direction.
        /// </summary>
        /// <param name="start">The start position of the line.</param>
        /// <param name="end">The end position of the line where the arrowhead will be drawn.</param>
        /// <param name="headSize">The size of the arrowhead. Defaults to 0.2f.</param>
        private void DrawArrowHead(Vector3 start, Vector3 end, float headSize = 0.2f)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

            Vector3 arrowTip = end;
            Vector3 arrowLeft = end - direction * headSize + right * headSize * 0.5f;
            Vector3 arrowRight = end - direction * headSize - right * headSize * 0.5f;
            
            Gizmos.DrawLine(arrowTip, arrowLeft);
            Gizmos.DrawLine(arrowTip, arrowRight);
        }
    }
}
