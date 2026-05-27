using Assets.Scripts.NewGeneration;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TruncatedOctahedron : MonoBehaviour
{
    private const float s2d2 = 0.70710678118654752440084436210485f;
    private const float s2   = 1.4142135623730950488016887242097f;

    public Vector3 offset = new Vector3(0.5f, 0.0f, 0.5f);

    [SerializeField] private Vector3Int initPosition;
    [SerializeField] private int NbrsIncludeCount;
    [SerializeField] private Base3D geometryBase3D;
    Vector3[] vertices = new Vector3[] {
            new Vector3( s2d2, 2,  s2d2),            //0
            new Vector3( s2d2, 2, -s2d2),            //1
            new Vector3( -s2d2, 2, -s2d2),           //2
            new Vector3(-s2d2, 2,  s2d2),            //3   
                                                    
            new Vector3( s2d2, -2,  s2d2),           //4
            new Vector3(-s2d2, -2,  s2d2),           //5 
            new Vector3( -s2d2, -2, -s2d2),          //6
            new Vector3( s2d2, -2, -s2d2),           //7

            new Vector3( s2d2,       0,    s2+s2d2), //8
            new Vector3( s2,        -1,    s2),      //9
            new Vector3( s2+s2d2,    0,    s2d2   ), //10
            new Vector3( s2,         1,    s2),      //11

            new Vector3( -s2d2,       0,  -s2-s2d2), //12
            new Vector3( -s2,        -1,      -s2),  //13
            new Vector3( -s2-s2d2,    0,  -s2d2   ), //14
            new Vector3( -s2,         1,      -s2),  //15

            new Vector3( -s2,         1,      s2),   //16
            new Vector3( -s2-s2d2,    0,     s2d2),  //17
            new Vector3( -s2,        -1,      s2),   //18
            new Vector3( -s2d2,    0,    s2d2+s2 ),  //19

            new Vector3( s2,        -1,      -s2),   //20
            new Vector3( s2d2,    0,     -s2d2-s2),  //21
            new Vector3( s2,         1,      -s2),   //22
            new Vector3( s2d2+s2,    0,    -s2d2 ),  //23


        };

    int[] triangles = new int[] {
             0,  1,  2,
             0,  2,  3,

             4,  5,  6,
             4,  6,  7,

             8,  9, 10,
             8, 10, 11,

            12, 13, 14,
            12, 14, 15,

            16, 17, 18,
            16, 18, 19,

            20, 21, 22,
            20, 22, 23,

/*
            0, 11, 1,
            22, 1, 11,
            22, 11, 10,
            10, 23, 22,
*/
            4, 7, 22,
            22, 11, 4,
 //           20, 11, 10,
  //          10, 23, 20,



        };

    [SerializeField] private GameObject octObj;
    
    public Vector3 BaseVector = Vector3.zero;

    public Vector3Int[] truncsPoses;
    private List<GameObject> truncObjs = new List<GameObject>();
    private GameObject initObject;

    void Start()
    {
        initObject = Instantiate(octObj);
        initObject.GetComponent<MeshRenderer>().material.color = Color.red;
        initObject.transform.position = GetPosition(initPosition);
        Vector3Int[] nbrs = geometryBase3D.GetNbrs(initPosition);
        for (int i = 0; i < nbrs.Length; i++)
        {
            GameObject newTrunc = Instantiate(octObj, GetPosition(truncsPoses[i]), Quaternion.identity);
            truncObjs.Add(newTrunc);
            if (i < 6)
                truncObjs[i].GetComponent<MeshRenderer>().material.color = Color.green;
        }
    }

    private void DrawAll()
    {
        initObject.transform.position = GetPosition(initPosition);
        Vector3Int[] nbrs = geometryBase3D.GetNbrs(initPosition);
        for (int i = 0; i < NbrsIncludeCount; i++)
        {
            if (i < NbrsIncludeCount)
            {
                truncObjs[i].SetActive(true);
                truncObjs[i].transform.position = GetPosition(nbrs[i] + initPosition);
            }
            else
                truncObjs[i].SetActive(false);
        }
    }

    private void Update()
    {
        DrawAll();
    }

    public Vector3 GetPosition(Vector3Int id)
    {
        return geometryBase3D.GetPosition(id);
    }
}