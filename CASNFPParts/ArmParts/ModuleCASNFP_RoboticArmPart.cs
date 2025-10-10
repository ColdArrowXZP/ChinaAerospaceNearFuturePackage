using ChinaAeroSpaceNearFuturePackage. Core. Managers;
using Expansions. Serenity;
using System;
using System. Collections;
using System. Collections. Generic;
using System. Linq;
using UnityEngine;
using UnityEngine. Events;

namespace ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts
{
    public class ModuleCASNFP_RoboticArmPart : PartModule
    {
        [KSPField]
        public int thisPartBelongWorkType = 0; // 机械臂类型索引,0=取样（嫦娥）机械臂,1=吸盘式（天宫）机械臂,2=抓取式机械臂,3=摄像类机械臂，4=其他类型
        [KSPField]
        public string workPosName = "workDrill"; // 机械臂工作端位置名称,用于机械臂工作端位置定位
        [KSPField]
        public string basePosName = "node1"; // 机械臂基座位置名称,用于机械臂基座位置定位
        [KSPField]
        public string baseJointInfo = "node1,10,Z,-180,180,0"; // 机械臂基座位置名称,用于机械臂基座位置定位
        [KSPField]
        public string effectJointInfo = "node4,10,X,-180,180,0"; // 机械臂工作端位置名称,用于机械臂工作端位置定位
        [KSPField]
        public string linkJointsInfo = "node2,5,X,-107,107,0|node3,5,X,-180,180,0"; // 机械臂大臂段位置名称,用“,”隔开,用于机械臂大臂段位置定位
        public Transform workPos;
        public Transform basePos;
        public ArmJoint[] joints;
        public float targetAngle { get; set; }
        public ArmWorkType WorkType
        {
            get
            {
                return ( ArmWorkType )thisPartBelongWorkType;
            }
        }
        private ArmState armState = ArmState.Idle;
        public ArmState ArmState 
        {
            get { return armState; } 
            set
            {
                if (armState != value)
                {
                    OnArmStateChanged?.Fire(value);
                    armState = value;
                }
            } 
        }
        public EventData<ArmState> OnArmStateChanged = new EventData<ArmState> ("OnArmStateChanged");
        public override void OnStart (StartState state)
        {
            base. OnStart (state);
            if ( HighLogic. LoadedScene != GameScenes. FLIGHT )
                return;
            OnArmStateChanged.Add (OnArmStateChangedEvent);
            InstializeJoints ();
        }
        private void OnArmStateChangedEvent(ArmState state)
        {
            //根据状态执行不同的操作,如：机械臂展开、机械臂收回、机械臂工作等。
            switch (state) 
            {
                case ArmState.Idle:
                    Idle();
                    break;
                case ArmState.Retracting:
                    StartCoroutine(RetractArm ());
                    break;
                case ArmState.Extanding:
                    StartCoroutine (ExtendArm (targetAngle));
                    break;
                case ArmState.Doing:
                    Doing ();
                    break;
                default:
                    break;
            }
        }
        public override void OnUpdate ()
        {
            base. OnUpdate ();

        }
        private void Idle() 
        {
            CASNFPLogger.Instance.Log("机械臂处于空闲状态");
        }
        private void Doing()
        {
            CASNFPLogger. Instance. Log("机械臂正在工作");
        }

        private IEnumerator ExtendArm (float targetAngle)
        {
            //机械臂展开,根据机械臂的关节信息，依次展开机械臂
            CASNFPLogger. Instance. Log ("机械臂正在展开");
            foreach (ArmJoint joint in joints)
            {
                joint.SetAngle(targetAngle);
            }
            yield return new WaitUntil(() => 
            {
                //判断机械臂是否全部展开(targetAngle - joint.currentAngle < 0.01f)
                return joints.All(joint => Mathf.Abs(targetAngle - joint.currentAngle) < 0.01f);
            });
            CASNFPLogger.Instance.Log("机械臂展开完成");
            ArmState = ArmState.Doing;
        }

        private IEnumerator RetractArm ()
        {
            //机械臂收回,根据机械臂的关节信息，依次收回机械臂
            CASNFPLogger. Instance. Log ("机械臂正在回收");
            foreach ( ArmJoint joint in joints )
            {
                joint. SetAngle (joint. initialAngle);
            }
            yield return new WaitUntil (() =>
            {
                //判断机械臂是否全部收回(joint.initialAngle - joint.currentAngle < 0.01f)
                return joints. All (joint => Mathf. Abs (joint. initialAngle - joint. currentAngle) < 0.01f);
            });
            CASNFPLogger. Instance. Log ("机械臂回收完成");
            ArmState = ArmState. Idle;
        }

        private void InstializeJoints() 
        {   //初始化关节
            workPos = this. part. FindModelTransform (workPosName);
            basePos = this.part. FindModelTransform ( basePosName);
            ArmJoint[] linkJoints = ArmHelper.SetJointWithString(linkJointsInfo,this.part).ToArray();
            joints = new ArmJoint[linkJoints.Length + 2];
            joints[0] = ArmHelper.SetJointWithString(baseJointInfo, this.part)[0];
            joints[joints.Length - 1] = ArmHelper.SetJointWithString(effectJointInfo, this. part)[0];
            for (int i = 0; i < linkJoints.Length; i++)
            {
                joints[i + 1] = linkJoints[i];
            }
            foreach (ArmJoint joint in joints)
            {
                joint.Init(); 
            }
            ArmState = ArmState.Idle;
        }
        private void OnDestroy ()
        {
            OnArmStateChanged.Remove(OnArmStateChangedEvent);
        }
    }
}