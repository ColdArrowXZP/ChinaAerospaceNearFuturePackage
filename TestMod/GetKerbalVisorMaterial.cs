using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UniLinq;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage.TestMod
{
    [KSPAddon(KSPAddon.Startup.Flight,false)]
    public class GetKerbalVisorMaterial:MonoBehaviour
    {
        //GameObject obj;
        //ParticleSystem myPS;
        //private void Start()
        //{
        //    ConfigNode node = SettingLoader.CASNFP_GlobalSettings;
        //    if (obj == null)
        //    {
        //        obj = SettingLoader.UIBundle.LoadAsset<GameObject>("testFX");
        //        Debug.Log("预制体加载完成" + obj.name);
        //    }
        //    GameEvents.onCrewOnEva.Add(onFlightReady);
        //    Debug.Log("设置监听完成");
        //}

        //private void onFlightReady(GameEvents.FromToAction<Part, Part> data)
        //{
        //    Part part = data.from;
        //    Vessel vessel = part.vessel;
        //    Vector3 vector = vessel.transform.position;
        //    Debug.Log("火箭坐标已经找到" + vessel.name + vector);
        //    GameObject gameObject = Instantiate<GameObject>(obj, vector, Quaternion.identity);
        //    Debug.Log("预制体已经实例化");
        //    myPS = gameObject.GetComponentInChildren<ParticleSystem>();

        //}
        //private void Update()
        //{
        //    if (myPS.isPlaying)
        //    {
        //        myPS.gameObject.SetActive(false);
        //    }
        //    else myPS.gameObject.SetActive(true);
        //}
        //private void OnDestroy()
        //{
        //    GameEvents.onCrewOnEva.Remove(onFlightReady);
        //    Debug.Log("移除监听完成");
        //}
    }
}
