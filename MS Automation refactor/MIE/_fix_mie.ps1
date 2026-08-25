$ErrorActionPreference = "Stop"
$enc = New-Object System.Text.UTF8Encoding $false
$Root = "c:\Users\Kreatx\source\repos\Test-Automation-\MIE"
$brokenFrag = 'By.XPath("                Log("Kliko Dergo Button");'

Get-ChildItem $Root -Recurse -Filter "*.cs" | Where-Object { $_.Name -notlike "_*" } | ForEach-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName, $enc)
    $orig = $t

    $t = $t.Replace(
        "        Log(`"Prit 1 minutë para ngarkimit të dokumentit të saktë…`");`r`n        Thread.Sleep(TimeSpan.FromMinutes(1));",
        "                Log(`"Prit 1 minutë para ngarkimit të dokumentit të saktë…`");`r`n                Thread.Sleep(TimeSpan.FromMinutes(1));"
    )

    while ($t.Contains($brokenFrag)) {
        $idx = $t.IndexOf($brokenFrag)
        $lineStart = $t.LastIndexOf("`n", $idx) + 1
        $nl = $t.IndexOf("`n", $idx)
        $lineEnd = if ($nl -lt 0) { $t.Length } else { $nl + 1 }
        $line = $t.Substring($lineStart, [Math]::Min($lineEnd, $t.Length) - $lineStart)

        $before = $t.Substring(0, $lineStart)
        $marker = 'Log("Click ' + "'Dergo'" + ' button");'
        $search = $before.LastIndexOf($marker)
        if ($line.TrimStart().StartsWith("//") -or $search -lt 0) {
            $t = $t.Substring(0, $lineStart) + $t.Substring($lineEnd)
        } else {
            $cutStart = $t.LastIndexOf("`n", $search) + 1
            $t = $t.Substring(0, $cutStart) + $t.Substring($lineEnd)
        }
    }

    if ($t -ne $orig) {
        [System.IO.File]::WriteAllText($_.FullName, $t, $enc)
        Write-Host "fixed $($_.FullName.Substring($Root.Length+1))"
    }
}
Write-Host "fix done"
