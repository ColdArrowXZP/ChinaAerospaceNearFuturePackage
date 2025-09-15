using Expansions. Serenity;
using System;
using System. Collections. Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm
{
    public class ModuleCASNFP_RoboticArmPart : PartModule
    {
        [KSPField]
        public int thisPartBelongArmType = 0;// 机械臂组件类型枚举,0=基座,1=连接臂,2=工作臂,3=其他类型
        [KSPField]
        public int thisPartBelongWorkType = 0; // 机械臂类型索引,0=取样（嫦娥）机械臂,1=吸盘式（天宫）机械臂,2=抓取式机械臂,3=摄像类机械臂，4=其他类型
        [KSPField]
        public string workPosName = "workDrill"; // 机械臂工作端位置名称,用于机械臂工作端位置定位
        [KSPField]
        public int armSpeed = 5; // 机械臂移动速度,单位度/秒
        public ArmJoint baseJoint;
        public ArmJoint link01Joint;
        public ArmJoint link02Joint;
        public ArmJoint effectJoint;
        public List<ArmJoint> joints = new List<ArmJoint> ();
        public ArmPartType armPartType
        {
            get
            {
                return ( ArmPartType )thisPartBelongArmType;
            }
        }
        public ArmWorkType armWorkType
        {
            get
            {
                return ( ArmWorkType )thisPartBelongWorkType;
            }
        }
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            if ( HighLogic. LoadedSceneIsFlight )
            {
                baseJoint = new ArmJoint (part. FindModelTransform ("node1"));
                baseJoint.rotateSpeed = armSpeed;
                baseJoint. rotateAxais = Vector3. forward;
                baseJoint. rotateSpeed = 10f;
                baseJoint. Init ();
                link01Joint = new ArmJoint (part. FindModelTransform ("node2"));
                baseJoint. rotateSpeed = armSpeed;
                link01Joint. rotateLimit = new Vector2 (-107, 107);
                link01Joint. Init ();
                link02Joint = new ArmJoint (part. FindModelTransform ("node3"));
                baseJoint. rotateSpeed = armSpeed;
                link02Joint. Init ();
                effectJoint = new ArmJoint (part. FindModelTransform ("node4"));
                baseJoint. rotateSpeed = armSpeed;
                effectJoint. Init ();
                joints. Add (baseJoint);
                joints. Add (link01Joint);
                joints. Add (link02Joint);
                joints. Add (effectJoint);

                // 伪物理方案：不再添加Rigidbody和HingeJoint，不做物理连接
            }
        }
    }
}

