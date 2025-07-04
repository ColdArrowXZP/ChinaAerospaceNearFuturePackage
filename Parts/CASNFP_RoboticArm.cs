using UnityEngine;
using Expansions. Serenity;
using System;
using System. Collections. Generic;
using KSP. Localization;
using System. Collections;
using Expansions;
using static Targeting;
using System. Linq;
namespace ChinaAeroSpaceNearFuturePackage. Parts
{
    public abstract class CASNFP_RoboticArm : PartModule
    {    
        [KSPField]
        public string servoHingeTransformNames = "";//读取cfg中从根部到顶部各关节名称汇总字符串。

        protected BaseServo[] _servoModules;//按顺序存储各关节BaseServo的数组。

        protected string[] _servoHingeTransformNames;//将cfg中设定的关节名称用“,”拆分，排序存储。
        
        //右键菜单“自动化取样”
        [KSPField (isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "取样开关")]
        [UI_Toggle (disabledText = "关闭", scene = UI_Scene. Flight, enabledText = "开始", affectSymCounterparts = UI_Scene. Flight)]
        public bool startSample = false;

        //检测是否安装了“破土重生”DLC，或者部件cfg中是否含有关节名称设置，没有则不启动这个mod
        private void Start ()
        {
            if ( !ExpansionsLoader. IsExpansionInstalled ("Serenity") && HighLogic. LoadedSceneIsGame)
            {
                Debug. Log ("[CASNFP_RoboticArm]:DLC “Serenity”not installed! or currentScene not game Scene!");
                Destroy (this);
            }
        }
        //在飞行场景中查找关节并设置为node1~4。
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            if ( !string. IsNullOrEmpty (servoHingeTransformNames) && _servoHingeTransformNames == null)
            {
                _servoHingeTransformNames = new string[servoHingeTransformNames. Split (','). Length];
                _servoHingeTransformNames = servoHingeTransformNames. Split (',');
                Debug. Log ("[CASNFP_RoboticArm]:cfg set correct");
            }
            else
            {
                if ( string. IsNullOrEmpty (servoHingeTransformNames) )
                {
                    Debug. Log ("[CASNFP_RoboticArm]:cfg set error! servoHingeTransformNames is NullOrEmpty");
                    Destroy (this);
                } 
            }
            //获取所有关节模块
            if ( _servoModules == null)
            {
                 if ( FindServoModules () )
                {
                    Debug. Log ($"[CASNFP_RoboticArm] Found {_servoModules.Length} servo modules!");
                    GameEvents. onFlightReady. Add (OnFlightReady);
                    GameEvents. onPartDestroyed. Add (OnPartDestroyed);

                }
                else
                {
                    Debug. Log ($"[CASNFP_RoboticArm] Found {_servoModules.Length} servo modules,DOF is error,CASNFP_Arm_SampleArm boot failed!");
                    Destroy (this);
                }
            }
        }
        private bool FindServoModules ()
        {
            _servoModules = new BaseServo[_servoHingeTransformNames.Length];
            for ( int i = 0 ; i < part. Modules.Count ; i++ )
            {
                if ( part. Modules[i] is BaseServo )
                {
                    foreach ( BaseField field in part. Modules[i]. Fields )
                    {
                        if ( field.FieldInfo.Name != "startSample")
                        {
                            field. guiActive = false;
                            field. guiActiveEditor = false;
                        }
                    }
                    foreach ( var item in part. Modules[i].Events )
                    {
                        item. guiActive = false;
                        item. guiActiveEditor = false;
                    }
                    
                }else  continue;

                if ( part.Modules[i] is ModuleRoboticRotationServo rotationServo )
                {
                    if ( _servoHingeTransformNames. Contains (rotationServo. servoTransformName) )
                    {
                        _servoModules[i] = rotationServo;
                    }
                    
                }
                else if ( part. Modules[i] is ModuleRoboticServoHinge servoHinge )
                {
                    if ( _servoHingeTransformNames. Contains (servoHinge. servoTransformName) )
                    {
                        _servoModules[i] = servoHinge;
                    }
                }
            }
            Debug. Log ("[CASNFP_RoboticArm] _servoModules is set"+ _servoModules.Length);
            //判断一下关节数组是否存在空元素。
            return  _servoModules.Contains(null) ? false:true;
        }
        protected virtual void SetServoRigB ()
        {
            foreach ( BaseServo baseServo in _servoModules )
            {
                if ( _servoModules. IndexOf (baseServo) <= 0 )
                {
                    continue;
                }
                baseServo. MovingObject (). GetComponent<ConfigurableJoint> (). connectedBody
                    = _servoModules[_servoModules. IndexOf (baseServo) - 1]. MovingObject (). GetComponent<Rigidbody> ();
            }
            Debug. Log ($"[CASNFP_RoboticArm]刚体连接设置完成");
        }
        protected virtual void OnFlightReady ()
        {
            Debug. Log ("[CASNFP_RoboticArm] Vessel is create,Check rigibody!");
            SetServoRigB ();
        }
        //注销监听的事件
        protected virtual void OnPartDestroyed (Part data)
        {
            if ( data == part )
            {
                GameEvents. onFlightReady. Remove (OnFlightReady);
                GameEvents. onPartDestroyed. Remove (OnPartDestroyed);
            }
        }
        protected abstract float[] CalculateInverseKinematics (Vector3 targetPos);
    }


}









