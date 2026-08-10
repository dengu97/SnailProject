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

$live = Get-Process | Where-Object { $_.ProcessName -eq 'Unity' }
if ($live) {
    Write-Output "FAIL: 유니티 에디터가 열려 있습니다 (PID $($live.Id -join ', ')). 닫고 다시 실행하세요."
    exit 2
}

$dll = Join-Path $ProjectPath "Build\SnailPet_Data\Managed\Assembly-CSharp.dll"
$before = if (Test-Path $dll) { (Get-Item $dll).LastWriteTime } else { [datetime]::MinValue }

$log = & $Unity -batchmode -quit -projectPath $ProjectPath -executeMethod $Method -logFile - 2>&1

$errors = $log | Select-String -Pattern "error CS|Aborting batchmode|cannot open the same project"
if ($errors) {
    Write-Output "FAIL: 빌드 실패"
    $errors | Select-Object -First 10 | ForEach-Object { "  $_" }
    exit 1
}

if (-not (Test-Path $dll)) { Write-Output "FAIL: 산출물이 없습니다: $dll"; exit 1 }

$after = (Get-Item $dll).LastWriteTime
if ($after -le $before) {
    Write-Output "FAIL: 빌드가 돌지 않았습니다. Assembly-CSharp.dll 이 그대로입니다 ($after)."
    exit 1
}

Write-Output "OK: 빌드 완료 ($after)"
