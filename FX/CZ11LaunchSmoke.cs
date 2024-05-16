using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage.FX
{
    [KSPAddon(KSPAddon.Startup.Flight,false)]
    public class CZ11LaunchSmoke : MonoBehaviour
    {
        Vessel currentVessel;
        private event Action<bool> EmitAction;
        private event Action<bool,ModuleEngines> EngineAction;
        GameObject objFX;
        ParticleSystem ps;
        float hight;
        double vesselOriginRadarAltitude;
        static ConfigNode thisNode;
        public void Start() 
        {
            GameEvents.onFlightReady.Add(OnFlightReady);
        }
        private void Emit(bool b) 
        {
            if (b)
            {
                ps.Play(true);
            }
            else
            {
                ps.Stop(true);
                Destroy(objFX,3f);
            }
        }
        private void OnFlightReady()
        {
            currentVessel = FlightGlobals.ActiveVessel;
            //这里判断是否有发射筒，如果没有就销毁
            //if(...)else{Destroy(this);return;}
            if (currentVessel != null) 
            {
                string a = currentVessel.GetDisplayName();
                if (a!="长征11号") { Debug.Log("当前火箭为"+a+"非长征11号"); return; }
            }
            if (currentVessel == null)
            {
                Destroy(this);
                return;
            }
            EngineAction = new Action<bool,ModuleEngines>(SetEngine);
            EmitAction = new Action<bool>(Emit);
            vesselOriginRadarAltitude = currentVessel.radarAltitude;
            Debug.Log(currentVessel.vesselSize);
            hight = currentVessel.vesselSize.x;
            thisNode = SettingLoader.CASNFP_GlobalSettings.GetNode("FX_Setting").GetNode("casnfp_FX_CZ11LaunchSmoke");
            string thisFXName = thisNode.GetValue("prefabFXName");
            GameObject objFXPrefeb = SettingLoader.AppBundle.LoadAsset<GameObject>(thisFXName);
            if (objFXPrefeb!= null)
            {
                objFXPrefeb.transform.localPosition = new Vector3(0,-hight-float.Parse(thisNode.GetValue("offsetDistance")), 0);
                objFX = Instantiate(objFXPrefeb, currentVessel.GetTransform());
                
                //moveTarget = currentVessel.GetTransform().up;
                //moveTarget *= float.Parse(thisNode.GetValue("targetHigh"));
                //objFX.transform.SetParent(null,true);
                ps = objFX.GetComponent<ParticleSystem>();
            }
            if (ps!= null && ps.isPlaying) ps.Stop(true);

        }

        private void SetEngine(bool b, ModuleEngines engines)
        {
            if (b)
            {
                engines.enabled = true;
            }
            else
            {
                engines.enabled = false;
            }
        }
        int flag = -1;
        bool flag2 = true;
        float time = 0;
        private List<ModuleEngines> GetVesselActiveEngines(Vessel vessel)
        {
            List < ModuleEngines > engines = new List <ModuleEngines >();
            foreach (var item in vessel.GetActiveParts())
            {
                if (item.FindModuleImplementing<ModuleEngines>() != null)
                {
                    engines.Add(item.FindModuleImplementing<ModuleEngines>());
                }
            }
            return engines;
        }
        private void Update() 
        {
            if (objFX == null) return;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                flag = 0;
                List<ModuleEngines> engines = GetVesselActiveEngines(currentVessel);
                if (engines.Count != 0) 
                {
                    flag = 1;
                    foreach (var item in engines)
                    {
                        EngineAction.Invoke(false,item);
                    }
                } 
            }
            if (flag == 0)
            { Debug.Log("[CZ11发射筒]：你把啥塞进来了，火箭点火引擎设置不正确，取消弹射");flag = -1; }
            if (flag == 1) 
            {
                if (time < 0.1f)
                {
                    List<ModuleEngines> engines = GetVesselActiveEngines(currentVessel);
                    float partForce = float.Parse(thisNode.GetValue("separatingForce"))/engines.Count;
                    foreach (var item in engines)
                    {
                        List<Transform> engineTrustForm = new List<Transform>();
                        foreach (var t in item.thrustTransforms) 
                        {
                            engineTrustForm.Add(t);
                        }
                        item.part.AddForceAtPosition(currentVessel.GetTransform().up*partForce, CalculateAveragePosition(engineTrustForm));
                        item.part.AddForce(currentVessel.GetTransform().up * partForce);
                    }  
                }
                if (time >= 0.2f && flag2) 
                {
                    EmitAction.Invoke(true);
                    flag2 = false;  
                }
                if (time >= 3f) 
                {
                    objFX.gameObject.transform.SetParent(null,true);
                }
                if (!flag2 && currentVessel.srfSpeed<10f)
                {
                    EmitAction.Invoke(false);
                    List<ModuleEngines> engines = GetVesselActiveEngines(currentVessel);
                    foreach (var item in engines)
                    {
                        EngineAction.Invoke(true, item);
                    }
                }
                time += Time.deltaTime;
            }

        }

        private void OnDestroy() 
        {
            if (objFX != null) Destroy(objFX);
            GameEvents.onFlightReady.Remove(OnFlightReady);
        }
        private Vector3 CalculateAveragePosition(List<Transform> positions)
        {
            Vector3 averagePosition = Vector3.zero;
            for (int i = 0; i < positions.Count; i++)
            {
                averagePosition += positions[i].position;
            }
            averagePosition /= positions.Count;
            return averagePosition;
        }
    }
}
