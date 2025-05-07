// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace PampelGames.RoadConstructor
{
    internal static class UndoConstruction
    {
        public static void SaveCurrentState(ComponentSettings settings)
        {
            if (Application.isPlaying) return;
#if UNITY_EDITOR
            if(settings.terrain != null && settings.levelHeight)
            {
                Undo.RegisterCompleteObjectUndo(settings.terrain.terrainData, "TerrainUndo");
                Undo.RegisterCompleteObjectUndo(settings.terrain.terrainData.alphamapTextures, "TerrainUndo");
            }
#endif

        }
    }
}
