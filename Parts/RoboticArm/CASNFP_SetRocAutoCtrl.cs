using ChinaAeroSpaceNearFuturePackage.UI;
using Expansions.Serenity;
using KSP. Localization;
using System;
using System. Collections. Generic;
using System. Linq;
using UnityEngine;
namespace ChinaAeroSpaceNearFuturePackage.Parts.RoboticArm
{
    public class CASNFP_SetRocAutoCtrl:MonoBehaviour
    {
        /// <summary>
        /// 所有的字段
        /// </summary>
        [KSPField (isPersistant = true)]
        int currentIndex;
        ConfigNode thisSetting = SettingLoader. CASNFP_GlobalSettings;
        Vector2 actionPram;
        public List<ArmPartJointInfo>[] CASNFP_RoboticArmPart;
        private event Action<Vector2> onValueChanged;
        public List<ArmPartJointInfo> currentWorkingRoboticArm;
        /// <summary>
        /// 所有的属性
        /// </summary>
        public int CurrentIndex
        {
            get
            {
                return currentIndex;
            }
            private set
            {
                if ( currentIndex != value )
                {
                    actionPram.x = currentIndex;
                    actionPram.y = value;
                    currentIndex = value;
                    onValueChanged?.Invoke (actionPram);
                }
            }
        }
        /// <summary>
        /// unity事件
        /// </summary>
        /// <summary>
        /// 机械臂自动控制程序，先区分有几个机械臂，然后让玩家选择一个机械臂进行控制，判断机械臂类型，选择目标位置和目标姿态，控制机械臂到达目标位置和姿态，按计划开始工作。
        /// </summary>
       
        public void Start ()
        {
            Debug. Log ("开始执行自动控制程序");
            CASNFP_RoboticArmPart = CASNFP_UI.Instance.CASNFP_RoboticArmPart;
            if ( thisSetting != null )
            {
                if ( thisSetting. HasValue ("currentIndex") )
                {
                    currentIndex = int. Parse (thisSetting. GetValue ("currentIndex"));
                }
            }
            if ( onValueChanged == null )
            {
                onValueChanged += new Action<Vector2> (OnValueChanged);
            }
            if ( CASNFP_RoboticArmPart.Length > 1 )
            {
                CreatArmSelectionWindow (CASNFP_RoboticArmPart);
                if ( currentIndex < 0 || currentIndex >= CASNFP_RoboticArmPart. Length )
                    currentIndex = 0;
                onValueChanged. Invoke (actionPram = new Vector2 (-1, currentIndex));
            }
            else
            {
                currentWorkingRoboticArm = CASNFP_RoboticArmPart[0];
                Debug. Log ("仅检测到一个机械臂，直接进入自动控制状态。");
                SeparateWorkType ();
            }
        }

        
        
        public void OnDestroy ()
        {
            OnSave (thisSetting);
            onValueChanged = null;
            if ( dialog1 != null )
            {
                dialog1. Dismiss ();
                PopupDialog. Destroy (dialog1);
            }
            if (sampleArmCtrlLogic != null)
            {
                Destroy(sampleArmCtrlLogic);
            }
            if(workArmCtrlLogic != null)
            {
                Destroy(workArmCtrlLogic);
            }
            if(grabbingArmCtrlLogic != null)
            {
                Destroy(grabbingArmCtrlLogic);
            }
            if(cameraArmCtrlLogic != null)
            {
                Destroy(cameraArmCtrlLogic);
            }
        }

        SampleArmCtrlLogic sampleArmCtrlLogic;
        WorkArmCtrlLogic workArmCtrlLogic;
        GrabbingArmCtrlLogic grabbingArmCtrlLogic;
        CameraArmCtrlLogic cameraArmCtrlLogic;
        PopupDialog dialog1;
        private void SeparateWorkType ()
        {
            Debug. Log ("进入机械臂工作类型区分逻辑" + $"当前机械臂是{CASNFP_RoboticArmPart.IndexOf(currentWorkingRoboticArm)+1}号");
            //启动各工作臂目标设置逻辑，目前只写取样臂逻辑。
            if( sampleArmCtrlLogic != null)
            {
                Destroy(sampleArmCtrlLogic);
            }
            if( workArmCtrlLogic != null)
            {
                Destroy(workArmCtrlLogic);
            }
            if( grabbingArmCtrlLogic != null)
            {
                Destroy(grabbingArmCtrlLogic);
            }
            if( cameraArmCtrlLogic != null)
            {
                Destroy(cameraArmCtrlLogic);
            }
            switch ( currentWorkingRoboticArm[0].armWorkType)
            {
                case ArmWorkType. Sample_ChangE:
                    sampleArmCtrlLogic = gameObject. AddComponent<SampleArmCtrlLogic> ();
                    break;
                case ArmWorkType. Walk_TianGong:
                    workArmCtrlLogic = gameObject. AddComponent<WorkArmCtrlLogic> ();
                    break;
                case ArmWorkType. Grabbing:
                    grabbingArmCtrlLogic = gameObject. AddComponent<GrabbingArmCtrlLogic> ();
                    break;
                case ArmWorkType. Camera:
                    cameraArmCtrlLogic = gameObject. AddComponent<CameraArmCtrlLogic> ();
                    break;
            }
        }

        #region 生成一个用户选择窗口，当用户选择机械臂时，CurrentIndex数值变化的事件触发
        /// <summary>
        /// 创建机械臂选择窗口
        /// </summary>
        DialogGUIToggleGroup toggleGroup;
        DialogGUILabel label;
        void CreatArmSelectionWindow (List<ArmPartJointInfo>[] roboticArmIndex)
        {
            int index = roboticArmIndex.Length;
            label = new DialogGUILabel (flexH: true, GetLabelString, 390f, 0f)
            {
                guiStyle = new UIStyle (HighLogic. UISkin. label)
                {
                    alignment = TextAnchor. MiddleLeft
                }
            };
            DialogGUIButton close = new DialogGUIButton ("确认选择", ConfirmSelection, true);
            DialogGUIToggle[] dialogGUIToggles = new DialogGUIToggle[index];
            if ( CurrentIndex > index )
            {
                currentIndex = 0;
            }
            for ( int i = 0 ; i < index ; i++ )
            {
                bool isCurrent = i == CurrentIndex ? true : false;
                dialogGUIToggles[i] = new DialogGUIToggle (isCurrent, $"{i + 1}  号", OnSelected);
            }
            toggleGroup = new DialogGUIToggleGroup (dialogGUIToggles);
            var dialog = new MultiOptionDialog (
                "CASNFP_ControlPanel",
                "",
                "机械臂选择面板",
                HighLogic. UISkin,
                new Rect (0.7f, 0.7f, 400f, 200f),
                new DialogGUIVerticalLayout (
                    label,
                    new DialogGUIHorizontalLayout (toggleGroup),
                    new DialogGUIHorizontalLayout (
                        new DialogGUIFlexibleSpace (), close, new DialogGUIFlexibleSpace ())
                )
            );
            dialog1 = PopupDialog. SpawnPopupDialog (
                new Vector2 (0.5f, 0.5f),
                new Vector2 (0.5f, 0.5f),
                dialog,
                false,
                HighLogic. UISkin
            );
        }
        /// <summary>
        /// CurrentIndex数值变化的事件执行逻辑
        /// </summary>
        /// <param name="oldIndex">前一个选择的机械臂序号，默认选择0号机械臂</param>
        /// <param name="newIndex">当前选择的机械臂序号</param>
        protected virtual void OnValueChanged (Vector2 actionPram)
        {
            //刷新控制面板内容
            label. Update ();
            Debug. Log ($"原有机械臂序号是{actionPram. x}，新的机械臂序号是{actionPram. y}");
            if ( actionPram. x >= 0 )
            {
                foreach ( var item in CASNFP_RoboticArmPart[( int )actionPram. x])
                {
                    Part p = item. part;
                    p. Highlight (false);
                }
            }
            if ( actionPram. y >= 0 )
            {
                foreach ( var item in CASNFP_RoboticArmPart[( int )actionPram. y])
                {
                    Part p = item. part;
                    p. Highlight (true);
                }
            }
        }
        private void OnSave (ConfigNode node)
        {
            if ( !HighLogic. LoadedSceneIsFlight )
            {
                return;
            }
            if ( node == null )
                return;
            if ( node. HasValue ("currentIndex") )
            {
                if ( int. Parse (node. GetValue ("currentIndex")) != currentIndex )
                    node. SetValue ("currentIndex", currentIndex, true);
            }
            else
                node. SetValue ("currentIndex", currentIndex, true);
        }
        private string GetLabelString ()
        {
            int index = CASNFP_RoboticArmPart.Length;
            string armSelection = $"    在飞船检测到{index}个机械臂，请选择需要控制的机械臂：\n";
            armSelection += $"    当前控制的是 {CurrentIndex+1} 号机械臂，可选择其他机械臂。";
            return armSelection;
        }
        private void OnSelected (bool arg1)
        {
            if ( !arg1 )
                return;
            if ( arg1 )
            {
                for ( int i = 0 ; i < toggleGroup. children. Count ; i++ )
                {
                    if ( toggleGroup. children[i] is DialogGUIToggle )
                    {
                        DialogGUIToggle toggle = toggleGroup. children[i] as DialogGUIToggle;
                        if ( toggle. toggle. isOn )
                        {
                            CurrentIndex = i;
                        }
                    }
                }
            }
        }
        private void ConfirmSelection ()
        {
            OnSave (thisSetting);
            //确认选择的机械臂，启动机械臂控制逻辑
            if ( actionPram. x >= 0 )
            {
                Debug. Log ("不是默认值");
                //有原来选择的机械臂要收回、保持、或取消确认
                if ( actionPram. x != actionPram. y )
                {
                    Debug. Log ("启动回收动作");
                    foreach ( var item in CASNFP_RoboticArmPart[( int )actionPram.x])
                    {

                        BaseServo servo = item. servoHinge;
                        if ( servo. Events. Contains ("ResetPosition") )
                        {
                            servo. Events["ResetPosition"]. Invoke ();
                        }
                    }
                }
            }
            currentWorkingRoboticArm = CASNFP_RoboticArmPart[( int )actionPram. y];
            foreach (var item in currentWorkingRoboticArm)
            {
                Part p = item.part;
                p.Highlight(false);
            }
            SeparateWorkType ();
        }
        #endregion
        
    }
}