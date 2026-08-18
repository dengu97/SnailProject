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

function Newest([string]$path) {
    if (-not (Test-Path $path)) { return $null }
    Get-ChildItem $path -Recurse -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
}

# 플레이어에 들어갈 수 있는 것 전부. .cs 만 보면 안 된다 —
# 프리팹이나 아트만 바뀐 경우 산출물이 그대로인데 「최신」으로 읽어 옛 빌드를 검사하게 된다.
# 실제로 UI 프리팹을 다시 구운 뒤 그렇게 두 번 속았다.
# Assets\Editor 는 뺀다. 플레이어에 안 들어가므로 그것만 바뀌면 산출물이 그대로인 게 정상이고,
# 넣으면 영원히 「오래됐다」로 나온다.
function NewestSource([string]$projectPath) {
    $editor = Join-Path $projectPath "Assets\Editor"
    Get-ChildItem (Join-Path $projectPath "Assets") -Recurse -File |
        Where-Object { $_.FullName -notlike "$editor*" } |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
}

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

# 로그는 반드시 파일로 받는다.
#   -logFile - 로 표준출력에 받아 2>&1 로 캡처하면, PowerShell 5.1 이 네이티브 stderr 를
#   ErrorRecord 로 감싸는 바람에 Select-String 이 "error CS" 를 못 잡는다.
#   실제로 컴파일 에러가 난 빌드가 신선도 검사에만 걸려서, 원인을 못 보고 세 번을 헛돌렸다.
#
# 시도마다 <b>다른 파일</b>에 쓴다. 한 파일을 돌려 쓰면 두 번째 시도에서 지우다 실패한다 —
# 앞 시도의 유니티가 프로세스 목록에서 사라진 뒤에도 로그 파일 손잡이를 잠깐 더 쥐고 있다.

# 에셋을 추가하거나 바꾼 뒤 첫 실행은 임포트만 하고 끝난다. 두 번까지 돌려 본다.
for ($try = 1; $try -le 2; $try++) {
    $logPath = Join-Path $env:TEMP "SnailPet-build.$try.log"

    # 못 지워도 그냥 간다. 유니티가 열면서 어차피 비운다.
    Remove-Item $logPath -Force -ErrorAction SilentlyContinue

    & $Unity -batchmode -quit -projectPath $ProjectPath -executeMethod $Method -logFile $logPath

    # 로그를 파일로 받으면 이 호출은 <b>유니티가 끝나기 전에 돌아온다</b> — 유니티가 자기
    # 자신을 다시 띄우고 부모는 먼저 빠진다. (-logFile - 로 받던 때는 파이프를 끝까지 읽느라
    # 저절로 기다려졌다.) 그래서 여기서 기다리지 않으면 반쯤 쓰인 로그를 읽고 에러를 못 본다.
    Start-Sleep -Seconds 1
    while (Get-Process | Where-Object { $_.ProcessName -eq 'Unity' }) { Start-Sleep -Seconds 2 }

    $errors = @()
    if (Test-Path $logPath) {
        # 같은 에러가 로그에 서너 번 되풀이되므로 줄 단위로 추린다
        $errors = @(Select-String -Path $logPath -Pattern "error CS|Aborting batchmode|cannot open the same project" |
                    ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique)
    }
    if ($errors.Count -gt 0) {
        # 첫 실행에서는 방금 더한 .cs 가 아직 안 잡혀 「타입이 없다」가 나올 수 있다.
        # 그건 진짜 오류가 아니므로 마지막 시도에서만 실패로 친다.
        if ($try -lt 2) {
            Write-Output "첫 실행에 오류가 있었습니다. 새 파일이 아직 안 잡힌 것일 수 있어 한 번 더 돌립니다."
            continue
        }

        Write-Output "FAIL: 빌드 실패"
        $errors | Select-Object -First 10 | ForEach-Object { "  $_" }
        Write-Output "  (전체 로그: $logPath)"
        exit 1
    }

    if (Test-Path $dll) {
        $out = Newest (Join-Path $ProjectPath "Build\SnailPet_Data")
        $src = NewestSource $ProjectPath
        if (-not $src -or -not $out -or $src.LastWriteTime -le $out.LastWriteTime) { break }
    }

    if ($try -eq 1) { Write-Output "첫 실행이 임포트만 하고 끝났습니다. 다시 빌드합니다." }
}

if (-not (Test-Path $dll)) { Write-Output "FAIL: 산출물이 없습니다: $dll"; exit 1 }

# 판정 기준으로 Assembly-CSharp.dll 만 보면 안 된다.
#  · 바뀐 게 없으면 유니티가 DLL 을 다시 쓰지 않아 멀쩡한 빌드를 실패로 읽는다
#  · 에디터 스크립트(임포트 설정 등)만 바뀌면 DLL 은 그대로인데 산출물은 달라진다
# 그래서 「빌드 폴더에서 가장 새 파일」과 「소스에서 가장 새 파일」을 견준다.
$newestOut = Newest (Join-Path $ProjectPath "Build\SnailPet_Data")
$newestSrc = NewestSource $ProjectPath

if ($newestSrc -and $newestOut -and $newestSrc.LastWriteTime -gt $newestOut.LastWriteTime) {
    Write-Output "FAIL: 빌드가 소스보다 오래됐습니다."
    Write-Output "  산출물  : $($newestOut.LastWriteTime)  $($newestOut.Name)"
    Write-Output "  최신 소스: $($newestSrc.LastWriteTime)  $($newestSrc.Name)"
    Write-Output "  위에 에러가 안 찍혔으면 로그를 직접 볼 것: $logPath"
    exit 1
}

Write-Output "OK: 빌드 최신 (산출물 $($newestOut.LastWriteTime))"
