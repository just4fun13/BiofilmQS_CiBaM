using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.MVVM_CA.Utils
{
    public class ParsePointsFromImage : MonoBehaviour
    {

        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private float minValX = 1;
        [SerializeField] private float maxValX = 1;
        [SerializeField] private float minValY = 1;
        [SerializeField] private float maxValY = 1;

        private Camera cam;


        private bool setMin = false;
        private bool setMax = false;

        private Vector2 minPoint;
        private Vector2 maxPoint;


        private List<Vector2> pointsRecord = new List<Vector2>();

        private void Start()
        {
            cam = Camera.main;
            Debug.Log($"Image to points inited;");
            Debug.Log($"Click on left bottom and right top points on graph");
        }



        private void ShowMouseFocus(Vector2 pos)
        {
            lineRenderer.SetPosition(0, new Vector2(-500, pos.y));
            lineRenderer.SetPosition(1, new Vector2(500, pos.y));
            lineRenderer.SetPosition(2, new Vector2(pos.x, -500));
            lineRenderer.SetPosition(3, new Vector2(pos.x, 500));
        }


        private void ProcessClick(Vector2 pos)
        {
            if (!setMin)
            {
                setMin = true;
                minPoint = pos;
                Debug.Log($"Min point is set to {pos}");
                return;
            }
            if (!setMax)
            {
                setMax = true;
                maxPoint = pos;
                Debug.Log($"Max point is set to {pos}");
                return;
            }
            Vector2 p2 = new Vector2(
                   minValX + (maxValX - minValX ) *  (pos.x - minPoint.x) / (maxPoint.x - minPoint.x),
                   minValY + (maxValY - minValY ) *  (pos.y - minPoint.y) / (maxPoint.y - minPoint.y));

            pointsRecord.Add(p2);   
        }




        private void Update()
        {
            ShowMouseFocus(cam.ScreenToWorldPoint(Input.mousePosition));
            if (Input.GetMouseButtonUp(0))
                ProcessClick(Input.mousePosition);
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log($"{string.Join(',',pointsRecord)}");
                pointsRecord.Clear();
            }

        }


    }
}
