using ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm;
using Expansions. Serenity;
using Microsoft. SqlServer. Server;
using Steamworks;
using System;
using System. Collections. Generic;
using UnityEngine;
using static FileIO;

public class RoboticArmIK
{
    private const float RadToDeg = 180f / Mathf. PI;
    public List<ArmPartJointInfo> armPartInfos;
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
        // 计算第一个关节的目标角度，使机械臂指向目标位置
        Vector3 targetPos = armPartInfos[0]. servoHinge. BaseObject (). transform.InverseTransformPoint(targetPosition);
        Vector3 targetXYDir = new Vector3(targetPos.x,targetPos.y,0). normalized;
        Vector3 currentXYDir = new Vector3 (0,1,0);
        float angleToRot = Vector3. Angle (currentXYDir, targetXYDir);
        Vector3 cross = Vector3. Cross (currentXYDir, targetXYDir);
        if (cross.z < 0) angleToRot = -angleToRot;
        jointTargetAngle.Clear();
        jointTargetAngle.Add(angleToRot);
        Debug. Log ("angleToRot:"+angleToRot);
        // 通过三角形内角和关系，已知各臂长以及到目标点长度，计算三角形各个内角
        float targetDist = targetPos. magnitude;
        float link1 = armPartInfos[1]. armLength;
        float link2 = armPartInfos[2]. armLength;
        float angleA = Mathf. Acos ((link1 * link1 + targetDist * targetDist - link2 * link2) / (2 * link1 * targetDist)) * RadToDeg;
        Debug. Log ("angleA:"+angleA);
        float angleB = Mathf. Acos ((link1 * link1 + link2 * link2 - targetDist * targetDist) / (2 * link1 * link2)) * RadToDeg;
        float angeleC = 180 - angleA - angleB;

        // 计算第二个关节的目标角度，使机械臂到达目标位置
        Vector3 targetYZDir = new Vector3 (0, targetPos. y, targetPos.z). normalized;
        Vector3 currentYZDir = new Vector3 (0, 0, -1);
        float angleToUp = Mathf.Abs(Vector3. Angle (currentYZDir, targetYZDir)) ;

        Debug. Log ($"angleA:{angleA},angleB:{angleB},angeleC:{angeleC}");
        jointTargetAngle.Add(angleA);
        jointTargetAngle.Add(angleB);
        jointTargetAngle.Add(angeleC);
        
        return true;
    }
    
}

