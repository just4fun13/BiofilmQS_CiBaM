using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class DiffusionSolver
{
    private int Nx;
    private int Ny;

    private double dt;
    private double dx;
    private double dy;
    private double D;
    private double ConsumptionRate;

    private bool reversed = true;

    private ParallelOptions parallelOptions;

    public double t { get; private set; }

    // current = текущее поле, temp1/temp2 = рабочие буферы
    private double[,] current;
    private double[,] temp1;
    private double[,] temp2;

    // true: нечётные строки j сдвинуты вправо относительно чётных
    // false: нечётные строки сдвинуты влево
    private bool oddRowsShiftRight = false;

    // внешний код читает solver.u как текущий результат
    public double[,] u => current;

    private int hexBEMaxIterations = 200;
    private double hexBETolerance = 1e-8;

    // 1.0 = обычный Gauss-Seidel.
    // 1.2..1.8 = SOR, быстрее, но может хуже сходиться при слишком большом dt.
    private double hexBEOmega = 1.0;
    private double hexGamma = 2.0 / 3.0;
    private int hexReferenceParity = 0;
    private bool hexOddRowsShiftRight = false;
    private int scale = 4;

    // Если массив хранит j сверху вниз, а Unity-сетка снизу вверх,
    // поставь true.
    private bool hexInvertRowParity = false;

    public void SetScale(int newScale)
    {
        int dScale = newScale - scale;
    }

    public void SetTimeStep(float val)
    {
    }


    public void SetHexLayout(bool oddRowsShiftRight, bool invertRowParity = false)
    {
        hexOddRowsShiftRight = oddRowsShiftRight;
        hexInvertRowParity = invertRowParity;
    }
    public void SetHexBackwardEulerOptions( int maxIterations = 200,  double tolerance = 1e-8, double omega = 1.0)
    {
        if (maxIterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxIterations));

        if (tolerance <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(tolerance));

        if (omega <= 0.0 || omega >= 2.0)
            throw new ArgumentOutOfRangeException(nameof(omega));

        hexBEMaxIterations = maxIterations;
        hexBETolerance = tolerance;
        hexBEOmega = omega;
    }
    // Рабочие буферы для вертикальной проекции.
    private double[,] hexYProjected;
    private double[,] hexYSolved;

    private struct CellRef
    {
        public int I;
        public int J;

        public CellRef(int i, int j)
        {
            I = i;
            J = j;
        }
    }
    public enum DiffusionMode
    {
        ADI,
        Implicit,
        HexADI,
        HexBackwardEuler
    }
    private enum HexAxis
    {
        Horizontal,
        DiagKMinusJ,
        DiagKPlusJ
    }

    private DiffusionMode mode = DiffusionMode.ADI;

    public void SetMode(DiffusionMode newMode)
    {
        mode = newMode;
    }

    public void Init( double[,] u0, double hStep, double deltaT, double diffusionKoef, int maxTR = 1, double consRate = 0.0)
    {
        Nx = u0.GetLength(0);
        Ny = u0.GetLength(1);

        dt = deltaT;
        D = diffusionKoef;
        dx = hStep;
        dy = hStep;
        ConsumptionRate = consRate;

        parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxTR
        };

        current = new double[Nx, Ny];
        temp1 = new double[Nx, Ny];
        temp2 = new double[Nx, Ny];

        CopyField(u0, current);

        t = 0.0;
        reversed = false;
        hexYProjected = new double[Nx, Ny];
        hexYSolved = new double[Nx, Ny];
        SetHexLayout( oddRowsShiftRight: true, invertRowParity: false);
    }

    public void RefreshAndDoStep(double[,] source)
    {
        CopyField(source, current);
        DoStep();
    }

    public void RefreshAndDoStep(double[,] source, double newD)
    {
        D = newD;
        CopyField(source, current);
        DoStep();
    }

    public void DoStep()
    {
        switch (mode)
        {
            case DiffusionMode.Implicit:
                DoImplicitStep();
                break;
            case DiffusionMode.HexADI:
                DoHexADI3Step();
                break;
            case DiffusionMode.HexBackwardEuler:
                DoHexBackwardEulerStep();
                break;
            case DiffusionMode.ADI:
            default:
                DoADIStep();
                break;
        }

        t += dt;
    }

    private void DoHexBackwardEulerStep()
    {
        double h = dx;

        if (h <= 0.0)
            throw new InvalidOperationException("Grid step must be positive.");

        double r = D * dt * hexGamma / (h * h);

        CopyField(current, temp1);
        CopyField(current, temp2);

        CellRef[] neighbors = new CellRef[6];

        for (int iter = 0; iter < hexBEMaxIterations; iter++)
        {
            double maxDelta = 0.0;

            for (int j = 0; j < Ny; j++)
            {
                for (int i = 0; i < Nx; i++)
                {
                    GetHexNeighbors(i, j, neighbors, out int count);

                    double sum = 0.0;

                    for (int k = 0; k < count; k++)
                    {
                        CellRef nb = neighbors[k];
                        sum += temp2[nb.I, nb.J];
                    }

                    double oldIterValue = temp2[i, j];

                    double gsValue =
                        (temp1[i, j] + r * sum)
                        / (1.0 + r * count);

                    double newValue =
                        oldIterValue + hexBEOmega * (gsValue - oldIterValue);

                    temp2[i, j] = newValue;

                    double delta = Math.Abs(newValue - oldIterValue);

                    if (delta > maxDelta)
                        maxDelta = delta;
                }
            }

            if (maxDelta < hexBETolerance)
                break;
        }

        ClampNonNegative(temp2);
        Swap(ref current, ref temp2);
        reversed = false;
    }

    private void ClampNonNegative(double[,] field)
    {
        for (int j = 0; j < Ny; j++)
        {
            for (int i = 0; i < Nx; i++)
            {
                if (field[i, j] < 0.0)
                    field[i, j] = 0.0;
            }
        }
    }

    private int HexRowParity(int j)
    {
        int row = hexInvertRowParity ? (Ny - 1 - j) : j;
        return row & 1;
    }

    private int HexRowShift2(int j)
    {
        int row = hexInvertRowParity ? (Ny - 1 - j) : j;

        if ((row & 1) == 0)
            return 0;

        return hexOddRowsShiftRight ? 1 : -1;
    }

    private void AddHexNeighborByDoubledX( int doubledX, int j, CellRef[] neighbors, ref int count)
    {
        if (j < 0 || j >= Ny)
            return;

        int shift = HexRowShift2(j);
        int raw = doubledX - shift;

        // Для настоящей клетки raw должен быть чётным.
        if ((raw & 1) != 0)
            return;

        int i = raw / 2;

        AddHexNeighbor(i, j, neighbors, ref count);
    }

    private void GetHexNeighbors(int i, int j, CellRef[] neighbors, out int count)
    {
        count = 0;

        int k = 2 * i + HexRowShift2(j);

        // Соседи слева/справа в той же строке.
        AddHexNeighborByDoubledX(k - 2, j, neighbors, ref count);
        AddHexNeighborByDoubledX(k + 2, j, neighbors, ref count);

        // Два соседа в строке ниже.
        AddHexNeighborByDoubledX(k - 1, j - 1, neighbors, ref count);
        AddHexNeighborByDoubledX(k + 1, j - 1, neighbors, ref count);

        // Два соседа в строке выше.
        AddHexNeighborByDoubledX(k - 1, j + 1, neighbors, ref count);
        AddHexNeighborByDoubledX(k + 1, j + 1, neighbors, ref count);
    }

    private void AddHexNeighbor(int i, int j, CellRef[] neighbors, ref int count)
    {
        if (i < 0 || i >= Nx || j < 0 || j >= Ny)
            return;

        neighbors[count++] = new CellRef(i, j);
    }

    private void DoADIStep()
    {
        if (reversed)
        {
            FirstStepADIParallel(current, temp1);
            SecondStepADIParallel(temp1, temp2);
        }
        else
        {
            SecondStepADIParallel(temp1, temp2);
            FirstStepADIParallel(current, temp1);
        }

        Swap(ref current, ref temp2);
        reversed = !reversed;
    }
    private bool TryGetHexCellFromDoubledX(int doubledX, int j, out int i)
    {
        i = 0;

        if (j < 0 || j >= Ny)
            return false;

        int shift = HexRowShift2(j);
        int raw = doubledX - shift;

        // Ячейка существует только если raw чётный.
        if ((raw & 1) != 0)
            return false;

        i = raw / 2;

        return i >= 0 && i < Nx;
    }
    private int BuildHexLine( HexAxis axis, int lineId, CellRef[] line)
    {
        int count = 0;

        if (axis == HexAxis.Horizontal)
        {
            int j = lineId;

            if (j < 0 || j >= Ny)
                return 0;

            for (int i = 0; i < Nx; i++)
                line[count++] = new CellRef(i, j);

            return count;
        }

        for (int j = 0; j < Ny; j++)
        {
            int doubledX;

            if (axis == HexAxis.DiagKMinusJ)
            {
                // k - j = lineId => k = lineId + j
                doubledX = lineId + j;
            }
            else
            {
                // k + j = lineId => k = lineId - j
                doubledX = lineId - j;
            }

            if (TryGetHexCellFromDoubledX(doubledX, j, out int i))
                line[count++] = new CellRef(i, j);
        }

        return count;
    }

    private void GetHexAxisLineIdRange( HexAxis axis, out int minId, out int maxId)
    {
        if (axis == HexAxis.Horizontal)
        {
            minId = 0;
            maxId = Ny - 1;
            return;
        }

        minId = int.MaxValue;
        maxId = int.MinValue;

        for (int j = 0; j < Ny; j++)
        {
            int shift = HexRowShift2(j);

            int kLeft = shift;
            int kRight = 2 * (Nx - 1) + shift;

            int idA;
            int idB;

            if (axis == HexAxis.DiagKMinusJ)
            {
                idA = kLeft - j;
                idB = kRight - j;
            }
            else
            {
                idA = kLeft + j;
                idB = kRight + j;
            }

            if (idA < minId) minId = idA;
            if (idB < minId) minId = idB;

            if (idA > maxId) maxId = idA;
            if (idB > maxId) maxId = idB;
        }
    }

    private void SolveImplicitHexLine( double[,] src, double[,] dst, CellRef[] line,  int n, double r,
    double[] A, double[] B, double[] C, double[] Dvec, double[] X, double[] Cp, double[] Dp)
    {
        if (n <= 0)
            return;

        if (n == 1)
        {
            CellRef c0 = line[0];
            dst[c0.I, c0.J] = src[c0.I, c0.J];
            return;
        }

        for (int p = 0; p < n; p++)
        {
            CellRef cell = line[p];

            bool hasPrev = p > 0;
            bool hasNext = p < n - 1;

            A[p] = hasPrev ? -r : 0.0;
            C[p] = hasNext ? -r : 0.0;

            double degree = 0.0;
            if (hasPrev) degree += 1.0;
            if (hasNext) degree += 1.0;

            B[p] = 1.0 + r * degree;
            Dvec[p] = src[cell.I, cell.J];
        }

        ThomasSolve(A, B, C, Dvec, X, Cp, Dp, n);

        for (int p = 0; p < n; p++)
        {
            CellRef cell = line[p];
            dst[cell.I, cell.J] = X[p];
        }
    }

    private void HexAxisSweep( HexAxis axis, double[,] src, double[,] dst, double r)
    {
        // На всякий случай, чтобы одиночные/пропущенные линии не оставили мусор.
        CopyField(src, dst);

        int maxLineLength = Math.Max(Nx, Ny);

        CellRef[] line = new CellRef[maxLineLength];

        double[] A = ArrayPool<double>.Shared.Rent(maxLineLength);
        double[] B = ArrayPool<double>.Shared.Rent(maxLineLength);
        double[] C = ArrayPool<double>.Shared.Rent(maxLineLength);
        double[] Dvec = ArrayPool<double>.Shared.Rent(maxLineLength);
        double[] X = ArrayPool<double>.Shared.Rent(maxLineLength);
        double[] Cp = ArrayPool<double>.Shared.Rent(maxLineLength);
        double[] Dp = ArrayPool<double>.Shared.Rent(maxLineLength);

        try
        {
            GetHexAxisLineIdRange(axis, out int minId, out int maxId);

            for (int lineId = minId; lineId <= maxId; lineId++)
            {
                int n = BuildHexLine(axis, lineId, line);

                if (n <= 0)
                    continue;

                SolveImplicitHexLine(
                    src,
                    dst,
                    line,
                    n,
                    r,
                    A,
                    B,
                    C,
                    Dvec,
                    X,
                    Cp,
                    Dp);
            }
        }
        finally
        {
            ArrayPool<double>.Shared.Return(A);
            ArrayPool<double>.Shared.Return(B);
            ArrayPool<double>.Shared.Return(C);
            ArrayPool<double>.Shared.Return(Dvec);
            ArrayPool<double>.Shared.Return(X);
            ArrayPool<double>.Shared.Return(Cp);
            ArrayPool<double>.Shared.Return(Dp);
        }
    }

    private void DoHexADI3Step()
    {
        double h = dx;

        if (h <= 0.0)
            throw new InvalidOperationException("Grid step must be positive.");

        // ВАЖНО: не делим на 3.
        // Lhex = gamma * (L0 + L1 + L2) / h^2,
        // значит каждый directional sweep получает тот же r.
        double r = D * dt * hexGamma / (h * h);

        if (reversed)
        {
            HexAxisSweep(HexAxis.DiagKPlusJ, current, temp1, r);
            HexAxisSweep(HexAxis.DiagKMinusJ, temp1, temp2, r);
            HexAxisSweep(HexAxis.Horizontal, temp2, temp1, r);
        }
        else
        {
            HexAxisSweep(HexAxis.Horizontal, current, temp1, r);
            HexAxisSweep(HexAxis.DiagKMinusJ, temp1, temp2, r);
            HexAxisSweep(HexAxis.DiagKPlusJ, temp2, temp1, r);
        }

        ClampNonNegative(temp1);

        Swap(ref current, ref temp1);

        reversed = !reversed;
    }
    public void RefreshAndDoImplicitStep(double[,] source)
    {
        CopyField(source, current);
        DoImplicitStep();
        t += dt;
    }

    public void RefreshAndDoImplicitStep(double[,] source, double newD)
    {
        D = newD;
        CopyField(source, current);
        DoImplicitStep();
        t += dt;
    }

    private static void Swap(ref double[,] a, ref double[,] b)
    {
        double[,] tmp = a;
        a = b;
        b = tmp;
    }

    private void CopyField(double[,] src, double[,] dst)
    {
        for (int i = 0; i < Nx; i++)
        {
            for (int j = 0; j < Ny; j++)
            {
                dst[i, j] = src[i, j];
            }
        }
    }

    private double RowShift(int parity)
    {
        if ((parity & 1) == 0)
            return 0.0;

        return oddRowsShiftRight ? 0.5 : -0.5;
    }

    private double SampleRowLinear(double[,] field, double xIndex, int j)
    {
        if (xIndex <= 0.0)
            return field[0, j];

        if (xIndex >= Nx - 1)
            return field[Nx - 1, j];

        int left = (int)Math.Floor(xIndex);
        int right = left + 1;

        double frac = xIndex - left;

        return (1.0 - frac) * field[left, j]
             + frac * field[right, j];
    }

    /// <summary>
    /// Этап 1: неявно по x, явно по y
    /// src -> dst
    /// </summary>
    private void FirstStepADIParallel(double[,] src, double[,] dst)
    {
        double alpha = D * dt / (2.0 * dx * dx);
        double beta = D * dt / (2.0 * dy * dy);

        int nx = Nx;
        int ny = Ny;

        Parallel.For(0, ny, parallelOptions, j =>
        {
            double[] A = ArrayPool<double>.Shared.Rent(nx);
            double[] B = ArrayPool<double>.Shared.Rent(nx);
            double[] C = ArrayPool<double>.Shared.Rent(nx);
            double[] Dvec = ArrayPool<double>.Shared.Rent(nx);
            double[] X = ArrayPool<double>.Shared.Rent(nx);
            double[] Cp = ArrayPool<double>.Shared.Rent(nx);
            double[] Dp = ArrayPool<double>.Shared.Rent(nx);

            try
            {
                for (int i = 0; i < nx; i++)
                {
                    double laplaceY = 0.0;
                    if (j > 0 && j < ny - 1)
                    {
                        laplaceY = beta * (src[i, j + 1] - 2.0 * src[i, j] + src[i, j - 1]);
                    }

                    A[i] = -alpha;
                    B[i] = 1.0 + 2.0 * alpha;
                    C[i] = -alpha;
                    Dvec[i] = src[i, j] + laplaceY;
                }

                int last = nx - 1;
                // Левая граница: Neumann
                A[0] = 0.0;
                B[0] = 1.0 + 2.0 * alpha;
                C[0] = -2.0 * alpha;

                // Правая граница: Neumann
                A[last] = -2.0 * alpha;
                B[last] = 1.0 + 2.0 * alpha;
                C[last] = 0.0;
                //Dvec[0] = src[0, j];
                //Dvec[last] = src[last, j];

                ThomasSolve(A, B, C, Dvec, X, Cp, Dp, nx);

                for (int i = 0; i < nx; i++)
                {
                    dst[i, j] = X[i];
                }
            }
            finally
            {
                ArrayPool<double>.Shared.Return(A);
                ArrayPool<double>.Shared.Return(B);
                ArrayPool<double>.Shared.Return(C);
                ArrayPool<double>.Shared.Return(Dvec);
                ArrayPool<double>.Shared.Return(X);
                ArrayPool<double>.Shared.Return(Cp);
                ArrayPool<double>.Shared.Return(Dp);
            }
        });
    }

    /// <summary>
    /// Этап 2: неявно по y, явно по x
    /// src -> dst
    /// </summary>
    private void SecondStepADIParallel(double[,] src, double[,] dst)
    {
        double alpha = D * dt / (2.0 * dx * dx);
        double beta = D * dt / (2.0 * dy * dy);

        int nx = Nx;
        int ny = Ny;

        Parallel.For(0, nx, parallelOptions, i =>
        {
            double[] A = ArrayPool<double>.Shared.Rent(ny);
            double[] B = ArrayPool<double>.Shared.Rent(ny);
            double[] C = ArrayPool<double>.Shared.Rent(ny);
            double[] Dvec = ArrayPool<double>.Shared.Rent(ny);
            double[] X = ArrayPool<double>.Shared.Rent(ny);
            double[] Cp = ArrayPool<double>.Shared.Rent(ny);
            double[] Dp = ArrayPool<double>.Shared.Rent(ny);

            try
            {
                for (int j = 0; j < ny; j++)
                {
                    double laplaceX = 0.0;
                    if (i > 0 && i < nx - 1)
                    {
                        laplaceX = alpha * (src[i + 1, j] - 2.0 * src[i, j] + src[i - 1, j]);
                    }

                    A[j] = -beta;
                    B[j] = 1.0 + 2.0 * beta;
                    C[j] = -beta;
                    Dvec[j] = src[i, j] + laplaceX;
                }

                // Нижняя граница: Neumann
                A[0] = 0.0;
                B[0] = 1.0 + 2.0 * beta;
                C[0] = -2.0 * beta;
                //Dvec[0] = src[i, 0];

                // Верхняя граница: Neumann
                int last = ny - 1;
                A[last] = -2.0 * beta;
                B[last] = 1.0 + 2.0 * beta;
                C[last] = 0.0;
                //Dvec[last] = src[i, last];

                ThomasSolve(A, B, C, Dvec, X, Cp, Dp, ny);

                for (int j = 0; j < ny; j++)
                {
                    dst[i, j] = X[j];
                }
            }
            finally
            {
                ArrayPool<double>.Shared.Return(A);
                ArrayPool<double>.Shared.Return(B);
                ArrayPool<double>.Shared.Return(C);
                ArrayPool<double>.Shared.Return(Dvec);
                ArrayPool<double>.Shared.Return(X);
                ArrayPool<double>.Shared.Return(Cp);
                ArrayPool<double>.Shared.Return(Dp);
            }
        });
    }

    /// <summary>
    /// Метод Томаса без внутренних аллокаций.
    /// x, cp, dp должны быть заранее выделены.
    /// </summary>
    private static void ThomasSolve( double[] a, double[] b, double[] c, double[] d, double[] x, double[] cp, double[] dp, int n)
    {
        cp[0] = c[0] / b[0];
        dp[0] = d[0] / b[0];

        for (int i = 1; i < n; i++)
        {
            double denom = b[i] - a[i] * cp[i - 1];
            cp[i] = c[i] / denom;
            dp[i] = (d[i] - a[i] * dp[i - 1]) / denom;
        }

        x[n - 1] = dp[n - 1];

        for (int i = n - 2; i >= 0; i--)
        {
            x[i] = dp[i] - cp[i] * x[i + 1];
        }
    }

    public void DoImplicitStep()
    {
        // Backward Euler splitting:
        // 1) implicit X
        // 2) implicit Y
        //
        // Более устойчиво и более сглаживающе, чем ADI Crank-Nicolson.

        ImplicitXParallel(current, temp1);
        ImplicitYParallel(temp1, temp2);

        Swap(ref current, ref temp2);

        reversed = false;
    }

    private void ImplicitXParallel(double[,] src, double[,] dst)
    {
        double alpha = D * dt / (dx * dx);

        int nx = Nx;
        int ny = Ny;

        Parallel.For(0, ny, parallelOptions, j =>
        {
            double[] A = ArrayPool<double>.Shared.Rent(nx);
            double[] B = ArrayPool<double>.Shared.Rent(nx);
            double[] C = ArrayPool<double>.Shared.Rent(nx);
            double[] Dvec = ArrayPool<double>.Shared.Rent(nx);
            double[] X = ArrayPool<double>.Shared.Rent(nx);
            double[] Cp = ArrayPool<double>.Shared.Rent(nx);
            double[] Dp = ArrayPool<double>.Shared.Rent(nx);

            try
            {
                for (int i = 0; i < nx; i++)
                {
                    A[i] = -alpha;
                    B[i] = 1.0 + 2.0 * alpha;
                    C[i] = -alpha;
                    Dvec[i] = src[i, j];
                }

                // Neumann boundary: нулевой поток слева
                A[0] = 0.0;
                B[0] = 1.0 + 2.0 * alpha;
                C[0] = -2.0 * alpha;

                // Neumann boundary: нулевой поток справа
                int last = nx - 1;
                A[last] = -2.0 * alpha;
                B[last] = 1.0 + 2.0 * alpha;
                C[last] = 0.0;

                ThomasSolve(A, B, C, Dvec, X, Cp, Dp, nx);

                for (int i = 0; i < nx; i++)
                    dst[i, j] = X[i] < 0.0 ? 0.0 : X[i];
            }
            finally
            {
                ArrayPool<double>.Shared.Return(A);
                ArrayPool<double>.Shared.Return(B);
                ArrayPool<double>.Shared.Return(C);
                ArrayPool<double>.Shared.Return(Dvec);
                ArrayPool<double>.Shared.Return(X);
                ArrayPool<double>.Shared.Return(Cp);
                ArrayPool<double>.Shared.Return(Dp);
            }
        });
    }

    private void ImplicitYParallel(double[,] src, double[,] dst)
    {
        double beta = D * dt / (dy * dy);

        int nx = Nx;
        int ny = Ny;

        Parallel.For(0, nx, parallelOptions, i =>
        {
            double[] A = ArrayPool<double>.Shared.Rent(ny);
            double[] B = ArrayPool<double>.Shared.Rent(ny);
            double[] C = ArrayPool<double>.Shared.Rent(ny);
            double[] Dvec = ArrayPool<double>.Shared.Rent(ny);
            double[] X = ArrayPool<double>.Shared.Rent(ny);
            double[] Cp = ArrayPool<double>.Shared.Rent(ny);
            double[] Dp = ArrayPool<double>.Shared.Rent(ny);

            try
            {
                for (int j = 0; j < ny; j++)
                {
                    A[j] = -beta;
                    B[j] = 1.0 + 2.0 * beta;
                    C[j] = -beta;
                    Dvec[j] = src[i, j];
                }

                // Neumann boundary: нулевой поток снизу
                A[0] = 0.0;
                B[0] = 1.0 + 2.0 * beta;
                C[0] = -2.0 * beta;

                // Neumann boundary: нулевой поток сверху
                int last = ny - 1;
                A[last] = -2.0 * beta;
                B[last] = 1.0 + 2.0 * beta;
                C[last] = 0.0;

                ThomasSolve(A, B, C, Dvec, X, Cp, Dp, ny);

                for (int j = 0; j < ny; j++)
                    dst[i, j] = X[j] < 0.0 ? 0.0 : X[j];
            }
            finally
            {
                ArrayPool<double>.Shared.Return(A);
                ArrayPool<double>.Shared.Return(B);
                ArrayPool<double>.Shared.Return(C);
                ArrayPool<double>.Shared.Return(Dvec);
                ArrayPool<double>.Shared.Return(X);
                ArrayPool<double>.Shared.Return(Cp);
                ArrayPool<double>.Shared.Return(Dp);
            }
        });
    }
}