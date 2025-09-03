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
        Debug. Log ("初始角度"+armPartInfos[0]. jointTransform.localRotation.eulerAngles.ToString());
        Debug. Log ("初始位置" + armPartInfos[0]. jointTransform.localPosition. ToString ());
        Debug. Log ("初始位置Pos" + armPartInfos[0]. jointTransform. position. ToString ());
        Debug. Log ("初始角度Rot" + armPartInfos[0]. jointTransform. rotation. eulerAngles. ToString ());
        Debug. Log ("Part位置" + armPartInfos[0].part.transform.position. ToString ());
        Debug. Log ("Part角度"+ armPartInfos[0].part.transform.rotation.eulerAngles.ToString());
        Vector3 localTargetPos = armPartInfos[0]. jointTransform.InverseTransformPoint(targetPosition);
        localTargetPos -= armPartInfos[0]. jointTransform. localPosition;
        Debug. Log ("localTargetPos:" + localTargetPos.ToString());
        float aHu = Mathf. Atan2 (localTargetPos. y, localTargetPos. x);
        float thetaDeg = aHu * RadToDeg;
        Debug. Log ("thetaDeg:"+thetaDeg);
        jointTargetAngle. Add (thetaDeg);
        for(int i =1 ; i < armPartInfos. Count; i++)
        {
            jointTargetAngle. Add (armPartInfos[i]. currentAngle);
        }
        return true;
    }
}

