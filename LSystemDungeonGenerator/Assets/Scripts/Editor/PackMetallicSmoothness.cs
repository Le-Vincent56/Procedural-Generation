using System.IO;
using UnityEditor;
using UnityEngine;

namespace Didionysymus.DungeonGeneration.Editor
{
    public class PackMetallicSmoothness : EditorWindow
    {
        private Texture2D _metalTex;
        private Texture2D _roughTex;
        private string _outputName = "MetallicSmoothness";
        
        [MenuItem("Tools/Pack Metallic and Smoothness")]
        private static void Open() => GetWindow<PackMetallicSmoothness>("Pack Metallic and Smoothness");

        private void OnGUI()
        {
            _metalTex = (Texture2D)EditorGUILayout.ObjectField(
                "Metal (grayscale)", 
                _metalTex, 
                typeof(Texture2D),
                false
            );
            _roughTex = (Texture2D)EditorGUILayout.ObjectField(
                "Roughness (grayscale)", 
                _roughTex, 
                typeof(Texture2D),
                false
            );
            _outputName = EditorGUILayout.TextField("Output Name", _outputName);

            using (new EditorGUI.DisabledScope(!_metalTex || !_roughTex))
            {
                if (GUILayout.Button("Create Packed Texture"))
                    CreatePacked();
            }
        }

        private void CreatePacked()
        {
            int width = _metalTex.width;
            int height = _metalTex.height;
            
            // Ensure read/write
            MakeReadable(_metalTex);
            MakeReadable(_roughTex);
            
            Color[] metalPixels = _metalTex.GetPixels();
            Color[] roughPixels = _roughTex.GetPixels();
            
            Texture2D outputTexture = new Texture2D(width, height, TextureFormat.RGBA32, true, true);
            Color32[] outputPixels = new Color32[width * height];

            // Pack the metal and roughness into a single texture
            for (int i = 0; i < outputPixels.Length; i++)
            {
                byte metal = (byte)Mathf.Clamp(metalPixels[i].r * 255f, 0f, 255f);
                byte rough = (byte)Mathf.Clamp(roughPixels[i].r * 255f, 0f, 255f);
                byte smooth = (byte)(255 - rough);
                outputPixels[i] = new Color32(metal, 0, 0, smooth);
            }
            
            // Apply the blended pixels to the texture
            outputTexture.SetPixels32(outputPixels);
            outputTexture.Apply(true, false);

            // Save the texture to disk
            string path = AssetDatabase.GetAssetPath(_metalTex);
            string directory = Path.GetDirectoryName(path);
            string savePath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{_outputName}.png");
            File.WriteAllBytes(savePath, outputTexture.EncodeToPNG());
            AssetDatabase.ImportAsset(savePath);
            
            // Set import flags
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(savePath);
            importer.sRGBTexture = false;
            importer.SaveAndReimport();

            EditorUtility.DisplayDialog("Packed", $"Created: {savePath}", "OK");
        }

        private static void MakeReadable(Texture2D texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (!importer) return;

            // Exit case - already readable and there is already has no sRGB texture
            if (importer.isReadable && !importer.sRGBTexture) return;
            
            // Make the texture readable
            importer.isReadable = true;
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }
    }
}
