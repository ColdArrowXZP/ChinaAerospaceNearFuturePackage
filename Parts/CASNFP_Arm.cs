using System;
using UnityEngine;
using UnityEngine.UI;
using Expansions;
using Expansions.Serenity;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
namespace ChinaAeroSpaceNearFuturePackage.Parts
{
    public class CASNFP_Arm : ModuleRoboticServoHinge
    {
        
        private Button button;
        bool isUIDisplay = false;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "CASNFP机械臂")]
        internal string title = "V1.0.0.0";
        [KSPEvent(guiName = "激活控制面板", active = true, guiActive = true, guiActiveEditor = true)]
        public void ArmContolUI() 
        {
            if (isUIDisplay == false)
            {
                Arm_UI.Instance.CreatUI();
                Debug.Log("生成UI窗口");
                isUIDisplay = true;
                GameObject @object = Arm_UI.arm_Control_window.GetChild("ControlBtn01").GetChild("L");
                button = @object.GetComponent<Button>();
                button.onClick.AddListener(delegate { this.setCtrUI(part); });
            }
            else{ Debug.Log("已经生成UI窗口，不再生成重复窗口"); }
            
        }
        
        private void setCtrUI(Part part) 
        {
            Debug.Log("启动委托事件");
        }

        [KSPEvent(guiName = "关闭控制面板", active = true, guiActive = true, guiActiveEditor = true)]
        public void CloseUI() 
        {
            if (isUIDisplay)
            {
                Arm_UI.Instance.CloseUI();
                isUIDisplay = false;
                Debug.Log("物体被删除");
            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (isUIDisplay)
            {
                Arm_UI.Instance.CloseUI();
                isUIDisplay = false;
                Debug.Log("物体被删除");
            }
        }
        protected override void OnJointInit(bool goodSetup)
        {
            base.OnJointInit(goodSetup);

            SoftJointLimit softJoint = servoJoint.linearLimit;
            softJoint.limit = 0f;
            softJoint.contactDistance = 0f;
            softJoint.bounciness = 0f;
            servoJoint.linearLimit = softJoint;
            Debug.Log("约束弹簧偏转距离为0" + servoJoint.linearLimit);
            Vector3 anchor = servoJoint.anchor;
            if (anchor != Vector3.zero)
            {
                anchor = Vector3.zero;
                servoJoint.anchor = anchor;
                Debug.Log("重新归位中心点" + servoJoint.anchor);
                
            }
            
        }

        
        //public override void OnStartFinished(StartState state)
        //{

        //    List<ConfigurableJoint> springJoints = part.FindModelComponents<ConfigurableJoint>();
        //    if (springJoints.Count > 0)
        //    {
        //        Debug.Log("找到弹簧" + springJoints.Count + "个，分别为：");
        //        foreach (ConfigurableJoint item in springJoints)
        //        {
        //            Debug.Log(item.name);
        //            DestroyImmediate(item);
        //            Debug.Log("删除了ConfigurableJoint组件");
        //        }
        //    }
        //    else { Debug.Log("没有找到弹簧"); }
        //}
    }
}
    

