using KSP.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChinaAeroSpaceNearFuturePackage.Parts
{
    public class Arm_UI : MonoBehaviour
    {
        
        private Arm_UI() { }
        private static GameObject windowPrefab;
        public static GameObject arm_Control_window { get; private set; }
        private static Arm_UI instance = null;
        public static Arm_UI Instance
        {
            get 
            {
                if (instance == null)
                {
                    instance = new Arm_UI();
                }
                return instance;
            }
        }
        public void CreatUI()
        {
            Debug.Log("开始运行UI生成代码");
            ConfigNode node = SettingLoader.CASNFP_GlobalSettings;
            string _UIPrefabName = node.GetNode("UI_Setting").GetValue("UIPrefabName");
            if (windowPrefab == null && SettingLoader.UIBundle != null)
            {
                Debug.Log("窗口资源名称：" + SettingLoader.UIBundle.name);
                windowPrefab = SettingLoader.UIBundle.LoadAsset<GameObject>(_UIPrefabName);
                Debug.Log("窗口预制体名称：" + windowPrefab.name);
            }
            Debug.Log("开始协程执行生成窗体"+ windowPrefab.name);
            while (arm_Control_window != null) 
            {
                Debug.Log("窗体处于生成状态");
                return;
            }
            arm_Control_window = Instantiate(windowPrefab, new Vector3(100,100,100), Quaternion.identity);
            Debug.Log("UI已经显示");
            if (arm_Control_window == null)
            {
                Debug.Log("CASNFP_Control_window is null");
            }
            
        }
        public void CloseUI() 
        {
            if (arm_Control_window!=null)
            {
                Destroy (arm_Control_window);
            }
            
        }
        
    }

}


