<#
.SYNOPSIS
  UI 프리팹 두 판의 좌표를 계층 경로로 견준다. 다시 굽기 전에 무엇이 사라지는지 보는 용도다.

.DESCRIPTION
  프리팹을 다시 구우면 fileID 가 전부 새로 생긴다. 그래서 텍스트 diff 는 7만 줄이 통째로
  바뀐 것으로 나오고, 정작 알고 싶은 「무엇이 몇 픽셀 움직였나」는 묻힌다.
  여기서는 오브젝트를 이름 경로로 맞춰 RectTransform 의 위치·크기만 견준다.

  쓰는 때: 프리팹에서 손으로 옮긴 배치는 다시 구우면 코드 좌표로 되돌아간다.
  git status 가 깨끗한 것은 근거가 되지 못한다 — 손 편집은 이미 커밋 안에 들어 있다.
  굽기 전에 이걸 돌려 차이를 뽑고, 그 값을 UiTheme 상수로 옮긴 다음 구울 것.
  (2026-08-13 에 이 순서를 건너뛰어 탭·설정·코인·하단 액션 10개를 날린 적이 있다.)

  좌표를 UiTheme 으로 되돌릴 때의 변환:
    Place        pos = ( x,       -y )
    PlaceCentered pos = ( x+w/2, -(y+h/2) )   ← Icon() 이 쓰는 것
    Above(r)     은 위 계산 전에 y 에서 At.Coin.y(-31) 를 뺀다
  RectInt 이라 소수점은 반올림되어 0.5px 이하 차이가 남는다.

.EXAMPLE
  Tools\Diff-PrefabLayout.ps1
  커밋된 프리팹과 지금 작업 폴더의 프리팹을 견준다.

.EXAMPLE
  Tools\Diff-PrefabLayout.ps1 -Old C:\temp\before.prefab
  따로 떠 둔 판과 견준다.
#>
param(
    [string]$Old,
    [string]$New,
    [string]$Repo = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = "Stop"

$relative = "SnailPet\Assets\Resources\Ui\SnailUi.prefab"
if (-not $New) { $New = Join-Path $Repo $relative }

# 기준 판을 안 주면 커밋된 것을 꺼내 쓴다. 다시 구운 직후라면 이쪽에 예전 배치가 들어 있다.
$temp = $null
if (-not $Old) {
    $temp = Join-Path $env:TEMP ("SnailUi-HEAD-" + [Guid]::NewGuid().ToString("N") + ".prefab")
    & git -C $Repo show ("HEAD:" + ($relative -replace '\\', '/')) | Set-Content -LiteralPath $temp -Encoding UTF8
    if ($LASTEXITCODE -ne 0) { Write-Output "FAIL: 커밋된 프리팹을 꺼내지 못했습니다."; exit 1 }
    $Old = $temp
}

# 견줄 값. RectTransform 에서 자리와 크기를 정하는 것 전부다.
$watched = @('m_LocalRotation','m_LocalPosition','m_LocalScale','m_AnchorMin','m_AnchorMax',
             'm_AnchoredPosition','m_SizeDelta','m_Pivot')

# 프리팹 하나를 읽어 「경로 -> 값들」 표로 만든다.
function Read-Layout([string]$path) {
    $names   = @{}    # GameObject id -> 이름
    $goOfRt  = @{}    # RectTransform id -> GameObject id
    $father  = @{}    # RectTransform id -> 부모 RectTransform id
    $fields  = @{}    # RectTransform id -> 값 이름 -> 값
    $children = @{}   # 부모 id -> 자식 id 순서대로. 이름이 같은 형제를 가르는 데 쓴다.

    $type = $null; $id = $null; $inChildren = $false

    foreach ($line in [System.IO.File]::ReadLines($path)) {
        if ($line.StartsWith('--- !u!')) {
            $m = [regex]::Match($line, '^--- !u!(\d+) &(\d+)')
            $type = $m.Groups[1].Value; $id = $m.Groups[2].Value
            $inChildren = $false
            continue
        }
        if ($type -eq '1') {
            if ($line.StartsWith('  m_Name: ')) { $names[$id] = $line.Substring(10).Trim() }
            continue
        }
        if ($type -ne '224') { continue }      # 224 = RectTransform

        if ($line.StartsWith('  m_Children:')) {
            $inChildren = $true
            $children[$id] = New-Object System.Collections.ArrayList
            continue
        }
        if ($inChildren) {
            if ($line.StartsWith('  - {fileID: ')) {
                $null = $children[$id].Add(($line -replace '.*fileID: (\d+).*', '$1'))
                continue
            }
            $inChildren = $false
        }
        if ($line.StartsWith('  m_GameObject: ')) { $goOfRt[$id] = ($line -replace '.*fileID: (\d+).*', '$1'); continue }
        if ($line.StartsWith('  m_Father: '))     { $father[$id] = ($line -replace '.*fileID: (\d+).*', '$1'); continue }

        # 자리를 정하는 값은 전부 본다. 위치와 크기만 보면 안 된다 —
        # 크기를 배율로 줄인 것(초상 0.7배)은 위치가 그대로라 눈치채지 못하고 날린 적이 있다.
        foreach ($k in $watched) {
            if ($line.StartsWith("  $k`: ")) {
                if (-not $fields.ContainsKey($id)) { $fields[$id] = @{} }
                $fields[$id][$k] = $line.Substring($k.Length + 4).Trim()
                break
            }
        }
    }

    $pathOf = @{}
    function Get-Path($rt) {
        if ($pathOf.ContainsKey($rt)) { return $pathOf[$rt] }

        $name = if ($goOfRt.ContainsKey($rt) -and $names.ContainsKey($goOfRt[$rt])) { $names[$goOfRt[$rt]] } else { '?' }
        $f = $father[$rt]
        if ($null -eq $f -or $f -eq '0' -or -not $father.ContainsKey($rt)) {
            $pathOf[$rt] = $name
            return $name
        }

        # 이름이 같은 형제(Text 가 여럿)는 부모의 자식 순서로 번호를 붙여 가른다
        $idx = 0
        if ($children.ContainsKey($f)) {
            $seen = 0
            foreach ($c in $children[$f]) {
                $cname = if ($goOfRt.ContainsKey($c) -and $names.ContainsKey($goOfRt[$c])) { $names[$goOfRt[$c]] } else { '?' }
                if ($cname -eq $name) {
                    if ($c -eq $rt) { $idx = $seen; break }
                    $seen++
                }
            }
        }

        $p = (Get-Path $f) + "/$name#$idx"
        $pathOf[$rt] = $p
        return $p
    }

    $out = @{}
    foreach ($rt in $goOfRt.Keys) {
        $flat = @()
        foreach ($k in $watched) {
            if ($fields.ContainsKey($rt) -and $fields[$rt].ContainsKey($k)) { $flat += "$k=$($fields[$rt][$k])" }
        }
        $out[(Get-Path $rt)] = ($flat -join '  ')
    }
    return $out
}

# "m_Pivot={x: 0, y: 1}  m_SizeDelta=..." 를 다시 값 이름별로 가른다
function Split-Fields([string]$flat) {
    $h = @{}
    foreach ($t in ($flat -split '  ')) { if ($t -match '^(\w+)=(.*)$') { $h[$matches[1]] = $matches[2] } }
    return $h
}

$a = Read-Layout $Old
$b = Read-Layout $New
if ($temp) { Remove-Item -LiteralPath $temp -Force }

$diff = New-Object System.Collections.ArrayList
foreach ($k in $a.Keys) {
    if (-not $b.ContainsKey($k)) { $null = $diff.Add("사라짐 $k"); continue }
    if ($a[$k] -eq $b[$k]) { continue }

    # 달라진 값만 적는다. 전부 늘어놓으면 정작 뭐가 바뀌었는지 안 보인다.
    # 이름을 $old / $new 로 하면 [string] 인 파라미터 $Old / $New 와 같은 변수가 되어
    # 해시테이블이 문자열로 바뀐다 (PowerShell 은 변수 이름의 대소문자를 안 가린다)
    $before = Split-Fields $a[$k]
    $after  = Split-Fields $b[$k]
    $text = "움직임 $k"
    foreach ($f in $watched) {
        if ($before.ContainsKey($f) -and $before[$f] -ne $after[$f]) {
            $text += "`n    $f`n      예전: $($before[$f])`n      지금: $($after[$f])"
        }
    }
    $null = $diff.Add($text)
}
foreach ($k in $b.Keys) { if (-not $a.ContainsKey($k)) { $null = $diff.Add("생김   $k") } }

"예전 $($a.Count)개 · 지금 $($b.Count)개 오브젝트"
"차이 $($diff.Count)개"
if ($diff.Count -gt 0) { "" }
$diff | Sort-Object | ForEach-Object { $_ }
