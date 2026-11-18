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
        protected bool startMoving;
        #region 着色渲染参数
        private LineRenderer _lineRenderer;
        private float lineWidth = 0.1f;
        private int segments = 128;
        private Color boundaryColor = Color. green;
        private Color fillColor = new Color (0f, 1f, 0f, 0.2f);
        #endregion
        #region 缓存边界区域坐标点
        private readonly List<Vector3> _outerBoundary = new List<Vector3> ();//外边界
        private readonly List<Vector3> _innerBoundary = new List<Vector3> ();//内边界
        private readonly List<Vector3> _allBoundaryPoints = new List<Vector3> ();//所有边界点
        public float groundDistance = 0f;
        #endregion
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
            if ( HighLogic.LoadedSceneIsFlight )
            {
                InitializeComponents ();
            }
            
            GameEvents. onPartActionUIDismiss. Add (OnPartActionUIDismiss);
            GameEvents. onPartActionUIShown. Add (OnPartActionUIShown);
            for ( int i = 0 ; i < jointsSaved. Length ; i++ )//设置关节初始位置。
            {
                part. gameObject. GetChild (jointsSaved[i]. jointTransformName). transform. localRotation = Quaternion. AngleAxis (jointsSaved[i]. currentAngle, jointsSaved[i]. axis);
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

            // 4. 清理渲染组件引用（如果它们是被动态添加的）
            // 注意：如果这些组件是 Part 的一部分，不要 Destroy 它们，只需置空引用
            if ( _lineRenderer != null )
            {
                _lineRenderer = null;
            }
             
        }

        public override void OnUpdate ()
        {
            base. OnUpdate ();
            if ( !IsJointUnlocked () )
            {
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
            _allBoundaryPoints. Clear ();
            UpdateVisuals ();
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

            UpdateIntersection ();
            //Vector3 targetPoint = GetTargetPoint ();//获取目标点
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
        #region 与自动取样控制相关方法
        public  void UpdateIntersection ()
        {

            _outerBoundary. Clear ();
            _innerBoundary. Clear ();
            _allBoundaryPoints. Clear ();
            Vector3 basePos = part. gameObject. GetChild (jointsSaved[0]. jointTransformName). transform. position;  // 机械臂底座在世界坐标系中的位置          
            Vector3 zAxis = transform.TransformDirection (jointsSaved[0].axis).normalized; // 底座旋转轴（局部Z）
            float maxReach =Vector3.Distance(part.gameObject.GetChild(jointsSaved[1].jointTransformName).transform.position, part.gameObject.GetChild(jointsSaved[2].jointTransformName).transform.position)+ Vector3. Distance (part. gameObject. GetChild (jointsSaved[2]. jointTransformName). transform. position, part. gameObject. GetChild (jointsSaved[3]. jointTransformName). transform. position); // 两臂长度之和
            float L = maxReach / 2;
            //检测离地高度
            // 目标天体实例
            CelestialBody body = part.vessel.mainBody;
            
            if ( body == null )
            {
                UpdateVisuals ();
                return;
            }
            // 仅在地表/低空场景执行地形计算，轨道场景直接隐藏边界
            if ( !HighLogic. LoadedSceneIsFlight || part. vessel. altitude > body. Radius + 500 )
            {
                _lineRenderer. positionCount = 0;
                return;
            }
            // 空间中任意点的世界坐标
            Vector3d worldPoint = basePos;

            // 地形所在层级：KSP1 中 PQS 地形的 LocalScenery 层索引为 15
            int terrainLayer = LayerMask. NameToLayer ("LocalScenery");
            if ( terrainLayer < 0 )
                terrainLayer = 15;   // 兜底，防止个别环境取不到名字
            int terrainMask = 1 << terrainLayer;

            // 从地心指向 worldPoint 的单位径向向量（当地天顶方向）
            Vector3d radialUp = ( worldPoint - body. position ). normalized;

            // 判断 worldPoint 是否在地表以下：比较它与该处地形的高度
            body. GetLatLonAlt (worldPoint, out double lat, out double lon, out double altAboveSphere);
            // allowNegative=true，海洋/凹陷处会返回负值，判断更准确
            double terrainAlt = body. TerrainAltitude (lat, lon, true);
            bool underGround = altAboveSphere < terrainAlt;

            // 射线起点：地表之上直接用 worldPoint；地表之下沿径向抬到地形之外
            Vector3d rayOrigin;
            if ( !underGround )
            {
                rayOrigin = worldPoint;
            }
            else
            {
                // 抬升高度必须超过该天体地形的最高点，保守取 1.1 倍基准半径
                // 更精确可换成 body.Radius + 该天体最大地形高度
                double safeRadius = body. Radius * 1.1;
                rayOrigin = body. position + radialUp * safeRadius;  // 位于地形上空的外部锚点
            }

            // 统一从外向内朝地心打，起点一定在碰撞体外部，能稳定命中地表
            Vector3d rayDir = -radialUp;
            float maxDistance = ( float )( body. Radius * 1.5 );

            Vector3d worldTerrainNormal = Vector3d. zero;
            Vector3d terrainHitPoint= Vector3d. zero;
            RaycastHit hitResult;
            if ( Physics. Raycast (rayOrigin, rayDir, out hitResult, maxDistance, terrainMask, QueryTriggerInteraction. Ignore) )
            {
                Debug. Log ("朝地心发射射线，检测到地表");

                Vector3d prevNormal = ( Vector3d )hitResult. normal;
                Vector3d prevPoint = ( Vector3d )hitResult. point;

                // 第二次：沿地形法线方向（朝地形内部）从同一外部起点再打
                if ( Physics. Raycast (rayOrigin, -prevNormal, out hitResult, maxDistance, terrainMask, QueryTriggerInteraction. Ignore) )
                {
                    Debug. Log ("沿法线方向发射射线，检测到地表");

                    worldTerrainNormal = ( Vector3d )hitResult. normal;
                    terrainHitPoint = ( Vector3d )hitResult. point;

                    // 法线校验：点积≈1 且位置基本不动即收敛，否则补射最多 2 次
                    for ( int i = 0 ; i < 2 ; i++ )
                    {
                        float dot = ( float )Vector3d. Dot (prevNormal, worldTerrainNormal);
                        if ( dot > 0.99999f && ( terrainHitPoint - prevPoint ). magnitude < 0.05 )
                            break;

                        prevNormal = worldTerrainNormal;
                        prevPoint = terrainHitPoint;

                        if ( !Physics. Raycast (rayOrigin, -prevNormal, out hitResult, maxDistance, terrainMask, QueryTriggerInteraction. Ignore) )
                            break; // 极端陡坡下沿法线擦过，保留上一次结果

                        worldTerrainNormal = ( Vector3d )hitResult. normal;
                        terrainHitPoint = ( Vector3d )hitResult. point;
                    }

                    // 强制法线朝外：左手系下顶点插值法线偶发反向时的保险
                    Vector3d outward = ( terrainHitPoint - body. position ). normalized;
                    if ( Vector3d. Dot (worldTerrainNormal, outward) < 0.0 )
                        worldTerrainNormal = -worldTerrainNormal;
                }
                else
                {
                    Debug. Log ("沿法线方向发射射线，未检测到地表");
                }
            }
            else
            {
                Debug. Log ("朝地心发射射线，未检测到地表");
            }
            //=====================转换为平面==============================
            Vector3 groundNormal = worldTerrainNormal. normalized; // 切平面法线
            Vector3 groundPoint =  terrainHitPoint;                  // 切平面上一点

            // 底座到切平面的有符号距离（底座在平面上方为正）
            float heightAboveGround = Vector3. Dot (basePos - groundPoint, groundNormal);
            Debug. Log ($"[MechArm] Height: {heightAboveGround}, MaxReach: {maxReach}, GroundDist: {groundDistance}");

            // 快速拒绝：底座离地表太远，完全无法到达
            if ( Mathf. Abs (heightAboveGround) > maxReach )
            {
                UpdateVisuals ();
                return;
            }

            // 子午面径向基准：任取一个与底座轴垂直的方向
            Vector3 sRef = Vector3. Cross (zAxis, groundNormal);
            if ( sRef. sqrMagnitude < 1e-8f )
                sRef = Vector3. Cross (zAxis, Vector3. forward);
            if ( sRef. sqrMagnitude < 1e-8f )
                sRef = Vector3. Cross (zAxis, Vector3. right);
            sRef. Normalize ();

            float angleStep = 2f * Mathf. PI / segments;

            for ( int i = 0 ; i <= segments ; i++ )
            {
                float phi = i * angleStep;
                Vector3 sDir = Quaternion. AngleAxis (phi * Mathf. Rad2Deg, zAxis) * sRef;
                sDir. Normalize ();

                // 切平面在子午面(s,t)下的直线：a*s + b*t + c = 0
                float a = Vector3. Dot (sDir, groundNormal);
                float b = Vector3. Dot (zAxis, groundNormal);
                float c = heightAboveGround;

                bool hasOuter = false;
                bool hasInner = false;
                Vector2 outerPoint = default;
                Vector2 innerPoint = default;

                // 1. 外边界：s² + t² = (2L)²，t ≥ 0
                if ( LineCircleIntersection (a, b, c, 0f, 0f, 2f * L, out var outerHits) )
                {
                    foreach ( var hit in outerHits )
                    {
                        if ( hit. x >= -1e-4f && hit. y >= -1e-4f )
                        {
                            hasOuter = true;
                            outerPoint = hit;
                            break;
                        }
                    }
                }

                // 2. 下边界：(s - L)² + t² = L²，s ≥ 0
                if ( LineCircleIntersection (a, b, c, L, 0f, L, out var innerHits) )
                {
                    foreach ( var hit in innerHits )
                    {
                        if ( hit. x >= -1e-4f )
                        {
                            if ( !hasInner || hit. x < innerPoint. x )
                            {
                                hasInner = true;
                                innerPoint = hit;
                            }
                            if ( !hasOuter || hit. x > outerPoint. x )
                            {
                                hasOuter = true;
                                outerPoint = hit;
                            }
                        }
                    }
                }

                // 3. 剔除假空洞：内圆交点落在半径 2L 的半球内部即为实心区域
                if ( hasInner )
                {
                    float si = innerPoint. x;
                    float ti = innerPoint. y;
                    if ( ti > 1e-4f && si * si + ti * ti < 4f * L * L - 1e-4f * L * L )
                        hasInner = false;
                }

                if ( hasOuter )
                    _outerBoundary. Add (basePos + outerPoint. x * sDir + outerPoint. y * zAxis);
                if ( hasInner )
                    _innerBoundary. Add (basePos + innerPoint. x * sDir + innerPoint. y * zAxis);
            }

            // 拼接闭合边界：外边界正向 + 内边界反向
            if ( _outerBoundary. Count > 1 )
                _allBoundaryPoints. AddRange (_outerBoundary);
            if ( _innerBoundary. Count > 1 )
                for ( int i = _innerBoundary. Count - 1 ; i >= 0 ; i-- )
                    _allBoundaryPoints. Add (_innerBoundary[i]);

            UpdateVisuals ();
        }
        /// <summary>
        /// 计算直线a*s + b*t + c = 0与圆(s-s0)² + (t-t0)² = r²的交点
        /// </summary>
        /// <returns>是否有实交点</returns>
        private bool LineCircleIntersection (float a, float b, float c, float s0, float t0, float r, out List<Vector2> hits)
        {
            hits = new List<Vector2> (2);

            // 处理直线垂直/水平特殊情况，避免除以零
            if ( Mathf. Abs (a) < 1e-6f && Mathf. Abs (b) < 1e-6f )
                return false; // 直线无效

            // 添加对 b 的零值检查
            if ( Mathf. Abs (b) < 1e-6f )
            {
                // 处理垂直线的情况
                float s = -c / a;
                float dt = r * r - ( s - s0 ) * ( s - s0 );
                if ( dt < -1e-6f )
                    return false;
                dt = Mathf. Max (dt, 0f);
                float sqrtDt = Mathf. Sqrt (dt);
                hits. Add (new Vector2 (s, t0 + sqrtDt));
                if ( sqrtDt > 1e-6f )
                    hits. Add (new Vector2 (s, t0 - sqrtDt));
                return true;
            }

            // 添加对 a 的零值检查
            if ( Mathf. Abs (a) < 1e-6f )
            {
                // 处理水平线的情况
                float t = -c / b;
                float dx = r * r - ( t - t0 ) * ( t - t0 );
                if ( dx < -1e-6f )
                    return false;
                dx = Mathf. Max (dx, 0f);
                float sqrtDx = Mathf. Sqrt (dx);
                hits. Add (new Vector2 (s0 + sqrtDx, t));
                if ( sqrtDx > 1e-6f )
                    hits. Add (new Vector2 (s0 - sqrtDx, t));
                return true;
            }

            // 一般情况：代入t = -(a*s + c)/b到圆方程，解一元二次方程
            float invB = 1f / b;
            float A = a * a + b * b;

            // 添加对 A 的零值检查
            if ( Mathf. Abs (A) < 1e-6f )
                return false;

            float B = 2f * ( a * c + a * b * t0 - b * b * s0 );
            float C = c * c + 2f * b * c * t0 + b * b * ( t0 * t0 + s0 * s0 - r * r );

            float discriminant = B * B - 4f * A * C;
            if ( discriminant < -1e-6f )
                return false;
            discriminant = Mathf. Max (discriminant, 0f);
            float sqrtD = Mathf. Sqrt (discriminant);
            float inv2A = 0.5f / A;

            float s1 = ( -B + sqrtD ) * inv2A;
            float t1 = -( a * s1 + c ) * invB;
            hits. Add (new Vector2 (s1, t1));

            if ( sqrtD > 1e-6f )
            {
                float s2 = ( -B - sqrtD ) * inv2A;
                float t2 = -( a * s2 + c ) * invB;
                hits. Add (new Vector2 (s2, t2));
            }

            return true;
        }

        /// <summary>
        /// 更新渲染组件
        /// </summary>
        private void UpdateVisuals ()
        {
            // 确保 lineWidth 不为零
            if ( lineWidth <= 0f )
                lineWidth = 0.1f;
            // 更新线边界
            if ( _allBoundaryPoints. Count < 2 )
            {
                if ( _lineRenderer != null )
                    _lineRenderer. positionCount = 0;
                Debug. Log ("[MechArm] No boundary points to render.");
                return;
            }

            // 渲染线框
            if ( _lineRenderer != null )
            {
                _lineRenderer. positionCount = _allBoundaryPoints. Count;
                _lineRenderer. SetPositions (_allBoundaryPoints. ToArray ());
                // 确保宽度不为0
                _lineRenderer. startWidth = lineWidth;
                _lineRenderer. endWidth = lineWidth;
            }
        }
        private void InitializeComponents ()
        {
            // 线渲染器设置
            _lineRenderer = part.gameObject.AddComponent<LineRenderer> ();
            _lineRenderer. shadowCastingMode = UnityEngine. Rendering. ShadowCastingMode. Off;
            _lineRenderer. receiveShadows = false;
            _lineRenderer. startWidth = lineWidth;
            _lineRenderer. endWidth = lineWidth;
            Shader lineShader = Shader. Find ("Sprites/Default");
            if ( lineShader == null )
            {
                CASNFPLogger. Instance. LogError ("无法找到 Sprites/Default Shader，线框渲染将失效");
                // 尝试备用 Shader
                lineShader = Shader. Find ("Legacy Shaders/Diffuse");
            }
            _lineRenderer. material = new Material (lineShader) { color = fillColor };
            _lineRenderer. startColor = boundaryColor;
            _lineRenderer. endColor = boundaryColor;
            _lineRenderer. loop = true;
            _lineRenderer. useWorldSpace = true;
            _lineRenderer. positionCount = 0;
        }
        #endregion
        #region 构建控制面板及各项功能
        private void OnPartActionUIShown (UIPartActionWindow data0, Part data1)
        {
            if ( data1 != part )
                return;

            if ( HighLogic. LoadedSceneIsFlight )
            {
                toggleLock = Fields["isAutoControl"]?.uiControlFlight as UI_Toggle;
                toggle = Fields["isControlPanelShow"]?.uiControlFlight as UI_Toggle;
            }
            else if ( HighLogic. LoadedSceneIsEditor )
            {
                toggleLock = Fields["isAutoControl"]?.uiControlEditor as UI_Toggle;
                toggle = Fields["isControlPanelShow"]?.uiControlEditor as UI_Toggle;
            }

            if ( toggleLock != null)toggleLock. onFieldChanged = OnLockFieldChanged;
            if( toggle != null )toggle. onFieldChanged = OnFieldChanged;
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
