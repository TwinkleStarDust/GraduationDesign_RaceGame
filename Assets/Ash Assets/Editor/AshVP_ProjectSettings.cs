using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace AshVP
{
    public class AshVP_ProjectSettings : EditorWindow
    {
        public static bool AshVP_ProjectSettings_Imported = false;

        public class ImportAssetPrompt : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (AshVP_ProjectSettings_Imported)
                {
                    return;
                }

                foreach (string assetPath in importedAssets)
                {
                    if (assetPath.Contains("Assets/Ash Assets/Project Settings")) // Adjust to match your asset's folder
                    {
                        ShowWindow();
                        break;
                    }
                }
            }
        }

        public static void ShowWindow()
        {
            AshVP_ProjectSettings window = GetWindow<AshVP_ProjectSettings>("Import Project Settings");
            window.minSize = new Vector2(300, 150);
            window.Focus(); // Bring the window to the front
        }

        private void OnGUI()
        {
            GUILayout.Label("Import Project Settings", EditorStyles.boldLabel);
            GUILayout.Label("Would you like to import the project settings required for Ash Vehicle Physics/Ai?", EditorStyles.wordWrappedLabel);

            GUILayout.Space(20);

            if (GUILayout.Button("Yes, Import Settings"))
            {
                ImportProjectSettings();
                AshVP_ProjectSettings_Imported = true; // Mark as imported
                Close();
            }

            if (GUILayout.Button("No, Skip"))
            {
                AshVP_ProjectSettings_Imported = true; // Mark as imported, even if skipped
                Close();
            }
        }

        private static void ImportProjectSettings()
        {
            ApplyPreset("Assets/Ash Assets/Project Settings/Physics_Preset.preset", "ProjectSettings/DynamicsManager.asset");
            ApplyPreset("Assets/Ash Assets/Project Settings/Tag_And_Layer_Preset.preset", "ProjectSettings/TagManager.asset");

            Debug.Log("Project settings have been successfully imported.");
        }

        private static void ApplyPreset(string presetPath, string settingsPath)
        {
            var preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);
            if (preset == null)
            {
                Debug.LogWarning($"Preset not found at path: {presetPath}");
                return;
            }

            var settingsAsset = AssetDatabase.LoadAssetAtPath<Object>(settingsPath);
            if (settingsAsset == null)
            {
                Debug.LogWarning($"Settings not found at path: {settingsPath}");
                return;
            }

            if (preset.ApplyTo(settingsAsset))
            {
                Debug.Log($"Preset applied successfully to {settingsPath}");
            }
            else
            {
                Debug.LogWarning($"Failed to apply preset to {settingsPath}");
            }
        }
    }
}