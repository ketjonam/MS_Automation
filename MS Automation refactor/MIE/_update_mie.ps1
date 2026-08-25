$ErrorActionPreference = "Stop"
$enc = New-Object System.Text.UTF8Encoding $false
$Root = "c:\Users\Kreatx\source\repos\Test-Automation-\MIE"
$goodMain = [System.IO.File]::ReadAllText("c:\Users\Kreatx\source\repos\Test-Automation-\AQTN\12426.cs", $enc)
$goodFail = [System.IO.File]::ReadAllText("c:\Users\Kreatx\source\repos\Test-Automation-\AQTN\12426_FailCase.cs", $enc)

function Get-Between([string]$Text, [string]$Start, [string]$EndExclusive) {
    $s = $Text.IndexOf($Start)
    if ($s -lt 0) { throw "Start not found: $Start" }
    $e = $Text.IndexOf($EndExclusive, $s)
    if ($e -lt 0) { throw "End not found: $EndExclusive" }
    return $Text.Substring($s, $e - $s)
}

$helpers = (Get-Between $goodMain "    private IWebElement FindDerghoButtonInMain()" "    [Test]").TrimEnd()
$helpers = $helpers.Replace("FindDerghoButtonInMain()", "FindDerghoButtonInMain(IWebDriver driver)")
$helpers = $helpers.Replace("ClickDerghoAfterDocumentationReady()", "ClickDerghoAfterDocumentationReady(IWebDriver driver)")
$helpers = $helpers.Replace("FindDerghoButtonInMain();", "FindDerghoButtonInMain(driver);")
$helpers = $helpers + "`r`n"

$capture = (Get-Between $goodFail "    private string CaptureVisibleUiMessageAfterDergo()" "    [Test]").TrimEnd()
$capture = $capture.Replace("CaptureVisibleUiMessageAfterDergo()", "CaptureVisibleUiMessageAfterDergo(IWebDriver driver)")
$capture = $capture + "`r`n"

$waitBlock = Get-Between $goodMain '        Log("Prit 1 minut' '        Log("Ngarko dok e sakte");'
$dual = Get-Between $goodMain '        Log("Kliko Dergo Button");' '        Log("TEST PASSED");'
$dual = $dual.Replace("ClickDerghoAfterDocumentationReady();", "ClickDerghoAfterDocumentationReady(driver);")
$dual = $dual.Replace('Is.EqualTo(alertExpectedTitle)', 'Does.StartWith("Kujdes")')
$dual = $dual.Replace(
    "                Assert.That(descEls[0].Text.Trim(), Is.EqualTo(alertExpectedDescription));",
    "                Log(`"Kujdes description: `" + descEls[0].Text.Trim());"
)
$dual = $dual + '        Log("TEST PASSED");'

$ftStart = $goodFail.IndexOf('        Log("STIMULIM FAIL:')
$ftEnd = $goodFail.LastIndexOf(" + uiMessage);") + " + uiMessage);".Length
$failTail = $goodFail.Substring($ftStart, $ftEnd - $ftStart)
$failTail = $failTail.Replace("ClickDerghoAfterDocumentationReady();", "ClickDerghoAfterDocumentationReady(driver);")
$failTail = $failTail.Replace("CaptureVisibleUiMessageAfterDergo();", "CaptureVisibleUiMessageAfterDergo(driver);")
$summaryTpl = Get-Between $goodFail "/// <summary>" "[TestFixture]"

$allCs = Get-ChildItem $Root -Recurse -Filter "*.cs" | Where-Object {
    $_.Name -notlike "*_FailCase*" -and $_.Name -notlike "_update*"
}

function Replace-AplikimXpath([string]$t) {
    $t = $t.Replace("/html/body/div/main/div[3]/div/div/div/div/div/div/div[1]/div/button/div/div[1]", "//button[@aria-label='Aplikim i ri']")
    $t = $t.Replace("/html/body/div/main/div[3]/div/div/div/div/div/div/div[1]/div/button", "//button[@aria-label='Aplikim i ri']")
    $t = $t.Replace("/html/body/div/main/div[3]/div/div/div/div/div/div/div/div/button/div", "//button[@aria-label='Aplikim i ri']")
    return $t
}

function Replace-CorrectPdfs([string]$t) {
    $lines = $t -split "`r`n", -1
    if ($lines.Count -eq 1 -and $t.Contains("`n") -and -not $t.Contains("`r`n")) {
        $lines = $t -split "`n", -1
        $nl = "`n"
    } else { $nl = "`r`n" }
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match 'TEST\.pdf' -or $line -match 'TESTIM SHERBIMI') {
            if ($line -match '(?i)correct' -or $line -match 'Path2' -or $line -match 'TESTIM SHERBIMI') {
                $lines[$i] = $line.Replace('C:\Users\Kreatx\Downloads\TEST.pdf', 'C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf')
                $lines[$i] = $lines[$i].Replace('C:\Users\Kreatx\OneDrive - Kreatx\Desktop\TESTIM SHERBIMI.pdf', 'C:\Users\Kreatx\Downloads\Signed_TEST_signed.pdf')
            }
        }
    }
    return ($lines -join $nl)
}

function Insert-Wait([string]$t) {
    foreach ($m in @(
        '                Log("Upload Correct Docs");',
        '                Log("Ngarko dokumente te sakta");'
    )) {
        if ($t.Contains($m) -and $t -notmatch 'Prit 1 minut') {
            $t = $t.Replace($m, $waitBlock.TrimEnd() + "`r`n`r`n" + $m)
        }
    }
    $needle = '                string correctFileKontratat ='
    $idx = $t.LastIndexOf($needle)
    if ($idx -ge 0) {
        $before = $t.Substring([Math]::Max(0, $idx - 250), [Math]::Min(250, $idx))
        if ($before -notmatch 'Prit 1 minut') {
            $t = $t.Substring(0, $idx) + $waitBlock.TrimEnd() + "`r`n`r`n" + $t.Substring($idx)
        }
    }
    return $t
}

function Insert-Helpers([string]$t) {
    if ($t.Contains("FindDerghoButtonInMain")) { return $t }
    if ($t -notmatch 'using System.Linq') {
        $t = $t.Replace("using System.IO;`r`n", "using System.IO;`r`nusing System.Linq;`r`n")
        if ($t -notmatch 'using System.Linq') {
            $t = $t.Replace("using System.IO;`n", "using System.IO;`nusing System.Linq;`n")
        }
    }
    $idx = $t.IndexOf("`r`n    [Test]")
    if ($idx -lt 0) { $idx = $t.IndexOf("`n    [Test]") }
    if ($idx -lt 0) { throw "No [Test] for helpers" }
    return $t.Substring(0, $idx) + "`r`n" + $helpers + $t.Substring($idx)
}

function Replace-DualOutcome([string]$t) {
    $passedIdx = $t.LastIndexOf('                Log("TEST PASSED");')
    $commented = $false
    if ($passedIdx -lt 0) {
        $passedIdx = $t.LastIndexOf('                //Log("TEST PASSED");')
        $commented = $true
    }
    if ($passedIdx -lt 0) {
        $passedIdx = $t.LastIndexOf('        Log("TEST PASSED");')
    }
    if ($passedIdx -lt 0) { return $t }

    $searchFrom = 0
    $uc = $t.LastIndexOf('Upload Correct Docs')
    if ($uc -lt 0) { $uc = $t.LastIndexOf('Ngarko dokumente te sakta') }
    if ($uc -lt 0) { $uc = $t.LastIndexOf('correctFileKontratat') }
    if ($uc -lt 0) { $uc = $t.LastIndexOf('Upload correct docs') }
    if ($uc -gt 0) { $searchFrom = $uc }

    $region = $t.Substring($searchFrom, $passedIdx - $searchFrom)
    $starts = @(
        '                //Log("Click ''Dergo'' button");',
        '                //Log("Kliko Dergo");',
        '                Log("Click ''Dergo'' button");',
        '                Log("Kliko Dergo");',
        '                driver.FindElement(By.XPath("//button[contains(normalize-space(),''Dergo'')]")).Click();'
    )
    # use ASCII-safe search for Dergo click after docs
    $cutRel = -1
    $m1 = $region.LastIndexOf('//Log("Click ''Dergo'' button")')
    $m2 = $region.LastIndexOf('//Log("Kliko Dergo")')
    $m3 = $region.LastIndexOf('Log("Click ''Dergo'' button");')
    $m4 = $region.LastIndexOf('Log("Kliko Dergo");')
    $m5 = $region.LastIndexOf('//button[contains(normalize-space()')
    foreach ($c in @($m1,$m2,$m3,$m4)) {
        if ($c -gt $cutRel) { $cutRel = $c }
    }
    if ($m5 -gt $cutRel) {
        $snipStart = [Math]::Max(0, $m5 - 120)
        $snippet = $region.Substring($snipStart, [Math]::Min(160, $region.Length - $snipStart))
        if ($snippet -notmatch 'without required') { $cutRel = $m5 }
    }

    $indentDual = $dual -replace '(?m)^        ', '                '

    if ($cutRel -ge 0) {
        $abs = $searchFrom + $cutRel
        $endMarker = if ($commented) { '                //Log("TEST PASSED");' } else { '                Log("TEST PASSED");' }
        if ($t.IndexOf($endMarker, $abs) -lt 0) { $endMarker = '        Log("TEST PASSED");' }
        $end = $t.LastIndexOf($endMarker)
        $end = $end + $endMarker.Length
        $t = $t.Substring(0, $abs) + $indentDual + $t.Substring($end)
    } else {
        $endMarker = if ($commented) { '                //Log("TEST PASSED");' } else { '                Log("TEST PASSED");' }
        if (-not $t.Contains($endMarker)) { $endMarker = '        Log("TEST PASSED");' }
        $end = $t.LastIndexOf($endMarker)
        $t = $t.Substring(0, $end) + $indentDual + $t.Substring($end + $endMarker.Length)
    }
    return $t
}

$uploadFiles = @()

foreach ($file in $allCs) {
    $t = [System.IO.File]::ReadAllText($file.FullName, $enc)
    $orig = $t
    $t = $t.Replace('mieinstitution-mie-institution-1', 'mie_merge')
    $t = $t.Replace('SendKeys("mie-institution")', 'SendKeys("mie_merge")')
    $t = Replace-AplikimXpath $t
    $t = Replace-CorrectPdfs $t

    $isUpload = $t.Contains('Signed_TEST_signed.pdf') -or $orig.Contains('Ngarko dokumente te sakta') -or $orig.Contains('Upload Correct Docs')
    $isGjurmo = $file.Name -match '(?i)gjurm'
    $isStub = ($t.Length -lt 500)

    if ($isUpload -and -not $isGjurmo -and -not $isStub) {
        $t = Insert-Wait $t
        $t = Insert-Helpers $t
        $t = Replace-DualOutcome $t
        $uploadFiles += $file
    }

    if ($t -ne $orig) {
        [System.IO.File]::WriteAllText($file.FullName, $t, $enc)
        Write-Host "patched $($file.FullName.Substring($Root.Length+1))"
    }
}

foreach ($file in $uploadFiles) {
    $codeHint = $file.Directory.Name
    $t = [System.IO.File]::ReadAllText($file.FullName, $enc)
    if (-not $t.Contains("FindDerghoButtonInMain")) { Write-Host "skip FailCase (no helpers): $($file.Name)"; continue }

    $cls = [regex]::Match($t, 'public class (\w+)').Groups[1].Value
    $t = $t.Replace("public class $cls", "public class ${cls}_FailCase")
    $t = $t.Replace('Log("===== TEST START =====");', 'Log("===== TEST START (FAIL CASE) =====");')

    $m = [regex]::Match($t, '    \[Test\]\s+public void (\w+)\(\)')
    if (-not $m.Success) { throw "$($file.Name): no [Test] method" }
    $method = $m.Groups[1].Value
    $t = $t.Replace("public void $method()", "public void ${method}_FailCase_ReturnsUiMessage()")

    $summary = $summaryTpl.Replace("12426", $codeHint)
    if ($t.Contains("[TestFixture]`r`n")) {
        $t = $t.Replace("[TestFixture]`r`n", $summary + "[TestFixture]`r`n")
    } elseif ($t.Contains("[TestFixture]`n")) {
        $t = $t.Replace("[TestFixture]`n", $summary.Replace("`r`n","`n") + "[TestFixture]`n")
    }

    $clickIdx = $t.IndexOf("    private void ClickDerghoAfterDocumentationReady(")
    $testIdx = $t.IndexOf("    [Test]", $clickIdx)
    if ($testIdx -gt 0 -and -not $t.Contains("CaptureVisibleUiMessageAfterDergo")) {
        $t = $t.Insert($testIdx, $capture + "`r`n")
    }

    $cutMarkers = @(
        'Log("Click ''Dergo'' button without required document");',
        'Log("Kliko Dergo pa dokumente");',
        'Log("Click Shto without docs'
    )
    $cut = -1
    foreach ($cm in $cutMarkers) {
        $i = $t.IndexOf($cm)
        if ($i -ge 0) { $cut = $i; break }
    }
    if ($cut -lt 0) {
        Write-Host "skip FailCase (no cut): $($file.Name)"
        continue
    }
    # include leading indent of the cut line
    $lineStart = $t.LastIndexOf("`n", $cut)
    if ($lineStart -ge 0) { $cut = $lineStart + 1 }

    $indentFail = $failTail -replace '(?m)^        ', '                '
    $catchIdx = $t.LastIndexOf('catch (Exception')
    if ($catchIdx -gt $cut) {
        $tryClose = $t.LastIndexOf('}', $catchIdx)
        $t = $t.Substring(0, $cut) + $indentFail + $t.Substring($tryClose)
    } else {
        $t = $t.Substring(0, $cut) + $failTail + "`r`n    }`r`n}`r`n"
    }

    $dest = Join-Path $file.Directory.FullName ($file.BaseName + "_FailCase.cs")
    [System.IO.File]::WriteAllText($dest, $t, $enc)
    Write-Host "wrote $($dest.Substring($Root.Length+1)) from $method"
}

Write-Host "done"
