using ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm;
using Expansions. Serenity;
using System;
using System. Collections. Generic;
using UnityEngine;

public class RoboticArmIK
{
    private List<ArmPartJointInfo> armPartInfos;
    private Vector3 targetPosition;
    public List<float> jointTargetAngle = new List<float>();
    public RoboticArmIK (List<ArmPartJointInfo> infos,Vector3 targetPos)
    {
        armPartInfos = infos;
        targetPosition = targetPos;
    }

    public bool SolveIK ()
    {
        // 仅支持平面2关节（大臂+小臂）机械臂的逆运动学（可根据实际项目扩展）
        // 假设armPartInfos顺序为：Base, Link1, Link2, ..., Work
        if ( armPartInfos == null || armPartInfos. Count < 3 )
        {
            Debug. LogError ("[RoboticArmIK] 关节数量不足，无法求解逆运动学！");
            return false;
        }

        // 1. 获取基座、各段长度
        Transform baseTransform = armPartInfos[0]. jointTransform;
        Vector3 basePos = baseTransform. position;
        Vector3 localTarget = targetPosition - basePos;

        // 计算基座旋转角度（绕Y轴）
        float baseYaw = Mathf. Atan2 (localTarget. x, localTarget. z); // Unity世界坐标，x/z平面
                                                                       // 将目标点旋转到基座正前方（即XZ平面上的距离）
        Quaternion invYaw = Quaternion. Inverse (Quaternion. Euler (0, baseYaw * Mathf. Rad2Deg, 0));
        Vector3 planarTarget = invYaw * localTarget;
        float targetDist = new Vector2 (planarTarget. x, planarTarget. z). magnitude; // XZ平面距离
                                                                                      // 只考虑平面（忽略Y）
        float y = planarTarget. y;

        // 2. 计算每段长度
        float l1 = armPartInfos[1]. armLength;
        float l2 = armPartInfos[2]. armLength;

        // 3. 检查目标是否可达
        float planarLen = Mathf. Sqrt (targetDist * targetDist + y * y);
        if ( planarLen > l1 + l2 )
        {
            Debug. LogWarning ("[RoboticArmIK] 目标超出机械臂最大长度！");
            return false;
        }

        // 4. 余弦定理求解夹角
        // θ2 = cos⁻¹((r² - l1² - l2²) / (2*l1*l2))
        float cosTheta2 = ( planarLen * planarLen - l1 * l1 - l2 * l2 ) / ( 2 * l1 * l2 );
        float theta2 = Mathf. Acos (Mathf. Clamp (cosTheta2, -1f, 1f)); // 弯曲角

        // 5. θ1 = atan2(y, r) - atan2(l2*sinθ2, l1 + l2*cosθ2)
        float angleToTarget = Mathf. Atan2 (y, targetDist);
        float k1 = l1 + l2 * Mathf. Cos (theta2);
        float k2 = l2 * Mathf. Sin (theta2);
        float theta1 = angleToTarget - Mathf. Atan2 (k2, k1);

        // 6. 结果写入jointTargetAngle
        jointTargetAngle. Clear ();
        // 0: 基座旋转角度（度），1: 大臂，2: 小臂
        jointTargetAngle. Add (baseYaw * Mathf. Rad2Deg);
        jointTargetAngle. Add (theta1 * Mathf. Rad2Deg);
        jointTargetAngle. Add (theta2 * Mathf. Rad2Deg);

        // 其余关节保持当前位置
        for ( int i = 3 ; i < armPartInfos. Count ; i++ )
        {
            jointTargetAngle. Add (armPartInfos[i]. currentAngle);
        }

        return true;
    }

}

