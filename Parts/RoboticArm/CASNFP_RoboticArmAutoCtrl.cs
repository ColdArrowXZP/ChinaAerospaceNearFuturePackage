using ChinaAeroSpaceNearFuturePackage.UI;
using Expansions.Serenity;
using KSP. UI;
using System;
using System. Collections. Generic;
using System.Linq;
using UnityEngine;
namespace ChinaAeroSpaceNearFuturePackage.Parts.RoboticArm
{
    public class CASNFP_RoboticArmAutoCtrl : PartModule
    {
        [KSPField(isPersistant = true)]
        int currentIndex = 0;
        public int CurrentIndex
        {
            get
            {
                return currentIndex;
            }
            set
            {
                if (currentIndex != value)
                {
                    int oldIndex = currentIndex;
                    int newIndex = value;
                    currentIndex = value;
                    OnValueChanged(oldIndex, newIndex);
                }

            }
        }
        Dictionary<int, originalArm> roboticArmIndex;
        public Part[] CASNFP_RoboticArmPart;
        public Action<int, int> onValueChanged;
        /// <summary>
        /// 机械臂自动控制程序，先区分有几个机械臂，然后让玩家选择一个机械臂进行控制，判断机械臂类型，选择目标位置和目标姿态，控制机械臂到达目标位置和姿态，按计划开始工作。
        /// </summary>

        public void Start()
        {
            if (onValueChanged == null)
            {
                onValueChanged += new Action<int, int>(OnValueChanged);
            }
            roboticArmIndex = CheckAndDistinguishTheArm(CASNFP_RoboticArmPart);
            
            if (roboticArmIndex.Count == 0)
            {
                MessageBox.Instance.ShowDialog("错误", "没有找到机械臂部件，请检查机械臂部件是否正确连接。");
                return;
            }
            else
            {
                CreatArmSelectionWindow(roboticArmIndex);
            }
        }
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            Debug.LogError("代码ONSAVE开始执行");
            if (node.SetValue("currentIndex", currentIndex,true))
            {
                Debug.Log($"给currentIndex字段赋值为{currentIndex}");
            }
            ;
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            Debug.LogError("代码ONLoad开始执行");
            currentIndex = int.Parse(node.GetValue("currentIndex")) ;
        }
        public void OnDestroy()
        {
            onValueChanged -= new Action<int, int>(OnValueChanged);
        }
        /// <summary>
        /// CurrentIndex数值变化的事件执行逻辑
        /// </summary>
        /// <param name="oldIndex">前一个选择的机械臂序号，默认选择0号机械臂</param>
        /// <param name="newIndex">当前选择的机械臂序号</param>
        private void OnValueChanged(int oldIndex, int newIndex)
        {
            //刷新控制面板内容
            label.Update();
            Debug.Log($"原有机械臂序号是{oldIndex}，新的机械臂序号是{newIndex}");
            InitializeCurrentArm(roboticArmIndex[oldIndex], roboticArmIndex[newIndex]);

        }
        struct CheckAndDistinguishThePart
        {
            public ArmWorkType armWorkType;
            public Part part;
            public ArmPartType partType;
        }
        struct originalArm {
            public ArmWorkType armWorkType;
            public Part[] armParts;
        }
        #region 生成一个用户选择窗口，当用户选择机械臂时，CurrentIndex数值变化的事件触发
        /// <summary>
        /// 创建机械臂选择窗口
        /// </summary>
        DialogGUIToggleGroup toggleGroup;
        DialogGUILabel label;
        void CreatArmSelectionWindow(Dictionary<int, originalArm> roboticArmIndex)
        {
            if (MessageBox.Instance != null)
            {
                Destroy(MessageBox.Instance.gameObject);
            }
            int index = roboticArmIndex.Count;
            label = new DialogGUILabel(flexH: true, GetLabelString, 390f, 0f)
            {
                guiStyle = new UIStyle(HighLogic.UISkin.label)
                {
                    alignment = TextAnchor.MiddleLeft
                }
            };
            DialogGUIButton close = new DialogGUIButton("关闭面板", OnClosed, true);
            DialogGUIToggle[] dialogGUIToggles = new DialogGUIToggle[index];
            for (int i = 0; i < index; i++)
            {
                bool isCurrent = i == CurrentIndex ? true : false;
                dialogGUIToggles[i] = new DialogGUIToggle(isCurrent, $"{i + 1}  号", onSelected);
            }
            toggleGroup = new DialogGUIToggleGroup(dialogGUIToggles);
            var dialog = new MultiOptionDialog(
                "CASNFP_ControlPanel",
                "",
                "机械臂选择面板",
                HighLogic.UISkin,
                new Rect(0.5f, 0.5f, 400f, 200f),
                new DialogGUIVerticalLayout(
                    label,
                    new DialogGUIHorizontalLayout(toggleGroup),
                    new DialogGUIHorizontalLayout(
                        new DialogGUIFlexibleSpace(), close, new DialogGUIFlexibleSpace())
                )
            );
            PopupDialog.SpawnPopupDialog(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                dialog,
                false,
                HighLogic.UISkin
            );
        }

        private string GetLabelString()
        {
            int index = roboticArmIndex.Count;
            string armSelection = $"    在飞船检测到{index}个机械臂，请选择需要控制的机械臂：\n";
            foreach (var item in roboticArmIndex)
            {
                armSelection += $"    机械臂序号: {item.Key + 1}, 机械臂类型: {item.Value.armWorkType}\n";
            }
            armSelection += $"    当前控制的是{CurrentIndex}号机械臂，可选择其他机械臂。";
            return armSelection;
        }

        private void OnClosed()
        {
            Debug.Log("提示" + $"关闭" + CurrentIndex);
        }


        private void onSelected(bool arg1)
        {
            if (!arg1) return;
            if (arg1)
            {
                for (int i = 0; i < toggleGroup.children.Count; i++)
                {
                    if (toggleGroup.children[i] is DialogGUIToggle)
                    {
                        DialogGUIToggle toggle = toggleGroup.children[i] as DialogGUIToggle;
                        if (toggle.toggle.isOn)
                        {
                            CurrentIndex = i;
                        }
                    }
                }
            }
        }
        #endregion
        #region 检查机械臂部件并区分机械臂类型
        /// <summary>
        /// 检查机械臂部件并区分机械臂类型
        /// </summary>

        private Dictionary<int, originalArm> CheckAndDistinguishTheArm(Part[] cASNFP_RoboticArmPart)
        {
            Dictionary<int, originalArm> roboticArmIndex = new Dictionary<int, originalArm>();//机械臂索引，键为机械臂索引，值为机械臂类型和机械臂部件数组
            List<CheckAndDistinguishThePart> thePartsList = new List<CheckAndDistinguishThePart>();//机械臂部件列表
            // 遍历所有机械臂部件，获取机械臂类型和部件类型
            foreach (Part part in cASNFP_RoboticArmPart)
            {
                ModuleCASNFP_RoboticArmPart roboticArmModule = part.Modules.GetModule<ModuleCASNFP_RoboticArmPart>();
                CheckAndDistinguishThePart thePart = new CheckAndDistinguishThePart
                {
                    part = part,
                    armWorkType = roboticArmModule.ArmType,
                    partType = roboticArmModule.ArmPartType
                };
                thePartsList.Add(thePart);
            }
            // 定义四个列表来存储不同类型的机械臂部件
            List<CheckAndDistinguishThePart> changEList = new List<CheckAndDistinguishThePart>();
            List<CheckAndDistinguishThePart> tianGongList = new List<CheckAndDistinguishThePart>();
            List<CheckAndDistinguishThePart> grabbingList = new List<CheckAndDistinguishThePart>();
            List<CheckAndDistinguishThePart> cameraList = new List<CheckAndDistinguishThePart>();
            //将机械臂组件按照机械臂类型进行分类
            foreach (var item in thePartsList)
            {
                switch (item.armWorkType)
                {
                    case ArmWorkType.ChangE:
                        changEList.Add(item);
                        break;
                    case ArmWorkType.TianGong:
                        tianGongList.Add(item);
                        break;
                    case ArmWorkType.Grabbing:
                        grabbingList.Add(item);
                        break;
                    case ArmWorkType.Camera:
                        cameraList.Add(item);
                        break;
                    default:
                        Debug.LogError($"未知的机械臂类型: {item.armWorkType}");
                        break;
                }
            }
            //执行将分类后的机械臂部件按照顺序组成机械臂的方法
            if (changEList.Count > 0)
            {
                //如果有嫦娥机械臂部件，则将其分解成机械臂
                Dictionary<int, originalArm> _originalArms = Spit_Arm(changEList);

                if (_originalArms != null && _originalArms.Count > 0)
                {
                    if (roboticArmIndex.Count == 0)
                    {
                        //如果没有机械臂索引，则直接赋值
                        roboticArmIndex = _originalArms;
                    }
                    else
                        roboticArmIndex = MergeDictionaries<originalArm>(roboticArmIndex, _originalArms);
                }
            }
            if (tianGongList.Count > 0)
            {
                Dictionary<int, originalArm> _originalArms = Spit_Arm(tianGongList);
                if ((_originalArms != null))
                {
                    roboticArmIndex = MergeDictionaries<originalArm>(roboticArmIndex, _originalArms);
                }
            }
            if (grabbingList.Count > 0)
            {
                Dictionary<int, originalArm> _originalArms = Spit_Arm(grabbingList);
                if ((_originalArms != null))
                {
                    roboticArmIndex = MergeDictionaries<originalArm>(roboticArmIndex, _originalArms);
                }
            }
            if (cameraList.Count > 0)
            {
                Dictionary<int, originalArm> _originalArms = Spit_Arm(cameraList);
                if ((_originalArms != null))
                {
                    roboticArmIndex = MergeDictionaries<originalArm>(roboticArmIndex, _originalArms);
                }
            }
            return roboticArmIndex;
        }
        private static Dictionary<int, T> MergeDictionaries<T>(
        Dictionary<int, T> dictA,
        Dictionary<int, T> dictB)
        {
            var merged = new Dictionary<int, T>();

            // 添加字典A的所有元素
            foreach (var item in dictA)
            {
                merged.Add(item.Key, item.Value);
            }

            // 添加字典B的所有元素，键值从字典A最大键+1开始
            int startKey = dictA.Keys.Max() + 1;
            foreach (var item in dictB)
            {
                merged.Add(startKey++, item.Value);
            }

            return merged;
        }
        /// <summary>
        /// 这里将机械臂数组自动分组，存入序号和结构体originalArm的字典里。
        /// </summary>
        /// <param name="thisPartList">所有工作类型的part数组</param>
        /// <returns>返回一个字典，key是int值序号，value是originalArm结构体的机械臂Part</returns>
        private Dictionary<int, originalArm> Spit_Arm(List<CheckAndDistinguishThePart> thisPartList)
        {
            ArmWorkType workType = thisPartList[0].armWorkType;
            Dictionary<int, originalArm> roboticArmIndex = new Dictionary<int, originalArm>();
            if (workType == ArmWorkType.ChangE)
            {
                IEnumerable<CheckAndDistinguishThePart> armBase = thisPartList.Where(item => item.partType == ArmPartType.Base);
                for (int i = 0; i < armBase.Count(); i++)
                {
                    List<Part> parts = new List<Part>();
                    List<int> index = new List<int>();
                    for (int j = thisPartList.IndexOf(armBase.ElementAt(i)); j < thisPartList.Count(); j++)
                    {
                        parts.Add(thisPartList[j].part);
                        index.Add(thisPartList[j].part.vessel.Parts.IndexOf(thisPartList[j].part));
                        if (j + 1 >= thisPartList.Count()) break; //如果已经是最后一个部件，则结束循环
                        if (thisPartList[j + 1].partType == ArmPartType.Base) break; //如果下一个部件是基座，则结束循环
                    }
                    if (index.Count < 2) break;
                    Array.Sort(index.ToArray());
                    //如果部件索引不连续，则结束循环
                    for (int j = 0; j < index.Count - 1; j++)
                    {
                        if (index[j + 1] - index[j] != 1)
                        {
                            //说明机械臂部件不连续，中间夹杂了非CASNFP_RoboticArmPart部件
                            Debug.LogError($"机械臂部件不连续，索引为{index[j]}和{index[j + 1]}之间夹杂了非CASNFP_RoboticArmPart部件，请检查机械臂部件是否正确连接。");
                            return null;
                        }
                    }
                    originalArm original = new originalArm
                    {
                        armWorkType = workType,
                        armParts = parts.ToArray()
                    };
                    roboticArmIndex.Add(i, original);
                }
                return roboticArmIndex;
            }
            if (workType == ArmWorkType.TianGong)
            {
                IEnumerable<CheckAndDistinguishThePart> armBase = thisPartList.Where(item => item.partType == ArmPartType.work);
                for (int i = 0; i < armBase.Count(); i++)
                {
                    List<Part> parts = new List<Part>();
                    List<int> index = new List<int>();
                    for (int j = thisPartList.IndexOf(armBase.ElementAt(i)); j < thisPartList.Count(); j++)
                    {
                        parts.Add(thisPartList[j].part);
                        index.Add(thisPartList[j].part.vessel.Parts.IndexOf(thisPartList[i].part));
                        if (j + 1 >= thisPartList.Count()) break; //如果已经是最后一个部件，则结束循环
                        if (thisPartList[j].partType == ArmPartType.work && thisPartList[j + 1].partType == ArmPartType.work) break; //如果下一个部件是基座，则结束循环
                    }
                    if (index.Count < 2) break;
                    Array.Sort(index.ToArray());
                    //如果部件索引不连续，则结束循环
                    for (int j = 0; j < index.Count - 1; j++)
                    {
                        if (index[j + 1] - index[j] != 1)
                        {
                            //说明机械臂部件不连续，中间夹杂了非CASNFP_RoboticArmPart部件
                            Debug.LogError($"机械臂部件不连续，索引为{index[j]}和{index[j + 1]}之间夹杂了非CASNFP_RoboticArmPart部件，请检查机械臂部件是否正确连接。");
                            return null;
                        }
                    }
                    originalArm original = new originalArm
                    {
                        armWorkType = workType,
                        armParts = parts.ToArray()
                    };
                    roboticArmIndex.Add(i, original);
                }
                return roboticArmIndex;
            }
            if (workType == ArmWorkType.Grabbing || workType == ArmWorkType.Camera)
            {
                Debug.LogError($"机械臂类型{workType}暂不支持自动控制");
                return null; //抓取式机械臂和摄像类机械臂暂不支持自动控制
            }
            return null; //如果机械臂类型不在上述范围内，则返回null
        }
        #endregion

        #region 机械臂确定后，执行的自动控制逻辑。
        ///<summary>
        /// 初始化当前控制的机械臂
        ///</summary>   
        /// <param name="oldArm">前一个控制中的机械臂</param>
        /// <param name="newArm">当前正在控制中的机械臂</param>
        float[] servoTargetAnglesAtInitial;//关节生成时角度，记录在cfg中。
        private void InitializeCurrentArm(originalArm oldArm, originalArm newArm)
        {
            Part[] armParts = newArm.armParts;
            List<float> servoInitialAngles = new List<float>();
            foreach (Part part in armParts) 
            { 
                if (part.FindModuleImplementing<BaseServo>() is ModuleRoboticServoHinge servoHinge)
                {
                    servoInitialAngles.Add(servoHinge.modelInitialAngle);
                }
            }
        }
        #endregion

    }

}