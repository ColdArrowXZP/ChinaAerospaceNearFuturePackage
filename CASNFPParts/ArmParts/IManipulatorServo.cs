using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts
{
    /// <summary>
    /// 机械臂伺服系统接口，定义了机械臂各个部件的基本操作方法
    /// </summary>
    internal interface IManipulatorServo
    {
        /// <summary>
        /// 将伺服系统的刚体附加到指定的连接节点
        /// </summary>
        /// <param name="node">要附加到的连接节点</param>
        /// <returns>附加后的刚体组件</returns>
        Rigidbody AttachServoRigidBody (AttachNode node);

        /// <summary>
        /// 转换指定名称的碰撞器状态
        /// </summary>
        /// <param name="colName">要转换的碰撞器名称</param>
        /// <returns>转换操作是否成功</returns>
        bool TransformCollider (string colName);

        /// <summary>
        /// 获取机械臂的运动部件
        /// </summary>
        /// <returns>运动部件的游戏对象</returns>
        GameObject GetMovingObject ();

        /// <summary>
        /// 获取机械臂的基础部件
        /// </summary>
        /// <returns>基础部件的游戏对象</returns>
        GameObject GetBaseObject ();

        /// <summary>
        /// 获取指定连接节点的刚体组件
        /// </summary>
        /// <param name="node">指定的连接节点</param>
        /// <returns>节点的刚体组件</returns>
        Rigidbody GetNodeRigidBody (AttachNode node);
    }
}
