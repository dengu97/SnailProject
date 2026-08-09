# 데이터 파이프라인

`SnailData.xlsx` → 검증 → 생성 코드 + 이식 가능한 JSON.

```bash
dotnet run --project Tools/DataPipeline                 # 검증 후 생성
dotnet run --project Tools/DataPipeline -- --check      # 검증만 (파일 안 씀)
dotnet run --project Tools/DataPipeline -- --root <경로> # 다른 트리 대상 (CI/테스트)
```

Excel 이 파일을 열어둔 상태에서도 동작한다 (임시 복사본을 읽는다).

## 생성물

| 경로 | 용도 |
|---|---|
| `SnailPet/Assets/Scripts/Generated/GameData.g.cs` | Unity 클라이언트. 데이터가 코드에 그대로 박혀 런타임 파싱이 없다 |
| `Data/gamedata.json` | 서버·툴용 이식 사본 |
| `Tools/DataPipeline/IdMap.json` | `[토큰]` → 정수 ID 매핑. **반드시 커밋한다** |

## 빌드 게이트

**오류가 하나라도 있으면 파일을 하나도 쓰지 않고 종료 코드 2 로 끝난다.**
깨진 데이터가 코드로 굳는 것을 막기 위해서다. 경고는 생성을 막지 않는다.

프리뷰 툴(`Tools/preview`)이 브라우저에서 하던 검사를 여기로 옮겨왔다. 검사 항목:

- 2행 타입 문자열 해석 가능 여부, `Enum<X>` 가 `EnumData` 에 실재하는지
- 값이 선언된 타입과 맞는지 (정수/실수/불리언/날짜/enum/리스트)
- `EnumValue` 중복 — 직렬화 신원이라 겹치면 안 된다
- 시트 내 `Id` 중복
- 시트 간 참조 무결성 — `ShopData`/`GachaData`/`EventData`/`FoodData` 가
  삭제된 항목을 가리키는지, `EggData.PartsGroupIds` 가 실재하는 그룹인지
- `ResourceKey` 가 가리키는 파일이 `SnailPet/Assets/Resources/Snail/` 에 있는지

같은 원인이 수십 행에서 터지면 한 줄로 접어 보여준다 (예: `24건`).

## `[토큰]` → ID 변환

데이터는 `[상추]`, `[일반그룹]` 같은 토큰으로 서로를 참조하는데 타입은 `Int` 로 선언돼 있다.
파이프라인이 토큰을 정수로 바꾸고 그 매핑을 `IdMap.json` 에 남긴다.

**이 번호는 세이브 데이터와 묶이는 직렬화 신원이다.** 한번 부여한 번호는 고정이고,
새 토큰만 뒤에 붙는다. 재배치하거나 삭제하면 기존 유저 데이터가 엉뚱한 아이템을 가리킨다.
`EnumValue` 를 노출 순서로 재활용하면 안 되는 것과 같은 이유다.

그래서 `IdMap.json` 은 생성물이지만 **커밋 대상**이다. 지우고 다시 만들면 번호가 바뀐다.

## 타입 표기

2행에 쓰는 문자열. 대소문자는 구분하지 않는다.

| 표기 | C# | 비고 |
|---|---|---|
| `int` | `int` | `[토큰]` 이면 ID 로 변환 |
| `nullableint` | `int?` | 빈 칸은 `null` |
| `double` | `double` | |
| `bool` | `bool` | `0` / `1` |
| `string`, `nullablestring` | `string` | |
| `DateTime` | `System.DateTime` | `2026년 7월 29일 06:00:00` 형식과 ISO 를 모두 받는다 |
| `list<int>` | `int[]` | 쉼표 구분. 항목이 `[토큰]` 이면 ID 로 변환 |
| `list<string>` | `string[]` | 리소스 키 목록처럼 토큰이 아닌 문자열용 |
| `Enum<RarityType>` | `RarityType` | `EnumData` 에 정의된 이름이어야 한다 |

`#` 로 시작하는 열(`#분류` 등)은 주석으로 보고 버린다.
같은 이름의 열이 두 번 나오면 뒤쪽에 숫자를 붙인다 (`Id`, `Id2`).

## 생성 코드 쓰는 법

```csharp
using SnailPet.Data;

foreach (var p in GameData.PartsData)
    if (p.PartsType == PartsType.Shell) { /* ... */ }

var lettuce = GameData.FoodDataById[GameData.IdByToken["[상추]"]];
Debug.Log(lettuce.Name + " " + lettuce.FullPoint);

// 로그에 토큰 이름을 되살릴 때
Debug.Log(GameData.TokenById[p.Id]);
```

`Id` 가 행마다 유일한 테이블에는 `XxxById` 딕셔너리가 함께 생성된다.
`GachaData` 처럼 같은 `Id` 가 여러 행에 걸치는 시트는 배열만 나온다.
