using ChinaAeroSpaceNearFuturePackage. Core. Managers;
using System. Collections. Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. CASNFPParts. ArmParts
{
    public static class ArmHelper
    {
        public static Vector3 rotateAxai (string axis)
        {
            Vector3 rotaAxai = Vector3. zero;
            switch ( axis )
            {
                case "X":
                    rotaAxai = Vector3. right;
                    break;

                case "Y":
                    rotaAxai = Vector3. up;
                    break;

                case "Z":
                    rotaAxai = Vector3. forward;
                    break;

                case "x":
                    rotaAxai = Vector3. right;
                    break;

                case "y":
                    rotaAxai = Vector3. up;
                    break;

                case "z":
                    rotaAxai = Vector3. forward;
                    break;
            }
            return rotaAxai;
        }

        public static List<ArmJoint> SetJointWithString (string jointString, Part part)
        {
            List<ArmJoint> joints = new List<ArmJoint> ();
            joints. Clear ();
            string[] splitJoint = jointString. Split ('|');
            string[] splitJointInfo;
            for ( int i = 0 ; i < splitJoint. Length ; i++ )
            {
                splitJointInfo = splitJoint[i]. Split (',');
                for ( int j = 0 ; j < splitJointInfo. Length ; j++ )
                {
                    splitJointInfo[j] = splitJointInfo[j]. Trim ();
                }
                ArmJoint joint = new ArmJoint (part. FindModelTransform (splitJointInfo[0]));
                joint. rotateSpeed = float. Parse (splitJointInfo[1]);
                joint. rotateAxais = rotateAxai (splitJointInfo[2]);
                Vector2 vector2 = new Vector2 (float. Parse (splitJointInfo[3]), float. Parse (splitJointInfo[4]));
                joint. rotateLimit = vector2;
                joint. initialAngle = float. Parse (splitJointInfo[5]);
                joints. Add (joint);
            }
            return joints;
        }
    }
    public enum ArmWorkType
    {
        Sample_ChangE,
        Walk_TianGong,
        Grabbing,
        Camera,
    }

    public enum ArmState
    {
        Idle,
        Extanding,
        Retracting,
        Doing,
    }

    public class ArmJoint
    {
        public Transform transform;
        public float rotateSpeed;
        public Vector2 rotateLimit;
        public float currentAngle;
        public Vector3 rotateAxais;
        public float initialAngle;

        public ArmJoint (Transform jointTransform)
        {
            transform = jointTransform;
            rotateSpeed = 5;
            rotateLimit = new Vector2 (-180, 180);
            currentAngle = 0;
            initialAngle = 0;
            rotateAxais = Vector3. right;
        }

        public void Init ()
        {
            currentAngle = initialAngle;
            transform. localRotation = Quaternion. Euler (initialAngle * rotateAxais);
        }

        public void SetAngle (float targetAngle)
        {
            targetAngle = Mathf. Clamp (targetAngle, rotateLimit. x, rotateLimit. y);
            currentAngle = Mathf. MoveTowards (currentAngle, targetAngle, rotateSpeed * Time. deltaTime);
            transform. localRotation = Quaternion. Euler (currentAngle * rotateAxais);
        }
    }
}