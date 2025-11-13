using UnityEngine;

namespace Didionysymus.WaveFunctionCollapse.Grid
{
    public class TileMetadata : MonoBehaviour
    {
        public int GridSizeX = 1;
        public int GridSizeZ = 1;
        public Vector3 ActualSize;
        public Transform[] ConnectionPoints;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Vector3 size = new Vector3(GridSizeX * 5f, 0.1f, GridSizeZ * 5f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}