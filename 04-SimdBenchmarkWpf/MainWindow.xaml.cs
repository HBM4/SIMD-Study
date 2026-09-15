using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CppCliAvxBridge;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace _04_SimdBenchmarkWpf
{
    public partial class MainWindow : Window
    {
        // 끝의 3개 원소가 Scalar 처리로 남도록 지정했습니다.
        private const int DefaultArrayLength = 10_000_003;
        private const int MaximumArrayLength = 10_000_003;

        public MainWindow()
        {
            InitializeComponent();
            ArrayLengthTextBox.Text = DefaultArrayLength.ToString(CultureInfo.InvariantCulture);
            UpdateRuntimeInfo();
        }

        // 입력한 배열 길이로 네 가지 덧셈 방식을 한 번씩 실행합니다.
        private void RunBenchmarkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetArrayLength(out int arrayLength))
            {
                MessageBox.Show(
                    $"배열 길이는 1부터 {MaximumArrayLength:N0} 사이의 정수여야 합니다.",
                    "입력 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            RunBenchmarkButton.IsEnabled = false;

            try
            {
                float[] left = new float[arrayLength];
                float[] right = new float[arrayLength];
                float[] scalarResult = new float[arrayLength];
                float[] vectorResult = new float[arrayLength];

                CreateInputData(left, right);

                // 기준 구현인 Scalar 결과를 먼저 만듭니다.
                double scalarMilliseconds = MeasureSingleMilliseconds(() => AddScalar(left, right, scalarResult));

                // Vector<T>는 JIT가 현재 CPU에 맞는 SIMD 명령을 선택합니다.
                double vectorMilliseconds = MeasureSingleMilliseconds(() => AddVector(left, right, vectorResult));

                var rows = new List<BenchmarkRow>
                {
                    new("C# Scalar", "float 원소 1개씩 처리", scalarMilliseconds, "기준값"),
                    new("C# Vector<T>", $"Vector<float> {System.Numerics.Vector<float>.Count}개씩 처리", vectorMilliseconds,
                        AreEqual(scalarResult, vectorResult) ? "동일" : "다름")
                };

                bool allResultsMatch = AreEqual(scalarResult, vectorResult);

                if (Avx.IsSupported)
                {
                    float[] avxResult = new float[arrayLength];
                    double avxMilliseconds = MeasureSingleMilliseconds(() => AddAvx(left, right, avxResult));
                    bool avxMatches = AreEqual(scalarResult, avxResult);

                    rows.Add(new BenchmarkRow(
                        "C# AVX",
                        "Vector256<float> 8개씩 처리",
                        avxMilliseconds,
                        avxMatches ? "동일" : "다름"));
                    allResultsMatch &= avxMatches;
                }
                else { rows.Add(new BenchmarkRow("C# AVX", "이 CPU에서 AVX 미지원", null, "실행 안 함")); }

                if (SimdAvxBridge.IsAvxSupported())
                {
                    float[] cppCliResult = new float[arrayLength];
                    double cppCliMilliseconds = MeasureSingleMilliseconds(() => SimdAvxBridge.AddTo(left, right, cppCliResult));
                    bool cppCliMatches = AreEqual(scalarResult, cppCliResult);

                    rows.Add(new BenchmarkRow(
                        "C++/CLI AVX",
                        "pin_ptr → 네이티브 AVX 8개 처리",
                        cppCliMilliseconds,
                        cppCliMatches ? "동일" : "다름"));
                    allResultsMatch &= cppCliMatches;
                }
                else { rows.Add(new BenchmarkRow("C++/CLI AVX", "이 CPU에서 AVX 미지원", null, "실행 안 함")); }

                ResultsDataGrid.ItemsSource = rows;
                ExecutionStatusTextBlock.Text = allResultsMatch
                    ? $"{arrayLength:N0}개 원소의 실행 결과가 Scalar 기준과 모두 일치합니다."
                    : "일부 구현의 결과가 Scalar 기준과 다릅니다. 코드와 CPU 지원 여부를 확인하세요.";
            }
            catch (Exception exception)
            {
                ExecutionStatusTextBlock.Text = "실행 중 오류가 발생했습니다.";
                MessageBox.Show(exception.Message, "실행 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RunBenchmarkButton.IsEnabled = true;
            }
        }

        // 현재 CPU와 C++/CLI 브리지의 SIMD 지원 상태를 표시합니다.
        private void UpdateRuntimeInfo()
        {
            VectorInfoTextBlock.Text = $"Vector<T> 하드웨어 가속: {System.Numerics.Vector.IsHardwareAccelerated} / float {System.Numerics.Vector<float>.Count}개";
            CSharpAvxInfoTextBlock.Text = $"C# AVX 지원: {Avx.IsSupported}";
            CppCliAvxInfoTextBlock.Text = $"C++/CLI 네이티브 AVX 지원: {SimdAvxBridge.IsAvxSupported()}";
            ExecutionStatusTextBlock.Text = "배열 길이를 입력하고 ‘한 번 실행’을 눌러 비교하세요.";
        }

        // 입력한 문자열이 실험 가능한 배열 길이인지 확인합니다.
        private bool TryGetArrayLength(out int arrayLength)
        {
            string text = ArrayLengthTextBox.Text.Replace("_", string.Empty);
            bool isValidNumber = int.TryParse(
                text,
                NumberStyles.Integer | NumberStyles.AllowThousands,
                CultureInfo.CurrentCulture,
                out arrayLength);

            return isValidNumber && arrayLength > 0 && arrayLength <= MaximumArrayLength;
        }

        // 두 입력 배열에 재현 가능한 float 데이터를 채웁니다.
        private static void CreateInputData(float[] left, float[] right)
        {
            for (int i = 0; i < left.Length; i++)
            {
                left[i] = (i % 100) * 0.1f;
                right[i] = (i % 50) * 0.2f;
            }
        }

        // float 원소를 하나씩 더하는 기준 구현입니다.
        private static void AddScalar(float[] left, float[] right, float[] result)
        {
            for (int i = 0; i < left.Length; i++)
            {
                result[i] = left[i] + right[i];
            }
        }

        // JIT가 선택한 Vector<float> 단위로 여러 원소를 더합니다.
        private static void AddVector(float[] left, float[] right, float[] result)
        {
            int vectorSize = System.Numerics.Vector<float>.Count;
            int vectorEnd = left.Length - (left.Length % vectorSize);
            int i = 0;

            for (; i < vectorEnd; i += vectorSize)
            {
                System.Numerics.Vector<float> leftVector = new(left, i);
                System.Numerics.Vector<float> rightVector = new(right, i);
                (leftVector + rightVector).CopyTo(result, i);
            }

            // SIMD 묶음에 남은 원소를 Scalar 방식으로 처리합니다.
            for (; i < left.Length; i++)
            {
                result[i] = left[i] + right[i];
            }
        }

        // AVX 256비트 레지스터로 float 8개씩 더합니다.
        private static unsafe void AddAvx(float[] left, float[] right, float[] result)
        {
            int vectorSize = Vector256<float>.Count;
            int vectorEnd = left.Length - (left.Length % vectorSize);
            int i = 0;

            // GC가 배열을 움직이지 않도록 포인터를 고정합니다.
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

            // 8개 묶음에 남은 원소를 Scalar 방식으로 처리합니다.
            for (; i < left.Length; i++)
            {
                result[i] = left[i] + right[i];
            }
        }

        // 두 결과 배열의 모든 원소가 같은지 확인합니다.
        private static bool AreEqual(float[] left, float[] right)
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
        private static double MeasureSingleMilliseconds(Action operation)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            operation();
            stopwatch.Stop();

            return stopwatch.Elapsed.TotalMilliseconds;
        }

        // 표에 표시할 구현별 실행 결과입니다.
        private sealed class BenchmarkRow
        {
            public BenchmarkRow(string method, string detail, double? elapsedMilliseconds, string result)
            {
                Method = method;
                Detail = detail;
                Elapsed = elapsedMilliseconds is null ? "-" : $"{elapsedMilliseconds.Value:F3} ms";
                Result = result;
            }

            public string Method { get; }
            public string Detail { get; }
            public string Elapsed { get; }
            public string Result { get; }
        }
    }
}
