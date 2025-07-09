using UnityEngine;
using Expansions.Serenity;
using System;
using System.Collections.Generic;
using KSP.Localization;
using System.Collections;
using Expansions;
using static Targeting;
using System.Linq;
namespace ChinaAeroSpaceNearFuturePackage.Parts
{
    public abstract class CASNFP_RoboticArm : PartModule
    {
        [KSPField]
        public string servoHingeTransformNames = "";//读取cfg中从根部到顶部各关节名称汇总字符串。

        protected BaseServo[] _servoModules;//按顺序存储各关节BaseServo的数组。

        protected string[] _servoHingeTransformNames;//将cfg中设定的关节名称用“,”拆分，排序存储。

        //右键菜单“自动化取样”
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "取样开关")]
        [UI_Toggle(disabledText = "关闭", scene = UI_Scene.Flight, enabledText = "开始", affectSymCounterparts = UI_Scene.Flight)]
        public bool startSample = false;

        //右键菜单显示“在厂房内不得使用机械臂的”警告信息
        [KSPField(isPersistant = true,guiActive = false,guiActiveEditor = true,guiName = "操作警告")]
        public string operationWarning = "在厂房内不得使用机械臂的！以避免伤害到坎巴拉子民。";

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
                _servoHingeTransformNames = servoHingeTransformNames.Split(',').Select(s => s.Trim()).ToArray();
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
                    GameEvents.onFlightReady.Add (OnFlightReady);
                    GameEvents.onPartDestroyed.Add (OnPartDestroyed);

                }
                else
                {
                    Debug. Log ($"[CASNFP_RoboticArm] Found {_servoModules.Length} servo modules,DOF is error,CASNFP_Arm_SampleArm boot failed!");
                    Destroy (this);
                }
            }
        }
        //查找并初始化关节模块
        private bool FindServoModules()
        {
            var servoList = new List<BaseServo>();
            var excludeFields = new HashSet<string> { nameof(startSample), nameof(operationWarning) };// 排除不需要关闭显示的右键菜单字段
            foreach (PartModule module in part.Modules)
            {
                if (module is BaseServo baseServo)
                {
                    foreach (BaseField field in module.Fields)
                    {
                        if (!excludeFields.Contains(field.FieldInfo.Name))
                        {
                            field.guiActive = false;
                            field.guiActiveEditor = false;
                        }
                    }
                    foreach (var item in module.Events)
                    {
                        item.guiActive = false;
                        item.guiActiveEditor = false;
                    }

                    string servoName = null;
                    if (module is ModuleRoboticRotationServo rotationServo)
                        servoName = rotationServo.servoTransformName;
                    else if (module is ModuleRoboticServoHinge servoHinge)
                        servoName = servoHinge.servoTransformName;

                    if (!string.IsNullOrEmpty(servoName) && _servoHingeTransformNames.Contains(servoName))
                        servoList.Add(baseServo);
                }
            }
            _servoModules = servoList.ToArray();
            Debug.Log($"[CASNFP_RoboticArm] _servoModules is set: {_servoModules.Length}");
            return _servoModules.Length == _servoHingeTransformNames.Length;
        }
        protected virtual void SetServoRigB ()
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
        protected virtual void OnFlightReady ()
        {
            Debug. Log ("[CASNFP_RoboticArm] Vessel is create,Check rigibody!");
            SetServoRigB ();

        }
        
        //注销监听的事件
        protected virtual void OnPartDestroyed(Part data)
        {
            if ( data == part )
            {
                GameEvents. onFlightReady. Remove (OnFlightReady);
                GameEvents. onPartDestroyed. Remove (OnPartDestroyed);
            }
        }
        //销毁时注销监听的事件
        protected virtual void OnDestroy()
        {
            GameEvents.onFlightReady.Remove(OnFlightReady);
            GameEvents.onPartDestroyed.Remove(OnPartDestroyed);
        }
        protected abstract float[] CalculateInverseKinematics (Vector3 targetPos);
    }


}









