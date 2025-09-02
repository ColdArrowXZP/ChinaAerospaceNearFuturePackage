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
    private Vector3 targetPosition;
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
        if ( armPartInfos == null || armPartInfos. Count < 3 )
        {
            Debug. LogError ("[RoboticArmIK] 关节数量不足，无法求解逆运动学！");
            return false;
        }

        //首先计算底座旋转角度，使得机械臂朝向目标位置。获得基座位置、旋转轴、当前角度和目标位置的方向向量，通过向量投影计算旋转角度。
        Transform baseTransform = armPartInfos[0]. jointTransform;
        Debug.Log("Base Position: " + baseTransform.position.ToString());
        Debug.Log("Base Rotation: " + baseTransform.rotation.eulerAngles.ToString());
        Vector3 localTargetPos = baseTransform.InverseTransformPoint(targetPosition);
        float aHu = Mathf. Atan2 (localTargetPos. y, localTargetPos. x);
        float thetaDeg = aHu * RadToDeg;
        Debug.Log("ThetaDeg: " + thetaDeg);
        float baseRotAngle = 0f;
        if ( thetaDeg >= 0 )
        {
            baseRotAngle = thetaDeg;
        }
        else
        {
            if ( thetaDeg >= -90 )
            {
                baseRotAngle = thetaDeg;
            }
            else
            {
                baseRotAngle = 360+thetaDeg;
            }
        }

        Debug. Log (localTargetPos. ToString ());
        Debug. Log (baseRotAngle);
        jointTargetAngle. Add (baseRotAngle);
        for(int i =1 ; i < armPartInfos. Count; i++)
        {
            jointTargetAngle. Add (armPartInfos[i]. currentAngle);
        }
        return true;
    }
}

