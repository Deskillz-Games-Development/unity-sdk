// =============================================================================
// Deskillz SDK -- Editor menu  [P2-3.7.0]
// Deskillz > Create Config Asset : creates Assets/Resources/DeskillzConfig.asset,
// which DeskillzConfig.Instance loads at runtime.
// =============================================================================
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deskillz.Editor
{
    public static class DeskillzMenu
    {
        private const string AssetPath = "Assets/Resources/DeskillzConfig.asset";

        [MenuItem("Deskillz/Create Config Asset", priority = 1)]
        public static void CreateConfigAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DeskillzConfig>(AssetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                Debug.Log("[Deskillz] DeskillzConfig already exists at " + AssetPath);
                return;
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            var config = ScriptableObject.CreateInstance<DeskillzConfig>();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            Debug.Log("[Deskillz] Created " + AssetPath + " -- fill in your API key and Game ID from deskillz.games/developer");
        }

        [MenuItem("Deskillz/Open Developer Portal", priority = 20)]
        public static void OpenPortal() => Application.OpenURL("https://deskillz.games/developer");

        [MenuItem("Deskillz/Documentation", priority = 21)]
        public static void OpenDocs() => Application.OpenURL("https://deskillz.games/docs");
    }
}
