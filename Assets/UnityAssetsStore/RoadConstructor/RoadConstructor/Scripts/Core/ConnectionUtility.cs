// ----------------------------------------------------
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace PampelGames.RoadConstructor
{
    public static class ConnectionUtility
    {
        private const float Tolerance = 0.01f;

        public static void UpdateConnections(RoadConstructor.SceneData sceneData, ConstructionObjects constructionObjects)
        {
            var removableObjects = constructionObjects.CombinedRemovableObjects;
            ClearConnections(removableObjects);

            var sceneObjects = constructionObjects.CombinedNewObjects;

            var checkedIDs = new HashSet<Vector2Int>();

            for (var i = 0; i < sceneObjects.Count; i++)
            {
                var sceneObject = sceneObjects[i];
                
                var roadDescr = sceneObject.roadDescr;
                var settings = roadDescr.settings;
                var bounds = WorldUtility.ExtendBounds(sceneObject.Bounds, math.max(settings.minOverlapDistance, Tolerance));
                var uniqueKnots = RoadSplineUtility.GetUniqueKnots(sceneObject.splineContainer);

                var ignoreObjects = new List<SceneObject>(removableObjects) {sceneObject};

                OverlapUtility.GetAllOverlaps(bounds, ignoreObjects, sceneData,
                    out var overlapIntersections, out var overlapRoads);

                var overlapObjects = new List<SceneObject>(overlapIntersections);
                overlapObjects.AddRange(overlapRoads);

                for (var j = 0; j < overlapObjects.Count; j++)
                {
                    var overlapObject = overlapObjects[j];
                    var id1 = sceneObject.GetInstanceID();
                    var id2 = overlapObject.GetInstanceID();
                    var connectionID = new Vector2Int(Mathf.Min(id1, id2), Mathf.Max(id1, id2));
                    if (!checkedIDs.Add(connectionID)) continue;

                    var overlapUniqueKnots = RoadSplineUtility.GetUniqueKnots(overlapObject.splineContainer);

                    for (var k = 0; k < uniqueKnots.Count; k++)
                    for (var l = 0; l < overlapUniqueKnots.Count; l++)
                    {
                        if (Mathf.Abs(uniqueKnots[k].Position.x - overlapUniqueKnots[l].Position.x) > Tolerance) continue;
                        if (Mathf.Abs(uniqueKnots[k].Position.z - overlapUniqueKnots[l].Position.z) > Tolerance) continue;
                        if (Mathf.Abs(uniqueKnots[k].Position.y - overlapUniqueKnots[l].Position.y) > Tolerance) continue;

                        sceneObject.AddConnection(overlapObject);
                        overlapObject.AddConnection(sceneObject);

                        break;
                    }
                }
            }
        }
        
        public static void CleanNullConnections<T>(List<T> sceneObjects) where T : SceneObject
        {
            for (int i = 0; i < sceneObjects.Count; i++)
            {
                sceneObjects[i].RoadConnections.RemoveAll(connection => connection == null);
                sceneObjects[i].IntersectionConnections.RemoveAll(intersection => intersection == null);
            }
        }
        
        /********************************************************************************************************************************/

        private static void ClearConnections(List<SceneObject> sceneObjects)
        {
            for (var i = 0; i < sceneObjects.Count; i++)
            {
                var sceneObject = sceneObjects[i];

                if (sceneObject is RoadObject roadObject)
                {
                    var intersectionConnections = roadObject.IntersectionConnections;
                    for (var j = 0; j < intersectionConnections.Count; j++) intersectionConnections[j].RoadConnections.Remove(roadObject);
                    var roadConnections = roadObject.RoadConnections;
                    for (var j = 0; j < roadConnections.Count; j++) roadConnections[j].RoadConnections.Remove(roadObject);
                }

                if (sceneObject is IntersectionObject intersectionObject)
                {
                    var roadConnections = intersectionObject.RoadConnections;
                    for (var j = 0; j < roadConnections.Count; j++) roadConnections[j].IntersectionConnections.Remove(intersectionObject);
                    var intersectionConnections = intersectionObject.IntersectionConnections;
                    for (var j = 0; j < intersectionConnections.Count; j++)
                        intersectionConnections[j].IntersectionConnections.Remove(intersectionObject);
                }

                sceneObject.ClearConnections();
            }
        }
        
    }
}