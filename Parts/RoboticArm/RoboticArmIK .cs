using ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm;
using Expansions. Serenity;
using Steamworks;
using System;
using System. Collections. Generic;
using UnityEngine;

public class RoboticArmIK
{
    private const float RadToDeg = 180f / Mathf. PI;
    private List<ArmPartJointInfo> armPartInfos;
    public Vector3 targetPosition;
    public List<float> jointTargetAngle = new List<float>();
    private float armlength;
    public RoboticArmIK (List<ArmPartJointInfo> infos,Vector3 targetPos, float length)
    {
        armPartInfos = infos;
        targetPosition = targetPos;
        armlength = length;
    }

    public bool SolveIK ()
    {
        // 假设armPartInfos顺序为：Base, Link1, Link2, ..., Work
        //if ( armPartInfos == null || armPartInfos. Count < 3 )
        //{
        //    Debug. LogError ("[RoboticArmIK] 关节数量不足，无法求解逆运动学！");
        //    return false;
        //}

        ////首先计算底座旋转角度，使得机械臂朝向目标位置。获得基座位置、旋转轴、当前角度和目标位置的方向向量，通过向量投影计算旋转角度。
        //Matrix4x4 worldToLocal = armPartInfos[0].jointTransform.worldToLocalMatrix;
        //worldToLocal.SetTRS(armPartInfos[0].jointTransform.position, Quaternion.identity, Vector3.one);
        //Vector3 localTargetPos = worldToLocal.MultiplyPoint3x4(targetPosition).normalized;
        //Debug. Log ("localTargetPos:" + localTargetPos.ToString());
        //float aHu = Mathf. Atan2 (localTargetPos. y, localTargetPos. x);
        //float thetaDeg = aHu * RadToDeg;
        //if (thetaDeg > 0) { thetaDeg = thetaDeg - 180; }
        //else {thetaDeg = thetaDeg +180; }
        //Debug. Log ("thetaDeg:"+thetaDeg);
        //jointTargetAngle. Add (thetaDeg);
        //for(int i =1 ; i < armPartInfos. Count; i++)
        //{
        //    jointTargetAngle. Add (armPartInfos[i]. currentAngle);
        //}
        //return true;


        // 计算底座旋转角度（底座绕Z轴旋转，初始0度为-X轴方向）
        Vector3 basePos = armPartInfos[0].servoHinge.servoTransformPosition;
        Vector3 vector3 = armPartInfos[0].servoHinge.transform.InverseTransformPoint(targetPosition);
        Vector3 targetDir = (targetPosition - vector3).normalized;
        Debug.Log("Base Pos: " + basePos);
        Debug.Log("Target Pos: " + targetPosition);
        Debug.Log("Target Dir: " + targetDir);
        // 忽略Z轴，只在XY平面计算
        Vector2 baseToTarget2D = new Vector2(targetDir.x, targetDir.y);
        Debug.Log("Base to Target 2D: " + baseToTarget2D);
        // -X轴方向
        Vector2 baseAxis2D = new Vector2(1f, 0f);
        // 计算夹角，底座0度为-X轴
        float baseAngle = Vector2.SignedAngle(baseAxis2D, baseToTarget2D);
        baseAngle = Mathf.Abs(baseAngle);
        Debug.Log("Base Angle: " + baseAngle);
        jointTargetAngle.Clear();
        jointTargetAngle.Add(baseAngle);

        // 计算大小臂夹角（二维平面，忽略底座旋转后的影响）
        //Vector3 baseToTarget = targetPosition - basePos;
        //float distance = baseToTarget.magnitude;
        //float l1 = armPartInfos[1].armLength;
        //float l2 = armPartInfos[2].armLength;

        //// 使用余弦定理计算大小臂夹角
        //float cosAngle2 = Mathf.Clamp((l1 * l1 + l2 * l2 - distance * distance) / (2 * l1 * l2), -1f, 1f);
        //float angle2 = Mathf.Acos(cosAngle2) * RadToDeg;
        //jointTargetAngle.Add(angle2);

        //float cosAngle1 = Mathf.Clamp((distance * distance + l1 * l1 - l2 * l2) / (2 * l1 * distance), -1f, 1f);
        //float angle1 = Mathf.Acos(cosAngle1) * RadToDeg;

        //// 计算目标点与底座的夹角（仰角）
        //float targetElevation = Mathf.Atan2(baseToTarget.z, new Vector2(baseToTarget.x, baseToTarget.y).magnitude) * RadToDeg;
        //jointTargetAngle.Add(targetElevation - angle1);

        // 其余关节保持当前位置
        for (int i = 1; i < armPartInfos.Count; i++)
        {
            jointTargetAngle.Add(armPartInfos[i].currentAngle);
        }
        return true;
    }
}

