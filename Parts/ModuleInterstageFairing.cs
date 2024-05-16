using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage.Parts
{
    public class ModuleInterstageFairing : PartModule
    {
        Vector3 partOldPosition;
        Vector3 partOldScale;
        Quaternion partOldRotation;
        bool isSeted = false;
        bool isRecord;
        bool hasChild = false;
        bool isRecordChild = false;
        Vector3 childOldScale;
        Part childPart;
        [KSPField]
        Direction direction = Direction.down;
        [KSPField]
        string topNodeName = "top";
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "调节部件高度为（米）")]
        [UI_FloatRange(minValue = 0.1f, maxValue = 10f, scene = UI_Scene.Editor)]
        internal float PartHigh = 1f;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "调节部件半径为（米）")]
        [UI_FloatRange(minValue = 0.1f, maxValue = 10f, stepIncrement = 0.05f, scene = UI_Scene.Editor)]
        internal float PartRadius = 0.65f;

        enum Direction
        {
            up = 1,
            down = -1
        }

        public override void OnStart(StartState state)
        {
            topNodeName = SettingLoader.CASNFP_GlobalSettings.GetNode("Parts_Setting").GetNode("InterstageFairing_Setting").GetValue("topNodeName");
            partOldRotation = part.gameObject.transform.localRotation;
            partOldScale = part.gameObject.transform.localScale;
            GameEvents.onPartAttach.Add(ThisPartAttach);
        }

        private void ThisPartAttach(GameEvents.HostTargetAction<Part, Part> data)
        {
            if (data.host == this.part && isRecord == true)
            {
                isRecord = false;
            }
            if (data.target == this.part)
            {
                childPart = data.host;
                Debug.Log("这个物体被" + childPart.name + "堆叠");
                hasChild = true;
            }
        }

        public void OnDestroy()
        {
            GameEvents.onPartAttach.Remove(ThisPartAttach);
        }
        [KSPEvent(guiName = "确认缩放", guiActiveEditor = true, isPersistent = true)]
        public void KSPEventModScale()
        {
            //判断堆叠方向。
            AttachNode attachNode = part.FindAttachNode(topNodeName);
            if (!(attachNode.FindOpposingNode() is AttachNode)) direction = Direction.up;
            //还原缩放和角度到初始值。
            part.gameObject.transform.localScale = partOldScale;
            part.gameObject.transform.localRotation = partOldRotation;
            //判断子级是否还存在，记录子级原缩放
            if (hasChild)
            {
                if (part.children.Contains(childPart))
                {

                    if (!isRecordChild)
                    {
                        childOldScale = childPart.gameObject.transform.localScale;
                        isRecordChild = true;
                    }
                }
                else hasChild = false;
            }
            //判断这个物体是否被重新放置了，记录新的初始坐标或者重置坐标。
            if (isRecord == false)
            {
                partOldPosition = part.gameObject.transform.localPosition;
                isRecord = true;
            }
            else part.gameObject.transform.localPosition = partOldPosition;

            ModelScale(part, PartHigh, PartRadius, direction);
            //重置子级缩放
            if (hasChild && isRecordChild)
            {
                Debug.Log("重置子级缩放");
                childPart.gameObject.transform.localScale = childOldScale;
            }
        }
        //求取缩放：
        private void ModelScale(Part part, float high, float radius, Direction direction)
        {
            float modelSizeZ = part.FindModelComponent<MeshFilter>().mesh.bounds.max.z;
            float moveDistance = (int)direction * (high - modelSizeZ * 2) / 2;
            float YMove = part.gameObject.transform.localPosition.y + moveDistance;
            part.gameObject.transform.localPosition = new Vector3(part.gameObject.transform.localPosition.x, YMove, part.gameObject.transform.localPosition.z);
            float scaleYSize = high / (modelSizeZ * 2);
            float modelSizeXY = part.FindModelComponent<MeshFilter>().mesh.bounds.max.x;
            float scaleXZSize = radius / modelSizeXY;
            part.gameObject.transform.localScale = new Vector3(scaleXZSize, scaleYSize, scaleXZSize);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (!isSeted)
            {
                Vector3 vector = part.gameObject.transform.localPosition;
                ModelScale(part, PartHigh, PartRadius, direction);
                part.gameObject.transform.localPosition = vector;
                isSeted = true;
                Debug.Log("变形体加载完成");
            }
        }
    }
}
