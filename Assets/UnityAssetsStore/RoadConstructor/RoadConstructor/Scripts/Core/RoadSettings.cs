// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using UnityEngine;

namespace PampelGames.RoadConstructor
{
    /// <summary>
    ///     Settings that can be dynamically applied using the <see cref="RoadConstructor"/> construction methods.
    /// </summary>
    public class RoadSettings
    {
        /// <summary>
        ///     Fixes the outgoing tangent of position01, forcing curvature.
        /// </summary>
        public bool setTangent01;

        /// <summary>
        ///     If <see cref="setTangent01" /> is true, tangent to use for position01.
        /// </summary>
        public Vector3 tangent01;

        /// <summary>
        ///     Fixes the outgoing tangent of position02, forcing curvature.
        /// </summary>
        public bool setTangent02;

        /// <summary>
        ///     If <see cref="setTangent02" /> is true, tangent to use for position02.
        /// </summary>
        public Vector3 tangent02;
    }
}