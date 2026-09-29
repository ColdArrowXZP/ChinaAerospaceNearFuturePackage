using ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts;
using System. Collections. Generic;
using UnityEngine;

namespace CASNFPParts. ArmParts
{
    /// <summary>
    /// 机械臂PartModule基类，实现IMechanicalArm
    /// 取样臂、爬行吸附臂继承此类
    /// </summary>
    public class BaseMechanicalArmModule : PartModule, IMechanicalArm
    {
        [KSPField (isPersistant = true)]
        public bool isArmPowered;

        [KSPField (isPersistant = true)]
        public bool isArmActive;

        // 注意：该字段仅用于序列化，运行时使用 joints.Count
        [KSPField (isPersistant = true)]
        public int JointCount;

        [KSPField (isPersistant = false)]
        public string endEffectorTransformName;

        protected Transform endEffectorTransform;
        protected IList<ArmJointData> joints = new List<ArmJointData> ();

        // 缓存，减少每帧Transform查找开销
        protected bool transformsLoaded;

        // 接口需要的目标点、臂状态
        protected Vector3 _targetWorldPos;
        protected ArmState _armState = ArmState. IDLE;

        public Part ArmPart => part;
        public int jointCount => joints. Count;

        public bool IsArmPowered => isArmPowered;

        public bool IsArmActive => isArmActive;

        public Transform GetJointTransform (int jointIndex)
        {
            if ( jointIndex < 0 || jointIndex >= joints. Count )
                return null;
            return joints[jointIndex]. jointTransform;
        }

        public Vector3 GetJointCurrentRotation (int jointIndex)
        {
            if ( jointIndex < 0 || jointIndex >= joints. Count )
                return Vector3. zero;
            return joints[jointIndex]. jointTransform. localEulerAngles;
        }

        public void SetJointTargetRotation (int jointIndex, Vector3 targetEuler)
        {
            if ( jointIndex < 0 || jointIndex >= joints. Count )
                return;
            var joint = joints[jointIndex];
            // 钳位到关节角度限制
            targetEuler = new Vector3 (
                Mathf. Clamp (targetEuler. x, joint. minAngle. x, joint. maxAngle. x),
                Mathf. Clamp (targetEuler. y, joint. minAngle. y, joint. maxAngle. y),
                Mathf. Clamp (targetEuler. z, joint. minAngle. z, joint. maxAngle. z)
            );
            joint. targetEuler = targetEuler;
        }

        public Transform GetEndEffectorTransform ()
        {
            return endEffectorTransform;
        }

        public virtual void ActivateArm ()
        {
            if ( !isArmPowered )
                return;
            isArmActive = true;
            Events["ToggleArm"]. guiName = "关闭机械臂";
        }

        public virtual void DeactivateArm ()
        {
            isArmActive = false;
            Events["ToggleArm"]. guiName = "开启机械臂";
        }

        public void ArmUpdate ()
        {
            if ( !isArmActive || !isArmPowered )
                return;
            // 每帧平滑驱动关节旋转到目标角度
            foreach ( var j in joints )
            {
                Vector3 current = j. jointTransform. localEulerAngles;
                // 欧拉角环绕修正 (0~360环绕问题)
                Vector3 delta = new Vector3 (
                    Mathf. DeltaAngle (current. x, j. targetEuler. x),
                    Mathf. DeltaAngle (current. y, j. targetEuler. y),
                    Mathf. DeltaAngle (current. z, j. targetEuler. z)
                );
                Vector3 maxDelta = j. maxAngularVelocity * Time. deltaTime;
                Vector3 moveStep = new Vector3 (
                    Mathf. Clamp (delta. x, -maxDelta. x, maxDelta. x),
                    Mathf. Clamp (delta. y, -maxDelta. y, maxDelta. y),
                    Mathf. Clamp (delta. z, -maxDelta. z, maxDelta. z)
                );
                j. jointTransform. localEulerAngles += moveStep;
            }
        }

        public void ArmFixedUpdate ()
        {
            // 物理帧，子类可重写：耗电、物理检查、吸附力等
        }

        /// <summary>
        /// 加载部件配置，读取JOINT节点，缓存Transform
        /// </summary>
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            LoadJointConfig ();
            LoadTransformReferences ();

            // 同步字段
            JointCount = joints. Count;

            Events["ToggleArm"]. guiName = isArmActive ? "关闭机械臂" : "开启机械臂";
        }

        /// <summary>
        /// 从part.cfg读取关节配置
        /// </summary>
        protected virtual void LoadJointConfig ()
        {
            joints. Clear ();
            ConfigNode[] jointNodes = part. partInfo. partConfig. GetNodes ("JOINT");
            foreach ( var node in jointNodes )
            {
                ArmJointData jd = new ArmJointData ();
                jd. transformName = node. GetValue ("transformName");
                jd. minAngle = ConfigNode. ParseVector3 (node. GetValue ("minAngle"));
                jd. maxAngle = ConfigNode. ParseVector3 (node. GetValue ("maxAngle"));
                jd. maxAngularVelocity = ConfigNode. ParseVector3 (node. GetValue ("maxAngularVelocity"));
                jd. targetEuler = Vector3. zero;
                joints. Add (jd);
            }
        }

        /// <summary>
        /// 查找并缓存模型Transform引用
        /// </summary>
        protected virtual void LoadTransformReferences ()
        {
            if ( transformsLoaded )
                return;
            foreach ( var j in joints )
            {
                j. jointTransform = part. FindModelTransform (j. transformName);
            }
            endEffectorTransform = part. FindModelTransform (endEffectorTransformName);
            transformsLoaded = true;
        }

        public override void OnUpdate ()
        {
            base. OnUpdate ();
            ArmUpdate ();
        }

        public override void OnFixedUpdate ()
        {
            base. OnUpdate ();
            ArmFixedUpdate ();
        }

        /// <summary>
        /// 右键菜单事件：开关机械臂
        /// </summary>
        [KSPEvent (guiActive = true, guiName = "开启机械臂", active = true)]
        public void ToggleArm ()
        {
            if ( isArmActive )
                DeactivateArm ();
            else
                ActivateArm ();
        }

        public virtual void LoadArmState (ConfigNode node)
        {
            if ( node == null )
                return;
            isArmPowered = bool. Parse (node. GetValue ("IsArmPowered"));
            isArmActive = bool. Parse (node. GetValue ("IsArmActive"));

            ConfigNode[] savedJoints = node. GetNodes ("SAVED_JOINT");
            for ( int i = 0 ; i < savedJoints. Length && i < joints. Count ; i++ )
            {
                joints[i]. targetEuler = ConfigNode. ParseVector3 (savedJoints[i]. GetValue ("targetEuler"));
                // 【修复】加载存档时同步模型当前旋转，防止重启后模型姿态和target不一致
                joints[i]. jointTransform. localEulerAngles = joints[i]. targetEuler;
            }
        }

        public virtual void SaveArmState (ConfigNode node)
        {
            node. SetValue ("IsArmPowered", isArmPowered. ToString ());
            node. SetValue ("IsArmActive", isArmActive. ToString ());
            node. RemoveNodes ("SAVED_JOINT");
            for ( int i = 0 ; i < joints. Count ; i++ )
            {
                ConfigNode jNode = node. AddNode ("SAVED_JOINT");
                jNode. SetValue ("targetEuler", ConfigNode. WriteVector (joints[i]. targetEuler));
            }
        }

        public override void OnLoad (ConfigNode node)
        {
            base. OnLoad (node);
            LoadArmState (node);
        }

        public override void OnSave (ConfigNode node)
        {
            base. OnSave (node);
            SaveArmState (node);
        }
        public virtual void SetTarget (Vector3 worldPoint)
        {
            if ( !isArmPowered )
                return;
            _targetWorldPos = worldPoint;
            _armState = ArmState. MOVING;
        }

        public virtual void ResetArm ()
        {
            _armState = ArmState. IDLE;
            _targetWorldPos = Vector3. zero;
            foreach ( var j in joints )
            {
                j. targetEuler = Vector3. zero;
            }
        }

        public Vector3 GetTargetWorldPos () => _targetWorldPos;
        public ArmState GetArmState () => _armState;
        // 必须和IMechanicalArm接口保持一致的枚举
        public enum ArmState
        {
            IDLE, MOVING, WOKING, ERROR
        }
    }
}