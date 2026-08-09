<#
  SnailData.xlsx -> Tools/preview/data.js

  Excel 이 파일을 잠그고 있어도 되도록 임시 복사본을 읽는다.
  3행 헤더 규약: 1행=대상 / 2행=타입 / 3행=변수명. 4행부터 데이터.
  Resource 폴더의 실제 png 목록도 함께 내보내어 클라이언트가 누락을 검증할 수 있게 한다.
#>
param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutFile
)

$ErrorActionPreference = 'Stop'
if (-not $OutFile) { $OutFile = Join-Path $PSScriptRoot 'preview\data.js' }

$xlsxPath    = Join-Path $ProjectRoot 'SnailData.xlsx'
$resourceDir = Join-Path $ProjectRoot 'Resource'

if (-not (Test-Path $xlsxPath))    { throw "SnailData.xlsx 를 찾을 수 없습니다: $xlsxPath" }
if (-not (Test-Path $resourceDir)) { throw "Resource 폴더를 찾을 수 없습니다: $resourceDir" }

Add-Type -AssemblyName System.IO.Compression.FileSystem

# Excel 이 원본을 잠근 상태여도 읽을 수 있도록 복사본을 사용한다.
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("snaildata_" + [guid]::NewGuid().ToString('N') + ".xlsx")
Copy-Item $xlsxPath $work -Force

function Get-EntryText($zip, $name) {
    $e = $zip.Entries | Where-Object { $_.FullName -eq $name }
    if (-not $e) { return $null }
    $sr = New-Object System.IO.StreamReader($e.Open(), [System.Text.Encoding]::UTF8)
    $t = $sr.ReadToEnd()
    $sr.Close()
    return $t
}

function Get-NodeText($node) {
    if ($null -eq $node) { return $null }
    if ($node -is [string]) { return $node }
    return $node.'#text'
}

# "A12" -> 열 인덱스(0-based)
function Get-ColIndex([string]$ref) {
    $letters = ($ref -replace '[0-9]', '')
    $n = 0
    foreach ($ch in $letters.ToCharArray()) {
        $n = $n * 26 + ([int][char]$ch - 64)
    }
    return $n - 1
}

$zip = [System.IO.Compression.ZipFile]::OpenRead($work)
try {
    # --- 공유 문자열 테이블 ---
    $shared = New-Object System.Collections.ArrayList
    $ssText = Get-EntryText $zip 'xl/sharedStrings.xml'
    if ($ssText) {
        $ssXml = [xml]$ssText
        foreach ($si in $ssXml.sst.si) {
            $sb = New-Object System.Text.StringBuilder
            if ($si.t) { [void]$sb.Append((Get-NodeText $si.t)) }
            if ($si.r) { foreach ($r in $si.r) { [void]$sb.Append((Get-NodeText $r.t)) } }
            [void]$shared.Add($sb.ToString())
        }
    }

    # --- 시트 목록 (workbook.xml + rels) ---
    $wbXml  = [xml](Get-EntryText $zip 'xl/workbook.xml')
    $relXml = [xml](Get-EntryText $zip 'xl/_rels/workbook.xml.rels')
    $relMap = @{}
    foreach ($r in $relXml.Relationships.Relationship) { $relMap[$r.Id] = $r.Target }

    $sheets = @()
    foreach ($s in $wbXml.workbook.sheets.sheet) {
        $rid = $s.id
        if (-not $rid) { $rid = $s.GetAttribute('r:id') }
        $target = $relMap[$rid]
        if ($target -notlike 'xl/*') { $target = 'xl/' + $target.TrimStart('/') }
        $sheets += [PSCustomObject]@{ Name = $s.name; Target = $target }
    }

    $tables = [ordered]@{}

    foreach ($sh in $sheets) {
        $sxText = Get-EntryText $zip $sh.Target
        if (-not $sxText) { continue }
        $sx = [xml]$sxText

        # 행 번호 -> (열인덱스 -> 값) 희소 테이블로 먼저 훑는다.
        $grid = @{}
        $maxCol = 0
        foreach ($row in $sx.worksheet.sheetData.row) {
            $rn = [int]$row.r
            $cells = @{}
            foreach ($c in $row.c) {
                $ci = Get-ColIndex $c.r
                $t  = $c.t
                $v  = $null
                if ($t -eq 'inlineStr') {
                    $v = Get-NodeText $c.is.t
                } elseif ($c.v) {
                    $raw = Get-NodeText $c.v
                    if ($t -eq 's') { $v = $shared[[int]$raw] } else { $v = $raw }
                }
                if ($null -ne $v -and "$v".Trim() -ne '') {
                    $cells[$ci] = "$v".Trim()
                    if ($ci -gt $maxCol) { $maxCol = $ci }
                }
            }
            if ($cells.Count -gt 0) { $grid[$rn] = $cells }
        }

        if (-not $grid.ContainsKey(3)) { continue }   # 3행 헤더가 없으면 데이터 시트가 아니다

        $typeRow   = if ($grid.ContainsKey(2)) { $grid[2] } else { @{} }
        $headerRow = $grid[3]

        # 헤더 이름 확정 (중복이면 _2, _3 … 을 붙여 키 충돌을 막는다)
        $headers = @{}
        $seen    = @{}
        for ($ci = 0; $ci -le $maxCol; $ci++) {
            if (-not $headerRow.ContainsKey($ci)) { continue }
            $name = $headerRow[$ci]
            if ($name.StartsWith('#')) { continue }   # #분류 같은 주석 열은 버린다
            if ($seen.ContainsKey($name)) {
                $seen[$name] = $seen[$name] + 1
                $name = "$name`_$($seen[$name])"
            } else {
                $seen[$name] = 1
            }
            $headers[$ci] = $name
        }

        $types = [ordered]@{}
        foreach ($ci in ($headers.Keys | Sort-Object)) {
            $types[$headers[$ci]] = if ($typeRow.ContainsKey($ci)) { $typeRow[$ci] } else { '' }
        }

        $rows = New-Object System.Collections.ArrayList
        foreach ($rn in ($grid.Keys | Where-Object { $_ -ge 4 } | Sort-Object)) {
            $cells = $grid[$rn]
            $obj = [ordered]@{ '_row' = $rn }
            $any = $false
            foreach ($ci in ($headers.Keys | Sort-Object)) {
                if ($cells.ContainsKey($ci)) {
                    $obj[$headers[$ci]] = $cells[$ci]
                    $any = $true
                } else {
                    $obj[$headers[$ci]] = $null
                }
            }
            if ($any) { [void]$rows.Add($obj) }
        }

        $tables[$sh.Name] = [ordered]@{
            types = $types
            rows  = $rows
        }
    }
}
finally {
    $zip.Dispose()
    Remove-Item $work -Force -ErrorAction SilentlyContinue
}

# --- 실제 리소스 파일 목록 ---
$rootLen = $ProjectRoot.TrimEnd('\').Length + 1
$files = New-Object System.Collections.ArrayList
foreach ($f in (Get-ChildItem $resourceDir -Recurse -Filter *.png)) {
    [void]$files.Add($f.FullName.Substring($rootLen).Replace('\', '/'))
}

$payload = [ordered]@{
    generatedAt = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    source      = 'SnailData.xlsx'
    tables      = $tables
    resources   = $files
}

# ConvertTo-Json 은 비ASCII를 \uXXXX 로 이스케이프하므로 인코딩 사고가 없다.
$json = $payload | ConvertTo-Json -Depth 12 -Compress
$js   = "// 자동 생성됨 - 직접 수정하지 말 것. Tools/ExportSnailData.ps1 을 다시 실행하세요.`r`nwindow.SNAIL_DATA = $json;`r`n"

$outDir = Split-Path -Parent $OutFile
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }
[System.IO.File]::WriteAllText($OutFile, $js, (New-Object System.Text.UTF8Encoding($false)))

Write-Output "생성 완료: $OutFile"
Write-Output ("  시트 {0}개 / 리소스 {1}개" -f $tables.Count, $files.Count)
foreach ($k in $tables.Keys) {
    Write-Output ("    {0,-18} {1} rows" -f $k, $tables[$k].rows.Count)
}
