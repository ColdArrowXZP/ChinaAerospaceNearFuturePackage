using Expansions.Serenity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage.Parts
{
    public class CZ2F_EscapeTower :PartModule
    {
        [KSPField]
        public string oneStepEngineID = "Esc";
        [KSPField]
        public string posEngineID = "pos";

        [KSPField(guiFormat = "F1", isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "逃逸塔一阶段高度")]
        [UI_FloatRange(minValue = 1000, maxValue = 5000f,stepIncrement =500f, scene = UI_Scene.All)]
        public float escOneStepHight = 2500f;

        [KSPField(guiFormat ="F1",isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "逃逸塔一阶段时间")]
        [UI_FloatRange( minValue = 1, maxValue = 20f, stepIncrement = 2f, scene = UI_Scene.All)]
        public float escOneStepTime = 10f;

        private List<ModuleEnginesFX> enginesFXes;

        private event Action<bool,ModuleEnginesFX> engineAction;

        private static ModuleEnginesFX stepOneEngine;
        private static ModuleEnginesFX posEngine;
        private static ModuleEnginesFX stepTwoEngine;
        
        bool isEngineSetted = false;
       
        public override void OnAwake()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT)
            {
                return;
                
            }
            GameEvents.onFlightReady.Add(OnFlightReady);
        }
        
        private void OnFlightReady()
        {
            enginesFXes = part.FindModulesImplementing<ModuleEnginesFX>();
            engineAction = new Action<bool, ModuleEnginesFX>(EngineAction);
            Debug.Log(posEngineID);
            foreach (var engine in enginesFXes) 
            {
                if (engine.engineID == oneStepEngineID)
                {
                    stepOneEngine = engine;
                }
                else 
                {
                    
                    if (engine.engineID == posEngineID)
                    {
                        posEngine = engine;
                        Debug.Log("设置调姿发动机");
                    }
                    else 
                    {
                        stepTwoEngine = engine;
                        Debug.Log("设置二阶发动机"+stepTwoEngine.engineID);
                    }
                    engineAction.Invoke(false, engine);
                }
            }
            isEngineSetted = true;
        }

        private void EngineAction(bool isWork, ModuleEnginesFX fX)
        {
            if (fX == null) { Debug.Log("发动机不存在"); return; }
            if (isWork) 
            {
                fX.enabled = true;
                fX.Activate(); 
            }
            else
            {
                if (fX.getIgnitionState)
                {
                    fX.Shutdown();
                }
                fX.enabled = false; 
            }
        }

        float time = 0;
        bool isOneStepStart = false;
        bool isOneStepEnd = false;
        bool isPosStepStart = false;
        bool isPosStepEnd = false;
        bool isTwoStepStart = false;
        bool isEsc = false;
        public override void OnUpdate()
        {
            if (!isEngineSetted) {return; }
            if (stepOneEngine.getIgnitionState && !isOneStepStart)
            {
                isOneStepStart = true;
            }
            if (isOneStepStart && ! isOneStepEnd)
            {
                Debug.Log("一阶段开始");
                if (time >= 1f && !isPosStepStart) 
                {
                    engineAction.Invoke(true,posEngine);
                    isPosStepStart = true;
                    Debug.Log("调姿发动机启动");
                }
                if (time >= 3f && isPosStepStart && !isPosStepEnd) 
                {
                    engineAction.Invoke(false, posEngine);
                    isPosStepEnd=true;
                    Debug.Log("调姿结束");
                }
                if ((time >= escOneStepTime || vessel.RevealAltitude() >= escOneStepHight)) 
                {
                    engineAction.Invoke(false,stepOneEngine);
                    isOneStepEnd = true;
                    Debug.Log("一阶段结束");
                    time = 0f;
                }
                time += Time.deltaTime;
            }
            if (isOneStepEnd )
            {
                Debug.Log("二阶段开始");
                if (time >= 5f && !isTwoStepStart) 
                { 
                    engineAction.Invoke(true, stepTwoEngine);
                    isTwoStepStart = true;
                    Debug.Log("二阶段点火");
                }
                if (time>= 10f && isTwoStepStart && !isEsc) 
                {
                    engineAction.Invoke(false, stepTwoEngine);
                    isEsc = true;
                    this.enabled = false;
                    Debug.Log("二阶段结束");
                }
                time += Time.deltaTime;
            }
        }
        private void OnDestroy() 
        {
            GameEvents.onFlightReady.Remove(OnFlightReady);
        }
    }
}
