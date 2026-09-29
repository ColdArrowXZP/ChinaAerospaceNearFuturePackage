using UnityEngine;

namespace CASNFPParts. ArmParts
{
    public class ArmJointData
    {
        public string transformName;
        public Transform jointTransform;
        public Vector3 minAngle;
        public Vector3 maxAngle;
        public Vector3 maxAngularVelocity;
        public Vector3 targetEuler;
        // 视需要添加其它字段/属性
    }
}