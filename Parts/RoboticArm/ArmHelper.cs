using Expansions. Serenity;
using System;
using System. Collections. Generic;
using System. Linq;
using System. Text;
using System. Threading. Tasks;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm
{
    public enum ArmPartType // 机械臂组件类型枚举
    {
        Base,// 基座
        link,// 连接臂
        work,// 工作臂
    }
    public enum ArmWorkType // 机械臂类型枚举
    {
        Sample_ChangE, // 取样（嫦娥）机械臂
        Walk_TianGong, // 吸盘式（天宫）机械臂
        Grabbing, // 抓取式机械臂
        Camera, // 摄像类机械臂
    }
    
    public class ArmPartJointInfo
    {
        public Vessel vessel;
        public int partIndexInVessel;
        public int partIndexInArm;

        public Part part;
        public ModuleRoboticServoHinge servoHinge;
        public ArmPartType partType;
        public ArmWorkType armWorkType;
        public Transform jointTransform;
        public float armLength;
        public float minLimit;
        public float maxLimit;
        public float rotationSpeed;
        public Vector3 rotationAxis;
        public float currentAngle;
        public float targetAngle;
        public Transform workPosTransform;
    }
}
