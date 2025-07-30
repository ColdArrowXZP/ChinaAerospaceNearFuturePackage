using Expansions. Serenity;
using KSP. Localization;
using System;
using System. Collections. Generic;
using System. Linq;
using System. Runtime. CompilerServices;
using System. Text;
using System. Threading. Tasks;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm
{
    public struct CheckAndDistinguishThePart
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


    public class RocArmAutoCtrl
    {   
        public ArmWorkType WorkType
        {
            get;
        }
        public Part[] ArmParts
        {
            get;
        }
        public RocArmAutoCtrl (ArmWorkType armWorkType, Part[] parts)
        {
            ArmParts = parts;
            WorkType = armWorkType;
        }
        public float armLength = 0;
        public float positionTerrainHeight = 0;
        public bool CanBeReach;
        Vector3 targetPoint;
        public Vector3 SetTarget ()
        {
            switch ( WorkType )
            {
                case ArmWorkType. Sample_ChangE:
                    SampleTargetSet (out targetPoint);
                    break;
                case ArmWorkType. Walk_TianGong:
                    WorkTargetSet (out targetPoint);
                    break;
                case ArmWorkType. Grabbing:
                    GrabbingTargetSet(out targetPoint);
                    break;
                case ArmWorkType. Camera:
                    CamTargetSet (out targetPoint);
                    break;
            }
            return targetPoint;
        }

        private void SampleTargetSet (out Vector3 targetPoint)
        {

            var ray = FlightGlobals. fetch. mainCameraRef. ScreenPointToRay (Input. mousePosition);

            if ( !Physics. Raycast (ray, out RaycastHit hit) || hit. collider == null )
            {
                targetPoint = Vector3. zero;
                return;
            }

            if ( hit. collider. gameObject. layer != 15 )
            {
                ScreenMessages. PostScreenMessage (
                    Localizer. Format ($"选择取样地点为{hit. collider. gameObject. name}不正确，请选择地面取样点"),
                    2f, ScreenMessageStyle. UPPER_RIGHT);
                targetPoint = Vector3. zero;
                return;
            }
            targetPoint = hit. point;
        }

        private void CamTargetSet (out Vector3 targetPoint)
        {
            throw new NotImplementedException ();
        }

        private void GrabbingTargetSet (out Vector3 targetPoint)
        {
            throw new NotImplementedException ();
        }

        private void WorkTargetSet (out Vector3 targetPoint)
        {
            throw new NotImplementedException ();
        }

        public bool SetSampleMaxRange ()
        {
            Debug. Log ("开始设置绿环");
            //List<Vector3> sevroPos = new List<Vector3> ();
            //for ( int i = 0 ; i < ArmParts. Length ; i++ )
            //{
            //    sevroPos. Add (ArmParts[i]. FindModuleImplementing<BaseServo> (). servoTransformPosition);
            //}
            //armLength = CalculateArmLength (sevroPos);
            //positionTerrainHeight = CalculatePositionTerrainHeight (sevroPos[0],ArmParts[0]);
            //float radius = ( float )Math. Sqrt (armLength * armLength - positionTerrainHeight * positionTerrainHeight);
            //if ( radius < 0.3f )
            //{
            //    Debug. Log ("机械臂距离地面过高，超出最大工作范围，无法设置取样点"+radius);
            //    return false;
            //}
            float radius = 5f;
            GameObject gameObject = new GameObject ();
            LineRenderer lineRenderer = gameObject. AddComponent<LineRenderer> ();
            lineRenderer. useWorldSpace = false;
            lineRenderer. startWidth = 0.15f;
            lineRenderer. endWidth = 0.15f;
            lineRenderer. positionCount = 64 + 1;
            lineRenderer. material = new Material (Shader. Find ("KSP/Particles/Additive"));
            lineRenderer. startColor = Color. green;
            lineRenderer. endColor = Color. green;
            float angle = 0f;
            for ( int i = 0 ; i < 64 + 1 ; i++ )
            {
                float x = Mathf. Sin (Mathf. Deg2Rad * angle) * radius;
                float y = Mathf. Cos (Mathf. Deg2Rad * angle) * radius;
                lineRenderer. SetPosition (i, new Vector3 (x, y, 0));
                angle += 360f / 64;
            }
            gameObject. transform. position = ArmParts[0].transform.position;
            gameObject.transform.parent = ArmParts[0].transform;
            Debug. Log ("绿环设置完成");
            return true;
        }
        public float CalculateArmLength (List<Vector3> sevroPos)
        {
            Debug. Log ("开始计算臂长");
            float armLength = 0;
            for ( int i = 0 ; i < sevroPos. Count ; i++ )
            {
                if ( i == 0 || i == sevroPos. Count - 1 )
                    continue;
                armLength += Vector3. Distance (sevroPos[i], sevroPos[i + 1]);
            }
            Debug. Log ("臂长为"+armLength);
            return armLength;
        }
        public float CalculatePositionTerrainHeight (Vector3 vector3,Part part)
        {
            Debug. Log ("开始计算基座地形高度");
            float heightFromTerrain = -1f;
            Vector3 vector = FlightGlobals. getUpAxis (FlightGlobals. getMainBody (), vector3);
            float num = FlightGlobals. getAltitudeAtPos (vector3, FlightGlobals. getMainBody ());
            if ( num < 0 )
            {
                num = 0 - ( float )part.vessel. PQSAltitude ();
            }
            num += 600f;
            RaycastHit hit;
            if ( Physics. Raycast (vector3, -vector, out hit, num, 32768, QueryTriggerInteraction. Ignore) )
            {
                heightFromTerrain = hit. distance;
            }
           
            Debug. Log ("基座地形高度为"+ heightFromTerrain);
            return heightFromTerrain;
        }
        Vector3 targetPos;
        private bool HandleSamplePointSelection ()
        {
            if ( ArmParts[0]. vessel. GetSrfVelocity (). magnitude > 0.1f )
            {
                Debug. Log ("载具移动速度太快，无法设置取样点");
                return false;
            }
            //生成一个绿色的可视化圆环；
            if ( !SetSampleMaxRange () )
            {
                Debug. Log ("未成功生成取样范围圆环");
                return false;
            }
            ScreenMessages. PostScreenMessage ("请左键选择取样地点", 1f, ScreenMessageStyle. UPPER_LEFT);
            if ( !Input. GetMouseButtonDown (0) )
                return false;

            if ( !TryGetSamplePoint (out targetPos) )
                return false;
            return true;
        }

        private bool TryGetSamplePoint (out Vector3 clickPoint)
        {
            clickPoint = Vector3. zero;
            var ray = FlightGlobals. fetch. mainCameraRef. ScreenPointToRay (Input. mousePosition);

            if ( !Physics. Raycast (ray, out RaycastHit hit) || hit. collider == null )
                return false;

            if ( hit. collider. gameObject. layer != 15 )
            {
                ScreenMessages. PostScreenMessage (
                    Localizer. Format ($"选择取样地点为{hit. collider. gameObject. name}不正确，请选择地面取样点"),
                    2f, ScreenMessageStyle. UPPER_RIGHT);
                return false;
            }
            clickPoint = hit. point;
            return true;
        }

        internal bool CalculatorCanReach (ref Vector3 targetPoint)
        {
            throw new NotImplementedException ();
        }

        internal void MoveTo (Vector3 targetPoint)
        {
            throw new NotImplementedException ();
        }
    }
}
