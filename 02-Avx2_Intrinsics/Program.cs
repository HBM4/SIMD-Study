namespace _02_Avx2_Intrinsics
{
    using System.Diagnostics;
    using System.Runtime.Intrinsics;
    using System.Runtime.Intrinsics.X86;

    internal class Program
    {
        // 끝의 3개 원소가 Scalar 처리로 남도록 지정했습니다.
        private const int ArrayLength = 10_000_003;

        // AVX 지원 여부를 확인하고 Scalar와 AVX 시간을 비교합니다.
        static void Main(string[] args)
        {
            Console.WriteLine("=== AVX Intrinsics 실행 환경 ===");
            Console.WriteLine($"AVX 지원: {Avx.IsSupported}");
            Console.WriteLine($"AVX2 지원: {Avx2.IsSupported}");
            Console.WriteLine($"Vector256<float> 한 묶음의 원소 수: {Vector256<float>.Count}");
            Console.WriteLine();

            // 지원하지 않는 CPU에서 AVX 명령을 실행하면 안 됩니다.
            if (!Avx.IsSupported)
            {
                Console.WriteLine("이 CPU에서는 AVX 예제를 실행할 수 없습니다.");
                return;
            }

            float[] left = new float[ArrayLength];
            float[] right = new float[ArrayLength];
            float[] scalarResult = new float[ArrayLength];
            float[] avxResult = new float[ArrayLength];

            CreateInputData(left, right);

            // Scalar 덧셈 한 번에 걸린 시간을 측정합니다.
            Stopwatch scalarStopwatch = Stopwatch.StartNew();
            AddScalar(left, right, scalarResult);
            scalarStopwatch.Stop();

            // AVX 덧셈 한 번에 걸린 시간을 측정합니다.
            Stopwatch avxStopwatch = Stopwatch.StartNew();
            AddAvx(left, right, avxResult);
            avxStopwatch.Stop();

            double scalarMilliseconds = scalarStopwatch.Elapsed.TotalMilliseconds;
            double avxMilliseconds = avxStopwatch.Elapsed.TotalMilliseconds;
            double speedup = scalarMilliseconds / avxMilliseconds;

            Console.WriteLine("=== 결과 검증 ===");
            Console.WriteLine($"Scalar와 AVX 결과 동일: {AreEqual(scalarResult, avxResult)}");
            Console.WriteLine($"첫 번째 결과: {avxResult[0]}");
            Console.WriteLine($"마지막 결과: {avxResult[^1]}");
            Console.WriteLine();

            Console.WriteLine("=== 단일 실행 시간 ===");
            Console.WriteLine($"Scalar: {scalarMilliseconds:F3} ms");
            Console.WriteLine($"AVX: {avxMilliseconds:F3} ms");
            Console.WriteLine($"속도 향상: {speedup:F2}배");
            Console.WriteLine();
            Console.WriteLine("참고: float 덧셈은 AVX 명령어를 사용합니다.");
        }

        // 두 입력 배열에 재현 가능한 테스트 데이터를 채웁니다.
        static void CreateInputData(float[] left, float[] right)
        {
            for (int i = 0; i < left.Length; i++)
            {
                left[i] = (i % 100) * 0.1f;
                right[i] = (i % 50) * 0.2f;
            }
        }

        // float 원소를 하나씩 더하는 기준 구현입니다.
        static void AddScalar(float[] left, float[] right, float[] result)
        {
            for (int i = 0; i < left.Length; i++)
            {
                result[i] = left[i] + right[i];
            }
        }

        // AVX 256비트 레지스터로 float 8개씩 더합니다.
        static unsafe void AddAvx(float[] left, float[] right, float[] result)
        {
            int vectorSize = Vector256<float>.Count;
            int vectorEnd = left.Length - (left.Length % vectorSize);
            int i = 0;

            // 배열을 고정해 GC가 측정 중 위치를 옮기지 못하게 합니다.
            fixed (float* leftPointer = left)
            fixed (float* rightPointer = right)
            fixed (float* resultPointer = result)
            {
                for (; i < vectorEnd; i += vectorSize)
                {
                    // 메모리에서 float 8개를 256비트 레지스터로 읽습니다.
                    Vector256<float> leftVector = Avx.LoadVector256(leftPointer + i);
                    Vector256<float> rightVector = Avx.LoadVector256(rightPointer + i);

                    // 레지스터 안의 같은 위치 원소끼리 동시에 더합니다.
                    Vector256<float> addedVector = Avx.Add(leftVector, rightVector);

                    // 계산한 8개 결과를 배열에 한 번에 저장합니다.
                    Avx.Store(resultPointer + i, addedVector);
                }
            }

            // 8개 묶음에 남은 원소는 Scalar 방식으로 처리합니다.
            for (; i < left.Length; i++)
            {
                result[i] = left[i] + right[i];
            }
        }

        // 두 결과 배열의 모든 원소가 같은지 확인합니다.
        static bool AreEqual(float[] left, float[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
