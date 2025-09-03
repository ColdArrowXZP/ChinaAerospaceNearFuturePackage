using ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm;
using Expansions. Serenity;
using KSP. UI;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace ChinaAeroSpaceNearFuturePackage.UI
{
    [KSPAddon (KSPAddon. Startup. Flight,false)]
    public class CASNFP_UI : AppLauncherBtn
    {
        private CASNFP_UI (){}
        private static CASNFP_UI instance;
        public static CASNFP_UI Instance { get => instance; }
        private CASNFP_SetRocAutoCtrl _armAutoCtrl;
        public CASNFP_SetRocAutoCtrl ArmAutoCtrl
        {
            get { return _armAutoCtrl; }
        }
        protected override void Awake ()
        {
            //设置单例
            if ( instance == null )
            {
                instance = this;
            }
            //在飞行界面生成启动按钮。
            base. Awake ();
            
            
        }
        public Rect rect = new Rect(0.5f, 0.5f, 300f, 200f);
        public MultiOptionDialog multi;
        PopupDialog popupDialog;
        public List<ArmPartJointInfo>[] CASNFP_RoboticArmPart;
        //[KSPEvent(guiActive = true,guiName ="",active =true)]
        protected override void OnTrue()
        {
            DialogGUIBox dialog = new DialogGUIBox("版本号：V"+CASNFP_Globals.CASNFP_VERSION+"\n"+"欢迎使用中国航天包",30f,30f);
            DialogGUIButton gUIButton1 = new DialogGUIButton ("启动机械臂自动控制程序", StartArmAutoCtrl, false);
            DialogGUIButton gUIButton2 = new DialogGUIButton("关闭CASNFP控制面板", OnFalse, false);
            DialogGUIBase[] a = { dialog, gUIButton1,gUIButton2 };
            multi = new MultiOptionDialog("CASNFP_ControlPanel", "","中国航天包控制面板", HighLogic.UISkin, rect,a);
            
            popupDialog = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f),new Vector2(0.5f, 0.5f),multi,true,HighLogic.UISkin,false, "CASNFP_UI");
        }
        protected virtual void StartArmAutoCtrl()
        {
            OnFalse ();
            if ( CASNFP_RoboticArmPart == null )
            {
                SetArmJointInfos ();
                StartAutoCtrl ();
            }
            else
                StartAutoCtrl ();
            if ( _armAutoCtrl == null )
            {
                Debug. LogError ("错误："+"机械臂识别过程中发生错误，自动控制程序终止");
            }

        }
        protected virtual void SetArmJointInfos ()
        {
            Vessel vessel = FlightGlobals. ActiveVessel?.GetVessel ();
            if ( vessel == null )
            {
                Debug. Log ("错误:" + "当前不存在处于控制中的载具");
                return;
            }
            //获取当前载具上的所有机械臂组件，将index为连续的机械臂组件放入机械臂数组中，如果中间有中断则划分为新的机械臂数组；检查同一机械臂数组是否符合至少一个底座、一个连接臂和一个工作臂的条件，符合条件则添加到机械臂列表中，不符合则忽略该机械臂数组并给出提示信息。
            List<ArmPartJointInfo> armPartJointInfos = new List<ArmPartJointInfo> ();
            foreach ( Part part in vessel. Parts )
            {
                ModuleCASNFP_RoboticArmPart item = part. FindModuleImplementing<ModuleCASNFP_RoboticArmPart> ();
                if ( item != null )
                {
                    item. ArmPartJointInfo. vessel = vessel;
                    item. ArmPartJointInfo. part = part;
                    item. ArmPartJointInfo. partIndexInVessel = vessel. Parts. IndexOf (part);
                    item. ArmPartJointInfo. servoHinge = part. FindModuleImplementing<ModuleRoboticServoHinge> ();
                    item. ArmPartJointInfo. jointTransform = part. gameObject. GetChild (item.jointName). transform;
                    item. ArmPartJointInfo. maxLimit = item. ArmPartJointInfo. servoHinge. softMinMaxAngles. y;
                    item. ArmPartJointInfo. minLimit = item. ArmPartJointInfo. servoHinge. softMinMaxAngles. x;
                    item. ArmPartJointInfo. rotationSpeed = item. ArmPartJointInfo. servoHinge. CurrentVelocityLimit;
                    item. ArmPartJointInfo. rotationAxis = item. ArmPartJointInfo. servoHinge. GetMainAxis ();
                    item. ArmPartJointInfo. currentAngle = item. ArmPartJointInfo. servoHinge. currentAngle;
                    item. ArmPartJointInfo. instanceAngle = item. ArmPartJointInfo. servoHinge.currentAngle;
                    item. ArmPartJointInfo. armLength = item.armPartLength;
                    if(item.ArmPartJointInfo.partType == ArmPartType.work )
                        item. ArmPartJointInfo. workPosTransform = part. gameObject. GetChild (item.workPosName). transform;
                    else item. ArmPartJointInfo. workPosTransform = part.gameObject.transform;
                    armPartJointInfos. Add (item. ArmPartJointInfo);
                }
            }
            if ( armPartJointInfos. Count == 0 )
            {
                Debug. Log ("错误:" + "当前载具上不存在机械臂组件");
                return;
            }
            
            //根据partIndexInVessel的连续性对机械臂组件进行初步划分为多个机械臂数组
            List<List<ArmPartJointInfo>> armGroups = new List<List<ArmPartJointInfo>> ();
            List<ArmPartJointInfo> currentGroup = new List<ArmPartJointInfo> ();
            for ( int i = 0; i < armPartJointInfos. Count; i++ )
            {
                if ( currentGroup. Count == 0 )
                {
                    currentGroup. Add (armPartJointInfos[i]);
                }
                else
                {
                    if ( armPartJointInfos[i]. partIndexInVessel == currentGroup[currentGroup. Count - 1]. partIndexInVessel + 1 )
                    {
                        currentGroup. Add (armPartJointInfos[i]);
                    }
                    else
                    {
                        armGroups. Add (new List<ArmPartJointInfo>(currentGroup) );
                        currentGroup. Clear ();
                        currentGroup. Add (armPartJointInfos[i]);
                    }
                }
            }
            armGroups. Add (new List<ArmPartJointInfo> (currentGroup));
            Debug.Log("提示:当前载具上共检测到" + armGroups.Count + "组机械臂组件组合");
            foreach ( var group in armGroups )
            {
                string groupInfo = "机械臂组件组合：";
                foreach ( var partInfo in group )
                {
                    groupInfo += $"[{partInfo.partIndexInVessel}:{partInfo.partType}]";
                }
                Debug. Log (groupInfo);
            }
            //对每个机械臂数组进行检查，符合条件的添加到机械臂列表中。条件：1、从底座或者工作臂开始，到底座或者工作臂结束，中间可以有多个连接臂，且至少包含一个底座、两个连接臂和一个工作臂。2、如果检测到多个底座或者多个工作臂，对该机械臂数组进行划分，形成多个机械臂。3、每个机械臂的组件必须是连续的。4、机械臂组件的类型必须一致。
            List<List<ArmPartJointInfo>> validArmGroups = new List<List<ArmPartJointInfo>> ();
            foreach ( var group in armGroups )
            {
                int n = group.Count;
                int i = 0;
                while (i < n)
                {
                    // 1. 找到下一个A或C作为start
                    if (!(group[i].partType == ArmPartType.Base || group[i].partType == ArmPartType.work))
                    {
                        i++;
                        continue;
                    }
                    int start = i;
                    ArmWorkType workType = group[start].armWorkType;

                    // 2. 向后找下一个A或C（不包括start本身）
                    int end = start + 1;
                    while (end < n && !(group[end].partType == ArmPartType.Base || group[end].partType == ArmPartType.work))
                    {
                        end++;
                    }
                    // end指向下一个A或C，或n
                    int segEnd = (end < n) ? end : n - 1;

                    // 3. 检查[start,segEnd]是否满足条件
                    int baseCount = 0, workCount = 0, linkCount = 0;
                    bool typeConsistent = true, isContinuous = true;
                    for (int k = start; k <= segEnd; k++)
                    {
                        if (group[k].armWorkType != workType)
                            typeConsistent = false;
                        if (k > start && group[k].partIndexInVessel != group[k - 1].partIndexInVessel + 1)
                            isContinuous = false;
                        if (group[k].partType == ArmPartType.Base) baseCount++;
                        else if (group[k].partType == ArmPartType.work) workCount++;
                        else if (group[k].partType == ArmPartType.link) linkCount++;
                    }
                    if (typeConsistent && isContinuous && baseCount >= 1 && workCount >= 1 && linkCount >= 2)
                    {
                        validArmGroups.Add(group.GetRange(start, segEnd - start + 1));
                    }
                    // 4. 下一个start
                    i = (end < n) ? end : n;
                }
            }
            if ( validArmGroups. Count == 0 )
            {
                Debug. Log ("错误:" + "当前载具上不存在符合条件的机械臂组件组合");
                return;
            }
            else
            {
                Debug. Log ("提示:当前载具上共检测到" + validArmGroups. Count + "组符合条件的机械臂组件组合" );
                foreach ( var arm in validArmGroups )
                {
                    string armInfo = "机械臂组件列表：";
                    foreach ( var partInfo in arm )
                    {
                        partInfo.partIndexInArm = arm. IndexOf (partInfo);
                        armInfo += $"[{partInfo.partIndexInVessel}:{partInfo.partType}]";
                    }
                    Debug. Log (armInfo);
                }
                CASNFP_RoboticArmPart = validArmGroups. ToArray ();
            }
        }
        //启动机械臂自动控制的UI控制窗口
        private void StartAutoCtrl ()
        {
            if ( CASNFP_RoboticArmPart. Length != 0 )
            {
                if ( !gameObject. TryGetComponent<CASNFP_SetRocAutoCtrl> (out _armAutoCtrl) )
                {
                    _armAutoCtrl = gameObject. AddComponent<CASNFP_SetRocAutoCtrl> ();
                }
                else
                    _armAutoCtrl. Start ();
            }else Debug. LogError ("错误："+"机械臂识别过程中发生错误，自动控制程序终止");
        }

        protected override void OnDestroy ()
        {
            base. OnDestroy ();
            if(popupDialog != null ) {PopupDialog.Destroy (popupDialog);}
            if ( ArmAutoCtrl != null )
            {
                Destroy (_armAutoCtrl);
            }
            if ( instance != null )
            {
                instance = null;
            }
        }
        protected override void OnFalse ()
        {
            popupDialog?.Dismiss ();
            if ( LauncherButton. toggleButton. CurrentState != UIRadioButton. State. False )
            {
                LauncherButton?.SetFalse ();
            }
        }
    }
}

