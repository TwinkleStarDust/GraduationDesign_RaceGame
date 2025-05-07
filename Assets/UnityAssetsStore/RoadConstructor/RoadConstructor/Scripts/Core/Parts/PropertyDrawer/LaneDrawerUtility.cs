// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

#if UNITY_EDITOR
using System.Collections.Generic;
using PampelGames.Shared;
using PampelGames.Shared.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PampelGames.RoadConstructor
{
    public static class LaneDrawerUtility
    {
        public static ListView CreateLaneItemsListView(List<SplineEdgeEditor> itemSource, SerializedProperty property)
        {
            var splineEdgesListView = new ListView();

            splineEdgesListView.showBoundCollectionSize = false;
            splineEdgesListView.itemsSource = itemSource;
            splineEdgesListView.PGObjectListViewStyle();
            splineEdgesListView.reorderable = true;
            splineEdgesListView.reorderMode = ListViewReorderMode.Animated;
            splineEdgesListView.headerTitle = "Construction Lanes";
            splineEdgesListView.showFoldoutHeader = true;

            splineEdgesListView.makeItem = CreateLaneItem;

            splineEdgesListView.bindItem = (item, j) =>
            {
                property.serializedObject.Update();
                var splineEdgeProperty = property.GetArrayElementAtIndex(j);

                BindLaneItem(item, splineEdgeProperty, itemSource[j]);
            };

            return splineEdgesListView;
        }

        private static VisualElement CreateLaneItem()
        {
            var item = new VisualElement();

            item.style.marginLeft = 6;
            item.style.marginBottom = 3;

            item.PGPadding(2);
            item.PGBorderWidth(1);
            item.PGBorderColor(PGColors.InspectorBorder());

            var laneType = new EnumField("Type");
            laneType.name = nameof(SplineEdgeEditor.laneType);
            var laneTypeTooltip = "Road: This lane is used for the roads (required).\n\n" +
                                  "Optional Lanes:\n\n" +
                                  "Left Side: Left side lane. Note that this lane is copied to the right side with inverted UVs.\n\n" +
                                  "Intersection: Connects roads within intersections. If none are set, the road lanes will be used.\n\n" +
                                  "Elevated: Only on elevated grounds.\n\n" +
                                  "Road End: Used when the road ends with no intersection. If none is set, a turnaround will be created.";
            laneType.tooltip = laneTypeTooltip;

            
            var material = new ObjectField("Material");
            material.name = nameof(material);
            material.objectType = typeof(Material);
            material.style.marginRight = 3f;
            material.tooltip = "Material for this lane.";
            material.style.flexGrow = 0.1f;

            
            var LanePositionWrapper = new VisualElement();
            LanePositionWrapper.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            var positionX = new Vector2Field("Width");
            positionX.name = nameof(SplineEdgeEditor.positionX);
            positionX.PGVector2ComponentLabel("Min", "Max");
            positionX.tooltip = "Position of this lane over the width of the road.";

            var uvX = new Vector2Field("UV X");
            uvX.name = nameof(SplineEdgeEditor.uvX);
            uvX.PGClampValue(0f, 1f);
            uvX.PGVector2ComponentLabel("Min", "Max");
            uvX.tooltip = "Relative x position of this lane on the UV texture map.";
            
            var height = new FloatField("Height");
            height.name = nameof(SplineEdgeEditor.height);
            height.PGClampValue();
            height.tooltip = "Height of this lane. Values above 0 will create additional geometry.";

            var closedEnds = new Toggle("Closed Ends");
            closedEnds.name = nameof(SplineEdgeEditor.closedEnds);
            closedEnds.tooltip = "Creates a rectangular mesh to close the start/end of the lanes.\n" +
                                 "Deactivating this setting can be useful, for example, to create basic tunnel functionality.";

            var invert = new Toggle("Upside Down");
            invert.name = nameof(SplineEdgeEditor.invert);
            invert.tooltip = "Inverts the normals and height of this lane so it is visible from the bottom.\n\n" +
                             "Especially useful for elevated lane types.";
            invert.PGToggleStyleDefault();
            

            item.Add(laneType);
            item.Add(material);
            item.Add(positionX);
            item.Add(uvX);
            item.Add(height);
            item.Add(closedEnds);
            item.Add(invert);
            return item;
        }

        private static void BindLaneItem(VisualElement item, SerializedProperty splineEdgeProperty, SplineEdgeEditor splineEdgeEditor)
        {
            var laneType = item.Q<EnumField>(nameof(SplineEdgeEditor.laneType));
            laneType.BindProperty(splineEdgeProperty.FindPropertyRelative(nameof(SplineEdgeEditor.laneType)));

            var material = item.Q<ObjectField>(nameof(SplineEdgeEditor.material));
            material.BindProperty(splineEdgeProperty.FindPropertyRelative(nameof(SplineEdgeEditor.material)));

            var positionX = item.Q<Vector2Field>(nameof(SplineEdgeEditor.positionX));
            positionX.BindProperty(splineEdgeProperty.FindPropertyRelative(nameof(SplineEdgeEditor.positionX)));
            positionX.PGClampValue();

            var uvX = item.Q<Vector2Field>(nameof(SplineEdgeEditor.uvX));
            uvX.BindProperty(splineEdgeProperty.FindPropertyRelative(nameof(SplineEdgeEditor.uvX)));
            
            var height = item.Q<FloatField>(nameof(SplineEdgeEditor.height));
            height.BindProperty(splineEdgeProperty.FindPropertyRelative(nameof(SplineEdgeEditor.height)));
            
            var closedEnds = item.Q<Toggle>(nameof(SplineEdgeEditor.closedEnds));
            closedEnds.BindProperty(splineEdgeProperty.FindPropertyRelative(nameof(SplineEdgeEditor.closedEnds)));
            closedEnds.PGDisplayStyleFlex(splineEdgeEditor.height > 0f);
            height.RegisterValueChangedCallback(evt =>
            {
                closedEnds.PGDisplayStyleFlex(splineEdgeEditor.height > 0f);
            });
            
            var invert = item.Q<Toggle>(nameof(SplineEdgeEditor.invert));
            invert.BindProperty(splineEdgeProperty.FindPropertyRelative(nameof(SplineEdgeEditor.invert)));
        }
    }
}
#endif