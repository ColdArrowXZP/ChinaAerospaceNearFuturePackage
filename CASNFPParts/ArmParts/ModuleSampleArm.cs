using System;
using System. Collections. Generic;
using System. Threading;
using System. Threading. Tasks;
using UnityEngine;

namespace CASNFPParts. ArmParts
{
    /// <summary>
    /// 示例机械臂实现，继承 BaseMechanicalArmModule。
    /// 把正/逆运动学求解委托给 ArmHelper。
    /// </summary>
    public class SampleArm : BaseMechanicalArmModule
    {
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            // 这里可以做臂特化初始化，例如设置默认 joint 目标、工具偏移等
            for ( int i = 0 ; i < joints. Count ; i++ )
            {
                // 确保 targetEuler 有合理初始值
                joints[i]. targetEuler = joints[i]. jointTransform != null ? joints[i]. jointTransform. localEulerAngles : Vector3. zero;
            }
        }

        /// <summary>
        /// 同步求解 IK：返回每关节 (x,y,z) 扁平化的角度数组（单位 deg）。
        /// </summary>
        public bool TrySolveIK (Vector3 targetWorldPos, out double[] jointAngles)
        {
            return ArmHelper. SolveIK_CCD (joints, endEffectorTransform, targetWorldPos, out jointAngles);
        }

        /// <summary>
        /// 异步移动到世界坐标位置：先求解 IK，再把求解出的角度写入 joints[].targetEuler，
        /// 由基类的 ArmUpdate 驱动角度平滑跟随。这里返回一个完成的 Task（如果需要等待到达可以改为轮询检测）。
        /// </summary>
        public Task<bool> MoveToPositionAsync (Vector3 targetWorldPos, double speed = 0.0, CancellationToken cancellationToken = default)
        {
            if ( !TrySolveIK (targetWorldPos, out double[] angles) || angles == null )
                return Task. FromResult (false);

            // angles 按每关节 (x,y,z) 存储
            int expected = joints. Count * 3;
            if ( angles. Length != expected )
                return Task. FromResult (false);

            for ( int i = 0 ; i < joints. Count ; i++ )
            {
                int baseIdx = i * 3;
                joints[i]. targetEuler = new Vector3 (
                    ( float )angles[baseIdx + 0],
                    ( float )angles[baseIdx + 1],
                    ( float )angles[baseIdx + 2]
                );
            }

            // 可在此处理 speed / duration 参数（例如将 maxAngularVelocity 调整为 speed），此处留空
            return Task. FromResult (true);
        }

        /// <summary>
        /// 快速同步移动（兼容旧接口）
        /// </summary>
        public bool MoveToPosition (Vector3 targetWorldPos, double speed = 0.0)
        {
            var t = MoveToPositionAsync (targetWorldPos, speed);
            return t. Result;
        }
    }
}
