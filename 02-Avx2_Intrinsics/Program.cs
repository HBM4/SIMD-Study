namespace _02_Avx2_Intrinsics
{
    using System.Diagnostics;
    using System.Numerics;
    using System.Runtime.Intrinsics;
    using System.Runtime.Intrinsics.X86;

    internal class Program
    {
        // 끝의 3개 원소가 Scalar 처리로 남도록 지정
        private const int ArrayLength = 1_000_003;

        // 세 SIMD 계층의 차이를 순서대로 실행
        static void Main(string[] args)
        {
            PrintCpuInfo();
            RunFloatComparison();
            RunIntComparison();
        }

        // 현재 CPU가 지원하는 SIMD 기능과 처리 폭을 출력합니다.
        static void PrintCpuInfo()
        {
            Console.WriteLine("=== SIMD 실행 환경 ===");
            Console.WriteLine($"Vector<T> 하드웨어 가속: {Vector.IsHardwareAccelerated}");
            Console.WriteLine($"Vector<float> 처리 원소 수: {Vector<float>.Count}");
            Console.WriteLine($"AVX 지원: {Avx.IsSupported}");
            Console.WriteLine($"AVX2 지원: {Avx2.IsSupported}");
            Console.WriteLine($"Vector256<float> 처리 원소 수: {Vector256<float>.Count}");
            Console.WriteLine();
        }

        // float 덧셈으로 Vector<T>와 AVX 방식을 비교합니다.
        static void RunFloatComparison()
        {
            float[] left = new float[ArrayLength];
            float[] right = new float[ArrayLength];
            float[] scalarResult = new float[ArrayLength];
            float[] vectorResult = new float[ArrayLength];

            CreateFloatInputData(left, right);

            double scalarMilliseconds = MeasureSingleMilliseconds(() => AddScalarFloat(left, right, scalarResult));
            double vectorMilliseconds = MeasureSingleMilliseconds(() => AddVectorFloat(left, right, vectorResult));

            Console.WriteLine("=== 1. Vector<T>와 AVX: float 덧셈 ===");
            Console.WriteLine("Vector<T>는 JIT가 CPU에 맞는 SIMD 명령을 선택합니다.");
            Console.WriteLine($"Scalar와 Vector<T> 결과 동일: {AreEqualFloat(scalarResult, vectorResult)}");
            Console.WriteLine($"Scalar: {scalarMilliseconds:F3} ms");
            Console.WriteLine($"Vector<T>: {vectorMilliseconds:F3} ms");

            if (!Avx.IsSupported)
            {
                Console.WriteLine("AVX를 지원하지 않아 AVX 비교는 건너뜁니다.");
                Console.WriteLine();
                return;
            }

            float[] avxResult = new float[ArrayLength];
            double avxMilliseconds = MeasureSingleMilliseconds(() => AddAvxFloat(left, right, avxResult));

            Console.WriteLine("AVX는 개발자가 256비트 float 연산을 직접 지정합니다.");
            Console.WriteLine($"Scalar와 AVX 결과 동일: {AreEqualFloat(scalarResult, avxResult)}");
            Console.WriteLine($"AVX: {avxMilliseconds:F3} ms");
            Console.WriteLine($"AVX 속도 향상: {scalarMilliseconds / avxMilliseconds:F2}배");
            Console.WriteLine();
        }

        // int 덧셈으로 AVX2 정수 연산을 확인합니다.
        static void RunIntComparison()
        {
            Console.WriteLine("=== 2. AVX2: int 덧셈 ===");
            Console.WriteLine("AVX2는 256비트 정수 연산을 추가한 명령 집합입니다.");

            // 지원하지 않는 CPU에서는 AVX2 명령을 실행하지 않습니다.
            if (!Avx2.IsSupported)
            {
                Console.WriteLine("이 CPU는 AVX2를 지원하지 않습니다.");
                return;
            }

            int[] left = new int[ArrayLength];
            int[] right = new int[ArrayLength];
            int[] scalarResult = new int[ArrayLength];
            int[] avx2Result = new int[ArrayLength];

            CreateIntInputData(left, right);

            double scalarMilliseconds = MeasureSingleMilliseconds(() => AddScalarInt(left, right, scalarResult));
            double avx2Milliseconds = MeasureSingleMilliseconds(() => AddAvx2Int(left, right, avx2Result));

            Console.WriteLine($"Vector256<int> 처리 원소 수: {Vector256<int>.Count}");
            Console.WriteLine($"Scalar와 AVX2 결과 동일: {AreEqualInt(scalarResult, avx2Result)}");
            Console.WriteLine($"Scalar: {scalarMilliseconds:F3} ms");
            Console.WriteLine($"AVX2: {avx2Milliseconds:F3} ms");
            Console.WriteLine($"AVX2 속도 향상: {scalarMilliseconds / avx2Milliseconds:F2}배");
        }

        // float 배열에 재현 가능한 테스트 데이터를 채웁니다.
        static void CreateFloatInputData(float[] left, float[] right)
        {
            for (int i = 0; i < left.Length; i++)
            {
                left[i] = (i % 100) * 0.1f;
                right[i] = (i % 50) * 0.2f;
            }
        }

        // int 배열에 재현 가능한 테스트 데이터를 채웁니다.
        static void CreateIntInputData(int[] left, int[] right)
        {
            for (int i = 0; i < left.Length; i++)
            {
                left[i] = i % 1_000;
                right[i] = (i % 500) * 2;
            }
        }

        // float 원소를 하나씩 더하는 기준 구현입니다.
        static void AddScalarFloat(float[] left, float[] right, float[] result)
        {
            for (int i = 0; i < left.Length; i++)
            {
                result[i] = left[i] + right[i];
            }
        }

        // JIT가 선택한 Vector<float> 단위로 여러 원소를 더합니다.
        static void AddVectorFloat(float[] left, float[] right, float[] result)
        {
            int vectorSize = Vector<float>.Count;
            int vectorEnd = left.Length - (left.Length % vectorSize);
            int i = 0;

            for (; i < vectorEnd; i += vectorSize)
            {
                Vector<float> leftVector = new(left, i);
                Vector<float> rightVector = new(right, i);
                (leftVector + rightVector).CopyTo(result, i);
            }

            // SIMD 묶음에 남은 float 원소를 처리합니다.
            for (; i < left.Length; i++)
            {
                result[i] = left[i] + right[i];
            }
        }

        // AVX 256비트 레지스터로 float 8개씩 더합니다.
        static unsafe void AddAvxFloat(float[] left, float[] right, float[] result)
        {
            int vectorSize = Vector256<float>.Count;
            int vectorEnd = left.Length - (left.Length % vectorSize);
            int i = 0;

            // 배열을 고정해 포인터로 안전하게 접근합니다.
            fixed (float* leftPointer = left)
            fixed (float* rightPointer = right)
            fixed (float* resultPointer = result)
            {
                for (; i < vectorEnd; i += vectorSize)
                {
                    Vector256<float> leftVector = Avx.LoadVector256(leftPointer + i);
                    Vector256<float> rightVector = Avx.LoadVector256(rightPointer + i);
                    Vector256<float> addedVector = Avx.Add(leftVector, rightVector);

                    Avx.Store(resultPointer + i, addedVector);
                }
            }

            // 8개 묶음에 남은 float 원소를 처리합니다.
            for (; i < left.Length; i++)
            {
                result[i] = left[i] + right[i];
            }
        }

        // int 원소를 하나씩 더하는 기준 구현입니다.
        static void AddScalarInt(int[] left, int[] right, int[] result)
        {
            for (int i = 0; i < left.Length; i++)
            {
                result[i] = left[i] + right[i];
            }
        }

        // AVX2 256비트 레지스터로 int 8개씩 더합니다.
        static unsafe void AddAvx2Int(int[] left, int[] right, int[] result)
        {
            int vectorSize = Vector256<int>.Count;
            int vectorEnd = left.Length - (left.Length % vectorSize);
            int i = 0;

            // int 포인터로 256비트 정수 벡터를 읽고 저장합니다.
            fixed (int* leftPointer = left)
            fixed (int* rightPointer = right)
            fixed (int* resultPointer = result)
            {
                for (; i < vectorEnd; i += vectorSize)
                {
                    Vector256<int> leftVector = Avx2.LoadVector256(leftPointer + i);
                    Vector256<int> rightVector = Avx2.LoadVector256(rightPointer + i);
                    Vector256<int> addedVector = Avx2.Add(leftVector, rightVector);

                    Avx2.Store(resultPointer + i, addedVector);
                }
            }

            // 8개 묶음에 남은 int 원소를 처리합니다.
            for (; i < left.Length; i++)
            {
                result[i] = left[i] + right[i];
            }
        }

        // 두 float 결과 배열의 모든 원소가 같은지 확인합니다.
        static bool AreEqualFloat(float[] left, float[] right)
        {
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        // 두 int 결과 배열의 모든 원소가 같은지 확인합니다.
        static bool AreEqualInt(int[] left, int[] right)
        {
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        // 작업을 한 번 실행한 시간을 밀리초로 반환합니다.
        static double MeasureSingleMilliseconds(Action operation)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            operation();
            stopwatch.Stop();

            return stopwatch.Elapsed.TotalMilliseconds;
        }
    }
}
