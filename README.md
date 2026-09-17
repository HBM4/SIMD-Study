# C# / C++ AVX·AVX2 명령어 참조표

.NET 8.0의 C# `Avx`·`Avx2`와 대응하는 Intel C/C++ intrinsic을 한눈에 찾기 위한 참조표입니다. 자료형별 오버로드는 묶어서 표시합니다.

## C# / C++에서 AVX와 AVX2 선택하기

AVX2는 AVX를 포함하는 확장 명령어 집합입니다. 다만 이름만 다른 것이 아니라, 주로 처리할 자료형과 가능한 연산 범위가 다릅니다.

| 처리하려는 데이터 | C# 선택 | C++ 예시 | 필요한 CPU 기능 |
| --- | --- | --- | --- |
| `float`, `double` 256비트 연산 | `Avx` | `_mm256_add_ps`, `_mm256_add_pd` | AVX |
| `int` 등 256비트 정수 연산 | `Avx2` | `_mm256_add_epi32` | AVX2 |

- C#에서 `Avx2`는 `Avx` 기능도 포함하지만, `float`/`double`만 다룬다면 `Avx`와 `Avx.IsSupported`를 사용하는 편이 최소 CPU 요구 사항을 정확히 보여 줍니다.
- C++은 `Avx`/`Avx2`라는 클래스 접두사가 없습니다. intrinsic마다 요구 기능이 정해집니다. 예를 들어 `_mm256_add_ps`는 AVX 명령이고, `_mm256_add_epi32`는 AVX2 명령입니다.
- 따라서 AVX2 지원 CPU에서 `_mm256_add_ps` 또는 `Avx.Add`를 써도 해당 연산이 AVX2 전용 명령으로 바뀌지는 않습니다.

## 사용 준비

| 항목 | C# | C++ |
| --- | --- | --- |
| 가져오기 | `using System.Runtime.Intrinsics;`<br>`using System.Runtime.Intrinsics.X86;` | `#include <immintrin.h>` |
| 256비트 형식 | `Vector256<T>` | `__m256` (`float`), `__m256d` (`double`), `__m256i` (정수) |
| 지원 확인 | `Avx.IsSupported`<br>`Avx2.IsSupported` | CPUID와 OSXSAVE/XGETBV를 실행 중 확인 |
| MSVC 대상 옵션 | 해당 없음(JIT이 판단) | `/arch:AVX`, `/arch:AVX2` |
| GCC·Clang 대상 옵션 | 해당 없음(JIT이 판단) | `-mavx`, `-mavx2` |

컴파일 옵션은 실행 중 CPU 확인을 대신하지 않습니다. 지원하지 않는 CPU에서 해당 명령을 실행하면 실패하므로, 배포 프로그램은 별도의 일반 코드 경로도 준비해야 합니다.

## 자료형과 이름 읽는 법

| C# 256비트 형식 | C++ 형식·접미사 | 한 번에 담는 원소 수 |
| --- | --- | --- |
| `Vector256<float>` | `__m256`, `_ps` | 8개 |
| `Vector256<double>` | `__m256d`, `_pd` | 4개 |
| `Vector256<sbyte>` / `Vector256<byte>` | `__m256i`, `_epi8` / `_epu8` | 32개 |
| `Vector256<short>` / `Vector256<ushort>` | `__m256i`, `_epi16` / `_epu16` | 16개 |
| `Vector256<int>` / `Vector256<uint>` | `__m256i`, `_epi32` / `_epu32` | 8개 |
| `Vector256<long>` / `Vector256<ulong>` | `__m256i`, `_epi64` / `_epu64` | 4개 |

- 표의 `{ps,pd}`처럼 중괄호 안의 값은 선택 가능한 실제 이름을 줄여 쓴 것입니다. 예를 들어 `_mm256_add_{ps,pd}`는 `_mm256_add_ps`와 `_mm256_add_pd`를 뜻합니다.
- `[256]`은 128비트 `_mm_...`와 256비트 `_mm256_...` 형태가 모두 있음을 뜻합니다.
- `_epi*`는 packed integer, `_epu*`는 unsigned packed integer, `_si256`은 원소 의미를 정하지 않은 256비트 정수 비트열입니다.
- `imm8`, `control`, `mask`가 즉시값인 명령은 일반적으로 컴파일 시간 상수가 필요합니다.

## AVX 명령어 목록

AVX는 256비트 `float`·`double` 연산이 중심입니다. 단순 정수 메모리 이동과 128비트 조각 이동처럼 비트 자체만 다루는 일부 정수 연산도 포함합니다.

### 지원 확인·메모리·데이터 구성

| C# `Avx` | C++ intrinsic | 간단한 설명 |
| --- | --- | --- |
| `IsSupported` | CPUID + OSXSAVE/XGETBV | 현재 프로세스에서 AVX를 안전하게 실행할 수 있는지 확인합니다. |
| `LoadVector256` | `_mm256_loadu_{ps,pd,si256}` | 정렬되지 않아도 되는 주소에서 256비트를 읽습니다. |
| `LoadAlignedVector256` | `_mm256_load_{ps,pd,si256}` | 32바이트 정렬 주소에서 읽습니다. |
| `LoadDquVector256` | `_mm256_lddqu_si256` | 정렬되지 않은 정수 데이터를 읽는 특수 힌트형 로드입니다. |
| `Store` | `_mm256_storeu_{ps,pd,si256}` | 정렬되지 않아도 되는 주소에 저장합니다. |
| `StoreAligned` | `_mm256_store_{ps,pd,si256}` | 32바이트 정렬 주소에 저장합니다. |
| `StoreAlignedNonTemporal` | `_mm256_stream_{ps,pd,si256}` | 캐시 오염을 줄이는 비시간적 방식으로 저장합니다. |
| `MaskLoad` | `_mm_maskload_{ps,pd}`<br>`_mm256_maskload_{ps,pd}` | 마스크가 선택한 원소만 메모리에서 읽습니다. |
| `MaskStore` | `_mm_maskstore_{ps,pd}`<br>`_mm256_maskstore_{ps,pd}` | 마스크가 선택한 원소만 메모리에 씁니다. |
| `BroadcastScalarToVector128` | `_mm_broadcast_ss` | 메모리의 `float` 하나를 128비트 전체에 복제합니다. |
| `BroadcastScalarToVector256` | `_mm256_broadcast_{ss,sd}` | 메모리의 스칼라 하나를 256비트 전체에 복제합니다. |
| `BroadcastVector128ToVector256` | `_mm256_broadcast_{ps,pd}` | 메모리의 128비트 블록을 양쪽 128비트 lane에 복제합니다. |
| `ExtractVector128` | `_mm256_extractf128_{ps,pd,si256}` | 256비트 벡터에서 128비트 절반을 꺼냅니다. |
| `InsertVector128` | `_mm256_insertf128_{ps,pd,si256}` | 256비트 벡터의 한쪽 128비트 절반을 교체합니다. |

### 산술·반올림·변환

| C# `Avx` | C++ intrinsic | 간단한 설명 |
| --- | --- | --- |
| `Add` | `_mm256_add_{ps,pd}` | 같은 위치의 실수를 더합니다. |
| `Subtract` | `_mm256_sub_{ps,pd}` | 같은 위치의 실수를 뺍니다. |
| `Multiply` | `_mm256_mul_{ps,pd}` | 같은 위치의 실수를 곱합니다. |
| `Divide` | `_mm256_div_{ps,pd}` | 같은 위치의 실수를 나눕니다. |
| `AddSubtract` | `_mm256_addsub_{ps,pd}` | 짝수 위치는 빼고 홀수 위치는 더합니다. |
| `Sqrt` | `_mm256_sqrt_{ps,pd}` | 각 원소의 제곱근을 계산합니다. |
| `Reciprocal` | `_mm256_rcp_ps` | `float`의 역수 근삿값을 빠르게 구합니다. |
| `ReciprocalSqrt` | `_mm256_rsqrt_ps` | `float`의 역제곱근 근삿값을 빠르게 구합니다. |
| `Min` | `_mm256_min_{ps,pd}` | 위치별 최솟값을 선택합니다. |
| `Max` | `_mm256_max_{ps,pd}` | 위치별 최댓값을 선택합니다. |
| `DotProduct` | `_mm256_dp_ps` | `float`를 곱하고 선택적으로 합산합니다. 128비트 lane별로 동작합니다. |
| `HorizontalAdd` | `_mm256_hadd_{ps,pd}` | 각 128비트 lane 안에서 이웃 원소를 더합니다. |
| `HorizontalSubtract` | `_mm256_hsub_{ps,pd}` | 각 128비트 lane 안에서 이웃 원소를 뺍니다. |
| `Ceiling` | `_mm256_ceil_{ps,pd}` | 각 원소를 양의 무한대 방향 정수값으로 올림합니다. |
| `Floor` | `_mm256_floor_{ps,pd}` | 각 원소를 음의 무한대 방향 정수값으로 내림합니다. |
| `RoundCurrentDirection` | `_mm256_round_{ps,pd}` + `_MM_FROUND_CUR_DIRECTION` | 현재 부동소수점 반올림 모드를 사용합니다. |
| `RoundToNearestInteger` | `_mm256_round_{ps,pd}` + `_MM_FROUND_TO_NEAREST_INT` | 가장 가까운 정수값으로 반올림합니다. |
| `RoundToNegativeInfinity` | `_mm256_round_{ps,pd}` + `_MM_FROUND_TO_NEG_INF` | 음의 무한대 방향으로 반올림합니다. |
| `RoundToPositiveInfinity` | `_mm256_round_{ps,pd}` + `_MM_FROUND_TO_POS_INF` | 양의 무한대 방향으로 반올림합니다. |
| `RoundToZero` | `_mm256_round_{ps,pd}` + `_MM_FROUND_TO_ZERO` | 0 방향으로 소수 부분을 버립니다. |
| `ConvertToVector128Int32` | `_mm256_cvtpd_epi32` | `double` 4개를 반올림하여 `int` 4개로 바꿉니다. |
| `ConvertToVector128Int32WithTruncation` | `_mm256_cvttpd_epi32` | `double` 4개의 소수 부분을 버리고 `int`로 바꿉니다. |
| `ConvertToVector128Single` | `_mm256_cvtpd_ps` | `double` 4개를 `float` 4개로 바꿉니다. |
| `ConvertToVector256Double` | `_mm256_cvtepi32_pd`<br>`_mm256_cvtps_pd` | `int` 또는 `float` 4개를 `double` 4개로 넓힙니다. |
| `ConvertToVector256Int32` | `_mm256_cvtps_epi32` | `float` 8개를 반올림하여 `int` 8개로 바꿉니다. |
| `ConvertToVector256Int32WithTruncation` | `_mm256_cvttps_epi32` | `float` 8개의 소수 부분을 버리고 `int`로 바꿉니다. |
| `ConvertToVector256Single` | `_mm256_cvtepi32_ps` | `int` 8개를 `float` 8개로 바꿉니다. |

### 비교·비트·원소 재배치

| C# `Avx` | C++ intrinsic | 간단한 설명 |
| --- | --- | --- |
| `Compare` | `_mm[256]_cmp_{ps,pd}` | `FloatComparisonMode` 또는 `_CMP_*` 조건으로 실수를 비교해 비트 마스크를 만듭니다. |
| `CompareEqual`<br>`CompareNotEqual`<br>`CompareGreaterThan`<br>`CompareGreaterThanOrEqual`<br>`CompareLessThan`<br>`CompareLessThanOrEqual` | C++에서는 `_mm256_cmp_{ps,pd}` + 대응 `_CMP_*` 사용 | 자주 쓰는 대소 비교를 이름으로 제공하는 C# 편의 메서드입니다. |
| `CompareNotGreaterThan`<br>`CompareNotGreaterThanOrEqual`<br>`CompareNotLessThan`<br>`CompareNotLessThanOrEqual` | C++에서는 `_mm256_cmp_{ps,pd}` + 대응 `_CMP_*` 사용 | NaN의 unordered 결과까지 포함하는 부정 비교입니다. |
| `CompareOrdered`<br>`CompareUnordered` | C++에서는 `_mm256_cmp_{ps,pd}` + `_CMP_ORD_Q` / `_CMP_UNORD_Q` | NaN이 없는지 또는 있는지를 위치별로 검사합니다. |
| `CompareScalar` | `_mm_cmp_{ss,sd}` | 가장 낮은 원소 하나만 조건에 따라 비교합니다. |
| `And` | `_mm256_and_{ps,pd}` | 비트 AND를 계산합니다. |
| `AndNot` | `_mm256_andnot_{ps,pd}` | `(~left) & right`를 계산합니다. |
| `Or` | `_mm256_or_{ps,pd}` | 비트 OR를 계산합니다. |
| `Xor` | `_mm256_xor_{ps,pd}` | 비트 XOR를 계산합니다. |
| `Blend` | `_mm256_blend_{ps,pd}` | 즉시값 마스크로 두 벡터의 원소를 선택합니다. |
| `BlendVariable` | `_mm256_blendv_{ps,pd}` | 벡터 마스크의 부호 비트로 원소를 선택합니다. |
| `Permute` | `_mm_permute_{ps,pd}`<br>`_mm256_permute_{ps,pd}` | 즉시값으로 각 128비트 lane 안의 원소 순서를 바꿉니다. |
| `PermuteVar` | `_mm_permutevar_{ps,pd}`<br>`_mm256_permutevar_{ps,pd}` | 벡터 인덱스로 각 lane 안의 원소 순서를 바꿉니다. |
| `Permute2x128` | `_mm256_permute2f128_{ps,pd,si256}` | 두 벡터에서 128비트 lane을 골라 새 벡터를 만듭니다. |
| `Shuffle` | `_mm256_shuffle_{ps,pd}` | 두 벡터의 원소를 즉시값 규칙으로 섞습니다. |
| `DuplicateEvenIndexed` | `_mm256_movedup_pd`<br>`_mm256_moveldup_ps` | 짝수 위치 원소를 이웃 위치에 복제합니다. |
| `DuplicateOddIndexed` | `_mm256_movehdup_ps` | `float`의 홀수 위치 원소를 이웃 위치에 복제합니다. |
| `UnpackHigh` | `_mm256_unpackhi_{ps,pd}` | 각 128비트 lane의 상위 절반을 번갈아 끼웁니다. |
| `UnpackLow` | `_mm256_unpacklo_{ps,pd}` | 각 128비트 lane의 하위 절반을 번갈아 끼웁니다. |
| `MoveMask` | `_mm256_movemask_{ps,pd}` | 각 실수 원소의 최상위 비트를 일반 정수 비트 마스크로 모읍니다. |
| `TestC` | `_mm[256]_testc_{ps,pd}`<br>`_mm256_testc_si256` | `(~left & right) == 0`인지 검사합니다. |
| `TestZ` | `_mm[256]_testz_{ps,pd}`<br>`_mm256_testz_si256` | `(left & right) == 0`인지 검사합니다. |
| `TestNotZAndNotC` | `_mm[256]_testnzc_{ps,pd}`<br>`_mm256_testnzc_si256` | AND 결과와 AND-NOT 결과가 모두 0이 아닌지 검사합니다. |

## AVX2 명령어 목록

AVX2는 256비트 정수 연산, gather, 더 넓은 broadcast·permute·가변 shift를 추가합니다. `Avx2`가 `Avx`를 상속하므로 기본 `LoadVector256`, `Store`, `LoadAlignedVector256`, `StoreAligned` 등은 위 AVX 표의 메서드를 그대로 사용합니다.

### 지원 확인·메모리·변환

| C# `Avx2` | C++ intrinsic | 간단한 설명 |
| --- | --- | --- |
| `IsSupported` | CPUID AVX2 + AVX/OS 상태 확인 | 현재 CPU와 프로세스에서 AVX2를 실행할 수 있는지 확인합니다. |
| `LoadAlignedVector256NonTemporal` | `_mm256_stream_load_si256` | 32바이트 정렬 정수 데이터를 비시간적 힌트로 읽습니다. |
| `MaskLoad` | `_mm[256]_maskload_epi{32,64}` | 마스크가 선택한 32·64비트 정수만 읽습니다. |
| `MaskStore` | `_mm[256]_maskstore_epi{32,64}` | 마스크가 선택한 32·64비트 정수만 저장합니다. |
| `BroadcastScalarToVector128` | `_mm_broadcast{b,w,d,q}_epi*`<br>`_mm_broadcast{ss,sd}_p*` | 가장 낮은 정수·실수 원소를 128비트 전체에 복제합니다. |
| `BroadcastScalarToVector256` | `_mm256_broadcast{b,w,d,q}_epi*`<br>`_mm256_broadcast{ss,sd}_p*` | 가장 낮은 정수·실수 원소를 256비트 전체에 복제합니다. |
| `BroadcastVector128ToVector256` | `_mm256_broadcastsi128_si256` | 128비트 정수 블록을 양쪽 lane에 복제합니다. |
| `GatherVector128` | `_mm[256]_i{32,64}gather_{ps,pd,epi32,epi64}` | 인덱스가 가리키는 흩어진 메모리 원소를 128비트 결과로 모읍니다. |
| `GatherVector256` | `_mm256_i{32,64}gather_{ps,pd,epi32,epi64}` | 인덱스가 가리키는 흩어진 메모리 원소를 256비트 결과로 모읍니다. |
| `GatherMaskVector128` | `_mm[256]_mask_i{32,64}gather_{ps,pd,epi32,epi64}` | 마스크가 선택한 흩어진 원소만 128비트 결과로 모읍니다. |
| `GatherMaskVector256` | `_mm256_mask_i{32,64}gather_{ps,pd,epi32,epi64}` | 마스크가 선택한 흩어진 원소만 256비트 결과로 모읍니다. |
| `ConvertToInt32`<br>`ConvertToUInt32` | `_mm256_cvtsi256_si32` | 벡터의 가장 낮은 32비트를 일반 정수로 꺼냅니다. |
| `ConvertToVector256Int16` | `_mm256_cvt{e,eu}pi8_epi16` | signed·unsigned 8비트 정수를 16비트 정수로 넓힙니다. |
| `ConvertToVector256Int32` | `_mm256_cvt{e,eu}pi{8,16}_epi32` | signed·unsigned 8·16비트 정수를 32비트 정수로 넓힙니다. |
| `ConvertToVector256Int64` | `_mm256_cvt{e,eu}pi{8,16,32}_epi64` | signed·unsigned 8·16·32비트 정수를 64비트 정수로 넓힙니다. |

### 정수 산술·곱셈·합산

| C# `Avx2` | C++ intrinsic | 간단한 설명 |
| --- | --- | --- |
| `Abs` | `_mm256_abs_epi{8,16,32}` | signed 정수의 절댓값을 구합니다. |
| `Add` | `_mm256_add_epi{8,16,32,64}` | 같은 위치의 정수를 더하고 넘치는 상위 비트는 버립니다. |
| `Subtract` | `_mm256_sub_epi{8,16,32,64}` | 같은 위치의 정수를 빼고 넘치는 상위 비트는 버립니다. |
| `AddSaturate` | `_mm256_adds_{epi8,epu8,epi16,epu16}` | 8·16비트 덧셈 결과를 자료형의 최솟값·최댓값에 고정합니다. |
| `SubtractSaturate` | `_mm256_subs_{epi8,epu8,epi16,epu16}` | 8·16비트 뺄셈 결과를 자료형 범위에 고정합니다. |
| `Average` | `_mm256_avg_epu{8,16}` | unsigned 8·16비트 두 값의 반올림 평균을 구합니다. |
| `Min` | `_mm256_min_{epi8,epu8,epi16,epu16,epi32,epu32}` | 위치별 정수 최솟값을 선택합니다. 64비트형은 지원하지 않습니다. |
| `Max` | `_mm256_max_{epi8,epu8,epi16,epu16,epi32,epu32}` | 위치별 정수 최댓값을 선택합니다. 64비트형은 지원하지 않습니다. |
| `Multiply` | `_mm256_mul_{epi32,epu32}` | 짝수 위치의 32비트 정수를 곱해 64비트 결과를 만듭니다. |
| `MultiplyLow` | `_mm256_mullo_epi{16,32}` | 각 곱셈 결과의 낮은 16·32비트를 남깁니다. |
| `MultiplyHigh` | `_mm256_mulhi_{epi16,epu16}` | 16비트 곱셈 결과의 높은 16비트를 남깁니다. |
| `MultiplyHighRoundScale` | `_mm256_mulhrs_epi16` | signed 16비트 곱을 반올림·스케일한 높은 부분으로 만듭니다. |
| `MultiplyAddAdjacent` | `_mm256_madd_epi16`<br>`_mm256_maddubs_epi16` | 16비트 쌍은 32비트 합으로, unsigned byte × signed byte 쌍은 포화 16비트 합으로 만듭니다. |
| `HorizontalAdd` | `_mm256_hadd_epi{16,32}` | 각 128비트 lane 안에서 이웃 정수를 더합니다. |
| `HorizontalSubtract` | `_mm256_hsub_epi{16,32}` | 각 128비트 lane 안에서 이웃 정수를 뺍니다. |
| `HorizontalAddSaturate` | `_mm256_hadds_epi16` | 이웃 signed 16비트 정수를 포화 덧셈합니다. |
| `HorizontalSubtractSaturate` | `_mm256_hsubs_epi16` | 이웃 signed 16비트 정수를 포화 뺄셈합니다. |
| `SumAbsoluteDifferences` | `_mm256_sad_epu8` | unsigned byte 차이의 절댓값을 8개씩 합산합니다. |
| `MultipleSumAbsoluteDifferences` | `_mm256_mpsadbw_epu8` | 선택한 byte 묶음끼리 절대 차이 합을 여러 개 계산합니다. |
| `Sign` | `_mm256_sign_epi{8,16,32}` | 두 번째 벡터의 부호에 따라 첫 번째 값을 양수·음수·0으로 만듭니다. |

### 비교·비트·선택

| C# `Avx2` | C++ intrinsic | 간단한 설명 |
| --- | --- | --- |
| `CompareEqual` | `_mm256_cmpeq_epi{8,16,32,64}` | 위치별 정수 값이 같으면 모든 비트가 1인 마스크를 만듭니다. |
| `CompareGreaterThan` | `_mm256_cmpgt_epi{8,16,32,64}` | 위치별 signed 정수의 `left > right` 마스크를 만듭니다. |
| `And` | `_mm256_and_si256` | 정수 비트 AND를 계산합니다. |
| `AndNot` | `_mm256_andnot_si256` | `(~left) & right`를 계산합니다. |
| `Or` | `_mm256_or_si256` | 정수 비트 OR를 계산합니다. |
| `Xor` | `_mm256_xor_si256` | 정수 비트 XOR를 계산합니다. |
| `MoveMask` | `_mm256_movemask_epi8` | 각 byte의 최상위 비트를 32비트 일반 정수 마스크로 모읍니다. |
| `Blend` | `_mm_blend_epi32`<br>`_mm256_blend_epi{16,32}` | 즉시값 마스크로 두 정수 벡터의 원소를 선택합니다. |
| `BlendVariable` | `_mm256_blendv_epi8` | 마스크 byte의 최상위 비트로 두 벡터의 byte를 선택합니다. |

### 원소 재배치·압축·shift

| C# `Avx2` | C++ intrinsic | 간단한 설명 |
| --- | --- | --- |
| `AlignRight` | `_mm256_alignr_epi8` | 각 128비트 lane에서 두 벡터를 이어 byte 단위로 오른쪽 위치를 맞춥니다. |
| `ExtractVector128` | `_mm256_extracti128_si256` | 256비트 정수 벡터에서 128비트 절반을 꺼냅니다. |
| `InsertVector128` | `_mm256_inserti128_si256` | 256비트 정수 벡터의 한쪽 128비트 절반을 교체합니다. |
| `Permute2x128` | `_mm256_permute2x128_si256` | 두 정수 벡터에서 128비트 lane을 골라 새 벡터를 만듭니다. |
| `Permute4x64` | `_mm256_permute4x64_{epi64,pd}` | 네 개의 64비트 원소를 256비트 전체 범위에서 재배치합니다. |
| `PermuteVar8x32` | `_mm256_permutevar8x32_{epi32,ps}` | 벡터 인덱스로 여덟 개의 32비트 원소를 재배치합니다. |
| `Shuffle` | `_mm256_shuffle_epi32`<br>`_mm256_shuffle_epi8` | 32비트 즉시값 또는 byte 인덱스 마스크로 원소를 섞습니다. |
| `ShuffleHigh` | `_mm256_shufflehi_epi16` | 각 128비트 lane의 상위 16비트 원소 네 개를 재배치합니다. |
| `ShuffleLow` | `_mm256_shufflelo_epi16` | 각 128비트 lane의 하위 16비트 원소 네 개를 재배치합니다. |
| `PackSignedSaturate` | `_mm256_packs_epi{16,32}` | 16→8 또는 32→16비트 signed 정수로 포화 축소합니다. |
| `PackUnsignedSaturate` | `_mm256_packus_epi{16,32}` | signed 입력을 unsigned 8·16비트 정수로 포화 축소합니다. |
| `UnpackHigh` | `_mm256_unpackhi_epi{8,16,32,64}` | 각 128비트 lane의 상위 절반을 번갈아 끼웁니다. |
| `UnpackLow` | `_mm256_unpacklo_epi{8,16,32,64}` | 각 128비트 lane의 하위 절반을 번갈아 끼웁니다. |
| `ShiftLeftLogical` | `_mm256_sll{i,}_epi{16,32,64}` | 모든 원소를 같은 수만큼 왼쪽으로 밀고 0을 채웁니다. |
| `ShiftRightLogical` | `_mm256_srl{i,}_epi{16,32,64}` | 모든 원소를 같은 수만큼 오른쪽으로 밀고 0을 채웁니다. |
| `ShiftRightArithmetic` | `_mm256_sra{i,}_epi{16,32}` | signed 원소를 오른쪽으로 밀고 부호 비트를 채웁니다. |
| `ShiftLeftLogicalVariable` | `_mm[256]_sllv_epi{32,64}` | 32·64비트 원소마다 서로 다른 횟수로 왼쪽 shift합니다. |
| `ShiftRightLogicalVariable` | `_mm[256]_srlv_epi{32,64}` | 32·64비트 원소마다 서로 다른 횟수로 논리 오른쪽 shift합니다. |
| `ShiftRightArithmeticVariable` | `_mm[256]_srav_epi32` | signed 32비트 원소마다 서로 다른 횟수로 산술 오른쪽 shift합니다. |
| `ShiftLeftLogical128BitLane` | `_mm256_bslli_epi128` | 각 128비트 lane 전체를 byte 단위로 왼쪽 이동합니다. |
| `ShiftRightLogical128BitLane` | `_mm256_bsrli_epi128` | 각 128비트 lane 전체를 byte 단위로 오른쪽 이동합니다. |

## 사용할 때 꼭 확인할 점

| 항목 | 확인 내용 |
| --- | --- |
| 지원 검사 | C#은 해당 intrinsic 호출보다 먼저 `Avx.IsSupported` 또는 `Avx2.IsSupported`를 확인합니다. |
| 메모리 정렬 | `LoadAlignedVector256`·`StoreAligned`는 주소가 32바이트 경계에 맞아야 합니다. 확실하지 않으면 unaligned 버전을 사용합니다. |
| 128비트 lane | 많은 horizontal·shuffle·unpack 명령은 256비트 전체가 아니라 두 개의 128비트 lane에서 각각 동작합니다. |
| 즉시값 | `control`, `mask`, shift 횟수 등은 명령에 따라 컴파일 시간 상수여야 하며, gather의 `scale`은 `1`, `2`, `4`, `8` 중 하나입니다. |
| 비교 결과 | 비교 결과는 `true`/`false` 객체가 아니라 원소별 모든 비트가 1 또는 0인 벡터 마스크입니다. |
| NaN 비교 | 부동소수점 비교는 ordered/unordered와 signaling 여부가 있으므로 C++에서는 정확한 `_CMP_*` 조건을 선택합니다. |
| FMA | FMA는 AVX2와 별도 CPU 기능입니다. C#에서는 `Fma`, C++에서는 `_mm256_fmadd_*` 계열과 별도 지원 검사를 사용합니다. |
| 성능 | intrinsic 하나가 항상 가장 빠른 것은 아닙니다. 메모리 접근, 정렬, lane 이동, CPU별 처리량을 함께 측정합니다. |

## 공식 문서

- [Microsoft Learn: .NET 8.0 Avx 클래스](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.intrinsics.x86.avx?view=net-8.0)
- [Microsoft Learn: .NET 8.0 Avx2 클래스](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.intrinsics.x86.avx2?view=net-8.0)
- [dotnet/runtime 8.0: Avx.cs 원본](https://github.com/dotnet/runtime/blob/release/8.0/src/libraries/System.Private.CoreLib/src/System/Runtime/Intrinsics/X86/Avx.cs)
- [dotnet/runtime 8.0: Avx2.cs 원본](https://github.com/dotnet/runtime/blob/release/8.0/src/libraries/System.Private.CoreLib/src/System/Runtime/Intrinsics/X86/Avx2.cs)
- [Intel Intrinsics Guide](https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html)
- [Intel: AVX와 AVX2 명령 집합 설명](https://www.intel.com/content/www/us/en/support/articles/000005779/processors.html)
- [Microsoft Learn: MSVC `/arch` 옵션](https://learn.microsoft.com/en-us/cpp/build/reference/arch-x64?view=msvc-170)
- [GCC: x86 옵션](https://gcc.gnu.org/onlinedocs/gcc/x86-Options.html)
