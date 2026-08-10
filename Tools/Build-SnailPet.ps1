<#
.SYNOPSIS
  SnailPet 플레이어를 batchmode 로 빌드하고, 산출물이 실제로 갱신됐는지까지 확인한다.

.DESCRIPTION
  에디터가 열려 있으면 batchmode 는 프로젝트 락에 막혀 중단되는데,
  이때 로그에 "error CS" 가 안 나온다. 컴파일 에러만 보고 있으면
  <b>빌드가 안 됐는데 성공으로 읽는다</b>. 실제로 그렇게 두 번 속았다.

  그래서 성공 판정을 로그가 아니라 Assembly-CSharp.dll 의 수정 시각으로 한다.
  빌드 전 시각보다 새로워야만 성공이다.
#>
param(
    [string]$ProjectPath = "C:\Users\tjwog\Desktop\SnailProject\SnailPet",
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\2022.3.13f1\Editor\Unity.exe",
    [string]$Method = "SnailPet.EditorTools.SnailPetSetup.BuildOnly"
)

$ErrorActionPreference = "Stop"

# 끝난 batchmode 유니티도 잠깐 프로세스로 남아 다음 실행을 막는다. 잠시 기다려 준다.
for ($i = 0; $i -lt 20; $i++) {
    $live = Get-Process | Where-Object { $_.ProcessName -eq 'Unity' }
    if (-not $live) { break }
    if ($i -eq 0) { Write-Output "유니티 프로세스가 있어 기다리는 중... (PID $($live.Id -join ', '))" }
    Start-Sleep -Seconds 3
}
$live = Get-Process | Where-Object { $_.ProcessName -eq 'Unity' }
if ($live) {
    Write-Output "FAIL: 유니티 에디터가 열려 있습니다 (PID $($live.Id -join ', ')). 닫고 다시 실행하세요."
    exit 2
}

$dll = Join-Path $ProjectPath "Build\SnailPet_Data\Managed\Assembly-CSharp.dll"

# 에셋을 추가하거나 바꾼 뒤 첫 실행은 임포트만 하고 끝난다. 두 번까지 돌려 본다.
$log = $null
for ($try = 1; $try -le 2; $try++) {
    $log = & $Unity -batchmode -quit -projectPath $ProjectPath -executeMethod $Method -logFile - 2>&1

    $errors = $log | Select-String -Pattern "error CS|Aborting batchmode|cannot open the same project"
    if ($errors) {
        Write-Output "FAIL: 빌드 실패"
        $errors | Select-Object -First 10 | ForEach-Object { "  $_" }
        exit 1
    }

    if (Test-Path $dll) {
        $out = Get-ChildItem (Join-Path $ProjectPath "Build\SnailPet_Data") -Recurse -File |
               Sort-Object LastWriteTime -Descending | Select-Object -First 1
        $src = Get-ChildItem (Join-Path $ProjectPath "Assets\Scripts") -Recurse -Include *.cs |
               Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if (-not $src -or -not $out -or $src.LastWriteTime -le $out.LastWriteTime) { break }
    }

    if ($try -eq 1) { Write-Output "첫 실행이 임포트만 하고 끝났습니다. 다시 빌드합니다." }
    while (Get-Process | Where-Object { $_.ProcessName -eq 'Unity' }) { Start-Sleep -Seconds 2 }
}

if (-not (Test-Path $dll)) { Write-Output "FAIL: 산출물이 없습니다: $dll"; exit 1 }

# 판정 기준으로 Assembly-CSharp.dll 만 보면 안 된다.
#  · 바뀐 게 없으면 유니티가 DLL 을 다시 쓰지 않아 멀쩡한 빌드를 실패로 읽는다
#  · 에디터 스크립트(임포트 설정 등)만 바뀌면 DLL 은 그대로인데 산출물은 달라진다
# 그래서 「빌드 폴더에서 가장 새 파일」과 「소스에서 가장 새 파일」을 견준다.
$newestOut = Get-ChildItem (Join-Path $ProjectPath "Build\SnailPet_Data") -Recurse -File |
             Sort-Object LastWriteTime -Descending | Select-Object -First 1
# Assets\Editor 는 제외한다. 에디터 스크립트는 플레이어에 안 들어가므로 그것만 바뀌면
# 산출물이 그대로인 게 정상인데, 포함시키면 영원히 「오래됐다」로 나온다.
# (임포트 설정을 바꾼 경우에는 해당 에셋의 .meta 를 지우고 다시 빌드해야 반영된다.)
$newestSrc = Get-ChildItem (Join-Path $ProjectPath "Assets\Scripts") -Recurse -Include *.cs |
             Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($newestSrc -and $newestOut -and $newestSrc.LastWriteTime -gt $newestOut.LastWriteTime) {
    Write-Output "FAIL: 빌드가 소스보다 오래됐습니다."
    Write-Output "  산출물  : $($newestOut.LastWriteTime)  $($newestOut.Name)"
    Write-Output "  최신 소스: $($newestSrc.LastWriteTime)  $($newestSrc.Name)"
    exit 1
}

Write-Output "OK: 빌드 최신 (산출물 $($newestOut.LastWriteTime))"
