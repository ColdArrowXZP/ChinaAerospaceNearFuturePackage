using System;
using System.Collections;
using System. Linq;
using System. Diagnostics. Eventing. Reader;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 动态机械臂模块：挂在机械臂基座Part上，运行时自动生成独立物理的大臂/小臂子Part
/// </summary>

namespace CASNFPParts.ArmParts
{
    public class ModuleSampleArm : PartModule
    {
        [KSPField] public string upperArmPartName = "MechArm_Sample_UpperArm"; // 大臂Part的cfg里的name
        [KSPField] public string lowerArmPartName = "MechArm_Sample_LowerArm"; // 小臂Part的cfg里的name

        // 大臂相对于基座锚点的位置偏移（本地坐标）
        [KSPField] public Vector3 upperArmOffset = new Vector3(0, 0.5f, 0);
        // 大臂初始旋转（本地欧拉角）
        [KSPField] public Vector3 upperArmRotation = Vector3.zero;
        // 大臂关节旋转范围（绕关节X轴，单位：度）
        [KSPField] public Vector2 upperAngleLimit = new Vector2(-90f, 90f);
        // 大臂关节电机驱动力
        [KSPField] public float upperMotorForce = 10f;

        // 小臂相对于大臂锚点的位置偏移
        [KSPField] public Vector3 lowerArmOffset = new Vector3(0, 1.2f, 0);
        [KSPField] public Vector3 lowerArmRotation = Vector3.zero;
        [KSPField] public Vector2 lowerAngleLimit = new Vector2(-120f, 0f);
        [KSPField] public float lowerMotorForce = 8f;

        // 关节断裂力/扭矩，设为Mathf.Infinity表示不会断
        [KSPField] public float jointBreakForce = Mathf.Infinity;
        [KSPField] public float jointBreakTorque = Mathf.Infinity;

        // ==============================================
        // 运行时私有变量
        // ==============================================
        private Part upperArmPart; // 动态生成的大臂Part实例
        private Part lowerArmPart; // 动态生成的小臂Part实例
        private ConfigurableJoint upperJoint; // 基座-大臂关节
        private ConfigurableJoint lowerJoint; // 大臂-小臂关节
        private Transform upperArmAnchor; // 基座上的大臂关节锚点
        private Transform lowerArmAnchor; // 大臂上的小臂关节锚点
        private bool isInitialized = false; // 初始化标记，防止重复生成

        // 存档持久化用：存储子Part的唯一ID，读档时恢复
        [KSPField(isPersistant = true)] private uint upperArmFlightId = 0;
        [KSPField(isPersistant = true)] private uint lowerArmFlightId = 0;

        // 机械臂控制目标角度
        private float upperTargetAngle = 0f;
        private float lowerTargetAngle = 0f;

        // ==============================================
        // 生命周期方法
        // ==============================================
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            // 仅在飞行场景执行动态生成逻辑，编辑器场景保留预览模型
            if (!HighLogic.LoadedSceneIsFlight) return;

            // 找到主Part上预设的锚点
            upperArmAnchor = part.gameObject.GetChild("node1")?.transform;
            if ( upperArmAnchor == null )
            {
                Debug. LogWarning ("[机械臂] 找不到大臂锚点：node1"); 
                upperArmAnchor = part.gameObject.transform; // 找不到就用根节点兜底
            }
            

            // 隐藏主Part自带的大臂/小臂预览模型（避免和动态生成的模型重叠）
            Transform previewUpper = part. gameObject. GetChild ("node2")?.transform;
            Transform previewLower = part.gameObject.GetChild("node3")?.transform;
            if (previewUpper != null) previewUpper.gameObject.SetActive(false);
            if (previewLower != null) previewLower.gameObject.SetActive(false);

            // 尝试从现有Vessel中恢复已生成的子Part（读档场景）
            bool restoredSuccess = RestoreSubPartsFromVessel();

            if (!restoredSuccess)
            {
                // 第一次加载，动态生成子Part
                try
                {
                    // 先挂大臂（基座→大臂）
                    upperArmPart = CreateSubPart(upperArmPartName, part, upperArmOffset, Quaternion.Euler(upperArmRotation));
                    if (upperArmPart == null)
                    {
                        Debug.LogError($"[机械臂] 找不到大臂Part配置：{upperArmPartName}");
                        return;
                    }
                    upperArmFlightId = upperArmPart.flightID; // 存ID用于存档

                    // 找到大臂上的小臂锚点
                    lowerArmAnchor = part. transform. Find ("node3"); //upperArmPart.transform.Find("LowerArmAnchor");
                    if (lowerArmAnchor == null) lowerArmAnchor = upperArmPart.transform;

                    // 再挂小臂（大臂→小臂）
                    lowerArmPart = CreateSubPart(lowerArmPartName, upperArmPart, lowerArmOffset, Quaternion.Euler(lowerArmRotation));
                    if (lowerArmPart == null)
                    {
                        Debug.LogError($"[机械臂] 找不到小臂Part配置：{lowerArmPartName}");
                        return;
                    }
                    lowerArmFlightId = lowerArmPart.flightID;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[机械臂] 生成子Part失败：{e.Message}\n{e.StackTrace}");
                    return;
                }
            }
            //处理Part销毁时的逻辑
            part.OnJustAboutToBeDestroyed += OnPartDestroy;
            Debug. Log ("[机械臂] 动态生成子Part完成，等待物理初始化...");
            // 等待物理初始化完成后再创建关节（避免Rigidbody未生成导致报错）
            StartCoroutine (WaitAndInitPhysics());
        }

        /// <summary>
        /// 固定物理帧更新：处理机械臂驱动和输入
        /// </summary>
        public void FixedUpdate()
        {
            if (!isInitialized || !vessel.isActiveVessel) return;

            // ===== 基础输入控制示例（可以替换成你自己的控制逻辑）=====
            // I/K控制大臂上下，O/L控制小臂上下
            if (Input.GetKey(KeyCode.I)) upperTargetAngle = Mathf.Clamp(upperTargetAngle + 1f, upperAngleLimit.x, upperAngleLimit.y);
            if (Input.GetKey(KeyCode.K)) upperTargetAngle = Mathf.Clamp(upperTargetAngle - 1f, upperAngleLimit.x, upperAngleLimit.y);
            if (Input.GetKey(KeyCode.O)) lowerTargetAngle = Mathf.Clamp(lowerTargetAngle + 1f, lowerAngleLimit.x, lowerAngleLimit.y);
            if (Input.GetKey(KeyCode.L)) lowerTargetAngle = Mathf.Clamp(lowerTargetAngle - 1f, lowerAngleLimit.x, lowerAngleLimit.y);

            // 更新关节驱动，让机械臂转到目标角度
            UpdateJointDrive(upperJoint, upperTargetAngle, upperMotorForce);
            UpdateJointDrive(lowerJoint, lowerTargetAngle, lowerMotorForce);
        }

        /// <summary>
        /// Part销毁时清理动态生成的子Part，避免残留垃圾
        /// </summary>
        public void OnPartDestroy()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (upperArmPart != null) Destroy(upperArmPart.gameObject);
                if (lowerArmPart != null) Destroy(lowerArmPart.gameObject);
            }
        }

        public void OnDestroy() 
        {
            part.OnJustAboutToBeDestroyed -= OnPartDestroy;
        }
        // ==============================================
        // 核心逻辑方法
        // ==============================================

        /// <summary>
        /// 动态创建子Part并挂载到父Part
        /// </summary>
        /// <param name="partName">子Part在cfg里的name</param>
        /// <param name="parentPart">父Part</param>
        /// <param name="localOffset">相对于父Part锚点的本地位置偏移</param>
        /// <param name="localRot">相对于父Part的本地旋转</param>
        /// 生成的Part实例，失败返回null</returns>
        private Part CreateSubPart(string partName, Part parentPart, Vector3 localOffset, Quaternion localRot)
        {
            partName = partName. Replace ("_", ".");

            // 1. 从KSP部件库找到子Part的配置
            AvailablePart availPart = null;
            foreach ( var item in PartLoader. Instance.loadedParts )
            {
                if ( item.name == partName )
                {
                    availPart = item;
                    break;
                }
            }
            if ( availPart == null )
            {
                Debug.LogError($"[机械臂] 未找到子Part配置：{partName}");
                return null;
            }
            

            // 2. 计算子Part的世界位置和旋转
            Vector3 worldPos = parentPart.transform.TransformPoint(localOffset);
            Debug.Log ($"[机械臂] 生成子Part {partName}，世界位置：{worldPos}");
            Quaternion worldRot = parentPart.transform.rotation * localRot;
            Debug. Log ($"[机械臂] 生成子Part {partName}，世界旋转：{worldRot. eulerAngles}");

            // 3. 实例化Part Prefab
            Part newPart = Instantiate (availPart. partPrefab, worldPos, worldRot);
            // 4. 基础属性赋值
            newPart. transform. parent = null;
            newPart. missionID = parentPart. missionID;
            newPart. flagURL = parentPart. flagURL;
            newPart. transform. localScale = parentPart. transform. lossyScale;
            newPart. flightID = newPart. flightID == 0 ? ( uint )UnityEngine. Random. Range (1, int. MaxValue) : newPart. flightID;
            // 5. 【关键修复】完全走KSP原生部件注册流程，不手动Add进parts列表
            newPart. InitializeModules ();
            if ( newPart. rb == null )
            {
                Debug. LogWarning ($"[机械臂] 子Part {partName} 的刚体是：null，尝试手动添加刚体");
                Rigidbody rb = newPart. gameObject. AddComponent<Rigidbody> ();
                rb. mass = newPart. mass;
            }
            parentPart. addChild (newPart);
            if ( !vessel.Parts.Contains(newPart) )
            {
                vessel. Parts. Add (newPart);
            }
            newPart.gameObject.SetActive (true);
            GameEvents. onVesselWasModified. Fire (parentPart. vessel);
            // 7. 启动所有子模块
            foreach ( PartModule module in newPart. Modules )
            {
                if ( !module. isEnabled )
                {
                    module. OnStart (PartModule.StartState.PreLaunch);
                }
            }

            Debug. Log ($"[机械臂] 部件初始化完成：{partName}");
            return newPart;
        }

        /// <summary>
        /// 等待物理帧初始化后，创建关节、开启碰撞
        /// </summary>
        private IEnumerator WaitAndInitPhysics()
        {
            Debug. Log ("[机械臂] 等待物理初始化...");
            int waitCount = 0;
            int maxWaitFrames = 10; // 最多等待10个物理帧，防止死等
            while (waitCount < maxWaitFrames)
            {
                yield return new WaitForFixedUpdate();
                waitCount++;

                if (upperArmPart?.Rigidbody != null && lowerArmPart?.Rigidbody != null)
                {
                    break;
                }
            }

            if (upperArmPart?.Rigidbody == null || lowerArmPart?.Rigidbody == null)
            {
                Debug.LogError("[机械臂] 等待Rigidbody超时，物理初始化失败");
                yield break;
            }
            Debug. Log ("[机械臂] Rigidbody初始化完成，开始创建关节和开启碰撞");
            // 1. 创建连接关节
            // 大臂关节传入大臂的电机力
            upperJoint = CreateRotationalJoint(part, upperArmPart, upperAngleLimit, jointBreakForce, jointBreakTorque, upperMotorForce);
            // 小臂关节传入小臂的电机力
            lowerJoint = CreateRotationalJoint(upperArmPart, lowerArmPart, lowerAngleLimit, jointBreakForce, jointBreakTorque, lowerMotorForce);

            Debug.Log ("[机械臂] 关节创建完成");
            // 2. 开启机械臂内部Part之间的碰撞（KSP默认关闭同飞船Part碰撞，必须手动开启）
            EnableCollisionBetweenParts (part, upperArmPart);
            EnableCollisionBetweenParts(upperArmPart, lowerArmPart);
            EnableCollisionBetweenParts(part, lowerArmPart);
            isInitialized = true;
            Debug.Log("[机械臂] 物理初始化完成");
        }

        /// <summary>
        /// 创建旋转铰链关节（机械臂专用，仅允许绕X轴旋转，其他轴全部锁死）
        /// </summary>
        private ConfigurableJoint CreateRotationalJoint(Part parent, Part child, Vector2 angleLimit, float breakForce, float breakTorque,float motorForce)
        {
            Rigidbody parentRb = parent.Rigidbody;
            Rigidbody childRb = child.Rigidbody;
            if (parentRb == null || childRb == null) return null;

            ConfigurableJoint joint = child.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = parentRb;

            // ----- 基础关节设置 -----
            joint.anchor = child.transform.InverseTransformPoint(parent.transform.position);
            joint.connectedAnchor = Vector3.zero; // 连接点在父Part原点
            joint.axis = Vector3.right; // 旋转轴为X轴（右方向），可根据你的模型修改为forward/up
            joint.secondaryAxis = Vector3.up;

            // 锁死所有平移自由度（机械臂关节不能移动，只能转动）
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            // 锁死其他旋转自由度，仅X轴可旋转
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Limited; // X轴受角度限制

            // ----- 角度限制设置 -----
            SoftJointLimit lowLimit = new SoftJointLimit();
            lowLimit.limit = angleLimit.x;
            lowLimit.bounciness = 0.2f; // 撞到限位的反弹力
            lowLimit.contactDistance = 1f;
            joint.lowAngularXLimit = lowLimit;

            SoftJointLimit highLimit = new SoftJointLimit();
            highLimit.limit = angleLimit.y;
            highLimit.bounciness = 0.2f;
            highLimit.contactDistance = 1f;
            joint.highAngularXLimit = highLimit;

            // ----- 稳定性设置（防止物理抖动错位）-----
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.01f;
            joint.projectionAngle = 1f;
            joint.enablePreprocessing = false; // 关闭预处理减少关节断裂bug

            // ----- 断裂力设置 -----
            joint.breakForce = breakForce;
            joint.breakTorque = breakTorque;

            // ----- 初始化驱动参数 -----
            JointDrive drive = new JointDrive();
            drive.positionSpring = 200f;
            drive.positionDamper = 20f;
            drive.maximumForce = motorForce;
            joint.angularXDrive = drive;
            joint.targetAngularVelocity = Vector3.zero;

            return joint;
        }

        /// <summary>
        /// 开启两个Part之间的所有碰撞器碰撞
        /// </summary>
        private void EnableCollisionBetweenParts(Part a, Part b)
        {
            if (a == null || b == null) return;

            Collider[] aCols = a.GetComponentsInChildren<Collider>(true);
            Collider[] bCols = b.GetComponentsInChildren<Collider>(true);

            foreach (Collider colA in aCols)
            {
                if (colA == null || !colA.gameObject.activeInHierarchy) continue;
                foreach (Collider colB in bCols)
                {
                    if (colB == null || !colB.gameObject.activeInHierarchy) continue;
                    if (colA != colB)
                    {
                        Physics.IgnoreCollision(colA, colB, false);
                    }
                }
            }
        }


        /// <summary>
        /// 更新关节电机驱动，转到目标角度
        /// </summary>
        private void UpdateJointDrive(ConfigurableJoint joint, float targetAngle, float motorForce)
        {
            if (joint == null) return;

            // 设置目标旋转（绕X轴旋转）
            joint.targetRotation = Quaternion.Euler(targetAngle, 0, 0);

            // 更新电机驱动力
            JointDrive drive = joint.angularXDrive;
            drive.maximumForce = motorForce;
            joint.angularXDrive = drive;
        }

        // ==============================================
        // 存档持久化逻辑
        // ==============================================

        /// <summary>
        /// 读档时从飞船现有Part中恢复之前生成的大臂/小臂
        /// </summary>
        private bool RestoreSubPartsFromVessel()
        {
            if (upperArmFlightId == 0 || lowerArmFlightId == 0) return false;

            // 遍历飞船所有Part，通过之前存的flightID找到子Part
            foreach (Part p in vessel.parts)
            {
                if (p.flightID == upperArmFlightId) upperArmPart = p;
                if (p.flightID == lowerArmFlightId) lowerArmPart = p;
            }

            if (upperArmPart != null && lowerArmPart != null)
            {
                // 找到大臂上的小臂锚点
                lowerArmAnchor = upperArmPart.transform.Find("LowerArmAnchor");
                if (lowerArmAnchor == null) lowerArmAnchor = upperArmPart.transform;

                // 隐藏预览模型
                Transform previewUpper = part.transform.Find("UpperArmModel");
                Transform previewLower = part.transform.Find("LowerArmModel");
                if (previewUpper != null) previewUpper.gameObject.SetActive(false);
                if (previewLower != null) previewLower.gameObject.SetActive(false);

                Debug.Log("[机械臂] 从存档恢复子Part成功");
                return true;
            }

            // 恢复失败，重置ID
            upperArmFlightId = 0;
            lowerArmFlightId = 0;
            return false;
        }
    }
}


//cfg中部分代码调用参考
//MODULE
//{
//    name = ModuleSampleArm
//    upperArmPartName = MechArm_UpperArm
//    lowerArmPartName = MechArm_LowerArm
//    upperArmOffset = 0，0.5，0
//    upperAngleLimit = -90，90
//    upperMotorForce = 15
//    lowerArmOffset = 0，1.2，0
//    lowerAngleLimit = -120，0
//    lowerMotorForce = 10
//    jointBreakForce = Infinity
//    jointBreakTorque = Infinity
//}
