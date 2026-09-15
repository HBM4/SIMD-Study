#pragma once

using namespace System;

namespace CppCliAvxBridge
{
    // C# 배열과 네이티브 AVX 코드를 연결하는 공개 클래스입니다.
    public ref class SimdAvxBridge abstract sealed
    {
    public:
        // 현재 CPU와 운영 체제가 AVX를 사용할 수 있는지 확인합니다.
        static bool IsAvxSupported();

        // 두 float 배열을 더한 새 결과 배열을 반환합니다.
        static array<float>^ Add(array<float>^ left, array<float>^ right);

        // 미리 만든 결과 배열에 두 float 배열의 덧셈 결과를 기록합니다.
        static void AddTo(array<float>^ left, array<float>^ right, array<float>^ result);
    };
}
