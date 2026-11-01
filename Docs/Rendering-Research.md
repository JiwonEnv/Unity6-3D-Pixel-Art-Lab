# Never’s End 스타일을 위한 Unity 6 렌더링 연구

검토일: 2026-11-01. 확인한 원작 기법과 이번 프로젝트의 근사 구현을 구분합니다.

## 공개 자료에서 확인한 핵심

Ryan Juckett의 GDC 2026 발표는 원작이 독자 엔진에서 3D 모델을 스프라이트처럼 보이게 한다고 설명합니다.
발표에서는 후처리 선화, 재질별 툰 램프, 조절한 법선과 그림자 형상,
카메라·모델·관절의 픽셀 정렬, 물체 단위 스프라이트 정렬을 다룹니다.
낮은 해상도로 축소하거나 텍스처만 Point로 바꾸는 것보다 범위가 넓습니다.

출처: [개발자 GDC 2026 발표 PDF](https://media.gdcvault.com/gdc2026/Slides/Juckett_Ryan_HowWeDrawA3DSpriteWorldTheStylizedArtOfNeversEnd.pdf),
PDF 페이지 13, 20–29, 35–43, 65–69, 74–83.

참고 영상: [Official Combat Demo Trailer](https://www.youtube.com/watch?v=n7To4hUKTYQ).
영상의 환경에서는 큰 색면, 물체 경계, 형태를 설명하는 내부선이 잘 읽힙니다.
이하의 구현 우선순위는 이 관찰에 따른 판단입니다.

## 초기 장면이 달랐던 이유

1. 상자를 많이 쌓는 것은 픽셀 스프라이트 표현과 다른 모델링 방식입니다.
2. 무작위 밝기 노이즈가 큰 색면과 실루엣을 방해했습니다.
3. Standard의 연속 명암에는 그림의 명암 경계와 재질별 그림자 색을 설계하는 단계가 없었습니다.
4. 저해상도여도 선화가 없으면 단순히 작은 3D 화면으로 보입니다.
5. 연속 카메라 이동과 임의 배율 확대는 픽셀의 모양·폭을 불안정하게 만듭니다.
6. 멀리서 보는 구도는 캐릭터의 의상·장비를 읽을 픽셀 수가 부족했습니다.

## 실제 적용한 Unity 설정

아래 값은 이번 연구의 실험값이며 원작에서 추출한 수치가 아닙니다.

| 항목 | 값 / 방식 | 목적 |
| --- | --- | --- |
| 파이프라인 | Built-in, Forward | 기존 프로젝트에서 커스텀 조명·후처리 검증 |
| 카메라 | Orthographic, yaw 42°, 기본 pitch 35° | 일정한 화면 밀도 |
| 내부 해상도 | 기본 세로 360px | 환경·캐릭터·선화의 픽셀 격자 통일 |
| 기본 밀도 | 21px / 월드 단위 | 2단위 캐릭터에 약 42px 높이 확보 |
| 직교 크기 | height / (2 × PPU) | 줌의 정수 밀도 유지 |
| 위치 | 화면축 X/Y를 격자 반올림 후 월드로 재변환 | 고정 방향 이동의 서브픽셀 흔들림 억제 |
| 기울기 | asin(round(sin(35°) × PPU) / PPU) | 지면 1단위의 세로 투영도 정수 픽셀 |
| 확대 | Point, 정수 배율, 남는 공간은 여백 | 픽셀 폭의 불균일 방지 |
| MSAA / HDR | OFF / OFF | 선명한 선·제한된 명암 |
| 그림자 | Hard, Very High, 2 cascades, distance 60 | 형태가 읽히는 그림자 |
| 광원 | 따뜻한 방향광 1개, intensity 1 | 평가하기 쉬운 통제 조명 |
| 안개 | OFF | 작은 비교 장면의 거리 흐림 배제 |
| 컬러 공간 | Linear | 조명·색 변환 기준 통일 |

픽셀 정렬을 켜도 자유 회전 중 실루엣 변화까지 정지하지는 않습니다.
한 방향을 고정하고 이동할 때의 안정성부터 평가해야 합니다.

## 조명 셰이더

`SpriteToon.shader`는 표면 방향과 주 광원의 조도·그림자 감쇠를 4단계로 나눕니다.
단계는 0 / 0.48 / 0.82 / 1, 조도 경계는 0.16 / 0.48 / 0.78입니다.
그림자는 재질의 ShadeColor로 조절합니다. T 키는 같은 재질의 명암만 연속값으로 바꿉니다.

`noambient`와 `noforwardadd`를 사용하므로 Ambient 설정과 추가 Point Light를
Standard와 동일하게 해석하지 않습니다. 주 광원·재질의 그림자 색이 조절 지점입니다.
랜턴 표면은 Emission이며 주변을 밝히는 광원 구현은 아닙니다.

## 선화

`PixelLinework`가 DepthNormals를 요청하고 OnRenderImage에서 선을 합성합니다.
저해상도 버퍼의 상하좌우 이웃을 비교해 깊이 차와 법선 변화를 감지합니다.
깊이 선은 앞쪽 픽셀에만 그리며 문턱은 월드 깊이 0.16입니다.
법선 문턱은 1 − dot(n1,n2) = 0.32이고 약한 선을 그립니다.
선 색은 표면 색에 어두운 보라를 곱합니다.

같은 깊이·법선의 두 물체나 재질 영역을 ID로 구분하지 못합니다.
후속 구현에서는 Object ID, Material ID, 마스크를 별도 버퍼에 출력하고
경계의 어느 쪽에 선을 그릴지 우선순위를 정해야 합니다.

Unity 구현 근거:

- [Unity 6 Camera: depthTextureMode / OnRenderImage](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera.html)
- [Unity 6 Surface Shaders: Built-in 전용](https://docs.unity3d.com/6000.0/Documentation/Manual/SL-SurfaceShaders.html)
- [Depth / DepthNormals 출력](https://docs.unity.com/en-us/engine/6000.6/manual/cameras/camera-output/sl-camera-depth-texture)

URP로 전환하면 Surface Shader와 OnRenderImage를 그대로 옮길 수 없습니다.
커스텀 HLSL 조명과 Renderer Feature/Render Graph 패스로 다시 구성해야 합니다.
URP를 켜는 것 자체가 스타일을 만들어주지는 않습니다.

## 모델 제작의 다음 단계

### 기존 도구를 활용하는 대안

| 경로 | 활용 범위 | 이번 프로젝트의 판단 |
| --- | --- | --- |
| 현재 커스텀 셰이더 | 렌더링 원리·값을 직접 실험 | 이미 구현, 외부 패키지 없이 비교 가능 |
| URP + ProPixelizer | 물체별 픽셀화·선화·이동 정렬을 묶은 기존 솔루션 | 다음 제작 단계의 후보, 구매·설치하지 않음 |
| 3D 애니메이션을 스프라이트로 미리 렌더 | 고정된 시점·방향에서 아트 결과를 직접 수정하기 쉬움 | 자유 카메라·장비 조합이 핵심이면 관리 비용 검토 필요 |

ProPixelizer는 공급자 문서에서 URP 요구 사항, 물체별 선화와 정렬 기능을 설명합니다.
사용하려면 현재 Built-in 프로젝트의 파이프라인·재질 이전이 필요합니다.
따라서 지금 패키지를 설치하는 대신 먼저 기본 원리가 보이는 비교 장면을 구성했습니다.

- [ProPixelizer 공식 개요](https://propixelizer.github.io/docs/usage/overview/)
- [공식 요구 사항과 버전 호환성](https://propixelizer.github.io/docs/getting_started/requirements/)
- [물체 위치 정렬 컴포넌트](https://propixelizer.github.io/docs/technical/objectrendersnapable/)

사전 렌더 방식의 실제 제작 사례로 Dead Cells의 아티스트는 3D 캐릭터의 애니메이션을
작은 PNG 프레임과 노멀맵으로 출력해 툰 조명을 적용하는 과정을 공개했습니다.
이는 자유롭게 회전하는 실시간 3D 월드 렌더링과 구별해서 검토할 방법입니다.
[Thomas Vasseur의 제작 설명](https://www.gamedeveloper.com/production/art-design-deep-dive-using-a-3d-pipeline-for-2d-animation-in-i-dead-cells-i-).

### 아트 제작 우선순위

현재 모델은 원리 검증용 절차적 프록시입니다. 완성된 게임용 아트가 아닙니다.
다음 순서가 효과적이라고 판단합니다.

1. 캐릭터 한 명과 나무 한 그루를 목표 카메라에서 완성합니다. 40–64px 높이에서 읽히는 실루엣이 우선입니다.
2. 큐브를 픽셀처럼 쌓기보다 몸통·머리·의상·잎의 큰 덩어리를 모델링합니다.
3. 줄눈·지붕 경계·얼굴에 필요한 선만 설계하고 균일한 노이즈를 피합니다.
4. 재질별 팔레트를 정하고 밝기뿐 아니라 그림자 색조도 검토합니다.
5. Blender에서 커스텀 법선과 그림자 프록시를 만들어 원치 않는 틈의 검은 영역을 제어합니다.
6. 환경·캐릭터의 관통을 검사하고 ID 선화와 물체 단위 정렬을 개발합니다.
7. 정지 화면을 확보한 다음 애니메이션 프레임·관절의 픽셀 안정성을 검토합니다.

일괄적인 포스터라이즈·디더링은 기본 기능으로 넣지 않았습니다.
전체 화면의 색 수를 줄이면 피부·석재·수목의 팔레트가 함께 손상될 수 있어 재질별로 검토할 대상입니다.

## 검증 범위

새 미리보기 두 장은 같은 장면·구도에서 명암 단계와 선화를 바꾼 Unity 렌더입니다.
자동 Play 검증은 격자, PPU, 정수 확대, 해상도 전환과 효과별 픽셀 변화를 확인합니다.
원작 수준의 시각적 품질이나 애니메이션 전체의 안정성을 보증하는 검증은 아닙니다.
