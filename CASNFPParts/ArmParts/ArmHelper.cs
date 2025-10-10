using ChinaAeroSpaceNearFuturePackage. Core. Managers;
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
    public static class ArmHelper
    {
        /// <summary>
        /// 根据轴名称获取轴向量，如X轴返回Vector3.right，Y轴返回Vector3.up，Z轴返回Vector3.forward
        /// </summary>
        /// <param name="axis">不区分大小写，如X、x、Y、y、Z、z</param>
        /// <returns>返回轴向量Vector3</returns>
        public static Vector3 rotateAxai (string axis)
        {
            Vector3 rotaAxai = Vector3. zero;
            switch ( axis )
            {
                case "X":
                    rotaAxai = Vector3. right;
                    break;

                case "Y":
                    rotaAxai = Vector3. up;
                    break;

                case "Z":
                    rotaAxai = Vector3. forward;
                    break;

                case "x":
                    rotaAxai = Vector3. right;
                    break;

                case "y":
                    rotaAxai = Vector3. up;
                    break;

                case "z":
                    rotaAxai = Vector3. forward;
                    break;
            }
            return rotaAxai;
        }

        /// <summary>
        /// 根据关节信息字符串获取关节信息。
        /// </summary>
        /// <param name="jointString">字符串格式要求：关节名称、旋转速度、旋转轴、最小角度、最大角度、初始角度，用“,”隔开；如果设置多个大臂则每个大臂之间用“|”隔开,如"node1,0,X,-180,180,0|node2,5,y,-90,90,0|node3,10,Z,0,180,0"</param>
        /// <param name="armPartGameObject">机械臂部件的GameObject</param>
        /// <returns>返回值：ArmJoint数组List，每个元素代表一个关节</returns>
        public static List<ArmJoint> SetJointWithString (string jointString, GameObject armPartGameObject)
        {
            List<ArmJoint> joints = new List<ArmJoint> ();
            string[] splitJoint = jointString. Split ('|');
            string[] splitJointInfo;
            for ( int i = 0 ; i < splitJoint. Length ; i++ )
            {
                splitJointInfo = splitJoint[i]. Split (',');
                for ( int j = 0 ; j < splitJointInfo. Length ; j++ )
                {
                    splitJointInfo[j] = splitJointInfo[j]. Trim ();
                    if ( splitJointInfo[j]. Length != 6 )
                    {
                        CASNFPLogger.Instance.LogError ("机械臂部件" + armPartGameObject. name + "的关节信息字符串格式错误，请检查字符串格式。");
                    }
                }
                ArmJoint joint = new ArmJoint (armPartGameObject. transform. Find (splitJointInfo[0]). transform);
                joint. rotateSpeed = float. Parse (splitJointInfo[1]);
                joint. rotateAxais = rotateAxai (splitJointInfo[2]);
                Vector2 vector2 = new Vector2(float.Parse(splitJointInfo[3]), float.Parse(splitJointInfo[4]));
                joint.rotateLimit = vector2;
                joint.initialAngle = float. Parse (splitJointInfo[5]);
                joint. Init ();
                joints. Add (joint);
            }
            return joints;
        }
    }

    public interface IArmController
    {
        ArmWorkType WorkType
        {
            get;
        }

        bool IsStartAutoCtrl
        {
            get; set;
        }

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