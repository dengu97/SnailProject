# SnailPet (Unity 2022.3.13f1)

PC 데스크톱 상주 클라이언트. 현재는 **투명 창 스파이크 단계**입니다.

`Tools/spike` 에서 OS 레벨(투명·항상 위·클릭 통과·창 테두리 감지)이 5/5 로 확인됐고,
여기서 확인하려는 것은 **Unity 플레이어 창에서도 per-pixel alpha 가 살아나는가** 하나입니다.

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
| `Assets/StreamingAssets/snail_preview.png` | 합성된 달팽이 (임시). 아래 명령으로 재생성 |

스프라이트는 `Resource/` 의 파츠를 합성해 만든 임시 이미지입니다. 다시 뽑으려면:

```bash
dotnet run --project Tools/spike/DesktopShellProbe -c Release -- --export SnailPet/Assets/StreamingAssets/snail_preview.png 512
```

파츠 합성을 Unity 런타임에서 직접 하는 것은 데이터 파이프라인 작업 이후입니다.
지금은 투명 창 검증에만 집중합니다.
