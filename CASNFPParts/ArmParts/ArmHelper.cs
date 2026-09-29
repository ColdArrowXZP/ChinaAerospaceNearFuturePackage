using System;
using System. Collections. Generic;
using UnityEngine;

namespace CASNFPParts. ArmParts
{
    /// <summary>
    /// 简单的 Arm 帮助类：包含 CCD 逆运动学求解（对任意关节数量）和正运动学帮助方法。
    /// NOTE: 求解过程中临时修改 Transform，完成后会恢复原始 localEulerAngles。
    /// 输出角度按每关节 (x,y,z) 扁平化到 double[] 中。
    /// </summary>
    internal static class ArmHelper
    {
        public static bool SolveIK_CCD (IList<ArmJointData> joints, Transform endEffector, Vector3 targetWorldPos,
            out double[] outAngles, int maxIterations = 15, float tolerance = 0.01f)
        {
            outAngles = null;
            if ( joints == null || joints. Count == 0 || endEffector == null )
                return false;

            // 保存原始 localEulerAngles
            var original = new Vector3[joints. Count];
            for ( int i = 0 ; i < joints. Count ; i++ )
            {
                var t = joints[i]. jointTransform;
                original[i] = t != null ? t. localEulerAngles : Vector3. zero;
            }

            try
            {
                for ( int iter = 0 ; iter < maxIterations ; iter++ )
                {
                    // 从末端到基座逐关节调整
                    for ( int i = joints. Count - 1 ; i >= 0 ; i-- )
                    {
                        var joint = joints[i];
                        var jt = joint. jointTransform;
                        if ( jt == null )
                            continue;

                        Vector3 toEnd = endEffector. position - jt. position;
                        Vector3 toTarget = targetWorldPos - jt. position;

                        if ( toEnd. sqrMagnitude < 1e-8f || toTarget. sqrMagnitude < 1e-8f )
                            continue;

                        // 计算旋转，使 end 向 target 转动
                        Quaternion rot = Quaternion. FromToRotation (toEnd, toTarget);
                        jt. rotation = rot * jt. rotation;

                        // 将 localEulerAngles 限制到关节限制范围
                        Vector3 local = jt. localEulerAngles;
                        local. x = ClampAngleToLimit (local. x, joint. minAngle. x, joint. maxAngle. x);
                        local. y = ClampAngleToLimit (local. y, joint. minAngle. y, joint. maxAngle. y);
                        local. z = ClampAngleToLimit (local. z, joint. minAngle. z, joint. maxAngle. z);
                        jt. localEulerAngles = local;
                    }

                    // 检查收敛
                    if ( ( endEffector. position - targetWorldPos ). sqrMagnitude <= tolerance * tolerance )
                        break;
                }

                // 输出角度：每关节 x,y,z（deg），按 joints 顺序扁平化
                outAngles = new double[joints. Count * 3];
                for ( int i = 0 ; i < joints. Count ; i++ )
                {
                    var t = joints[i]. jointTransform;
                    Vector3 le = t != null ? t. localEulerAngles : joints[i]. targetEuler;
                    int baseIdx = i * 3;
                    outAngles[baseIdx + 0] = le. x;
                    outAngles[baseIdx + 1] = le. y;
                    outAngles[baseIdx + 2] = le. z;
                }

                return true;
            }
            finally
            {
                // 恢复原始角度，保证不即时修改场景（调用者可把结果写入 targetEuler）
                for ( int i = 0 ; i < joints. Count ; i++ )
                {
                    var t = joints[i]. jointTransform;
                    if ( t != null )
                        t. localEulerAngles = original[i];
                }
            }
        }

        public static Vector3 ComputeForwardKinematics_EndEffector (IList<ArmJointData> joints, Transform endEffector)
        {
            if ( joints == null || joints. Count == 0 || endEffector == null )
                return Vector3. zero;
            // 直接返回当前末端世界位置信息（基于 Transform 层级）
            return endEffector. position;
        }

        static float NormalizeAngle (float angle)
        {
            angle = Mathf. Repeat (angle + 180f, 360f) - 180f;
            return angle;
        }

        static float ClampAngleToLimit (float angle, float minLimit, float maxLimit)
        {
            // 将角度正规化到 -180..180 以便正确比较/夹持
            float a = NormalizeAngle (angle);
            float min = NormalizeAngle (minLimit);
            float max = NormalizeAngle (maxLimit);

            // 如果限制跨越 -180/180 边界，做兼容处理
            if ( min <= max )
            {
                a = Mathf. Clamp (a, min, max);
            }
            else
            {
                // 例如 min = 120, max = -120 表示允许 120..180 U -180..-120
                if ( a > max && a < min )
                {
                    // 将 a 移到最近一端
                    float distToMin = Mathf. Abs (Mathf. DeltaAngle (a, min));
                    float distToMax = Mathf. Abs (Mathf. DeltaAngle (a, max));
                    a = ( distToMin < distToMax ) ? min : max;
                }
            }

            // 返回到 0..360 区间，以兼容 Unity localEulerAngles 表示
            float res = Mathf. Repeat (a + 360f, 360f);
            return res;
        }
    }
}
