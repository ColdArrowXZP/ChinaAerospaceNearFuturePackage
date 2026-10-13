using ChinaAeroSpaceNearFuturePackage. Core. Managers;
using KSP. Localization;
using KSPAchievements;
using System;
using System. Collections;
using System. Collections. Generic;
using System. Linq;
using System. Reflection;
using System. Text;
using System. Threading. Tasks;
using UnityEngine;
using UnityEngine. Events;
using UnityEngine. SceneManagement;
namespace ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts
{

    /// <summary>
    /// 所有机械臂部件的基类[CASNFP]
    /// </summary>
    public class BaseManipulator:PartModule //IAxisFieldLimits, IConstruction, IJointLockState, IPartCostModifier, IPartMassModifier, IResourceConsumer, IManipulatorServo
    {
        [UI_Toggle (disabledText = "已关闭", scene = UI_Scene. All, enabledText = "已启动")]
        [KSPField (isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "机械臂自动控制开关",groupName ="CASNFP",groupDisplayName ="机械臂控制",groupStartCollapsed =true)]
        public bool servoIsLocked = false;//是否锁定
        [UI_Toggle (disabledText = "关闭", scene = UI_Scene. All, enabledText = "打开")]
        [KSPField (isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "控制面板开关", groupName = "CASNFP", groupDisplayName = "机械臂控制", groupStartCollapsed = true)] 
        public bool isControlPanelShow = false;
        [KSPField]
        public int jointCount = 4;
        [KSPField]
        public string jointsPartArray= "";
        [KSPField]
        public string bottomNodeName = "bottom";
        [KSPField]
        public string workNodeName = "workDrill";
        protected SaveJointInfo bottomJoint;
        protected SaveJointInfo topJoint;
        public SaveJointInfo[] jointsSaved;
        protected ConfigNode thisConfigNode;
        protected UI_Toggle toggleLock;
        protected UI_Toggle toggle;
        private PopupDialog dialog;
        protected bool isMoing;
        public override void OnAwake ()
        {
            base. OnAwake ();
            Debug. Log ("BaseManipulator OnAwake");
            if ( thisConfigNode == null )
            {
                thisConfigNode = part. partInfo. partConfig. GetNode ("MODULE", "name", this.moduleName);
                if ( thisConfigNode == null ){Debug. Log ("thisConfigNode is null");Destroy (this); return;
                }
            }
            if ( jointsSaved == null || jointsSaved. Length == 0 || jointsSaved. Length != jointCount )
            {
                jointsSaved=CreatJointInfoWithConfig (thisConfigNode);
                if(jointsSaved.Length == 0 )
                {
                    CASNFPLogger. Instance. LogError ("程序出错，机械臂部件的关节信息为空，终止机械臂控制程序");
                    Destroy (this);
                    return;
                }
            }
            else
            {
                bottomJoint = jointsSaved[0];
                topJoint = jointsSaved[jointsSaved. Length - 1];
            }
        }
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            Debug. Log ("BaseManipulator Onstart");
            GameEvents. onPartActionUIDismiss. Add (OnPartActionUIDismiss);
            GameEvents. onPartActionUIShown. Add (OnPartActionUIShown);
            for ( int i = 0 ; i < jointsSaved. Length ; i++ )
            {
                part. gameObject. GetChild (jointsSaved[i]. jointTransformName). transform. localRotation = Quaternion. AngleAxis (jointsSaved[i]. currentAngle, jointsSaved[i]. axis);
            }
        }
        public override void OnSave (ConfigNode node)
        {
            base. OnSave (node);
            Debug. Log ("BaseManipulator Onsave");
            if ( jointsSaved == null || jointsSaved. Length == 0 )return;
            if ( node. HasNode ("MANIPULATOR_JOINTS") )
            {
                node.RemoveNode ("MANIPULATOR_JOINTS");
            }
            ConfigNode jointsNode = node. AddNode ("MANIPULATOR_JOINTS");
            for ( int i = 0 ; i < jointsSaved. Length ; i++ )
            {
                ConfigNode jointNode = jointsNode. AddNode ("MYJOINT");
                jointNode. AddValue ("name", jointsSaved[i]. name);
                jointNode. AddValue ("currentAngle", jointsSaved[i].currentAngle);
                jointNode. AddValue ("targetAngle", jointsSaved[i]. targetAngle);
            }
        }
        public override void OnLoad (ConfigNode node)
        {
            base. OnLoad (node);
            Debug. Log ("BaseManipulator Onsave");
            if ( jointsSaved == null || jointsSaved. Length == 0 )
                {
                Debug. Log ("onload:jointsSaved is null");
                return;
            }
            ConfigNode jointsNode = node. GetNode ("MANIPULATOR_JOINTS");
            if ( jointsNode != null )
            {
                Debug. Log ("onload:catch jointsNode");
                ConfigNode[] jointNodes = jointsNode. GetNodes ("MYJOINT");
                for ( int i = 0 ;i < jointsSaved. Length ; i++ )
                {
                    string name = jointsSaved[i]. name;
                    for ( int j = 0 ; j < jointNodes. Length ; j++ )
                    {
                        if ( jointNodes[j]. GetValue ("name") == name )
                        {
                            jointsSaved[i].currentAngle = float. Parse (jointNodes[j]. GetValue ("currentAngle"));
                            jointsSaved[i]. targetAngle = float. Parse (jointNodes[j]. GetValue ("targetAngle"));
                            break;
                        }
                    }
                }
            }
        }
        public void OnDestroy ()
        {
            Debug. Log ("BaseManipulator Ondestroy");
             GameEvents. onPartActionUIDismiss. Remove (OnPartActionUIDismiss);
            GameEvents.onPartActionUIShown. Remove (OnPartActionUIShown);
            
        }
        public override void OnUpdate ()
        {
            base. OnUpdate ();
            if ( !isMoing )
            {
                return;
            }
            if ( HighLogic. LoadedSceneIsFlight && servoIsLocked == false )
            {
                int j = 0;
                for ( int i = 0 ; i < jointsSaved. Length ; i++ )
                {
                    RotateJointInFlight (i);
                    if ( jointsSaved[i]. targetAngle == jointsSaved[i]. currentAngle )
                        j++;
                }
                if ( j == jointsSaved. Length )
                {
                    Debug. Log ("机械臂运动结束");
                    isMoing = false;
                }
            }
        }
        
        protected virtual void RotateJointInFlight (int index)
        {
            if ( jointsSaved[index].targetAngle == jointsSaved[index].currentAngle ) 
            {
                return;
            }
            float angle = Mathf. MoveTowards (jointsSaved[index].currentAngle, jointsSaved[index].targetAngle, Time. deltaTime * jointsSaved[index].rotaSpeed);
            part. gameObject. GetChild (jointsSaved[index]. jointTransformName). transform. localRotation = Quaternion. AngleAxis (angle, jointsSaved[index]. axis);
            jointsSaved[index]. currentAngle = angle;
        }
        protected virtual void RotateJointInEditor(int index)
        {
            if ( index < 0 || index >= jointsSaved. Length )
            {
                CASNFPLogger. Instance. LogError ($"无效的关节索引: {index}");
                return;
            }

            SaveJointInfo joint = jointsSaved[index];
            if ( joint. jointIsLocked )
               {
                ScreenMessages. PostScreenMessage ("机械臂已启动自动控制，无法人工操作", 3f, ScreenMessageStyle. LOWER_CENTER);
                                return;
            }
            if ( joint. jointTransformName == null )
            {
                CASNFPLogger. Instance. LogError ($"关节 {index} 的 Transform 未设置");
                return;
            }
            float angle = Mathf. Clamp (joint. targetAngle, joint. minMaxRotationAngle. x, joint. minMaxRotationAngle. y);
            Quaternion rotation = Quaternion. AngleAxis (angle, joint. axis);
            part. gameObject. GetChild (joint. jointTransformName). transform. localRotation = rotation;
            jointsSaved[index].currentAngle = joint. targetAngle;
        }
        #region 构建控制面板及各项功能
        private void OnPartActionUIShown (UIPartActionWindow data0, Part data1)
        {
            if ( data1 != part )
            {
                return;
            }
            if ( HighLogic. LoadedSceneIsFlight )
            {
                toggleLock = Fields["servoIsLocked"]. uiControlFlight as UI_Toggle;
                toggle = Fields["isControlPanelShow"]. uiControlFlight as UI_Toggle;
            }
            else
            {
                if ( HighLogic. LoadedSceneIsEditor )
                    {toggleLock = Fields["servoIsLocked"]. uiControlEditor as UI_Toggle;
                    toggle = Fields["isControlPanelShow"]. uiControlEditor as UI_Toggle;
                }
            }
            toggleLock. onFieldChanged = OnLockFieldChanged;
            toggle. onFieldChanged = OnFieldChanged;
        }
        private void OnPartActionUIDismiss (Part data)
        {
            if (isControlPanelShow)isControlPanelShow = !isControlPanelShow;
            if ( dialog != null )
            {
                dialog. Dismiss ();
                dialog = null;
            }
            toggleLock. onFieldChanged = null;
            toggle. onFieldChanged = null;
        }

        private void OnLockFieldChanged (BaseField arg1, object arg2)
        {
            if ( servoIsLocked )
            {
                toggle. onFieldChanged = null;
                toggle. controlEnabled = false;
            }
            else
            {
                toggle. onFieldChanged = OnFieldChanged;
                toggle. controlEnabled = true;
            }
            for ( int i = 0 ; i < jointsSaved. Length ; i++ )
            {
                jointsSaved[i]. jointIsLocked = servoIsLocked;
            }
        }

        private void OnFieldChanged (BaseField arg1, object arg2)
        {
            if ( isControlPanelShow )
            {
                    dialog = ShowDialog ();
            }
            else
            {
                if ( dialog != null )
                {
                    dialog. Dismiss ();
                    dialog = null;
                }
            }
        }

        private PopupDialog ShowDialog ()
        {
            UIStyle lowerRightStyle = new UIStyle (HighLogic. UISkin. label)
            {
                alignment = TextAnchor. LowerRight,
                stretchWidth = true,
                fontSize = 12
            };
            UIStyle midLeftStyle = new UIStyle (HighLogic. UISkin. label)
            {
                alignment = TextAnchor.MiddleLeft,
                stretchWidth = true,
                fontSize = 12
            };
            DialogGUIBase[] dialogGUIs = new DialogGUIBase[jointsSaved.Length+2];
            for ( int i = 0 ; i < dialogGUIs.Length ; i++ )
            {
                int currentIndex = i;
                if ( currentIndex == 0 )
                {
                    dialogGUIs[currentIndex] = new DialogGUILabel ($"当前机械臂有{jointsSaved.Length}个关节,可调节各关节角度",true, false);
                    continue;
                }
                if ( currentIndex > jointsSaved. Length )
                {
                    dialogGUIs[currentIndex] = new DialogGUILabel ("V:" + CASNFP_Globals. CASNFP_VERSION, lowerRightStyle, true, false);
                    break;
                }
                DialogGUIBase leftDialog = new DialogGUIHorizontalLayout (false, false, 0f, new RectOffset (0, 0, 0, 0), TextAnchor. MiddleLeft, new DialogGUILabel (()=> "第" + ( currentIndex ). ToString () + "号关节角度："+ jointsSaved[currentIndex - 1]. targetAngle. ToString () + "°", midLeftStyle, true, false));
                DialogGUIBase sliderDialog = new DialogGUIHorizontalLayout (false, false, 0f, new RectOffset (0, 0, 0,0), TextAnchor.MiddleLeft, new DialogGUISlider (() => jointsSaved[currentIndex - 1]. targetAngle, jointsSaved[currentIndex - 1]. minMaxRotationAngle. x, jointsSaved[currentIndex - 1]. minMaxRotationAngle. y, true, 150f, 5f,
                    (float value) =>
                    {
                        if ( currentIndex >= 1 && currentIndex <= jointsSaved. Length )
                        {

                            jointsSaved[currentIndex - 1]. targetAngle = Mathf. Clamp (value, jointsSaved[currentIndex - 1]. minMaxRotationAngle. x, jointsSaved[currentIndex - 1]. minMaxRotationAngle. y);
                            if ( HighLogic. LoadedSceneIsEditor )
                                {
                                RotateJointInEditor (currentIndex - 1);
                            }
                            if(HighLogic.LoadedSceneIsFlight)
                            isMoing = true;
                        }

                    }));
                //最后将三个信息组合起来
                dialogGUIs[currentIndex] = new DialogGUIHorizontalLayout (false, false, 0f, new RectOffset (0, 0,0,0), TextAnchor.MiddleLeft, leftDialog, sliderDialog);
            }
            DialogGUIVerticalLayout dialogGUIVerticalLayout = new DialogGUIVerticalLayout (true,
                true,
                5f,
                new RectOffset (10, 10, 10, 10),
                TextAnchor. MiddleCenter,
                dialogGUIs);
            return PopupDialog.SpawnPopupDialog (new Vector2 (0.5f, 0.5f), new Vector2 (0.5f, 0.5f), new MultiOptionDialog ("", "", "机械臂控制面板", HighLogic.UISkin, dialogGUIVerticalLayout),false, HighLogic.UISkin,false,"");
        }


        /// <summary>
        /// 读取cfg中各关节的参数，设置关节信息。
        /// </summary>
        /// <param name="node">cfg中这个MODULE的存储节点</param>
        /// <returns>SaveJointInfo[]数组</returns>
        private SaveJointInfo[] CreatJointInfoWithConfig (ConfigNode node)
        {
            if ( jointsPartArray == "" )
            {
                CASNFPLogger. Instance. LogWarning ("机械臂部件的关节名称为空值");
                return new SaveJointInfo[0];
            }
            string[] jointsPartNames = jointsPartArray. Split (new char[3] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            ConfigNode[] readNodes = node. GetNodes ("JointConfig");
            if ( jointsPartNames. Length != jointCount || readNodes. Length != jointCount )
            {
                CASNFPLogger. Instance. LogWarning ("机械臂部件的关节数量和关节名称数量不匹配");
                return new SaveJointInfo[0];
            }
            for ( int i = 0 ; i < jointsPartNames. Length ; i++ )
            {
                jointsPartNames[i] = jointsPartNames[i]. Trim ();
            }
            SaveJointInfo[] jointInfos = new SaveJointInfo[jointCount];
            for ( int i = 0 ; i < readNodes. Length ; i++ )
            {
                string jointPartName = readNodes[i]. GetValue ("jointPartName").Trim();
                for ( int j = 0 ; j < jointsPartNames. Length ; j++ )
                {
                    if ( jointsPartNames[j] == jointPartName)
                    {
                        jointInfos[j] = CreatJointsInfo (readNodes[i]);
                        if ( string. IsNullOrEmpty (jointInfos[j]. name) )
                        {
                            return new SaveJointInfo[0];
                        }
                    }
                }

            }
            bottomJoint = jointInfos[0];
            topJoint = jointInfos[jointInfos. Length - 1];
            return jointInfos;
        }
        private SaveJointInfo CreatJointsInfo (ConfigNode configNode)
        {
            try
            {
                float angle = Mathf. Clamp (float. Parse (configNode. GetValue ("offsetAngle")), ConfigNode. ParseVector2 (configNode. GetValue ("minMaxRotationAngle")). x, ConfigNode. ParseVector2 (configNode. GetValue ("minMaxRotationAngle")). y);
                SaveJointInfo jointInfo = new SaveJointInfo ()
                {
                    name = configNode. GetValue ("jointPartName"),
                    jointTransformName = configNode. GetValue ("jointTransformName"),
                    axis = ChangeToV3 (configNode. GetValue ("rotationAxis")),
                    minMaxRotationAngle = ConfigNode. ParseVector2 (configNode. GetValue ("minMaxRotationAngle")),
                    spring = float. Parse (configNode. GetValue ("driveSpring")),
                    damper = float. Parse (configNode. GetValue ("driveDamper")),
                    maxMotorForce = float. Parse (configNode. GetValue ("maxDriveForce")),
                    yzLimit = float. Parse (configNode. GetValue ("yzLimit")),
                    breakForce = float. Parse (configNode. GetValue ("jointBreakForce")),
                    rotaSpeed = float. Parse (configNode. GetValue ("rotaSpeed")),
                    startAngle = angle,
                    targetAngle = angle,
                    currentAngle = angle
                };
                return jointInfo;
            }
            catch ( Exception )
            {
                CASNFPLogger.Instance.LogError ("关节参数读取错误");
                return new SaveJointInfo ();
            }
        }
        #endregion
        protected Vector3 ChangeToV3 (string v)
        {
            switch ( v )
            {
                case "x":
                    return Vector3. right;
                case "y":
                    return Vector3. up;
                case "z":
                    return Vector3. forward;
                case "X":
                    return Vector3. right;
                case "Y":
                    return Vector3. up;
                case "Z":
                    return Vector3. forward;
                case "-x":
                    return Vector3.left;
                case "-y":
                    return Vector3.down;
                case "-z":
                    return Vector3.back;
                case "-X":
                    return Vector3.left;
                case "-Y":
                    return Vector3.down;
                case "-Z":
                    return Vector3.back;
                default:
                    return Vector3. right;
            }
        }
    }
}
