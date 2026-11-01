# 세미리얼 에셋 비교 씬

`Assets/Scenes/Semi-Real-Pixel-Woodland.unity`를 열고 Play.
기존 Amberwood 씬을 복제해 야영지와 호숫가 일부에 상세 환경을 적용했다.
캐릭터·야영 장비와 나머지 지역은 비교를 위해 기존 에셋을 유지한다.

## 에셋

Powered by Poly Haven. 아래 모델과 텍스처는 모두 무료 CC0이며 재배포 가능하다.
출처: https://polyhaven.com/license

| 에셋 | 적용 |
| --- | --- |
| https://polyhaven.com/a/island_tree_01 | 잎·잔가지·굴곡진 줄기, 2그루 |
| https://polyhaven.com/a/boulder_01 | 표면 굴곡과 거칠기가 있는 바위, LOD1 12개 |
| https://polyhaven.com/a/fern_02 | 잎이 분리된 고사리, 네 변형 16개 |
| https://polyhaven.com/a/forest_ground_04 | 지면과 산책로의 낙엽·흙 표면 |

원본 FBX와 표면 맵을 ThirdParty/PolyHaven에 포함한다.
텍스처 가져오기 최대 크기는 1024, 노멀은 NormalMap, ARM·마스크는 선형 색 공간이다.
source-files.json은 공개 API의 다운로드 출처 기록이다.

## 기존 렌더링에 추가한 것

- 기존 Sprite Toon 셰이더에 노멀 맵, AO·거칠기, 직접광 GGX 반사와 연속 명암 혼합을 추가했다. 기본값은 0이므로 기존 재질은 기존 명암을 쓴다.
- 잎은 양면 조명과 투명 마스크를 사용한다. 색상 렌더링·그림자·물체 ID·깊이 패스 모두 같은 투명 경계와 바람 변형을 적용한다.
- 나뭇잎 텍스처의 투명 경계 색 번짐을 보정해 검은 테두리를 줄인다. 상세 에셋은 내부 선화를 약하게 한다. 노멀 맵의 미세 요철을 선화로 그리지 않는다.
- 물은 기존 수면 셰이더에 연속 시간의 겹친 파형, 두 방향으로 흐르는 잔물결 노멀과 평면 반사를 추가했다. 반사는 640×360, 최대 20회/초이며 수면·강수는 반사 카메라에서 제외한다.
- 낮·밤, 횃불, 바람, 비와 눈의 젖음·적설을 새 재질에도 적용한다.

완전한 Standard/PBR 셰이딩이나 유체 시뮬레이션은 아니다. GGX는 직접광 근사이며 실시간 평면 반사로 수면의 주변 풍경을 보완한다.

## 비교 조작

| 키 | 기능 |
| --- | --- |
| J | Hybrid → Toon → 연속 명암 |
| K | 노멀 맵 켜기/끄기 |
| C | 수면 반사 켜기/끄기 |
| M | 야영지 / 호숫가 이동 |
| Tab | 픽셀 해상도 / 화면 해상도 |
| O | 선화 |
| WASD / Shift / Space | 이동 / 달리기 / 점프 |
| Q,E / 휠 | 카메라 회전 / 확대 |
| F1~F4 / L | 아침·정오·노을·밤 / 시간 흐름 |
| 1,2,3 / B / V | 맑음·비·눈 / 바람 / 물결 |

상세한 잎은 360픽셀 화면에서 작은 점으로 합쳐진다. 확대하거나 Tab으로 원래 해상도와 비교할 수 있다.
원본 나무는 약 160만 삼각형이므로 두 그루만 사용했다. 이 씬은 표현 비교용이며 넓은 숲 전체에 배치할 수준의 LOD·잎 군집 최적화는 아직 하지 않았다.

## 검증과 재생성

- 메뉴: `Pixel Art Lab > Create Semi-Real Woodland Comparison` (비교 씬과 파생 재질을 다시 만든다).
- 렌더링 검증: `PixelArtLab.Editor.DetailedWoodlandVerification.Run`.
- Unity Play 모드 이동·충돌·점프 검증: `PixelArtLab.Editor.DetailedWoodlandVerification.Play`.
- 결과: `Previews/Detailed-Woodland-Verification.txt`, `Previews/Detailed-Woodland-Playtest.txt`.
- 실행 파일은 빌드하지 않았다.
