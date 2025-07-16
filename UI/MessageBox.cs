using System;
using System. Collections. Generic;
using System. Linq;
using System. Text;
using System. Threading. Tasks;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage. UI
{
    public class MessageBox : MonoBehaviour
    {
        private PopupDialog dialog;
        private float timeoutTimer;
        private bool isTimeoutActive;
        private static MessageBox instance;
        public static MessageBox Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("MessageBox");
                    instance = obj.AddComponent<MessageBox>();
                }
                return instance;
            }
        }
        /// <summary>
        /// 显示一个消息框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        /// <param name="onConfirm">确认按钮的回调</param>
        /// <param name="onCancel">取消按钮的回调</param>
        private void awake()
        {
            // 确保只有一个实例存在
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        public void ShowDialog (string title, string message, float waitTime = 0f)
        {
            string message1 = "    " + message;
            DialogGUIButton confirmBtn = new DialogGUIButton ("确认", () =>
            {
                if ( dialog != null )
                    StopTimeout ();
                dialog. Dismiss ();
            }, 50, 25, true, new UISkinDef (). button);

            MultiOptionDialog dialogContent = new MultiOptionDialog (
            "infoDialog",
            "",
            title,
            HighLogic. UISkin,
            new DialogGUIVerticalLayout (
                new DialogGUISpace (5),
                new DialogGUIHorizontalLayout (
                    new DialogGUIFlexibleSpace (),
                    new DialogGUILabel (message1, true, false),
                    new DialogGUIFlexibleSpace ()
                ),
                new DialogGUISpace (10),
                new DialogGUIHorizontalLayout (
                    new DialogGUIFlexibleSpace (),
                    confirmBtn,
                    new DialogGUIFlexibleSpace ()
                )
            )
        );

            dialog = PopupDialog. SpawnPopupDialog (
                new Vector2 (0.5f, 0.5f),
                new Vector2 (0.5f, 0.5f),
                dialogContent,
                false,
                HighLogic. UISkin
            );
            if ( waitTime > 0 )
                StartTimeout (waitTime);
        }
        void StartTimeout (float duration)
        {
            isTimeoutActive = true;
            timeoutTimer = duration;
        }

        void StopTimeout ()
        {
            isTimeoutActive = false;
        }

        void Update ()
        {
            if ( isTimeoutActive && dialog != null )
            {
                timeoutTimer -= Time. deltaTime;
                if ( timeoutTimer <= 0 )
                {
                    dialog. Dismiss ();
                    isTimeoutActive = false;
                }
            }
        }
        void OnDestroy ()
        {
            if ( dialog != null )
            {
                dialog. Dismiss ();
                dialog = null;
            }
            StopTimeout ();
        }
    }
}
