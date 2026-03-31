$txtPath = "d:\Projeler\YLCDORSE\YALCINDORSE\cariler.txt"
$sqlPath = "d:\Projeler\YLCDORSE\YALCINDORSE\TumCariler_SQL_Sorgusu.sql"

if (!(Test-Path $txtPath)) {
    Write-Host "Hata: 'cariler.txt' dosyasi bulunamadi!" -ForegroundColor Red
    Write-Host "Lutfen chatteki o uzun listeyi kopyalayip bu klasore 'cariler.txt' adinda kaydedin." -ForegroundColor Yellow
    Read-Host "Cikmak icin Enter"
    exit
}

$lines = Get-Content -Path $txtPath -Encoding UTF8

$out = "INSERT INTO `"YLCustomers`" (`"CustomerCode`", `"Title`", `"TaxNumber`", `"TaxOffice`", `"Country`", `"Address`", `"RiskLimit`", `"CurrentBalance`", `"IsActive`") VALUES`n"

$values = @()

foreach ($line in $lines) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    
    $parts = $line.Split("`t")
    if ($parts.Length -ge 9) {
        $code = $parts[1].Trim('"')
        if ([string]::IsNullOrWhiteSpace($code)) { continue }
        
        $title = $parts[2].Trim('"').Replace("'", "''")
        
        $taxnum = if (-not [string]::IsNullOrWhiteSpace($parts[3])) { "'$($parts[3].Trim('"'))'" } else { "NULL" }
        $taxoff = if (-not [string]::IsNullOrWhiteSpace($parts[4])) { "'$($parts[4].Trim('"'))'" } else { "NULL" }
        $country = if (-not [string]::IsNullOrWhiteSpace($parts[5])) { "'$($parts[5].Trim('"'))'" } else { "'TR'" }
        $addr = if (-not [string]::IsNullOrWhiteSpace($parts[6])) { "'$($parts[6].Trim('"'))'" } else { "NULL" }
        
        $rlimitStr = $parts[7].Trim()
        $rlimit = if (-not [string]::IsNullOrWhiteSpace($rlimitStr)) { $rlimitStr } else { "0" }
        
        $currStr = $parts[8].Trim()
        $curr = if (-not [string]::IsNullOrWhiteSpace($currStr)) { $currStr } else { "0" }
        
        $values += "('$code', '$title', $taxnum, $taxoff, $country, $addr, $rlimit, $curr, true)"
    }
}

if ($values.Count -gt 0) {
    $finalSql = $out + ($values -join ",`n") + ";"
    Set-Content -Path $sqlPath -Value $finalSql -Encoding UTF8
    Write-Host ">> Islem tamamlandi!" -ForegroundColor Green
    Write-Host ">> Olusturulan Dosya: $sqlPath" -ForegroundColor Cyan
    Write-Host ">> Toplam $($values.Count) adet SQL kaydi eklendi." -ForegroundColor Yellow
} else {
    Write-Host "Veri bulunamadi veya formati uygun degil (Tab ile ayrilmis olmali)." -ForegroundColor Red
}

Read-Host "`nKapatmak icin Enter'a basin..."
