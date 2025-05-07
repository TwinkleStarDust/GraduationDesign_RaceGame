// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace PampelGames.RoadConstructor
{
    public class UndoObject : MonoBehaviour
    {
        public ConstructionObjects constructionObjects;
        public TerrainUpdateUndo TerrainUpdateUndo;
    }

    public class TerrainUpdateUndo
    {
        public readonly List<Vector2Int> undoHeightPixels;
        public readonly List<float[,]> undoHeights;
        public readonly List<float[,,]> undoAlphamaps;
        public readonly List<Vector2Int> undoAlphaPixels;
        public readonly List<TerrainDetailUndo> undoDetails;
        public readonly List<TreeInstance[]> undoTrees;
        
        public TerrainUpdateUndo(List<Vector2Int> undoHeightPixels, List<float[,]> undoHeights, 
            List<Vector2Int> undoAlphaPixels, List<float[,,]> undoAlphamaps,
            List<TerrainDetailUndo> undoDetails, List<TreeInstance[]> undoTrees)
        {
            this.undoHeightPixels = undoHeightPixels;
            this.undoHeights = undoHeights;
            this.undoAlphamaps = undoAlphamaps;
            this.undoAlphaPixels = undoAlphaPixels;
            this.undoDetails = undoDetails;
            this.undoTrees = undoTrees;
        }

        public void AddUndo(TerrainUpdateUndo updateUndo)
        {
            undoHeightPixels.AddRange(updateUndo.undoHeightPixels);
            undoHeights.AddRange(updateUndo.undoHeights);
            undoAlphamaps.AddRange(updateUndo.undoAlphamaps);
            undoAlphaPixels.AddRange(updateUndo.undoAlphaPixels);
            undoDetails.AddRange(updateUndo.undoDetails);
            undoTrees.AddRange(updateUndo.undoTrees);
        }
    }

    public class TerrainDetailUndo
    {
        public Vector2Int detailPixels;
        public readonly List<int[,]> detailLayers = new();
    }

    internal static class UndoObjectUtility
    {
        public static void RegisterUndo(ComponentSettings settings, Transform undoParent, LinkedList<UndoObject> undoObjects,
            ConstructionObjects constructionObjects, TerrainUpdateUndo terrainUpdateUndo)
        {
            if (settings.undoStorageSize <= 0)
            {
                constructionObjects.DestroyRemovableObjects();
                return;
            }
            
            var undoObj = ObjectUtility.CreateUndoObject();

            var removableObjects = constructionObjects.CombinedRemovableObjects;
            
            foreach (var removableObject in removableObjects)
            {
                removableObject.transform.SetParent(undoObj.transform);
                removableObject.gameObject.SetActive(false);
            }

            undoObj.transform.SetParent(undoParent);
            var undoObject = undoObj.AddComponent<UndoObject>();

            undoObject.constructionObjects = constructionObjects;
            undoObject.TerrainUpdateUndo = terrainUpdateUndo;

            undoObjects.AddLast(undoObject);
            if (undoObjects.Count > settings.undoStorageSize)
            {
                var dequeuedUndo = undoObjects.First.Value;
                ObjectUtility.DestroyObject(dequeuedUndo.gameObject);
                undoObjects.RemoveFirst();
            }
        }

    }
    
}