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
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    internal static class LODCreation
    {
        public static void RoadObject(ComponentSettings settings, RoadObject roadObject)
        {
            if (roadObject.TryGetComponent<LODGroup>(out _))
            {
                return;
            }
            
            var roadDescr = roadObject.roadDescr;
            
            var firstChild = ObjectUtility.CreateLODObject(roadDescr.road.shadowCastingMode, roadObject.gameObject, 0,
                out var meshFilter, out var meshRenderer);
            firstChild.transform.SetParent(roadObject.transform);

            meshFilter.sharedMesh = roadObject.meshFilter.sharedMesh;
            if(Application.isPlaying) meshRenderer.materials = roadObject.meshRenderer.materials;
            else meshRenderer.materials = roadObject.meshRenderer.sharedMaterials;
            ObjectUtility.DestroyObject(roadObject.meshFilter);
            ObjectUtility.DestroyObject(roadObject.meshRenderer);
            roadObject.meshFilter = meshFilter;
            roadObject.meshRenderer = meshRenderer;
            roadObject.meshFilterLODs.Add(meshFilter);
            roadObject.meshRendererLODs.Add(meshRenderer);
                
            
            if (roadObject.gameObject.TryGetComponent<LODGroup>(out var lodGroup))
            {
                var existingLods = lodGroup.GetLODs();
                for (int i = 0; i < existingLods.Length; i++)
                {
                    if(existingLods[i].renderers.Length > 0)
                    {
                        ObjectUtility.DestroyObject(existingLods[i].renderers[0].gameObject);
                    }
                }
            }
            else lodGroup = roadObject.gameObject.AddComponent<LODGroup>();
            
            var lodList = settings.lodList;
            var lods = new LOD[lodList.Count + 1];
            lods[0] = new LOD(1f - lodList[0], new Renderer[] {meshRenderer});
            
            for (var i = 1; i < lodList.Count; i++)
            {
                var lodAmount = 1f - lodList[i - 1];
                var lodRoadObject = RoadCreation.CreateRoad(roadDescr, roadObject.splineContainer.Spline, roadObject.elevated, lodAmount);
                lodRoadObject.name = roadObject.gameObject.name + "_LOD" + i;
                
                lods[i] = new LOD(1f - lodList[i], new Renderer[] {lodRoadObject.meshRenderer});
                    
                roadObject.meshFilterLODs.Add(lodRoadObject.meshFilter);
                roadObject.meshRendererLODs.Add(lodRoadObject.meshRenderer);
                
                lodRoadObject.transform.SetParent(roadObject.transform);
                ObjectUtility.DestroyObject(lodRoadObject);
            }
            
            lodGroup.SetLODs(lods);
        }

        public static void IntersectionObject(ComponentSettings settings, IntersectionObject newIntersection, List<RoadObject> newReplacedRoads)
        {
            if (newIntersection.TryGetComponent<LODGroup>(out _))
            {
                return;
            }
            
            var roadDescr = newIntersection.roadDescr;
            
            var roadLodGroups = new List<LODGroup>();
            var roadLODs = new List<LOD[]>();

            var lodList = settings.lodList;


            var firstChild = ObjectUtility.CreateLODObject(roadDescr.road.shadowCastingMode, newIntersection.gameObject, 0,
                out var meshFilter, out var meshRenderer);
            firstChild.transform.SetParent(newIntersection.transform);
            meshFilter.sharedMesh = newIntersection.meshFilter.sharedMesh;
            if(Application.isPlaying) meshRenderer.materials = newIntersection.meshRenderer.materials;
            else meshRenderer.materials = newIntersection.meshRenderer.sharedMaterials;
            ObjectUtility.DestroyObject(newIntersection.meshFilter);
            ObjectUtility.DestroyObject(newIntersection.meshRenderer);
            newIntersection.meshFilter = meshFilter;
            newIntersection.meshRenderer = meshRenderer;
            newIntersection.meshFilterLODs.Add(meshFilter);
            newIntersection.meshRendererLODs.Add(meshRenderer);
                
            
            var intersectionLodGroup = newIntersection.gameObject.AddComponent<LODGroup>();
            var intersectionLOD = new LOD[lodList.Count + 1];
            intersectionLOD[0] = new LOD(1f - lodList[0], new Renderer[] {meshRenderer});
            
            
            for (var i = 0; i < newReplacedRoads.Count; i++)
            {
                // var newRoad = newReplacedRoads[i];
                // var _firstChild = ObjectUtility.CreateLODObject(newRoad.roadDescr.road.shadowCastingMode, newRoad.gameObject, 0,
                //     out var _meshFilter, out var _meshRenderer);
                // _firstChild.transform.SetParent(newRoad.transform);
                // _meshFilter.sharedMesh = newRoad.meshFilter.sharedMesh;
                // if(Application.isPlaying) _meshRenderer.materials = newRoad.meshRenderer.materials;
                // else _meshRenderer.materials = newRoad.meshRenderer.sharedMaterials;
                // ObjectUtility.DestroyObject(newRoad.meshFilter);
                // ObjectUtility.DestroyObject(newRoad.meshRenderer);
                // newRoad.meshFilter = _meshFilter;
                // newRoad.meshRenderer = _meshRenderer;
                // newRoad.meshFilterLODs.Add(_meshFilter);
                // newRoad.meshRendererLODs.Add(_meshRenderer);
                //
                // roadLodGroups.Add(newRoad.gameObject.AddComponent<LODGroup>());
                // var _lods = new LOD[lodList.Count + 1];
                // _lods[0] = new LOD(1f - lodList[0], new Renderer[] {_meshRenderer});
                // roadLODs.Add(_lods);
            }


            var roadConnections = newIntersection.RoadConnections;
            var createIntersectionMeshDatas = new List<IntersectionCreation.CreateIntersectionMeshData>();
            for (int i = 0; i < roadConnections.Count; i++)
            {
                var roadSpline = roadConnections[i].splineContainer.Spline;
                var nearestKnotIndex = RoadSplineUtility.GetNearestKnotIndex(roadSpline, newIntersection.centerPosition);
                var nearestKnot = roadSpline[nearestKnotIndex];
                createIntersectionMeshDatas.Add(new IntersectionCreation.CreateIntersectionMeshData(roadConnections[i].roadDescr, nearestKnot, nearestKnotIndex));
            }
            
            
            for (var i = 1; i < lodList.Count; i++)
            {
                var lodAmount = 1f - lodList[i - 1];
                
                var intersectionMesh = IntersectionCreation.CreateIntersectionMesh(settings, newIntersection.centerPosition,
                    createIntersectionMeshDatas, newIntersection.elevated,
                    out var newMaterials, out var newSplines, out var newPositions, lodAmount);
                
                IntersectionCreation.CreateIntersectionObject(newIntersection.roadDescr.road,
                    out var lodIntersection, out var splineContainer, out var _meshFilter, out var _meshRenderer);
                
                _meshFilter.mesh = intersectionMesh;
                _meshRenderer.materials = newMaterials.ToArray();
                
                intersectionLOD[i] = new LOD(1f - lodList[i], new Renderer[] {_meshRenderer});
                lodIntersection.name = newIntersection.name + "_LOD" + (i + 1);
                lodIntersection.transform.SetParent(newIntersection.transform);
                newIntersection.meshFilterLODs.Add(lodIntersection.meshFilter);
                newIntersection.meshRendererLODs.Add(lodIntersection.meshRenderer);
                ObjectUtility.DestroyObject(lodIntersection.splineContainer);
                ObjectUtility.DestroyObject(lodIntersection);
            }

            intersectionLodGroup.SetLODs(intersectionLOD);
        }
        
        public static void EndObject(ComponentSettings settings, SO_DefaultReferences _DefaultReferences,
            RoadDescr roadDescr, IntersectionObject endObject)
        {
            if (endObject.TryGetComponent<LODGroup>(out _))
            {
                return;
            }
            
            var position = endObject.centerPosition;
            var forward = -endObject.splineContainer.Spline.Knots.First().TangentOut;
            
            PrepareIntersection(roadDescr, endObject);
            
            var lodList = settings.lodList;
            var lodGroup = endObject.gameObject.AddComponent<LODGroup>();
            var lods = new LOD[lodList.Count + 1];
            lods[0] = new LOD(1f - lodList[0], new Renderer[] {endObject.meshRenderer});

            var originalResolution = roadDescr.resolution;
            var originalDetailResolution = roadDescr.detailResolution;

            for (var i = 1; i < lodList.Count; i++)
            {
                roadDescr.resolution = (int) math.round(originalResolution * (1f - lodList[i - 1]));
                roadDescr.detailResolution = (int) math.round(originalDetailResolution * (1f - lodList[i - 1]));

                var lodEndObject = RoadEndCreation.CreateEndObject(settings, _DefaultReferences, roadDescr, true, position, forward);
                lodEndObject.name = endObject.name + "_LOD" + i;
                ObjectUtility.DestroyObject(lodEndObject.splineContainer);
                var endObj = lodEndObject.gameObject;
                
                lods[i] = new LOD(1f - lodList[i], new Renderer[] {lodEndObject.meshRenderer});
                
                endObject.meshFilterLODs.Add(lodEndObject.meshFilter);
                endObject.meshRendererLODs.Add(lodEndObject.meshRenderer);
                
                endObj.transform.SetParent(endObject.transform);
                ObjectUtility.DestroyObject(lodEndObject);
            }

            roadDescr.resolution = originalResolution;
            roadDescr.detailResolution = originalDetailResolution;

            lodGroup.SetLODs(lods);
        }
        
        public static void RoundaboutObject(ComponentSettings settings,
            RoadDescr roadDescr, RoundaboutObject roundabout)
        {
            PrepareIntersection(roadDescr, roundabout);
            
            if (!roundabout.gameObject.TryGetComponent<LODGroup>(out var lodGroup))
                lodGroup = roundabout.gameObject.AddComponent<LODGroup>();
            
            var lodList = settings.lodList;
            var lods = new LOD[lodList.Count + 1];
            lods[0] = new LOD(1f - lodList[0], new Renderer[] {roundabout.meshRenderer});

            for (var i = 1; i < lodList.Count; i++)
            {
                var lodAmount = 1f - lodList[i - 1];
                
                RoundaboutCreation.CreateRoundaboutMesh(settings, roundabout.roadDescr, roundabout.centerPosition, roundabout.radius,
                    roundabout.RoadConnections, out var combinedMaterials, out var combinedMesh, out var roadSplines, out var splineMiddle, lodAmount);
                
                var roundaboutObj = new GameObject(roundabout.name + "_LOD" + i);
                var meshFilter = roundaboutObj.AddComponent<MeshFilter>();
                var meshRenderer = roundaboutObj.AddComponent<MeshRenderer>();
                meshFilter.sharedMesh = combinedMesh;
                meshRenderer.materials = combinedMaterials;
                roundabout.meshFilterLODs.Add(meshFilter);
                roundabout.meshRendererLODs.Add(meshRenderer);
                
                lods[i] = new LOD(1f - lodList[i], new Renderer[] {meshRenderer});
                
                roundaboutObj.transform.SetParent(roundabout.transform);
            }
            
            lodGroup.SetLODs(lods);
        }

        private static void PrepareIntersection(RoadDescr roadDescr, IntersectionObject intersection)
        {
            var firstChild = ObjectUtility.CreateLODObject(roadDescr.road.shadowCastingMode, intersection.gameObject, 0,
                out var meshFilter, out var meshRenderer);
            firstChild.transform.SetParent(intersection.transform);
            meshFilter.sharedMesh = intersection.meshFilter.sharedMesh;
            if (Application.isPlaying) meshRenderer.materials = intersection.meshRenderer.materials;
            else meshRenderer.materials = intersection.meshRenderer.sharedMaterials;
            
            if(intersection.meshFilterLODs.Count == 0)
            {
                ObjectUtility.DestroyObject(intersection.meshFilter);
                ObjectUtility.DestroyObject(intersection.meshRenderer);
            }
            else
            {
                for (int i = 0; i < intersection.meshFilterLODs.Count; i++)
                {
                    ObjectUtility.DestroyObject(intersection.meshFilterLODs[i].gameObject);
                }
                intersection.meshFilterLODs.Clear();
                intersection.meshRendererLODs.Clear();
            }
            
            intersection.meshFilter = meshFilter;
            intersection.meshRenderer = meshRenderer;
            intersection.meshFilterLODs.Add(meshFilter);
            intersection.meshRendererLODs.Add(meshRenderer);
        }
    }
}