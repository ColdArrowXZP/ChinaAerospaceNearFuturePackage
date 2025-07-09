using UnityEngine;
using Expansions.Serenity;
using System;
using System.Collections.Generic;
using KSP.Localization;
using System.Collections;
using Expansions;
using System.Linq;
using Expansions. Missions. Editor;
using UniLinq;
using UnityEngine. UI;
namespace ChinaAeroSpaceNearFuturePackage.Parts
{
    public abstract class CASNFP_RoboticArm : PartModule
    {
        public enum RoboticArmState //机械臂状态枚举，表示机械臂的当前状态。
        {
            Retract = 1,//收回状态，机械臂处于收起状态。
            Extend,//展开状态，机械臂处于展开状态。
            Moving,//移动状态，机械臂正在移动到指定位置。
            Working//工作状态，机械臂正在执行任务。
        }
        [KSPField]
        public string servoHingeTransformNames = "";//读取cfg中从根部到顶部各关节名称汇总字符串。

        protected BaseServo[] _servoModules;//按顺序存储各关节BaseServo的数组。

        protected string[] _servoHingeTransformNames;//将cfg中设定的关节名称用“,”拆分，排序存储。

        //右键菜单“自动化取样”
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "机械臂工作开关")]
        [UI_Toggle(disabledText = "关闭", scene = UI_Scene.Flight, enabledText = "开始", affectSymCounterparts = UI_Scene.Flight)]
        public bool armStartWork = false;

        [KSPField(isPersistant = true)]
        private RoboticArmState _robotiArmState;

        //获取当前机械臂状态，供其他模块调用。
        public RoboticArmState RobotiArmState
        {
            get 
            {
                return _robotiArmState; 
            }
            set
            {
                _robotiArmState = value;
            }
        }
        [KSPField (isPersistant = true)]
        private float[] _originalAngles;//记录发射时的关节角度。

        [KSPField (isPersistant = true)]
        protected abstract float[] _extendAngles{get;}//记录机械臂伸展时的关节角度。

        //检测是否安装了“破土重生”DLC，或者部件cfg中是否含有关节名称设置，没有则不启动这个mod
        private void Start ()
        {
            if ( !ExpansionsLoader.IsExpansionInstalled ("Serenity") && HighLogic.LoadedSceneIsGame)
            {
                Debug. Log ("[CASNFP_RoboticArm]:DLC “Serenity”not installed! or currentScene not game Scene!");
                Destroy (this);
            }
        }
        //在飞行场景中查找关节并设置为node1~4。
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            if ( !string.IsNullOrEmpty (servoHingeTransformNames) && _servoHingeTransformNames == null)
            {
                _servoHingeTransformNames = servoHingeTransformNames.Split(new[] {','},StringSplitOptions.RemoveEmptyEntries);
                Debug. Log ("[CASNFP_RoboticArm]:cfg set correct");
            }
            else
            {
                if ( string.IsNullOrEmpty (servoHingeTransformNames) )
                {
                    Debug. Log ("[CASNFP_RoboticArm]:cfg set error! servoHingeTransformNames is NullOrEmpty");
                    Destroy (this);
                    return;
                } 
            }
            //获取所有关节模块
            if ( _servoModules == null)
            {
                 if ( FindServoModules () )
                {
                    Debug. Log ($"[CASNFP_RoboticArm] Found {_servoModules.Length} servo modules!");
                    GameEvents. onGameSceneSwitchRequested.Add(OnGameSceneSwitchRequested) ;
                    GameEvents.onFlightReady.Add (OnFlightReady);
                    GameEvents.onPartDestroyed.Add (OnPartDestroyed);
                    Fields["armStartWork"]. OnValueModified += OnArmStartWorkButtonWasModified;
                }
                else
                {
                    Debug. Log ($"[CASNFP_RoboticArm] Found {_servoModules.Length} servo modules,DOF is error,CASNFP_Arm_SampleArm boot failed!");
                    Destroy (this);
                }
            }
        }

        private bool canSetTargetPos = false; //是否可以设置目标位置，默认为false。

        //监听机械臂状态变化事件
        private void OnArmStateChange (object arg)
        {
            switch(RobotiArmState)
            {
                case RoboticArmState. Retract:
                    StartCoroutine(DoRetract()) ;
                    Debug. Log ("[CASNFP_RoboticArm] 机械臂状态变为收回");
                    break;
                case RoboticArmState. Extend:
                    StartCoroutine (DoExtend ());
                    Debug. Log ("[CASNFP_RoboticArm] 机械臂状态变为伸展");
                    break;
                case RoboticArmState. Moving:
                    StartCoroutine (DoMoving ());
                    Debug. Log ("[CASNFP_RoboticArm] 机械臂状态变为移动");
                    break;
                case RoboticArmState. Working:
                    StartCoroutine (DoWorking ());
                    Debug. Log ("[CASNFP_RoboticArm] 机械臂状态变为工作");
                    break;
            }
        }
        //机械臂展开状态
        protected virtual IEnumerator DoExtend ()
        {
            canSetTargetPos = false; //禁止设置目标位置
            Debug. Log ("[CASNFP_RoboticArm] 机械臂开始展开");
            for ( int i = 0 ; i < _servoModules. Length ; i++ )
            {
                var servo = _servoModules[i];
                float extendAngle = _extendAngles[i];

                switch ( servo )
                {
                    case ModuleRoboticRotationServo rotationServo:
                        rotationServo. targetAngle = extendAngle; // 设置目标角度为发射时的角度
                        break;
                    case ModuleRoboticServoHinge servoHinge:
                        servoHinge. targetAngle = extendAngle; // 设置目标角度为发射时的角度
                        break;
                }
            }
            // 等待所有关节到达目标角度
            yield return new WaitUntil (() =>
               System. Linq. Enumerable. All (_servoModules, servo =>
               {
                   switch ( servo )
                   {
                       case ModuleRoboticRotationServo rotationServo:
                           return Math. Abs (rotationServo. currentAngle - rotationServo. targetAngle) < 0.1f;
                       case ModuleRoboticServoHinge servoHinge:
                           return Math. Abs (servoHinge. currentAngle - servoHinge. targetAngle) < 0.1f;
                       default:
                           return false;
                   }
               }));
            yield return 1f; // 等待一帧，确保所有关节都已到达目标角度
            Debug. Log ("[CASNFP_RoboticArm] 机械臂已展开");
            canSetTargetPos = true;//允许设置目标位置
        }
        //机械臂工作状态
        protected virtual IEnumerator DoWorking ()
        {
            yield return 1f;//模拟机械臂工作状态的延时
            Debug. Log ("[CASNFP_RoboticArm] 机械臂正在工作");
        }

        //机械臂移动状态
        protected virtual  IEnumerator DoMoving ()
        {
            canSetTargetPos = false; //禁止设置目标位置
            if ( !HighLogic. LoadedSceneIsFlight )
               yield break;
            if ( _servoModules == null || _servoModules. Length == 0 )
            {
                Debug. Log ("[CASNFP_RoboticArm] No servo modules found, cannot move!");
                yield break;
            }
            //获取目标位置
            Vector3 targetPos = targetPosition;
            //计算逆运动学
            InverseKinematicsResult result = CalculateInverseKinematics (targetPos);
            if ( !result. success )
            {
                Debug. Log ("[CASNFP_RoboticArm] Inverse kinematics calculation failed!");
                yield break;
            }
            //设置各关节目标角度
            for ( int i = 0 ; i < _servoModules. Length ; i++ )
            {
                var servo = _servoModules[i];
                float targetAngle = result. angles[i];
                switch ( servo )
                {
                    case ModuleRoboticRotationServo rotationServo:
                        rotationServo. targetAngle = targetAngle; // 设置目标角度
                        break;
                    case ModuleRoboticServoHinge servoHinge:
                        servoHinge. targetAngle = targetAngle; // 设置目标角度
                        break;
                }
            }
            // 等待所有关节到达目标角度
            yield return new WaitUntil (() =>
                System. Linq. Enumerable. All (_servoModules, servo =>
                {
                    switch ( servo )
                    {
                        case ModuleRoboticRotationServo rotationServo:
                            return Math. Abs (rotationServo. currentAngle - rotationServo. targetAngle) < 0.1f;
                        case ModuleRoboticServoHinge servoHinge:
                            return Math. Abs (servoHinge. currentAngle - servoHinge. targetAngle) < 0.1f;
                        default:
                            return false;
                    }
                }));
            yield return 1f; // 等待一帧，确保所有关节都已到达目标角度
            RobotiArmState = RoboticArmState. Working; //设置状态为工作
        }

        //机械臂收回状态
        protected virtual IEnumerator DoRetract ()
        {
            canSetTargetPos = false; //禁止设置目标位置
            Debug. Log ("[CASNFP_RoboticArm] 机械臂开始收回");
            DoExtend (); //先执行展开逻辑，确保机械臂处于可收回状态
            //这里可以添加机械臂收回的逻辑
            for ( int i = 0 ; i < _servoModules. Length ; i++ )
            {
                var servo = _servoModules[i];
                float originalAngle = _originalAngles[i];

                switch ( servo )
                {
                    case ModuleRoboticRotationServo rotationServo:
                        rotationServo. targetAngle = originalAngle; // 设置目标角度为发射时的角度
                        break;
                    case ModuleRoboticServoHinge servoHinge:
                        servoHinge. targetAngle = originalAngle; // 设置目标角度为发射时的角度
                        break;
                }
            }
            // 等待所有关节到达目标角度
            yield return new WaitUntil (() =>
               System. Linq. Enumerable. All (_servoModules, servo =>
               {
                   switch ( servo )
                   {
                       case ModuleRoboticRotationServo rotationServo:
                           return Math. Abs (rotationServo. currentAngle - rotationServo. targetAngle) < 0.1f;
                       case ModuleRoboticServoHinge servoHinge:
                           return Math. Abs (servoHinge. currentAngle - servoHinge. targetAngle) < 0.1f;
                       default:
                           return false;
                   }
               }));
            yield return 1f; // 等待一帧，确保所有关节都已到达目标角度
            RobotiArmState = RoboticArmState. Retract; //重置状态为收回
        }

        //在场景切换时让机械臂恢复初始状态
        private void OnGameSceneSwitchRequested (GameEvents. FromToAction<GameScenes, GameScenes> data)
        {
            if ( data. from == GameScenes. FLIGHT && armStartWork)
            {
                Debug. Log ("[CASNFP_RoboticArm] Scene switch requested,reset arm state!");
                armStartWork = false; //重置机械臂工作状态
                if ( RobotiArmState != RoboticArmState. Retract )
                {
                   RobotiArmState = RoboticArmState. Retract; //重置机械臂状态为收回
                }
            }
        }

        private void OnArmStartWorkButtonWasModified (object arg1)
        {
            if ( armStartWork )
            {
                Fields["_robotiArmState"]. OnValueModified += OnArmStateChange;
                Debug. Log ("[CASNFP_RoboticArm] 机械臂开始工作");
                //这里可以添加机械臂开始工作的逻辑
                if ( RobotiArmState != RoboticArmState. Extend )
                {
                    RobotiArmState = RoboticArmState. Extend; //设置机械臂状态为展开
                }
                ArmStartWork ();
            }
            else
            {
                //这里可以添加机械臂停止工作的逻辑
                Debug. Log ("[CASNFP_RoboticArm] 机械臂停止工作");
                if (RobotiArmState != RoboticArmState.Retract )
                    RobotiArmState = RoboticArmState. Retract; //设置状态为收回
                ArmStopWork ();
                Fields["_robotiArmState"]. OnValueModified -= OnArmStateChange;
            }
        }

        //查找并初始化关节模块
        private bool FindServoModules()
        {
            var servoList = new List<BaseServo>();
            var angleList = new List<float> ();
            foreach (PartModule module in part.Modules)
            {
                if (module is BaseServo baseServo)
                {
                    string servoName = null;
                    float angle = 0f;
                    if ( module is ModuleRoboticRotationServo rotationServo )
                    {
                        servoName = rotationServo. servoTransformName;
                        angle = rotationServo.currentAngle;
                    }
                    else if ( module is ModuleRoboticServoHinge servoHinge )
                    {
                        servoName = servoHinge.servoTransformName;
                        angle = servoHinge. currentAngle;
                    }
                    if ( !string. IsNullOrEmpty (servoName) && System.Linq.Enumerable.Contains(_servoHingeTransformNames,servoName) )
                    {
                        servoList.Add(baseServo);
                        angleList. Add (angle);
                    } 
                }
            }
            _servoModules = servoList.ToArray();
            _originalAngles = angleList.ToArray();
            Debug.Log($"[CASNFP_RoboticArm] _servoModules is set: {_servoModules.Length}");
            return _servoModules.Length == _servoHingeTransformNames.Length;
        }
        //设置关节刚体连接
        protected virtual void OnFlightReady ()
        {
            for (int i = 1; i < _servoModules.Length; i++)
            {
                var current = _servoModules[i];
                var previous = _servoModules[i - 1];
                current.MovingObject().GetComponent<ConfigurableJoint>().connectedBody =
                    previous.MovingObject().GetComponent<Rigidbody>();
            }
            Debug.Log("[CASNFP_RoboticArm] 刚体连接设置完成");
        }
        //每帧更新
        public override void OnUpdate ()
        {
            base. OnUpdate ();
            if ( canSetTargetPos )
            {
                Debug. Log ("[CASNFP_RoboticArm] 机械臂已展开，请选择目标点");
            }
        }
        //注销监听的事件
        protected virtual void OnPartDestroyed(Part data)
        {
            if ( data == part )
            {
                
                GameEvents. onFlightReady. Remove (OnFlightReady);
                GameEvents. onPartDestroyed. Remove (OnPartDestroyed);
                Fields["armStartWork"]. OnValueModified -= OnArmStartWorkButtonWasModified;
            }
        }
        //销毁时注销监听的事件
        protected virtual void OnDestroy()
        {
            GameEvents.onFlightReady.Remove(OnFlightReady);
            GameEvents.onPartDestroyed.Remove(OnPartDestroyed);
            Fields["armStartWork"]. OnValueModified -= OnArmStartWorkButtonWasModified;
        }
        
        protected abstract Vector3 targetPosition { get; }//设置目标位置，返回值为目标位置。
          
        protected abstract float GetMaxReachDistance ();//获取机械臂的最大作用范围，返回值为最大作用范围。

        protected struct InverseKinematicsResult
        {
            public bool success; //是否成功计算逆运动学
            public float[] angles; //关节角度数组
        }
        protected abstract InverseKinematicsResult CalculateInverseKinematics (Vector3 targetPos);//计算逆运动学，获返回当前机械臂的关节角度数组，返回值为null表示获取失败。

        protected abstract void ArmStartWork ();//机械臂开始工作，子类实现具体功能。

        protected abstract void ArmStopWork ();//机械臂开始工作，子类实现具体功能。
    }


}









