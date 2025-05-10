// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using PampelGames.Shared.Utility;
using UnityEngine;
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    internal static class IntersectionUpdate
    {
        public static void UpdateIntersections(ComponentSettings settings, SO_DefaultReferences _DefaultReferences, List<IntersectionObject> recreateIntersections)
        {
            for (int i = 0; i < recreateIntersections.Count; i++)
            {
                if (recreateIntersections[i].RoadConnections.Count != 2 &&
                    recreateIntersections[i].intersectionType == IntersectionType.Intersection)
                {
                    recreateIntersections[i].DestroySpawnedObjects();
                }
                
                var centerPosition = recreateIntersections[i].centerPosition;
                
                /********************************************************************************************************************************/
                // Roundabout Object
                if (recreateIntersections[i].intersectionType == IntersectionType.Roundabout)
                {
                    var roundabout = recreateIntersections[i] as RoundaboutObject;
                    
                    RoundaboutCreation.CreateRoundaboutMesh(settings, roundabout!.roadDescr, roundabout.centerPosition, roundabout.radius,
                        roundabout.RoadConnections, out var combinedMaterials, out var combinedMesh, out var roadSplines, out var splineMiddle, 1f);
                    
                    roundabout.meshFilter.mesh = combinedMesh;
                    roundabout.meshRenderer.materials = combinedMaterials;

                    var _splineContainer = roundabout.splineContainer;
                    for (int j = 0; j < _splineContainer.Splines.Count(); j++) _splineContainer.RemoveSpline(_splineContainer.Splines[j]);
                    
                    _splineContainer.Spline = splineMiddle;
                    for (int j = 0; j < roadSplines.Count; j++) _splineContainer.AddSpline(roadSplines[j]);
                    
                    if (settings.lodList.Count > 1) LODCreation.RoundaboutObject(settings, roundabout.roadDescr, roundabout);
                }
                
                /********************************************************************************************************************************/
                // End Object
                else if (recreateIntersections[i].RoadConnections.Count == 1)
                {
                    var roadConnection = recreateIntersections[i].RoadConnections[0];
                    var nearestKnotIndex =
                        RoadSplineUtility.GetNearestKnotIndex(roadConnection.splineContainer.Spline, centerPosition);
                    var nearestKnot = roadConnection.splineContainer.Spline.Knots.ElementAt(nearestKnotIndex);
                    var otherKnot = roadConnection.splineContainer.Spline.Knots.ElementAt(nearestKnotIndex == 1 ? 0 : 1);
                    var tangent = nearestKnot.TangentOut;
                    tangent = PGTrigonometryUtility.DirectionalTangentToPointXZ(otherKnot.Position, nearestKnot.Position, tangent);
                    
                    var endObject = RoadEndCreation.CreateEndObject(settings, _DefaultReferences, roadConnection.roadDescr,
                        true, nearestKnot.Position, tangent);
                    
                    recreateIntersections[i].centerPosition = nearestKnot.Position;

                    if (settings.lodList.Count > 1)
                    {
                        LODCreation.EndObject(settings, _DefaultReferences, roadConnection.roadDescr, endObject);
                    }

                    var intersectionMeshFilters = recreateIntersections[i].GetMeshFilters();
                    var endObjectMeshFilters = endObject.GetMeshFilters();
                    var intersectionMeshRenderers = recreateIntersections[i].GetMeshRenderers();
                    var endObjectMeshRenderers = endObject.GetMeshRenderers();

                    for (int j = 0; j < intersectionMeshFilters.Count; j++)
                    {
                        intersectionMeshFilters[j].sharedMesh = endObjectMeshFilters[j].sharedMesh;
                        if(Application.isPlaying) intersectionMeshRenderers[j].materials = endObjectMeshRenderers[j].materials;
                        else intersectionMeshRenderers[j].materials = endObjectMeshRenderers[j].sharedMaterials;
                    }

                    recreateIntersections[i].splineContainer.Spline = new Spline(endObject.splineContainer.Spline);
                    ObjectUtility.DestroyObject(endObject.gameObject);
                }
                
                /********************************************************************************************************************************/
                // Intersection
                else
                {
                    var createIntersectionMeshDatas = new List<IntersectionCreation.CreateIntersectionMeshData>(); 
                    
                    for (var j = 0; j < recreateIntersections[i].RoadConnections.Count; j++)
                    {
                        var roadConnection = recreateIntersections[i].RoadConnections[j];
                        var roadSpline = roadConnection.splineContainer.Spline;
                        var roadDescr = roadConnection.roadDescr;
                        var nearestKnotIndex = RoadSplineUtility.GetNearestKnotIndex(roadSpline, centerPosition);
                
                        createIntersectionMeshDatas.Add(new IntersectionCreation.CreateIntersectionMeshData(roadDescr, roadSpline.Knots.ElementAt(nearestKnotIndex), nearestKnotIndex));
                    }

                    var intersectionMeshes = new List<Mesh>();
                    intersectionMeshes.Add(IntersectionCreation.CreateIntersectionMesh(settings, centerPosition, 
                        createIntersectionMeshDatas, recreateIntersections[i].elevated,
                        out var newMaterials, out var newSplines, out var newPositions, 1f));
                    
                    var intersectionSplines = RoadSplineUtility.CreateIntersectionSplines(centerPosition, newPositions);

                    if (intersectionSplines.Count == 1)
                    {
                        TangentCalculation.CalculateCurvedTangents(intersectionSplines[0], settings.smoothSlope, settings.tangentLength);
                    }
                
                    if (settings.lodList.Count > 1)
                    {
                        for (var j = 1; j < settings.lodList.Count; j++)
                        {
                            var lodAmount = 1f - settings.lodList[j - 1];
                            intersectionMeshes.Add(IntersectionCreation.CreateIntersectionMesh(settings, centerPosition, 
                                createIntersectionMeshDatas, recreateIntersections[i].elevated,
                                out var _newMaterials, out var _newSplines, out var _newPositions, lodAmount));
                        }
                    }

                    var intersectionMeshFilters = recreateIntersections[i].GetMeshFilters();
                    var intersectionMeshRenderers = recreateIntersections[i].GetMeshRenderers();
                    
                    for (int j = 0; j < intersectionMeshFilters.Count; j++)
                    {
                        intersectionMeshFilters[j].sharedMesh = intersectionMeshes[j];
                        intersectionMeshRenderers[j].materials = newMaterials.ToArray();
                    }

                    var splineContainer = recreateIntersections[i].splineContainer;
                    for (var j = splineContainer.Splines.Count - 1; j >= 0; j--) splineContainer.RemoveSplineAt(j);
                    for (var j = 0; j < intersectionSplines.Count; j++) splineContainer.AddSpline(intersectionSplines[j]);
                }
            }
            
            
        }
    }
}