using System. Collections. Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm
{
    public class AdvancedArmCtrlLogic : MonoBehaviour
    {
        private ArmStateMachine _stateMachine;
        private ModuleCASNFP_RoboticArmPart _currentArm;
        private List<ArmJoint> _joints;
        private float _jointLength;
        private Transform _workTransform;
        private Vector3 _targetPosition;
        private bool _hasTarget;

        private void Awake ()
        {
            _stateMachine = GetComponent<ArmStateMachine> ();
            _stateMachine. ChangeState (ArmState. Idle);
        }

        private void Start ()
        {
            InitializeArm ();
        }

        private void InitializeArm ()
        {
            _currentArm = GetComponent<CASNFP_SetRocAutoCtrl> (). currentWorkingRoboticArm;
            _joints = _currentArm. joints;
            _workTransform = _currentArm. part. FindModelTransform (_currentArm. workPosName);
            _jointLength = Vector3. Distance (_joints[_joints. Count - 1]. transform. position, _workTransform. position);
        }

        private void Update ()
        {
            switch ( _stateMachine. GetCurrentState () )
            {
                case ArmState. Moving:
                    UpdateMovement ();
                    break;
                case ArmState. Working:
                    UpdateWork ();
                    break;
                case ArmState. Resetting:
                    UpdateReset ();
                    break;
            }
        }

        private void UpdateMovement ()
        {
            if ( Vector3. Distance (_workTransform. position, _targetPosition) < 0.1f )
            {
                _stateMachine. ChangeState (ArmState. Working);
            }
            else
            {
                PerformIK (_targetPosition);
            }
        }

        private void UpdateWork ()
        {
            // 执行工作逻辑
        }

        private void UpdateReset ()
        {
            // 执行复位逻辑
        }

        private void PerformIK (Vector3 target)
        {
            // 使用CCD算法进行逆运动学求解
            for ( int iteration = 0 ; iteration < 10 ; iteration++ )
            {
                for ( int i = _joints. Count - 2 ; i >= 0 ; i-- )
                {
                    Vector3 toTarget = target - _joints[i]. transform. position;
                    Vector3 toJoint = _workTransform. position - _joints[i]. transform. position;

                    float angle = Vector3. Angle (toTarget, toJoint);
                    if ( angle > 0.1f )
                    {
                        Vector3 axis = Vector3. Cross (toJoint, toTarget). normalized;
                        _joints[i]. transform. Rotate (axis, angle * Time. deltaTime);
                    }
                }
            }
        }

        public void SetTarget (Vector3 target)
        {
            _targetPosition = target;
            _hasTarget = true;
            _stateMachine. ChangeState (ArmState. Moving);
        }
    }
}
