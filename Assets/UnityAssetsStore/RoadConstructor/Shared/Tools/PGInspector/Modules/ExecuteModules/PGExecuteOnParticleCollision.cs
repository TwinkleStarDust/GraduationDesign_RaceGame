﻿// ----------------------------------------------------
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System;
using UnityEngine;

namespace PampelGames.Shared.Tools.PGInspector
{
    public class PGExecuteOnParticleCollision : PGExecuteClassBase
    {

        public override string ModuleName()
        {
            return "On Particle Collision";
        }
        
        public override string ModuleInfo()
        {
            return "Starts when an attached collider gets hit by a particle.";
        }

        public override void ComponentOnParticleCollision(MonoBehaviour baseComponent, Action ExecuteAction)
        {
            base.ComponentOnParticleCollision(baseComponent, ExecuteAction);
            ExecuteAction();
        }
        
    }
}