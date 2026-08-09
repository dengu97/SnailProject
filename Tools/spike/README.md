# 데스크톱 셸 스파이크

기획서의 「PC 데스크톱 상주」가 Windows 에서 실제로 성립하는지 확인하는 스파이크입니다.
Unity 프로젝트를 세우기 **전에** OS 레벨 위험을 먼저 걷어내는 것이 목적입니다.

```bash
dotnet run --project Tools/spike/DesktopShellProbe -c Release -- 8
```

인자는 걸어다닐 초 수(기본 10). 실행하면 달팽이가 화면에 나타나 가장 넓은 창의
위쪽 테두리를 따라 기어간 뒤 스스로 종료하고, 결과를 `Tools/spike/probe-result.txt` 에 씁니다.

## 검증 결과 (Windows 11 build 26200, .NET 8.0.5) — 5/5 통과

| 항목 | 결과 |
|---|---|
| 파츠 합성 | OK — 220×220, **반투명 픽셀 1,796개** |
| 투명 창 (per-pixel alpha) | OK — `WS_EX_LAYERED` + `UpdateLayeredWindow` |
| 항상 위 / 작업표시줄·Alt-Tab 숨김 | OK — `WS_EX_TOOLWINDOW` + `TopMost` |
| 클릭 통과 | OK — `WS_EX_TRANSPARENT`, 중심점 히트테스트가 뒤 창을 반환 |
| 창 표면 수집 | OK — 실제 앱 창 6개 |

「반투명 픽셀 1,796개」가 핵심 증거입니다. 색상 키(TransparencyKey) 방식이었다면 이 값이 0이고
달팽이 외곽선이 계단처럼 깨집니다. 안티에일리어싱된 알파가 그대로 살아있다는 뜻입니다.

## 스파이크가 밝혀낸 제약

**① 멀티 모니터에서 좌표가 음수가 됩니다.**
이 PC 는 주 모니터 왼쪽에 두 번째 모니터가 있어 x 범위가 `-1920..1920` 입니다.
`Screen.PrimaryScreen` 기준으로 짜면 왼쪽 모니터에서 달팽이가 사라집니다.
**가상 화면 좌표(virtual screen)** 를 써야 합니다.

**② `GetWindowRect` 를 쓰면 안 됩니다.**
Win10/11 은 창 주변에 보이지 않는 그림자 여백을 포함해서 돌려주므로, 달팽이가 테두리에서
몇 픽셀 떠 보입니다. `DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)` 를 써야
눈에 보이는 테두리와 일치합니다.

**③ 창 목록에 유령이 많습니다.** 필터 없이 수집하면 이런 것들이 걸어다닐 표면으로 잡힙니다:

- `Program Manager` (바탕화면 셸) — 가상 화면 전체 폭이라 항상 「가장 넓은 창」으로 뽑힘
- `NVIDIA GeForce Overlay` — 보이지 않는 전체 화면 오버레이
- `KakaoTalkShadowWnd` — 그림자 헬퍼 창
- cloaked 상태로 살아있는 UWP 앱

`WindowSurfaces.Collect` 가 소유된 창 / `WS_EX_TRANSPARENT` / `WS_EX_TOOLWINDOW` /
셸 클래스(`Progman`, `WorkerW`, `Shell_TrayWnd`, `*ShadowWnd`) / cloaked 를 걸러냅니다.
`WS_EX_TRANSPARENT` 필터는 덤으로 **다른 달팽이 인스턴스도 자동으로 제외**합니다 —
펫 자신이 정확히 그 스타일을 쓰기 때문입니다.

## Unity 로 가져갈 때

`Win32.cs` 와 `WindowSurfaces.cs` 는 **`UnityEngine` 의존성이 없도록** 작성했습니다.
그대로 `Assets/Scripts/Desktop/` 에 복사하면 됩니다. `PetForm.cs` 만 WinForms 전용이라,
Unity 에서는 자기 창 핸들을 얻어(`GetActiveWindow`) 동일한 확장 스타일을 심는 코드로 대체합니다.

**아직 확인되지 않은 것**: Unity 자체 창에서 per-pixel alpha 가 나오는지.
Unity 는 자기 스왑체인으로 그리므로 `UpdateLayeredWindow` 대신
카메라 클리어 알파 0 + `DwmExtendFrameIntoClientArea(margins = -1)` 경로를 씁니다.
이건 Unity 프로젝트를 세운 뒤 확인해야 합니다.

## 파일

| 파일 | 역할 |
|---|---|
| `Win32.cs` | P/Invoke 선언. Unity 로 그대로 이식 |
| `WindowSurfaces.cs` | 걸어다닐 창 테두리 수집 + 유령 창 필터. Unity 로 그대로 이식 |
| `PetForm.cs` | 레이어드 윈도우 + 스프라이트 밀어넣기. WinForms 전용 |
| `Program.cs` | 검증 시나리오와 리포트 |
