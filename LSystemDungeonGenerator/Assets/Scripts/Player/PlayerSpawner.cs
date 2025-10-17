using Didionysymus.DungeonGeneration.LSystem;
using UnityEngine;


namespace Didionysymus.DungeonGeneration.Player
{
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private DungeonGenerator Generator;
        [SerializeField] private GameObject FirstPersonPlayerPrefab;
        [SerializeField] private bool SpawnOnAwake = true;
        [SerializeField] private bool AlignToRoomFacing = false;

        private void OnEnable()
        {
            if (!Generator) return;
            Generator.OnDungeonGenerated += Spawn;
        }

        private void OnDisable()
        {
            if (!Generator) return;

            Generator.OnDungeonGenerated -= Spawn;
        }

        private void Spawn()
        {
            if (!Generator || !FirstPersonPlayerPrefab)
            {
                Debug.LogWarning("Cannot spawn player");
                return;
            }

            // Get the starting room
            RoomData start = Generator.GetStartRoom();
            if (start == null)
            {
                // Fallback to the room nearest the grid center if no starting room
                Vector2Int gridCenter = Generator.Configuration.MaxGridSize / 2;
                start = Generator.Rooms[0];
                float best = float.PositiveInfinity;
                foreach (RoomData r in Generator.Rooms)
                {
                    float distance = Vector2Int.Distance(gridCenter, r.GetCenter());
                    if (distance < best)
                    {
                        best = distance; 
                        start = r;
                    }
                }
            }
            
            // Compute a good spawn height
            DungeonConfig config = Generator.Configuration;
            Vector3 world = start.GetWorldCenter(config.CellSize, config.FloorHeightUnits);
            
            // Lift the spawn by half a meter to prevent clipping with the floor
            world.y += 0.5f;
            
            // Instantiate the player
            GameObject player = Instantiate(FirstPersonPlayerPrefab, world, Quaternion.identity);
            
            // Align to face in the direction of a doorway
            if (AlignToRoomFacing && start.DoorPositions.Count > 0)
            {
                // Look toward the first door’s outside cell center
                Vector2Int doorCell = start.DoorPositions[0];
                Vector3 doorWorld = new Vector3(doorCell.x * config.CellSize, world.y, doorCell.y * config.CellSize);
                Vector3 dir = (doorWorld - world); dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }
    }
}
