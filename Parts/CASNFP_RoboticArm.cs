using Expansions;
using Expansions. Serenity;
using System;
using System. Collections;
using System. Collections. Generic;
using System. Linq;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Parts
{
    public abstract class CASNFP_RoboticArmBase : PartModule
    {
        // 机械臂状态枚举，表示机械臂的当前状态。
        public enum RoboticArmState 
        {
            Retract = 1, //收回状态，机械臂处于收起状态。
            Extend,      //展开状态，机械臂处于展开状态。
            Moving,      //移动状态，机械臂正在移动到指定位置。
            Working      //工作状态，机械臂正在执行任务。
        }

        private RoboticArmState _robotiArmState;

        //获取当前机械臂状态，供其他模块调用。
        public RoboticArmState RobotArmState
        {
            get
            {
                return _robotiArmState;
            }
            set
            {
                if ( _robotiArmState != value )
                {
                    _robotiArmState = value;
                    OnArmStateChange (); //触发状态变化事件
                }
            }
        }

        //读取cfg中从根部到顶部各关节名称汇总字符串。
        [KSPField]
        public string servoHingeTransformNames = ""; 
        
        //按顺序存储各关节BaseServo的数组。
        protected BaseServo[] _servoModules; 
        //将cfg中设定的关节名称用“,”拆分，排序存储。
        private string[] _servoHingeTransformNames; 

        //右键菜单“自动化取样”
        [KSPField (isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "机械臂工作开关")]
        [UI_Toggle (disabledText = "关闭", scene = UI_Scene. Flight, enabledText = "开始", affectSymCounterparts = UI_Scene. Flight)]
        public bool armStartWork = false;

        //机械臂状态UI字符串，显示在右键菜单中。
        [KSPField (isPersistant = true, guiFormat = "N1", guiActive = true, guiActiveEditor = false, guiName = "机械臂状态")]
        public string armStateUIString = "机械臂处于收起状态";

        //记录发射时的关节角度。
        protected float[] _originalAngles;

        //记录机械臂伸展时的关节角度，由于机械臂模型不同，子类必须自行实现。
        protected abstract float[] ExtendAngles
        {
            get;
            set;
        }
        //是否可以设置目标位置，默认为false。
        protected bool canSetTargetPos = false; 
        
        // 机械臂动作委托，传入部件和机械臂状态。
        protected Action<Part, RoboticArmState> armAction;

        //检测是否安装了“破土重生”DLC，或者部件cfg中是否含有关节名称设置，没有则不启动这个mod
        private void Start ()
        {
            if ( !ExpansionsLoader. IsExpansionInstalled ("Serenity") && HighLogic. LoadedSceneIsGame )
            {
                Debug. Log ("[CASNFP_RoboticArm]:DLC “Serenity”not installed! or currentScene not game Scene!");
                Destroy (this);
            }
        }

        //初始化模块，设置机械臂状态和关节模块。
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            //如果armAction为null，则初始化armAction委托。
            if ( armAction == null )
            {
                armAction = new Action<Part, RoboticArmState> (ArmMovement);
            }
            //如果servoHingeTransformNames不为空且_servoHingeTransformNames为null，则拆分字符串并赋值给_servoHingeTransformNames。
            if ( !string. IsNullOrEmpty (servoHingeTransformNames) && _servoHingeTransformNames == null )
            {
                _servoHingeTransformNames = servoHingeTransformNames. Split (new[] { ',' }, StringSplitOptions. RemoveEmptyEntries);
                Debug. Log ("[CASNFP_RoboticArm]:cfg set correct");
            }
            else
            {
                if ( string. IsNullOrEmpty (servoHingeTransformNames) )
                {
                    Debug. Log ("[CASNFP_RoboticArm]:cfg set error! servoHingeTransformNames is NullOrEmpty");
                    Destroy (this);
                    return;
                }
            }
            //如果_servoModules为null，则查找机械臂关节模块。
            if ( _servoModules == null )
            {
                if ( FindServoModules () )
                {
                    Debug. Log ($"[CASNFP_RoboticArm] Found {_servoModules. Length} servo modules!");
                    GameEvents. onGameSceneSwitchRequested. Add (OnGameSceneSwitchRequested);
                    GameEvents. onFlightReady. Add (OnFlightReady);
                    GameEvents. onPartDestroyed. Add (OnPartDestroyed);
                    Fields["armStartWork"]. OnValueModified += OnArmStartWorkButtonWasModified;
                    RobotArmState = RoboticArmState. Retract; //初始化机械臂状态为收回
                }
                else
                {
                    Debug. Log ($"[CASNFP_RoboticArm] Found {_servoModules. Length} servo modules,DOF is error,CASNFP_Arm_SampleArm boot failed!");
                    Destroy (this);
                }
            }
        }

        //机械臂动作方法，传入部件和机械臂状态，根据状态执行相应的动作。
        private void ArmMovement (Part part, RoboticArmState roboticArmState)
        {
            switch ( roboticArmState )
            {
                case RoboticArmState. Retract:
                    StartCoroutine (DoRetract (part));
                    break;
                case RoboticArmState. Extend:
                    StartCoroutine (DoExtend (part));
                    break;
                case RoboticArmState. Moving:
                    StartCoroutine (DoMoving (part));
                    break;
                case RoboticArmState. Working:
                    StartCoroutine (DoWorking (part));
                    break;
                default:
                    Debug. Log ("[CASNFP_RoboticArm] 机械臂接收到未知动作指令，请检查代码");
                    break;
            }
        }

        //机械臂状态变化时触发的事件
        protected virtual void OnArmStateChange ()
        {
            switch ( RobotArmState )
            {
                case RoboticArmState. Retract:
                    armStateUIString = "机械臂处于收起状态";
                    break;
                case RoboticArmState. Extend:
                    armStateUIString = "机械臂处于展开状态";
                    break;
                case RoboticArmState. Moving:
                    armStateUIString = "机械臂处于移动状态";
                    break;
                case RoboticArmState. Working:
                    armStateUIString = "机械臂处于工作状态";
                    break;
            }
        }

        //机械臂展开状态
        protected virtual IEnumerator DoExtend (Part part)
        {
            if ( RobotArmState == RoboticArmState. Extend )yield break; //如果已经是展开状态，则直接返回
            canSetTargetPos = false; //禁止设置目标位置
            Debug. Log ("[CASNFP_RoboticArm] 机械臂开始展开");
            if (RobotArmState == RoboticArmState.Moving)
            {
                yield return new WaitUntil(() => RobotArmState != RoboticArmState.Moving);
            }
            //这里增加展开逻辑，确保机械臂处于可展开状态
            for ( int i = 0 ; i < _servoModules. Length ; i++ )
            {
                var servo = _servoModules[i];
                if ( ExtendAngles == null || ExtendAngles. Length != _servoModules. Length )
                {
                    Debug. LogError ("[CASNFP_RoboticArm] ExtendAngles is null or length not match!");
                    yield break; //如果目标角度数组为空或长度不匹配，则直接返回
                }
                float extendAngle = ExtendAngles[i];
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
            RobotArmState = RoboticArmState. Moving; //设置状态为移动
            yield return new WaitUntil (() =>
                _servoModules. All (servo =>
                {
                    switch ( servo )
                    {
                        case ModuleRoboticRotationServo rotationServo:
                            return Math. Abs (rotationServo. currentAngle - rotationServo. targetAngle) < 0.5f;
                        case ModuleRoboticServoHinge servoHinge:
                            return Math. Abs (servoHinge. currentAngle - servoHinge. targetAngle) < 0.5f;
                        default:
                            return false;
                    }
                }));
            yield return 1f; // 等待一帧，确保所有关节都已到达目标角度
            Debug. Log ("[CASNFP_RoboticArm] 机械臂已展开");
            RobotArmState = RoboticArmState. Extend; //设置状态为展开
            canSetTargetPos = true; //允许设置目标位置
        }

        //机械臂工作状态
        protected virtual IEnumerator DoWorking (Part part)
        {
            yield return 1f; //模拟机械臂工作状态的延时
            Debug. Log ("[CASNFP_RoboticArm] 机械臂正在工作");
        }
        
        
        //机械臂移动状态
        protected virtual IEnumerator DoMoving (Part part)
        {
            if(RobotArmState == RoboticArmState. Moving) yield break; //如果已经是移动状态，则直接返回
            if(RobotArmState != RoboticArmState. Extend)
            {
                armAction.Invoke(part,RoboticArmState.Extend); //如果不是展开状态，则先将机械臂重置为展开状态
            }
            canSetTargetPos = false; //禁止设置目标位置
            Debug. Log ("[CASNFP_RoboticArm] 机械臂开始移动至目标点");
            for ( int i = 0 ; i < _servoModules. Length ; i++ )
            {
                var servo = _servoModules[i];
                //反向运动学计算目标角度
                if ( !_InverseKinematicsResult. success )
                {
                    Debug. LogError ("[CASNFP_RoboticArm] 反向运动学计算失败，无法移动机械臂");
                    yield break; //如果反向运动学计算失败，则直接返回
                }
                if ( _InverseKinematicsResult.angles == null || _InverseKinematicsResult. angles. Length != _servoModules. Length)
                {
                    Debug. LogError ("[CASNFP_RoboticArm] TargetAngles is null or length not match!");
                    yield break; //如果目标角度数组为空或长度不匹配，则直接返回
                }
                float targetAngle = _InverseKinematicsResult. angles[i];
                Debug. Log ($"[CASNFP_RoboticArm] 机械臂关节{i}目标角度: {targetAngle}");
                switch ( servo )
                {
                    case ModuleRoboticRotationServo rotationServo:
                        rotationServo. targetAngle = targetAngle; // 设置目标角度为发射时的角度
                        break;
                    case ModuleRoboticServoHinge servoHinge:
                        servoHinge. targetAngle = targetAngle; // 设置目标角度为发射时的角度
                        break;
                }
            }
            RobotArmState = RoboticArmState. Moving; //设置状态为移动
            // 等待所有关节到达目标角度
            yield return new WaitUntil (() =>
                _servoModules. All (servo =>
                {
                    switch ( servo )
                    {
                        case ModuleRoboticRotationServo rotationServo:
                            return Math. Abs (rotationServo. currentAngle - rotationServo. targetAngle) < 0.5f;
                        case ModuleRoboticServoHinge servoHinge:
                            return Math. Abs (servoHinge. currentAngle - servoHinge. targetAngle) < 0.5f;
                        default:
                            return false;
                    }
                }));
            yield return 1f; // 等待一帧，确保所有关节都已到达目标角度
            Debug. Log ("[CASNFP_RoboticArm] 机械臂已到达目标点");
            RobotArmState = RoboticArmState.Working; //设置状态为工作
        }

        //机械臂收回状态
        protected virtual IEnumerator DoRetract (Part part)
        {
            if ( RobotArmState == RoboticArmState. Retract ) yield break;// 如果已经是收回状态，则直接返回
            canSetTargetPos = false; //禁止设置目标位置
            Debug. Log ("[CASNFP_RoboticArm] 机械臂开始收回");
            if (RobotArmState == RoboticArmState.Moving)
            {
                yield return new WaitUntil (()=>RobotArmState != RoboticArmState.Moving);
            }
            //先执行展开逻辑，确保机械臂处于可收回状态
            if ( RobotArmState != RoboticArmState. Extend )
            {
                armAction. Invoke (this. part, RoboticArmState. Extend);
            }

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
            RobotArmState = RoboticArmState. Moving; //设置状态为移动
            // 等待所有关节到达目标角度
            yield return new WaitUntil (() =>
                _servoModules. All (servo =>
                {
                    switch ( servo )
                    {
                        case ModuleRoboticRotationServo rotationServo:
                            return Math. Abs (rotationServo. currentAngle - rotationServo. targetAngle) < 0.5f;
                        case ModuleRoboticServoHinge servoHinge:
                            return Math. Abs (servoHinge. currentAngle - servoHinge. targetAngle) < 0.5f;
                        default:
                            return false;
                    }
                }));
            yield return 1f; // 等待一帧，确保所有关节都已到达目标角度
            RobotArmState = RoboticArmState. Retract; //重置状态为收回  
        }

        //在场景切换时让机械臂恢复初始状态
        protected void OnGameSceneSwitchRequested (GameEvents. FromToAction<GameScenes, GameScenes> data)
        {
            if (armStartWork )
            {
                Debug. Log ("[CASNFP_RoboticArm] Scene switch requested,reset arm state!");
                Fields["armStartWork"].SetValue (false, this); //重置机械臂工作状态
            }
        }

        public void OnArmStartWorkButtonWasModified (object obj)
        {
            if ( armStartWork )
            {
                Debug. Log ("[CASNFP_RoboticArm] 机械臂开始工作");
                //这里可以添加机械臂开始工作的逻辑
                if ( RobotArmState != RoboticArmState. Extend )
                {
                    Debug. Log ("[CASNFP_RoboticArm] 机械臂展开");
                    armAction. Invoke (this.part, RoboticArmState. Extend); //执行机械臂展开动作
                }
                ArmStartWork ();
            }
            else
            {
                //这里可以添加机械臂停止工作的逻辑
                Debug. Log ("[CASNFP_RoboticArm] 机械臂停止工作");
                if ( RobotArmState != RoboticArmState. Retract )
                    armAction. Invoke (this.part, RoboticArmState. Retract); //设置状态为收回
                ArmStopWork ();
            }
        }

        //查找并初始化关节模块
        protected virtual  bool FindServoModules ()
        {
            var servoList = new List<BaseServo> ();
            var angleList = new List<float> ();
            foreach ( PartModule module in part. Modules )
            {
                if ( module is BaseServo baseServo )
                {
                    string servoName = null;
                    float angle = 0f;
                    if ( module is ModuleRoboticRotationServo rotationServo )
                    {
                        servoName = rotationServo. servoTransformName;
                        angle = rotationServo. currentAngle;
                    }
                    else if ( module is ModuleRoboticServoHinge servoHinge )
                    {
                        servoName = servoHinge. servoTransformName;
                        angle = servoHinge. currentAngle;
                    }
                    if ( !string. IsNullOrEmpty (servoName) && System. Linq. Enumerable. Contains (_servoHingeTransformNames, servoName) )
                    {
                        servoList. Add (baseServo);
                        angleList. Add (angle);
                    }
                }
            }
            _servoModules = servoList. ToArray ();
            _originalAngles = angleList. ToArray ();
            Debug. Log ($"[CASNFP_RoboticArm] _servoModules is set: {_servoModules. Length}");
            return _servoModules. Length == _servoHingeTransformNames. Length;
        }

        //设置关节刚体连接
        protected virtual void OnFlightReady ()
        {
            for ( int i = 1 ; i < _servoModules. Length ; i++ )
            {
                var current = _servoModules[i];
                var previous = _servoModules[i - 1];
                current. MovingObject (). GetComponent<ConfigurableJoint> (). connectedBody =
                    previous. MovingObject (). GetComponent<Rigidbody> ();
            }
            Debug. Log ("[CASNFP_RoboticArm] 刚体连接设置完成");
            foreach (var item in _originalAngles)
            {
                Debug.Log("originalAngles=" +item);
            }
            foreach (var item in ExtendAngles)
            {
                Debug.Log("ExtendAngles=" +item);
            }
            Debug. Log ("[CASNFP_RoboticArm] 刚体连接设置完成");
        }

        //注销监听的事件
        private void OnPartDestroyed (Part data)
        {
            if ( data == part )
            {
                GameEvents. onFlightReady. Remove (OnFlightReady);
                GameEvents. onPartDestroyed. Remove (OnPartDestroyed);
                Fields["armStartWork"]. OnValueModified -= OnArmStartWorkButtonWasModified;
            }
        }

        //销毁时注销监听的事件
        private void OnDestroy ()
        {
            GameEvents. onFlightReady. Remove (OnFlightReady);
            GameEvents. onPartDestroyed. Remove (OnPartDestroyed);
            Fields["armStartWork"]. OnValueModified -= OnArmStartWorkButtonWasModified;
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            Debug.Log("[CASNFP_RoboticArm] Saving arm state to config node.");
            // 这里可以添加保存机械臂状态的逻辑
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            Debug.Log("[CASNFP_RoboticArm] Loading arm state from config node.");
            // 这里可以添加加载机械臂状态的逻辑
        }

        protected struct InverseKinematicsResult
        {
            public bool success;
            public float[] angles;
        }

        protected abstract InverseKinematicsResult _InverseKinematicsResult {
            get;
            set;
        }
        protected virtual void ArmStartWork ()
        {
        } //机械臂开始工作，可以设置灯光闪烁或者其他功能，子类可选择实现具体功能。

        protected virtual void ArmStopWork ()
        {
        }//机械臂结束工作，可以设置灯光闪烁或者其他功能，子类可选择实现具体功能。
    }
}