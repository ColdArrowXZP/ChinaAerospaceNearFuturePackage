using System;
using System. Collections. Generic;
using System. Linq;
using System. Text;
using System. Threading. Tasks;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm
{
    public enum ArmPartType // 机械臂组件类型枚举
    {
        Base,// 基座
        link,// 连接臂
        work,// 工作臂
    }
    public enum ArmWorkType // 机械臂类型枚举
    {
        Sample_ChangE, // 取样（嫦娥）机械臂
        Walk_TianGong, // 吸盘式（天宫）机械臂
        Grabbing, // 抓取式机械臂
        Camera, // 摄像类机械臂
    }
    public struct OriginalArmPart
    {
        public ArmWorkType armWorkType;
        public Part part;
        public ArmPartType partType;
    }
    public struct OriginalArm
    {
        public ArmWorkType armWorkType;
        public Part[] armParts;
    }
    public class IK : MonoBehaviour
    {
        private IK ()
        {
        }
        public IK (Transform targetPoint, Transform joint0, Transform joint1, Transform hand, bool angleMode = false)
        {
            Target = targetPoint;
            Joint0 = joint0;
            Joint1 = joint1;
            Hand = hand;
            angle_mode = angleMode;
        }
        //[Header("Joints")]
        Transform Joint0;
        Transform Joint1;
        Transform Hand;

        //[Header("Target")]
        Transform Target;


        private float length0, L1;
        private float length1, L2;

        private float L1_2, L2_2;

        private int X_AXIS = 0;
        private int Y_AXIS = 1;
        private int Z_AXIS = 2;

        private float SCARA_OFFSET_X = 0;
        private float SCARA_OFFSET_Y = 0.72f;
        private float SCARA_OFFSET_Z = 0;

        bool angle_mode;

        private float[] cartesian = new float[3], f_scara = new float[3];


        void Start ()
        {
            length0 = Vector3. Distance (Joint0. position, Joint1. position);
            length1 = Vector3. Distance (Joint1. position, Hand. position);
            L1 = length0;
            L2 = length1;
            L1_2 = Mathf. Pow (L1, 2);
            L2_2 = Mathf. Pow (L2, 2);
        }




        // Update is called once per frame
        void Update ()
        {
            float length2 = Vector3. Distance (Joint0. position, Target. position);

            if ( length2 > length0 + length1 - 0.3f )
            {

            }
            else
            {
                cartesian[0] = Target. position. x;
                cartesian[1] = Target. position. y;
                cartesian[2] = Target. position. z;

                inverse_kinematics (cartesian, f_scara);

                float[] cart = new float[3];
                float[] scar = new float[3];

                scar = ( float[] )f_scara. Clone ();

                forward_kinematics_SCARA (scar, cart);
                Debug. Log ("x:" + cart[0]);
                Debug. Log ("y:" + cart[1]);
                Debug. Log ("z:" + cart[2]);
            }

            Joint0. transform. localEulerAngles = new Vector3 (0, 0, 0);
            Vector3 Euler0 = Joint0. transform. localEulerAngles;
            Euler0. x = 90f - f_scara[0];
            Joint0. transform. localEulerAngles = Euler0;

            Joint1. transform. localEulerAngles = new Vector3 (0, 0, 0);
            Vector3 Euler1 = Joint1. transform. localEulerAngles;
            Euler1. x = f_scara[1];
            Joint1. transform. localEulerAngles = Euler1;

            this. transform. localEulerAngles = new Vector3 (0, 0, 0);
            Vector3 Euler2 = this. transform. localEulerAngles;
            Euler2. y = f_scara[2];
            this. transform. localEulerAngles = Euler2;


        }


        // f_scara是大臂和小臂的角度，cartesian是坐标
        void inverse_kinematics (float[] cartesian, float[] f_scara)
        {
            /***********************robot arm****************************/

            //         y +  /z
            //           | /
            //           |/
            //           +-----+x

            float ROBOTARM_alpha, ROBOTARM_beta, ROBOTARM_cta, ROBOTARM_alphapsi, projectxyLength, X, Y, X_2, Y_2, sqrtx_2ay_2;

            //首先求得目标点 到 原点的距离
            //获取机械臂投射到xy平面的长度
            //length = sqrt(x*x + y*y)
            projectxyLength = Mathf. Sqrt (Mathf. Pow (( cartesian[X_AXIS] ), 2) + Mathf. Pow (( cartesian[Z_AXIS] ), 2));//对调yz,坐标系不同
                                                                                                                          //将3d机械臂变量变为2d机械臂的变量
                                                                                                                          //	projectxyLength长度为2d机械臂的X
            X = projectxyLength;
            X_2 = Mathf. Pow (X, 2);
            Y = cartesian[Y_AXIS];//对调yz,坐标系不同
            Y_2 = Mathf. Pow (Y, 2);
            sqrtx_2ay_2 = Mathf. Sqrt (X_2 + Y_2);
            //求得机械臂所在yz旋转平面的alphapsi角度
            ROBOTARM_alphapsi = Mathf. Acos (X / sqrtx_2ay_2);
            //如果坐标在平面以下，将alphapsi取反
            if ( Y < 0 )
            {
                ROBOTARM_alphapsi = -ROBOTARM_alphapsi;
            }

            //求得机械臂所在yz旋转平面的alpha角度,，即大臂到xy平面的角度(实际是弧度)
            ROBOTARM_alpha = Mathf. Acos (( L1_2 + X_2 + Y_2 - L2_2 ) / ( 2 * L1 * sqrtx_2ay_2 )) + ROBOTARM_alphapsi;
            //求得小臂的角度(实际是弧度)
            ROBOTARM_beta = Mathf. Acos (( X_2 + Y_2 - L1_2 - L2_2 ) / ( 2 * L1 * L2 ));
            //求得整体机械臂的旋转角度(实际是弧度)
            ROBOTARM_cta = Mathf. Atan2 (cartesian[X_AXIS], cartesian[Z_AXIS]);

            //如果不是角度模式
            if ( !angle_mode )
            {
                f_scara[X_AXIS] = Mathf. Rad2Deg * ROBOTARM_alpha; //大臂旋转弧度转换为角度
                f_scara[Y_AXIS] = Mathf. Rad2Deg * ROBOTARM_beta;   //小臂旋转弧度转换为角度
                f_scara[Z_AXIS] = Mathf. Rad2Deg * ROBOTARM_cta;
            }
            //否则是角度模式
            else
            {
                f_scara[X_AXIS] = cartesian[X_AXIS];
                f_scara[Y_AXIS] = cartesian[Y_AXIS];
                f_scara[Z_AXIS] = cartesian[Z_AXIS];

            }
        }



        //传入值 f_scara是大臂和小臂的角度，cartesian是最终求得的坐标
        void forward_kinematics_SCARA (float[] f_scara, float[] cartesian)
        {
            /***********************robot arm****************************/
            float X, Y, Z;
            float project3D;
            //f_scara里面的0,1,2代表：大臂角度，小臂角度，旋转角度


            //         y +  /z
            //           | /
            //           |/
            //           +-----+x


            //unity 角度空间转换转换
            //f_scara[X_AXIS] = 90f - f_scara[X_AXIS];

            //2D机械臂朝向的方向,两臂投影到平台平面长度
            //Z = Mathf.Cos(Mathf.Deg2Rad * f_scara[X_AXIS]) * L1 + Mathf.Sin((Mathf.Deg2Rad * f_scara[Y_AXIS]) + (Mathf.Deg2Rad * 90) - (Mathf.Deg2Rad * f_scara[X_AXIS])) * L2;

            //3D 两臂投影到平台平面长度,通过三角函数转化为坐标轴方向长度
            project3D = Mathf. Cos (Mathf. Deg2Rad * f_scara[X_AXIS]) * L1 + Mathf. Sin (( Mathf. Deg2Rad * f_scara[Y_AXIS] ) + ( Mathf. Deg2Rad * 90 ) - ( Mathf. Deg2Rad * f_scara[X_AXIS] )) * L2;
            Z = Mathf. Cos (( Mathf. Deg2Rad * f_scara[Z_AXIS] )) * project3D;

            //垂直向上的方向
            Y = Mathf. Sin (( Mathf. Deg2Rad * f_scara[X_AXIS] )) * L1 + Mathf. Cos (( Mathf. Deg2Rad * f_scara[Y_AXIS] ) + ( Mathf. Deg2Rad * 90 ) - ( Mathf. Deg2Rad * f_scara[X_AXIS] )) * L2;

            //2D 站机械臂后方看向它，它的右边方向
            //X = Mathf.Sin((Mathf.Deg2Rad * f_scara[Z_AXIS])) * Mathf.Cos((Mathf.Deg2Rad * f_scara[X_AXIS])) * L1 + Mathf.Sin((Mathf.Deg2Rad * f_scara[Y_AXIS]) + (Mathf.Deg2Rad * 90) - (Mathf.Deg2Rad * f_scara[X_AXIS])) * L2;

            //3D 站机械臂后方看向它，它的右边方向
            X = Mathf. Sin (( Mathf. Deg2Rad * f_scara[Z_AXIS] )) * project3D;




            cartesian[X_AXIS] = X + SCARA_OFFSET_X;  //求得机械手顶点的坐标
            cartesian[Y_AXIS] = Y + SCARA_OFFSET_Y;  //求得机械手顶点的坐标
            cartesian[Z_AXIS] = Z + SCARA_OFFSET_Z;

            /***********************robot arm****************************/
        }
    }
}
