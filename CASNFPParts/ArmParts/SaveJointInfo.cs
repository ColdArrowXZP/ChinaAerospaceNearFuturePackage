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
        public float targetAngle;
        public float startAngle;
        public bool jointIsLocked;
        public float currentAngle;
        public float rotaSpeed;
    }
}
