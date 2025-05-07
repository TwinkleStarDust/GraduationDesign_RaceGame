// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using PampelGames.Shared.Utility;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Splines;
using GameObject = UnityEngine.GameObject;

namespace PampelGames.RoadConstructor
{
    /// <summary>
    ///     Road Constructor Component.
    /// </summary>
    [AddComponentMenu("Pampel Games/Road Constructor")]
    public class RoadConstructor : MonoBehaviour
    {
        private void Reset()
        {
            componentSettings.groundLayers |= 1 << LayerMask.NameToLayer("Default");
        }

        public SO_DefaultReferences _DefaultReferences;
        public RoadSet _RoadSet;

        public ComponentSettings componentSettings = new();

#if UNITY_EDITOR
        public EditorDisplay _editorDisplay = EditorDisplay.RoadSet;
        public PartType _editorActivePartType = PartType.Roads;
        public float roadPreviewLength = 25f;
        public bool roadPreviewElevated;
#endif

        /********************************************************************************************************************************/

        private SceneData sceneData = new();

        public class SceneData
        {
            public readonly List<RoadObject> roadObjects = new();
            public readonly List<Bounds> roadBounds = new();

            public readonly List<IntersectionObject> intersectionObjects = new();
            public readonly List<Bounds> intersectionBounds = new();

            public void AddRoad(RoadObject roadObject)
            {
                roadObjects.Add(roadObject);
                roadBounds.Add(roadObject.meshRenderer.bounds);
            }

            public void AddIntersection(IntersectionObject intersectionObject)
            {
                intersectionObjects.Add(intersectionObject);
                intersectionBounds.Add(intersectionObject.meshRenderer.bounds);
            }

            public void RemoveRoad(int index)
            {
                roadBounds.RemoveAt(index);
                roadObjects.RemoveAt(index);
            }

            public void RemoveIntersection(int index)
            {
                intersectionBounds.RemoveAt(index);
                intersectionObjects.RemoveAt(index);
            }

            public void Clear()
            {
                roadObjects.Clear();
                roadBounds.Clear();
                intersectionObjects.Clear();
                intersectionBounds.Clear();
            }
        }

        /********************************************************************************************************************************/
        private LinkedList<UndoObject> undoObjects;
        /********************************************************************************************************************************/

        private List<RoadDescr> roadDescrs;

        private Dictionary<string, int> roadIndexDict;

        private Transform displayParent;
        private Transform constructionParent;
        private Transform undoParent;
        private Transform displayDemolishParent;
        private Transform displayMoveIntersectionParent;

        private List<GameObject> displayedObjects;
        private List<GameObject> displayedDemolishObjects;
        private List<GameObject> displayedMoveIntersectionObjects;
        private List<GameObject> deactivatedObjects;

        private bool initialized;

        /* Public Delegates *************************************************************************************************************/

        public event Action<List<RoadObject>, List<IntersectionObject>> OnRoadsAdded = delegate { };
        public event Action<List<RoadObject>, List<IntersectionObject>> OnRoadsRemoved = delegate { };

        /********************************************************************************************************************************/
        private void Awake()
        {
            Initialize();
        }

        private bool VerifyInitialization()
        {
            if (Application.isPlaying && initialized) return false;

#if UNITY_EDITOR
            if (componentSettings.terrainSettings && componentSettings.terrain == null)
            {
                Debug.LogError("Terran settings are enabled but no terrain assigned!");
                return false;
            }
#endif
            return true;
        }

        private void InitializeScene()
        {
            displayedObjects = new List<GameObject>();
            displayParent = transform.Find(Constants.DisplayConstructionParent);
            if (displayParent == null) displayParent = new GameObject(Constants.DisplayConstructionParent).transform;
            displayParent.transform.SetParent(transform);
            constructionParent = transform.Find(Constants.ConstructionParent);
            if (constructionParent == null) constructionParent = new GameObject(Constants.ConstructionParent).transform;
            constructionParent.transform.SetParent(transform);
            displayDemolishParent = transform.Find(Constants.DisplayDemolishParent);
            if (displayDemolishParent == null) displayDemolishParent = new GameObject(Constants.DisplayDemolishParent).transform;
            displayDemolishParent.transform.SetParent(transform);
            displayMoveIntersectionParent = transform.Find(Constants.DisplayMoveIntersectionParent);
            if (displayMoveIntersectionParent == null)
                displayMoveIntersectionParent = new GameObject(Constants.DisplayMoveIntersectionParent).transform;
            displayMoveIntersectionParent.transform.SetParent(transform);
            displayedDemolishObjects = new List<GameObject>();
            displayedMoveIntersectionObjects = new List<GameObject>();
            deactivatedObjects = new List<GameObject>();
            undoParent = transform.Find(Constants.UndoParent);
            if (undoParent == null) undoParent = new GameObject(Constants.UndoParent).transform;
            undoParent.transform.SetParent(transform);
            sceneData = new SceneData();
            undoObjects = new LinkedList<UndoObject>();
        }

        private void InitializeRoads()
        {
            roadIndexDict = new Dictionary<string, int>();
            for (var i = 0; i < _RoadSet.roads.Count; i++)
            {
                if (string.IsNullOrEmpty(_RoadSet.roads[i].roadName))
                {
                    Debug.LogWarning("No name is assigned to the road at index " + i + ", which is not supported!");
                    continue;
                }

                if (!roadIndexDict.TryAdd(_RoadSet.roads[i].roadName, i))
                    Debug.LogWarning("The road name: " + _RoadSet.roads[i].roadName + " is not unique!");
            }

            roadDescrs = new List<RoadDescr>();
            for (var i = 0; i < _RoadSet.roads.Count; i++)
            {
                var road = _RoadSet.roads[i];
                var roadDescr = new RoadDescr(road, componentSettings, _RoadSet.lanePresets);
                SplineEdgeUtility.CreateRoadLanes(componentSettings, roadDescr, _RoadSet.lanePresets);
                roadDescr.trafficLanes = TrafficUtility.GetTrafficLanesEditor(_RoadSet.trafficLanePresets, roadDescr);
                roadDescrs.Add(roadDescr);
            }
        }

        private void UninitializeInternal()
        {
            for (var i = gameObject.transform.childCount - 1; i >= 0; i--) ObjectUtility.DestroyObject(gameObject.transform.GetChild(i).gameObject);
            if (!IsInitialized()) return;

            sceneData.Clear();
            roadDescrs.Clear();
            roadIndexDict.Clear();
            roadIndexDict = null;
        }


        /********************************************************************************************************************************/
        // Public
        /********************************************************************************************************************************/

        /// <summary>
        ///     Initializes this <see cref="RoadConstructor" /> component for constructing.
        ///     In play-mode, this is done automatically in Awake.
        /// </summary>
        public void Initialize()
        {
            if (!VerifyInitialization()) return;
            InitializeScene();
            InitializeRoads();
            if (Application.isPlaying) initialized = true;
        }

        /// <summary>
        ///     Deinitializes this <see cref="RoadConstructor" /> component,
        ///     which includes clearing the history and removing helper objects.
        /// </summary>
        public void Uninitialize()
        {
            UninitializeInternal();
            if (Application.isPlaying) initialized = false;
        }

        /// <summary>
        ///     Returns true if this component has been initialized.
        /// </summary>
        public bool IsInitialized()
        {
#if UNITY_EDITOR
            if (roadIndexDict == null) return false;
            if (displayedObjects == null) return false;
#endif
            return true;
        }

        /// <summary>
        ///     Registers existing scene objects for construction.
        ///     The component has to be initialized first.
        ///     The active construction set needs to contain the road name for each object to be registered.
        /// </summary>
        public void RegisterSceneObjects()
        {
            if (Application.isPlaying && !initialized) Initialize();
            RegisterSceneObjectsInternal(out var sceneRoadObjects, out var sceneIntersectionObjects);
        }

        /// <summary>
        ///     Registers existing scene objects for construction.
        ///     The component has to be initialized first.
        ///     The active construction set needs to contain the road name for each object to be registered.
        /// </summary>
        public void RegisterSceneObjects(out List<RoadObject> sceneRoadObjects, out List<IntersectionObject> sceneIntersectionObjects)
        {
            RegisterSceneObjectsInternal(out sceneRoadObjects, out sceneIntersectionObjects);
        }

        /// <summary>
        ///     Gets a <see cref="Road" /> by name.
        /// </summary>
        public bool TryGetRoad(string roadName, out Road road)
        {
            road = default;
            if (!IsInitialized()) return false;
            if (!roadIndexDict.TryGetValue(roadName, out var partIndex)) return false;
            road = _RoadSet.roads[partIndex];
            return true;
        }

        /// <summary>
        ///     Gets a <see cref="RoadDescr" /> by road name.
        /// </summary>
        public bool TryGetRoadDescr(string roadName, out RoadDescr roadDescr)
        {
            roadDescr = default;
            if (!roadIndexDict.TryGetValue(roadName, out var partIndex))
            {
                Debug.LogWarning("The road: " + roadName + " does not exist!");
                return false;
            }

            roadDescr = roadDescrs[partIndex];
            return true;
        }

        /// <summary>
        ///     Gets a list of all registered <see cref="RoadObject" />s from the scene.
        /// </summary>
        public List<RoadObject> GetRoads()
        {
            return sceneData.roadObjects;
        }

        /// <summary>
        ///     Gets a list of all registered <see cref="IntersectionObject" />s from the scene.
        /// </summary>
        public List<IntersectionObject> GetIntersections()
        {
            return sceneData.intersectionObjects;
        }

        /// <summary>
        ///     Gets a list of all registered <see cref="RoadObject" />s and <see cref="IntersectionObject" />s from the scene.
        /// </summary>
        public List<SceneObject> GetSceneObjects()
        {
            var sceneObjects = new List<SceneObject>();
            sceneObjects.AddRange(GetRoads());
            sceneObjects.AddRange(GetIntersections());
            return sceneObjects;
        }

        /// <summary>
        ///     Gets the parent object holding all registered <see cref="RoadObject" />s and <see cref="IntersectionObject" />s from the scene.
        /// </summary>
        /// <returns></returns>
        public Transform GetConstructionParent()
        {
            return constructionParent;
        }

        /// <summary>
        ///     Clears all displayed objects and displayed demolish objects in the scene hierarchy.
        /// </summary>
        public void ClearAllDisplayObjects(bool activateSceneObjects = true)
        {
            if (!IsInitialized()) return;

            ClearDisplayedObjects();
            ClearDisplayedDemolishObjects();
            ClearDisplayedMoveIntersectionObjects();

            if (activateSceneObjects)
            {
                for (var i = 0; i < deactivatedObjects.Count; i++) deactivatedObjects[i].SetActive(true);
                deactivatedObjects.Clear();
            }
        }

        /// <summary>
        ///     Clears all displayed objects in the scene hierarchy.
        /// </summary>
        public void ClearDisplayedObjects()
        {
            for (var i = 0; i < displayedObjects.Count; i++) ObjectUtility.DestroyObject(displayedObjects[i]);
            displayedObjects.Clear();
        }

        /// <summary>
        ///     Clears all displayed demolish objects in the scene hierarchy.
        /// </summary>
        public void ClearDisplayedDemolishObjects()
        {
            for (var i = 0; i < displayedDemolishObjects.Count; i++) ObjectUtility.DestroyObject(displayedDemolishObjects[i]);
            displayedDemolishObjects.Clear();
        }

        /// <summary>
        ///     Clears all displayed move intersection objects in the scene hierarchy.
        /// </summary>
        public void ClearDisplayedMoveIntersectionObjects()
        {
            for (var i = 0; i < displayedMoveIntersectionObjects.Count; i++) ObjectUtility.DestroyObject(displayedMoveIntersectionObjects[i]);
            displayedMoveIntersectionObjects.Clear();
        }

        /// <summary>
        ///     Snaps a position to the nearest road, intersection or grid using the width of the road.
        /// </summary>
        public Vector3 SnapPosition(string roadName, Vector3 position, out Overlap overlap)
        {
            overlap = new Overlap();
            if (!TryGetRoadDescr(roadName, out var roadDescr)) return position;

            return SnapPosition(roadDescr.width, position, out overlap);
        }

        /// <summary>
        ///     Snaps a position to the nearest road, intersection or grid using a custom radius.
        /// </summary>
        public Vector3 SnapPosition(float radius, Vector3 position, out Overlap overlap)
        {
            float3 newPosition = position;

            overlap = OverlapUtility.GetOverlap(componentSettings, radius, float.MaxValue, position, sceneData);

            if (overlap.exists) newPosition = overlap.position;
            else ApplyGridPositions(ref newPosition, ref newPosition);
            return newPosition;
        }

        /// <summary>
        ///     Undoes the previous construction operation and restores the previous state.
        ///     Requires objects within the undo storage.
        /// </summary>
        public void UndoLastConstruction()
        {
            UndoInternal();
        }

        /// <summary>
        ///     Removes all registered undo objects from the scene.
        /// </summary>
        public void ClearUndoStorage()
        {
            foreach (var undoObject in undoObjects) ObjectUtility.DestroyObject(undoObject.gameObject);
            undoObjects.Clear();
        }

        /// <summary>
        ///     Creates a road object and two intersection objects (start and end) for a segment between two positions.
        ///     These objects are not initialized in the system and should be deleted in the next frame via
        ///     the <see cref="ClearAllDisplayObjects" /> method.
        /// </summary>
        /// <param name="roadName">The name of the road.</param>
        /// <param name="position01">The first road position.</param>
        /// <param name="position02">The second road position.</param>
        /// <returns>The construction result, including new objects and detailed construction failures.</returns>
        public ConstructionResultRoad DisplayRoad(string roadName, float3 position01, float3 position02)
        {
            return DisplayRoadInternal(roadName, position01, position02, new RoadSettings());
        }

        /// <summary>
        ///     Creates a road object and two intersection objects (start and end) for a segment between two positions.
        ///     These objects are not initialized in the system and should be deleted in the next frame via
        ///     the <see cref="ClearAllDisplayObjects" /> method.
        /// </summary>
        /// <param name="roadName">The name of the road.</param>
        /// <param name="spline">Spline used for construction. Note that only the first and last knots are utilized - any others will be disregarded.</param>
        /// <returns>The construction result, including new objects and detailed construction failures.</returns>
        public ConstructionResultRoad DisplayRoad(string roadName, Spline spline)
        {
            var knots = spline.Knots.ToList();
            var knot01 = knots[0];
            var knot02 = knots[^1];
            return DisplayRoadInternal(roadName, knot01.Position, knot02.Position, new RoadSettings
            {
                setTangent01 = true,
                setTangent02 = true,
                tangent01 = knot01.TangentOut,
                tangent02 = knot02.TangentIn
            });
        }

        /// <summary>
        ///     Creates a road object and two intersection objects (start and end) for a segment between two positions.
        ///     These objects are not initialized in the system and should be deleted in the next frame via
        ///     the <see cref="ClearAllDisplayObjects" /> method.
        /// </summary>
        /// <param name="roadName">The name of the road.</param>
        /// <param name="position01">The first road position.</param>
        /// <param name="position02">The second road position.</param>
        /// <param name="roadSettings">Optional settings that can be dynamically applied.</param>
        /// <returns>The construction result, including new objects and detailed construction failures.</returns>
        public ConstructionResultRoad DisplayRoad(string roadName, float3 position01, float3 position02, RoadSettings roadSettings)
        {
            return DisplayRoadInternal(roadName, position01, position02, roadSettings);
        }

        /// <summary>
        ///     Constructs new intersection and road objects for a segment between two positions and registers them into the construction system.
        /// </summary>
        /// <param name="roadName">The name of the road.</param>
        /// <param name="position01">The first road position.</param>
        /// <param name="position02">The second road position.</param>
        /// <returns>The construction result, including new objects and detailed construction failures.</returns>
        public ConstructionResultRoad ConstructRoad(string roadName, float3 position01, float3 position02)
        {
            return ConstructRoadInternal(roadName, position01, position02, new RoadSettings());
        }

        /// <summary>
        ///     Constructs new intersection and road objects for a segment between two positions and registers them into the construction system.
        /// </summary>
        /// <param name="roadName">The name of the road.</param>
        /// <param name="spline">Spline used for construction. Note that only the first and last knots are utilized - any others will be disregarded.</param>
        /// <returns>The construction result, including new objects and detailed construction failures.</returns>
        public ConstructionResultRoad ConstructRoad(string roadName, Spline spline)
        {
            var knots = spline.Knots.ToList();
            var knot01 = knots[0];
            var knot02 = knots[^1];
            return ConstructRoadInternal(roadName, knot01.Position, knot02.Position, new RoadSettings
            {
                setTangent01 = true,
                setTangent02 = true,
                tangent01 = knot01.TangentOut,
                tangent02 = knot02.TangentIn
            });
        }

        /// <summary>
        ///     Constructs new intersection and road objects for a segment between two positions and registers them into the construction system.
        /// </summary>
        /// <param name="roadName">The name of the road.</param>
        /// <param name="position01">The first road position.</param>
        /// <param name="position02">The second road position.</param>
        /// <param name="roadSettings">Optional settings that can be dynamically applied.</param>
        /// <returns>The construction result, including new objects and detailed construction failures.</returns>
        public ConstructionResultRoad ConstructRoad(string roadName, float3 position01, float3 position02, RoadSettings roadSettings)
        {
            return ConstructRoadInternal(roadName, position01, position02, roadSettings);
        }

        /// <summary>
        ///     Displays a roundabout at the specified position with the given road name and radius.
        /// </summary>
        /// <param name="roadName">The name of the road to use for the roundabout.</param>
        /// <param name="position">The position where the center of the roundabout will be located.</param>
        /// <param name="radius">The radius of the roundabout.</param>
        /// <returns>The construction result of displaying the roundabout.</returns>
        public ConstructionResultRoundabout DisplayRoundabout(string roadName, float3 position, float radius)
        {
            return DisplayRoundaboutInternal(roadName, position, radius);
        }

        /// <summary>
        ///     Constructs a roundabout at the specified position with the given road name and radius.
        /// </summary>
        /// <param name="roadName">The name of the road for the roundabout.</param>
        /// <param name="position">The position where the roundabout will be constructed.</param>
        /// <param name="radius">The radius of the roundabout.</param>
        /// <returns>A ConstructionResultRoundabout indicating the result of the construction.</returns>
        public ConstructionResultRoundabout ConstructRoundabout(string roadName, float3 position, float radius)
        {
            return ConstructRoundaboutInternal(roadName, position, radius);
        }

        public ConstructionResultRamp DisplayRamp(string roadName, float3 position01, float3 position02, RoadSettings roadSettings)
        {
            return DisplayRampInternal(roadName, position01, position02, roadSettings);
        }

        public ConstructionResultRamp ConstructRamp(string roadName, float3 position01, float3 position02, RoadSettings roadSettings)
        {
            return ConstructRampInternal(roadName, position01, position02, roadSettings);
        }

        /// <summary>
        ///     Displays move intersection object.
        /// </summary>
        /// <param name="currentPosition">The current position where the intersection object is displayed.</param>
        /// <param name="searchRadius">The search radius within which the intersection object will be found.</param>
        /// <param name="newPosition">The new position where the intersection object will be displayed.</param>
        /// <param name="deactivateSceneObjects">A flag indicating whether to deactivate scene objects.</param>
        /// <returns>A list of GameObjects representing the displayed move intersection objects.</returns>
        public List<GameObject> DisplayMoveIntersection(Vector3 currentPosition, float searchRadius, Vector3 newPosition,
            bool deactivateSceneObjects = true)
        {
            return DisplayMoveIntersectionInternal(currentPosition, searchRadius, newPosition, deactivateSceneObjects);
        }

        /// <summary>
        ///     Moves an existing <see cref="IntersectionObject" /> to a new position.
        ///     Undo history will be lost after this method is called.
        /// </summary>
        /// <param name="currentPosition">The position where to search for the intersection.</param>
        /// <param name="searchRadius">The radius within which to search for the intersection.</param>
        /// <param name="newPosition">The new position to move the intersection to.</param>
        /// <returns>A <see cref="ConstructionResultMoveIntersection" /> representing the result of the operation.</returns>
        public ConstructionResultMoveIntersection MoveIntersection(Vector3 currentPosition, float searchRadius, Vector3 newPosition)
        {
            return MoveIntersectionInternal(currentPosition, searchRadius, newPosition);
        }

        /// <summary>
        ///     Creates display objects for demolishing for the specified position.
        ///     These objects are not initialized in the system and should be deleted in the next frame via
        ///     the <see cref="ClearAllDisplayObjects" /> method.
        /// </summary>
        /// <param name="position">The position from which to demolish objects.</param>
        /// <param name="searchRadius">Search radius to find the demolishable objects.</param>
        /// <param name="deactivateSceneObjects">Deactivates the scene objects.</param>
        /// <returns></returns>
        public List<GameObject> DisplayDemolishObjects(Vector3 position, float searchRadius, bool deactivateSceneObjects = true)
        {
            return DisplayDemolishObjectsInternal(position, searchRadius, deactivateSceneObjects);
        }

        /// <summary>
        ///     Demolishes objects within a specified radius from a given position.
        /// </summary>
        /// <param name="position">The position from which to demolish objects.</param>
        /// <param name="searchRadius">Search radius to find the demolishable objects.</param>
        public void Demolish(Vector3 position, float searchRadius)
        {
            DemolishObjectsInternal(position, searchRadius);
        }

        /// <summary>
        ///     Updates the colliders of the specified scene objects based on the component settings.
        /// </summary>
        /// <param name="sceneObjects">The list of scene objects to update colliders for.</param
        public void UpdateColliders<T>(List<T> sceneObjects) where T : SceneObject
        {
            for (var i = 0; i < sceneObjects.Count; i++)
                if (sceneObjects[i].meshFilter.gameObject.TryGetComponent<MeshCollider>(out var meshCollider))
                    ObjectUtility.DestroyObject(meshCollider);

            if (componentSettings.addCollider != AddCollider.None)
                for (var i = 0; i < sceneObjects.Count; i++)
                    if (componentSettings.addCollider == AddCollider.Convex)
                        sceneObjects[i].meshFilter.gameObject.AddComponent<MeshCollider>().convex = true;
                    else
                        sceneObjects[i].meshFilter.gameObject.AddComponent<MeshCollider>().convex = false;
        }

        /// <summary>
        ///     Update the layers and tags of scene objects based on the component settings.
        /// </summary>
        /// <param name="sceneObjects">List of scene objects to update layers and tags</param>
        public void UpdateLayersAndTags<T>(List<T> sceneObjects) where T : SceneObject
        {
            for (var i = 0; i < sceneObjects.Count; i++) sceneObjects[i].meshFilter.gameObject.layer = componentSettings.addColliderLayer;
            for (var i = 0; i < sceneObjects.Count; i++) sceneObjects[i].tag = componentSettings.roadTag;
        }


        /// <summary>
        ///     Editor Only. Exports road and intersection meshes into the project folder.
        /// </summary>
        /// <param name="checkExistingMeshes">
        ///     Checks the project folder for each mesh before exporting.
        ///     If set to false, each mesh will be exported regardless of whether one already exists.
        /// </param>
        public void ExportMeshes(bool checkExistingMeshes)
        {
#if UNITY_EDITOR
            Export.ExportMeshes(constructionParent.gameObject, checkExistingMeshes);
#endif
        }

        /// <summary>
        ///     Removes existing <see cref="Traffic" /> components and creates new ones for the full existing road system.
        ///     The traffic component is required for the <see cref="AddWaypoints" /> method.
        /// </summary>
        public void AddTrafficComponents()
        {
            AddTrafficComponents(GetRoads(), GetIntersections());
        }

        /// <summary>
        ///     Removes existing <see cref="Traffic" /> components and creates new ones for the specified roads/intersections.
        ///     The traffic component is required for the <see cref="AddWaypoints" /> method.
        /// </summary>
        public void AddTrafficComponents(List<RoadObject> roads, List<IntersectionObject> intersections)
        {
            for (var i = 0; i < roads.Count; i++) TrafficUtility.AddTrafficComponent(componentSettings, roads[i], false);
            for (var i = 0; i < intersections.Count; i++) TrafficUtility.AddTrafficComponent(componentSettings, intersections[i], false);
        }

        /// <summary>
        ///     Creates a list of interconnected waypoints for the specified roads/intersections and adds them to the existing waypoint system.
        ///     Before using this method, make sure all registered roads have a 'Traffic' component assigned.
        ///     Either by enabling 'Add Traffic Comp.' in the settings or by invoking the AddTrafficComponents method.
        /// </summary>
        public void AddWaypoints(List<RoadObject> roads, List<IntersectionObject> intersections)
        {
            AddWaypoints(roads, intersections, componentSettings.waypointDistance);
        }

        /// <summary>
        ///     Creates a list of interconnected waypoints for the specified roads/intersections and adds them to the existing waypoint system.
        ///     Before using this method, make sure all registered roads have a 'Traffic' component assigned.
        ///     Either by enabling 'Add Traffic Comp.' in the settings or by invoking the AddTrafficComponents method.
        /// </summary>
        /// <param name="maxDistance">Maximum space between each waypoint.</param>
        public void AddWaypoints(List<RoadObject> roads, List<IntersectionObject> intersections, float maxDistance)
        {
            AddWaypoints(roads, intersections, new Vector2(maxDistance, maxDistance));
        }

        /// <summary>
        ///     Creates a list of interconnected waypoints for the specified roads/intersections and adds them to the existing waypoint system.
        ///     Before using this method, make sure all registered roads have a 'Traffic' component assigned.
        ///     Either by enabling 'Add Traffic Comp.' in the settings or by invoking the AddTrafficComponents method.
        /// </summary>
        /// <param name="maxDistance">Maximum space between each waypoint, based on curvature.</param>
        public void AddWaypoints(List<RoadObject> roads, List<IntersectionObject> intersections, Vector2 maxDistance)
        {
            WaypointUtility.CreateWaypoints(roads, intersections, TrafficLaneType.Car, maxDistance);
            WaypointUtility.ConnectWaypoints(roads, TrafficLaneType.Car);

            WaypointUtility.CreateWaypoints(roads, intersections, TrafficLaneType.Pedestrian, maxDistance);
            WaypointUtility.ConnectWaypoints(roads, TrafficLaneType.Pedestrian);
        }

        /// <summary>
        ///     Creates a list of interconnected waypoints for the specified intersections and adds them to the existing waypoint system.
        ///     Before using this method, make sure all registered roads have a 'Traffic' component assigned.
        ///     Either by enabling 'Add Traffic Comp.' in the settings or by invoking the AddTrafficComponents method.
        /// </summary>
        /// <param name="maxDistance">Maximum space between each waypoint, based on curvature.</param>
        public void AddWaypoints(List<IntersectionObject> intersections, Vector2 maxDistance)
        {
            WaypointUtility.CreateWaypoints(new List<RoadObject>(), intersections, TrafficLaneType.Car, maxDistance);
            WaypointUtility.ConnectWaypoints(intersections, TrafficLaneType.Car);

            WaypointUtility.CreateWaypoints(new List<RoadObject>(), intersections, TrafficLaneType.Pedestrian, maxDistance);
            WaypointUtility.ConnectWaypoints(intersections, TrafficLaneType.Pedestrian);
        }

        /// <summary>
        ///     Creates a list of interconnected waypoints from all road and intersection splines in the scene, overwriting any existing waypoints.
        ///     Before using this method, make sure all registered roads have a 'Traffic' component assigned.
        ///     Either by enabling 'Add Traffic Comp.' in the settings or by invoking the AddTrafficComponents method.
        /// </summary>
        public void CreateAllWaypoints()
        {
            CreateAllWaypoints(componentSettings.waypointDistance);
        }

        /// <summary>
        ///     Creates a list of interconnected waypoints from all road and intersection splines in the scene, overwriting any existing waypoints.
        ///     Before using this method, make sure all registered roads have a 'Traffic' component assigned.
        ///     Either by enabling 'Add Traffic Comp.' in the settings or by invoking the AddTrafficComponents method.
        /// </summary>
        /// <param name="maxDistance">Maximum space between each waypoint.</param>
        public void CreateAllWaypoints(float maxDistance)
        {
            CreateAllWaypoints(new Vector2(maxDistance, maxDistance));
        }

        /// <summary>
        ///     Creates a list of interconnected waypoints from all road and intersection splines in the scene, overwriting any existing waypoints.
        ///     Before using this method, make sure all registered roads have a 'Traffic' component assigned.
        ///     Either by enabling 'Add Traffic Comp.' in the settings or by invoking the AddTrafficComponents method.
        /// </summary>
        /// <param name="maxDistance">Maximum space between each waypoint, based on curvature.</param>
        public void CreateAllWaypoints(Vector2 maxDistance)
        {
            WaypointUtility.CreateWaypoints(GetRoads(), GetIntersections(), TrafficLaneType.Car, maxDistance);
            WaypointUtility.ConnectWaypoints(GetRoads(), TrafficLaneType.Car);

            WaypointUtility.CreateWaypoints(GetRoads(), GetIntersections(), TrafficLaneType.Pedestrian, maxDistance);
            WaypointUtility.ConnectWaypoints(GetRoads(), TrafficLaneType.Pedestrian);
        }

        /// <summary>
        ///     Removes all traffic components and waypoints from the system.
        /// </summary>
        public void RemoveTrafficSystem()
        {
            var sceneObjects = GetSceneObjects();
            for (var i = 0; i < sceneObjects.Count; i++)
            {
                var sceneObject = sceneObjects[i];
                if (sceneObject.traffic != null)
                {
                    ObjectUtility.DestroyObject(sceneObject.traffic.gameObject);
                    sceneObject.traffic = null;
                }
            }
        }

        /// <summary>
        ///     Removes waypoints to these roads in connecting intersections.
        /// </summary>
        public void RemoveConnectingWaypoints(List<RoadObject> roads)
        {
            WaypointUtility.RemoveConnectingWaypoints(roads, TrafficLaneType.Car);
            WaypointUtility.RemoveConnectingWaypoints(roads, TrafficLaneType.Pedestrian);
        }

        /// <summary>
        ///     Returns a list of all registered waypoints of the specified type.
        /// </summary>
        public List<Waypoint> GetWaypoints(TrafficLaneType trafficLaneType)
        {
            var waypoints = new List<Waypoint>();
            var sceneObjects = GetSceneObjects();
            for (var i = 0; i < sceneObjects.Count; i++)
            {
                var trafficLanes = sceneObjects[i].GetTrafficLanes(trafficLaneType);
                for (var j = 0; j < trafficLanes.Count; j++) waypoints.AddRange(trafficLanes[j].GetWaypoints());
            }

            return waypoints;
        }

        /// <summary>
        ///     For each road marked as 'expanded' in the Road Setup tab, a corresponding road is created in the scene.
        ///     These objects are parented to the 'Road Preview' GameObject and not registered in the system.
        /// </summary>
        public void ConstructPreviewRoads(Transform parent, float roadLength, bool elevated)
        {
            var roadSettings = new RoadSettings();
            var expandedRoads = new List<Road>(_RoadSet.roads.Where(road => road._editorVisible));

            if (expandedRoads.Count == 0)
            {
                Debug.LogWarning("No roads are marked as expanded.\n" +
                                 "Please check the Road Setup tab in the inspector to see if all roads are collapsed.");
                return;
            }

            var LOD = componentSettings.lodList.Count > 1;

            var positionX = 0f;
            RoadDescr lastRoadDescr = default;
            for (var i = 0; i < expandedRoads.Count; i++)
            {
                var road = expandedRoads[i];
                var roadDescr = new RoadDescr(road, componentSettings, _RoadSet.lanePresets);
                SplineEdgeUtility.CreateRoadLanes(componentSettings, roadDescr, _RoadSet.lanePresets);
                roadDescr.trafficLanes = TrafficUtility.GetTrafficLanesEditor(_RoadSet.trafficLanePresets, roadDescr);

                if (i > 0) positionX += lastRoadDescr!.width * 0.5f + roadDescr.width * 0.5f + 2f;
                var position01 = new float3(positionX, 0f, 0f);
                var position02 = new float3(positionX, 0f, roadLength);
                lastRoadDescr = roadDescr;

                var roadData = CreateRoadData(roadSettings, roadDescr, position01, position02, false,
                    out var roadObjectClass, out var endObjectClass01, out var endObjectClass02,
                    out var overlap01, out var overlap02);

                var roadObject = RoadCreation.CreateRoad(roadDescr, roadObjectClass.spline, elevated, 1f);
                if (LOD) LODCreation.RoadObject(componentSettings, roadObject);

                roadObject.previewRoad = true;

                SpawnObjectUtility.SpawnObjects(_RoadSet.spawnObjectPresets, new List<RoadObject> {roadObject}, new List<IntersectionObject>(),
                    sceneData, new List<int>(), new List<int>());

                roadObject.transform.SetParent(parent);
            }
        }

        /********************************************************************************************************************************/
        // Private
        /********************************************************************************************************************************/

        private void RegisterSceneObjectsInternal(out List<RoadObject> sceneRoadObjects, out List<IntersectionObject> sceneIntersectionObjects)
        {
            sceneRoadObjects = new List<RoadObject>();
            sceneIntersectionObjects = new List<IntersectionObject>();

            if (!IsInitialized()) return;

            var constructionObjects = new ConstructionObjects();

            sceneRoadObjects = FindObjectsOfType<RoadObject>().ToList();
            for (var i = sceneRoadObjects.Count - 1; i >= 0; i--)
            {
                if (sceneRoadObjects[i].previewRoad)
                {
                    sceneRoadObjects.RemoveAt(i);
                    continue;
                }

                if (sceneData.roadObjects.Contains(sceneRoadObjects[i]))
                {
                    sceneRoadObjects.RemoveAt(i);
                    continue;
                }

                if (!TryGetRoadDescr(sceneRoadObjects[i].road.roadName, out var roadDescr))
                {
                    sceneRoadObjects.RemoveAt(i);
                    continue;
                }

                sceneRoadObjects[i].roadDescr = roadDescr;
            }
            

            sceneIntersectionObjects = FindObjectsOfType<IntersectionObject>().ToList();
            for (var i = sceneIntersectionObjects.Count - 1; i >= 0; i--)
            {
                if (sceneData.intersectionObjects.Contains(sceneIntersectionObjects[i]))
                {
                    sceneIntersectionObjects.RemoveAt(i);
                    continue;
                }

                if (!TryGetRoadDescr(sceneIntersectionObjects[i].road.roadName, out var roadDescr))
                {
                    sceneIntersectionObjects.RemoveAt(i);
                    continue;
                }

                sceneIntersectionObjects[i].roadDescr = roadDescr;
            }
            
            ConnectionUtility.CleanNullConnections(sceneRoadObjects);
            ConnectionUtility.CleanNullConnections(sceneIntersectionObjects);
            
            constructionObjects.newRoads.AddRange(sceneRoadObjects);
            constructionObjects.newIntersections.AddRange(sceneIntersectionObjects);
            FinalizeConstruction(constructionObjects, false, false);
        }

        /********************************************************************************************************************************/

        private ConstructionResultRoad DisplayRoadInternal(string roadName, float3 position01, float3 position02, RoadSettings roadSettings)
        {
            if (!IsInitialized()) return new ConstructionResultRoad(false);
            if (!TryGetRoadDescr(roadName, out var roadDescr)) return new ConstructionResultRoad(false);

            ApplyGridPositions(ref position01, ref position02);

            var roadData = CreateRoadData(roadSettings, roadDescr, position01, position02, false,
                out var roadObjectClass, out var endObjectClass01, out var endObjectClass02,
                out var overlap01, out var overlap02);

            RoadEndCreation.CreateEndObjects(componentSettings, _DefaultReferences, roadDescr, roadObjectClass.spline,
                out var endObject01, out var endObject02);

            var overlapBounds = WorldUtility.ExtendBounds(roadObjectClass.splineBounds,
                roadObjectClass.roadDescr.width + componentSettings.minOverlapDistance);
            var ignoreObjects = new List<SceneObject>();
            ignoreObjects.AddRange(OverlapUtility.GetIgnoreObjects(overlap01));
            ignoreObjects.AddRange(OverlapUtility.GetIgnoreObjects(overlap02));
            OverlapUtility.GetAllOverlapIndexes(overlapBounds, ignoreObjects, sceneData,
                out var overlapIntersectionIndexes, out var overlapRoadIndexes);

            var constructionFails = RoadValidation.ValidateRoad(componentSettings, roadData, roadObjectClass.roadDescr,
                roadObjectClass.spline, sceneData, overlapIntersectionIndexes, overlapRoadIndexes);

            var roadObject = RoadCreation.CreateRoad(roadDescr, roadObjectClass.spline, roadData.elevated, 1f);

            var result = new ConstructionResultRoad(roadData, roadObject, overlap01, overlap02, constructionFails);

            constructionFails.AddRange(IntersectionValidation.ValidateIntersection(componentSettings, overlap01));
            constructionFails.AddRange(IntersectionValidation.ValidateIntersection(componentSettings, overlap02));

            result.intersectionObjects.Add(endObject01);
            result.intersectionObjects.Add(endObject02);
            roadObject.transform.SetParent(displayParent.transform);
            endObject01.transform.SetParent(displayParent.transform);
            endObject02.transform.SetParent(displayParent.transform);
            displayedObjects.Add(roadObject.gameObject);
            displayedObjects.Add(endObject01.gameObject);
            displayedObjects.Add(endObject02.gameObject);

            return result;
        }

        private ConstructionResultRoad ConstructRoadInternal(string roadName, float3 position01, float3 position02, RoadSettings roadSettings)
        {
            UndoConstruction.SaveCurrentState(componentSettings);

            if (!IsInitialized()) return new ConstructionResultRoad(false);
            if (!TryGetRoadDescr(roadName, out var roadDescr)) return new ConstructionResultRoad(false);

            var constructionObjects = new ConstructionObjects();

            TerrainUpdateUndo terrainUpdateUndo = default;

            ApplyGridPositions(ref position01, ref position02);

            var roadData = CreateRoadData(roadSettings, roadDescr, position01, position02, true,
                out var roadObjectClass, out var endObjectClass01, out var endObjectClass02,
                out var overlap01, out var overlap02);

            var roadBounds = WorldUtility.ExtendBounds(roadObjectClass.splineBounds,
                roadObjectClass.roadDescr.width + componentSettings.minOverlapDistance);
            var ignoreObjects = new List<SceneObject>();
            ignoreObjects.AddRange(OverlapUtility.GetIgnoreObjects(overlap01));
            ignoreObjects.AddRange(OverlapUtility.GetIgnoreObjects(overlap02));
            OverlapUtility.GetAllOverlapIndexes(roadBounds, ignoreObjects, sceneData,
                out var overlapIntersectionIndexes, out var overlapRoadIndexes);

            var constructionFails = RoadValidation.ValidateRoad(componentSettings, roadData, roadObjectClass.roadDescr,
                roadObjectClass.spline, sceneData, overlapIntersectionIndexes, overlapRoadIndexes);

            var roadObject = RoadCreation.CreateRoad(roadDescr, roadObjectClass.spline, roadData.elevated, 1f);

            constructionObjects.newRoads.Add(roadObject);

            CreateOverlapIntersection(overlap01);
            CreateOverlapIntersection(overlap02);

            void CreateOverlapIntersection(Overlap _overlap)
            {
                if (!_overlap.exists) return;

                constructionFails.AddRange(IntersectionValidation.ValidateIntersection(componentSettings, _overlap));

                if (constructionFails.Count > 0) return;

                if (_overlap.IsExtension(roadObject.road))
                {
                    RoadExtension.CreateExtension(componentSettings, _overlap, roadObject, constructionObjects);
                }
                else if (_overlap.intersectionType == IntersectionType.Intersection)
                {
                    IntersectionCreation.CreateIntersection(componentSettings, _overlap, roadObject, constructionObjects);
                }
                else if (_overlap.intersectionType == IntersectionType.Roundabout)
                {
                    var overlapRoundabout = _overlap.intersectionObject as RoundaboutObject;
                    constructionObjects.removableIntersections.Add(overlapRoundabout);

                    RoundaboutCreation.CreateRoundabout(componentSettings, _overlap, roadObject, constructionObjects, overlapRoundabout.roadDescr, overlapRoundabout.centerPosition, overlapRoundabout.radius);
                }
            }

            var result = new ConstructionResultRoad(roadData, roadObject, overlap01, overlap02, constructionFails);

            /********************************************************************************************************************************/

            if (constructionFails.Count > 0)
            {
                constructionObjects.DestroyNewObjects();
                return result;
            }

            /********************************************************************************************************************************/
            // Terrain
            if (componentSettings.terrainSettings)
            {
                TerrainUpdate.CreateTerrainUpdateSplines(endObjectClass01, endObjectClass02, overlap01, overlap02, constructionObjects,
                    out var roadSplines, out var roadWidths, out var intersectionSplines, out var intersectionWidths);

                terrainUpdateUndo = TerrainUpdate.UpdateTerrain(componentSettings, roadSplines, roadWidths, true);
                terrainUpdateUndo.AddUndo(TerrainUpdate.UpdateTerrain(componentSettings, intersectionSplines, intersectionWidths, false));
            }
            
            /********************************************************************************************************************************/

            result.roadObject = roadObject;
            result.replacedRoadObjects.AddRange(constructionObjects.newReplacedRoads);
            result.intersectionObjects.AddRange(constructionObjects.newIntersections);

            FinalizeConstruction(constructionObjects, true, true, terrainUpdateUndo);

            return result;
        }

        /********************************************************************************************************************************/

        private ConstructionResultRoundabout DisplayRoundaboutInternal(string roadName, float3 position, float radius)
        {
            var result = ConstructionRoundabout(roadName, position, radius,
                out var overlap, out var constructionObjectLists, out var terrainUpdateUndo,
                out var overlapIntersectionIndexes, out var overlapRoadIndexes, false);

            if (!result.isValid) return result;

            for (var i = 0; i < constructionObjectLists.newIntersections.Count; i++)
            {
                var roundabout = constructionObjectLists.newIntersections[i];
                result.intersectionObjects.Add(roundabout);
                roundabout.transform.SetParent(displayParent.transform);
                displayedObjects.Add(roundabout.gameObject);
            }

            return result;
        }

        private ConstructionResultRoundabout ConstructRoundaboutInternal(string roadName, float3 position, float radius)
        {
            UndoConstruction.SaveCurrentState(componentSettings);

            var constructionResult = ConstructionRoundabout(roadName, position, radius,
                out var overlap, out var constructionObjects, out var terrainUpdateUndo,
                out var overlapIntersectionIndexes, out var overlapRoadIndexes, true);

            if (!constructionResult.isValid) return constructionResult;

            if (constructionResult.constructionFails.Count > 0)
            {
                constructionObjects.DestroyNewObjects();
                return constructionResult;
            }

            constructionResult.intersectionObjects.AddRange(constructionObjects.newIntersections);
            constructionResult.replacedRoadObjects.AddRange(constructionObjects.newReplacedRoads);

            FinalizeConstruction(constructionObjects, true, true, terrainUpdateUndo);

            return constructionResult;
        }

        /********************************************************************************************************************************/

        private ConstructionResultRamp DisplayRampInternal(string roadName, float3 position01, float3 position02, RoadSettings roadSettings)
        {
            var resultRoad = DisplayRoadInternal(roadName, position01, position02, roadSettings);
            if (!resultRoad.isValid) return new ConstructionResultRamp(false);

            RoadValidation.ValidateRamp(resultRoad.constructionFails, resultRoad.roadData, resultRoad.roadObject.roadDescr,
                resultRoad.overlap01, resultRoad.overlap02);

            var result = new ConstructionResultRamp(resultRoad.overlap01);
            result.constructionFails = resultRoad.constructionFails;

            return result;
        }

        private ConstructionResultRamp ConstructRampInternal(string roadName, float3 position01, float3 position02, RoadSettings roadSettings)
        {
            UndoConstruction.SaveCurrentState(componentSettings);

            if (!IsInitialized()) return new ConstructionResultRamp(false);
            if (!TryGetRoadDescr(roadName, out var roadDescr)) return new ConstructionResultRamp(false);


            TerrainUpdateUndo terrainUpdateUndo = default;
            var LOD = componentSettings.lodList.Count > 1;

            ApplyGridPositions(ref position01, ref position02);

            var roadData = CreateRoadData(roadSettings, roadDescr, position01, position02, true,
                out var roadObjectClass, out var endObjectClass01, out var endObjectClass02,
                out var overlap01, out var overlap02);

            var overlapBounds = WorldUtility.ExtendBounds(roadObjectClass.splineBounds,
                roadObjectClass.roadDescr.width + componentSettings.minOverlapDistance);
            var ignoreObjects = new List<SceneObject>();
            ignoreObjects.AddRange(OverlapUtility.GetIgnoreObjects(overlap01));
            ignoreObjects.AddRange(OverlapUtility.GetIgnoreObjects(overlap02));
            OverlapUtility.GetAllOverlapIndexes(overlapBounds, ignoreObjects, sceneData,
                out var overlapIntersectionIndexes, out var overlapRoadIndexes);

            var constructionFails = RoadValidation.ValidateRoad(componentSettings, roadData, roadObjectClass.roadDescr,
                roadObjectClass.spline, sceneData, overlapIntersectionIndexes, overlapRoadIndexes);

            RoadValidation.ValidateRamp(constructionFails, roadData, roadObjectClass.roadDescr, overlap01, overlap02);

            var rampObjectClass = new RampObjectClass(roadObjectClass.roadDescr, roadObjectClass.spline, overlap01, overlap02);

            var overlap = rampObjectClass.overlap01.exists ? rampObjectClass.overlap01 : rampObjectClass.overlap02;

            var constructionObjects = RampCreation.CreateRamp(rampObjectClass, overlap, 1f);

            var result = new ConstructionResultRamp(overlap);
            result.intersectionObjects.AddRange(constructionObjects.newIntersections);
            result.replacedRoadObjects.AddRange(constructionObjects.newReplacedRoads);


            terrainUpdateUndo = new TerrainUpdateUndo(new List<Vector2Int>(), new List<float[,]>(), new List<Vector2Int>(), new List<float[,,]>(),
                new List<TerrainDetailUndo>(), new List<TreeInstance[]>());

            FinalizeConstruction(constructionObjects, true, true, terrainUpdateUndo);

            return result;
        }

        /********************************************************************************************************************************/

        private void FinalizeConstruction(ConstructionObjects constructionObjects,
            bool spawnObjects, bool registerUndo, TerrainUpdateUndo terrainUpdateUndo = default)
        {
            AddRoadObjects(constructionObjects.CombinedNewRoads);
            AddIntersectionObjects(constructionObjects.newIntersections);
            
            ConnectionUtility.UpdateConnections(sceneData, constructionObjects);
            
            AddIntersectionObjects(RoadEndCreation.CreateMissingEndObjects(componentSettings, _DefaultReferences, constructionObjects));
            
            ApplyComponentSettings(constructionObjects.CombinedNewObjects);

            InvokeOnRoadsRemoved(constructionObjects.removableRoads, constructionObjects.removableIntersections);
            InvokeOnRoadsAdded(constructionObjects.newRoads, constructionObjects.newIntersections);

            RemoveRoadObjects(constructionObjects.removableRoads);
            RemoveIntersectionObjects(constructionObjects.removableIntersections);
            
            if (spawnObjects) SpawnObjects(constructionObjects);
            
            if(registerUndo) UndoObjectUtility.RegisterUndo(componentSettings, undoParent, undoObjects, constructionObjects, terrainUpdateUndo);
            else constructionObjects.DestroyRemovableObjects();
            
        }
        
        private void ApplyComponentSettings<T>(List<T> sceneObjects) where T : SceneObject
        {
            UpdateColliders(sceneObjects);
            UpdateLayersAndTags(sceneObjects);

            if (componentSettings.addTrafficComponent)
            {
                for (var i = 0; i < sceneObjects.Count; i++) TrafficUtility.AddTrafficComponent(componentSettings, sceneObjects[i], false);

                if (componentSettings.updateWaypoints)
                {
                    var roads = sceneObjects.OfType<RoadObject>().ToList();
                    var intersections = sceneObjects.OfType<IntersectionObject>().ToList(); // Includes inheritors
                    
                    AddWaypoints(roads, intersections, componentSettings.waypointDistance);
                }
            }
            
            var LOD = componentSettings.lodList.Count > 1;
            if (LOD)
            {
                for (int i = 0; i < sceneObjects.Count; i++)
                {
                    var sceneObject = sceneObjects[i];
                    var roadDescr = sceneObject.roadDescr;
                    
                    if(sceneObject is RoadObject)
                        LODCreation.RoadObject(componentSettings, sceneObject as RoadObject);  
                    else if (sceneObject.IsEndObject())
                        LODCreation.EndObject(componentSettings, _DefaultReferences, roadDescr, sceneObject as IntersectionObject);
                    else if (sceneObject.IsIntersection())
                        LODCreation.IntersectionObject(componentSettings, sceneObject as IntersectionObject, sceneObject.RoadConnections);
                    else if (sceneObject.IsRoundabout())
                        LODCreation.RoundaboutObject(componentSettings, roadDescr, sceneObject as RoundaboutObject);
                }
            }
        }

        private void SpawnObjects(ConstructionObjects constructionObjects)
        {
            var combinedBounds = new Bounds();
            for (var i = 0; i < constructionObjects.CombinedNewObjects.Count; i++)
            {
                var splineBounds = constructionObjects.CombinedNewObjects[i].splineContainer.Spline.GetBounds();
                splineBounds = WorldUtility.ExtendBounds(splineBounds,
                    constructionObjects.CombinedNewObjects[i].roadDescr.width + componentSettings.minOverlapDistance);

                combinedBounds.Encapsulate(splineBounds);
            }

            var ignoreObjects = new List<SceneObject>(constructionObjects.CombinedNewObjects);
                
            OverlapUtility.GetAllOverlapIndexes(combinedBounds, ignoreObjects, sceneData,
                out var overlapIntersectionIndexes, out var overlapRoadIndexes);

            OverlapUtility.CleanRemovedIndexes(sceneData.roadObjects, overlapRoadIndexes, constructionObjects.removableRoads);
            OverlapUtility.CleanRemovedIndexes(sceneData.intersectionObjects, overlapIntersectionIndexes, constructionObjects.removableIntersections);
                
            SpawnObjectUtility.SpawnObjects(_RoadSet.spawnObjectPresets, constructionObjects.CombinedNewRoads, constructionObjects.newIntersections,
                sceneData, overlapIntersectionIndexes, overlapRoadIndexes);
        }

        /********************************************************************************************************************************/
        /********************************************************************************************************************************/
        private ConstructionResultRoundabout ConstructionRoundabout(string roadName, float3 position, float radius,
            out Overlap overlap, out ConstructionObjects constructionObjects, out TerrainUpdateUndo terrainUpdateUndo,
            out List<int> overlapIntersectionIndexes, out List<int> overlapRoadIndexes,
            bool construct)
        {
            overlap = default;
            terrainUpdateUndo = default;
            overlapIntersectionIndexes = default;
            overlapRoadIndexes = default;
            constructionObjects = new ConstructionObjects();

            if (!IsInitialized()) return new ConstructionResultRoundabout(false);
            if (!TryGetRoadDescr(roadName, out var roadDescr)) return new ConstructionResultRoundabout(false);

            ApplyGridPosition(ref position);
            
            RoundaboutCreation.CreateRoundabout(componentSettings, new Overlap(), null, constructionObjects, roadDescr, position, radius);

            var roundabout = constructionObjects.newIntersections[0] as RoundaboutObject;

            var result = new ConstructionResultRoundabout(true);
            var constructionFails = result.constructionFails;
            constructionFails.AddRange(IntersectionValidation.ValidateRoundabout(componentSettings, roundabout));

            /********************************************************************************************************************************/
            // Terrain
            if (componentSettings.terrainSettings && construct && constructionFails.Count == 0)
            {
                var splines = roundabout.splineContainer.Splines.ToList();
                var widths = new List<float>();
                for (var i = 0; i < splines.Count; i++) widths.Add(roadDescr.width);
                terrainUpdateUndo = TerrainUpdate.UpdateTerrain(componentSettings, splines, widths, false);
            }

            return result;
        }

        /********************************************************************************************************************************/

        private List<GameObject> DisplayMoveIntersectionInternal(Vector3 currentPosition, float searchRadius, Vector3 newPosition,
            bool deactivateSceneObjects = true)
        {
            if (!GetMoveIntersection(searchRadius, currentPosition, out var intersection))
                return displayedMoveIntersectionObjects;

            var intersectionCopyObj = Instantiate(intersection.gameObject);
            var intersectionCopy = intersectionCopyObj.GetComponent<IntersectionObject>();
            displayedMoveIntersectionObjects.Add(intersectionCopyObj);

            var deltaPosition = newPosition - currentPosition;

            var roadCopies = new List<RoadObject>();
            for (var i = 0; i < intersectionCopy.RoadConnections.Count; i++)
            {
                var roadCopyObj = Instantiate(intersectionCopy.RoadConnections[i].gameObject);
                var roadCopy = roadCopyObj.GetComponent<RoadObject>();
                roadCopies.Add(roadCopy);
                displayedMoveIntersectionObjects.Add(roadCopyObj);
            }

            for (var i = 0; i < roadCopies.Count; i++) ClearMoveObject(roadCopies[i], true);
            ClearMoveObject(intersectionCopy, true);

            OffsetRoadSplines(roadCopies, deltaPosition, newPosition);

            for (var i = 0; i < roadCopies.Count; i++)
            {
                roadCopies[i].roadDescr = sceneData.roadObjects[i].roadDescr;
                UpdateRoadMesh(roadCopies[i], false);
            }

            OffsetIntersectionSpline(intersectionCopy.splineContainer, deltaPosition);

            intersectionCopy.centerPosition += deltaPosition;
            intersectionCopy.ClearRoadConnections();
            intersectionCopy.AddRoadConnections(roadCopies);

            IntersectionUpdate.UpdateIntersections(componentSettings, _DefaultReferences, new List<IntersectionObject> {intersectionCopy});

            for (var i = 0; i < displayedMoveIntersectionObjects.Count; i++)
            {
                var obj = displayedMoveIntersectionObjects[i];
                obj.transform.SetParent(displayMoveIntersectionParent.transform);
            }

            if (deactivateSceneObjects)
            {
                deactivatedObjects.Add(intersection.gameObject);
                intersection.gameObject.SetActive(false);

                for (var i = 0; i < intersection.RoadConnections.Count; i++)
                {
                    deactivatedObjects.Add(intersection.RoadConnections[i].gameObject);
                    intersection.RoadConnections[i].gameObject.SetActive(false);
                }
            }

            return displayedMoveIntersectionObjects;
        }

        private ConstructionResultMoveIntersection MoveIntersectionInternal(Vector3 currentPosition, float searchRadius, Vector3 newPosition)
        {
            if (!GetMoveIntersection(searchRadius, currentPosition, out var intersection))
                return new ConstructionResultMoveIntersection(false);

            var deltaPosition = newPosition - currentPosition;
            var newRoads = intersection.RoadConnections;

            for (var i = 0; i < newRoads.Count; i++) ClearMoveObject(newRoads[i], false);
            ClearMoveObject(intersection, false);

            OffsetRoadSplines(newRoads, deltaPosition, newPosition);
            intersection.centerPosition += deltaPosition;
            OffsetIntersectionSpline(intersection.splineContainer, deltaPosition);

            for (var i = 0; i < newRoads.Count; i++)
            {
                UpdateRoadMesh(newRoads[i], true);
                newRoads[i].length = newRoads[i].splineContainer.Spline.GetLength();
            }

            IntersectionUpdate.UpdateIntersections(componentSettings, _DefaultReferences, new List<IntersectionObject> {intersection});

            var moveResult = new ConstructionResultMoveIntersection(true);
            moveResult.intersectionObjects.Add(intersection);
            moveResult.replacedRoadObjects.AddRange(newRoads);


            /********************************************************************************************************************************/
            // Terrain
            if (componentSettings.terrainSettings && newRoads.Count > 0)
            {
                var constructionObjectLists = new ConstructionObjects();
                constructionObjectLists.newIntersections.Add(intersection);
                constructionObjectLists.newRoads.AddRange(newRoads);

                TerrainUpdate.CreateTerrainUpdateSplines(null, null,
                    new Overlap {exists = true}, new Overlap {exists = true}, constructionObjectLists,
                    out var roadSplines, out var roadWidths, out var intersectionSplines, out var intersectionWidths);

                var terrainUpdateUndo = TerrainUpdate.UpdateTerrain(componentSettings, roadSplines, roadWidths, true);
                terrainUpdateUndo.AddUndo(TerrainUpdate.UpdateTerrain(componentSettings, intersectionSplines, intersectionWidths, false));
            }

            /********************************************************************************************************************************/
            // Object Spawn
            var constructionFails = new List<ConstructionFail>();
            for (var i = 0; i < newRoads.Count; i++)
            {
                var roadData = new ConstructionData();

                var overlapBounds = WorldUtility.ExtendBounds(newRoads[i].meshRenderer.bounds,
                    newRoads[i].roadDescr.width + componentSettings.minOverlapDistance);
                OverlapUtility.GetAllOverlapIndexes(overlapBounds, new List<SceneObject>(), sceneData,
                    out var overlapIntersectionIndexes, out var overlapRoadIndexes);

                for (var j = overlapIntersectionIndexes.Count - 1; j >= 0; j--) // Removing self-overlaps
                    if (sceneData.intersectionObjects[overlapIntersectionIndexes[j]].gameObject.name == intersection.name)
                        overlapIntersectionIndexes.RemoveAt(j);

                for (var j = overlapRoadIndexes.Count - 1; j >= 0; j--) // Removing self-overlaps
                    if (sceneData.roadObjects[overlapRoadIndexes[j]].gameObject.name == newRoads[i].name)
                        overlapRoadIndexes.RemoveAt(j);

                constructionFails.AddRange(RoadValidation.ValidateRoad(componentSettings, roadData, newRoads[i].roadDescr,
                    newRoads[i].splineContainer.Spline, sceneData, overlapIntersectionIndexes, overlapRoadIndexes));

                var newIntersections = new List<IntersectionObject>();
                if (i == 0) newIntersections.Add(intersection);
                SpawnObjectUtility.SpawnObjects(_RoadSet.spawnObjectPresets, new List<RoadObject> {newRoads[i]}, newIntersections,
                    sceneData, overlapIntersectionIndexes, overlapRoadIndexes);
            }

            /********************************************************************************************************************************/

            var sceneObjects = new List<SceneObject>();
            sceneObjects.AddRange(newRoads);
            sceneObjects.Add(intersection);
            for (var i = 0; i < sceneObjects.Count; i++) ObjectUtility.DestroyObject(sceneObjects[i].meshFilter.gameObject.GetComponent<Collider>());

            ApplyComponentSettings(sceneObjects);

            return moveResult;
        }

        private bool GetMoveIntersection(float searchRadius, Vector3 currentPosition, out IntersectionObject intersection)
        {
            intersection = default;

            var overlap = OverlapUtility.GetOverlap(componentSettings, searchRadius, float.MaxValue, currentPosition,
                sceneData);

            if (!overlap.exists || overlap.roadType != RoadType.Intersection || overlap.intersectionType != IntersectionType.Intersection
                || overlap.intersectionObject.RoadConnections.Count < 1)
                return false;

            intersection = overlap.intersectionObject;
            return true;
        }

        private void OffsetIntersectionSpline(SplineContainer splineContainer, float3 deltaPosition)
        {
            var intersectionKnots = splineContainer.Spline.Knots.ToList();
            for (var i = 0; i < intersectionKnots.Count; i++)
            {
                var intersectionKnot = intersectionKnots[i];
                intersectionKnot.Position += deltaPosition;
                splineContainer.Spline.SetKnot(i, intersectionKnot);
            }
        }

        private void OffsetRoadSplines(List<RoadObject> roads, float3 deltaPosition, float3 newPosition)
        {
            for (var i = 0; i < roads.Count; i++)
            {
                var roadSpline = roads[i].splineContainer.Spline;
                var roadKnots = roadSpline.Knots.ToList();
                var nearestKnotIndex = RoadSplineUtility.GetNearestKnotIndex(roadSpline, newPosition);
                var roadKnot = roadKnots[nearestKnotIndex];
                roadKnot.Position += deltaPosition;
                roadSpline.SetKnot(nearestKnotIndex, roadKnot);
                TangentCalculation.CalculateTangents(roadSpline, componentSettings.smoothSlope, componentSettings.tangentLength);
            }
        }

        private void ClearMoveObject(SceneObject sceneObject, bool clearLOD)
        {
            sceneObject.DestroySpawnedObjects();

            var traffic = sceneObject.GetComponentInChildren<Traffic>();
            if (traffic != null) ObjectUtility.DestroyObject(sceneObject.traffic.gameObject);

            if (clearLOD)
            {
                if (sceneObject.meshFilterLODs.Count > 1)
                    for (var j = 1; j < sceneObject.meshFilterLODs.Count; j++)
                        ObjectUtility.DestroyObject(sceneObject.meshFilterLODs[j].gameObject);
                ObjectUtility.DestroyObject(sceneObject.GetComponent<LODGroup>());
            }
        }

        private void UpdateRoadMesh(RoadObject roadObject, bool updateLOD)
        {
            var roadSpline = roadObject.splineContainer.Spline;

            RoadCreation.CreateRoadMesh(roadObject.roadDescr, roadSpline, 0, roadObject.elevated,
                out var _materials, out var newSplineMesh);

            roadObject.meshFilter.mesh = newSplineMesh;
            roadObject.meshRenderer.materials = _materials;

            if (updateLOD)
                if (componentSettings.lodList.Count > 1)
                    for (var j = 1; j < componentSettings.lodList.Count; j++)
                    {
                        if (j >= roadObject.meshFilterLODs.Count) break;

                        var lodAmount = 1f - componentSettings.lodList[j - 1];

                        RoadCreation.CreateRoadMesh(roadObject.roadDescr, roadSpline, lodAmount, roadObject.elevated,
                            out var _materialsLOD, out var newSplineMeshLOD);

                        roadObject.meshFilterLODs[j].sharedMesh = newSplineMeshLOD;
                    }
        }

        /********************************************************************************************************************************/
        /********************************************************************************************************************************/

        private void ApplyGridPositions(ref float3 position01, ref float3 position02)
        {
            ApplyGridPosition(ref position01);
            ApplyGridPosition(ref position02);
        }

        private void ApplyGridPosition(ref float3 position)
        {
            float3 snapPosition = componentSettings.grid;
            float3 snapOffset = componentSettings.gridOffset;

            if (snapPosition.x > 0f) position.x = Mathf.Round(position.x / snapPosition.x) * snapPosition.x;
            if (snapPosition.y > 0f) position.y = Mathf.Round(position.y / snapPosition.y) * snapPosition.y;
            if (snapPosition.z > 0f) position.z = Mathf.Round(position.z / snapPosition.z) * snapPosition.z;
            position += snapOffset;
        }

        private ConstructionData CreateRoadData(RoadSettings roadSettings, RoadDescr roadDescr, float3 position01, float3 position02,
            bool constructInfo,
            out RoadObjectClass roadObjectClass, out EndObjectClass endObjectClass01, out EndObjectClass endObjectClass02,
            out Overlap overlap01, out Overlap overlap02)
        {
            var roadData = RoadCreationData.GenerateRoadData(roadSettings, sceneData,
                roadDescr, position01, position02, constructInfo,
                out overlap01, out overlap02, out var roadSpline);

            roadObjectClass = new RoadObjectClass(roadDescr, roadSpline);

            RoadEndCreation.CreateEndObjectClasses(roadDescr, roadSpline,
                out endObjectClass01, out endObjectClass02);

            return roadData;
        }

        private void AddRoadObjects(List<RoadObject> _roadObjects)
        {
            foreach (var roadObject in _roadObjects)
            {
                if (string.IsNullOrEmpty(roadObject.iD)) roadObject.iD = roadObject.name; // Should be removable in later versions.
                
                roadObject.gameObject.SetActive(true);
                roadObject.transform.SetParent(constructionParent.transform);
                sceneData.AddRoad(roadObject);
            }
        }

        private void AddIntersectionObjects(List<IntersectionObject> _intersectionObjects)
        {
            foreach (var intersectionObject in _intersectionObjects)
            {
                if (string.IsNullOrEmpty(intersectionObject.iD))
                    intersectionObject.iD = intersectionObject.name; // Should be removable in later versions.

                intersectionObject.gameObject.SetActive(true);
                intersectionObject.transform.SetParent(constructionParent.transform);
                sceneData.AddIntersection(intersectionObject);
            }
        }

        private void RemoveRoadObjects(List<RoadObject> _roadObjects)
        {
            if (componentSettings.addTrafficComponent && componentSettings.updateWaypoints)
                RemoveConnectingWaypoints(_roadObjects);

            var indicesToDelete = new List<int>();
            for (var i = 0; i < sceneData.roadObjects.Count; i++)
                if (_roadObjects.Contains(sceneData.roadObjects[i]))
                    indicesToDelete.Add(i);

            indicesToDelete.Reverse();
            foreach (var index in indicesToDelete) sceneData.RemoveRoad(index);
        }

        private void RemoveIntersectionObjects(List<IntersectionObject> _intersectionObjects)
        {
            var indicesToDelete = new List<int>();
            for (var i = 0; i < sceneData.intersectionObjects.Count; i++)
                if (_intersectionObjects.Contains(sceneData.intersectionObjects[i]))
                    indicesToDelete.Add(i);

            indicesToDelete.Reverse();
            foreach (var index in indicesToDelete) sceneData.RemoveIntersection(index);
        }

        private List<GameObject> DisplayDemolishObjectsInternal(Vector3 position, float searchRadius, bool deactivateSceneObjects)
        {
            PampelGames.RoadConstructor.Demolish.GetDemolishSceneObjects(componentSettings, position, searchRadius, sceneData,
                out var demolishIntersections, out var demolishRoads, out var overlap);

            if (!overlap.exists) return displayedDemolishObjects;

            if (overlap.roadType == RoadType.Intersection)
            {
                var intersection = Instantiate(overlap.intersectionObject.gameObject);
                displayedDemolishObjects.Add(intersection);
            }
            else
            {
                var road = Instantiate(overlap.roadObject.gameObject);
                displayedDemolishObjects.Add(road);

                for (var i = 0; i < overlap.roadObject.RoadConnections.Count; i++)
                {
                    var roadConnection = overlap.roadObject.RoadConnections[i];
                    if (!roadConnection.snapPositionSet) continue;
                    var snapRoad = Instantiate(roadConnection.gameObject);
                    displayedDemolishObjects.Add(snapRoad);
                }
            }

            for (var i = 0; i < displayedDemolishObjects.Count; i++)
            {
                var obj = displayedDemolishObjects[i];
                obj.transform.SetParent(displayDemolishParent.transform);
            }

            if (deactivateSceneObjects)
            {
                for (var i = 0; i < demolishIntersections.Count; i++)
                {
                    deactivatedObjects.Add(demolishIntersections[i].gameObject);
                    demolishIntersections[i].gameObject.SetActive(false);
                }

                for (var i = 0; i < demolishRoads.Count; i++)
                {
                    deactivatedObjects.Add(demolishRoads[i].gameObject);
                    demolishRoads[i].gameObject.SetActive(false);
                }
            }

            return displayedDemolishObjects;
        }

        private void DemolishObjectsInternal(Vector3 position, float radius)
        {
            PampelGames.RoadConstructor.Demolish.GetDemolishSceneObjects(componentSettings, position, radius, sceneData,
                out var demolishIntersections, out var demolishRoads, out var overlap);

            PampelGames.RoadConstructor.Demolish.UpdateSceneObjects(componentSettings, _DefaultReferences,
                demolishIntersections, demolishRoads,
                out var recreateIntersections);

            var constructionObjects = new ConstructionObjects();
            constructionObjects.removableRoads.AddRange(demolishRoads);
            constructionObjects.removableIntersections.AddRange(demolishIntersections);
            constructionObjects.newIntersections.AddRange(recreateIntersections);

            var removableObjects = constructionObjects.CombinedRemovableObjects;
            for (int i = 0; i < removableObjects.Count; i++)
            {
                var roadConnections = removableObjects[i].RoadConnections;
                for (int j = 0; j < roadConnections.Count; j++)
                {
                    var roadConnection = roadConnections[j];
                    if(demolishRoads.Contains(roadConnection)) continue;

                    var replacedRoad = RoadCreation.CreateReplaceRoadObject(roadConnection, roadConnection.splineContainer.Spline, 1f);
                    constructionObjects.newReplacedRoads.Add(replacedRoad);
                    constructionObjects.removableRoads.Add(roadConnection);
                }
            }
            
            FinalizeConstruction(constructionObjects, true, false, null);

            ClearUndoStorage();
        }

        private void UndoInternal()
        {
            if (undoObjects.Count == 0) return;

            var dequeuedUndo = undoObjects.Last.Value;

            if (componentSettings.addTrafficComponent && componentSettings.updateWaypoints)
                AddWaypoints(dequeuedUndo.constructionObjects.removableRoads, dequeuedUndo.constructionObjects.removableIntersections,
                    componentSettings.waypointDistance);

            var terrainFitUndo = dequeuedUndo.TerrainUpdateUndo;
            if (terrainFitUndo != null)
            {
                var terrainData = componentSettings.terrain.terrainData;
                for (var i = 0; i < terrainFitUndo.undoHeightPixels.Count; i++)
                {
                    var pixels = terrainFitUndo.undoHeightPixels[i];
                    var heights = terrainFitUndo.undoHeights;
                    terrainData.SetHeightsDelayLOD(pixels.x, pixels.y, heights[i]);
                }

                for (var i = 0; i < terrainFitUndo.undoAlphamaps.Count; i++)
                {
                    var alphaPixels = terrainFitUndo.undoAlphaPixels[i];
                    var alphamap = terrainFitUndo.undoAlphamaps[i];
                    if (alphamap.Length == 0) continue;
                    terrainData.SetAlphamaps(alphaPixels.x, alphaPixels.y, alphamap);
                }

                for (var i = 0; i < terrainFitUndo.undoDetails.Count; i++)
                {
                    var undoDetails = terrainFitUndo.undoDetails[i];
                    for (var j = 0; j < undoDetails.detailLayers.Count; j++)
                        terrainData.SetDetailLayer(undoDetails.detailPixels.x, undoDetails.detailPixels.y, j, undoDetails.detailLayers[j]);
                }

                terrainData.SyncHeightmap();

                var treeInstances = terrainData.treeInstances.ToList();
                for (var i = 0; i < terrainFitUndo.undoTrees.Count; i++) treeInstances.AddRange(terrainFitUndo.undoTrees[i]);
                terrainData.SetTreeInstances(treeInstances.ToArray(), true);
            }

            /********************************************************************************************************************************/
            // Terrain
            // Roads only
            if (componentSettings.terrainSettings && dequeuedUndo.constructionObjects.removableRoads.Count > 0)
            {
                TerrainUpdate.CreateTerrainUpdateSplines(null, null,
                    new Overlap {exists = true}, new Overlap {exists = true}, dequeuedUndo.constructionObjects,
                    out var roadSplines, out var roadWidths, out var intersectionSplines, out var intersectionWidths);

                TerrainUpdate.UpdateTerrain(componentSettings, roadSplines, roadWidths, true);
            }
            /********************************************************************************************************************************/

            var constructionObjects = new ConstructionObjects();
            constructionObjects.newReplacedRoads.AddRange(dequeuedUndo.constructionObjects.removableRoads);
            constructionObjects.newIntersections.AddRange(dequeuedUndo.constructionObjects.removableIntersections);
            constructionObjects.removableIntersections.AddRange(dequeuedUndo.constructionObjects.newIntersections);
            constructionObjects.removableRoads.AddRange(dequeuedUndo.constructionObjects.CombinedNewRoads);
            
            FinalizeConstruction(constructionObjects, true, false);
            
            undoObjects.RemoveLast();
            ObjectUtility.DestroyObject(dequeuedUndo.gameObject);
        }

        /********************************************************************************************************************************/
        /********************************************************************************************************************************/

        private void InvokeOnRoadsAdded(List<RoadObject> _roadObjects, List<IntersectionObject> _intersectionObjects)
        {
            if (_roadObjects.Count == 0 && _intersectionObjects.Count == 0) return;
            OnRoadsAdded(_roadObjects, _intersectionObjects);
        }

        private void InvokeOnRoadsRemoved(List<RoadObject> _roadObjects, List<IntersectionObject> _intersectionObjects)
        {
            if (_roadObjects.Count == 0 && _intersectionObjects.Count == 0) return;
            OnRoadsRemoved(_roadObjects, _intersectionObjects);
        }
    }
}