using ChinaAeroSpaceNearFuturePackage.UI;
using Expansions.Serenity;
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
        bool isStartingAutoCtrl = false;
        ConfigNode thisSetting = SettingLoader. CASNFP_GlobalSettings;
        Vector2 actionPram;
        Dictionary<int, OriginalArm> roboticArmIndex;
        public Part[] CASNFP_RoboticArmPart;
        public Action<Vector2> onValueChanged;
        Vector3 targetPoint = Vector3. zero;
        bool isTargetRingSetUp = false;
        public static OriginalArm currentWorkingRoboticArm = default (OriginalArm);
        public GameObject sampleMaxRangeRing;
        /// <summary>
        /// 所有的属性
        /// </summary>
        public int CurrentIndex
        {
            get
            {
                return currentIndex;
            }
            set
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
        public bool IsStartingAutoCtrl
        {
            get; private set;
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
                MessageBox. Instance. ShowDialog ("错误", "没有找到机械臂部件，或自动识别机械臂错误，请检查机械臂部件是否正确连接。");
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
                    IsStartingAutoCtrl = true;
                }
            }
        }

        public void Update ()
        {
            if (!HighLogic.LoadedSceneIsFlight || !IsStartingAutoCtrl)
                return;
            if ( currentWorkingRoboticArm. armParts[0].vessel.srf_velocity.magnitude > 0.01d )
            {
                Debug. Log (currentWorkingRoboticArm. armParts[0]. vessel. srf_velocity. magnitude);
                MessageBox. Instance. ShowDialog ("错误", "载具处于移动中，请停止移动后取样", 5f);
                return;
            }
            //这里设置一个可视化的圆环
            if ( !isTargetRingSetUp )
            {
                SeparateWorkType ();
            }
            else
            {
                MessageBox. Instance. ShowDialog ("错误","设置圆环失败",5f);
            }
            IsStartingAutoCtrl = false;
        }
        public void OnDestroy ()
        {
            OnSave (thisSetting);
            onValueChanged -= new Action<Vector2> (OnValueChanged);
            if ( sampleMaxRangeRing != null )
            {
                Destroy (sampleMaxRangeRing);
            }
            if ( Instance != null )
            {
                _instance = null;
            }
        }




        /// <summary>
        /// 所有的方法
        /// </summary>
        #region 在确定机械臂后，这里是主要的计算逻辑。
        private float armLength = 0;
        private float positionTerrainHeight = 0;
        private bool CanBeReach;
        private void SeparateWorkType ()
        {
            //启动各工作臂目标设置逻辑，目前只写取样臂逻辑。
            switch ( currentWorkingRoboticArm.armWorkType )
            {
                case ArmWorkType. Sample_ChangE:
                    isTargetRingSetUp = SampleTargetSet ();
                    break;
                case ArmWorkType. Walk_TianGong:
                    isTargetRingSetUp = WorkTargetSet ();
                    break;
                case ArmWorkType. Grabbing:
                    isTargetRingSetUp = GrabbingTargetSet ();
                    break;
                case ArmWorkType. Camera:
                    isTargetRingSetUp = CamTargetSet ();
                    break;
            }
        }
        private bool SampleTargetSet ()
        {
            //开始计算取样范围，设置一个绿色圆环供玩家参考取样点
            Debug. Log ("开始设置绿环");
            armLength = CalculateArmLength ();//计算大臂长度
            positionTerrainHeight = CalculatePositionTerrainHeight (out Vector3 ringCenter, out Vector3 normal);
            if ( positionTerrainHeight < 0 )
            {
                MessageBox. Instance. ShowDialog ("错误", "飞船所处位置没有检测到地面实体", 5f);
                return false;
            }
            if ( positionTerrainHeight >= armLength / 2 )//这里的比较也有问题，暂时不管。
            {
                MessageBox. Instance. ShowDialog ("错误", "机械臂距离地面过高，超出最大工作范围，无法设置取样点", 5f);
                return false;
            }
            float radius = ( float )Math. Sqrt (armLength * armLength - positionTerrainHeight * positionTerrainHeight);
            Debug. Log ("计算得出绿环半径为：" + radius);
            SetSampleMaxRange (radius, ringCenter, normal);
            //开始获取鼠标点击事件
            //var ray = FlightGlobals. fetch. mainCameraRef. ScreenPointToRay (Input. mousePosition);

            //if ( !Physics. Raycast (ray, out RaycastHit hit) || hit. collider == null )
            //{
            //    targetPoint = Vector3. zero;
            //    return;
            //}

            //if ( hit. collider. gameObject. layer != 15 )
            //{
            //    ScreenMessages. PostScreenMessage (
            //        Localizer. Format ($"选择取样地点为{hit. collider. gameObject. name}不正确，请选择地面取样点"),
            //        2f, ScreenMessageStyle. UPPER_RIGHT);
            //    targetPoint = Vector3. zero;
            //    return;
            //}
            //targetPoint = hit. point;
            return true;
        }

        private bool CamTargetSet ()
        {
            throw new NotImplementedException ();
        }

        private bool GrabbingTargetSet ()
        {
            throw new NotImplementedException ();
        }

        private bool WorkTargetSet ()
        {
            throw new NotImplementedException ();
        }
        
        public void SetSampleMaxRange (float ringRadius, Vector3 ringCenter, Vector3 normal)
        {

            float radius = ringRadius;
            sampleMaxRangeRing = new GameObject ();
            LineRenderer lineRenderer = sampleMaxRangeRing. AddComponent<LineRenderer> ();
            lineRenderer. useWorldSpace = false;
            lineRenderer. startWidth = 0.15f;
            lineRenderer. endWidth = 0.15f;
            lineRenderer. loop = true;
            lineRenderer. positionCount = 64 + 1;
            lineRenderer. material = new Material (Shader. Find ("KSP/Particles/Additive"));
            lineRenderer. startColor = Color. green;
            lineRenderer. endColor = Color. green;
            float angle = 0f;
            for ( int i = 0 ; i < 64 + 1 ; i++ )
            {
                float x = Mathf. Sin (Mathf. Deg2Rad * angle) * radius;
                float y = Mathf. Cos (Mathf. Deg2Rad * angle) * radius;
                lineRenderer. SetPosition (i, new Vector3 (x, y, 0));
                angle += 360f / 64;
            }
            sampleMaxRangeRing. transform. position = ringCenter;
            sampleMaxRangeRing. transform. rotation = Quaternion. LookRotation (normal, sampleMaxRangeRing. transform. right);
            sampleMaxRangeRing. transform. SetParent (currentWorkingRoboticArm. armParts[0].vessel.transform);
        }
        public float CalculateArmLength ()
        {
            Debug. Log ("开始计算臂长");
            float armLength = 0;
            List<Vector3> linkNodePos = new List<Vector3> ();
            foreach ( Part part in currentWorkingRoboticArm.armParts )
            {
                if ( part. FindModuleImplementing<ModuleCASNFP_RoboticArmPart> (). ArmPartType == ArmPartType. link )

                    linkNodePos. Add (part. gameObject. GetChild (part. FindModuleImplementing<ModuleRoboticServoHinge> (). servoTransformName). transform. position);
            }
            for ( int i = 1 ; i < linkNodePos. Count ; i++ )
            {
                armLength += ( linkNodePos[i] - linkNodePos[i - 1] ). magnitude;
            }
            Debug. Log ("臂长为" + armLength);
            return armLength * 2;//这里默认是大小臂相等且只有两段，是不对的，以后再修改这里臂长的计算方法。
        }
        public float CalculatePositionTerrainHeight (out Vector3 ringCenter, out Vector3 normal)
        {
            Debug. Log ("开始计算基座地形高度");
            float heightFromTerrain = -1f;
            Part basePart = null;
            foreach ( Part part in currentWorkingRoboticArm. armParts )
            {
                if ( part. FindModuleImplementing<ModuleCASNFP_RoboticArmPart> (). ArmPartType == ArmPartType. Base )
                {
                    basePart = part;
                    break;
                }
            }
            Vector3 basePos = basePart. gameObject. GetChild (basePart. FindModuleImplementing<ModuleRoboticServoHinge> (). baseTransformName). transform. position;
            Vector3 calNormal = ( Vector3 )FlightGlobals. getUpAxis (FlightGlobals. getMainBody (), basePos). normalized;
            float num = ( float )FlightGlobals. getMainBody (). Radius / 2;
            RaycastHit[] hits = Physics. RaycastAll (basePos, -calNormal, num);
            ringCenter = new Vector3 (0, 0, 0);
            normal = new Vector3 (0, 0, 0);
            foreach ( var item in hits )
            {
                if ( item. collider. gameObject. layer == 15 )
                {

                    RaycastHit rightHit = item;
                    heightFromTerrain = rightHit. distance;
                    ringCenter = rightHit. point;
                    normal = rightHit. normal;
                    break;
                }
            }
            Debug. Log ("基座地形高度" + heightFromTerrain + "     绿环中心" + ringCenter + "   绿环法向" + normal);
            return heightFromTerrain;
        }
        #endregion
        #region 生成一个用户选择窗口，当用户选择机械臂时，CurrentIndex数值变化的事件触发
        /// <summary>
        /// 创建机械臂选择窗口
        /// </summary>
        DialogGUIToggleGroup toggleGroup;
        DialogGUILabel label;
        void CreatArmSelectionWindow (Dictionary<int, OriginalArm> roboticArmIndex)
        {
            if ( MessageBox. Instance != null )
            {
                GameObject. Destroy (MessageBox. Instance);
            }
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
            IsStartingAutoCtrl = true;
        }
        #endregion
        #region 检查机械臂部件并区分机械臂类型
        /// <summary>
        /// 检查机械臂部件并区分机械臂类型
        /// </summary>

        private Dictionary<int, OriginalArm> CheckAndDistinguishTheArm (Part[] cASNFP_RoboticArmPart)
        {
            Dictionary<int, OriginalArm> roboticArmIndex = new Dictionary<int, OriginalArm> ();//机械臂索引，键为机械臂索引，值为机械臂类型和机械臂部件数组
            List<CheckAndDistinguishThePart> thePartsList = new List<CheckAndDistinguishThePart> ();//机械臂部件列表
            // 遍历所有机械臂部件，获取机械臂类型和部件类型
            foreach ( Part part in cASNFP_RoboticArmPart )
            {
                ModuleCASNFP_RoboticArmPart roboticArmModule = part. Modules. GetModule<ModuleCASNFP_RoboticArmPart> ();
                CheckAndDistinguishThePart thePart = new CheckAndDistinguishThePart
                {
                    part = part,
                    armWorkType = roboticArmModule. ArmType,
                    partType = roboticArmModule. ArmPartType
                };
                thePartsList. Add (thePart);
            }
            // 定义四个列表来存储不同类型的机械臂部件
            List<CheckAndDistinguishThePart> changEList = new List<CheckAndDistinguishThePart> ();
            List<CheckAndDistinguishThePart> tianGongList = new List<CheckAndDistinguishThePart> ();
            List<CheckAndDistinguishThePart> grabbingList = new List<CheckAndDistinguishThePart> ();
            List<CheckAndDistinguishThePart> cameraList = new List<CheckAndDistinguishThePart> ();
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
        private Dictionary<int, OriginalArm> Spit_Arm (List<CheckAndDistinguishThePart> thisPartList)
        {
            ArmWorkType workType = thisPartList[0]. armWorkType;
            Dictionary<int, OriginalArm> roboticArmIndex = new Dictionary<int, OriginalArm> ();
            if ( workType == ArmWorkType. Sample_ChangE )
            {
                IEnumerable<CheckAndDistinguishThePart> armBase = thisPartList. Where (item => item. partType == ArmPartType. Base);
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
                IEnumerable<CheckAndDistinguishThePart> armBase = thisPartList. Where (item => item. partType == ArmPartType. work);
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