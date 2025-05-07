// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace PampelGames.RoadConstructor
{
    public abstract class ConstructionResult
    {
        /// <summary>
        ///     True if this result exists.
        ///     Note that a true value can still mean that the construction failed, if the <see cref="constructionFails" /> count is > 0.
        ///     The purpose of this field is mainly to avoid potential null checks for the classes below.
        /// </summary>
        public bool isValid;
        
        /// <summary>
        ///     If the list count is >0, construction will fail.
        /// </summary>
        public List<ConstructionFail> constructionFails = new();

        /// <summary>
        ///     Newly created <see cref="IntersectionObject" />s.
        /// </summary>
        public readonly List<IntersectionObject> intersectionObjects = new();
        
        /// <summary>
        ///     Newly created <see cref="RoadObject" />s which replaced existing ones.
        ///     This can happen for example if the length gets reduced to fit into an intersection.
        /// </summary>
        public readonly List<RoadObject> replacedRoadObjects = new();
    }
    
    /********************************************************************************************************************************/
    
    public class ConstructionResultRoad : ConstructionResult
    {
        /// <summary>
        ///     Geometric information about the new road.
        /// </summary>
        public ConstructionData roadData;

        /// <summary>
        ///     Newly created <see cref="RoadObject" /> in the scene.
        /// </summary>
        public RoadObject roadObject;

        /// <summary>
        ///     <see cref="Overlap" /> data of the first position.
        ///     Make sure the <see cref="Overlap.exists" /> before using it.
        /// </summary>
        public Overlap overlap01;

        /// <summary>
        ///     <see cref="Overlap" /> data of the second position.
        ///     Make sure the <see cref="Overlap.exists" /> before using it.
        /// </summary>
        public Overlap overlap02;

        public ConstructionResultRoad(bool isValid)
        {
            this.isValid = isValid;
        }
        public ConstructionResultRoad(ConstructionData roadData, RoadObject roadObject, Overlap overlap01, Overlap overlap02,
            List<ConstructionFail> constructionFails)
        {
            isValid = true;
            this.roadData = roadData;
            this.roadObject = roadObject;
            this.overlap01 = overlap01;
            this.overlap02 = overlap02;
            this.constructionFails = constructionFails;
        }
    }

    /********************************************************************************************************************************/

    public class ConstructionResultRoundabout : ConstructionResult
    {
        public ConstructionResultRoundabout(bool isValid)
        {
            this.isValid = isValid;
        }
    }
    
    /********************************************************************************************************************************/

    public class ConstructionResultRamp : ConstructionResult
    {
        public Overlap overlap;
        public ConstructionResultRamp(bool isValid)
        {
            this.isValid = isValid;
        }
    
        public ConstructionResultRamp(Overlap overlap) 
        {
            isValid = true;
            this.overlap = overlap;
        }
    }
    
    /********************************************************************************************************************************/

    public class ConstructionResultMoveIntersection : ConstructionResult
    {
        public ConstructionResultMoveIntersection(bool isValid)
        {
            this.isValid = isValid;
        }
    }
    
    /********************************************************************************************************************************/
    
    public class ConstructionFail
    {
        public readonly FailCause failCause;

        public ConstructionFail(FailCause failCause)
        {
            this.failCause = failCause;
        }
    }
}