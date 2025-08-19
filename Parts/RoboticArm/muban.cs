using System;
using System. Collections. Generic;
using System. Linq;
using System. Text;
using System. Threading. Tasks;

namespace ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm
{
    internal class muban
    {
        public static double[] CalculateJointAngles (
        double[] basePos,
        double[] links,
        double[] targetPos,
        Tuple<double, double>[] angleLimits)
        {
            int n = links. Length;
            double[] angles = new double[n];
            Vector3 currentPos = new Vector3 (basePos);
            Vector3 target = new Vector3 (targetPos);

            // 工作空间检查
            double maxReach = links. Sum ();
            double remainingDist = ( target - currentPos ). Magnitude;
            if ( remainingDist > maxReach + 1e-6 )
                throw new ArgumentException ("Target position unreachable");

            // 第一关节角度（绕Z轴旋转）
            Vector3 delta = target - currentPos;
            angles[0] = Math. Atan2 (delta. Y, delta. X);

            // 平面内运动学计算
            if ( n > 1 )
            {
                double planarDist = Math. Sqrt (delta. X * delta. X + delta. Y * delta. Y);
                Vector2 effTarget = new Vector2 (planarDist, delta. Z);

                // 两连杆解析解
                if ( n == 2 )
                {
                    double l1 = links[0], l2 = links[1];
                    double D = ( effTarget. X * effTarget. X + effTarget. Y * effTarget. Y - l1 * l1 - l2 * l2 ) / ( 2 * l1 * l2 );
                    D = Math. Clamp (D, -1.0, 1.0);

                    double theta2 = Math. Acos (D);
                    double theta1 = Math. Atan2 (effTarget. Y, effTarget. X) -
                                   Math. Atan2 (l2 * Math. Sin (theta2), l1 + l2 * Math. Cos (theta2));

                    angles[1] = theta1;
                    if ( n == 2 )
                        return ApplyAngleLimits (angles, angleLimits);
                }

                // 多连杆迭代解法
                for ( int i = 1 ; i < n ; i++ )
                {
                    double remainingLinks = links. Skip (i). Sum ();
                    double ratio = remainingLinks / remainingDist;

                    Vector3 jointToTarget = ( target - currentPos ). Normalized;
                    angles[i] = Math. Acos (Vector3. Dot (jointToTarget, Vector3. UnitX));

                    currentPos += links[i - 1] * new Vector3 (
                        Math. Cos (angles[i]),
                        Math. Sin (angles[i]),
                        0);
                    remainingDist = ( target - currentPos ). Magnitude;
                }
            }

            return ApplyAngleLimits (angles, angleLimits);
        }

        private static double[] ApplyAngleLimits (double[] angles, Tuple<double, double>[] limits)
        {
            for ( int i = 0 ; i < angles. Length ; i++ )
                angles[i] = Math. Clamp (angles[i], limits[i]. Item1, limits[i]. Item2);
            return angles;
        }
    }

    public struct Vector3
    {
        public double X, Y, Z;
        public Vector3 (double[] arr) : this (arr[0], arr[1], arr[2]) { }
        public Vector3 (double x, double y, double z) => (X, Y, Z) = (x, y, z);

        public static Vector3 operator - (Vector3 a, Vector3 b) =>
            new Vector3 (a. X - b. X, a. Y - b. Y, a. Z - b. Z);

        public double Magnitude => Math. Sqrt (X * X + Y * Y + Z * Z);
        public Vector3 Normalized => this / Magnitude;
        public static Vector3 operator / (Vector3 v, double d) =>
            new Vector3 (v. X / d, v. Y / d, v. Z / d);

        public static double Dot (Vector3 a, Vector3 b) =>
            a. X * b. X + a. Y * b. Y + a. Z * b. Z;

        public static readonly Vector3 UnitX = new Vector3 (1, 0, 0);
    }

    public struct Vector2
    {
        public double X, Y;
        public Vector2 (double x, double y) => (X, Y) = (x, y);
    }

    public class Program
    {
        public static void Main ()
        {
            // 示例用法
            var angles = InverseKinematicsCalculator. CalculateJointAngles (
                basePos: new double[] { 0, 0, 0 },
                links: new double[] { 1.0, 0.8, 0.5 },
                targetPos: new double[] { 1.2, 0.9, 0.6 },
                angleLimits: new Tuple<double, double>[] {
                Tuple.Create(-Math.PI, Math.PI),
                Tuple.Create(-Math.PI/2, Math.PI/2),
                Tuple.Create(-Math.PI/3, Math.PI/3)
                });

            Console. WriteLine ("Calculated joint angles (radians):");
            Console. WriteLine (string. Join (", ", angles. Select (a => a. ToString ("F3"))));
        }
    }
}
