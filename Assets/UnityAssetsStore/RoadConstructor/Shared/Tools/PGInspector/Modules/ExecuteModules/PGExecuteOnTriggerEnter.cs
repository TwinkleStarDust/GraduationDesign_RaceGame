﻿// ----------------------------------------------------
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System;
using System.Collections.Generic;
using PampelGames.Shared.Utility;
using UnityEngine;

namespace PampelGames.Shared.Tools.PGInspector
{
    public class PGExecuteOnTriggerEnter : PGExecuteClassBase
    {

        public override string ModuleName()
        {
            return "On Trigger Enter";
        }
        
        public override string ModuleInfo()
        {
            return "Starts when an attached trigger collider collides with another.";
        }

        [Tooltip("Layer Filter: Only execute when one of the specified Layers matches.")]
        public bool useLayerFilter;
        
        public LayerMask matchingLayers;

        [Tooltip("Tag Filter: Only execute when one of the specified Tags matches.")]
        public bool useTagFilter;

        public List<string> matchingTags = new();
        
        public override void ComponentOnTriggerEnter(MonoBehaviour baseComponent, Action ExecuteAction, Collider other)
        {
            base.ComponentOnTriggerEnter(baseComponent, ExecuteAction, other);
            
            if (useLayerFilter && matchingLayers != (matchingLayers | (1 << other.transform.gameObject.layer)) && !useTagFilter) return;
            if (useTagFilter && !matchingTags.Contains(other.gameObject.tag)) return;
            ExecuteAction();
        }
        
    }
}