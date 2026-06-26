param(
    [string]$CsvPath = "Assets/Data/StructDefinition.csv",
    [string]$OutputPath = "Assets/Data/StructDefinition.generated.txt"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$resolvedCsv = Join-Path $root $CsvPath
$resolvedOutput = Join-Path $root $OutputPath

if (-not (Test-Path -LiteralPath $resolvedCsv)) {
    throw "CSV not found: $resolvedCsv"
}

$rows = Import-Csv -LiteralPath $resolvedCsv -Encoding UTF8
$requiredColumns = @(
    "건물 이름",
    "출력 이름",
    "해금 년도",
    "자금생산량",
    "인구수 증가량",
    "기술력 증가량",
    "사랑 증가량",
    "편의성 증가량",
    "이미지 링크",
    "부연설명",
    "설립연도"
)

$header = @()
if ($rows.Count -gt 0) {
    $header = $rows[0].PSObject.Properties.Name
} else {
    $headerLine = Get-Content -LiteralPath $resolvedCsv -TotalCount 1 -Encoding UTF8
    if ($headerLine) {
        $header = $headerLine -split ","
    }
}

foreach ($column in $requiredColumns) {
    if ($header -notcontains $column) {
        throw "Missing required column: $column"
    }
}

$numericColumns = @("해금 년도", "자금생산량", "인구수 증가량", "기술력 증가량", "사랑 증가량", "편의성 증가량")
$errors = New-Object System.Collections.Generic.List[string]
$rowIndex = 1

foreach ($row in $rows) {
    $rowIndex++
    if ([string]::IsNullOrWhiteSpace($row."건물 이름")) {
        $errors.Add("Line ${rowIndex}: 건물 이름 is empty")
    }
    if ([string]::IsNullOrWhiteSpace($row."출력 이름")) {
        $errors.Add("Line ${rowIndex}: 출력 이름 is empty")
    }
    if ([string]::IsNullOrWhiteSpace($row."부연설명")) {
        $errors.Add("Line ${rowIndex}: 부연설명 is empty")
    }
    if ([string]::IsNullOrWhiteSpace($row."설립연도")) {
        $errors.Add("Line ${rowIndex}: 설립연도 is empty")
    }
    foreach ($column in $numericColumns) {
        $value = $row.$column
        $parsed = 0
        if (-not [int]::TryParse($value, [ref]$parsed)) {
            $errors.Add("Line ${rowIndex}: $column must be an integer, value='$value'")
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($row."이미지 링크")) {
        $imagePath = Join-Path $root $row."이미지 링크"
        if (-not (Test-Path -LiteralPath $imagePath)) {
            $errors.Add("Line ${rowIndex}: image not found: $($row."이미지 링크")")
        }
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    throw "StructDefinition.csv validation failed with $($errors.Count) error(s)."
}

$summary = @()
$summary += "# Generated from Assets/Data/StructDefinition.csv"
$summary += "Rows=$($rows.Count)"
foreach ($row in $rows) {
    $summary += "$($row."건물 이름")|Display=$($row."출력 이름")|Unlock=$($row."해금 년도")|Money=$($row."자금생산량")|People=$($row."인구수 증가량")|Science=$($row."기술력 증가량")|Love=$($row."사랑 증가량")|Convenience=$($row."편의성 증가량")|Image=$($row."이미지 링크")|Description=$($row."부연설명")|StartYear=$($row."설립연도")"
}

$summary | Set-Content -LiteralPath $resolvedOutput -Encoding UTF8
Write-Host "[generateData] OK rows=$($rows.Count)"
Write-Host "[generateData] Wrote $resolvedOutput"
