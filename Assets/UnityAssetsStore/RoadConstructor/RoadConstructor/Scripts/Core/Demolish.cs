// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using PampelGames.Shared.Utility;
using Unity.Mathematics;
using UnityEngine;

namespace PampelGames.RoadConstructor
{
    internal static class Demolish
    {
        public static void GetDemolishSceneObjects(ComponentSettings componentSettings, float3 position, float radius, 
            RoadConstructor.SceneData sceneData,
            out List<IntersectionObject> demolishIntersections, out List<RoadObject> demolishRoads, out Overlap overlap)
        {
            demolishIntersections = new List<IntersectionObject>();
            demolishRoads = new List<RoadObject>();
            var settings = componentSettings;
            
            overlap = OverlapUtility.GetOverlap(settings, radius, settings.snapHeight * 2f, position, sceneData);
            
            if (!overlap.exists) return;
            
            if (overlap.roadType == RoadType.Intersection)
            {
                demolishIntersections.Add(overlap.intersectionObject);
            }
            else
            {
                demolishRoads.Add(overlap.roadObject);
                for (int i = 0; i < overlap.roadObject.RoadConnections.Count; i++)
                {
                    var roadConnection = overlap.roadObject.RoadConnections[i];
                    if(!roadConnection.snapPositionSet) continue;
                    demolishRoads.Add(roadConnection);
                }
            }
        }
        
        public static void UpdateSceneObjects(ComponentSettings settings, SO_DefaultReferences _DefaultReferences,
            List<IntersectionObject> demolishIntersections, List<RoadObject> demolishRoads, 
            out List<IntersectionObject> recreateIntersections)
        {
            for (int i = 0; i < demolishIntersections.Count; i++)
            {
                for (int j = 0; j < demolishIntersections[i].RoadConnections.Count; j++)
                {
                    var roadConnection = demolishIntersections[i].RoadConnections[j];
                    if(!demolishRoads.Contains(roadConnection)) demolishRoads.Add(roadConnection);
                }
            }

            /********************************************************************************************************************************/
            // Recreate Intersections
            recreateIntersections = new List<IntersectionObject>();

            for (int i = 0; i < demolishRoads.Count; i++)
            {
                for (int j = 0; j < demolishRoads[i].IntersectionConnections.Count; j++)
                {
                    var intersectionConnection = demolishRoads[i].IntersectionConnections[j];
                    
                    if(demolishIntersections.Contains(intersectionConnection)) continue;
                    if(recreateIntersections.Contains(intersectionConnection)) continue;
                    
                    if (intersectionConnection.intersectionType == IntersectionType.Roundabout)
                    {
                        recreateIntersections.Add(intersectionConnection);
                        continue;
                    }
                    
                    if(intersectionConnection.RoadConnections.Count == 1)
                    {
                        demolishIntersections.Add(intersectionConnection);
                        continue;
                    }
                    recreateIntersections.Add(intersectionConnection);
                }
            }
            
            for (int i = 0; i < recreateIntersections.Count; i++)
            {
                for (int j = recreateIntersections[i].RoadConnections.Count - 1; j >= 0; j--)
                {
                    if(demolishRoads.Contains(recreateIntersections[i].RoadConnections[j]))
                    {
                        recreateIntersections[i].RemoveRoadConnection(recreateIntersections[i].RoadConnections[j]);
                    }
                }
            }
            
            // Only updating existing meshes and materials.
            IntersectionUpdate.UpdateIntersections(settings, _DefaultReferences, recreateIntersections);
            
        }
        
    }
}