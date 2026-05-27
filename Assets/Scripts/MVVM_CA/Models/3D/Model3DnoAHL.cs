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
    public class Model3DnoAHL : Model3D
    {
        public Model3DnoAHL(int W, int H, int L, float initSub, ModelType gridT, double TimeStep, int maxThread)
        {
            gridType = gridT;
            AreaWidth = W;
            AreaHeight = H;
            AreaLength = L;
            maxThreadCount = maxThread;
    

            BiomassCells3D = new List<Vector3Int>();
            NewBiomassCells.Clear();
            BiomassCells3D.Clear();
            newFrontCells3D.Clear();
            bottomLayer.Clear();

            DeltaTime = TimeStep;
            InitSubstrateCount = initSub;
            rng = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));
                //new ThreadLocal<System.Random>(() => new System.Random());
            Cells3D = new CellState[AreaWidth, AreaHeight, AreaLength];


            randomDIrectionDIvideProbability = 1;// 0.5;// rng.Value.NextDouble();


            S3D = new double[AreaWidth, AreaHeight, AreaLength];
            C3D = new double[AreaWidth, AreaHeight, AreaLength];
            frontPoints3D = new List<Vector3>();
            frontCells3D = new List<Vector3Int>();  
            GridBaseDic = new Dictionary<ModelType, Vector3[]>
            {
                {ModelType.SimpleCube,   CubeBase  },
                {ModelType.TruncOct,     TruncOctBase },
                {ModelType.ExtendedCube, CubeExtendBase }
            };
            GridNbrsDic = new Dictionary<ModelType, Vector3Int[]>
            {   
                {ModelType.SimpleCube,   CubeNbrs  },
                {ModelType.TruncOct, TruncOctNbrs },
                {ModelType.ExtendedCube, CubeExtendNbrs }
            };
            SetInitSubstrate();
            Debug.Log($"New 3D model inited with size {W},{H},{L}; G = {H*H*μmax/DiffusionKoef/InoculationCount}");

        }
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
                        S3D[i, j, k] = S3D[i, j, k] + DiffusionKoef * (AverageSubstrateAround(new Vector3Int(i, j, k)) - S3D[i, j, k]) * DeltaTime;
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
                    int x = (int) (R * Mathf.Cos(phi)) + o.x ;
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
        private void InoculateRandom()
        {
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaLength; j++)
                    bottomLayer.Add(new Vector2Int(i, j));

            int k = (int) Math.Ceiling(Math.Sqrt(InoculationCount));
            int h = AreaWidth / k;

            List<Vector2Int> points = new List<Vector2Int>();
            for (int i = 0; i < k; i++)
                for (int j = 0; j < k; j++)
                    points.Add(new Vector2Int(h/2 + i * h, h / 2 + j * h));


            foreach (Vector2Int p in points)
            {
                if (!bottomLayer.Contains(p))
                    continue;
                NewCellBlank(new Vector3Int(p.x, 0, p.y));
                bottomLayer.Remove(p);

            }
            BiomassCells3D.AddRange(NewBiomassCells);
            NewBiomassCells.Clear();

            Debug.Log($"Inited Random inoculation with {InoculationCount} count");

            /*            for (int i = 0; i < InoculationCount; i++)
                        {

                            int x = rng.Value.Next(AreaWidth);
                            int y = rng.Value.Next(AreaLength);
                            Vector2Int p = new Vector2Int(x, y);
                            if (!bottomLayer.Contains(p))
                                continue;
                            NewCellBlank(new Vector3Int(x, 0, y));
                            bottomLayer.Remove(p);
                        }

                        BiomassCells3D.AddRange(NewBiomassCells);
                        NewBiomassCells.Clear();
            */
        }
        public override void InitInoculate()
        {
            InoculateRandom();
            //InoculateInitialBacterialLayer();
        }
        public override void InitInoculateFromFile(List<Vector2Int> vList)
        {
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaLength; j++)
                    bottomLayer.Add(new Vector2Int(i, j));


            for (int i = 0; i <= InoculationCount; i++)
            {
                Vector3Int vPos = new Vector3Int(vList[i].x, 0, vList[i].y);
                NewCellBlank(vPos);
                bottomLayer.Remove(vList[i]);
            }
            Debug.Log($"Inited inoculation with {InoculationCount} count");

            BiomassCells3D.AddRange(NewBiomassCells);
            NewBiomassCells.Clear();
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

            C3D[cellIndex.x, cellIndex.y, cellIndex.z] = rng.Value.NextDouble();
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
        private void ConsumeSubstrateAndProduceAHL(Vector3Int pos)
        {
            int i = pos.x; int j = pos.y; int k = pos.z;
            double v = GetConsume(C3D[i, j, k], S3D[i, j, k]);
            averageConsume += v;
            C3D[i, j, k] += v;
            S3D[i, j, k] -= v;

        }
        private void TryDivide(Vector3Int pos)
        {
            // Проверка наличия свободного пространства
            List<Vector3Int> freeDirsAround = FreeDirsAround(pos);
            if (freeDirsAround.Count == 0) return;

            // Плавная вероятность деления

            // Стохастическая проверка деления
            if (C3D[pos.x, pos.y, pos.z] > ConcToDivide)
            {

                // Выбор направления деления
                Vector3Int dirToDivide = freeDirsAround[rng.Value.Next(freeDirsAround.Count)];
                Divide(pos, dirToDivide);
            }
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
                    ConsumeSubstrateAndProduceAHL(cellPos);
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
            List<Vector3> vxlist = new List<Vector3>();
            foreach (Vector3Int v in BiomassCells3D)
            {
                vxlist.Add(v);
                if (HasFreeSpaceAround(v))
                {
                    frontCells3D.Add(v);
                    AddPointToFront(GetPos(v));
                }
            }
            frontPoints3D = frontPoints3D.Distinct().ToList();
            Vector2 ans = Vector2.zero;

//            ans.x = BoxCountingMachine3D.GetFractalDimension(MinBounds, new Vector3Int(AreaWidth, AreaHeight, AreaLength), frontPoints3D);
            ans.x = BoxCountingMachine3D.GetFractalDimension(MinBounds, new Vector3Int(AreaWidth, AreaHeight, AreaLength), vxlist);
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
        public override Vector4 ModelStats3D()
        {
            int[,] busyCount = new int[AreaWidth, AreaLength];
            int[,] maxH = new int[AreaWidth, AreaLength];
            int totalBusyCount = 0;
            double totalSum = 0;


            for (int i = 0; i < AreaWidth; i++)
                for (int k = 0; k < AreaLength; k++)
                    for (int j = 0; j < AreaHeight; j++)
                    {
                        if (C3D[i, j, k] == 0)
                            continue;

                        if (j + 1 > maxH[i, k])
                            maxH[i, k] = j + 1;

                        busyCount[i, k]++;
                        totalBusyCount++;
                        totalSum += C3D[i, j, k];
                    }

            float avHeighOfall = 0;
            float avHeightWithPores = 0;
            int busyCountLayers = 0;

            for (int i = 0; i < AreaWidth; i++)
                for (int k = 0; k < AreaLength; k++)
                {
                    if (maxH[i, k] == 0)
                        continue;
                    busyCountLayers++;
                    avHeighOfall += maxH[i, k];
                    avHeightWithPores += busyCount[i, k];
                }
            avHeighOfall = avHeighOfall * 1f / busyCountLayers;
            avHeightWithPores = avHeightWithPores * 1f / busyCountLayers;

            float avConc = (float) totalSum * 1f / totalBusyCount;
            float avCoun = totalBusyCount * 1f / (AreaWidth * AreaLength);


            return new Vector4(avHeighOfall, avHeightWithPores, avConc, avCoun);
        }
    }
}
