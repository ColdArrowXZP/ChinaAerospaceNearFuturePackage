using ChinaAeroSpaceNearFuturePackage. Core. Managers;
using System;
using System. Collections;
using System. Collections. Generic;
using System. Linq;
using System. Reflection;
using System. Text;
using System. Threading. Tasks;
using UnityEngine;
namespace ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts
{

    /// <summary>
    /// 所有机械臂部件的基类[CASNFP]
    /// </summary>
    public class ModuleManipulator:PartModule,IJointLockState
    {
        [UI_Toggle (disabledText = "已关闭", scene = UI_Scene.Flight, enabledText = "已启动")]
        [KSPField (isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "机械臂自动控制开关",groupName = "ManipulatorControl", groupDisplayName = "机械臂控制", groupStartCollapsed = true)]
        public bool isAutoControl = false;//是否锁定
        [UI_Toggle (disabledText = "关闭", scene = UI_Scene. All, enabledText = "打开")]
        [KSPField (isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "控制面板开关", groupName = "ManipulatorControl", groupDisplayName = "机械臂控制", groupStartCollapsed = true)] 
        public bool isControlPanelShow = false;
        [KSPEvent (guiActiveUncommand = true, active = true, guiActiveUnfocused = true, guiActive = true, guiActiveEditor = false, guiName = "机械臂复位", groupName = "ManipulatorControl", groupDisplayName = "机械臂控制", groupStartCollapsed = true)]
        public void ResetPosition ()
        {
            ResetLaunchPosition ();
        }
        [KSPField]
        public int jointCount = 0;
        [KSPField]
        public string jointsPartArray= string.Empty;
        [KSPField(isPersistant = true)]
        public bool isJointUnlocked = true;
        [KSPField (isPersistant = true)]
        public int armTypeIndex = 0;
        //[KSPField]
        //public string bottomNodeName = "bottom";
        //[KSPField]
        //public string workNodeName = "workDrill";
        protected SaveJointInfo bottomJoint;
        protected SaveJointInfo topJoint;
        /// 各关节 Transform 缓存，避免每帧 GetChild</summary>
        protected Transform[] jointTransforms;
        /// 末端执行器在关节数组中的索引（第4个关节，不参与 IK 计算）</summary>
        protected int endEffectorIndex = 3;
        public Vector3 effectorAxisLocal = Vector3.up;
        public SaveJointInfo[] jointsSaved;
        protected ConfigNode thisConfigNode;
        protected UI_Toggle toggleLock;
        protected UI_Toggle toggle;
        private PopupDialog dialog;
        protected bool startMoving;
        private bool isWaitForTargetPoint = false;
        private ArmType armType;
        private Vector3 targetPoint;
        public override void OnAwake ()
        {
            base. OnAwake ();
            if ( thisConfigNode == null )
            {
                thisConfigNode = part. partInfo. partConfig. GetNode ("MODULE", "name", this.moduleName);
                if ( thisConfigNode == null )
                {
                    Debug. Log ("thisConfigNode is null");
                    Destroy (this);
                    return;
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
            bottomJoint = jointsSaved[0];
            topJoint = jointsSaved[jointsSaved. Length - 1];
        }
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            armType = CheckArmType ();
            // 缓存关节 Transform 引用
            jointTransforms = new Transform[jointsSaved. Length];
            for ( int i = 0 ; i < jointsSaved. Length ; i++ )
            {
                jointTransforms[i] = part. gameObject. GetChild (jointsSaved[i]. jointTransformName). transform;
            }
            endEffectorIndex = jointsSaved. Length - 1; // 第4个关节即末端执行器
            GameEvents. onPartActionUIDismiss. Add (OnPartActionUIDismiss);
            GameEvents. onPartActionUIShown. Add (OnPartActionUIShown);
            for ( int i = 0 ; i < jointsSaved. Length ; i++ )//设置关节初始位置。
            {
                part. gameObject. GetChild (jointsSaved[i]. jointTransformName). transform. localRotation = Quaternion. AngleAxis (jointsSaved[i]. currentAngle, jointsSaved[i]. axis);
            }
        }

        private ArmType CheckArmType ()
        {
            switch(armTypeIndex)
            {
                case 0:
                    return ArmType. Sampling;
                case 1:
                    return ArmType. Grasping;
                case 2:
                    return ArmType. Crawling;
                case 3:
                    return ArmType.Connecting;
                default:
                    throw new  System. Exception ("未知的机械臂类型，请检查配置文件");
            }
        }

        public override void OnSave (ConfigNode node)
        {
            base. OnSave (node);
            if ( jointsSaved == null || jointsSaved. Length == 0 )
                return;

            // 创建或获取专门存储关节数据的节点
            ConfigNode jointsNode;
            if ( node. HasNode ("MANIPULATOR_JOINTS") )
            {
                jointsNode = node. GetNode ("MANIPULATOR_JOINTS");
                // 清除旧数据，防止残留导致数据错乱（可选，但推荐重新构建以保证一致性）
                jointsNode. ClearData ();
            }
            else
            {
                jointsNode = node. AddNode ("MANIPULATOR_JOINTS");
            }

            // 遍历所有关节，保存关键数据
            for ( int i = 0 ; i < jointsSaved. Length ; i++ )
            {
                ConfigNode jointNode = jointsNode. AddNode ("JOINT");
                jointNode. AddValue ("name", jointsSaved[i]. name); // 关键：用于加载时匹配
                jointNode. AddValue ("currentAngle", jointsSaved[i]. currentAngle);
                jointNode. AddValue ("targetAngle", jointsSaved[i]. targetAngle);
                jointNode. AddValue ("startAngle", jointsSaved[i]. startAngle); // 保存起始角度
            }
        }

        public override void OnLoad (ConfigNode node)
        {
            base. OnLoad (node);

            // 如果 jointsSaved 尚未初始化（极端情况），则跳过，等待 OnAwake 或 OnStart 处理
            if ( jointsSaved == null || jointsSaved. Length == 0 )
            {
                Debug. LogWarning ("[ModuleManipulator] OnLoad: jointsSaved is not initialized yet.");
                return;
            }

            ConfigNode jointsNode = node. GetNode ("MANIPULATOR_JOINTS");
            if ( jointsNode != null )
            {
                ConfigNode[] jointNodes = jointsNode. GetNodes ("JOINT"); // 注意节点名称要与 OnSave 一致

                foreach ( ConfigNode jNode in jointNodes )
                {
                    string savedName = jNode. GetValue ("name");
                    if ( string. IsNullOrEmpty (savedName) )
                        continue;

                    // 查找匹配的关节
                    for ( int i = 0 ; i < jointsSaved. Length ; i++ )
                    {
                        if ( jointsSaved[i]. name == savedName )
                        {
                            // 恢复数值
                            if ( jNode. HasValue ("currentAngle") )
                                jointsSaved[i]. currentAngle = float. Parse (jNode. GetValue ("currentAngle"));

                            if ( jNode. HasValue ("targetAngle") )
                                jointsSaved[i]. targetAngle = float. Parse (jNode. GetValue ("targetAngle"));

                            if ( jNode. HasValue ("startAngle") )
                                jointsSaved[i]. startAngle = float. Parse (jNode. GetValue ("startAngle"));

                            break; // 找到匹配项后跳出内层循环
                        }
                    }
                }
            }
        }

        public void OnDestroy ()
        {
            // 1. 移除事件监听，防止回调访问已销毁的对象
            GameEvents. onPartActionUIDismiss. Remove (OnPartActionUIDismiss);
            GameEvents. onPartActionUIShown. Remove (OnPartActionUIShown);

            // 2. 安全解散 Dialog
            if ( dialog != null )
            {
                try
                {
                    // 使用 Dismiss 而不是直接 Destroy，让 KSP 优雅地清理 UI
                    dialog. Dismiss ();
                }
                catch ( System. Exception e )
                {
                    // 捕获可能的异常，防止影响其他清理工作
                    Debug. LogWarning ($"[ModuleManipulator] Error dismissing dialog: {e. Message}");
                }
                finally
                {
                    dialog = null;
                }
            }

            // 3. 清空 UI 控件引用，防止悬空引用
            if ( toggleLock != null )
            {
                toggleLock. onFieldChanged = null;
                toggleLock = null;
            }

            if ( toggle != null )
            {
                toggle. onFieldChanged = null;
                toggle = null;
            }
        }

        public override void OnUpdate ()
        {
            base. OnUpdate ();
            if ( !IsJointUnlocked () )
            {
                return;
            }
            if (isWaitForTargetPoint)
            {
                SetTragetPoint();
                return;
            }
            if (startMoving)
            {
                foreach ( var joint in jointsSaved )
                {
                    RotateInFlight (Array. IndexOf (jointsSaved, joint));
                }
                if ( jointsSaved. All (j => j. targetAngle == j. currentAngle) )
                {
                    Debug. Log ("机械臂运动结束");
                    startMoving = false;
                }
            }
        }

        protected virtual void SetTragetPoint ()
        {
            //选择目标点。
            if (targetPoint == null)
            {
                targetPoint = new Vector3 (0, 0, 0);
            }
            if ( armType == ArmType.Sampling )
            {
                SetTargetWithSampling ();
            }
            if ( armType == ArmType.Grasping)
            {
                SetTargetWithGrasping();
            }
            if ( armType == ArmType.Crawling )
            {
                SetTargetWithGrasping ();
            }
            if ( armType == ArmType.Connecting)
            {
                SetTargetWithGrasping ();
            }
        }

        private void SetTargetWithGrasping ()
        {
            //其他类型机械臂获取目标点
            return;
        }

        private void SetTargetWithSampling ()
        {
            if ( !isWaitForTargetPoint )
                return;

            if ( Input. GetMouseButtonDown (0) ) // 左键点击
            {
                Ray ray = FlightGlobals. fetch. mainCameraRef. ScreenPointToRay (Input. mousePosition);
                RaycastHit hit;

                // 创建一个只包含地形层的层掩码
                int terrainLayer = LayerMask. NameToLayer ("LocalScenery");
                if ( terrainLayer < 0 )
                    terrainLayer = 15;
                int terrainMask = 1 << terrainLayer;

                if ( !Physics. Raycast (ray, out hit, Mathf. Infinity, terrainMask) )
                {
                    ScreenMessages. PostScreenMessage ("未选中有效月面点，请重新点击地形", 3f, ScreenMessageStyle. UPPER_CENTER);
                    return;
                }

                // 获取点击点的世界坐标
                Vector3 hitPoint = hit. point;
                Vector3  movePoint = hitPoint + hit.normal.normalized * 0.1f;
                //=======根据语义，这里应该调用机械臂的 IK 计算函数，请帮我实现这里的代码=======
                float[] targetAngle = new float[jointsSaved. Length];
                for ( int i = 0 ; i < jointsSaved. Length ; i++ ) // 默认保持当前角度，第4关节不动
                    targetAngle[i] = jointsSaved[i]. currentAngle;

                if ( SolveIKToTarget (movePoint,hit.normal, out float[] solvedAngles) )
                {
                    targetAngle = solvedAngles;
                    isWaitForTargetPoint = false;
                    ScreenMessages. PostScreenMessage ("目标点已设置", 3f, ScreenMessageStyle. UPPER_CENTER);
                }
                else
                {
                    targetAngle = solvedAngles;
                    isWaitForTargetPoint = false;
                    ScreenMessages. PostScreenMessage ("目标点超出可达范围，已移动到最近位置", 4f, ScreenMessageStyle. UPPER_CENTER);
                }

                for ( int i = 0 ; i < jointsSaved. Length ; i++ )
                {
                    jointsSaved[i]. targetAngle = targetAngle[i];
                }
                startMoving = true;
            }
        }

        /// 
        /// 数值逆运动学求解：前3个关节使末端执行器到达目标点；
        /// 收敛后再解算第4关节，使末端执行器参考轴与月面平行。
        /// 第4个关节不参与位置计算。全程使用关节自身的旋转轴与父级坐标系。
        /// </summary>
        /// <param name="targetWorldPos">目标点世界坐标</param>
        /// <param name="targetNormal">目标点的月面法线（射线命中点的法线）</param>
        /// <param name="targetAngles">输出：各关节目标角度（度）</param>
        /// 是否成功收敛（末端距目标点 1cm 以内）</returns>
        protected bool SolveIKToTarget (Vector3 targetWorldPos, Vector3 targetNormal, out float[] targetAngles)
        {
            targetAngles = null;
            if ( jointTransforms == null || jointsSaved == null || jointsSaved. Length < 2 )
                return false;

            int activeCount = endEffectorIndex; // 参与位置解算的关节（0 ~ activeCount-1），第4关节不参与
            const int maxIter = 40;
            const float tolerance = 0.0005f; // 0.5mm 收敛阈值

            // 以当前实际角度作为迭代初值；第4个关节保持不变
            float[] angles = new float[jointsSaved. Length];
            for ( int i = 0 ; i < jointsSaved. Length ; i++ )
                angles[i] = jointsSaved[i]. currentAngle;

            // 备份当前姿态，迭代结束后恢复（迭代只用于计算，不产生视觉跳变）
            Quaternion[] backup = new Quaternion[jointsSaved. Length];
            for ( int i = 0 ; i < jointsSaved. Length ; i++ )
                backup[i] = jointTransforms[i]. localRotation;

            // ===== 位置迭代：仅前3个关节，第4个关节保持 currentAngle 不动 =====
            for ( int iter = 0 ; iter < maxIter ; iter++ )
            {
                for ( int i = activeCount - 1 ; i >= 0 ; i-- )
                {
                    Vector3 endPos = jointTransforms[endEffectorIndex]. position;
                    if ( ( endPos - targetWorldPos ). sqrMagnitude < tolerance * tolerance )
                        break;

                    Vector3 jointPos = jointTransforms[i]. position;

                    // 关节在世界中的旋转轴 = 父级坐标系旋转 * 关节自身轴向
                    Vector3 axisWorld = jointTransforms[i]. parent != null
                        ? jointTransforms[i]. parent. rotation * jointsSaved[i]. axis
                        : jointsSaved[i]. axis;

                    // 投影到垂直于旋转轴的平面，求需要补偿的角度
                    Vector3 toEnd = Vector3. ProjectOnPlane (endPos - jointPos, axisWorld);
                    Vector3 toTarget = Vector3. ProjectOnPlane (targetWorldPos - jointPos, axisWorld);
                    if ( toEnd. sqrMagnitude < 1e-8f || toTarget. sqrMagnitude < 1e-8f )
                        continue;

                    float delta = Vector3. SignedAngle (toEnd, toTarget, axisWorld);

                    float newAngle = Mathf. Clamp (angles[i] + delta,
                        jointsSaved[i]. minMaxRotationAngle. x,
                        jointsSaved[i]. minMaxRotationAngle. y);

                    if ( Mathf. Abs (newAngle - angles[i]) < 1e-4f )
                        continue;

                    angles[i] = newAngle;
                    // 立即应用，使子级（含末端）position 在同一帧内更新
                    jointTransforms[i]. localRotation = Quaternion. AngleAxis (angles[i], jointsSaved[i]. axis);
                }

                if ( ( jointTransforms[endEffectorIndex]. position - targetWorldPos ). sqrMagnitude
                     < tolerance * tolerance )
                    break;
            }

            float posError = Vector3. Distance (jointTransforms[endEffectorIndex]. position, targetWorldPos);

            // ===== 第4关节姿态解算：让末端执行器参考轴与月面平行 =====
            int i4 = endEffectorIndex;
            Vector3 axis4World = jointTransforms[i4]. parent != null
                ? jointTransforms[i4]. parent. rotation * jointsSaved[i4]. axis
                : jointsSaved[i4]. axis;

            // 当前参考轴的世界方向（此时 J4 仍为 currentAngle）
            Vector3 refWorld = jointTransforms[i4]. rotation * effectorAxisLocal;

            // 目标方向：参考轴在“垂直于月面法线的平面”上的投影，即离当前最近的水平姿态
            Vector3 planeNormal = targetNormal. normalized;
            Vector3 targetDir = Vector3. ProjectOnPlane (refWorld, planeNormal);

            Vector3 refOnAxisPlane = Vector3. ProjectOnPlane (refWorld, axis4World);
            Vector3 tgtOnAxisPlane = Vector3. ProjectOnPlane (targetDir, axis4World);

            // 参考轴与旋转轴接近平行时投影退化，跳过姿态解算
            if ( refOnAxisPlane. sqrMagnitude > 1e-8f && tgtOnAxisPlane. sqrMagnitude > 1e-8f )
            {
                float delta4 = Vector3. SignedAngle (refOnAxisPlane, tgtOnAxisPlane, axis4World);
                float newAngle4 = Mathf. Clamp (angles[i4] + delta4,
                    jointsSaved[i4]. minMaxRotationAngle. x,
                    jointsSaved[i4]. minMaxRotationAngle. y);
                angles[i4] = newAngle4;
                jointTransforms[i4]. localRotation = Quaternion. AngleAxis (angles[i4], jointsSaved[i4]. axis);
            }

            // 恢复真实姿态
            for ( int i = 0 ; i < jointsSaved. Length ; i++ )
                jointTransforms[i]. localRotation = backup[i];

            targetAngles = angles;
            Debug. Log ($"[ModuleManipulator] IK 解算完成，末端残余误差 {posError * 1000f:F1} mm");
            return posError < 0.01f;
        }

        protected virtual void ResetLaunchPosition ()
        {
            // 先关闭 UI，再执行逻辑
            if ( isControlPanelShow )
            {
                isControlPanelShow = false;
                Fields["isControlPanelShow"]. SetValue (false, this);
                if ( dialog != null )
                {
                    dialog. Dismiss ();
                    dialog = null;
                }
            }

            if ( isAutoControl )
            {
                isAutoControl = false;
                Fields["isAutoControl"]. SetValue (false, this);
            }
                for ( int i = 0 ; i < jointsSaved. Length ; i++ )
            {
                // 这里使用的是每个关节各自保存的 startAngle
                jointsSaved[i]. targetAngle = jointsSaved[i]. startAngle;
            }
            startMoving = true;
        }
        protected virtual void RotateInFlight (int index)
        {
            if ( jointsSaved[index].targetAngle == jointsSaved[index].currentAngle ) 
            {
                return;
            }
            float angle = Mathf. MoveTowards (jointsSaved[index].currentAngle, jointsSaved[index].targetAngle, Time. deltaTime * jointsSaved[index].rotaSpeed);
            part. gameObject. GetChild (jointsSaved[index]. jointTransformName). transform. localRotation = Quaternion. AngleAxis (angle, jointsSaved[index]. axis);
            jointsSaved[index]. currentAngle = angle;
        }
        protected virtual void RotateInEditor(int index)
        {
            if ( index < 0 || index >= jointsSaved. Length )
            {
                CASNFPLogger. Instance. LogError ($"无效的关节索引: {index}");
                return;
            }

            SaveJointInfo joint = jointsSaved[index];
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
        protected virtual void StartAutoControl ()
        {
            Debug. Log ("机械臂开始自动控制");
            startMoving = false;
            for ( int i = 0 ; i < jointsSaved. Length ; i++ ) //重置目标角度为当前角度。
            {
                jointsSaved[i]. targetAngle = jointsSaved[i]. currentAngle;
            }
            isWaitForTargetPoint = true;
            ScreenMessages. PostScreenMessage ("开始选择目标点", 5f, ScreenMessageStyle. UPPER_CENTER);
            //获取目标点
            //if ( targetPoint != Vector3. zero )
            //{
            //    float[] targetAngle = CalTargetAngle (targetPoint);
            //    for ( int i = 0 ; i < jointsSaved. Length ; i++ )
            //    {
            //        jointsSaved[i]. targetAngle = targetAngle[i];
            //    }
            //    startMoving = true;
            //}
        }
        #region 构建控制面板及各项功能
         private void OnPartActionUIShown(UIPartActionWindow data0, Part data1)
{
    if (data1 != part) return;
    if (HighLogic.LoadedSceneIsFlight)
    {
        toggleLock = Fields["isAutoControl"]?.uiControlFlight as UI_Toggle;
        toggle = Fields["isControlPanelShow"]?.uiControlFlight as UI_Toggle;
    }
    else if (HighLogic.LoadedSceneIsEditor)
    {
        toggleLock = Fields["isAutoControl"]?.uiControlEditor as UI_Toggle;
        toggle = Fields["isControlPanelShow"]?.uiControlEditor as UI_Toggle;
    }

    if (toggleLock != null)
    {
        toggleLock.onFieldChanged -= OnLockFieldChanged;
        toggleLock.onFieldChanged += OnLockFieldChanged;
    }
    if (toggle != null)
    {
        toggle.onFieldChanged -= OnFieldChanged;
        toggle.onFieldChanged += OnFieldChanged;
    }
}


        private void OnPartActionUIDismiss (Part data)
        {
            if ( data != part )
                return;

            // 如果控制面板是打开的，关闭它并销毁 Dialog
            if ( isControlPanelShow )
            {
                isControlPanelShow = false;
                // 显式更新字段以同步 UI 状态
                Fields["isControlPanelShow"]. SetValue (false, this);
            }

            if ( dialog != null )
            {
                try
                {
                    dialog. Dismiss ();
                }
                catch ( System. Exception e )
                {
                    Debug. LogWarning ($"[ModuleManipulator] Error dismissing dialog on PAW dismiss: {e. Message}");
                }
                dialog = null;
            }

            // 清除事件绑定，防止后续回调
            if ( toggleLock != null )
                toggleLock. onFieldChanged = null;
            if ( toggle != null )
                toggle. onFieldChanged = null;
        }


        private void OnLockFieldChanged (BaseField arg1, object arg2)
        {
            if ( isAutoControl )
            {
                if ( isControlPanelShow)
                {
                    Fields["isControlPanelShow"].SetValue (!isControlPanelShow,this);
                    if ( dialog != null )
                    {
                        dialog. Dismiss ();
                        dialog = null;
                    }
                }
                StartAutoControl ();
            }
            else
            {
                if ( !isControlPanelShow )
                {
                    Fields["isControlPanelShow"]. SetValue (!isControlPanelShow, this);
                    dialog = ShowDialog ();
                }
            }
        }
        private void OnFieldChanged (BaseField arg1, object arg2)
        {
            if ( isControlPanelShow )
            {
                if (!isAutoControl )
                {
                    dialog = ShowDialog ();
                }else 
                {
                    isControlPanelShow = !isControlPanelShow;
                    ScreenMessages. PostScreenMessage ("机械臂启动自动控制，手动控制模式关闭", 3f, ScreenMessageStyle. UPPER_CENTER);
                }
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
                                RotateInEditor (currentIndex - 1);
                            }
                            if ( HighLogic. LoadedSceneIsFlight && !isAutoControl)
                                startMoving = true;
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
                // 解析初始偏移量
                float offsetAngle = float. Parse (configNode. GetValue ("offsetAngle"));
                Vector2 minMax = ConfigNode. ParseVector2 (configNode. GetValue ("minMaxRotationAngle"));

                //  clamp 初始角度
                float angle = Mathf. Clamp (offsetAngle, minMax. x, minMax. y);
                SaveJointInfo jointInfo = new SaveJointInfo ()
                {
                    name = configNode. GetValue ("jointPartName"),
                    jointTransformName = configNode. GetValue ("jointTransformName"),
                    axis = ChangeToV3 (configNode. GetValue ("rotationAxis")),
                    minMaxRotationAngle = minMax,
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

        public bool IsJointUnlocked ()
        {
            return isJointUnlocked;
        }
    }
}
