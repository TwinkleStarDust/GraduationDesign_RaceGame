// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System;
using System.Collections.Generic;
using PampelGames.Shared;
using PampelGames.Shared.Editor;
using PampelGames.Shared.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PampelGames.RoadConstructor.Editor
{
    [CustomEditor(typeof(RoadBuilder))]
    public class RoadBuilderInspector : UnityEditor.Editor
    {
        public VisualTreeAsset _visualTree;
        private VisualElement container;
        private RoadBuilder _roadBuilder;

        /********************************************************************************************************************************/

        private ToolbarButton documentation;

        private VisualElement Initialized;
        private ToolbarToggle roadConstructorSetup;
        private GroupBox RoadConstructorGroup;
        private ObjectField roadConstructor;
        private Button initializeButton;
        private Button uninitializeButton;
        private Button registerSceneObjectsButton;
        private Button createTrafficLanesButton;
        private Button createWaypointsButton;
        private Button removeTrafficSystemButton;
        private Button updateCollidersButton;
        private Button updateLayersTagsButton;
        private Toggle checkExistingMeshes;
        private Button exportButton;
        private Button cleanUpConnectionsButton;

        private ToolbarButton undo;
        private ToolbarToggle demolish;

        private EnumField builderRoadType;
        private FloatField roundAboutRadius;

        private Label buildingParameter;
        private Label constructionData;
        private Label constructionFails;

        private EnumField increaseHeight;
        private EnumField decreaseHeight;
        private EnumField increaseRadius;
        private EnumField decreaseRadius;
        private FloatField deltaSpeed;
        private EnumField fixTangent1;
        private EnumField fixTangent2;
        private EnumField detachRoad;
        private Toggle continuous;

        private VisualElement Roads;


        /********************************************************************************************************************************/
        protected void OnEnable()
        {
            container = new VisualElement();
            _visualTree.CloneTree(container);
            _roadBuilder = target as RoadBuilder;

            FindElements(container);
            BindElements();
            VisualizeElements();
        }

        /********************************************************************************************************************************/

        private void FindElements(VisualElement root)
        {
            documentation = root.Q<ToolbarButton>(nameof(documentation));

            checkExistingMeshes = root.Q<Toggle>(nameof(checkExistingMeshes));
            exportButton = root.Q<Button>(nameof(exportButton));

            Initialized = root.Q<VisualElement>(nameof(Initialized));

            roadConstructorSetup = root.Q<ToolbarToggle>(nameof(roadConstructorSetup));
            RoadConstructorGroup = root.Q<GroupBox>(nameof(RoadConstructorGroup));

            roadConstructor = root.Q<ObjectField>(nameof(roadConstructor));

            initializeButton = root.Q<Button>(nameof(initializeButton));
            uninitializeButton = root.Q<Button>(nameof(uninitializeButton));
            registerSceneObjectsButton = root.Q<Button>(nameof(registerSceneObjectsButton));
            createTrafficLanesButton = root.Q<Button>(nameof(createTrafficLanesButton));
            createWaypointsButton = root.Q<Button>(nameof(createWaypointsButton));
            removeTrafficSystemButton = root.Q<Button>(nameof(removeTrafficSystemButton));
            updateCollidersButton = root.Q<Button>(nameof(updateCollidersButton));
            updateLayersTagsButton = root.Q<Button>(nameof(updateLayersTagsButton));
            cleanUpConnectionsButton = root.Q<Button>(nameof(cleanUpConnectionsButton));

            undo = root.Q<ToolbarButton>(nameof(undo));
            demolish = root.Q<ToolbarToggle>(nameof(demolish));

            builderRoadType = root.Q<EnumField>(nameof(builderRoadType));
            roundAboutRadius = root.Q<FloatField>(nameof(roundAboutRadius));

            buildingParameter = root.Q<Label>(nameof(buildingParameter));
            constructionData = root.Q<Label>(nameof(constructionData));
            constructionFails = root.Q<Label>(nameof(constructionFails));

            increaseHeight = root.Q<EnumField>(nameof(increaseHeight));
            decreaseHeight = root.Q<EnumField>(nameof(decreaseHeight));
            increaseRadius = root.Q<EnumField>(nameof(increaseRadius));
            decreaseRadius = root.Q<EnumField>(nameof(decreaseRadius));
            deltaSpeed = root.Q<FloatField>(nameof(deltaSpeed));
            fixTangent1 = root.Q<EnumField>(nameof(fixTangent1));
            fixTangent2 = root.Q<EnumField>(nameof(fixTangent2));
            detachRoad = root.Q<EnumField>(nameof(detachRoad));
            continuous = root.Q<Toggle>(nameof(continuous));


            Roads = root.Q<VisualElement>(nameof(Roads));
        }

        private void BindElements()
        {
            checkExistingMeshes.PGSetupBindProperty(serializedObject, nameof(checkExistingMeshes));

            roadConstructorSetup.PGSetupBindProperty(serializedObject, nameof(RoadBuilder._editorSettingsVisible));
            roadConstructor.PGSetupBindProperty(serializedObject, nameof(roadConstructor));

            builderRoadType.PGSetupBindProperty(serializedObject, nameof(RoadBuilder.builderRoadType));
            roundAboutRadius.PGSetupBindProperty(serializedObject, nameof(roundAboutRadius));

            increaseHeight.PGSetupBindProperty(serializedObject, nameof(increaseHeight));
            decreaseHeight.PGSetupBindProperty(serializedObject, nameof(decreaseHeight));
            increaseRadius.PGSetupBindProperty(serializedObject, nameof(increaseRadius));
            decreaseRadius.PGSetupBindProperty(serializedObject, nameof(decreaseRadius));
            deltaSpeed.PGSetupBindProperty(serializedObject, nameof(deltaSpeed));
            fixTangent1.PGSetupBindProperty(serializedObject, nameof(fixTangent1));
            fixTangent2.PGSetupBindProperty(serializedObject, nameof(fixTangent2));
            detachRoad.PGSetupBindProperty(serializedObject, nameof(detachRoad));
            continuous.PGSetupBindProperty(serializedObject, nameof(continuous));
        }

        private void VisualizeElements()
        {
            fixTangent1.tooltip =
                "Fixes the start tangent to create curvature. This requires the current road segment to be connected to an existing road or intersection.";
            fixTangent2.tooltip =
                "Fixes the end tangent to create curvature. This requires the current road segment to be connected to an existing road or intersection.";
            
            roadConstructorSetup.tooltip = "Show/hide builder settings.";

            checkExistingMeshes.tooltip = "Checks the project folder for each mesh before exporting.\n" +
                                          "If set to false, each mesh will be exported regardless of whether one already exists.";
            exportButton.tooltip = "Export road and intersection meshes into the project folder.";

            documentation.tooltip = "Open the documentation page.";

            roadConstructor.objectType = typeof(RoadConstructor);
            roadConstructor.tooltip = "Reference to the Road Constructor component in the scene.";

            initializeButton.tooltip = "Initializes Road Constructor.";
            uninitializeButton.tooltip = "Uninitializes Road Constructor and reparents constructed parts to the scene.";
            registerSceneObjectsButton.tooltip = "Registers existing scene objects for construction.\n" + "\n" +
                                                 "The construction set needs to contain the road for each object to be registered.";
            createTrafficLanesButton.tooltip =
                "Remove existing Traffic components and create new ones for the existing road system.\n" +
                "The traffic component is necessary to create waypoints.";
            createWaypointsButton.tooltip = "Create interconnected waypoints for the existing road system.\n" +
                                            "Requires the traffic component on the constructed roads.";
            removeTrafficSystemButton.tooltip = "Removes all traffic components and waypoints from the system.";
            updateCollidersButton.tooltip = "Update colliders for all registered objects based on the component settings.";
            updateLayersTagsButton.tooltip = "Update layers and tags for registered objects based on the component settings.";
            cleanUpConnectionsButton.tooltip = "Clears missing connections, which can occur if roads are manually removed from the scene.";

            detachRoad.tooltip = "Resets the positions, which disconnects the displayed roads.\n" +
                                 "Also applicable with right-mouse click.";

            roundAboutRadius.PGClampValue();
        }

        /********************************************************************************************************************************/
        /********************************************************************************************************************************/

        private void FocusSceneView()
        {
            if (SceneView.sceneViews.Count <= 0) return;
            var sceneView = (SceneView) SceneView.sceneViews[0];
            sceneView.Focus();
        }

        private void OnSceneGUI()
        {
            Update();
            UpdateMouseSelection();
        }

        private float lastTime;
        private float deltaTime;
        private Vector3 pointerPosition;
        private Vector3 pointerDemolishPosition;

        private bool setTangent01Pressed;
        private bool setTangent02Pressed;

        private void Update()
        {
            if (_roadBuilder.roadConstructor == null) return;
            if (!_roadBuilder.roadConstructor.IsInitialized()) return;

            _roadBuilder.roadConstructor.ClearAllDisplayObjects();

            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (!Physics.Raycast(ray, out var hit))
            {
                _roadBuilder.SetPointerActive(false);
                _roadBuilder.SetPointerDemolishActive(false);
                return;
            }

            _roadBuilder.SetPointerDemolishActive(_roadBuilder.IsDemolishActive());

            if (_roadBuilder.IsDemolishActive())
            {
                setTangent01Pressed = false;
                setTangent02Pressed = false;

                var radius = Mathf.Abs(_roadBuilder.roadConstructor.componentSettings.heightRange.y) + 1f;
                pointerDemolishPosition = _roadBuilder.SnapPointerDemolish(radius, hit.point, hit.normal);

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    _roadBuilder.roadConstructor.Demolish(pointerDemolishPosition, radius);
                }
                else
                {
                    // Creating display objects, only used for visual representation.
                    var demolishDisplayObjects = _roadBuilder.roadConstructor.DisplayDemolishObjects(pointerDemolishPosition, radius);
                }

                return;
            }

            var activeRoad = _roadBuilder.GetActiveRoad();
            if (string.IsNullOrEmpty(activeRoad)) return;

            deltaTime = (float) EditorApplication.timeSinceStartup - lastTime;
            deltaTime *= _roadBuilder.deltaSpeed;
            lastTime = (float) EditorApplication.timeSinceStartup;
            var deltaHeight = _roadBuilder.GetDeltaHeight();

            pointerPosition = _roadBuilder.SnapPointer(hit.point, hit.normal);

            ConstructionResult result;
            var roadSettings = new RoadSettings();

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == _roadBuilder.increaseHeight)
                    _roadBuilder.SetDeltaHeight(deltaHeight + deltaTime);
                else if (Event.current.keyCode == _roadBuilder.decreaseHeight)
                    _roadBuilder.SetDeltaHeight(deltaHeight - deltaTime);
                else if (Event.current.keyCode == _roadBuilder.increaseRadius)
                    _roadBuilder.SetRadius(_roadBuilder.GetRadius() + deltaTime);
                else if (Event.current.keyCode == _roadBuilder.decreaseRadius)
                    _roadBuilder.SetRadius(_roadBuilder.GetRadius() - deltaTime);
            }

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == _roadBuilder.fixTangent1) setTangent01Pressed = true;
                if (Event.current.keyCode == _roadBuilder.fixTangent2) setTangent02Pressed = true;
            }

            if (Event.current.type == EventType.KeyUp)
            {
                if (Event.current.keyCode == _roadBuilder.fixTangent1) setTangent01Pressed = false;
                if (Event.current.keyCode == _roadBuilder.fixTangent2) setTangent02Pressed = false;
            }

            if (setTangent01Pressed)
            {
                roadSettings.setTangent01 = true;
                roadSettings.tangent01 = _roadBuilder.lastTangent01;
            }

            if (setTangent02Pressed)
            {
                roadSettings.setTangent02 = true;
                roadSettings.tangent02 = _roadBuilder.lastTangent02;
            }


            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (_roadBuilder.builderRoadType == BuilderRoadType.Road) result = _roadBuilder.ConstructRoad(pointerPosition, roadSettings);
                  else if (_roadBuilder.builderRoadType == BuilderRoadType.Roundabout) result = _roadBuilder.ConstructRoundabout(pointerPosition);
                else result = _roadBuilder.ConstructRamp(pointerPosition, roadSettings);
            }
            else
            {
                if (_roadBuilder.builderRoadType == BuilderRoadType.Road) result = _roadBuilder.DisplayRoad(pointerPosition, roadSettings);
                else if (_roadBuilder.builderRoadType == BuilderRoadType.Roundabout) result = _roadBuilder.DisplayRoundabout(pointerPosition);
                else result = _roadBuilder.DisplayRamp(pointerPosition, roadSettings);
            }

            if (result.isValid && result.GetType() == typeof(ConstructionResultRoad))
            {
                var roadResult = (ConstructionResultRoad) result;
                if (!roadSettings.setTangent01) _roadBuilder.lastTangent01 = roadResult.roadData.tangent01;
                if (!roadSettings.setTangent02) _roadBuilder.lastTangent02 = roadResult.roadData.tangent02;
            }

            if ((Event.current.type == EventType.MouseDown && Event.current.button == 1) ||
                (Event.current.type == EventType.KeyDown && Event.current.keyCode == _roadBuilder.detachRoad))
                _roadBuilder.ResetValues();

            buildingParameter.text = _roadBuilder.BuildingParameterText();
            constructionData.text = _roadBuilder.ConstructionDataText(result);
            constructionFails.text = _roadBuilder.ConstructionFailText(result);

            InfoLabelsDisplay();
        }

        private void InfoLabelsDisplay()
        {
            buildingParameter.style.display = string.IsNullOrEmpty(buildingParameter.text) || buildingParameter.text == "Elevation: 0"
                ? new StyleEnum<DisplayStyle>(DisplayStyle.None)
                : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            constructionData.style.display = string.IsNullOrEmpty(constructionData.text)
                ? new StyleEnum<DisplayStyle>(DisplayStyle.None)
                : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            constructionFails.style.display = string.IsNullOrEmpty(constructionFails.text)
                ? new StyleEnum<DisplayStyle>(DisplayStyle.None)
                : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }


        // Make sure mouse selection remains on the component.
        private void UpdateMouseSelection()
        {
            var demolishActive = _roadBuilder.IsDemolishActive();
            var activeRoad = _roadBuilder.GetActiveRoad();

            if (string.IsNullOrEmpty(activeRoad) && !demolishActive) return;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                GUIUtility.hotControl = 0;
                Event.current.Use();
                Selection.activeGameObject = _roadBuilder.gameObject;
            }
        }


        /********************************************************************************************************************************/
        /********************************************************************************************************************************/

        public override VisualElement CreateInspectorGUI()
        {
            DrawTopToolbar();
            DrawRoadTypeSelection();
            DrawExportSettings();
            DrawButtons();
            InfoLabelsDisplay();
            DrawRoads();

            return container;
        }

        /********************************************************************************************************************************/

        private void DrawTopToolbar()
        {
            documentation.clicked += () => { Application.OpenURL(Constants.DocumentationURL); };

            roadConstructorSetup.RegisterValueChangedCallback(evt =>
            {
                RoadConstructorGroup.PGDisplayStyleFlex(_roadBuilder._editorSettingsVisible);
            });
        }

        /********************************************************************************************************************************/

        private void DrawRoadTypeSelection()
        {
            roundAboutRadius.PGDisplayStyleFlex(_roadBuilder.builderRoadType == BuilderRoadType.Roundabout);
            builderRoadType.RegisterValueChangedCallback(evt =>
            {
                roundAboutRadius.PGDisplayStyleFlex(_roadBuilder.builderRoadType == BuilderRoadType.Roundabout);
                FocusSceneView();
            });
        }

        /********************************************************************************************************************************/
        private void DrawExportSettings()
        {
            if (_roadBuilder.roadConstructor == null) return;
            exportButton.clicked += () => { _roadBuilder.roadConstructor.ExportMeshes(_roadBuilder.checkExistingMeshes); };
        }

        /********************************************************************************************************************************/

        private void DrawButtons()
        {
            InitializedDisplay();

            initializeButton.clicked += () =>
            {
                if (_roadBuilder.roadConstructor == null) return;
                if (_roadBuilder.roadConstructor._RoadSet == null) return;

                _roadBuilder.builderRoadType = BuilderRoadType.Road;
                EditorUtility.SetDirty(_roadBuilder);

                SetRoadButtonsInactive();

                _roadBuilder.SetDemolishActive(false);
                _roadBuilder.roadConstructor.Initialize();
                _roadBuilder.InitializePointer();
                InitializedDisplay();
                buildingParameter.text = "";
                constructionData.text = "";
                constructionFails.text = "";
                InfoLabelsDisplay();
            };
            uninitializeButton.clicked += () =>
            {
                if (_roadBuilder.roadConstructor == null) return;

                var constructionParent = _roadBuilder.roadConstructor.GetConstructionParent();
                if (constructionParent != null && constructionParent.childCount > 0)
                {
                    constructionParent.parent = null;
                    constructionParent.name += " " + DateTime.Now;
                }

                SetRoadButtonsInactive();
                _roadBuilder.SetDemolishActive(false);
                _roadBuilder.roadConstructor.Uninitialize();
                _roadBuilder.DestroyPointers();
                InfoLabelsDisplay();
                InitializedDisplay();
            };
            registerSceneObjectsButton.clicked += () =>
            {
                if (_roadBuilder.roadConstructor == null) return;
                _roadBuilder.roadConstructor.RegisterSceneObjects(out var sceneRoadObjects, out var sceneIntersectionObjects);
                Debug.Log(
                    "Registered " + sceneRoadObjects.Count + " roads and " + sceneIntersectionObjects.Count + " intersections for construction.");
                
                CheckConnections();
            };

            createTrafficLanesButton.clicked += () =>
            {
                if (_roadBuilder.roadConstructor == null) return;
                _roadBuilder.roadConstructor.AddTrafficComponents();
                Debug.Log("Traffic components successfully created.");
            };
            createWaypointsButton.clicked += () =>
            {
                if (_roadBuilder.roadConstructor == null) return;
                if (!CheckConnections()) return;
                _roadBuilder.roadConstructor.CreateAllWaypoints();
                Debug.Log("Waypoints successfully created.");
            };
            removeTrafficSystemButton.clicked += () =>
            {
                if (_roadBuilder.roadConstructor == null) return;
                _roadBuilder.roadConstructor.RemoveTrafficSystem();
                Debug.Log("Traffic system cleared successfully.");
            };
            updateCollidersButton.clicked += () =>
            {
                if (_roadBuilder.roadConstructor == null) return;
                var sceneObjects = _roadBuilder.roadConstructor.GetSceneObjects();
                _roadBuilder.roadConstructor.UpdateColliders(sceneObjects);
            };
            updateLayersTagsButton.clicked += () =>
            {
                if (_roadBuilder.roadConstructor == null) return;
                var sceneObjects = _roadBuilder.roadConstructor.GetSceneObjects();
                _roadBuilder.roadConstructor.UpdateLayersAndTags(sceneObjects);
            };

            cleanUpConnectionsButton.clicked += () =>
            {
                if (_roadBuilder.roadConstructor == null) return;
                CleanUpConnections();
            };

            void SetRoadButtonsInactive()
            {
                for (var i = 0; i < _roadBuilder.roadConstructor._RoadSet.roads.Count; i++)
                {
                    var roadToggle = Roads.Q<ToolbarToggle>("roadToggle" + i);
                    if (roadToggle == null) continue;
                    roadToggle.value = false;
                }
            }

            undo.clicked += () =>
            {
                if (_roadBuilder.roadConstructor != null) _roadBuilder.roadConstructor.UndoLastConstruction();
            };

            if (_roadBuilder.IsDemolishActive()) demolish.SetValueWithoutNotify(true);
            demolish.RegisterValueChangedCallback(evt =>
            {
                SetRoadButtonsInactive();
                _roadBuilder.SetDemolishActive(demolish.value);
                EditorUtility.SetDirty(_roadBuilder);
            });
        }

        private bool CheckConnections() 
        {
            bool connectionsValid = true;
            var roads = _roadBuilder.roadConstructor.GetRoads();
            for (int i = 0; i < roads.Count; i++)
            {
                var roadConnections = roads[i].RoadConnections;
                for (int j = 0; j < roadConnections.Count; j++)
                {
                    if (roadConnections[j] == null)
                    {
                        Debug.LogWarning("The road: " + roads[i].iD + " has a missing road connection.\n" +
                                         "Either remove it manually or use the 'Clean Up Connections' button.");
                        connectionsValid = false;
                    }
                }

                var intersectionConnections = roads[i].IntersectionConnections;
                for (int k = 0; k < intersectionConnections.Count; k++)
                {
                    if (intersectionConnections[k] == null)
                    {
                        Debug.LogWarning("The road: " + roads[i].iD + " has a missing intersection connection.\n" +
                                         "Either remove it manually or use the 'Clean Up Connections' button.");
                        connectionsValid = false;
                    }
                }
            }

            var intersections = _roadBuilder.roadConstructor.GetIntersections();
            for (int i = 0; i < intersections.Count; i++)
            {
                var roadConnections = intersections[i].RoadConnections;
                for (int j = 0; j < roadConnections.Count; j++)
                {
                    if (roadConnections[j] == null)
                    {
                        Debug.LogWarning("The intersection: " + intersections[i].iD + " has a missing road connection.\n" +
                                         "Either remove it manually or use the 'Clean Up Connections' button.");
                        connectionsValid = false;
                    }
                }
            }

            return connectionsValid;
        }

        private void CleanUpConnections()
        {
            var sceneObjects = _roadBuilder.roadConstructor.GetSceneObjects();
            ConnectionUtility.CleanNullConnections(sceneObjects);
            for (int i = 0; i < sceneObjects.Count; i++) EditorUtility.SetDirty(sceneObjects[i]);
            Debug.Log("Connections successfully cleaned.");
        }

        private void InitializedDisplay()
        {
            if (_roadBuilder.gameObject.scene.name == null) return;
            var initialized = _roadBuilder.roadConstructor != null && _roadBuilder.roadConstructor.IsInitialized();
            Initialized.PGDisplayStyleFlex(initialized);
            initializeButton.PGDisplayStyleFlex(!initialized);
        }

        /********************************************************************************************************************************/

        private void DrawRoads()
        {
            Roads.Clear();
            var _roadConstructor = _roadBuilder.roadConstructor;
            if (_roadConstructor == null) return;
            var _constructionSet = _roadConstructor._RoadSet;
            if (_constructionSet == null) return;

            for (var i = 0; i < _constructionSet.roads.Count; i++)
            {
                var road = _constructionSet.roads[i];

                var roadToggle = new ToolbarToggle();
                roadToggle.name = nameof(roadToggle) + i;

                var allLanes = road.GetAllLanes(_roadConstructor._RoadSet.lanePresets);
                roadToggle.text += road.GetRoadDisplayText(allLanes);

                SetRoadStyle(roadToggle);

                var i1 = i;
                roadToggle.RegisterValueChangedCallback(evt =>
                {
                    setTangent01Pressed = false;
                    setTangent02Pressed = false;

                    if (roadToggle.value)
                    {
                        _roadBuilder.SetDemolishActive(false);
                        demolish.SetValueWithoutNotify(false);

                        _roadBuilder.InitializePointer();
                        _roadBuilder.ActivateRoad(road.roadName);
                        EditorUtility.SetDirty(_roadBuilder);
                        FocusSceneView();
                    }
                    else
                    {
                        _roadBuilder.DeactivateRoad();
                    }

                    var activeRoad = _roadBuilder.GetActiveRoad();


                    for (var j = 0; j < _constructionSet.roads.Count; j++)
                    {
                        if (i1 == j) continue;
                        var innerRoadToggle = Roads.Q<ToolbarToggle>(nameof(roadToggle) + j);
                        if (innerRoadToggle == null) continue;
                        if (innerRoadToggle.text == activeRoad) continue;
                        innerRoadToggle.SetValueWithoutNotify(false);
                    }
                });

                Roads.Add(roadToggle);
            }
        }

        private void SetRoadStyle(ToolbarToggle roadToggle)
        {
            roadToggle.style.height = 33;
            roadToggle.style.marginBottom = 3;
            roadToggle.PGBorderWidth(1);
        }


        /********************************************************************************************************************************/

        private void OnDisable()
        {
            if (_roadBuilder == null) return;
            _roadBuilder.DeactivateRoad();
            _roadBuilder.ResetValues();
            _roadBuilder.SetPointerActive(false);
            _roadBuilder.SetPointerDemolishActive(false);
            EditorUtility.SetDirty(_roadBuilder);
        }
    }
}