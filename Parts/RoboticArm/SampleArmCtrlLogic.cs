using EdyCommonTools;
using Expansions. Serenity;
using KSP. Localization;
using System;
using System. Collections. Generic;
using System. Linq;
using System. Text;
using System. Threading. Tasks;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. Parts. RoboticArm
{
     public class SampleArmCtrlLogic:MonoBehaviour
    {
        CASNFP_SetRocAutoCtrl rocAutoCtrl;
        List<ArmPartJointInfo> currentArmParts;
        bool isTargetRingSetUp = false,isCalCom = false, isGetTargetPoint = false;
        GameObject sampleMaxRangeRing;
        Vector3 targetPoint;
        float armLength;
        float positionTerrainHeight;
        float radius;
        Vector3 ringCenter;
        Vector3 normal;
        List<float> targetAngles = new List<float>();
        private event Action<bool> isMoving;
        GameObject XAxis, YAxis, ZAxis, WXAxis, WYAxis, WZAxis;
        public bool IsCalCom
        {
            get { return isCalCom; }
            private set 
            {
                if ( isCalCom != value )
                    isCalCom = value; 
                    isMoving?.Invoke (isCalCom);
            }
        }

        public void Awake ()
        {
            Debug. Log ("开始执行取样臂Awake方法");
            rocAutoCtrl = gameObject.GetComponent<CASNFP_SetRocAutoCtrl>();
            if ( rocAutoCtrl == null )
            {
                Debug. Log ("程序错误,没有找到嫦娥机械臂控制程序");
                Destroy(this);
            }
        }
        public void Start ()
        {
            currentArmParts = rocAutoCtrl. currentWorkingRoboticArm;
            XAxis = new GameObject("XAxis");
            YAxis = new GameObject("YAxis");
            ZAxis = new GameObject("ZAxis");
            WXAxis = new GameObject("WXAxis");
            WYAxis = new GameObject("WYAxis");
            WZAxis = new GameObject("WZAxis");
            if (isMoving == null)
            {
                isMoving += new Action<bool> (IsMoving);
            }
            for ( int i = 0 ; i < currentArmParts. Count ; i++ )
            {
                if ( currentArmParts[i]. partType == ArmPartType. link )
                {
                    armLength += currentArmParts[i]. armLength;
                }
            }
            Debug.Log("当前机械臂计算臂长为："+armLength);
        }

        private void IsMoving (bool obj)
        {
            if ( obj )
            {
                for ( int i = 0 ; i < currentArmParts. Count ; i++ )
                {
                    currentArmParts[i]. servoHinge. targetAngle = targetAngles[i];
                }
                Debug. Log ("机械臂开始移动");
                isGetTargetPoint = false;
            }
            else
            {
                Debug. Log ("机械臂移动结束");
                for ( int i = 0 ; i < currentArmParts. Count ; i++ )
                {
                    //机械臂停止
                    currentArmParts[i]. servoHinge. targetAngle = currentArmParts[i]. servoHinge.currentAngle;
                    currentArmParts[i]. currentAngle = currentArmParts[i]. servoHinge. currentAngle;
                }
            }
        }

        public void OnDestroy ()
        {
            isMoving = null;
            if ( sampleMaxRangeRing != null )
            {
                Destroy (sampleMaxRangeRing);
                isTargetRingSetUp = false;
            }
        }
        //步骤：1、获取机械臂长度，2、获取机械臂基座位置地形高度，3、计算出机械臂工作范围半径，4、设置一个绿色圆环供玩家参考取样点，5、获取鼠标点击事件，6、计算取样点位置，7、执行取样动作。
        private bool TryGetValidSamplePoint (out Vector3 clickPoint)
        {
            clickPoint = Vector3.zero;
            var ray = FlightGlobals. fetch. mainCameraRef. ScreenPointToRay (Input. mousePosition);
            if ( Physics. Raycast (ray, out RaycastHit terrainHit, Mathf. Infinity) )
            {
                int num = terrainHit. collider. gameObject. layer;
                if ( num != 10 && num !=15)
                {
                    ScreenMessages. PostScreenMessage (
                        Localizer. Format ($"选择的不是地面点无法取样，请在原地面选点"),
                        2f, ScreenMessageStyle. UPPER_RIGHT);
                    return false;
                }
                clickPoint = terrainHit. point;
                if ( Vector3. Distance (clickPoint, ringCenter) > radius )
                {
                    ScreenMessages. PostScreenMessage (
                        Localizer. Format ($"取样点超出机械臂最大工作范围"),
                        2f, ScreenMessageStyle. UPPER_RIGHT);
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        RoboticArmIK roboticArmIK;
        public void Update ()
        {

            if ( !HighLogic. LoadedSceneIsFlight || isGetTargetPoint )
            {
                //SetLine();
                if ( !IsCalCom )
                {
                    XAxis.transform.position = YAxis.transform.position = ZAxis.transform.position = currentArmParts[0].servoHinge.gameObject.transform.position;
                    XAxis.transform.rotation = Quaternion.LookRotation(currentArmParts[0].servoHinge.gameObject.transform.forward, currentArmParts[0].servoHinge.gameObject.transform.up);
                    YAxis.transform.rotation = Quaternion.LookRotation(currentArmParts[0].servoHinge.gameObject.transform.forward, currentArmParts[0].servoHinge.gameObject.transform.up);
                    ZAxis.transform.rotation = Quaternion.LookRotation(currentArmParts[0].servoHinge.gameObject.transform.forward, currentArmParts[0].servoHinge.gameObject.transform.up);
                    XAxis.transform.parent = YAxis.transform.parent = ZAxis.transform.parent = currentArmParts[0].servoHinge.gameObject.transform;
                    SetLine(XAxis, Vector3.zero, Vector3.right, Color.red);
                    SetLine(YAxis, Vector3.zero, Vector3.up, Color.green);
                    SetLine(ZAxis, Vector3.zero, Vector3.forward, Color.blue);
                    //开始计算机械臂逆解
                    targetAngles. Clear ();
                    if (roboticArmIK == null)
                    {
                        roboticArmIK = new RoboticArmIK (currentArmParts, targetPoint,armLength);
                    }else
                    {
                        roboticArmIK.targetPosition = targetPoint;
                    }

                    if ( roboticArmIK. SolveIK () )
                    {
                        ScreenMessages. PostScreenMessage (
                            Localizer. Format ($"开始执行动作"),
                            2f, ScreenMessageStyle. UPPER_RIGHT);
                        targetAngles = roboticArmIK. jointTargetAngle;
                        IsCalCom = true;
                    }
                    else
                    {
                        ScreenMessages. PostScreenMessage (
                            Localizer. Format ($"机械臂逆解计算失败，请检查机械臂设置或取样点位置"),
                            2f, ScreenMessageStyle. UPPER_RIGHT);
                        isGetTargetPoint = false;
                    }
                    roboticArmIK = null;
                }
                return;
            }
                
            if ( currentArmParts[0]. vessel. srf_velocity. magnitude > 0.1d )
            {
                ScreenMessages. PostScreenMessage (
                    Localizer. Format ($"载具移动中，无法取样"),
                    2f, ScreenMessageStyle. UPPER_RIGHT);
                if ( isTargetRingSetUp )
                {
                    Destroy (sampleMaxRangeRing);
                    isTargetRingSetUp = false;
                }
                return;
            }
            //这里设置一个可视化的圆环
            if ( !isTargetRingSetUp)
            {
                if ( SampleTargetSet () )
                {
                    isTargetRingSetUp = true;
                }
                else
                {
                    Debug. Log ("圆环设置失败，请返回发射中心调整设置或重新选择机械臂");
                    Destroy (this);
                }
                return;
            }

            if ( !Input. GetMouseButtonDown (0) )
            {
                ScreenMessages. PostScreenMessage (
                    Localizer. Format ($"请在绿色圆环内选择取样地点"),
                    2f, ScreenMessageStyle. UPPER_RIGHT);
                return;
            }
            if ( !TryGetValidSamplePoint (out Vector3 clickPoint) )
            {
                return;
            }
            else
            {
                targetPoint = clickPoint;
                isGetTargetPoint = true;
                if ( IsCalCom )IsCalCom = false;
                ScreenMessages. PostScreenMessage (
                    Localizer. Format ($"开始计算并执行取样动作，请稍候"),
                    2f, ScreenMessageStyle. UPPER_RIGHT);
            }
        }
        private bool SampleTargetSet ()
        {
            //初始化所选择的机械臂的各项参数
            //开始计算取样范围，设置一个绿色圆环供玩家参考取样点
            Debug. Log ("开始设置绿环");
            positionTerrainHeight = CalculatePositionTerrainHeight (out ringCenter, out normal);
            if ( positionTerrainHeight < 0 )
            {
                Debug. Log ("错误:" + "飞船所处位置没有检测到地面实体");
                return false;
            }
            float C = armLength * armLength - positionTerrainHeight * positionTerrainHeight;
            if( C <= 0 )
            {
                Debug. Log ("错误:" + "机械臂距离地面过高，超出机械臂总长度，无法设置取样点");
                return false;
            }
            radius = Mathf. Sqrt (C);
            SetSampleMaxRange (radius, ringCenter, normal);
            return true;
        }
        public void SetSampleMaxRange (float ringRadius, Vector3 ringCenter, Vector3 normal)
        {
            float ra = ringRadius;
            sampleMaxRangeRing = new GameObject ();
            LineRenderer lineRenderer = sampleMaxRangeRing. AddComponent<LineRenderer> ();
            lineRenderer. useWorldSpace = false;
            lineRenderer. startWidth = 0.15f;
            lineRenderer. endWidth = 0.15f;
            lineRenderer. loop = true;
            lineRenderer. positionCount = 64 + 1;
            lineRenderer. material = new Material (Shader. Find ("KSP/Particles/Additive"));
            lineRenderer. startColor = Color. green;
            lineRenderer. endColor = Color. green;
            float angle = 0f;
            for ( int i = 0 ; i < 64 + 1 ; i++ )
            {
                float x = Mathf. Sin (Mathf. Deg2Rad * angle) * ra;
                float y = Mathf. Cos (Mathf. Deg2Rad * angle) * ra;
                lineRenderer. SetPosition (i, new Vector3 (x, y, 0));
                angle += 360f / 64;
            }
            sampleMaxRangeRing. transform. position = ringCenter;
            sampleMaxRangeRing. transform. rotation = Quaternion. LookRotation (normal, sampleMaxRangeRing. transform. right);
            sampleMaxRangeRing. transform. SetParent (currentArmParts[0]. vessel. transform);
        }
        
        public void SetLine(GameObject gameObject,Vector3 start,Vector3 end,Color color) 
        {
            LineRenderer lineRenderer;
            if (!gameObject.TryGetComponent<LineRenderer>(out lineRenderer)) 
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
            lineRenderer.positionCount = 2; // XYZ轴各需要2个点
            lineRenderer.useWorldSpace = false;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.loop = false;
            lineRenderer.material = new Material(Shader.Find("KSP/Particles/Additive"));
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
        public float CalculatePositionTerrainHeight (out Vector3 ringCenter, out Vector3 normal)
        {
            float heightFromTerrain = -1f;
            ArmPartJointInfo baseJoint = currentArmParts[0];
            foreach ( var item in currentArmParts )
            {
                if ( item.partType == ArmPartType . Base )
                {
                    baseJoint = item;
                    break;
                }
            }
            Vector3 basePos = baseJoint.part.gameObject.transform.position;
            Vector3d calNormal = FlightGlobals. getUpAxis (FlightGlobals. getMainBody (), basePos). normalized;
            float num = ( float )FlightGlobals. getMainBody (). Radius / 2;
            if(Physics.Raycast(basePos,-calNormal,out RaycastHit hit,num, (1 << 15)|( 1<<10)) )
            {
                heightFromTerrain = hit. distance;
                ringCenter = hit. point;
                normal = hit. normal;
                return heightFromTerrain;
            }
            else
            {
                ringCenter = Vector3. zero;
                normal = Vector3. zero;
                return -1f;
            }
        }
    }
}
