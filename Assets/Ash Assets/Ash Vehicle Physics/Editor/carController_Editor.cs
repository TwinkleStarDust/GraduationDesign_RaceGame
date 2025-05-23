using UnityEditor;
using UnityEngine;

namespace AshVP
{
    [CustomEditor(typeof(carController))]
    public class carContrtoller_Editor : Editor
    {
        private const string DiscordUrl = "https://discord.com/invite/zD6AhYcYdG";
        private const string TutorialUrl = "https://youtu.be/SK4WxPUUejE";
        private const string DocumentationUrl = "/Ash Assets/Ash Vehicle Physics/Documentation/Car_Controller_Documentation.pdf";
        private const string RateUrl = "https://assetstore.unity.com/packages/tools/physics/ash-vehicle-physics-187803#reviews";

        private Texture2D headerBackground;

        private void OnEnable()
        {
            // Create a white texture for the header background
            headerBackground = new Texture2D(1, 1);
            headerBackground.SetPixel(0, 0, Color.black);
            headerBackground.Apply();
        }

        private void OnDisable()
        {
            // Destroy the texture to free up memory
            DestroyImmediate(headerBackground);
        }

        public override void OnInspectorGUI()
        {
            // Define the colors
            Color primaryColor = new Color(0, 1f, 0); // Green

            // Create a header for the script with white background
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 27;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.normal.textColor = primaryColor;
            headerStyle.normal.background = headerBackground;
            headerStyle.padding = new RectOffset(1, 1, 1, 1);
            GUILayout.Space(10f);
            GUILayout.Label("Ash Vehicle Physics", headerStyle);
            GUILayout.Space(10f);

            // Create the buttons
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.fontSize = 12;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.padding = new RectOffset(5, 5, 5, 5);


            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Join Discord", null, "Join the Discord community"), buttonStyle, GUILayout.Height(20f), GUILayout.ExpandWidth(true)))
            {
                Application.OpenURL(DiscordUrl);
            }
            if (GUILayout.Button(new GUIContent("Tutorials", null, "Watch videos on YouTube"), buttonStyle, GUILayout.Height(20f), GUILayout.ExpandWidth(true)))
            {
                Application.OpenURL(TutorialUrl);
            }
            if (GUILayout.Button(new GUIContent("Documentation", null, "Read the documentation"), buttonStyle, GUILayout.Height(20f), GUILayout.ExpandWidth(true)))
            {
                string doc_path = Application.dataPath + DocumentationUrl;
                Application.OpenURL("file://" + doc_path);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Rate the Asset", null, "Rate this asset on the Unity Asset Store"), buttonStyle, GUILayout.Height(20f), GUILayout.ExpandWidth(true)))
            {
                Application.OpenURL(RateUrl);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);

            // Display all public variables of the SimcadeVehicleController script
            DrawDefaultInspector();
        }
    }
}
