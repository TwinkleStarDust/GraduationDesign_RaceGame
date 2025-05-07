// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace PampelGames.RoadConstructor
{
    public class RoadObject : SceneObject
    {
        public float length;
        
        [HideInInspector] public bool snapPositionSet; 
        [HideInInspector] public Vector3 snapPosition;
        [HideInInspector] public bool previewRoad;
    }
}