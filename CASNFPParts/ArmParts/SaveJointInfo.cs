using System;
using System. Collections. Generic;
using System. Linq;
using System. Text;
using System. Threading. Tasks;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts
{
    public struct SaveJointInfo
    {
        public string name;
        public string jointTransformName;
        public Vector3 axis;
        public Vector2 minMaxRotationAngle;
        public float spring;
        public float damper;
        public float maxMotorForce;
        public float yzLimit;
        public float breakForce;
        public float targetAngle;//目标角度
        public float startAngle;//出厂起始角度，飞行场景中用于还原
        public float currentAngle;//当前角度
        public float cfgAngle;//配置角度,在编辑厂房里用于还原
        public float rotaSpeed;
    }
}
