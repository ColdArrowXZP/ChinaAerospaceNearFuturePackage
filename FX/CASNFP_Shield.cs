using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ChinaAeroSpaceNearFuturePackage.FX
{
    public class CASNFP_Shield : PartModule
    {
        public static GameObject prefabObject;
        private ConfigNode thisNode;
        private GameObject shield;
        public override void OnAwake()
        {
            if (prefabObject == null)
            {
                thisNode = SettingLoader.CASNFP_GlobalSettings.GetNode("FX_Setting").GetNode("CASNFP_Shield");
                string prefabObjectName = thisNode.GetValue("prefabObjectName");
                prefabObject = SettingLoader.AppBundle.LoadAsset<GameObject>(prefabObjectName);
                prefabObject.SetActive(false);
                prefabObject.transform.localScale = new Vector3(0.1f,0.1f,0.1f);
                Debug.Log("加载了预制体");
            }
            base.OnAwake();
        }
        
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "力场护盾半径")]
        [UI_FloatRange(minValue = 1f, maxValue = 10f, stepIncrement = 1f, scene = UI_Scene.Editor)]
        public float partRadius = 1f;
        [KSPEvent(guiActive =true, active = true, guiName = "turn Shield")]
        public void OnShield() 
        {
            Debug.Log("protoVessel.height=" + vessel.protoVessel.height+ "vesselSize=" + vessel.vesselSize + "heightFromPartOffsetLocal" + vessel.heightFromPartOffsetLocal+ "heightFromSurface=" + vessel.heightFromSurface + "heightFromTerrain=" + vessel.heightFromTerrain + "HeightFromSurfaceHit=" + vessel.HeightFromSurfaceHit.distance);//火箭高度
            if (shield != null)
            {
                if (shield.activeSelf)
                {
                    shield.SetActive(false);
                }
                else
                {
                    shield.SetActive(true);
                }
            }
            else
            {
                if (prefabObject != null)
                {
                    
                    shield = Instantiate(prefabObject, part.vessel.GetTransform());
                }
            }
        }
        //float time = 1f;
        //private void Update()
        //{

        //    if (shield != null)
        //    {
        //        if (shield.transform.localScale.x <= partRadius)
        //        {
        //            shield.transform.localScale *= time;
        //            time += Time.deltaTime;
        //        }
        //    }
        //}
        //计算部件到火箭顶部距离
        private void CalPos()
        {
            
             float high = vessel.protoVessel.height;//火箭高度
             



            float moveDistance;
            Vector3 pos1 = part.transform.position;
            Vector3 pos2 = part.vessel.rootPart.transform.position;
            float distance = Vector3.Distance(pos1, pos2);
            if (Part.PartToVesselSpacePos(part.transform.position, part, part.vessel, PartSpaceMode.Current).x > 0)
            {
                moveDistance = distance + vessel.vesselSize.x / 2;
            }
            //计算部件位于火箭的位置，然后往火箭前端确定生成位置。
            Vector3 partVesselPosition = Part.PartToVesselSpacePos(part.transform.position, part, part.vessel, PartSpaceMode.Current);
            ;
        }
    }
}
