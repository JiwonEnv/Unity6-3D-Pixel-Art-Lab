# 프로젝트 규칙 (재사용 구조)

이 프로젝트는 배경 제작에 자주 쓰는 기술을 **기능별로 따로 구현하고, 다른 환경 프로젝트에 그대로 옮겨 쓸 수 있게** 만드는 것을 목표로 한다.
이 문서는 그 재사용 구조의 규칙이다. 리팩터링 단계(R0~R5)는 아래 "로드맵"에 있다.

## 1. 폴더 구조

```txt
Assets
└─ PixelArtLab                      다른 프로젝트로 복사하는 단위
   ├─ Runtime                       재사용 코어 (PixelArtLab.Runtime)
   │  ├─ Core                       렌더러 기능, 레이어, 공용 렌더 유틸
   │  ├─ PixelCamera                픽셀 버퍼·격자 스냅·서브픽셀 보정
   │  ├─ Linework                   물체 ID 선화
   │  ├─ Shading                    조정한 법선
   │  ├─ Environment                시간대·날씨·바람·강수·생활 조명
   │  ├─ Water                      웅덩이·평면 반사·발 파문
   │  ├─ Snow                       눈 발자국
   │  ├─ Visibility                 캐릭터 가림 해소
   │  ├─ Atmosphere                 픽셀 볼류메트릭
   │  └─ Character                  예제 캐릭터 이동 (R3에서 기능과 분리 예정)
   ├─ Shaders                       Sprite Toon, 선화, 볼류메트릭, 강수 (R2에서 기능별 모듈로 분리 예정)
   ├─ Editor                        재사용 에디터 도구 (PixelArtLab.Editor)
   │  ├─ Setup                      URP 파이프라인 설정
   │  └─ Tools                      프로젝트 점검 도구
   ├─ Settings                      프로젝트 공통 렌더 설정 (RP_, RD_)
   └─ Samples                       이 프로젝트 전용 예제 (다른 프로젝트로 옮기지 않음)
      ├─ Runtime                    예제 게임플레이·비교 조작 (PixelArtLab.Samples)
      ├─ Editor                     씬 생성·검증·단계 적용 스크립트 (PixelArtLab.Samples.Editor)
      └─ Settings                   Woodland 전용 Volume 프로필 (VP_)
```

- 씬(`Assets/Scenes`), 아트(`Assets/Art`), 외부 에셋(`Assets/ThirdParty`)은 예제 콘텐츠다. 씬 생성 스크립트가 경로 문자열로 참조하므로 R0에서는 옮기지 않았다(R4에서 기술별 테스트 씬과 함께 정리).
- `Docs`와 `Previews`는 저장소 루트에 둔다.

## 2. 어셈블리 (asmdef)

| 어셈블리 | 위치 | 참조 | 플랫폼 |
| --- | --- | --- | --- |
| `PixelArtLab.Runtime` | Runtime | URP Core·Universal Runtime | 전체 |
| `PixelArtLab.Editor` | Editor | Runtime | Editor |
| `PixelArtLab.Samples` | Samples/Runtime | Runtime | 전체 |
| `PixelArtLab.Samples.Editor` | Samples/Editor | Runtime, Samples, Editor | Editor |

- **참조는 안쪽으로만 간다.** Samples → Runtime은 되지만 Runtime → Samples는 안 된다. 그래서 Runtime 폴더만 다른 프로젝트에 복사해도 컴파일된다.
- 네임스페이스는 `PixelArtLab`(런타임), `PixelArtLab.Editor`(에디터)다.

## 3. 이름 규칙

새로 만드는 에셋부터 적용한다. 기존 예제 아트 에셋 이름은 씬 생성 스크립트가 참조하므로 R4에서 함께 바꾼다.

| 접두사 | 종류 | 예 |
| --- | --- | --- |
| `SH_` | 셰이더 파일 | `SH_SpriteToon` (R2) |
| `MAT_` | 머티리얼 | `MAT_Lantern_Glass_Warm` |
| `TEX_` | 텍스처 | `TEX_ForestGround_Diff` |
| `PFB_` | 프리팹 | `PFB_Lantern_Post` |
| `SO_` | ScriptableObject 프리셋 | `SO_TimeOfDay_Woodland` (R1) |
| `VP_` | URP Volume 프로필 | `VP_Woodland_Grade_Night` |
| `RP_` | 렌더 파이프라인 에셋 | `RP_PixelArtLab_PC` |
| `RD_` | 렌더러 데이터 | `RD_PixelArtLab_PC` |

- 셰이더 전역값과 재질 속성은 `_Lab` 접두사를 쓴다(예: `_LabEnvironment`, `_LabHeightFog`). 다른 셰이더와 이름이 겹치지 않게 하기 위해서다.
- 메뉴는 `Pixel Art Lab/…` 아래에 둔다.

## 4. 레이어

| 번호 | 이름 | 용도 |
| --- | --- | --- |
| 4 | Water | Unity 기본 레이어. 반사 촬영에서 제외 |
| 8 | Character | 플레이어 몸체. 지면 탐색에서 제외 |
| 13 | Effects | 강수 입자·파문·발자국. 반사와 지면 탐색에서 제외 |

- 코드는 숫자를 직접 쓰지 않고 `PixelArtLab.LabLayers`를 쓴다. 다른 프로젝트에서 레이어 번호가 다르면 이 파일만 고친다.
- 이름은 `Project Settings > Tags and Layers`에 설정한다.

## 5. 다른 프로젝트로 옮기는 방법 (R0 기준)

1. `Assets/PixelArtLab`에서 `Runtime`, `Shaders`, `Editor`, `Settings`를 복사한다(`Samples`는 제외).
2. URP 17.3 패키지를 설치하고 `Pixel Art Lab > Set Up PC URP`를 실행한다.
3. 레이어 4/8/13을 비워 두거나 `LabLayers`를 그 프로젝트 번호로 바꾼다.
4. `Pixel Art Lab > Tools > Check Missing Scripts`로 누락 스크립트를 확인한다.

현재 한계(이후 단계에서 해결):
- 여러 기능이 `WorldEnvironment` 하나와 예제 캐릭터(`ExplorerMotor`)에 의존한다 → R1·R3
- Sprite Toon 하나에 모든 표면 기능이 들어 있다 → R2
- 강수 입자에 예제 호수 위치가 박혀 있다 → R3

## 6. 로드맵

| 단계 | 내용 | 상태 |
| --- | --- | --- |
| R0 | 폴더·어셈블리·이름 규칙·레이어 정리 | 완료 |
| R1 | 환경 코어 분리, 시간대·날씨 프로필 SO | 예정 |
| R2 | Sprite Toon 기능별 모듈·키워드 분리 | 예정 |
| R3 | 기능 컴포넌트·프리셋, Volume 기반 설정, 예제 의존 제거 | 예정 |
| R4 | 기술별 테스트 씬과 검증 | 예정 |
| R5 | Notion 정리 | 예정 |
