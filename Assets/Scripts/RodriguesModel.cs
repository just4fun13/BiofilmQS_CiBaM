
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.Rendering;

namespace CellularAutomaton
{

    public class RodCell
    {
        public GameObject prefab;
        public float[] force = new float[8];
        public float conc = 0f;
        public bool eps = false;
        public Vector2Int pos;
        public List<Vector2Int> FreeCellsAround = new List<Vector2Int>();
    }

    public class RodriguesModel : MonoBehaviour
    {
        
        [SerializeField] private GameObject cellPrefab;

        [SerializeField] private float Fparameter = 0.5f;

        [SerializeField] private float ReParameter = 0.8f;

        [SerializeField] private float K = 0.2f;

        [SerializeField] private float alphaParameter = 0.5f;

        [SerializeField] private float substrateInitValue = 0.5f;

        [SerializeField] private float substrateDiffusionK = 0.5f;

        [SerializeField] private float F = 0.01f;


        const int AreaSize = 200;

        private bool[,] celllStates = new bool[AreaSize, AreaSize];
        private float[,] Cs = new float[AreaSize, AreaSize];
        private float[,] newCs = new float[AreaSize, AreaSize];

        private Dictionary<Vector2Int, RodCell> CellInfos = new Dictionary<Vector2Int, RodCell>();
        private Dictionary<Vector2Int, float> E;
        private List<Vector2Int> BiofilmFront = new List<Vector2Int>();

        private Vector2Int[] nbrs =
        {
            new Vector2Int(1,0),
            new Vector2Int(0,1),
            new Vector2Int(-1,0),
            new Vector2Int(0,-1),
            new Vector2Int(1,1),
            new Vector2Int(1,-1),
            new Vector2Int(-1,1),
            new Vector2Int(-1,-1),
        };

        private void Awake()
        {
            E = new Dictionary<Vector2Int, float>()
            {
                {nbrs[0], 0.2f },
                {nbrs[1], 0.1f },
                {nbrs[2], 0.1f },
                {nbrs[3], 0.2f },
                {nbrs[4], 0.1f },
                {nbrs[5], 0.1f },
                {nbrs[6], 0.1f },
                {nbrs[7], 0.1f },
            };
            SetSubstrateConcentration();
            InitBiomassArea();
        }

        private void InitBiomassArea()
        {
            float off = 0.2f;
            int xStart = (int)(AreaSize * off);
            int xEnd = AreaSize - xStart;
            for (int i = xStart; i < xEnd; i++)
            {
                GenerateCell(new Vector2Int(i, 0));
            }
            
            foreach (var pair in CellInfos)
            {
                pair.Value.FreeCellsAround = CalcNbrsCount(pair.Key);
                if (pair.Value.FreeCellsAround.Count > 0)
                    BiofilmFront.Add(pair.Key);
            }
            StartCoroutine(GrowthProcess());
        }

        private List<Vector2Int> CalcNbrsCount(Vector2Int pos)
        {
            List<Vector2Int> nbrs = new List<Vector2Int>();
            foreach (Vector2Int v in nbrs)
            {
                Vector2Int sum = v + pos;
                if (InRange(sum) && !celllStates[sum.x, sum.y])
                {
                    Vector2 myPos = GetPos(pos);
                    Vector2 newPos = GetPos(sum);
                    nbrs.Add(sum);
                }
            }
            return nbrs;
        }

        // INCORRECT FOR BIG FRACTAL SCTRUCTURES
        private int GetMinDistanceToFront(Vector2Int pos)
        {
            int ans = AreaSize;
            foreach (Vector2Int frontPos in BiofilmFront)
            {
                int dy = Mathf.Abs(frontPos.y - pos.y);
                int dx = Mathf.Abs(frontPos.x - pos.x);
                if (dy == 0 && dx < ans)
                {
                    ans = dx;
                    continue;
                }
                if (dx == 0 && dy < ans)
                {
                    ans = dy;
                    continue;
                }
                float k = dy / dx;
                if (k > 0.98f && k < 1.02f && dy < ans)
                    ans = dy;
            }
            return ans;
        }

        private Vector3 GetPos(Vector2Int pos)
        {
            return new Vector3(pos.x, pos.y, 0f);
        }

        private void GenerateCell(Vector2Int pos)
        {
            RodCell newCell = new RodCell();
            GameObject newObj = Instantiate(cellPrefab, GetPos(pos), Quaternion.identity, transform); 
            newCell.prefab = newObj;
            newCell.pos = pos;
            CellInfos.Add(pos, newCell);
        }

        private void SetSubstrateConcentration()
        {
            for (int i = 0; i < AreaSize; i++) 
                for (int j = 0; j < AreaSize; j++)
                {
                    Cs[i, j] = substrateInitValue;
                }
        }

        IEnumerator GrowthProcess()
        {
            int wannaDiv = 1;
            while (wannaDiv > 0)
            {
                foreach (var inf in CellInfos)
                {
                    Vector2Int pos = inf.Key;
                    float sumD = 0;
                    foreach (Vector2Int nbr in nbrs)
                        sumD += 1f/(Mathf.Pow(GetMinDistanceToFront(pos + nbr)*1f/AreaSize, 2));
                    inf.Value.conc = Mathf.Pow( Mathf.Sqrt(Cs[pos.x, pos.y]) - Mathf.Sqrt(F * 8f / sumD), 2);
                    Debug.Log($"For cell {pos} -> Cs = {Cs[pos.x, pos.y]} - C = {inf.Value.conc}, Perr = {Perrsion(inf.Key)}, Pdiv = {Pdiv(inf.Value.conc)}");
                }
                yield return new WaitForEndOfFrame();
            }
        }


        private void ProcessNewBuddy(RodCell buddy)
        {
            // Debug.Log($"proc: now cell {buddy.myPos} - {buddy.myPos.x * GridBaseDic[gridType][0] + buddy.myPos.y * GridBaseDic[gridType][1]} is busy");
            Vector2Int pos = buddy.pos;
            foreach (Vector2Int v in nbrs)
            {
                Vector2Int sum = v + pos;
                if (!InRange(sum)) continue;

                Vector2 myPos = GetPos(buddy.pos); ;
                Vector2 newPos = GetPos(sum);
                if (!celllStates[sum.x, sum.y])
                {
                    buddy.FreeCellsAround.Add(sum);
                }
                else
                {
                    if (CellInfos.ContainsKey(sum))
                    {
                        CellInfos[sum].FreeCellsAround.Remove(pos);
                        if (CellInfos[sum].FreeCellsAround.Count == 0)
                            BiofilmFront.Remove(CellInfos[sum].pos);
                    }
                }
                if (buddy.FreeCellsAround.Count > 0)
                    BiofilmFront.Add(buddy.pos);
            }
        }

        private int Hi(Vector2Int pos)
        {
            if (celllStates[pos.x, pos.y])
                return 1;
            else 
                return 0;
        }

        private float Tao(Vector2Int pos)
        {
            float s = 0f;
            int k = 0;
            Vector2Int left = Vector2Int.left + pos;
            if (InRange(left) && celllStates[left.x, left.y])
                return 0;
            foreach (Vector2Int v in nbrs)
            {
                if (v == Vector2Int.left)
                    continue;
                Vector2Int pn = pos + v;
                if (InRange(pn) && celllStates[pn.x, pn.y])
                {
                    s += E[pn] * Hi(pn);
                    k++;
                }
            }

            return ReParameter * (1 - s);   ;
        }

        private float Sigma(Vector2Int pos)
        {
            if (!celllStates[pos.x, pos.y]) return 0f;
            if (CellInfos[pos].eps) return 1f;
            else return alphaParameter;
        }

        private void DiffuseNutrient()
        {
            float[,] newCs = new float[AreaSize, AreaSize];
            for (int i = 0; i < AreaSize; i++)
                for (int j=0; j < AreaSize; j++)    
                {
                    newCs[i, j] = Cs[i, j] + substrateDiffusionK*NbrSubstrateBalancedSum(i,j);
                }


        }

        private float Peps(Vector2Int pos)
        {
            return ReParameter * (1 - CellInfos[pos].conc / (CellInfos[pos].conc + K));
        }

        private float Perrsion(Vector2Int pos)
        {
            if (Tao(pos) == 0) return 0f;
            return 1f/(1 + Sigma(pos)/Tao(pos));
        }

        private float Pdiv(float C)
        {
            return C / (C + K);
        }

        private float NbrSubstrateBalancedSum(int col, int row)
        {
            float sum = 0f;
            int k = 0;
            if (InRange(col + 1, row))
            {
                sum += Cs[col + 1, row];
                k++;
            }
            if (InRange(col, row + 1))
            {
                sum += Cs[col, row + 1];
                k++;
            }
            if (InRange(col - 1, row))
            {
                sum += Cs[col - 1, row];
                k++;
            }
            if (InRange(col, row - 1))
            {
                sum += Cs[col, row - 1];
                k++;
            }
            if (InRange(col - 1, row - 1))
            {
                sum += Cs[col - 1, row - 1];
                k++;
            }
            if (InRange(col + 1, row + 1))
            {
                sum += Cs[col + 1, row + 1];
                k++;
            }
            if (InRange(col + 1, row - 1))
            {
                sum += Cs[col + 1, row - 1];
                k++;
            }
            if (InRange(col - 1, row + 1))
            {
                sum += Cs[col - 1, row + 1];
                k++;
            }
            return (sum-k*Cs[col,row])*1f/k;
        }

        private bool InRange(Vector2Int pos) => pos.x >=0 && pos.y >= 0 && pos.x < AreaSize && pos.y < AreaSize;

        private bool InRange(int row, int col) => col >= 0 && row >= 0 && col < AreaSize && row < AreaSize;
    }
}
