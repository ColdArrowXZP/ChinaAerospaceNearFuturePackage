using Expansions. Serenity;
using System;
using System. Collections. Generic;
using System. Linq;
using System. Text;
using System. Threading. Tasks;
using TMPro;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts
{
    public interface IModuleCASNFP_RoboticArmPart
    {
        ArmWorkType WorkType {get;}
        bool IsStartAutoCtrl {get; set;}
        void StartAutoCtrl ();
        ArmState GetArmState ();
        Vector3 GetTargetPos ();//获取目标位置
        void ExtendArm (Vector3 targetPos); // 伸展机械臂
        void RetractArm (); // 收缩机械臂
        void PerformAction (); // 执行动作
        void StopAction (); // 停止动作 

    }
    public enum ArmWorkType // 机械臂类型枚举
    {
        Sample_ChangE, // 取样（嫦娥）机械臂
        Walk_TianGong, // 吸盘式（天宫）机械臂
        Grabbing, // 抓取式机械臂
        Camera, // 摄像类机械臂
    }
    public enum ArmState // 机械臂状态枚举
    {
        Idle, // 空闲
        Extanding, // 拓展
        Retracting, // 收缩
        Doing, // 执行
    }
    public class ArmJoint
    {
        public Transform transform;
        public float rotateSpeed;
        public Vector2 rotateLimit;
        public float currentAngle;
        public Vector3 rotateAxais;
        public float initialAngle;

        public ArmJoint (Transform jointTransform)
        {
            transform = jointTransform;
            rotateSpeed = 5;
            rotateLimit = new Vector2 (-180, 180);
            currentAngle = 0;
            initialAngle = 0;
            rotateAxais = Vector3. right;
        }

        // 初始化时直接设置初始角度
        public void Init ()
        {
            currentAngle = initialAngle;
            transform. localRotation = Quaternion. Euler (initialAngle * rotateAxais);
        }

        // 直接用Transform控制角度
        public void SetAngle (float targetAngle)
        {
            targetAngle = Mathf. Clamp (targetAngle, rotateLimit. x, rotateLimit. y);
            // 平滑插值到目标角度
            currentAngle = Mathf. MoveTowards (currentAngle, targetAngle, rotateSpeed * Time. deltaTime);
            transform. localRotation = Quaternion. Euler (currentAngle * rotateAxais);
        }
    }
}
