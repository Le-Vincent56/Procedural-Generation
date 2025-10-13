using UnityEditor;
using UnityEngine;

namespace Didionysymus.DungeonGeneration.Editor
{
    public static class AddSockets
    {
        const float Cell = 2f; // meters per cell
        
        [MenuItem("Tools/Add NSEW Sockets (1x1 cell)")]
        public static void AddNorthSoutheastWest()
        {
            GameObject gameObject = Selection.activeGameObject;
            
            // Exit case - no selection
            if (!gameObject)
            {
                Debug.LogWarning("Select a prefab root."); 
                return;
            }

            Make(gameObject, "Socket_N", new Vector3(Cell*0.5f, Cell*0.5f, Cell), Quaternion.Euler(0,   0, 0));
            Make(gameObject, "Socket_E", new Vector3(Cell,       Cell*0.5f, Cell*0.5f), Quaternion.Euler(0,  90, 0));
            Make(gameObject, "Socket_S", new Vector3(Cell*0.5f, Cell*0.5f, 0), Quaternion.Euler(0, 180, 0));
            Make(gameObject, "Socket_W", new Vector3(0,          Cell*0.5f, Cell*0.5f), Quaternion.Euler(0, 270, 0));
            Debug.Log("Added NSEW sockets to " + gameObject.name);
        }

        [MenuItem("Tools/PCG/Add Stair UD Sockets (1x1 run North)")]
        public static void AddUpDown()
        {
            GameObject gameObject = Selection.activeGameObject;
            
            // Exit case - no selection
            if (!gameObject)
            {
                Debug.LogWarning("Select a stair root."); 
                return;
            }

            Make(gameObject, "Socket_D", new Vector3(Cell*0.5f, Cell*0.5f, 0), Quaternion.Euler(0, 0, 0));
            Make(gameObject, "Socket_U", new Vector3(Cell*0.5f, Cell*0.5f, Cell), Quaternion.Euler(0, 0, 0));
            Debug.Log("Added U/D sockets to " + gameObject.name);
        }

        static void Make(GameObject root, string name, Vector3 localPos, Quaternion localRot)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(root.transform, false);
            t.localPosition = localPos;
            t.localRotation = localRot;
            t.localScale = Vector3.one;
        }
    }
}
