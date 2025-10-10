using ChinaAeroSpaceNearFuturePackage. Core. Managers;
using KSP. Localization;
using System;
using System. Collections. Generic;
using System. Linq;
using System. Text;
using System. Threading. Tasks;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts
{
    public class RoboticArmController : MonoBehaviour
    {
        public ModuleCASNFP_RoboticArmPart controlledArm;
        private ArmJoint[] joints;
        private ArmJoint baseJoint;
        private ArmJoint effectJoint;
        private Transform workTransform;
        private float jointLength;

        public void Awake ()
        {
            
        }
        public void Start ()
        {
            if ( controlledArm == null )
            {
                CASNFPLogger. Instance. Log ("RoboticArmController controlledArm is null, please assign it in the inspector");
                Destroy (this);
                return;
            }
            joints = controlledArm. joints;
            baseJoint = controlledArm. joints[0];
            effectJoint = controlledArm. joints[joints.Length - 1];
            workTransform = controlledArm. workPos;
            jointLength = Vector3. Distance (effectJoint. transform. position, workTransform. position);

        }
        bool flag1 = false;
        public void Update ()
        {
            if ( !HighLogic. LoadedSceneIsFlight )
            {
                return;
            }
            if ( !flag1 )
            {
                controlledArm. targetAngle = 30;
                controlledArm. ArmState = ArmState. Extanding;
                flag1 = true;
            }
        }
    }
}
