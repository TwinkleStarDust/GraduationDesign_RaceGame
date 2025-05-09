﻿// ----------------------------------------------------
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System;
using System.Collections;
using UnityEngine;

namespace PampelGames.Shared.Tools.PGInspector
{
    public class PGExecuteGetButtonUp : PGExecuteClassBase
    {

        public override string ModuleName()
        {
            return "Get Button Up";
        }
        public override string ModuleInfo()
        {
            return "Starts when Input.GetButtonUp() recognizes the specified button.\n" +
                   "Edit > ProjectSettings > InputManager";
        }

        [Tooltip("Identifier of the Button the user needs to release.\n" +
                 "\n" + "Edit > ProjectSettings > InputManager")]
        public string buttonName = "Fire1";

        private Coroutine checkButtonCoroutine;
        
        public override void ComponentOnEnable(MonoBehaviour baseComponent, Action ExecuteAction)
        {
            base.ComponentOnEnable(baseComponent, ExecuteAction);
            checkButtonCoroutine = baseComponent.StartCoroutine(_GetButtonUpStartCheck(ExecuteAction));
        }
        
        private IEnumerator _GetButtonUpStartCheck(Action ExecuteAction)
        {
            for (;;)
            {
                if (isPaused) yield return null;
                if (Input.GetButtonUp(buttonName))
                    ExecuteAction();
                yield return null;
            }
        }
    }
}