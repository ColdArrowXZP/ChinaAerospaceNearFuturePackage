
using Expansions.Serenity;
using KSP.Localization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage.Parts
{
    public class CASNFP_Arm_CE_SampleArm : CASNFP_RoboticArmBase
    {
        private const float RadToDeg = 180f / (float)Math.PI;

        [KSPField]
        public string extendAngles = "0,0,90,90";

        private float[] thisExtendAngles;
        protected override float[] ExtendAngles
        {
            get => thisExtendAngles;
            set => thisExtendAngles = value;
        }

        private InverseKinematicsResult thisInverseKinematicsResult;
        protected override InverseKinematicsResult _InverseKinematicsResult
        {
            get => thisInverseKinematicsResult;
            set => thisInverseKinematicsResult = value;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            InitializeArmAngles();
        }

        private void InitializeArmAngles()
        {
            string[] angleStrings = extendAngles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (_servoModules == null)
            {
                Debug.LogError("[CASNFP_Arm_CE_SampleArm] _servoModules is null!");
                return;
            }

            thisExtendAngles = angleStrings.Length == _servoModules.Length
                ? Array.ConvertAll(angleStrings, float.Parse)
                : new float[_servoModules.Length];
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!HighLogic.LoadedSceneIsFlight || !canSetTargetPos)
                return;

            HandleSamplePointSelection();
        }

        private void HandleSamplePointSelection()
        {
            ScreenMessages.PostScreenMessage(
                Localizer.Format("请左键选择取样地点", vessel.GetDisplayName()),
                1f, ScreenMessageStyle.UPPER_LEFT);

            if (!Input.GetMouseButtonDown(0))
                return;

            if (!TryGetValidSamplePoint(out Vector3 clickPoint))
                return;

            CalculateAndMoveArm(clickPoint);
        }

        private bool TryGetValidSamplePoint(out Vector3 clickPoint)
        {
            clickPoint = Vector3.zero;
            var ray = FlightGlobals.fetch.mainCameraRef.ScreenPointToRay(Input.mousePosition);

            if (!Physics.Raycast(ray, out RaycastHit hit) || hit.collider == null)
                return false;

            if (hit.collider.gameObject.layer != 15)
            {
                ScreenMessages.PostScreenMessage(
                    Localizer.Format($"选择取样地点为{hit.collider.gameObject.name}不正确，请选择地面取样点",
                    vessel.GetDisplayName()),
                    2f, ScreenMessageStyle.UPPER_RIGHT);
                return false;
            }

            clickPoint = hit.point;
            return true;
        }

        private void CalculateAndMoveArm(Vector3 targetPos)
        {
            thisInverseKinematicsResult = CalculateInverseKinematics(targetPos);

            if (thisInverseKinematicsResult.success)
            {
                Debug.Log($"[CASNFP_Arm_CE_SampleArm] 计算成功，角度: {string.Join(", ", thisInverseKinematicsResult.angles)}");
                armAction.Invoke(this.part, RoboticArmState.Moving);
            }
            else
            {
                ScreenMessages.PostScreenMessage(
                    Localizer.Format("逆运动学计算失败，请检查取样点位置", vessel.GetDisplayName()),
                    2f, ScreenMessageStyle.UPPER_RIGHT);
            }
        }

        private InverseKinematicsResult CalculateInverseKinematics(Vector3 targetPos)
        {
            List<Vector2> servoSoftLimits = new List<Vector2>(); 
            foreach (var servoModule in _servoModules)
            {
                if (servoModule is ModuleRoboticRotationServo servo) 
                {
                    servoSoftLimits.Add(servoModule.hardMinMaxLimits); 
                    continue; 
                }
                if (servoModule is ModuleRoboticServoHinge servoHinge) 
                {
                    servoSoftLimits.Add(servoModule.hardMinMaxLimits); 
                    continue;
                }
            }
                var result = new InverseKinematicsResult
            {
                success = false,
                angles = new float[_servoModules.Length]
            };

            try
            {
                var joint1Transform = part.gameObject.GetChild(_servoModules[1].servoTransformName).transform;
                Vector3 localTargetPos = joint1Transform.InverseTransformPoint(targetPos);

                // 计算底座旋转角度
                result.angles[0] = CalculateBaseRotation(localTargetPos, targetPos);

                // 计算臂长
                float bigArmLength = CalculateArmLength(_servoModules[1], _servoModules[2]);
                float smallArmLength = CalculateArmLength(_servoModules[2], _servoModules[3]);

                // 计算目标距离
                float distanceToTarget = localTargetPos.magnitude;

                // 检查可达性
                if (bigArmLength + smallArmLength <= distanceToTarget)
                {
                    Debug.Log("[CASNFP_Arm_CE_SampleArm]目标点超出机械臂最大长度");
                    return result;
                }

                // 计算大臂下倾角
                double bHu = Math.Atan2(Math.Abs(localTargetPos.z), Math.Abs(localTargetPos.y));
                float bHuAngle = (float)(bHu * RadToDeg);

                // 使用余弦定理计算关节角度
                float a = distanceToTarget;
                float b = bigArmLength;
                float c = smallArmLength;

                float angleA = (float)(Math.Acos((b * b + c * c - a * a) / (2 * b * c)) * RadToDeg);
                float angleB = (float)(Math.Acos((a * a + b * b - c * c) / (2 * a * b)) * RadToDeg);
                float angleC = 180 - angleA - angleB;

                // 设置各关节角度
                for (int i = 0; i < _servoModules.Length; i++)
                {
                    switch (i) 
                    {
                        case 0:
                            result.angles[i] = result.angles[0]; // 底座角度
                            break;
                            case 1:
                            result.angles[i] = 180 - angleC - bHuAngle; // 大臂角度
                            break;
                            case 2:
                            result.angles[i] = thisExtendAngles[i] - angleA; // 小臂角度
                            break;
                            case 3:
                            result.angles[i] = thisExtendAngles[i]; // 其他关节保持默认
                            break;
                    }
                }

                // 检查软限制
                for (int i = 0; i < _servoModules.Length; i++)
                {
                    if (result.angles[i] < servoSoftLimits[i].x || result.angles[i] > servoSoftLimits[i].y)
                    {
                        Debug.Log($"[CASNFP_Arm_CE_SampleArm] 关节{i}角度超出限制");
                        return result;
                    }
                }

                result.success = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CASNFP_Arm_CE_SampleArm] 计算错误: {ex.Message}");
            }

            return result;
        }

        private float CalculateBaseRotation(Vector3 localTargetPos, Vector3 worldTargetPos)
        {
            double aHu = Math.Atan2(Math.Abs(localTargetPos.x), Math.Abs(localTargetPos.z));
            float thetaDeg = (float)(aHu * RadToDeg);

            return _servoModules[0].transform.InverseTransformPoint(worldTargetPos).x < 0
                ? thetaDeg * -1 + thisExtendAngles[0]
                : thetaDeg + thisExtendAngles[0];
        }

        private float CalculateArmLength(BaseServo start, BaseServo end)
        {
            var startPos = part.gameObject.GetChild(start.servoTransformName).transform.position;
            var endPos = part.gameObject.GetChild(end.servoTransformName).transform.position;
            return Vector3.Distance(startPos, endPos);
        }
    }
}
