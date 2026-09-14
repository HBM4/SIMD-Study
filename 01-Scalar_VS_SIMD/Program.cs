namespace _01_Scalar_VS_SIMD
{
    using System.Diagnostics;
    using System.Numerics;

    internal class Program
    {
        // 끝의 3개 원소가 SIMD 묶음에 남도록 지정했습니다.
        private const int ArrayLength = 10_000_003;

        // 프로그램을 실행하고 Scalar와 SIMD 성능을 비교합니다.
        static void Main(string[] args)
        {
            // 덧셈에 사용할 입력 배열입니다.
            float[] left = new float[ArrayLength];
            float[] right = new float[ArrayLength];

            // 두 구현이 각각 기록할 결과 배열입니다.
            float[] scalarResult = new float[ArrayLength];
            float[] vectorResult = new float[ArrayLength];

            // 매번 같은 입력값으로 공정하게 비교합니다.
            CreateInputData(left, right);

            // Scalar 연산 한 번에 걸린 시간을 측정합니다.
            Stopwatch scalarStopwatch = Stopwatch.StartNew();
            AddScalar(left, right, scalarResult);
            scalarStopwatch.Stop();

            // Vector 연산 한 번에 걸린 시간을 측정합니다.
            Stopwatch vectorStopwatch = Stopwatch.StartNew();
            AddVector(left, right, vectorResult);
            vectorStopwatch.Stop();

            double scalarMilliseconds = scalarStopwatch.Elapsed.TotalMilliseconds;
            double vectorMilliseconds = vectorStopwatch.Elapsed.TotalMilliseconds;

            // 1보다 크면 Vector 방식이 더 빠른 것입니다.
            double speedup = scalarMilliseconds / vectorMilliseconds;

            Console.WriteLine("=== SIMD 실행 환경 ===");
            Console.WriteLine($"하드웨어 SIMD 가속 사용: {Vector.IsHardwareAccelerated}");
            Console.WriteLine($"Vector<float> 한 묶음의 원소 수: {Vector<float>.Count}");
            Console.WriteLine($"배열 길이: {ArrayLength:N0}");
            Console.WriteLine();

            Console.WriteLine("=== 결과 검증 ===");
            // 시간을 측정한 뒤 두 구현의 정답이 같은지 확인합니다.
            Console.WriteLine($"첫 번째 결과: {vectorResult[0]}");
            Console.WriteLine($"마지막 결과: {vectorResult[^1]}");
            Console.WriteLine();

            Console.WriteLine("=== 단일 실행 시간 ===");
            Console.WriteLine($"Scalar: {scalarMilliseconds:F3} ms");
            Console.WriteLine($"Vector: {vectorMilliseconds:F3} ms");
            Console.WriteLine($"속도 향상: {speedup:F2}배");
        }

        // 두 입력 배열에 재현 가능한 테스트 데이터를 채웁니다.
        static void CreateInputData(float[] left, float[] right)
        {
            for (int i = 0; i < left.Length; i++)
            {
                // 나머지 연산으로 일정한 값 패턴을 만듭니다.
                left[i] = (i % 100) * 0.1f;
                right[i] = (i % 50) * 0.2f;
            }
        }

        // float 원소를 하나씩 더하는 기준 구현입니다.
        static void AddScalar(float[] left, float[] right, float[] result)
        {
            // 현재는 반복마다 float 원소 하나씩만 더합니다.
            for (int i = 0; i < left.Length; i++) { result[i] = left[i] + right[i]; }
        }

        // Vector<float> 단위로 여러 원소를 함께 더합니다.
        static void AddVector(float[] left, float[] right, float[] result)
        {
            // 현재 PC가 한 묶음으로 처리할 float 원소 수입니다.
            int vectorSize = Vector<float>.Count;

            // SIMD 묶음으로 처리 가능한 마지막 위치입니다.
            int vectorEnd = left.Length - (left.Length % vectorSize);
            int i = 0;

            // Vector<float>는 여러 float를 하나의 묶음으로 연산합니다.
            for (; i < vectorEnd; i += vectorSize)
            {
                // 배열에서 현재 SIMD 묶음을 읽습니다.
                Vector<float> leftVector = new(left, i);
                Vector<float> rightVector = new(right, i);
                Vector<float> addedVector = leftVector + rightVector;

                // 묶음 연산 결과를 결과 배열에 기록합니다.
                addedVector.CopyTo(result, i);
            }

            // 묶음에 남은 원소는 기존 Scalar 방식으로 처리합니다.
            for (; i < left.Length; i++) { result[i] = left[i] + right[i]; }
        }
    }
}
