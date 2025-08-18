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
        Part[] currentArmParts;
        bool isTargetRingSetUp;
        GameObject sampleMaxRangeRing;
        Vector3 targetPoint;
        float armLength;
        float positionTerrainHeight;
        float radius;
        Vector3 ringCenter;
        Vector3 normal;
        bool isUpdate;
        public void Awake ()
        {
            Debug. Log ("开始执行取样臂Awake方法");
            rocAutoCtrl = CASNFP_SetRocAutoCtrl.Instance;
            if ( rocAutoCtrl == null )
            {
                Debug. Log ("程序错误,没有找到嫦娥机械臂控制程序");
            }
        }
        public void Start ()
        {
            Debug. Log ("开始执行取样臂Start方法");
            currentArmParts = rocAutoCtrl. currentWorkingRoboticArm. armParts;
            isUpdate = true;
        }
        //步骤：1、获取机械臂长度，2、获取机械臂基座位置地形高度，3、计算出机械臂工作范围半径，4、设置一个绿色圆环供玩家参考取样点，5、获取鼠标点击事件，6、计算取样点位置，7、执行取样动作。
        private bool TryGetValidSamplePoint (out Vector3 clickPoint)
        {
            clickPoint = Vector3. zero;
            var ray = FlightGlobals. fetch. mainCameraRef. ScreenPointToRay (Input. mousePosition);

            if ( !Physics. Raycast (ray, out RaycastHit hit) || hit. collider == null )
                return false;

            if ( hit. collider. gameObject. layer != 15 && hit. collider. gameObject. layer != 10 )
            {
                ScreenMessages. PostScreenMessage (
                    Localizer. Format ($"选择取样点为{hit. collider. gameObject. name}，第{hit. collider. gameObject. layer}层，无法取样，请选择地面取样点"),
                    2f, ScreenMessageStyle. UPPER_RIGHT);
                return false;
            }
            else
            {
                if ( Vector3. Distance (hit. point, ringCenter) > radius )
                {
                    ScreenMessages. PostScreenMessage (
                        Localizer. Format ($"超出机械臂最大工作范围，请在圆环范围内取点"),
                        2f, ScreenMessageStyle. UPPER_RIGHT);
                    return false;
                }
            }
            clickPoint = hit. point;
            return true;
        }
        public void Update ()
        {

            if ( !HighLogic. LoadedSceneIsFlight || !isUpdate )
                return;
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
            if ( !isTargetRingSetUp )
            {
                if (SampleTargetSet ())
                {
                    isTargetRingSetUp = true;
                }
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
                ScreenMessages. PostScreenMessage (
                    Localizer. Format ($"取样点为{targetPoint. ToString ()},开始计算并执行取样动作，请稍候"),
                    2f, ScreenMessageStyle. UPPER_RIGHT);
                isUpdate = false;
            }

        }
        private bool SampleTargetSet ()
        {
            //初始化所选择的机械臂的各项参数
            //开始计算取样范围，设置一个绿色圆环供玩家参考取样点
            Debug. Log ("开始设置绿环");
            armLength = CalculateArmLength ();//计算大臂长度
            positionTerrainHeight = CalculatePositionTerrainHeight (out ringCenter, out normal);
            if ( positionTerrainHeight < 0 )
            {
                Debug. Log ("错误:" + "飞船所处位置没有检测到地面实体");
                return false;
            }
            if ( positionTerrainHeight >= armLength / 2 )//这里的比较也有问题，暂时不管。
            {
                Debug. Log ("错误:" + "机械臂距离地面过高，超出最大工作范围，无法设置取样点");
                return false;
            }
            radius = ( float )Math. Sqrt (armLength * armLength - positionTerrainHeight * positionTerrainHeight);
            Debug. Log ("计算得出绿环半径为：" + radius);
            SetSampleMaxRange (radius, ringCenter, normal);
            return true;
        }
        public void SetSampleMaxRange (float ringRadius, Vector3 ringCenter, Vector3 normal)
        {
            Debug. Log ("圆环初始数据：  " + $"半径：{ringRadius}" + $"  中心：{ringCenter. ToString ()}" + $"  法向：{normal. ToString ()}");
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
        public float CalculateArmLength ()
        {
            Debug. Log ("开始计算臂长");
            float armLength = 0;
            List<Vector3> linkNodePos = new List<Vector3> ();
            foreach ( Part part in currentArmParts)
            {
                if ( part. FindModuleImplementing<ModuleCASNFP_RoboticArmPart> (). ArmPartType == ArmPartType. link )

                    linkNodePos. Add (part. gameObject. GetChild (part. FindModuleImplementing<ModuleRoboticServoHinge> (). servoTransformName). transform. position);
            }
            for ( int i = 1 ; i < linkNodePos. Count ; i++ )
            {
                armLength += ( linkNodePos[i] - linkNodePos[i - 1] ). magnitude;
            }
            Debug. Log ("臂长为" + armLength);
            return armLength * 2;//这里默认是大小臂相等且只有两段，是不对的，以后再修改这里臂长的计算方法。
        }
        public float CalculatePositionTerrainHeight (out Vector3 ringCenter, out Vector3 normal)
        {
            Debug. Log ("开始计算基座地形高度");
            float heightFromTerrain = -1f;
            Part basePart = null;
            foreach ( Part part in currentArmParts )
            {
                if ( part. FindModuleImplementing<ModuleCASNFP_RoboticArmPart> (). ArmPartType == ArmPartType. Base )
                {
                    basePart = part;
                    break;
                }
            }
            Vector3 basePos = basePart. gameObject. GetChild (basePart. FindModuleImplementing<ModuleRoboticServoHinge> (). baseTransformName). transform. position;
            Vector3 calNormal = ( Vector3 )FlightGlobals. getUpAxis (FlightGlobals. getMainBody (), basePos). normalized;
            float num = ( float )FlightGlobals. getMainBody (). Radius / 2;
            RaycastHit[] hits = Physics. RaycastAll (basePos, -calNormal, num);
            RaycastHit rightHit = new RaycastHit ();
            bool hasHit = false;
            foreach ( var item in hits )
            {
                if ( item. collider. gameObject. layer == 15 )
                {
                    hasHit = true;
                    rightHit = item;
                    break;
                }
            }
            if ( hasHit )
            {
                heightFromTerrain = rightHit. distance;
                ringCenter = rightHit. point;
                normal = rightHit. normal;
            }
            else
            {
                ringCenter = Vector3. zero;
                normal = Vector3. zero;
                return -1f;
            }
            return heightFromTerrain;
        }
    }
}
