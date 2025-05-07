// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    internal static class ObjectUtility
    {
        public const string PrefixRoad = "Road_";
        public const string PrefixIntersection = "Intersection_";
        public const string PrefixUndo = "UndoObject_";
        public static GameObject CreateRoadObject(ShadowCastingMode shadowCastingMode, out MeshFilter meshFilter, out MeshRenderer meshRenderer)
        {
            var obj = new GameObject();
            obj.name = PrefixRoad + obj.GetInstanceID();
            meshFilter = obj.AddComponent<MeshFilter>();
            meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = shadowCastingMode;
            return obj;
        }
        
        public static GameObject CreateIntersectionObject(ShadowCastingMode shadowCastingMode, out MeshFilter meshFilter, out MeshRenderer meshRenderer)
        {
            var obj = new GameObject();
            obj.name = PrefixIntersection + obj.GetInstanceID();
            meshFilter = obj.AddComponent<MeshFilter>();
            meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = shadowCastingMode;
            return obj;
        }

        public static GameObject CreateLODObject(ShadowCastingMode shadowCastingMode, GameObject parent, int lod,
            out MeshFilter meshFilter, out MeshRenderer meshRenderer)
        {
            var obj = new GameObject();
            obj.name = parent.name + "_LOD" + lod;
            meshFilter = obj.AddComponent<MeshFilter>();
            meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = shadowCastingMode;
            return obj;
        }
        
        public static GameObject CreateUndoObject()
        {
            var obj = new GameObject();
            obj.name = PrefixUndo + obj.GetInstanceID();
            return obj;
        }
        
        public static void DestroyObject(Object obj)
        {
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }

        public static SplineContainer CreateTestObject(Spline spline, string name = "Test Spline")
        {
            var test = new GameObject(name);
            var cont = test.AddComponent<SplineContainer>();
            var testSpline = new Spline(spline);
            testSpline.SetTangentMode(TangentMode.Broken);
            cont.Spline = testSpline;
            return cont;
        }
    }
}
