using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage.FX
{
    [KSPAddon(KSPAddon.Startup.Flight,false)]
    public class Casnfp_CZ11LaunchSmoke : MonoBehaviour
    {
        Vessel currentVessel;
        private event Action<bool> EmitAction;
        GameObject objFX;
        ParticleSystem ps;
        float hight;
        double vesselOriginRadarAltitude;
        public void Start() 
        {
            GameEvents.onFlightReady.Add(OnFlightReady);
        }
        private void Emit(bool b) 
        {
            if (b)
            {
                objFX.transform.SetParent(currentVessel.GetTransform(), true);
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
            EmitAction = new Action<bool>(Emit);
            currentVessel = FlightGlobals.ActiveVessel;
            //设置对当前火箭状态的判断（比如是否含有CZ-11发射筒）
            if (currentVessel == null) 
            {Destroy(this);return;
            } 
            vesselOriginRadarAltitude = currentVessel.radarAltitude;
            Debug.Log(currentVessel.vesselSize);
            hight = currentVessel.vesselSize.x;
            string thisFXName = SettingLoader.CASNFP_GlobalSettings.GetNode("FX_Setting").GetValue("prefabFXName");
            GameObject objFXPrefeb = SettingLoader.AppBundle.LoadAsset<GameObject>(thisFXName);
            if (objFXPrefeb!= null)
            {
                objFX = Instantiate(objFXPrefeb, currentVessel.GetTransform());
                objFX.transform.SetParent(null);
                ps = objFX.GetComponent<ParticleSystem>();
            }
            if (ps!= null && ps.isPlaying) ps.Stop(true); 
            
        }
        private void Update() 
        {
            if (objFX == null) return;
            if ((currentVessel.radarAltitude - vesselOriginRadarAltitude) > hight && (currentVessel.radarAltitude - vesselOriginRadarAltitude <= 200f)) EmitAction.Invoke(true);
            if ((currentVessel.radarAltitude - vesselOriginRadarAltitude) > 200f) EmitAction.Invoke(false);

        }
        private void OnDestroy() 
        {
            if (objFX != null) Destroy(objFX);
            GameEvents.onFlightReady.Remove(OnFlightReady);
        }
    }
}
