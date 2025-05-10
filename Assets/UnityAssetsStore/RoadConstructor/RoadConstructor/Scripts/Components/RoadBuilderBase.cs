// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace PampelGames.RoadConstructor
{
    /// <summary>
    ///     Base class for road builder scripts.
    ///     You may modify it to your liking, or you can create your own base class from scratch.
    /// </summary>
    public abstract class RoadBuilderBase : MonoBehaviour
    {
        public RoadConstructor roadConstructor;
        public GameObject pointerPrefab;
        public GameObject pointerDemolishPrefab;

        public BuilderRoadType builderRoadType = BuilderRoadType.Road;
        
        [Space(10)] public float roundAboutRadius = 10f;
        
        [Space(10)] public KeyCode increaseHeight = KeyCode.E;
        public KeyCode decreaseHeight = KeyCode.Q;
        public KeyCode increaseRadius = KeyCode.T;
        public KeyCode decreaseRadius = KeyCode.R;
        public float deltaSpeed = 5f;
        
        [Space(10)] public KeyCode fixTangent1 = KeyCode.LeftShift;
        public KeyCode fixTangent2 = KeyCode.LeftControl;
        public KeyCode detachRoad = KeyCode.Escape;

        [Space(10)] [Tooltip("Registers existing objects in the scene for construction.")]
        public bool registerSceneObjects = true;
        
        [Tooltip("When a road has been placed, a new road connects to it immediately.")]
        public bool continuous = true;


        private GameObject pointer;
        private GameObject pointerDemolish;

        protected string activeRoad;
        [HideInInspector] public string activeMenu;
        protected float deltaHeight;
        [HideInInspector] public Vector3 lastTangent01;
        [HideInInspector] public Vector3 lastTangent02;

        private bool position01Set;
        private float3 position01;
        private float3 position02;

        [HideInInspector] public bool demolishActive;
        
        [HideInInspector] public bool moveActive;
        [HideInInspector] public MoveStatus moveStatus;
        [HideInInspector] public Vector3 movePosition;

        /********************************************************************************************************************************/

        public void InitializePointer()
        {
            CreatePointer();
            SetPointerActive(false);
            SetPointerDemolishActive(false);
        }

        public void DestroyPointers()
        {
            if (Application.isPlaying) Destroy(pointer);
            else DestroyImmediate(pointer);
            if (Application.isPlaying) Destroy(pointerDemolish);
            else DestroyImmediate(pointerDemolish);
        }

        /********************************************************************************************************************************/

        private void CreatePointer()
        {
            if (pointer == null && pointerPrefab != null)
            {
                pointer = Instantiate(pointerPrefab, roadConstructor.transform, true);
                pointer.name = "Pointer";
            }
            if (pointerDemolish == null && pointerDemolishPrefab != null)
            {
                pointerDemolish = Instantiate(pointerDemolishPrefab, roadConstructor.transform, true);
                pointerDemolish.name = "PointerDemolish";
            }
        }

        public Vector3 SnapPointer(Vector3 position, Vector3 direction)
        {
            SetPointerActive(true);

            if(string.IsNullOrEmpty(activeRoad))
            {
                var radius = GetDefaultRadius();
                pointer.transform.position = roadConstructor.SnapPosition(radius, position, out var overlap1);
                pointer.transform.localScale = new Vector3(radius, radius, radius);
            }
            else
                pointer.transform.position = roadConstructor.SnapPosition(activeRoad, position, out var overlap2);

            var up = Vector3.up;
            var right = Vector3.Cross(up, Vector3.forward);
            var forward = Vector3.Cross(right, up);
            var rotation = quaternion.LookRotationSafe(forward, up);

            pointer.transform.rotation = rotation;

            return pointer.transform.position;
        }
        
        public Vector3 SnapPointerDemolish(float radius, Vector3 position, Vector3 direction)
        {
            SetPointerActive(true);
            
            pointerDemolish.transform.position = roadConstructor.SnapPosition(radius, position, out var overlap);
            
            var up = direction.normalized;
            var right = Vector3.Cross(up, Vector3.forward);
            var forward = Vector3.Cross(right, up);
            var rotation = quaternion.LookRotationSafe(forward, up);

            pointerDemolish.transform.rotation = rotation;
            
            return pointerDemolish.transform.position;
        }

        public void SetPointerActive(bool active)
        {
            if (pointer == null) return;
            if (!moveActive && active && activeRoad == string.Empty) return;
            pointer.SetActive(active);
        }
        
        public void SetPointerDemolishActive(bool active)
        {
            if (pointerDemolish == null) return;
            pointerDemolish.SetActive(active);
        }

        public float GetDefaultRadius()
        {
            var radius = math.abs(roadConstructor.componentSettings.heightRange.y) + 1f;
            return radius;
        }
        
        /********************************************************************************************************************************/

        public virtual void ActivateRoad(string roadName)
        {
            ResetValues();
            roadConstructor.ClearAllDisplayObjects();

            if (activeRoad == roadName)
            {
                SetActiveRoadData(string.Empty);
                return;
            }

            if (!roadConstructor.TryGetRoadDescr(roadName, out var roadDescr)) return;
            SetActiveRoadData(roadName);
            pointer.transform.localScale = Vector3.one * roadDescr.width;
            pointerDemolish.transform.localScale = Vector3.one * roadDescr.width;
        }

        public void DeactivateRoad()
        {
            SetPointerActive(false);
            activeRoad = string.Empty;
        }

        private void SetActiveRoadData(string roadName)
        {
            activeRoad = roadName;
        }

        public string GetActiveRoad()
        {
            return activeRoad;
        }

        /********************************************************************************************************************************/

        public bool IsDemolishActive()
        {
            return demolishActive;
        }

        public void SetDemolishActive(bool active)
        {
            DeactivateRoad();
            demolishActive = active;
        }
        
        /********************************************************************************************************************************/

        public bool IsMoveActive()
        {
            return moveActive;
        }

        public void SetMoveActive(bool active)
        {
            DeactivateRoad();
            moveActive = active;
            moveStatus = MoveStatus.Select;
        }

        /********************************************************************************************************************************/

        public float GetDeltaHeight()
        {
            return deltaHeight;
        }

        public void SetDeltaHeight(float value)
        {
            deltaHeight = value;
        }
        
        public void SetRadius(float value)
        {
            roundAboutRadius = value;
        }
        
        public float GetRadius()
        {
            return roundAboutRadius;
        }

        /********************************************************************************************************************************/

        public ConstructionResultRoad DisplayRoad(Vector3 position, RoadSettings roadSettings)
        {
            var heightDeltaPosition = new float3(0f, deltaHeight, 0f);
            
            if (!position01Set)
            {
                position01 = (float3) position + heightDeltaPosition;
            }
            else
            {
                var pos2 = (float3) position + heightDeltaPosition;
                var result = roadConstructor.DisplayRoad(activeRoad, position01, pos2, roadSettings);
                return result;
            }

            return new ConstructionResultRoad(false);
        }

        public ConstructionResultRoad ConstructRoad(Vector3 position, RoadSettings roadSettings)
        {
            var heightDeltaPosition = new float3(0f, deltaHeight, 0f);

            if (!position01Set)
            {
                position01 = (float3) position + heightDeltaPosition;
                position01Set = true;
            }
            else
            {
                var pos2 = (float3) position + heightDeltaPosition;
                var result = roadConstructor.ConstructRoad(activeRoad, position01, pos2, roadSettings);
                if (result.constructionFails.Count == 0)
                {
                    if (!continuous) position01Set = false;
                    position01 = pos2;
                }

                return result;
            }

            return new ConstructionResultRoad(false);
        }
        
        public ConstructionResultRoundabout DisplayRoundabout(Vector3 position)
        {
            var heightDeltaPosition = new float3(0f, deltaHeight, 0f);
            
            position = (float3) position + heightDeltaPosition;
            var result = roadConstructor.DisplayRoundabout(activeRoad, position, roundAboutRadius);
            position01Set = false;
            
            return result;
        }
        
        public ConstructionResultRoundabout ConstructRoundabout(Vector3 position)
        {
            var heightDeltaPosition = new float3(0f, deltaHeight, 0f);
            
            position = (float3) position + heightDeltaPosition;
            var result = roadConstructor.ConstructRoundabout(activeRoad, position, roundAboutRadius);
            position01Set = false;

            return result;
        }
        
        public ConstructionResultRamp DisplayRamp(Vector3 position, RoadSettings roadSettings)
        {
            var heightDeltaPosition = new float3(0f, deltaHeight, 0f);
            
            if (!position01Set)
            {
                position01 = (float3) position + heightDeltaPosition;
            }
            else
            {
                var pos2 = (float3) position + heightDeltaPosition;
                var result = roadConstructor.DisplayRamp(activeRoad, position01, pos2, roadSettings);
                return result;
            }

            return new ConstructionResultRamp(false);
        }

        public ConstructionResultRamp ConstructRamp(Vector3 position, RoadSettings roadSettings)
        {
            var heightDeltaPosition = new float3(0f, deltaHeight, 0f);

            if (!position01Set)
            {
                position01 = (float3) position + heightDeltaPosition;
                position01Set = true;
            }
            else
            {
                var pos2 = (float3) position + heightDeltaPosition;
                var result = roadConstructor.ConstructRamp(activeRoad, position01, pos2, roadSettings);
                if (result.constructionFails.Count == 0)
                {
                    if (!continuous) position01Set = false;
                    position01 = pos2;
                }

                return result;
            }

            return new ConstructionResultRamp(false);
        }

        public void ResetValues()
        {
            deltaHeight = 0f;
            lastTangent01 = Vector3.zero;
            lastTangent02 = Vector3.zero;
            position01Set = false;
        }

        public void UndoLastConstruction()
        {
            roadConstructor.UndoLastConstruction();
        }

        /********************************************************************************************************************************/

        public string BuildingParameterText()
        {
            var infoText = "Elevation: " + deltaHeight;
            return infoText;
        }

        // For convenience, using reflection to retrieve all available road data fields.
        public string ConstructionDataText(ConstructionResult result)
        {
             if (result is ConstructionResultRoad resultRoad)
            {
                if (!resultRoad.isValid) return string.Empty;
                
                var roadData = resultRoad.roadData;
                var type = roadData.GetType();
                var fields = type.GetFields();
                var infoText = new StringBuilder();

                foreach (var field in fields)
                {
                    var fieldName = field.Name;
                    var fieldValue = field.GetValue(roadData);
                    if (field.FieldType == typeof(float))
                        fieldValue = Math.Round((float) fieldValue, 2); // convert to float and round to 2 decimal places
                    infoText.AppendLine(fieldName + ": " + fieldValue);
                }

                return infoText.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        public string ConstructionFailText(ConstructionResult result)
        {
            var failText = string.Empty;
            var fails = result.constructionFails;
            for (var i = 0; i < fails.Count; i++)
            {
                failText += "Fail: " + fails[i].failCause;
                if (i != fails.Count - 1) failText += "\n";
            }

            return failText;
        }
    }
    
}