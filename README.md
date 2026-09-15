# SIMD Study

SIMD를 처음 익힐 때, 같은 일을 처리 방식에 따라 어떻게 나누는지 살펴보기 위한 프로젝트입니다. 같은 배열 덧셈을 Scalar, `Vector<T>`, AVX/AVX2, C++/CLI 네이티브 AVX로 구현해 비교합니다.

속도 수치만 보는 대신 여러 원소를 한 번에 처리하는 흐름, SIMD 묶음에서 남는 원소의 처리, C# 배열을 C++ 코드에 넘기는 방법을 따라가 보는 데 초점을 두었습니다. 각 예제는 Scalar 결과와 비교해 계산이 맞는지도 함께 확인합니다.

<img width="957" height="678" alt="image" src="https://github.com/user-attachments/assets/c03a0dbc-4301-4377-9fca-a0186a1c8bdf" />

## 구성

- [`01-Scalar_VS_SIMD`](01-Scalar_VS_SIMD) — 일반 반복문과 `Vector<T>`를 비교하는 가장 기본적인 예제입니다.
- [`02-Avx2_Intrinsics`](02-Avx2_Intrinsics) — `float` 배열은 AVX로, `int` 배열은 AVX2로 직접 처리합니다.
- [`03-CppCliAvxBridge`](03-CppCliAvxBridge) — C# 배열을 C++/CLI에서 받아 네이티브 AVX 코드로 넘기는 브리지입니다.
- [`04-SimdBenchmarkWpf`](04-SimdBenchmarkWpf) — 앞의 방식을 한 화면에서 비교하는 WPF 예제입니다. **(시작 프로젝트로 설정)**

CPU와 런타임에 따라 SIMD 처리 폭과 지원되는 명령은 달라질 수 있습니다. 그래서 코드에서는 실행 환경을 확인하고, 지원하지 않는 AVX/AVX2 명령은 건너뜁니다.
