// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using UnityEngine;
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    /// <summary>
    ///     Used for calculations before creating the actual objects.
    /// </summary>
    public class SceneObjectClass
    {
        public RoadDescr roadDescr;
        public Spline spline;
    }

    public class RoadObjectClass : SceneObjectClass
    {
        public Bounds splineBounds;

        public RoadObjectClass(RoadDescr roadDescr, Spline spline)
        {
            this.roadDescr = roadDescr;
            this.spline = spline;
            splineBounds = spline.GetBounds();
        }
    }

    public class EndObjectClass : SceneObjectClass
    {
        public EndObjectClass(RoadDescr roadDescr, Spline spline)
        {
            this.roadDescr = roadDescr;
            this.spline = spline;
        }
    }
    
    public class RampObjectClass : SceneObjectClass
    {
        public Overlap overlap01;
        public Overlap overlap02;
        
        public RampObjectClass(RoadDescr roadDescr, Spline spline, Overlap overlap01, Overlap overlap02)
        {
            this.roadDescr = roadDescr;
            this.spline = spline;
            this.overlap01 = overlap01;
            this.overlap02 = overlap02;
        }
    }
    
    
}