using ChinaAeroSpaceNearFuturePackage.Core.Enums;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage.Core.Interfaces
{
    internal interface IRoboticArm
    {
        string ArmPartName { get; set; }//机械臂名称
        ArmType ArmType { get; set; }//机械臂类型
        Transform TopEffectorTransform { get; set; }//上部或末端执行器坐标
        Transform BottomOrEndEffectorTransform { get; set; }//下部或末端执行器坐标
        ArmState CurrentArmState { get; set; }//当前机械臂状态
        int JointCount { get; set; }//关节数量
        double[] PerJointLengths { get; set; }//每个关节的长度
        double[] PerJointTorqueLimits { get; set; }//每个关节的扭矩限制
        double[] PerJointSpeedLimits { get; set; }//每个关节的速度限制
        double[] PerjointCurrentAngles { get; set; }//每个关节的当前角度
        double[] PerjointTargetAngles { get; set; }//每个关节的目标角度
        void MoveToPosition(double x, double y, double z);//移动至目标位置
        void StartWork();//开始工作
        void EndWork();//结束工作
        void Calibrate();//校准
        void Reset();//复位
    }
}
