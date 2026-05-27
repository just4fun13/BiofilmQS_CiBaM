using Cinemachine;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public class CameraFocusMan
    {
        private static Camera mainCam;

        private static Transform PlaneTransform;
        private static Transform ViewportTransform;
        private static Transform TopViewCamT;
        private static Transform FrontViewCamT;
        private static Transform SideViewCamT;
        private static int view2DsideOffset = 1000;
        private static CinemachineTransposer cinemachineTransposer;

        private static string sideCamTag = "SideViewCam";
        private static string frontCamTag = "FrontViewCam";
        private static string topCamTag = "TopViewCam";
        private static string viewPortTag = "ViewPortPoint";
        private static string planeTag = "Plane";
        private static string vcTag = "VirtualCam";


        static CameraFocusMan()
        {
            mainCam = Camera.main;
            SideViewCamT = GameObject.FindGameObjectWithTag(sideCamTag).transform;
            FrontViewCamT = GameObject.FindGameObjectWithTag(frontCamTag).transform;
            TopViewCamT = GameObject.FindGameObjectWithTag(topCamTag).transform;
            ViewportTransform = GameObject.FindGameObjectWithTag(viewPortTag).transform;
            PlaneTransform = GameObject.FindGameObjectWithTag(planeTag).transform;
            PlaneTransform = GameObject.FindGameObjectWithTag(planeTag).transform;
            cinemachineTransposer = GameObject.FindGameObjectWithTag(vcTag).GetComponent<CinemachineVirtualCamera>().GetComponentInChildren<CinemachineTransposer>();
        }

        public static void Focus3DCamera(Vector3Int gridSize)
        {
            int AreaWidth = gridSize.x;//sizeFromImg.x;// 
            int AreaHeight = gridSize.y;
            int AreaLength = gridSize.z;//sizeFromImg.y;// 
            mainCam.orthographic = false;
            mainCam.ResetProjectionMatrix();
            PlaneTransform.localScale = new Vector3(0.11f * AreaWidth, 1, 0.11f * AreaLength);
            PlaneTransform.localPosition = new Vector3(AreaWidth / 2f, -1, AreaLength / 2f);
            ViewportTransform.localPosition = new Vector3(AreaWidth / 2f, AreaHeight / 3f, AreaLength / 2f);
            cinemachineTransposer.m_FollowOffset = new Vector3(0, 1.6f * AreaHeight, 0.9f * AreaLength);
            TopViewCamT.localPosition = new Vector3(AreaWidth / 2f, 500, AreaLength / 2f);
            TopViewCamT.GetComponent<Camera>().orthographicSize = Mathf.Max(AreaWidth, AreaLength) / 2 + 1;
            FrontViewCamT.localPosition = new Vector3(AreaWidth / 2f, AreaHeight / 2f, -500);
            FrontViewCamT.GetComponent<Camera>().orthographicSize = Mathf.Max(AreaWidth, AreaHeight) / 2 + 1;
            SideViewCamT.localPosition = new Vector3(-500, AreaHeight / 2f, AreaLength / 2f);
            SideViewCamT.GetComponent<Camera>().orthographicSize = Mathf.Max(AreaHeight, AreaLength) / 2 + 1;



            /*            TopViewCamT.localPosition = new Vector3(AreaWidth / 2f, 1.5f, AreaLength / 2f);
                        FrontViewCamT.localPosition = new Vector3(AreaWidth / 2f, 10, AreaLength / 2f);
                        SideViewCamT.localPosition = new Vector3(view2DsideOffset + AreaWidth / 2f, AreaHeight / 2, -5);
                        FrontViewCamT.GetComponent<Camera>().orthographicSize = Mathf.Max(AreaWidth, AreaHeight) / 2 + 1;
                        TopViewCamT.GetComponent<Camera>().orthographicSize = Mathf.Max(AreaWidth, AreaHeight) / 2 + 6;
                        SideViewCamT.GetComponent<Camera>().orthographicSize = Mathf.Max(AreaWidth, AreaHeight) / 2 + 1;
            */
            //            GameObject.FindGameObjectWithTag("CamCaptions").GetComponent<CanvasGroup>().alpha = 1;
        }
    }
}
