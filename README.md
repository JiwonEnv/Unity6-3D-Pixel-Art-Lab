# Unity6-3D-Pixel-Art-Lab

Unity 6에서 **3D 모델을 2D 픽셀 스프라이트처럼 표현하는 렌더링 연구**입니다.
Never’s End 개발자의 GDC 2026 발표와 Unity 문서를 참고합니다. 원작의 에셋·코드는 사용하지 않습니다.

![Toon 명암을 유지하면서 횃불 주변광을 적용한 화면](Previews/10-After-Toon-Torch.png)

## 실행

1. Unity Hub에 이 폴더를 등록하고 **6000.3.9f1**로 엽니다.
2. **Assets/Scenes/3D-Sprite-Rendering-Study.unity**를 열고 Play를 누릅니다.
3. I / G / N으로 물체별 선과 조정한 법선을 비교합니다.

| 입력 | 기능 |
| --- | --- |
| WASD / 방향키 | 카메라 이동 |
| Q / E | 카메라 회전 |
| 휠 | 정수 픽셀 밀도 변경 |
| T | 단계형 툰 명암 / 연속 명암 |
| O | 한 소스 픽셀 두께 선화 |
| P | 카메라 위치·기울기 픽셀 정렬 |
| Tab | 저해상도 / 기본 해상도 |
| I | 물체 ID별 선화 / 깊이·법선만 사용 |
| G | 물체 ID를 서로 다른 색으로 확인 |
| N | 설계한 법선 / 자동 계산 법선 비교 |
| 1 / 2 / 3 | 전체 / 기본 / 캐릭터 확대 구도 |
| R / H | 구도 초기화 / 안내 숨기기 |

## 세 번째 연구: 물체별 선화와 설계한 명암

- Built-in용 커스텀 툰 조명, 재질별 기본색·그림자 색, 4단계 명암.
- 16bit 물체 ID와 외곽·내부선 강도를 별도 버퍼에 기록합니다.
- 나무·캐릭터·건물을 의미 단위로 묶어 불필요한 내부선을 억제합니다.
- 수목·관목·얼굴·머리카락·망토의 법선 방향을 조정한 메쉬를 저장합니다.
- 기본 세로 360px, 27px/unit, Point 필터, 정수 배율 확대.
- 화면 픽셀 격자에 카메라를 정렬하고 정수 밀도에 맞춰 줌·기울기를 계산.
- 무작위 텍셀 노이즈 대신 넓은 색면과 형상을 사용하는 모델 구성.
- 얼굴·장비를 보강한 캐릭터, 가늘어지는 가지, 잎 덩어리, 관목과 꽃.
- 방향광과 실제 Point Light의 추가 툰 조명 패스로 횃불 주변을 밝힙니다.
- 추가 광원은 표면 명암만 단계화하고 거리 감쇠를 유지해 Toon 모드에서도 주변광이 사라지지 않습니다.
- 기존 Sprite Toon 재질의 `Local Light Strength`로 국소 조명의 반응 세기를 조절할 수 있습니다(기본 1).

[캐릭터 확대](Previews/07-Character-Lighting-Closeup.png) · [물체 ID 확인](Previews/06-Object-Identity-Debug.png) · [기본 법선 비교](Previews/08-Default-Normals-Comparison.png)

법선은 Blender에서 수작업으로 편집한 것이 아니라 스크립트로 방향을 설계한 결과입니다.
렌더링 메쉬의 법선만 바꾸며 별도 그림자용 모델은 아직 사용하지 않습니다.

[두 번째 연구](Previews/04-Before-Art-Direction.png)와 [당시 연속 명암·선화 OFF](Previews/03-Sprite-World-Continuous-No-Lines.png)는 이전 버전 비교용입니다. Standard/PBR 비교는 아닙니다.

원작과 같은 완성도를 달성한 결과가 아니라 핵심 원리를 검증하는 장면입니다.
미구현: Material ID 선화, 물체 단위 앞뒤 정렬, 별도 그림자 수신 형상,
관절 픽셀 정렬, 애니메이션, 원작과 같은 재질별 조명 램프·SSAO.
물체 ID는 현재 선화 제어용이며 기본 3D 깊이 판정을 스프라이트 정렬로 대체하지 않습니다.

설정과 제작 방향은 [렌더링 연구 문서](Docs/Rendering-Research.md)에 정리했습니다.

## 재생성·검증

- `Pixel Art Lab > Build Research-Based Sprite World`: 새 장면과 비교 이미지 생성.
- 장면을 덮어쓰므로 편집한 장면은 다른 이름으로 저장하세요.
- 자동 검증 진입점: `PixelArtLab.Editor.SpriteWorldVerification.Run`.
- 횃불 회귀 검증: `PixelArtLab.Editor.SpriteWorldVerification.RunTorchLighting`.
- [횃불 검증 결과](Previews/Torch-Lighting-Verification.txt): Toon/연속 명암에서 광원 ON/OFF의 화면 차이 비교.
- [검증 결과](Previews/Rendering-Verification.txt): 카메라 정렬, 정수 확대, ID 복원, 선화·법선·광원의 픽셀 변화, 해상도 전환.

## 첫 번째 연구 보존

`Assets/Scenes/3D-Pixel-Art-Lighting-Study.unity`는 블록 모델과 픽셀 표면 질감 위주였던 초기 시도입니다.
목표와 차이가 커 비교용으로 보존합니다. 현재 기본 실행 장면은 세 번째 연구로 갱신했습니다.
