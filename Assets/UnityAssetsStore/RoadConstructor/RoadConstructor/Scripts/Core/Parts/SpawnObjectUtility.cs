// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Linq;
using PampelGames.Shared.Utility;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace PampelGames.RoadConstructor
{
    internal static class SpawnObjectUtility
    {
        public static void SpawnObjects(List<SpawnObjectPreset> spawnObjectPresets,
            List<RoadObject> roads, List<IntersectionObject> intersections,
            RoadConstructor.SceneData sceneData,
            List<int> overlapIntersectionIndexes, List<int> overlapRoadIndexes)
        {
            var roadObjects = sceneData.roadObjects;
            var intersectionObjects = sceneData.intersectionObjects;
            var intersectionBounds = sceneData.intersectionBounds;
            
            // Removing self-overlapping intersections from the replaced roads
            for (var i = 0; i < roads.Count; i++)
            {
                var newOverlapIntersectionIndexes = new List<int>(overlapIntersectionIndexes);
                var _intersections = roads[i].IntersectionConnections;
                
                for (int j = newOverlapIntersectionIndexes.Count - 1; j >= 0; j--)
                {
                    var overlapIntersection = intersectionObjects[newOverlapIntersectionIndexes[j]];
                    var selfOverlap = _intersections.Any(intersection => intersection.name == overlapIntersection.name);
                    if(!selfOverlap) continue;
                    newOverlapIntersectionIndexes.RemoveAt(j);
                }
                
                ProcessRoad(roads[i], newOverlapIntersectionIndexes);
            }

            void ProcessRoad(RoadObject _roadObject, List<int> _overlapIntersectionIndexes)
            {
                var _spawnObjects = GetSpawnObjects(spawnObjectPresets, _roadObject);

                for (var i = 0; i < _spawnObjects.Count; i++)
                {
                    List<SpawnedObject> spawns;
                    var spawnObject = _spawnObjects[i];
                    if (spawnObject.objectType == SpawnObjectType.IntersectionApproach)
                        spawns = SpawnRoadEnd(spawnObject, _roadObject,
                            roadObjects, intersectionBounds, _overlapIntersectionIndexes, overlapRoadIndexes);
                    else if (spawnObject.objectType == SpawnObjectType.Road)
                        spawns = SpawnRoad(spawnObject, _roadObject,
                            roadObjects, intersectionBounds, _overlapIntersectionIndexes, overlapRoadIndexes);
                    else if (spawnObject.objectType == SpawnObjectType.IntersectionExit)
                        spawns = SpawnIntersection(spawnObject, _roadObject,
                            roadObjects, intersectionBounds, _overlapIntersectionIndexes, overlapRoadIndexes);
                    else
                        spawns = new List<SpawnedObject>();

                    for (var j = 0; j < spawns.Count; j++) spawns[j].transform.SetParent(_roadObject.transform);
                }
            }

            /********************************************************************************************************************************/
            // Destroying objects also from the overlapping indexes (undo gets lost for those)

            var tempNewRoads = new List<RoadObject>();
            var tempRoadIndexes = new List<int>();
            for (var i = 0; i < roads.Count; i++)
            {
                tempNewRoads.Add(roads[i]);
                tempRoadIndexes.Add(i);
            }
            
            var tempIntersectionIndexes = new List<int>();
            var tempIntersectionBounds = new List<Bounds>();
            for (var i = 0; i < intersections.Count; i++)
            {
                tempIntersectionIndexes.Add(i);
                tempIntersectionBounds.Add(intersections[i].meshRenderer.bounds);
            }
            
            for (var i = 0; i < overlapRoadIndexes.Count; i++)
            {
                var overlapRoad = roadObjects[overlapRoadIndexes[i]];
            
                var spawnedObjects = overlapRoad.GetSpawnedObjects();
            
                for (var j = spawnedObjects.Length - 1; j >= 0; j--)
                {
                    var overlapObj = spawnedObjects[j];
                    if (!overlapObj.spawnObject.removeOverlap) continue;
                    if (CheckOverlap(overlapObj.gameObject, tempNewRoads, tempIntersectionBounds, tempIntersectionIndexes,
                            tempRoadIndexes)) continue;
                    ObjectUtility.DestroyObject(overlapObj.gameObject);
                }
            }
            
            for (var i = 0; i < overlapIntersectionIndexes.Count; i++)
            {
                var overlapIntersection = intersectionObjects[overlapIntersectionIndexes[i]];
                var spawnedObjects = overlapIntersection.GetSpawnedObjects();
            
                for (var j = spawnedObjects.Length - 1; j >= 0; j--)
                {
                    var overlapObj = spawnedObjects[j];
                    if (!overlapObj.spawnObject.removeOverlap) continue;
                    if (CheckOverlap(overlapObj.gameObject, tempNewRoads, tempIntersectionBounds, tempIntersectionIndexes,
                            tempRoadIndexes)) continue;
                    ObjectUtility.DestroyObject(overlapObj.gameObject);
                }
            }
        }

        /********************************************************************************************************************************/
        /********************************************************************************************************************************/

        private static List<SpawnedObject> SpawnRoad(SpawnObject spawnObject, SceneObject sceneObject,
            List<RoadObject> roadObjects, List<Bounds> intersectionBounds,
            List<int> overlapIntersectionIndexes, List<int> overlapRoadIndexes)
        {
            var spawnedObjects = new List<SpawnedObject>();
            if (spawnObject.elevation == Elevation.ElevatedOnly && !sceneObject.elevated) return spawnedObjects;
            if (spawnObject.elevation == Elevation.GroundOnly && sceneObject.elevated) return spawnedObjects;

            var spline = sceneObject.splineContainer.Spline;
            var spacing = spawnObject.spacing;

            if (spawnObject.position != SpawnObjectPosition.Middle)
            {
                var newSpline = new Spline(sceneObject.splineContainer.Spline);
                RoadSplineUtility.OffsetSplineX(newSpline, sceneObject.roadDescr.width * 0.5f);
                spline = newSpline;
            }

            Process(sceneObject.splineContainer.Spline, spline, false);

            if (spawnObject.position == SpawnObjectPosition.BothSides)
            {
                var newSpline = new Spline(sceneObject.splineContainer.Spline);
                RoadSplineUtility.OffsetSplineX(newSpline, -sceneObject.roadDescr.width * 0.5f);
                RoadSplineUtility.InvertSpline(newSpline);
                Process(sceneObject.splineContainer.Spline, newSpline, true);
            }


            void Process(Spline middleSpline, Spline _spline, bool otherSide)
            {
                if (spawnObject.spacingType == SpacingType.Bounds)
                {
                    var meshRenderers = spawnObject.obj.GetComponentsInChildren<MeshRenderer>();
                    if (meshRenderers.Length == 0) return;
                    var combinedBounds = meshRenderers[0].bounds;
                    for (var i = 1; i < meshRenderers.Length; i++) combinedBounds.Encapsulate(meshRenderers[i].bounds);

                    if (spawnObject.rotation == SpawnObjectRotation.Forward || spawnObject.rotation == SpawnObjectRotation.Backward)
                        spacing = combinedBounds.size.z;
                    else
                        spacing = combinedBounds.size.x;
                }

                var length = _spline.GetLength();

                if (spacing > length) spacing *= 0.5f; // Try to add at least one object.

                var numberOfObjects = (int) (length / spacing);
                var normalizedSpacing = 1f / (numberOfObjects + 1);
                var evaluations = new List<float>();

                for (var i = 1; i <= numberOfObjects; i++) evaluations.Add(i * normalizedSpacing);

                for (var i = 0; i < evaluations.Count; i++)
                {
                    if (spawnObject.chance < Random.value) continue;

                    var t = evaluations[i];
                    var middleT = otherSide ? 1f - t : t;
                    middleSpline.Evaluate(middleT, out var position, out var tangent, out var upVector);
                    
                    // Left and right positions have wrong heights. Use middle spline height, identical to SplineMesh.cs.
                    if (spawnObject.position != SpawnObjectPosition.Middle)
                    {
                        var sidePosition = _spline.EvaluatePosition(t);
                        position.x = sidePosition.x;
                        position.z = sidePosition.z;
                    }

                    var checkElevation = true;
#if UNITY_EDITOR
                    if (sceneObject.GetType() == typeof(RoadObject))
                    {
                        var roadObject = sceneObject as RoadObject;
                        if (roadObject!.previewRoad) checkElevation = false;
                    }
#endif
                    if (checkElevation && !CheckElevation(sceneObject.roadDescr.settings, position, spawnObject.heightRange, spawnObject.elevation)) continue;

                    if (otherSide) tangent *= -1f;
                    var spawnedObject = Spawn(sceneObject.roadDescr, spawnObject, position, tangent, upVector, otherSide, false);

                    if (spawnObject.removeOverlap)
                        if (!CheckOverlap(spawnedObject.gameObject, roadObjects, intersectionBounds,
                                overlapIntersectionIndexes, overlapRoadIndexes))
                        {
                            ObjectUtility.DestroyObject(spawnedObject.gameObject);
                            continue;
                        }

                    spawnedObjects.Add(spawnedObject);
                }
            }

            return spawnedObjects;
        }

        private static List<SpawnedObject> SpawnRoadEnd(SpawnObject spawnObject, RoadObject roadObject,
            List<RoadObject> roadObjects, List<Bounds> intersectionBounds,
            List<int> overlapIntersectionIndexes, List<int> overlapRoadIndexes)
        {
            var spawnedObjects = new List<SpawnedObject>();

            if (spawnObject.elevation == Elevation.ElevatedOnly && !roadObject.elevated) return spawnedObjects;
            if (spawnObject.elevation == Elevation.GroundOnly && roadObject.elevated) return spawnedObjects;

            var spline = roadObject.splineContainer.Spline;

            for (var i = 0; i < roadObject.IntersectionConnections.Count; i++)
            {
                var intersection = roadObject.IntersectionConnections[i];
                var roadConnections = intersection.RoadConnections;
                if (intersection.intersectionType == IntersectionType.Intersection)
                {
                    if (roadConnections.Count < 3) continue;    
                }
                
                var nearestIndex = RoadSplineUtility.GetNearestKnotIndex(spline, intersection.centerPosition);
                var t = nearestIndex == 0 ? 0f : 1f;

                var offsetT = spawnObject.positionOffsetForward / roadObject.length;
                if (nearestIndex == 0) t += offsetT;
                else t -= offsetT;

                spline.Evaluate(t, out var position, out var tangent, out var upVector);

                if (!CheckElevation(roadObject.roadDescr.settings, position, spawnObject.heightRange, spawnObject.elevation)) continue;

                tangent = PGTrigonometryUtility.DirectionalTangentToPointXZ(intersection.centerPosition, position, tangent);

                if (!CheckDirections(roadObject, intersection, spawnObject, tangent)) continue;


                var spawns = new List<SpawnedObject>();

                if (spawnObject.chance >= Random.value) spawns.Add(Spawn(roadObject.roadDescr, spawnObject, position, tangent, upVector, false, true));

                if (spawnObject.position == SpawnObjectPosition.BothSides)
                    if (spawnObject.chance >= Random.value)
                        spawns.Add(Spawn(roadObject.roadDescr, spawnObject, position, -tangent, upVector, true, true));

                if (spawnObject.removeOverlap)
                    for (var j = spawns.Count - 1; j >= 0; j--)
                        if (!CheckOverlap(spawns[j].gameObject, roadObjects, intersectionBounds,
                                overlapIntersectionIndexes, overlapRoadIndexes))
                        {
                            ObjectUtility.DestroyObject(spawns[j].gameObject);
                            spawns.RemoveAt(j);
                        }

                spawnedObjects.AddRange(spawns);
            }

            return spawnedObjects;
        }

        private static List<SpawnedObject> SpawnIntersection(SpawnObject spawnObject, RoadObject roadObject,
            List<RoadObject> roadObjects, List<Bounds> intersectionBounds,
            List<int> overlapIntersectionIndexes, List<int> overlapRoadIndexes)
        {
            var spawnedObjects = new List<SpawnedObject>();

            var spline = roadObject.splineContainer.Spline;
            
            for (var i = 0; i < roadObject.IntersectionConnections.Count; i++)
            {
                var settings = roadObject.roadDescr.settings;
                var intersection = roadObject.IntersectionConnections[i];
                var roadConnections = intersection.RoadConnections;
                if (intersection.intersectionType == IntersectionType.Intersection)
                {
                    if (roadConnections.Count < 3) continue;    
                }
                
                if (spawnObject.elevation == Elevation.ElevatedOnly && !intersection.elevated) continue;
                if (spawnObject.elevation == Elevation.GroundOnly && intersection.elevated) continue;

                var nearestIndex = RoadSplineUtility.GetNearestKnotIndex(spline, intersection.centerPosition);
                
                var knot = spline.Knots.ElementAt(nearestIndex);
                var knotOther = spline.Knots.ElementAt(nearestIndex == 0 ? 1 : 0);
                var position = knot.Position;
                
                if (!CheckElevation(roadObject.roadDescr.settings, position, spawnObject.heightRange, spawnObject.elevation)) continue;

                var tangent = knot.TangentOut;
                tangent = PGTrigonometryUtility.DirectionalTangentToPointXZ(knotOther.Position, knot.Position, tangent) * (-1f);
                tangent.y = 0f;
                tangent = math.normalizesafe(tangent);
                
                var upVector = math.up();

                position += tangent * settings.intersectionDistance * 0.5f;

                var spawns = new List<SpawnedObject>();

                if (spawnObject.chance >= Random.value) spawns.Add(Spawn(roadObject.roadDescr, spawnObject, position, tangent, upVector, false, true));

                if (spawnObject.position == SpawnObjectPosition.BothSides)
                    if (spawnObject.chance >= Random.value)
                        spawns.Add(Spawn(roadObject.roadDescr, spawnObject, position, -tangent, upVector, true, true));

                if (spawnObject.removeOverlap)
                    for (var j = spawns.Count - 1; j >= 0; j--)
                        if (!CheckOverlap(spawns[j].gameObject, roadObjects, intersectionBounds,
                                overlapIntersectionIndexes, overlapRoadIndexes))
                        {
                            ObjectUtility.DestroyObject(spawns[j].gameObject);
                            spawns.RemoveAt(j);
                        }

                spawnedObjects.AddRange(spawns);
            }

            return spawnedObjects;
        }

        /********************************************************************************************************************************/

        private static List<SpawnObject> GetSpawnObjects(List<SpawnObjectPreset> spawnObjectPresets, SceneObject sceneObject)
        {
            var spawnObjects = new List<SpawnObject>();
            spawnObjects.AddRange(sceneObject.road.spawnObjects);
            for (var i = 0; i < spawnObjectPresets.Count; i++)
            {
                if (sceneObject.road.category != spawnObjectPresets[i].category) continue;
                spawnObjects.AddRange(spawnObjectPresets[i].spawnObjects);
            }

            return spawnObjects;
        }

        private static SpawnedObject Spawn(RoadDescr roadDescr, SpawnObject spawnObject, float3 position, float3 tangent, float3 upVector,
            bool otherSide, bool roadEnd)
        {
            var positionOffset = GetSpawnPositionOffset(tangent, upVector, roadDescr.width, roadDescr.settings.baseRoadHeight,
                spawnObject.position, spawnObject.alignToNormal, spawnObject.positionOffsetRight, spawnObject.heightOffset, roadEnd);

            var spawnRotation = GetSpawnRotation(spawnObject, tangent, upVector);

            var spawnPosition = position + (float3) positionOffset;

            GameObject spawnedObj = default; // Needed for build!
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                spawnedObj = (GameObject) PrefabUtility.InstantiatePrefab(spawnObject.obj);
                spawnedObj.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
#endif
            }
            else
            {
                spawnedObj = Object.Instantiate(spawnObject.obj, spawnPosition, spawnRotation);
            }

            if (spawnObject.scale != Vector2.one)
            {
                var scale = Random.Range(spawnObject.scale.x, spawnObject.scale.y);
                spawnedObj.transform.localScale = new Vector3(scale, scale, scale);
            }

            var spawnedObject = spawnedObj.AddComponent<SpawnedObject>();
            spawnedObject.Initialize(spawnObject);
            spawnedObject.otherSide = otherSide;
            return spawnedObject;
        }

        private static Vector3 GetSpawnPositionOffset(float3 tangent, float3 upVector, float width, float baseHeight,
            SpawnObjectPosition spawnObjectPosition, bool alignToNormal, float spawnPositionOffsetRight, float spawnHeightOffset,
            bool roadEnd)
        {
            var positionOffset = new Vector3();

            upVector = math.normalizesafe(upVector);

            if (!alignToNormal)
            {
                upVector = math.up();
                tangent.y = 0f;
            }

            if (spawnObjectPosition != SpawnObjectPosition.Middle)
            {
                var tangentPerp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(new float3(tangent.x, 0f, tangent.z));
                tangentPerp = math.normalizesafe(tangentPerp);
                
                if (roadEnd)
                {
                    positionOffset = tangentPerp * width * 0.5f;
                    positionOffset -= (Vector3) tangentPerp * spawnPositionOffsetRight;
                }
                else
                {
                    positionOffset = (Vector3) tangentPerp * (spawnPositionOffsetRight * -1f);   
                }
            }

            var heightOffset = upVector * (baseHeight + spawnHeightOffset);
            positionOffset += (Vector3) heightOffset;

            return positionOffset;
        }

        private static Quaternion GetSpawnRotation(SpawnObject spawnObject, float3 tangent, float3 upVector)
        {
            upVector = math.normalizesafe(upVector);

            if (!spawnObject.alignToNormal)
            {
                upVector = math.up();
                tangent.y = 0f;
            }

            var tangentPerp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(new float3(tangent.x, 0f, tangent.z));
            var forward = tangent;

            if (spawnObject.position != SpawnObjectPosition.Middle)
            {
                if (spawnObject.rotation == SpawnObjectRotation.Inside) forward = -tangentPerp;
                else if (spawnObject.rotation == SpawnObjectRotation.Outside) forward = tangentPerp;
                else if (spawnObject.rotation == SpawnObjectRotation.Forward) forward = tangent;
                else if (spawnObject.rotation == SpawnObjectRotation.Backward) forward = -tangent;
            }

            var spawnRotationAdd = spawnObject.rotation == SpawnObjectRotation.Random ? Random.Range(0f, 360f) : 0f;
            var spawnRotation = Quaternion.LookRotation(forward, upVector) * Quaternion.Euler(0f, spawnRotationAdd, 0f);
            var eulerAngles = spawnRotation.eulerAngles;
            eulerAngles.z = 0;
            spawnRotation = Quaternion.Euler(eulerAngles);

            return spawnRotation;
        }

        private static bool CheckDirections(RoadObject road, IntersectionObject intersection, SpawnObject spawnObject, float3 tangent)
        {
            if (!spawnObject.requiresDirection) return true;

            var forward = !spawnObject.requiresDirectionForward;
            var left = !spawnObject.requiresDirectionLeft;
            var right = !spawnObject.requiresDirectionRight;
            
            if (intersection.intersectionType == IntersectionType.Roundabout)
            {
                if (!spawnObject.requiresDirection) return true;
                if (spawnObject.requiresDirectionLeft || spawnObject.requiresDirectionForward) return false;
                return true;
            }

            for (var i = 0; i < intersection.RoadConnections.Count; i++)
            {
                var roadConnection = intersection.RoadConnections[i];
                if(roadConnection.road.oneWay) continue;
                if (road == roadConnection) continue;
                var splineConnect = roadConnection.splineContainer.Spline;
                var nearestIndex = RoadSplineUtility.GetNearestKnotIndex(splineConnect, intersection.centerPosition);
                splineConnect.Evaluate(nearestIndex == 0 ? 0f : 1f, out var posConnect, out var tanConnect, out var upVector);
                tanConnect = PGTrigonometryUtility.DirectionalTangentToPointXZ(intersection.centerPosition, posConnect, tanConnect) * -1f;
                var angle = math.degrees(PGTrigonometryUtility.AngleXZ(tangent, tanConnect));

                if (!forward)
                    if (Constants.ForwardAngle(angle))
                        forward = true;
                if (!left)
                    if (Constants.LeftAngle(angle))
                        left = true;
                if (!right)
                    if (Constants.RightAngle(angle))
                        right = true;
            }

            return forward && left && right;
        }

        private static bool CheckOverlap(GameObject spawnedObject,
            List<RoadObject> roadObjects, List<Bounds> intersectionBounds,
            List<int> overlapIntersectionIndexes, List<int> overlapRoadIndexes)
        {
            var meshRenderers = spawnedObject.GetComponentsInChildren<MeshRenderer>();
            if (meshRenderers.Length == 0) return true;

            var position = spawnedObject.transform.position;

            for (var i = 0; i < meshRenderers.Length; i++)
            {
                var spawnBounds = meshRenderers[i].bounds;

                for (var j = 0; j < overlapIntersectionIndexes.Count; j++)
                {
                    var intersectionBound = intersectionBounds[overlapIntersectionIndexes[j]];

                    if (spawnBounds.Intersects(intersectionBound)) return false;
                }

                for (var j = 0; j < overlapRoadIndexes.Count; j++)
                {
                    var roadObject = roadObjects[overlapRoadIndexes[j]];
                    var spline = roadObject.splineContainer.Spline;
                    var maxDistance = roadObject.roadDescr.width * 0.5f;
                    SplineUtility.GetNearestPoint(spline, position, out var nearest, out var t);
                    var distance = PGTrigonometryUtility.DistanceXZ(position, nearest);
                    if (distance <= maxDistance) return false;
                }
            }

            return true;
        }


        private static bool CheckElevation(ComponentSettings settings, float3 position, Vector2 heightRange, Elevation elevation)
        {
            if (elevation == Elevation.Any) return true;

            var raycastOffset = Constants.RaycastOffset(settings);

            var ray = new Ray((Vector3) position + raycastOffset, Vector3.down);
            if (Physics.Raycast(ray, out var hit, float.MaxValue, settings.groundLayers))
            {
                var elevationHeight = position.y - hit.point.y;
                if (elevation == Elevation.ElevatedOnly &&
                    elevationHeight >= 0 && elevationHeight > settings.elevationStartHeight)
                    if (elevationHeight >= heightRange.x && elevationHeight <= heightRange.y)
                        return true;
                if (elevation == Elevation.GroundOnly && elevationHeight < settings.elevationStartHeight) return true;
            }

            return false;
        }
    }
}