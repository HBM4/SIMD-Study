#include "03-CppCliAvxBridge.h"

// 필수
#include <immintrin.h>

// 현재 PC가 AVX2를 지원하는 CPU인지 체크하기 위함 (저수준 CPU 제어 함수)
#include <intrin.h>

// C++/CLI(Managed C++) 환경에서 C#(CLR) 객체와 C++ 네이티브 포인터를 이어주는 유틸리티 (pin_ptr 등)
#include <vcclr.h>

#pragma managed(push, off)

namespace
{
    // CPU와 운영 체제가 AVX 레지스터 상태를 저장할 수 있는지 확인합니다.
    bool IsAvxSupportedNative()
    {
        int cpuInfo[4] = {};
        __cpuidex(cpuInfo, 1, 0);

        const bool osXSave = (cpuInfo[2] & (1 << 27)) != 0;
        const bool avx = (cpuInfo[2] & (1 << 28)) != 0;

        if (!osXSave || !avx)
        {
            return false;
        }

        const unsigned __int64 xcr0 = _xgetbv(0);
        return (xcr0 & 0x6) == 0x6;
    }

    // 네이티브 포인터 배열을 AVX로 더합니다.
    void AddAvxNative(const float* left, const float* right, float* result, size_t length)
    {
        constexpr size_t vectorSize = 8;
        const size_t vectorEnd = length - (length % vectorSize);
        size_t i = 0;

        for (; i < vectorEnd; i += vectorSize)
        {
            // float 8개를 256비트 레지스터로 읽습니다.
            const __m256 leftVector = _mm256_loadu_ps(left + i);
            const __m256 rightVector = _mm256_loadu_ps(right + i);

            // 같은 위치의 float 8개를 동시에 더합니다.
            const __m256 addedVector = _mm256_add_ps(leftVector, rightVector);

            // 계산 결과를 배열에 한 번에 저장합니다.
            _mm256_storeu_ps(result + i, addedVector);
        }

        // 8개 묶음에 남은 원소는 일반 반복문으로 처리합니다.
        for (; i < length; ++i) { result[i] = left[i] + right[i]; }
    }
}

#pragma managed(pop)

namespace CppCliAvxBridge
{
    // C#에서 AVX 사용 가능 여부를 확인할 수 있게 합니다.
    bool SimdAvxBridge::IsAvxSupported() { return IsAvxSupportedNative(); }

    // 관리 배열을 고정한 뒤 네이티브 AVX 함수에 전달합니다.
    array<float>^ SimdAvxBridge::Add(array<float>^ left, array<float>^ right)
    {
        if (left == nullptr)
        {
            throw gcnew ArgumentNullException("입력 배열은 null일 수 없습니다.");
        }

        array<float>^ result = gcnew array<float>(left->Length);
        AddTo(left, right, result);
        return result;
    }

    // 결과 배열을 재사용해 배열 생성 비용 없이 AVX 연산을 수행합니다.
    void SimdAvxBridge::AddTo(array<float>^ left, array<float>^ right, array<float>^ result)
    {
        if (left == nullptr || right == nullptr || result == nullptr) { throw gcnew ArgumentNullException("입력과 결과 배열은 null일 수 없습니다."); }
        if (left->Length != right->Length) { throw gcnew ArgumentException("두 배열의 길이는 같아야 합니다."); }
        if (left->Length != result->Length) { throw gcnew ArgumentException("결과 배열의 길이는 입력 배열과 같아야 합니다."); }
        if (!IsAvxSupportedNative()) { throw gcnew PlatformNotSupportedException("이 CPU에서는 AVX를 사용할 수 없습니다."); }

        if (left->Length == 0) { return; }

        // pin_ptr는 GC가 배열을 이동하지 못하게 고정합니다.
        pin_ptr<float> leftPointer = &left[0];
        pin_ptr<float> rightPointer = &right[0];
        pin_ptr<float> resultPointer = &result[0];

        AddAvxNative(leftPointer, rightPointer, resultPointer, static_cast<size_t>(left->Length));
    }
}

