using System. Collections. Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm
{
    public enum ArmState
    {
        Idle,
        Moving,
        Working,
        Resetting,
        Error
    }
    public class ErrorStateHandler : IArmStateHandler
    {
        private float _errorStartTime;
        private const float ERROR_DISPLAY_DURATION = 3f;
        private string _errorCode = "ERR_0001";
        private bool _isErrorResolved = false;

        public void EnterState (ArmStateMachine stateMachine)
        {
            Debug. LogError ("机械臂进入错误状态");
            _errorStartTime = Time. time;
            _isErrorResolved = false;

            // 执行紧急停止操作
            EmergencyStop ();

            // 记录错误信息
            LogErrorDetails ();
        }

        public void UpdateState (ArmStateMachine stateMachine)
        {
            // 显示错误信息持续一段时间
            if ( Time. time - _errorStartTime < ERROR_DISPLAY_DURATION )
            {
                DisplayErrorMessage ();
            }
            else if ( !_isErrorResolved )
            {
                // 检查是否可以重置
                if ( CanAttemptReset () )
                {
                    stateMachine. ChangeState (ArmState. Resetting);
                    _isErrorResolved = true;
                }
                else if ( ShouldRemainInError () )
                {
                    // 继续保持在错误状态
                    DisplayErrorMessage ();
                }
            }
        }

        public void ExitState (ArmStateMachine stateMachine)
        {
            Debug. Log ("机械臂离开错误状态");
            // 清理错误状态相关资源
            ClearErrorState ();
        }

        private void EmergencyStop ()
        {
            // 实现紧急停止逻辑
            Debug. Log ("执行紧急停止操作");
            // 停止所有关节运动
            // 切断动力输出
            // 锁定当前位置
        }

        private void LogErrorDetails ()
        {
            // 记录详细的错误信息
            Debug. LogError ($"错误发生时间: {Time. time}");
            Debug. LogError ($"当前状态: {GetCurrentStateDetails ()}");
            Debug. LogError ($"错误代码: {_errorCode}");
        }

        private void DisplayErrorMessage ()
        {
            // 在UI上显示错误信息
            Debug. LogError ("机械臂处于错误状态");
        }

        private bool CanAttemptReset ()
        {
            // 检查是否可以尝试重置
            // 例如：检查错误类型是否可恢复、系统是否安全等
            return true; // 简化实现，实际应根据具体错误类型判断
        }

        private bool ShouldRemainInError ()
        {
            // 检查是否应该保持在错误状态
            // 例如：严重错误需要人工干预
            return false; // 简化实现，实际应根据错误严重程度判断
        }

        private void ClearErrorState ()
        {
            // 清理错误状态相关资源
            Debug. Log ("清理错误状态资源");
        }

        private string GetCurrentStateDetails ()
        {
            // 获取当前状态的详细信息
            return "未知状态";
        }
    }

    public class MovingStateHandler : IArmStateHandler
    {
        private Vector3 _targetPosition;
        private float _moveSpeed = 2.0f;
        private bool _isMoving = false;

        public void EnterState (ArmStateMachine stateMachine)
        {
            Debug. Log ("机械臂进入移动状态");
            // 获取目标位置
            _targetPosition = GetTargetPosition ();
            _isMoving = true;
        }

        public void UpdateState (ArmStateMachine stateMachine)
        {
            if ( !_isMoving )
                return;

            // 计算移动方向
            Vector3 direction = ( _targetPosition - part. transform. position ). normalized;
            float distance = Vector3. Distance (part. transform. position, _targetPosition);

            // 如果接近目标位置，停止移动
            if ( distance < 0.1f )
            {
                _isMoving = false;
                stateMachine. ChangeState (ArmState. Working);
                return;
            }

            // 执行移动
            part. transform. position += direction * _moveSpeed * Time. deltaTime;
        }

        public void ExitState (ArmStateMachine stateMachine)
        {
            Debug. Log ("机械臂离开移动状态");
            _isMoving = false;
        }

        private Vector3 GetTargetPosition ()
        {
            // 这里实现获取目标位置的逻辑
            // 可以是从配置、用户输入或其他系统获取
            return part. transform. position + Vector3. forward * 5f; // 示例：向前移动5个单位
        }
    }
    public class WorkingStateHandler : IArmStateHandler
    {
        private float _workDuration = 5.0f; // 工作持续时间
        private float _currentWorkTime = 0f;
        private bool _isWorking = false;

        public void EnterState (ArmStateMachine stateMachine)
        {
            Debug. Log ("机械臂进入工作状态");
            _isWorking = true;
            _currentWorkTime = 0f;
            StartWork ();
        }

        public void UpdateState (ArmStateMachine stateMachine)
        {
            if ( !_isWorking )
                return;

            _currentWorkTime += Time. deltaTime;

            // 检查工作是否完成
            if ( _currentWorkTime >= _workDuration )
            {
                _isWorking = false;
                stateMachine. ChangeState (ArmState. Idle);
                return;
            }

            // 执行工作逻辑
            PerformWork ();
        }

        public void ExitState (ArmStateMachine stateMachine)
        {
            Debug. Log ("机械臂离开工作状态");
            _isWorking = false;
            StopWork ();
        }

        private void StartWork ()
        {
            // 开始工作，例如启动机械臂的执行器
            Debug. Log ("开始执行工作任务");
        }

        private void PerformWork ()
        {
            // 执行具体的工作逻辑
            // 例如：抓取、放置、操作等
        }

        private void StopWork ()
        {
            // 停止工作，关闭执行器
            Debug. Log ("工作任务完成");
        }
    }
    public class ResettingStateHandler : IArmStateHandler
    {
        private float _resetDuration = 3.0f; // 重置持续时间
        private float _currentResetTime = 0f;
        private bool _isResetting = false;

        public void EnterState (ArmStateMachine stateMachine)
        {
            Debug. Log ("机械臂进入重置状态");
            _isResetting = true;
            _currentResetTime = 0f;
            StartReset ();
        }

        public void UpdateState (ArmStateMachine stateMachine)
        {
            if ( !_isResetting )
                return;

            _currentResetTime += Time. deltaTime;

            // 检查重置是否完成
            if ( _currentResetTime >= _resetDuration )
            {
                _isResetting = false;
                stateMachine. ChangeState (ArmState. Idle);
                return;
            }

            // 执行重置逻辑
            PerformReset ();
        }

        public void ExitState (ArmStateMachine stateMachine)
        {
            Debug. Log ("机械臂离开重置状态");
            _isResetting = false;
            FinishReset ();
        }

        private void StartReset ()
        {
            // 开始重置，例如关闭所有执行器，回到初始位置
            Debug. Log ("开始重置机械臂");
        }

        private void PerformReset ()
        {
            // 执行具体的重置逻辑
            // 例如：逐步回到初始位置
        }

        private void FinishReset ()
        {
            // 完成重置，清理状态
            Debug. Log ("机械臂重置完成");
        }
    }

    public class IdleStateHandler : IArmStateHandler
    {
        private float _idleTime = 0f;
        private const float IDLE_THRESHOLD = 5f; // 空闲阈值时间

        public void EnterState (ArmStateMachine stateMachine)
        {
            Debug. Log ("机械臂进入空闲状态");
            _idleTime = 0f;
            // 在进入空闲状态时，可以执行一些初始化或重置操作
            // 例如：停止所有运动、重置计时器等
        }

        public void UpdateState (ArmStateMachine stateMachine)
        {
            _idleTime += Time. deltaTime;

            // 在空闲状态下，可以检查是否有新的任务指令
            // 这里可以添加逻辑来检测输入信号、用户指令等

            // 示例：如果有工作信号，切换到工作状态
            if ( HasWorkSignal () )
            {
                stateMachine. ChangeState (ArmState. Working);
            }
            // 示例：如果有移动指令，切换到移动状态
            else if ( HasMovementCommand () )
            {
                stateMachine. ChangeState (ArmState. Moving);
            }
            // 示例：如果收到重置指令，切换到重置状态
            else if ( ShouldReset () )
            {
                stateMachine. ChangeState (ArmState. Resetting);
            }
            // 示例：如果空闲时间过长，可以进入节能模式
            else if ( _idleTime > IDLE_THRESHOLD )
            {
                EnterIdleMode ();
            }
        }

        public void ExitState (ArmStateMachine stateMachine)
        {
            Debug. Log ("机械臂离开空闲状态");
            // 在离开空闲状态时，可以执行一些清理操作
            // 例如：清除待处理的任务、重置标志位等
        }

        private bool HasWorkSignal ()
        {
            // 检查是否有工作信号
            // 这里可以添加实际的检测逻辑
            // 示例：检查输入信号、队列中是否有任务等
            return false; // 临时返回false，实际使用时替换为真实逻辑
        }

        private bool HasMovementCommand ()
        {
            // 检查是否有移动指令
            // 示例：检查目标位置是否已设定
            return false; // 临时返回false，实际使用时替换为真实逻辑
        }

        private bool ShouldReset ()
        {
            // 检查是否需要重置
            // 示例：检查是否有重置标志被触发
            return false; // 临时返回false，实际使用时替换为真实逻辑
        }

        private void EnterIdleMode ()
        {
            // 进入空闲模式，例如降低能耗、进入待机状态等
            Debug. Log ("机械臂进入空闲模式");
        }
    }

    public class ArmStateMachine : MonoBehaviour
    {
        private ArmState _currentState = ArmState.Idle;
        private Dictionary<ArmState, IArmStateHandler> _stateHandlers;

        private void Awake ()
        {
            _stateHandlers = new Dictionary<ArmState, IArmStateHandler>
            {
                { ArmState.Idle, new IdleStateHandler() },
                { ArmState.Moving, new MovingStateHandler() },
                { ArmState.Working, new WorkingStateHandler() },
                { ArmState.Resetting, new ResettingStateHandler() },
                { ArmState.Error, new ErrorStateHandler() }
            };
        }

        private void Update ()
        {
            if ( _stateHandlers. ContainsKey (_currentState) )
            {
                _stateHandlers[_currentState]. UpdateState (this);
            }
            else
            {
                Debug. LogError ($"状态机中未注册当前状态: {_currentState}");
                ChangeState (ArmState. Error);  // 自动切换到错误状态
            }
        }

        public void ChangeState (ArmState newState)
        {
            // 检查新状态是否有效
            if ( !_stateHandlers. ContainsKey (newState) )
            {
                Debug. LogError ($"无法切换到未注册的状态: {newState}");
                newState = ArmState. Error;
            }

            // 执行状态退出和进入
            _stateHandlers[_currentState]?.ExitState (this);
            _currentState = newState;
            _stateHandlers[_currentState]?.EnterState (this);
        }

        public ArmState GetCurrentState ()
        {
            return _currentState;
        }
    }

    public interface IArmStateHandler
    {
        void EnterState (ArmStateMachine stateMachine);
        void UpdateState (ArmStateMachine stateMachine);
        void ExitState (ArmStateMachine stateMachine);
    }
}
