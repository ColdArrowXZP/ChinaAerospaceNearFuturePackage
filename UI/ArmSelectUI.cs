using ChinaAeroSpaceNearFuturePackage. Core. Managers;
using ChinaAeroSpaceNearFuturePackage. CASNFPParts.ArmParts;
using System;
using System. Collections. Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage.UI
{
    public class ArmSelectUI : MonoBehaviour
    {
        private static int selectedArmIndex = 0;
        private List<ModuleCASNFP_RoboticArmPart> roboticArms;
        private MainControlPanel mainControlPanel;
        private Vector2 actionPram = new Vector2 (-1, -1);

        public ModuleCASNFP_RoboticArmPart CurrentArm
        {
            get; private set;
        }

        private event Action<Vector2> onValueChanged;

        //监听selectedArmIndex的变化，并触发onValueChanged事件
        public int CurrentIndex
        {
            get
            {
                return selectedArmIndex;
            }
            private set
            {
                if ( selectedArmIndex != value )
                {
                    actionPram. x = selectedArmIndex;
                    actionPram. y = value;
                    selectedArmIndex = value;
                    onValueChanged?.Invoke (actionPram);
                }
            }
        }

        private void OnValueChanged (Vector2 vector)
        {
            //刷新控制面板内容，调整高亮当前选择的机械臂
            label. Update ();
            if ( actionPram. x >= 0 )
            {
                roboticArms[( int )actionPram. x]. part. Highlight (false);
            }
            if ( actionPram. y >= 0 )
            {
                roboticArms[( int )actionPram. y]. part. Highlight (true);
            }
        }

        public void Awake ()
        {
            onValueChanged += new Action<Vector2> (OnValueChanged);
        }

        public void Start ()
        {
            mainControlPanel = this. gameObject. GetComponent<MainControlPanel> ();
            roboticArms = mainControlPanel. RoboticArms;
            roboticArms[CurrentIndex]. part. Highlight (true);
            if ( roboticArms. Count > 1 )
            {
                ShowArmSelectionWindow (roboticArms);
            }
            else
            {
                Debug. Log ("仅检测到一个机械臂，直接进入自动控制状态。");
                CurrentArm = roboticArms[0];
                StartAutoCtrl (CurrentArm);
            }
        }
        //获取目标点位置
        private void Update ()
        {
            if ( CurrentArm == null )
                return;

        }
        private void OnDestroy ()
        {
            onValueChanged = null;
        }
        //根据当前机械臂工作类型，启动相应的自动控制逻辑
        public RoboticArmController armController;
        private void StartAutoCtrl (ModuleCASNFP_RoboticArmPart currentArm)
        {
            CASNFPLogger. Instance. Log ("进入自动控制状态，当前控制的机械臂为:" + currentArm. part. name + "；工作类型为：" + currentArm. WorkType. ToString ());
            armController = new RoboticArmController ();
            switch ( currentArm. WorkType )
            {
                case ArmWorkType. Sample_ChangE:
                    if ( !currentArm.part. gameObject. TryGetComponent<RoboticArmController> (out armController) )
                    {
                        armController = currentArm.part.gameObject. AddComponent<RoboticArmController> ();
                        armController. controlledArm = currentArm;
                    }
                    else
                    {
                        armController. controlledArm = currentArm;
                    }
                    break;

                case ArmWorkType. Grabbing:
                    CASNFPLogger. Instance. Log ("进入抓取机械臂控制程序。（当前未实现）");
                    break;

                case ArmWorkType. Walk_TianGong:
                    CASNFPLogger. Instance. Log ("进入天宫巡游机械臂控制程序。（当前未实现）");
                    break;

                case ArmWorkType. Camera:
                    CASNFPLogger. Instance. Log ("进入相机机械臂控制程序。（当前未实现）");
                    break;

                default:
                    CASNFPLogger. Instance. LogError ("未知的机械臂类型");
                    break;
            }
        }

        #region 创建机械臂选择窗口，确认选择并显示

        private DialogGUIToggle[] dialogGUIToggles;
        private DialogGUILabel label;
        private PopupDialog dialog1;

        private void ShowArmSelectionWindow (List<ModuleCASNFP_RoboticArmPart> roboticArms)
        {
            int index = roboticArms. Count;
            label = new DialogGUILabel (flexH: true, GetLabelString, 390f, 0f)
            {
                guiStyle = new UIStyle (HighLogic. UISkin. label)
                {
                    alignment = TextAnchor. MiddleLeft
                }
            };
            DialogGUIButton close = new DialogGUIButton ("确认选择", ConfirmSelection, true);
            dialogGUIToggles = new DialogGUIToggle[index];
            if ( selectedArmIndex > index - 1 || selectedArmIndex < 0 )
            {
                selectedArmIndex = 0;
            }
            for ( int i = 0 ; i < index ; i++ )
            {
                bool isCurrent = i == selectedArmIndex ? true : false;
                dialogGUIToggles[i] = new DialogGUIToggle (isCurrent, $"{i + 1}  号", OnSelected);
            }
            DialogGUIToggleGroup toggleGroup = new DialogGUIToggleGroup (dialogGUIToggles);
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
                        new DialogGUIFlexibleSpace (),
                        close,
                        new DialogGUIFlexibleSpace ())
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

        private void OnSelected (bool arg1)
        {
            if ( !arg1 )
                return;
            if ( arg1 )
            {
                for ( int i = 0 ; i < dialogGUIToggles. Length ; i++ )
                {
                    if ( dialogGUIToggles[i]. toggle. isOn )
                    {
                        CurrentIndex = i;
                    }
                }
            }
        }

        private void ConfirmSelection ()
        {
            dialog1?.Dismiss ();
            roboticArms[CurrentIndex]. part. Highlight (false);
            MainControlPanel. LauncherButton. SetFalse ();
            CurrentArm = roboticArms[CurrentIndex];
            StartAutoCtrl (CurrentArm);
        }

        private string GetLabelString ()
        {
            string armSelection = $"    在飞船检测到{roboticArms. Count}个机械臂，请选择需要控制的机械臂：\n";
            armSelection += $"    当前控制的是 {selectedArmIndex + 1} 号机械臂，可选择其他机械臂。";
            return armSelection;
        }

        #endregion 创建机械臂选择窗口，确认选择并显示
    }
}