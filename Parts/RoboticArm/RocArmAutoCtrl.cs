using ChinaAeroSpaceNearFuturePackage. UI;
using Expansions. Serenity;
using KSP. Localization;
using KSP. UI. Screens;
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
        public void StartCtrl ()
        {

            //区分开机械臂类型的，分别启动不同的控制逻辑。
            SeparateWorkType ();
        }
        private void SeparateWorkType ()
        {
            //启动各工作臂目标设置逻辑，目前只写取样臂逻辑。
            switch ( WorkType )
            {
                case ArmWorkType. Sample_ChangE:
                    SampleTargetSet ();
                    break;
                case ArmWorkType. Walk_TianGong:
                    WorkTargetSet ();
                    break;
                case ArmWorkType. Grabbing:
                    GrabbingTargetSet();
                    break;
                case ArmWorkType. Camera:
                    CamTargetSet ();
                    break;
            }
        }
        /// <summary>
        /// 取样机械臂的目标点获取逻辑，首先计算总臂长，再计算基座的地形高度，获得取样范围，设置个绿色圆环提示玩家
        /// </summary>
        private void SampleTargetSet ()
        {
            //开始计算取样范围，设置一个绿色圆环供玩家参考取样点
            Debug. Log ("开始设置绿环");
            armLength = CalculateArmLength ();//计算大臂长度
            positionTerrainHeight = CalculatePositionTerrainHeight (out Vector3 ringCenter, out Vector3 normal);
            if ( positionTerrainHeight < 0 )
            {
                Debug. Log ("所处位置没有检测到地面碰撞体");
                return;
            }
            if ( positionTerrainHeight >= armLength/2 )//这里的比较也有问题，暂时不管。
            {
                Debug. Log ("机械臂距离地面过高，超出最大工作范围，无法设置取样点");
                return;
            }
            float radius = ( float )Math. Sqrt (armLength * armLength - positionTerrainHeight * positionTerrainHeight);
            Debug. Log ("计算得出绿环半径为："+radius);
            SetSampleMaxRange (radius, ringCenter, normal);
            //开始获取鼠标点击事件
            //var ray = FlightGlobals. fetch. mainCameraRef. ScreenPointToRay (Input. mousePosition);

            //if ( !Physics. Raycast (ray, out RaycastHit hit) || hit. collider == null )
            //{
            //    targetPoint = Vector3. zero;
            //    return;
            //}

            //if ( hit. collider. gameObject. layer != 15 )
            //{
            //    ScreenMessages. PostScreenMessage (
            //        Localizer. Format ($"选择取样地点为{hit. collider. gameObject. name}不正确，请选择地面取样点"),
            //        2f, ScreenMessageStyle. UPPER_RIGHT);
            //    targetPoint = Vector3. zero;
            //    return;
            //}
            //targetPoint = hit. point;
        }

        private void CamTargetSet ()
        {
            throw new NotImplementedException ();
        }

        private void GrabbingTargetSet ()
        {
            throw new NotImplementedException ();
        }

        private void WorkTargetSet ()
        {
            throw new NotImplementedException ();
        }

        public void SetSampleMaxRange (float ringRadius, Vector3 ringCenter,Vector3 normal)
        {
           
            float radius = ringRadius;
            GameObject gameObject = new GameObject ();
            LineRenderer lineRenderer = gameObject. AddComponent<LineRenderer> ();
            lineRenderer. useWorldSpace = false;
            lineRenderer. startWidth = 0.15f;
            lineRenderer. endWidth = 0.15f;
            lineRenderer.loop = true;
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
            gameObject. transform. position = ringCenter;
            gameObject.transform.rotation = Quaternion.LookRotation(normal,gameObject.transform.right);
            gameObject. transform. SetParent (ArmParts[0]. transform);
        }
        public float CalculateArmLength ()
        {
            Debug. Log ("开始计算臂长");
            float armLength = 0;
            List<Vector3> linkNodePos = new List<Vector3> ();
            foreach ( Part part in ArmParts )
            {
                if ( part. FindModuleImplementing<ModuleCASNFP_RoboticArmPart> (). ArmPartType == ArmPartType. link )
                    
                    linkNodePos. Add (part. gameObject. GetChild (part. FindModuleImplementing<ModuleRoboticServoHinge> (). servoTransformName). transform. position);
            }
            for ( int i = 1 ; i < linkNodePos. Count ; i++ )
            {
                armLength += ( linkNodePos[i] - linkNodePos[i - 1] ). magnitude;
            }
            Debug. Log ("臂长为"+armLength);
            return armLength*2;//这里默认是大小臂相等且只有两段，是不对的，以后再修改这里臂长的计算方法。
        }
        public float CalculatePositionTerrainHeight (out Vector3 ringCenter,out Vector3 normal)
        {
            Debug. Log ("开始计算基座地形高度");
            float heightFromTerrain = -1f;
            Part basePart = null;
            foreach ( Part part in ArmParts )
            {
                if ( part. FindModuleImplementing<ModuleCASNFP_RoboticArmPart> (). ArmPartType == ArmPartType. Base )
                {
                    basePart = part;
                    break;
                } 
            }
            Vector3 basePos = basePart. gameObject. GetChild (basePart. FindModuleImplementing<ModuleRoboticServoHinge> (). baseTransformName). transform.position;
            Vector3 calNormal = (Vector3)FlightGlobals.getUpAxis (FlightGlobals. getMainBody (), basePos).normalized;
            float num = (float)FlightGlobals. getMainBody ().Radius/2;
            RaycastHit[] hits = Physics. RaycastAll(basePos,-calNormal, num);
            ringCenter = new Vector3 (0,0,0);
            normal = new Vector3 (0,0,0);
            foreach ( var item in hits )
            {
                if ( item. collider.gameObject.layer == 15)
                {
                    
                    RaycastHit rightHit = item;
                    heightFromTerrain = rightHit. distance;
                    ringCenter = rightHit. point;
                    normal = rightHit. normal;
                    break;
                }
            }
            Debug. Log ("基座地形高度"+ heightFromTerrain+"     绿环中心"+ringCenter+"   绿环法向"+normal);
            return heightFromTerrain;
        }
        //private void DrawARay (Vector3 startPos,Vector3 endPos,Color color)
        //{
        //    GameObject gameObject = new GameObject ();
        //    LineRenderer lineRenderer = gameObject. AddComponent<LineRenderer> ();
        //    lineRenderer. useWorldSpace = true;
        //    lineRenderer. startWidth = 0.1f;
        //    lineRenderer. endWidth = 0.1f;
        //    lineRenderer. material = new Material (Shader. Find ("KSP/Particles/Additive"));
        //    lineRenderer. startColor = color;
        //    lineRenderer. endColor = color;
        //    lineRenderer. SetPositions (new Vector3[] {startPos,endPos} );
        //    gameObject. transform. SetParent (ArmParts[0]. transform);
        //}
        Vector3 targetPos;
        private bool HandleSamplePointSelection ()
        {
            if ( ArmParts[0]. vessel. GetSrfVelocity (). magnitude > 0.1f )
            {
                Debug. Log ("载具移动速度太快，无法设置取样点");
                return false;
            }
            //生成一个绿色的可视化圆环；
            
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
