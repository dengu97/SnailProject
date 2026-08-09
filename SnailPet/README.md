# SnailPet (Unity 2022.3.13f1)

PC 데스크톱 상주 클라이언트.

투명 창은 검증 완료입니다. `Tools/spike` 에서 OS 레벨(투명·항상 위·클릭 통과·창
테두리 감지)이 5/5 로 확인됐고, Unity 플레이어 창에서도 per-pixel alpha 가 살아나는 것을
빌드해서 확인했습니다.

현재는 **데이터 기반 런타임 파츠 합성**이 동작합니다. 실행하면 `EggData` 에서 알을 하나
골라 부화시키고, 그 결과를 `PartsData` / `PartsColorData` / `EnumData.SortOrder` 대로
합성해 창 테두리 위를 걷습니다.

## 처음 열 때

1. Unity Hub → `Add project from disk` → 이 폴더(`SnailPet`) 선택
2. 2022.3.13f1 로 엽니다. 첫 임포트에 몇 분 걸립니다.
3. 프로젝트가 열리면 **셋업이 자동 실행**됩니다 (`SnailPetSetup` 이 `[InitializeOnLoad]`).
   Console 에 `[SnailPet] SnailPet 프로젝트 셋업 완료` 가 뜨면 정상입니다.
   수동으로 다시 돌리려면 메뉴 **SnailPet → 1. 프로젝트 셋업**.
4. 메뉴 **SnailPet → 2. 빌드 & 실행**

## 왜 에디터 Play 모드로는 확인할 수 없나

`TransparentWindow.Apply()` 는 `GetActiveWindow()` 로 자기 창 핸들을 얻는데,
에디터에서는 그게 **에디터 창**입니다. 에디터 창을 투명·클릭 통과로 만들어버리면
Unity 를 조작할 수 없게 되므로, `Application.isEditor` 일 때는 적용을 건너뜁니다.
**반드시 빌드된 플레이어에서 확인**해야 합니다.

## 판정 방법

빌드된 플레이어가 뜨면 화면 위를 달팽이가 왕복합니다.

- **성공** — 달팽이 뒤로 바탕화면과 다른 창들이 그대로 보인다. 외곽선이 매끄럽다.
- **실패** — 달팽이가 검은(또는 회색) 사각형 위에 얹혀 있다.

**25초 뒤 자동 종료됩니다.** 클릭 통과 + 항상 위 + 테두리 없는 전체 화면 창은
마우스로 닫을 수 없기 때문에 넣어둔 안전장치입니다. 급하면 ESC, 그래도 안 되면
작업 관리자에서 `SnailPet.exe` 를 종료하세요.

실행 결과는 `SnailPet/Build/unity-probe-result.txt` 와 플레이어 로그
(`%USERPROFILE%\AppData\LocalLow\SnailTown\SnailPet\Player.log`) 에 남습니다.

## 셋업이 건드리는 설정과 이유

| 설정 | 값 | 이유 |
|---|---|---|
| Run In Background | 켬 | 포커스가 없어도 달팽이가 계속 움직여야 함 |
| Visible In Background | 켬 | 다른 창이 앞에 와도 계속 보여야 함 |
| **Use Flip Model Swapchain** | **끔** | 켜져 있으면 DWM 유리 영역 알파 합성이 깨짐. **투명 창의 핵심** |
| Graphics API | Direct3D11 고정 | D3D12/Vulkan 은 레이어드 윈도우 알파가 불안정 |
| Fullscreen Mode | Windowed | 전체 화면이면 창 스타일을 바꿀 수 없음 |
| Resizable Window | 끔 | 크기는 코드가 가상 화면에 맞춤 |

## 구조

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Desktop/Win32.cs` | P/Invoke 선언. `Tools/spike` 와 동일 내용 |
| `Assets/Scripts/Desktop/WindowSurfaces.cs` | 걸어다닐 창 테두리 수집 + 유령 창 필터. 스파이크에서 검증됨 |
| `Assets/Scripts/Desktop/TransparentWindow.cs` | Unity 전용. 테두리 제거 + DWM 유리 확장 + 항상 위 |
| `Assets/Scripts/SnailPetBootstrap.cs` | 씬 없이 런타임에 카메라·달팽이 생성, 표면 위 왕복 |
| `Assets/Editor/SnailPetSetup.cs` | 프로젝트 설정 자동화, 빌드 메뉴 |
| `Assets/Scripts/Snail/SnailAppearance.cs` | 개체 외형 정의 + `SortOrder` 조회 |
| `Assets/Scripts/Snail/SnailComposer.cs` | 파츠 스프라이트를 겹쳐 달팽이를 만든다 |
| `Assets/Scripts/Snail/SnailMetrics.cs` | 발선(몸통만) + 가로 경계(전체) 실측, 캐시 |
| `Assets/Scripts/Snail/SnailHatchery.cs` | 알 → 개체. `PartsGroupIds` union 추첨 |
| `Assets/Editor/SnailArtImporter.cs` | 아트를 Sprite 로 임포트 (PPU=1, 최대 512px) |
| `Assets/Resources/Snail/` | 파츠 아트 (단일 소스) |

데이터는 `Tools/DataPipeline` 이 생성한 `Assets/Scripts/Generated/GameData.g.cs` 를 씁니다.
`SnailData.xlsx` 를 고쳤다면 파이프라인을 다시 돌려야 반영됩니다.

## 배치모드 빌드

```bash
& "C:\Program Files\Unity\Hub\Editor\2022.3.13f1\Editor\Unity.exe" -batchmode -quit -nographics `
  -projectPath SnailPet -executeMethod SnailPet.EditorTools.SnailPetSetup.BuildOnly -logFile -
```

> 스크립트를 새로 추가한 직후 첫 실행은 **에셋 임포트가 끝나기 전에 컴파일이 돌아**
> `CS0234` 로 실패할 수 있습니다. 그대로 한 번 더 실행하면 통과합니다.

## 발선(接地) 처리 — 런타임 합성 때 반드시 손볼 것

달팽이를 창 테두리에 올려놓으려면 「발이 어디인가」를 알아야 한다.
지금은 **합성된 스프라이트 전체의 최하단 불투명 픽셀**을 발로 간주한다.

현재 아트 실측값 (1200x1200 캔버스, 선화 기준):

| 파츠 | 하단 y | |
|---|---|---|
| `commonbody01` | 957 | 발선 |
| `commonbody02` | 954 | 실루엣이 달라도 3px 차이 |
| `commonshell01` / `02` / `rareshell01` | 804 / 782 / 784 | 몸통보다 위 |
| 더듬이 4종 | 317~362 | 훨씬 위 |
| 눈 4종 | 439~536 | 훨씬 위 |

몸통이 항상 가장 아래라 지금은 우연히 맞는다. 하지만 **점액(Mucus)이나
아래로 늘어지는 가방**처럼 몸통보다 내려오는 파츠가 들어오면 발 위치가
그쪽으로 끌려가 달팽이가 테두리에서 뜬다.

**런타임 합성을 만들 때:**

- **세로(발선)는 `Body` 레이어의 알파 경계만** 쓴다 → 다른 파츠가 아무리 늘어져도 무관
- **가로는 합성 전체**를 쓴다 → 더듬이가 몸통보다 바깥으로 나오므로
  (`commonfeeler01` 좌단 73 vs `commonbody01` 좌단 104) 전체를 써야 화면 끝에서 안 잘린다
- Body 파츠별로 **한 번만 재고 `ResourceKey` 로 캐시** → 멀티방에 달팽이가 많아도 비용 고정

발선이 아트마다 크게 흔들리면 그때 `PartsData` 에 `FootOffsetY` 열을 추가해
명시적으로 잡는다. 현재는 3px 차이라 실측으로 충분하다.
