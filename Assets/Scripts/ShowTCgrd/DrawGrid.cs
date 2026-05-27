using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DrawGrid : MonoBehaviour
{
    [SerializeField] private GameObject truncOctObj;

    [SerializeField] private Vector3Int GridSize;

    [SerializeField] private Vector3 offset;

    [SerializeField] private Transform viewPointTransform;
    [SerializeField] float fadeDist = 5;
    [SerializeField] float fontSizeD = 20;

    List<Transform> trs = new List<Transform>();

    float minDist = 1000;
    void Start()
    {
        Vector3 middlePoint = Vector3.zero;
        for (int i = 0; i < GridSize.x; i++)
           for (int j = 0; j < GridSize.y;j++)
                for (int k = 0; k < GridSize.z;k++)
                {
                    Vector3 pos = new Vector3(i, j * 0.5f, k);
                    if (j % 2 == 1) pos += new Vector3(0.5f, 0, 0.5f);
                    middlePoint += pos;
                    GameObject newObj = Instantiate(truncOctObj, pos, Quaternion.Euler(0, 45, 0), transform);
                    TMP_Text tm = newObj.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>();
                    newObj.name = $"[{i},{j},{k}]";
                    tm.text = $"[{i},{j},{k}]";
                    tm.transform.localScale *= 1.4f;
                    Vector3 vt = tm.transform.localScale;
                    tm.transform.localScale = new Vector3(-vt.x, vt.y, vt.z);
                    tm.transform.position -= (Vector3.up - Vector3.forward) * 0.35f;
                    Color textColor = Color.red;

                    trs.Add(tm.transform);
                    //tm.transform.rotation = Quaternion.Euler(90, 0, 0);
                    tm.transform.localPosition += offset;
                    newObj.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = Color.gray;
                    if (j % 2 == 1)
                    {
                        Color matCol = Color.blue;
                        //matCol.a = 0.4f;
                        newObj.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = matCol;
                        textColor = Color.red;
                    }
                    float dist = (tm.transform.position - Camera.main.transform.position).magnitude;
                    if (dist < minDist) minDist = dist;
                    tm.color = textColor;


                    //    new Color(i * 1f / (GridSize.x - 1), j * 1f / (GridSize.y - 1), k * 1f / (GridSize.z - 1));

                }

        middlePoint = middlePoint / (GridSize.x * GridSize.z * GridSize.y);
    }

    private void Update()
    {
        foreach (Transform t in trs)
        {
            t.LookAt(Camera.main.transform);
            TMP_Text tmtext = t.GetComponent<TMP_Text>();
            tmtext.fontSize = fontSizeD;
            Color textColor = tmtext.color;
            float dist = (t.position - Camera.main.transform.position).magnitude;
            textColor.a = Mathf.Clamp01(1 - (dist - minDist) / fadeDist);
            tmtext.color = textColor;
        }

    }

}
