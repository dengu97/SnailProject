<#
.SYNOPSIS
  빌드된 SnailPet 플레이어를 띄우고 화면을 캡처해서 투명 창이 실제로 동작했는지 판정한다.

.DESCRIPTION
  "검은 사각형이 보이면 실패" 를 눈으로 보는 대신 픽셀로 판정한다.
  플레이어 창 영역에서 순수 검정(그리고 거의 검정) 픽셀의 비율을 재는데,
  투명이 동작하면 뒤의 바탕화면·창들이 비쳐서 이 비율이 낮게 나온다.

  캡처 이미지는 저장소가 아니라 임시 폴더에 남긴다 (바탕화면이 통째로 찍히므로).

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File Tools/spike/Verify-UnityTransparency.ps1
#>
param(
    [string]$Exe = "$PSScriptRoot\..\..\SnailPet\Build\SnailPet.exe",
    [int]$WaitSeconds = 6,
    [string]$OutDir = (Join-Path $env:TEMP "snailpet-verify")
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

if (-not (Test-Path $Exe)) {
    Write-Output "빌드가 없습니다: $Exe"
    Write-Output "Unity 메뉴 SnailPet > 3. 빌드만 (실행 안 함) 을 먼저 실행하세요."
    exit 1
}

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

Write-Output "플레이어 실행: $Exe"
$proc = Start-Process -FilePath $Exe -PassThru
Start-Sleep -Seconds $WaitSeconds

# 가상 화면 전체를 캡처
$vs = [System.Windows.Forms.SystemInformation]::VirtualScreen
$bmp = New-Object System.Drawing.Bitmap($vs.Width, $vs.Height)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($vs.X, $vs.Y, 0, 0, $bmp.Size)
$g.Dispose()

$shot = Join-Path $OutDir ("capture_" + (Get-Date -Format "HHmmss") + ".png")
$bmp.Save($shot, [System.Drawing.Imaging.ImageFormat]::Png)

# 픽셀 판정: 화면 전체에서 거의 검정인 픽셀 비율
$black = 0; $total = 0
$step = 4     # 4px 간격 샘플링이면 충분하고 훨씬 빠르다
for ($y = 0; $y -lt $bmp.Height; $y += $step) {
    for ($x = 0; $x -lt $bmp.Width; $x += $step) {
        $c = $bmp.GetPixel($x, $y)
        $total++
        if ($c.R -le 8 -and $c.G -le 8 -and $c.B -le 8) { $black++ }
    }
}
$bmp.Dispose()

if (-not $proc.HasExited) {
    Write-Output "플레이어는 스스로 종료됩니다 (자동 종료 타이머)."
}

$ratio = if ($total -gt 0) { $black / $total } else { 0 }
Write-Output ""
Write-Output "가상 화면 : $($vs.Width)x$($vs.Height)"
Write-Output "검정 픽셀 : $black / $total  ($([math]::Round($ratio*100,1))%)"
Write-Output "캡처      : $shot"
Write-Output ""
if ($ratio -gt 0.5) {
    Write-Output "판정: 실패로 보임 — 화면 절반 이상이 검정입니다. 불투명한 창이 덮고 있을 가능성이 큽니다."
} else {
    Write-Output "판정: 성공으로 보임 — 뒤 화면이 비치고 있습니다."
}
Write-Output "숫자는 참고치입니다. 캡처 이미지를 직접 확인하세요."
