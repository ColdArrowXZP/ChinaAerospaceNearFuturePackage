using UnityEngine;
using Expansions.Serenity;
using System;
using System.Collections.Generic;
using KSP.Localization;
using System.Collections;
namespace ChinaAeroSpaceNearFuturePackage.Parts
{
    public class CASNFP_Arm_SamleArm : PartModule
    {
        public enum SamleArmState
        {
            Extend = 1,
            Retract,
            Moving,
            Sample
        }
        [KSPField(isPersistant = true)]
        public string servoHingeTransformNames = "";
        GameObject clickPointObj;
        ModuleRoboticRotationServo node1;
        ModuleRoboticRotationServo node4;
        ModuleRoboticServoHinge node2;
        ModuleRoboticServoHinge node3;
        string[] servoHingeTransformName;
        [KSPField(isPersistant = true, guiFormat = "N1", guiActive = true, guiActiveEditor = false, guiName = "机械臂状态")]
        string armStateUIString;
        [KSPField(isPersistant = false, guiFormat = "N1", guiActive = false, guiActiveEditor = true, guiName = "取样机械臂")]
        string armEditorSay = "请勿在组装大厅里操作机械臂，避免危害坎巴拉\n工程师安全！\n请水平安装，否则后果不可预测！";
        private SamleArmState armState;
        public SamleArmState ArmState
        {
            get { return armState; }
            set { armState = value; }
        }
        Action<PartModule, float> setAngle;
        [KSPField(isPersistant = true)]
        bool isSampleComplete = false;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "表取/钻取")]
        [UI_Toggle(disabledText = "表取", scene = UI_Scene.Flight, enabledText = "钻取", affectSymCounterparts = UI_Scene.Flight)]
        public bool sampleMode = false;

        [KSPField(isPersistant = true)]
        public string srfSamplePoint = "";
        private Transform srfSamplePointObj;
        [KSPField(isPersistant = true)]
        public string drillSamplePoint = "";
        private Transform drillSamplePointObj;

        //右键菜单“自动化取样”
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "取样开关")]
        [UI_Toggle(disabledText = "关闭", scene = UI_Scene.Flight, enabledText = "开始", affectSymCounterparts = UI_Scene.Flight)]
        public bool startSample = false;

        //右键菜单“清空已取得样品”
        [KSPEvent(guiName = "清空已取得样品", guiActiveUncommand = true, active = true, guiActiveUnfocused = true, guiActive = true, guiActiveEditor = false)]
        protected void RestSample()
        {
            if (isSampleComplete)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("已清空所取样品，可进行再次取样", vessel.GetDisplayName()), 5f, ScreenMessageStyle.UPPER_LEFT);
                //在这里调用清空已取得样品的方法；
                isSampleComplete = false;
            }
        }

        //模组启动时调用
        public override void OnStart(StartState state)
        {
            //设置角度setAngle委托
            if (setAngle == null)
            {
                setAngle = new Action<PartModule, float>(SetAngle);
            }

            GameEvents.onPartDestroyed.Add(OnPartDestroyed);
            GameEvents.onPartActionUIShown.Add(OnPartActionUIShown);
            GameEvents.onFlightReady.Add(OnFlightReady);
            Fields["startSample"].OnValueModified += StartSample;
            Fields["sampleMode"].OnValueModified += SetSampleMode;
            //注册游戏事件//注册按钮监听
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (!string.IsNullOrEmpty(servoHingeTransformNames))
                {
                    servoHingeTransformName = new string[4];
                    servoHingeTransformName = servoHingeTransformNames.Split(',');
                }
                else
                {
                    Debug.Log("[CASNFP_Arm]:servoHingeTransformNames is NullOrEmpty");
                    return;
                }
                foreach (var item in part.Modules)
                {
                    if (item.moduleName.Equals("ModuleRoboticRotationServo"))
                    {
                        ModuleRoboticRotationServo obj = item as ModuleRoboticRotationServo;
                        if (obj.servoTransformName == servoHingeTransformName[0])
                        {
                            node1 = obj;

                            continue;
                        }
                        if (obj.servoTransformName == servoHingeTransformName[3])
                        {
                            node4 = obj;
                            continue;
                        }
                    }
                    else
                    {
                        if (item.moduleName.Equals("ModuleRoboticServoHinge"))
                        {
                            ModuleRoboticServoHinge obj = item as ModuleRoboticServoHinge;
                            if (obj.servoTransformName == servoHingeTransformName[1])
                            {
                                node2 = obj;
                                continue;
                            }
                            if (obj.servoTransformName == servoHingeTransformName[2])
                            {
                                node3 = obj;
                                continue;
                            }
                        }
                    }
                    if (node1 != null && node2 != null && node3 != null && node4 != null)
                    {
                        if (armState == 0)
                        {
                            armState = SamleArmState.Retract;
                        }
                        SetArmStateUIString();
                        break;
                    }
                }

            }
            Debug.Log("[CASNFP_Arm]OnStart");
        }

        private void SetSampleMode(object arg1)
        {
            if (armState != SamleArmState.Retract) 
            {
                if (sampleMode) sampleMode = false;else sampleMode = true;
            }
            
        }

        //右键菜单“自动化取样”监听的方法
        private void StartSample(object obj)
        {
            if (startSample)
            {
                if (isSampleComplete)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("取样已完成，如需再次取样请清空已取得的样品！", vessel.GetDisplayName()), 5f, ScreenMessageStyle.UPPER_LEFT);
                    startSample = false;
                    return;
                }
                if (!HighLogic.LoadedSceneIsFlight)
                {
                    Debug.LogWarning("[CASNFP_Arm]当前场景无法取样");
                    startSample = false;
                    return;
                }
                if (base.vessel.srfSpeed > 0.10000000149011612)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_8004352", vessel.GetDisplayName()), 5f, ScreenMessageStyle.UPPER_LEFT);
                    startSample = false;
                    return;
                }
                if (armState == SamleArmState.Retract) 
                {
                    StartCoroutine(DoExtend());
                }
            }
            if (!startSample)
            {
                if (armState != SamleArmState.Retract && armState != SamleArmState.Moving)
                {
                    StartCoroutine(DoRetract());
                }
                else 
                {
                    startSample = true;
                }
            }
        }
        private void SetArmStateUIString()
        {
            switch (armState)
            {
                case SamleArmState.Retract:
                    armStateUIString = "收回";
                    break;
                case SamleArmState.Extend:
                    armStateUIString = "展开";
                    break;
                case SamleArmState.Sample:
                    armStateUIString = "取样中";
                    break;
                case SamleArmState.Moving:
                    armStateUIString = "移动中";
                    break;
            }
        }

        //控制模组右键菜单
        private void OnPartActionUIShown(UIPartActionWindow data0, Part data1)
        {
            //if (data1 == part && data0 == part.PartActionWindow)
            //{
            //    foreach (var item in part.PartActionWindow.ListItems)
            //    {
            //        if (item.PartModule.moduleName != this.moduleName) item.gameObject.SetActive(false);
            //    }
            //}
        }
        private IEnumerator DoExtend()
        {
            armState = SamleArmState.Moving;
            SetArmStateUIString();
            setAngle.Invoke(node1, 90);
            yield return new WaitUntil(() => Math.Abs(node1CurrentAngle - 90) <= 0.1f);
            setAngle.Invoke(node2, 90);
            yield return new WaitUntil(() => Math.Abs(node2CurrentAngle - 90) <= 0.1f);
            armState = SamleArmState.Extend;
            SetArmStateUIString();
        }
        private IEnumerator DoRetract()
        {
            armState = SamleArmState.Moving;
            SetArmStateUIString();
            if (clickPointObj != null) Destroy(clickPointObj);
            if (Math.Abs(node2CurrentAngle - 90) > 0.1f)
            {
                setAngle.Invoke(node2, 90);
                yield return new WaitUntil(() => Math.Abs(node2CurrentAngle - 90) <= 0.1f);
            }
            if (Math.Abs(node4CurrentAngle - node4.launchPosition) > 0.1f)
            {
                setAngle.Invoke(node4, node4.launchPosition);
                yield return new WaitUntil(() => Math.Abs(node4CurrentAngle - node4.launchPosition) <= 0.1f);
            }
            if (Math.Abs(node3CurrentAngle - node3.launchPosition) > 0.1f)
            {
                setAngle.Invoke(node3, node3.launchPosition);
                yield return new WaitUntil(() => Math.Abs(node3CurrentAngle - node3.launchPosition) <= 0.1f);

            }
            if (Math.Abs(node2CurrentAngle - node2.launchPosition) > 0.1f)
            {
                setAngle.Invoke(node2, node2.launchPosition);
                yield return new WaitUntil(() => Math.Abs(node2CurrentAngle - node2.launchPosition) <= 0.1f);
            }
            if (Math.Abs(node1CurrentAngle - node1.launchPosition) > 0.1f)
            {
                setAngle.Invoke(node1, node1.launchPosition);
                yield return new WaitUntil(() => Math.Abs(node1CurrentAngle - node1.launchPosition) <= 0.1f);
            }
            armState = SamleArmState.Retract;
            SetArmStateUIString();

        }

        //角度setAngle委托方法
        private void SetAngle(PartModule item, float arg2)
        {
            ModuleRoboticRotationServo roboticRotationServo = null;
            ModuleRoboticServoHinge roboticServoHinge = null;
            if (item.moduleName.Equals("ModuleRoboticRotationServo")) roboticRotationServo = item as ModuleRoboticRotationServo;
            if (item.moduleName.Equals("ModuleRoboticServoHinge")) roboticServoHinge = item as ModuleRoboticServoHinge;
            if (roboticRotationServo != null) roboticRotationServo.targetAngle = arg2;
            if (roboticServoHinge != null) roboticServoHinge.targetAngle = arg2;
        }
        //设置关节间刚体连接
        private void OnFlightReady()
        {
            if (srfSamplePointObj == null)
            {
                srfSamplePointObj = part.gameObject.transform.Find(srfSamplePoint);
            }
            if (drillSamplePointObj == null)
            {
                drillSamplePointObj = part.gameObject.transform.Find(drillSamplePoint);
            }
            node2.MovingObject().GetComponent<ConfigurableJoint>().connectedBody = node1.MovingObject().GetComponent<Rigidbody>();
            node3.MovingObject().GetComponent<ConfigurableJoint>().connectedBody = node2.MovingObject().GetComponent<Rigidbody>();
            node4.MovingObject().GetComponent<ConfigurableJoint>().connectedBody = node3.MovingObject().GetComponent<Rigidbody>();
            Debug.Log("[CASNFP_Arm]刚体连接设置完成");
        }
        //移除游戏事件
        private void OnPartDestroyed(Part data)
        {
            if (data == part)
            {
                GameEvents.onFlightReady.Remove(OnFlightReady);
                GameEvents.onPartActionUIShown.Remove(OnPartActionUIShown);
                GameEvents.onPartDestroyed.Remove(OnPartDestroyed);
            }
        }
        float node1CurrentAngle = 0;
        float node2CurrentAngle = 0;
        float node3CurrentAngle = 0;
        float node4CurrentAngle = 0;
        public override void OnUpdate()
        {
            if (armState == SamleArmState.Moving)
            {
                node1CurrentAngle = currentTransformAngle(node1);
                node2CurrentAngle = currentTransformAngle(node2);
                node3CurrentAngle = currentTransformAngle(node3);
                node4CurrentAngle = currentTransformAngle(node4);
            }
            if (armState == SamleArmState.Extend && armState != SamleArmState.Moving)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("请左键选择取样地点", vessel.GetDisplayName()), 1f, ScreenMessageStyle.UPPER_LEFT);
                //设置一个取样点
                if (Input.GetMouseButtonDown(0))
                {
                    Camera cam = FlightGlobals.fetch.mainCameraRef;
                    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    Vector3 clickPoint = Vector3.zero;
                    if (Physics.Raycast(ray, out hit))
                    {
                        if (hit.collider != null)
                        {
                            if (hit.collider.gameObject.layer == 15)
                            {
                                clickPoint = hit.point;
                            }
                            else
                            {
                                ScreenMessages.PostScreenMessage(Localizer.Format("选择取样地点为" + hit.collider.gameObject.name + "不正确，请选择地面取样点", vessel.GetDisplayName()), 2f, ScreenMessageStyle.UPPER_LEFT);
                                return;
                            }
                        }
                    }
                    if (clickPoint == Vector3.zero) return;//点击地面失败
                    if (!armCanReach(sampleMode, clickPoint, node1, node2, node3, node4))
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("超出机械臂工作范围，请重新选择取样地点!", vessel.GetDisplayName()), 5f, ScreenMessageStyle.UPPER_LEFT);
                        return;
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("取样点选择符合要求，取样开始,请稍后...!", vessel.GetDisplayName()), 5f, ScreenMessageStyle.UPPER_LEFT);
                    }
                    //生成一个红色圆球。粒子着色器。
                    Material material = new Material(Shader.Find("KSP/Particles/Additive"));
                    material.SetColor("_TintColor", Color.red);
                    clickPointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    clickPointObj.transform.position = clickPoint;
                    clickPointObj.transform.localScale = Vector3.one * 0.1f;
                    clickPointObj.GetComponent<MeshRenderer>().material = material;
                    StartCoroutine(DoSampling());
                }
            }

            //开始取样
            if (armState == SamleArmState.Sample)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("正在取样中，请等候本次取样完成!,\"X\"键中断取样", vessel.GetDisplayName()), 1f, ScreenMessageStyle.UPPER_LEFT);
                //在这里设置各种取样动作：1、设置机械臂角度；2、播放钻取动画；3、检测资源
                //右键终止采样
                if (Input.GetKeyDown(KeyCode.X))
                {
                    Fields["startSample"].SetValue(false, this);
                    ScreenMessages.PostScreenMessage("已中断取样工作，机械臂重置，请稍后！", 5f, ScreenMessageStyle.UPPER_LEFT);
                }

                ////取样已满
                //isSampling = false;
                //isSampleComplete = true;
                //canStartSample = false;
            }

        }

        private IEnumerator DoSampling()
        {
            armState = SamleArmState.Moving;
            SetArmStateUIString();
            setAngle.Invoke(node1, rootAngle);
            yield return new WaitUntil(() => Math.Abs(node1CurrentAngle - rootAngle) < 0.1f);
            if (BAngle <= 90)
            {
                setAngle.Invoke(node3, BAngle);
                yield return new WaitUntil(() => Math.Abs(node3CurrentAngle - BAngle) < 0.1f);
            }
            if (BAngle > 90)
            {
                setAngle.Invoke(node3, 90);
                yield return new WaitUntil(() => Math.Abs(node3CurrentAngle - 90) < 0.1f);
                setAngle.Invoke(node3, BAngle);
                yield return new WaitUntil(() => Math.Abs(node3CurrentAngle - (180 - BAngle)) < 0.1f);
            }
            if (CAngle <= 90 && CAngle >= -90)
            {
                setAngle.Invoke(node4, CAngle);
                yield return new WaitUntil(() => Math.Abs(node4CurrentAngle - CAngle) < 0.1f);
            }
            if (CAngle > 90)
            {
                setAngle.Invoke(node4, 90);
                yield return new WaitUntil(() => Math.Abs(node4CurrentAngle - 90) < 0.1f);
                setAngle.Invoke(node4, CAngle);
                yield return new WaitUntil(() => Math.Abs(node4CurrentAngle - (180 - CAngle)) < 0.1f);
            }
            if (CAngle < -90)
            {
                setAngle.Invoke(node4, -90);
                yield return new WaitUntil(() => Math.Abs(node4CurrentAngle - (-90)) < 0.1f);
                setAngle.Invoke(node4, CAngle);
                yield return new WaitUntil(() => Math.Abs(node4CurrentAngle - (-180 - CAngle)) < 0.1f);
            }
            setAngle.Invoke(node2, AAngle);
            yield return new WaitUntil(() => Math.Abs(node2CurrentAngle - AAngle) < 0.1f);
            armState = SamleArmState.Sample;
            SetArmStateUIString();
            Debug.Log("到达取样点");
            //播放钻取动画
            
        }

        float BAngle;
        float AAngle;
        float CAngle;
        float rootAngle;
        private bool armCanReach(bool sampleMode, Vector3 targetPoint, ModuleRoboticRotationServo node1, ModuleRoboticServoHinge node2, ModuleRoboticServoHinge node3, ModuleRoboticRotationServo node4)
        {
            Debug.Log("载具与世界坐标系夹角 = "+Vector3.Angle(Vector3.up,vessel.up));
            //计算根物体旋转角度(限制在-90,90度)
            #region
            Vector3 calA = Vector3.ProjectOnPlane(targetPoint - node1.MovingObject().transform.position, node1.MovingObject().transform.up);
            float calAB = Vector3.Angle(calA, -node1.MovingObject().transform.forward);
            Vector3 dirction = Vector3.Cross(calA, -node1.MovingObject().transform.forward);

            if (calAB > 90)
            {
                Debug.Log("取样点超出node1角度限制，重新选点");
                return false;
            }
            else
            {
                if (dirction.y < 0)
                {
                    rootAngle = node1CurrentAngle - calAB;
                }
                else
                {
                    rootAngle = node1CurrentAngle + calAB;
                }
            }
            Debug.Log("rootAngle=" + rootAngle);

            #endregion

            //计算目标点与节点1、2能够构成三角形
            #region
            Quaternion.AngleAxis(rootAngle, node1.MovingObject().transform.up);

            float distance1to2 = Vector3.Distance(node1.MovingObject().transform.position, node2.MovingObject().transform.position);//小臂1长度
            float distance2to3 = Vector3.ProjectOnPlane((node3.MovingObject().transform.position - node2.MovingObject().transform.position), node2.MovingObject().transform.right).magnitude;//大臂1长度
            float distance3to4 = Vector3.ProjectOnPlane((node4.MovingObject().transform.position - node3.MovingObject().transform.position), node3.MovingObject().transform.right).magnitude;//大臂2长度
            float targetDistance1 = Vector3.Distance(node2.MovingObject().transform.position, targetPoint);//计算与目标直线距离（斜边）
            float targetDistance =(float)Math.Sqrt(Math.Pow(distance1to2,2)+Math.Pow(targetDistance1,2)) ;
            Debug.Log(distance2to3 + ";" + distance3to4 + ";" + targetDistance + ";");
            if (distance2to3 + distance3to4 < targetDistance || distance2to3 + targetDistance < distance3to4 || targetDistance + distance3to4 < distance2to3)
            { Debug.Log("取样点超出超出长度限制，请重新选点"); return false; }//两边之和小于第三边。
            #endregion
            //计算三条边构成的三角形各夹角（cosA = (b2+c2-a2)/(2bc)）
            float calXA = (float)(Math.Acos((Math.Pow(targetDistance, 2) + Math.Pow(distance2to3, 2) - Math.Pow(distance3to4, 2)) / (2 * targetDistance * distance2to3)) * (180 / Math.PI));
            float calXB = Vector3.Angle(targetPoint - node1.MovingObject().transform.position, -node1.MovingObject().transform.up);
            float calAAngle = calXA - (90 - calXB);
            Debug.Log(calXA + ";" + calXB + ";" + calAAngle + ";");
            if (calAAngle > 90 || calAAngle < 0)
            {
                Debug.Log("取样点超出node2角度限制，重新选点"); return false;
            }
            else
            {
                AAngle = calAAngle;
            }
            Debug.Log(AAngle);


            float calBAngle1 = (float)(Math.Acos((Math.Pow(distance2to3, 2) + Math.Pow(distance3to4, 2) - Math.Pow(targetDistance, 2)) / (2 * distance2to3 * distance3to4)) * (180 / Math.PI));
            if (sampleMode) //钻取模式
            {
                CAngle = 180 - (calBAngle1 - (90 - AAngle));
            }
            else  //表取模式
            {
                CAngle = (180 - (calBAngle1 - (90 - AAngle))) - 90;
            }
            Debug.Log("CAngle=" + CAngle);
            if (calBAngle1 >= 180 || calBAngle1 <= 0)
            {
                Debug.Log("取样点超出node3角度限制，重新选点"); return false;
            }
            else
            {
                BAngle = calBAngle1;
            }
            Debug.Log(BAngle);
            //其次计算对机械臂1号臂角度限制
            #region



            //float targetDistance2 = Vector3.Distance(node1.transform.position, nodeTargetPos);//计算与目标平面距离[(临边)]
            //float targetHigh = (float)Math.Sqrt(Math.Pow(targetDistance2, 2) - Math.Pow(targetDistance, 2));//计算与目标垂直高度（对边）

            //Quaternion rot = Quaternion.LookRotation(targetObj.transform.position - node1.transform.position, node1.transform.right);//计算转点1与目标的角度。
            #endregion
            //所有条件都满足要求，返回true值
            return true;
        }
        
        float currentTransformAngle(ModuleRoboticRotationServo servo)
        {
            
            float num = 0f;
            if (servo.MovingObject() == null)
            {
                return num;
            }
            switch (servo.mainAxis)
            {
                case "Z":
                    num = servo.MovingObject().transform.localEulerAngles.z;
                    break;
                case "Y":
                    num = servo.MovingObject().transform.localEulerAngles.y;
                    break;
                case "X":
                    num = servo.MovingObject().transform.localEulerAngles.x;
                    break;
            }
            if (num > 180f)
            {
                num -= 360f;
            }
            return num;
        }
        float currentTransformAngle(ModuleRoboticServoHinge servo)
        {

            float num = 0f;
            if (servo.MovingObject() == null)
            {
                return num;
            }
            switch (servo.mainAxis)
            {
                case "Z":
                    num = servo.MovingObject().transform.localEulerAngles.z;
                    break;
                case "Y":
                    num = servo.MovingObject().transform.localEulerAngles.y;
                    break;
                case "X":
                    num = servo.MovingObject().transform.localEulerAngles.x;
                    break;
            }
            if (num > 180f)
            {
                num -= 360f;
            }
            return num;
        }
    }


}









