using UnityEngine;
namespace ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts
{
    /// <summary>
    /// 所有机械臂部件的通用接口
    /// 取样机械臂、行走机械臂、抓取机械臂等都应实现此接口
    /// </summary>
    internal interface IMechanicalArm
    {
        /// <summary>
        /// 机械臂所属的部件
        /// </summary>
        Part ArmPart
        {
            get;
        }
        /// <summary>
        /// 机械臂是否启用了电源
        /// </summary> 
        bool IsArmPowered
        {
            get;
        }
        /// <summary>
        /// 机械臂是否处于活动状态
        /// </summary>
        bool IsArmActive
        {
            get;
        }
        /// <summary>
        /// 机械臂的关节数量
        /// </summary>
        int jointCount
        {
            get;
        }
        /// <summary>
        /// 获取机械臂的关节变换
        /// </summary>
        /// <param name="jointIndex">关节索引</param>
        /// <returns>返回关节坐标变换Transform，不存在返回null</returns>
        Transform GetJointTransform (int jointIndex);
        /// <summary>
        /// 设置机械臂关节的目标旋转角度
        /// </summary>
        /// <param name="jointIndex">关节索引</param>
        /// <param name="targetEuler">目标欧拉角</param>
        void SetJointTargetRotation (int jointIndex, Vector3 targetEuler);
        /// <summary>
        /// 获取机械臂关节的当前旋转角度
        /// </summary>
        /// <param name="jointIndex">关节索引</param>
        /// <returns>返回关节当前欧拉角，不存在返回null</returns>
        Vector3 GetJointCurrentRotation (int jointIndex);
        /// <summary>
        /// 激活机械臂
        /// </summary>
        void ActivateArm ();
        /// <summary>
        /// 停用机械臂
        /// </summary>
        void DeactivateArm ();
        /// <summary>
        /// 更新机械臂状态
        /// </summary>
        void ArmUpdate ();
        /// <summary>
        /// 固定更新机械臂状态
        /// </summary>
        void ArmFixedUpdate ();
        /// <summary>
        /// 加载机械臂状态
        /// </summary>
        /// <param name="node">配置节点</param>
        void LoadArmState (ConfigNode node);
        /// <summary>
        /// 保存机械臂状态
        /// </summary>
        /// <param name="node">配置节点</param>
        void SaveArmState (ConfigNode node);
        /// <summary>
        /// 获取末端执行器的变换
        /// </summary>
        /// <returns>返回末端执行器坐标变换Transform，不存在返回null</returns>
        Transform GetEndEffectorTransform ();
        void SetTarget (Vector3 worldPoint);
       
    }
}
