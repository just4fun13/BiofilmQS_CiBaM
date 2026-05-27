using CellularAutomaton;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

namespace Assets.Scripts.MVVM_CA
{
    public class Model3D : Model
    {

        public double[,,] S3D { get; protected set; }
        public double[,,] C3D { get; protected set; }
        public double[,,] U3D { get; protected set; }
        public double[,,] E3D { get; protected set; }
        public double[,,] A3D { get; protected set; }

        protected Vector3 MinBounds => new Vector3(MinX, MinY, MinZ);
        protected Vector3 MaxBounds => new Vector3(MaxX, MaxY, MaxZ);
        protected Dictionary<ModelType, Vector3[]> GridBaseDic;
        protected Dictionary<ModelType, Vector3Int[]> GridNbrsDic;
        protected int seed = Environment.TickCount;
        protected List<Vector3Int> NewBiomassCells = new List<Vector3Int>();
        protected List<Vector2Int> bottomLayer = new List<Vector2Int>();
        protected List<Vector3Int> newFrontCells3D = new List<Vector3Int>();
        protected object locker = new object();
        protected object bottomRemoveLocker = new object();
        protected object lockerRemoveFront = new object();
        protected Vector3Int[] TruncOctNbrs =
        {
            new Vector3Int( 0, 2, 0),
            new Vector3Int( 0, -2, 0),
            new Vector3Int( 0, 1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(-1, 1,-1),
            new Vector3Int( 0, 1,-1),

            new Vector3Int( 0,-1, 0),
            new Vector3Int( -1,-1, 0),
            new Vector3Int( 0,-1, -1),
            new Vector3Int( -1,-1, -1),

            new Vector3Int( 1, 0, 0),
            new Vector3Int( 0, 0, 1),
            new Vector3Int(-1, 0, 0),
            new Vector3Int( 0, 0,-1),
        };
        protected Vector3Int[] TruncOctNeg =
        {
            new Vector3Int( 0, 2, 0),
            new Vector3Int( 0, -2, 0),
            new Vector3Int( 0, 1, 0),
            new Vector3Int( 1, 1, 0),
            new Vector3Int( 0, 1, 1),
            new Vector3Int( 1, 1, 1),

            new Vector3Int( 1,-1, 1),
            new Vector3Int( 0,-1, 1),
            new Vector3Int( 1,-1, 0),

            new Vector3Int( 0,-1, 0),
            new Vector3Int( 1, 0, 0),
            new Vector3Int( 0, 0, 1),
            new Vector3Int(-1, 0, 0),
            new Vector3Int( 0, 0,-1),
        };
        protected Vector3Int[] CubeNbrs =
        {
            new Vector3Int(  1,  0,  0),
            new Vector3Int(  0,  1,  0),
            new Vector3Int(  0,  0,  1),
            new Vector3Int( -1,  0,  0),
            new Vector3Int(  0, -1,  0),
            new Vector3Int(  0,  0, -1),
        };
        protected Vector3Int[] CubeExtendNbrs =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),

            new Vector3Int(1, 1, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int(1, 0, 1),
            new Vector3Int(-1, 0, -1),
            new Vector3Int(0, 1, 1),
            new Vector3Int(0, -1, -1),

            new Vector3Int(-1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, 0, 1),
            new Vector3Int(1, 0, -1),
            new Vector3Int(0, -1, 1),
            new Vector3Int(0, 1, -1),
        };
        protected Vector3[] CubeBase =
        {
            new Vector3( 1f, 0f, 0f),
            new Vector3( 0f, 1f, 0f),
            new Vector3( 0f, 0f, 1f),
        };
        protected Vector3[] TruncOctBase =
        {
            new Vector3(1f, 0f,  0f),
            new Vector3(0f, 0.5f, 0f),
            new Vector3(0f, 0, 1f),
        };
        protected Vector3[] CubeExtendBase =
        {
            new Vector3( 1f, 0f, 0f),
            new Vector3( 0f, 1f, 0f),
            new Vector3( 0f, 0f, 1f),
        };
        public CellState[,,] Cells3D { get; protected set; }
        public List<Vector3> frontPoints3D { get; protected set; }
        public List<Vector3Int> frontCells3D { get; protected set; }
        public List<Vector3Int> BiomassCells3D { get; protected set; }

        private void SetInitSubstrate()
        {
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                    for (int k = 0; k < AreaLength; k++)
                        S3D[i, j, k] = InitSubstrateCount;
        }
        private Vector3Int[] GetNbrs(int y)
        {
            if (gridType != ModelType.TruncOct || y % 2 == 0)
                return GridNbrsDic[gridType];
            return TruncOctNeg;

        }
        private void DiffuseSubstrate()
        {
            double[] sums = new double[AreaWidth];
            object sumLock = new object();
            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreadCount
            };

            Parallel.For(0, AreaWidth, parallelOptions, i =>
            {
                double localSum = 0;
                for (int j = 0; j < AreaHeight; j++)
                    for (int k = 0; k < AreaLength; k++)
                    {
                        localSum += S3D[i, j, k];

                    }
                lock (sumLock)
                {
                    sums[i] += localSum;
                }
            });
            double totalSum = 0;

            for (int i = 0; i < AreaWidth; i++)
            {
                totalSum += sums[i];
            }
            AverageNutrientRemain = totalSum / (AreaWidth * AreaHeight * AreaLength);
            //            Debug.Log($"SubSub = {subSum}");
        }
        private double AverageSubstrateAround(Vector3Int pos)
        {
            double av = 0;
            int k = 0;
            Vector3Int[] nbrs = GetNbrs(pos.y);
            foreach (Vector3Int nbr in nbrs)
            {
                Vector3Int v = pos + nbr;
                if (IsLegal(v))
                {
                    k++;
                    av += S3D[v.x, v.y, v.z];
                }
            }
            av = av * 1d / k;
            return av;
        }
        private double AverageEPSAround(Vector3Int pos)
        {
            double av = 0;
            int k = 0;
            Vector3Int[] nbrs = GetNbrs(pos.y);
            foreach (Vector3Int nbr in nbrs)
            {
                Vector3Int v = pos + nbr;
                if (IsLegal(v))
                {
                    k++;
                    av += E3D[v.x, v.y, v.z];
                }
            }
            av = av * 1d / k;
            return av;
        }

        private void TryBounds(Vector3 v)
        {
            if (v.x < MinX) MinX = v.x;
            if (v.x > MaxX) MaxX = v.x;
            if (v.y < MinY) MinY = v.y;
            if (v.y > MaxY) MaxY = v.y;
            if (v.z < MinZ) MinZ = v.z;
            if (v.z > MaxZ) MaxZ = v.z;
        }
        private void InoculateInitialBacterialLayer()
        {
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaLength; j++)
                    bottomLayer.Add(new Vector2Int(i, j));

            if (InoculationCount == 1)
            {
                NewCellBlank(new Vector3Int(AreaWidth / 2, 0, AreaLength / 2));
                bottomLayer.Remove(new Vector2Int(AreaWidth / 2, AreaHeight / 2));
            }
            else
            {
                float R = AreaWidth / 4;
                Vector2Int o = new Vector2Int(AreaWidth / 2, AreaLength / 2);
                float deltaPhi = 2 * Mathf.PI / InoculationCount;
                for (int i = 0; i < InoculationCount; i++)
                {
                    float phi = i * deltaPhi;
                    int x = (int) (R * Mathf.Cos(phi)) + o.x;
                    int y = (int) (R * Mathf.Sin(phi)) + o.y;
                    Debug.Log($"i={i}, R = {R}, dPhi={deltaPhi}, phi={phi}, x = {x}, y = {y}");
                    Vector2Int p = new Vector2Int(x, y);
                    NewCellBlank(new Vector3Int(x, 0, y));
                    bottomLayer.Remove(p);
                }
            }
            Debug.Log($"Inited inoculation with {InoculationCount} count");

            BiomassCells3D.AddRange(NewBiomassCells);
            NewBiomassCells.Clear();
        }
        public override void InitInoculate()
        {
            InoculateInitialBacterialLayer();
        }


        private bool IsLegal(Vector3Int v) => v.x >= 0 && v.y >= 0 && v.z >= 0 && v.x < AreaWidth && v.y < AreaHeight && v.z < AreaLength;
        public override Vector3 GetPos(Vector3Int coord)
        {
            if (gridType != ModelType.TruncOct)
            {
                Vector3 v = coord.x * GridBaseDic[gridType][0] + coord.y * GridBaseDic[gridType][1] + coord.z * GridBaseDic[gridType][2];
                return v;
            }
            Vector3 v2 = coord.x * TruncOctBase[0] + coord.y * TruncOctBase[1] + coord.z * TruncOctBase[2];
            if (coord.y % 2 != 0)
                v2 += new Vector3(0.5f, 0, 0.5f);
            return v2;
        }
        private void NewCellBlank(Vector3Int cellIndex)
        {

            C3D[cellIndex.x, cellIndex.y, cellIndex.z] = 0.5;
            Vector3 cellPos = GetPos(cellIndex);
            TryBounds(cellPos);
            NewBiomassCells.Add(cellIndex);
            CellCount++;
            Cells3D[cellIndex.x, cellIndex.y, cellIndex.z] = CellState.busyCanDiv;
            Vector3Int[] nbrs = GetNbrs(cellIndex.y);
            foreach (Vector3Int nbr in nbrs)
            {
                Vector3Int nbrPos = nbr + cellIndex;
                if (IsLegal(nbrPos) && Cells3D[nbrPos.x, nbrPos.y, nbrPos.z] == CellState.empty)
                    newFrontCells3D.Add(nbr + cellIndex);
            }
        }
        private void NewCell(Vector3Int cellIndex, double Bac)
        {

            C3D[cellIndex.x, cellIndex.y, cellIndex.z] = Bac;
            Vector3 cellPos = GetPos(cellIndex);
            TryBounds(cellPos);
            NewBiomassCells.Add(cellIndex);
            CellCount++;
            Cells3D[cellIndex.x, cellIndex.y, cellIndex.z] = CellState.busyCanDiv;
            lock (lockerRemoveFront)
            {
                if (frontCells3D.Contains(cellIndex))
                    frontCells3D.Remove(cellIndex);
                if (cellIndex.y == 0 && bottomLayer.Contains(new Vector2Int(cellIndex.x, cellIndex.z)))
                    bottomLayer.Remove(new Vector2Int(cellIndex.x, cellIndex.z));
            }
            Vector3Int[] nbrs = GetNbrs(cellIndex.y);
            foreach (Vector3Int nbr in nbrs)
            {
                Vector3Int nbrPos = nbr + cellIndex;
                if (IsLegal(nbrPos) && Cells3D[nbrPos.x, nbrPos.y, nbrPos.z] == CellState.empty)
                    newFrontCells3D.Add(nbr + cellIndex);
            }
        }
        private void ConsumeSubstrate(Vector3Int pos)
        {
            int i = pos.x; int j = pos.y; int k = pos.z;
            double v = GetConsume(C3D[i, j, k], S3D[i, j, k]);
            averageConsume += v;
            S3D[i, j, k] -= v;
        }
        private void TryDivide(Vector3Int pos)
        {
            List<Vector3Int> freeDirsAround = FreeDirsAround(pos);
            bool SmartDecision = true;// (U2D[pos.x, pos.y] >= AHLthreshold);
            bool HasFreeSpaceAround = (freeDirsAround.Count > 0);
            bool HasSpreadSpace = (bottomLayer.Count > 0);
            // try Spread
            if (SmartDecision && HasSpreadSpace)
            {
                if (rng.Value.NextDouble() < SpreadProbMax)
                {
                    lock (bottomRemoveLocker)
                    {
                        RangedSpreadDivide(pos);
                        return;
                    }
                }
            }
            if (HasFreeSpaceAround)
                Divide(pos, freeDirsAround[rng.Value.Next(0, freeDirsAround.Count)]);
            // else
            //     PushDivde(pos);
        }
        private void PushDivde(Vector3Int pos)
        {
            Vector3Int randomCellOnFront;
            randomCellOnFront = frontCells3D[rng.Value.Next(0, frontCells3D.Count)];
            Divide(pos, randomCellOnFront);
        }
        private void RangedSpreadDivide(Vector3Int pos)
        {
            List<Vector3Int> nodesInRange = new List<Vector3Int>();
            int range = 2 * pos.y + 2;
            int minX = Mathf.Max(pos.x - range, 0);
            int maxX = Mathf.Min(pos.x + range, AreaWidth);
            int minZ = Mathf.Max(pos.z - range, 0);
            int maxZ = Mathf.Min(pos.z + range, AreaLength);

            for (int x = minX; x < maxX; x++)
                for (int z = minZ; z < maxZ; z++)
                {
                    if (Cells3D[x, 0, z] == CellState.empty)
                    {
                        if ((x - pos.x) * (x - pos.x) + (z - pos.z) * (z - pos.z) <= range * range)
                        {
                            nodesInRange.Add(new Vector3Int(x, 0, z));
                        }
                    }
                }

            /*
            Vector2Int vx = new Vector2Int(pos.x, pos.z);
            foreach (Vector2Int xVal in bottomLayer)
            {
                if ((xVal - vx).sqrMagnitude <= range * range)
                    nodesInRange.Add(new Vector3Int(xVal.x, 0, xVal.y));
            }
            bottomLayer.Remove(new Vector2Int(dirToDiv.x, dirToDiv.z));
            */
            if (nodesInRange.Count == 0)
                return;

            Vector3Int dirToDiv = nodesInRange[rng.Value.Next(0, nodesInRange.Count)];
            Divide(pos, dirToDiv);
        }
        private void Divide(Vector3Int fromPos, Vector3Int toPos)
        {
            C3D[fromPos.x, fromPos.y, fromPos.z] /= 2f;
            NewCell(toPos, C3D[fromPos.x, fromPos.y, fromPos.z]);
        }
        public override void DoGrowthStep()
        {
            //        var watch = new Stopwatch();
            foreach (Vector3Int frontCell in newFrontCells3D)
            {
                if (frontCell.y == 0 && bottomLayer.Contains(new Vector2Int(frontCell.x, frontCell.z)))
                    bottomLayer.Remove(new Vector2Int(frontCell.x, frontCell.z));
            }
            NewBiomassCells.Clear();
            newFrontCells3D.Clear();

            //Parallel.ForEach(BiomassCells, (cellPos) =>
            averageConsume = 0f;

            // Create a ParallelOptions object with a MaxDegreeOfParallelism of 4
            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreadCount
            };
     //       for (int i = 0; i < difCOunt; i++)
                DiffuseSubstrate();

            Parallel.ForEach(BiomassCells3D, parallelOptions, cellPos =>
            {
                if (Cells3D[cellPos.x, cellPos.y, cellPos.z] == CellState.busyCanDiv)
                    ConsumeSubstrate(cellPos);
                //C3D[cellPos.x, cellPos.y, cellPos.z] -= LifetimeCost;
                if (C3D[cellPos.x, cellPos.y, cellPos.z] > ConcToDivide )
                    TryDivide(cellPos);
            }
            );
            averageConsume /= BiomassCells3D.Count;
            BiomassCells3D.AddRange(NewBiomassCells);
            frontCells3D.AddRange(newFrontCells3D);
        }
        private bool HasFreeSpaceAround(Vector3Int coord)
        {
            Vector3Int[] nbrs = GetNbrs(coord.y);
            foreach (Vector3Int v in nbrs)
            {
                Vector3Int sum = coord + v;
                if (IsLegal(sum) && Cells3D[sum.x, sum.y, sum.z] == CellState.empty)
                    return true;
            }
            return false;
        }
        public override Vector2 GetFractalDimension()
        {
            frontCells3D.Clear();
            frontPoints3D.Clear();
            foreach (Vector3Int v in BiomassCells3D)
                if (HasFreeSpaceAround(v))
                {
                    frontCells3D.Add(v);
                    AddPointToFront(GetPos(v));
                }
            frontPoints3D = frontPoints3D.Distinct().ToList();
            Vector2 ans = Vector2.zero;
            ans.x = BoxCountingMachine3D.GetFractalDimension(MinBounds, new Vector3Int(AreaWidth, AreaHeight, AreaLength), frontPoints3D);
            return ans;
        }
        private void AddPointToFront(Vector3 Pos)
        {
            if (gridType == ModelType.TruncOct)
            {
                TryAddFrontPoint(Pos + new Vector3(0, 0.5f, 0));
                TryAddFrontPoint(Pos + new Vector3(0, -0.5f, 0));
                TryAddFrontPoint(Pos + new Vector3(0, 0, 0.5f));
                TryAddFrontPoint(Pos + new Vector3(0, 0f, -0.5f));
                TryAddFrontPoint(Pos + new Vector3(0.5f, 0, 0));
                TryAddFrontPoint(Pos + new Vector3(-0.5f, 0, 0));
                TryAddFrontPoint(Pos + 0.25f * new Vector3(1, 1, 1));
                TryAddFrontPoint(Pos + 0.25f * new Vector3(1, 1, -1));
                TryAddFrontPoint(Pos + 0.25f * new Vector3(1, -1, 1));
                TryAddFrontPoint(Pos + 0.25f * new Vector3(1, -1, -1));
                TryAddFrontPoint(Pos + 0.25f * new Vector3(-1, 1, 1));
                TryAddFrontPoint(Pos + 0.25f * new Vector3(-1, 1, -1));
                TryAddFrontPoint(Pos + 0.25f * new Vector3(-1, -1, 1));
                TryAddFrontPoint(Pos + 0.25f * new Vector3(-1, -1, -1));
            }
            else
            {
                TryAddFrontPoint(Pos + 0.5f * new Vector3(1, 1, 1));
                TryAddFrontPoint(Pos + 0.5f * new Vector3(1, 1, -1));
                TryAddFrontPoint(Pos + 0.5f * new Vector3(1, -1, 1));
                TryAddFrontPoint(Pos + 0.5f * new Vector3(1, -1, -1));
                TryAddFrontPoint(Pos + 0.5f * new Vector3(-1, 1, 1));
                TryAddFrontPoint(Pos + 0.5f * new Vector3(-1, 1, -1));
                TryAddFrontPoint(Pos + 0.5f * new Vector3(-1, -1, 1));
                TryAddFrontPoint(Pos + 0.5f * new Vector3(-1, -1, -1));
            }
        }
        private void TryAddFrontPoint(Vector3 point)
        {
            //if (!frontPoints3D.Contains(point))
                frontPoints3D.Add(point);
        }
        private List<Vector3Int> FreeDirsAround(Vector3Int coord)
        {
            List<Vector3Int> dirs = new List<Vector3Int>();
            Vector3Int[] nbrs = GetNbrs(coord.y);
            foreach (Vector3Int v in nbrs)
            {
                Vector3Int sum = coord + v;
                if (IsLegal(sum) && Cells3D[sum.x, sum.y, sum.z] == CellState.empty)
                    dirs.Add(sum);
            }
            return dirs;
        }
        public override int BottomLayerCountRemain()
        {
            return bottomLayer.Count; 
        }
        public override int BiomassCount => BiomassCells3D.Count;
    }
}
