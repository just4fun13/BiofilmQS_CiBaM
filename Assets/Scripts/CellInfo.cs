using UnityEngine;
using System.Collections.Generic;


namespace CellularAutomaton
{
    public class CellInfo
    {
        public int divisionCount;
        public int tryCount;
        public int age;
        public Vector2Int myPos;
        public List<Vector2Int> freeNeighAround = new List<Vector2Int>();
        public List<Vector2Int> highNbrs = new List<Vector2Int>();
        public List<Vector2Int> midNbrs = new List<Vector2Int>();
        public List<Vector2Int> lowNbrs = new List<Vector2Int>();
        public bool HasNbr(Vector2Int p)
        {
            foreach (Vector2Int n in highNbrs)
                if (n == p) return true;
            foreach (Vector2Int n in midNbrs)
                if (n == p) return true;
            foreach (Vector2Int n in lowNbrs)
                if (n == p) return true;
            return false;
        }

        public bool HasntNbr => GetNbrsCount == 0;

        public int GetNbrsCount => highNbrs.Count + midNbrs.Count + lowNbrs.Count;

        public GameObject myObj;
        public SpriteRenderer mySpr;
        public CellInfo(Vector2Int newPos)
        {
            tryCount = 0;
            age = 0;
            divisionCount = 0;
            myPos = newPos;
        }
        public CellInfo()
        {
            tryCount = 0;
            age = 0;
            divisionCount = 0;
        }

        public CellInfo(int divC)
        {
            tryCount = 0;
            age = 0;
            divisionCount = divC;
        }
    }
}
