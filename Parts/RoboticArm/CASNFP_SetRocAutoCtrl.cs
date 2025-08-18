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
        Dictionary<int, OriginalArm> roboticArmIndex;
        public Part[] CASNFP_RoboticArmPart;
        private Action<Vector2> onValueChanged;
        public OriginalArm currentWorkingRoboticArm = default (OriginalArm);
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
                    OnValueChanged (actionPram);
                }

            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        //实现控制器UI为单例
        private static CASNFP_SetRocAutoCtrl _instance;
        private CASNFP_SetRocAutoCtrl (){}
        public static CASNFP_SetRocAutoCtrl Instance
        {
            get
            {
                if ( _instance == null )
                {
                    _instance = FindObjectOfType<CASNFP_SetRocAutoCtrl> ();
                    if ( _instance == null )
                    {
                        GameObject go = new GameObject (typeof (CASNFP_SetRocAutoCtrl). Name);
                        _instance = go. AddComponent<CASNFP_SetRocAutoCtrl> ();
                    }
                    else
                    {
                        _instance. Start ();
                    }
                }
                else
                {
                    _instance. Start ();
                }
                return _instance;
            }
            
        }


        /// <summary>
        /// unity事件
        /// </summary>
        /// <summary>
        /// 机械臂自动控制程序，先区分有几个机械臂，然后让玩家选择一个机械臂进行控制，判断机械臂类型，选择目标位置和目标姿态，控制机械臂到达目标位置和姿态，按计划开始工作。
        /// </summary>
        public void Awake ()
        {
            if ( _instance == null )
            {
                _instance = this;
            }
        }
        public void Start ()
        {
            Debug. Log ("开始执行自动控制程序");
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
            roboticArmIndex = CheckAndDistinguishTheArm (CASNFP_RoboticArmPart);
            if ( roboticArmIndex. Count == 0 )
            {
                Debug. Log ("错误:" + "没有找到机械臂部件，或自动识别机械臂错误，请检查机械臂部件是否正确连接。");
                return;
            }
            else
            {
                if ( roboticArmIndex. Count > 1 )
                {
                    CreatArmSelectionWindow (roboticArmIndex);
                    if ( currentIndex < 0 || currentIndex > roboticArmIndex. Count )
                        currentIndex = 0;
                    onValueChanged. Invoke (actionPram = new Vector2 (-1, currentIndex));
                }
                else
                {
                    currentWorkingRoboticArm = roboticArmIndex[0];
                    Debug. Log ("仅检测到一个机械臂，直接进入自动控制状态。");
                    SeparateWorkType ();
                }
            }
        }

        
        
        public void OnDestroy ()
        {
            OnSave (thisSetting);
            onValueChanged -= new Action<Vector2> (OnValueChanged);
            if(sampleArmCtrlLogic != null)
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
            //如果是单例模式，则销毁实例
            if ( Instance != null )
            {
                _instance = null;
            }
        }
        SampleArmCtrlLogic sampleArmCtrlLogic;
        WorkArmCtrlLogic workArmCtrlLogic;
        GrabbingArmCtrlLogic grabbingArmCtrlLogic;
        CameraArmCtrlLogic cameraArmCtrlLogic;
        private void SeparateWorkType ()
        {

            //启动各工作臂目标设置逻辑，目前只写取样臂逻辑。
            switch ( currentWorkingRoboticArm.armWorkType )
            {
                case ArmWorkType. Sample_ChangE:
                    if ( !gameObject. TryGetComponent<SampleArmCtrlLogic> (out sampleArmCtrlLogic) )
                    {
                        sampleArmCtrlLogic = gameObject. AddComponent<SampleArmCtrlLogic> ();
                    }
                    else
                        sampleArmCtrlLogic. Start();
                    break;
                case ArmWorkType. Walk_TianGong:
                    if ( !gameObject. TryGetComponent<WorkArmCtrlLogic> (out workArmCtrlLogic) )
                    {
                        workArmCtrlLogic = gameObject. AddComponent<WorkArmCtrlLogic> ();
                    }
                    else
                        workArmCtrlLogic. Start ();
                    break;
                case ArmWorkType. Grabbing:
                    if ( !gameObject. TryGetComponent<GrabbingArmCtrlLogic> (out grabbingArmCtrlLogic) )
                    {
                        grabbingArmCtrlLogic = gameObject. AddComponent<GrabbingArmCtrlLogic> ();
                    }
                    else
                        grabbingArmCtrlLogic. Start ();
                    break;
                case ArmWorkType. Camera:
                    if ( !gameObject. TryGetComponent<CameraArmCtrlLogic> (out cameraArmCtrlLogic) )
                    {
                        cameraArmCtrlLogic = gameObject. AddComponent<CameraArmCtrlLogic> ();

                    }
                    else
                        cameraArmCtrlLogic. Start ();
                    break;
            }
        }

        #region 生成一个用户选择窗口，当用户选择机械臂时，CurrentIndex数值变化的事件触发
        /// <summary>
        /// 创建机械臂选择窗口
        /// </summary>
        DialogGUIToggleGroup toggleGroup;
        DialogGUILabel label;
        void CreatArmSelectionWindow (Dictionary<int, OriginalArm> roboticArmIndex)
        {
            int index = roboticArmIndex. Count;
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
            PopupDialog dialog1 = PopupDialog. SpawnPopupDialog (
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
                foreach ( var item in roboticArmIndex[( int )actionPram. x]. armParts )
                {
                    item. Highlight (false);
                }
            }
            if ( actionPram. y >= 0 )
            {
                foreach ( var item in roboticArmIndex[( int )actionPram. y]. armParts )
                {
                    item. Highlight (true);
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
            int index = roboticArmIndex. Count;
            string armSelection = $"    在飞船检测到{index}个机械臂，请选择需要控制的机械臂：\n";
            foreach ( var item in roboticArmIndex )
            {
                armSelection += $"    机械臂序号: {item. Key + 1}, 机械臂类型: {item. Value. armWorkType}\n";
            }
            armSelection += $"    当前控制的是 {CurrentIndex} 号机械臂，可选择其他机械臂。";
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
                    foreach ( var item in currentWorkingRoboticArm. armParts )
                    {
                        BaseServo servo = item. FindModuleImplementing<BaseServo> ();
                        if ( servo. Events. Contains ("ResetPosition") )
                        {
                            servo. Events["ResetPosition"]. Invoke ();
                        }
                    }
                }
            }
            currentWorkingRoboticArm = roboticArmIndex[( int )actionPram. y];
            SeparateWorkType ();
        }
        #endregion
        #region 检查机械臂部件并区分机械臂类型
        /// <summary>
        /// 检查机械臂部件并区分机械臂类型
        /// </summary>

        private Dictionary<int, OriginalArm> CheckAndDistinguishTheArm (Part[] cASNFP_RoboticArmPart)
        {
            Dictionary<int, OriginalArm> roboticArmIndex = new Dictionary<int, OriginalArm> ();//机械臂索引，键为机械臂索引，值为机械臂类型和机械臂部件数组
            List<OriginalArmPart> thePartsList = new List<OriginalArmPart> ();//机械臂部件列表
            // 遍历所有机械臂部件，获取机械臂类型和部件类型
            foreach ( Part part in cASNFP_RoboticArmPart )
            {
                ModuleCASNFP_RoboticArmPart roboticArmModule = part. Modules. GetModule<ModuleCASNFP_RoboticArmPart> ();
                OriginalArmPart thePart = new OriginalArmPart
                {
                    part = part,
                    armWorkType = roboticArmModule. ArmType,
                    partType = roboticArmModule. ArmPartType
                };
                thePartsList. Add (thePart);
            }
            // 定义四个列表来存储不同类型的机械臂部件
            List<OriginalArmPart> changEList = new List<OriginalArmPart> ();
            List<OriginalArmPart> tianGongList = new List<OriginalArmPart> ();
            List<OriginalArmPart> grabbingList = new List<OriginalArmPart> ();
            List<OriginalArmPart> cameraList = new List<OriginalArmPart> ();
            //将机械臂组件按照机械臂类型进行分类
            foreach ( var item in thePartsList )
            {
                switch ( item. armWorkType )
                {
                    case ArmWorkType. Sample_ChangE:
                        changEList. Add (item);
                        break;
                    case ArmWorkType. Walk_TianGong:
                        tianGongList. Add (item);
                        break;
                    case ArmWorkType. Grabbing:
                        grabbingList. Add (item);
                        break;
                    case ArmWorkType. Camera:
                        cameraList. Add (item);
                        break;
                    default:
                        Debug. LogError ($"未知的机械臂类型: {item. armWorkType}");
                        break;
                }
            }
            //执行将分类后的机械臂部件按照顺序组成机械臂的方法
            if ( changEList. Count > 0 )
            {
                //如果有嫦娥机械臂部件，则将其分解成机械臂
                Dictionary<int, OriginalArm> _originalArms = Spit_Arm (changEList);

                if ( _originalArms != null && _originalArms. Count > 0 )
                {
                    if ( roboticArmIndex. Count == 0 )
                    {
                        //如果没有机械臂索引，则直接赋值
                        roboticArmIndex = _originalArms;
                    }
                    else
                        roboticArmIndex = MergeDictionaries<OriginalArm> (roboticArmIndex, _originalArms);
                }
            }
            if ( tianGongList. Count > 0 )
            {
                Dictionary<int, OriginalArm> _originalArms = Spit_Arm (tianGongList);
                if ( ( _originalArms != null ) )
                {
                    roboticArmIndex = MergeDictionaries<OriginalArm> (roboticArmIndex, _originalArms);
                }
            }
            if ( grabbingList. Count > 0 )
            {
                Dictionary<int, OriginalArm> _originalArms = Spit_Arm (grabbingList);
                if ( ( _originalArms != null ) )
                {
                    roboticArmIndex = MergeDictionaries<OriginalArm> (roboticArmIndex, _originalArms);
                }
            }
            if ( cameraList. Count > 0 )
            {
                Dictionary<int, OriginalArm> _originalArms = Spit_Arm (cameraList);
                if ( ( _originalArms != null ) )
                {
                    roboticArmIndex = MergeDictionaries<OriginalArm> (roboticArmIndex, _originalArms);
                }
            }
            return roboticArmIndex;
        }
        private static Dictionary<int, T> MergeDictionaries<T> (
        Dictionary<int, T> dictA,
        Dictionary<int, T> dictB)
        {
            var merged = new Dictionary<int, T> ();

            // 添加字典A的所有元素
            foreach ( var item in dictA )
            {
                merged. Add (item. Key, item. Value);
            }

            // 添加字典B的所有元素，键值从字典A最大键+1开始
            int startKey = dictA. Keys. Max () + 1;
            foreach ( var item in dictB )
            {
                merged. Add (startKey++, item. Value);
            }

            return merged;
        }
        /// <summary>
        /// 这里将机械臂数组自动分组，存入序号和结构体originalArm的字典里。
        /// </summary>
        /// <param name="thisPartList">所有工作类型的part数组</param>
        /// <returns>返回一个字典，key是int值序号，value是originalArm结构体的机械臂Part</returns>
        private Dictionary<int, OriginalArm> Spit_Arm (List<OriginalArmPart> thisPartList)
        {
            ArmWorkType workType = thisPartList[0]. armWorkType;
            Dictionary<int, OriginalArm> roboticArmIndex = new Dictionary<int, OriginalArm> ();
            if ( workType == ArmWorkType. Sample_ChangE )
            {
                IEnumerable<OriginalArmPart> armBase = thisPartList. Where (item => item. partType == ArmPartType. Base);
                for ( int i = 0 ; i < armBase. Count () ; i++ )
                {
                    List<Part> parts = new List<Part> ();
                    List<int> index = new List<int> ();
                    for ( int j = thisPartList. IndexOf (armBase. ElementAt (i)) ; j < thisPartList. Count () ; j++ )
                    {
                        parts. Add (thisPartList[j]. part);
                        index. Add (thisPartList[j]. part. vessel. Parts. IndexOf (thisPartList[j]. part));
                        if ( j + 1 >= thisPartList. Count () )
                            break; //如果已经是最后一个部件，则结束循环
                        if ( thisPartList[j + 1]. partType == ArmPartType. Base )
                            break; //如果下一个部件是基座，则结束循环
                    }
                    if ( index. Count < 2 )
                        break;
                    Array. Sort (index. ToArray ());
                    //如果部件索引不连续，则结束循环
                    for ( int j = 0 ; j < index. Count - 1 ; j++ )
                    {
                        if ( index[j + 1] - index[j] != 1 )
                        {
                            //说明机械臂部件不连续，中间夹杂了非CASNFP_RoboticArmPart部件
                            Debug.LogError($"错误:机械臂部件不连续，索引为{index[j]}和{index[j + 1]}之间夹杂了非CASNFP_RoboticArmPart部件，请检查机械臂部件是否正确连接。");
                            return null;
                        }
                    }
                    OriginalArm original = new OriginalArm
                    {
                        armWorkType = workType,
                        armParts = parts. ToArray ()
                    };
                    roboticArmIndex. Add (i, original);
                }
                return roboticArmIndex;
            }
            if ( workType == ArmWorkType. Walk_TianGong )
            {
                IEnumerable<OriginalArmPart> armBase = thisPartList. Where (item => item. partType == ArmPartType. work);
                for ( int i = 0 ; i < armBase. Count () ; i++ )
                {
                    List<Part> parts = new List<Part> ();
                    List<int> index = new List<int> ();
                    for ( int j = thisPartList. IndexOf (armBase. ElementAt (i)) ; j < thisPartList. Count () ; j++ )
                    {
                        parts. Add (thisPartList[j]. part);
                        index. Add (thisPartList[j]. part. vessel. Parts. IndexOf (thisPartList[i]. part));
                        if ( j + 1 >= thisPartList. Count () )
                            break; //如果已经是最后一个部件，则结束循环
                        if ( thisPartList[j]. partType == ArmPartType. work && thisPartList[j + 1]. partType == ArmPartType. work )
                            break; //如果下一个部件是基座，则结束循环
                    }
                    if ( index. Count < 2 )
                        break;
                    Array. Sort (index. ToArray ());
                    //如果部件索引不连续，则结束循环
                    for ( int j = 0 ; j < index. Count - 1 ; j++ )
                    {
                        if ( index[j + 1] - index[j] != 1 )
                        {
                            //说明机械臂部件不连续，中间夹杂了非CASNFP_RoboticArmPart部件
                            Debug. LogError ($"机械臂部件不连续，索引为{index[j]}和{index[j + 1]}之间夹杂了非CASNFP_RoboticArmPart部件，请检查机械臂部件是否正确连接。");
                            return null;
                        }
                    }
                    OriginalArm original = new OriginalArm
                    {
                        armWorkType = workType,
                        armParts = parts. ToArray ()
                    };
                    roboticArmIndex. Add (i, original);
                }
                return roboticArmIndex;
            }
            if ( workType == ArmWorkType. Grabbing || workType == ArmWorkType. Camera )
            {
                Debug. LogError ($"机械臂类型{workType}暂不支持自动控制");
                return null; //抓取式机械臂和摄像类机械臂暂不支持自动控制
            }
            return null; //如果机械臂类型不在上述范围内，则返回null
        }
        #endregion
    }
}