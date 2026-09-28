using ChinaAeroSpaceNearFuturePackage. Core. Managers;
using System;
using System. Collections. Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage.CASNFPParts.ArmParts
{
    public static class ArmHelper
    {
        // 获取指定轴向的方向向量
        private static Vector3 GetAxis(string axis)
        {
            switch (axis.ToLower())
            {
                case "x":
                    return Vector3.right;
                case "y":
                    return Vector3.up;
                case "z":
                    return Vector3.forward;
                default:
                    throw new ArgumentException("Invalid axis");
            }
        }

        // 获取指定轴向的方向向量
        public static Vector3 RotateAxis(string axis)
        {
            return GetAxis(axis);
        }

        // 根据字符串设置关节信息并返回关节列表
        public static List<ArmJoint> SetJointWithString(string jointString, Part part)
        {
            List<ArmJoint> joints = new List<ArmJoint>();
            joints.Clear();
            string[] splitJoint = jointString.Split('|');
            foreach (string jointInfo in splitJoint)
            {
                string[] splitJointInfo = jointInfo.Split(',');
                for (int j = 0; j < splitJointInfo.Length; j++)
                {
                    splitJointInfo[j] = splitJointInfo[j].Trim();
                }
                
            }
            return joints;
        }
    }

    // 枚举机器人手臂的工作类型
    public enum ArmWorkType
    {
        Sample_ChangE,
        Walk_TianGong,
        Grabbing,
        Camera,
    }

    // 枚举机器人手臂的状态
    public enum ArmState
    {
        Idle,
        Extending,
        Retracting,
        Doing,
    }

    // 表示一个关节的类
    public class ArmJoint
    {
        private Transform _transform;
        private float _rotateSpeed;
        private Vector2 _rotateLimit;
        private float _currentAngle;
        private Vector3 _rotateAxais;
        private float _initialAngle;

        // 获取或设置关节的变换
        public Transform Transform
        {
            get => _transform;
            set => _transform = value;
        }

        // 获取或设置关节的旋转速度
        public float RotateSpeed
        {
            get => _rotateSpeed;
            set => _rotateSpeed = value;
        }

        // 获取或设置关节的旋转限制
        public Vector2 RotateLimit
        {
            get => _rotateLimit;
            set => _rotateLimit = value;
        }

        // 获取或设置关节的当前角度
        public float CurrentAngle
        {
            get => _currentAngle;
            set => _currentAngle = value;
        }

        // 获取或设置关节的旋转轴向
        public Vector3 RotateAxais
        {
            get => _rotateAxais;
            set => _rotateAxais = value;
        }

        // 获取或设置关节的初始角度
        public float InitialAngle
        {
            get => _initialAngle;
            set => _initialAngle = value;
        }

        // 构造函数，初始化关节
        public ArmJoint(Transform jointTransform)
        {
            Transform = jointTransform;
            RotateSpeed = 5;
            RotateLimit = new Vector2(-180, 180);
            CurrentAngle = 0;
            InitialAngle = 0;
            RotateAxais = Vector3.right;
        }

        // 初始化关节
        public void Init()
        {
            CurrentAngle = InitialAngle;
            Transform.localRotation = Quaternion.Euler(InitialAngle * RotateAxais);
        }

        // 设置关节的角度
        public void SetAngle(float targetAngle)
        {
            targetAngle = Mathf.Clamp(targetAngle, RotateLimit.x, RotateLimit.y);
            CurrentAngle = Mathf.MoveTowards(CurrentAngle, targetAngle, RotateSpeed * Time.deltaTime);
            Transform.localRotation = Quaternion.Euler(CurrentAngle * RotateAxais);
        }
    }
}
