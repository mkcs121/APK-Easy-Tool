# Regression tests for the parts that rewrite app files. Run on CI before the build:
#   powershell -NoProfile -ExecutionPolicy Bypass -File tests\regression.ps1
# Compiles the standalone helper classes with Add-Type (C# 5) and checks them on temporary fixtures.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root 'APKEasyTool\Utils'
Add-Type -Path "$src\ExternalTexts.cs", "$src\FontTools.cs", "$src\KeystorePassword.cs" `
    -ReferencedAssemblies System.Core, System.Web.Extensions, System.Security, System.Drawing

$script:failures = 0
function Check($name, $condition) {
    if ($condition) { Write-Host "PASS  $name" }
    else { Write-Host "FAIL  $name" -ForegroundColor Red; $script:failures++ }
}
function Dict { return (New-Object 'System.Collections.Generic.Dictionary[string,string]') }
function NewProject {
    $d = Join-Path ([IO.Path]::GetTempPath()) ("aet_test_" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $d | Out-Null
    return $d
}
$utf8 = New-Object System.Text.UTF8Encoding($false)

# ---------- JSON language files (AET-05) ----------
$p = NewProject
$lang = Join-Path $p 'assets\lang'
New-Item -ItemType Directory -Path $lang | Out-Null
[IO.File]::WriteAllText("$lang\en.json", '{"hello":"Hello there","menu":{"start":"Start game"}}', $utf8)
[IO.File]::WriteAllText("$lang\de.json", '{"hello":"Hallo"}', $utf8)
$texts = [APKEasyTool.ExternalTexts]::LoadJson($p)
Check 'json: texts found' ($texts.Count -eq 2)

$threw = $false
try { [APKEasyTool.ExternalTexts]::NewLanguageFileName('en.json', 'en') | Out-Null } catch { $threw = $true }
Check 'json: new language "en" refuses to overwrite en.json' $threw
Check 'json: pt-rBR keeps its region' ([APKEasyTool.ExternalTexts]::NewLanguageFileName('en.json', 'pt-rBR') -eq 'pt-BR.json')
Check 'json: pt-rBR and pt-rPT differ' (([APKEasyTool.ExternalTexts]::NewLanguageFileName('en.json', 'pt-rBR')) -ne ([APKEasyTool.ExternalTexts]::NewLanguageFileName('en.json', 'pt-rPT')))
Check 'json: prefixed name' ([APKEasyTool.ExternalTexts]::NewLanguageFileName('com.en.json', 'fa') -eq 'com.fa.json')

$enBefore = [IO.File]::ReadAllText("$lang\en.json")
$tr = Dict
$tr[$texts[0].Key] = 'سلام'
[APKEasyTool.ExternalTexts]::SaveJson($p, $tr, $false, 'fa') | Out-Null
Check 'json: new-language mode leaves en.json unchanged' ([IO.File]::ReadAllText("$lang\en.json") -eq $enBefore)
Check 'json: fa.json written' ((Test-Path "$lang\fa.json") -and ([IO.File]::ReadAllText("$lang\fa.json", [Text.Encoding]::UTF8) -match 'سلام'))
$trDe = Dict
$trDe[$texts[0].Key] = 'Hallo Welt'
[APKEasyTool.ExternalTexts]::SaveJson($p, $trDe, $false, 'de') | Out-Null
$fa = [APKEasyTool.ExternalTexts]::LoadSidecar($p, $false, 'fa')
$de = [APKEasyTool.ExternalTexts]::LoadSidecar($p, $false, 'de')
Check 'json: translation memory kept per language' (($fa[$texts[0].Key] -eq 'سلام') -and ($de[$texts[0].Key] -eq 'Hallo Welt'))

# ---------- Web apps: keys and comparisons stay (AET-06) ----------
$p = NewProject
$web = Join-Path $p 'assets\public'
New-Item -ItemType Directory -Path $web | Out-Null
$js = 'const m={"Send message":1};' +
      'if(a==="Send message"){x()}' +
      'switch(t){case "Send message":y()}' +
      'localStorage.getItem("Send message");' +
      'o["Send message"]=2;' +
      'if("Send message" in m){}' +
      'h("button",null,"Send message");' +
      'const label=ok?"Send message":"Try again later";'
[IO.File]::WriteAllText("$web\index-abc.js", $js, $utf8)
$items = [APKEasyTool.ExternalTexts]::LoadWeb($p, $false)
Check 'web: UI texts found' (($items | Where-Object { $_.Original -eq 'Send message' }).Count -eq 1 -and ($items | Where-Object { $_.Original -eq 'Try again later' }).Count -eq 1)
$wt = Dict
$wt['Send message'] = 'ارسال "پیام" و ''متن'' `${x}'
$wt['Try again later'] = 'بعداً دوباره امتحان کنید'
[APKEasyTool.ExternalTexts]::SaveWeb($p, $wt, $false, 'fa', $false) | Out-Null
$out = [IO.File]::ReadAllText("$web\index-abc.js", [Text.Encoding]::UTF8)
Check 'web: object key unchanged'      ($out.Contains('{"Send message":1}'))
Check 'web: comparison unchanged'      ($out.Contains('a==="Send message"'))
Check 'web: case label unchanged'      ($out.Contains('case "Send message":'))
Check 'web: API argument unchanged'    ($out.Contains('getItem("Send message")'))
Check 'web: property name unchanged'   ($out.Contains('o["Send message"]'))
Check 'web: "in" operand unchanged'    ($out.Contains('if("Send message" in m)'))
Check 'web: shown text translated'     ($out.Contains('h("button",null,"ارسال'))
Check 'web: ternary texts translated'  ($out.Contains('بعداً دوباره امتحان کنید') -and -not $out.Contains('ok?"Send message"'))
$again = [APKEasyTool.ExternalTexts]::SaveWeb($p, $wt, $false, 'fa', $false)
Check 'web: second save changes nothing' ($again -eq 0)
$node = Get-Command node -ErrorAction SilentlyContinue
if ($node) {
    & node --check "$web\index-abc.js" 2>$null
    Check 'web: translated JS is still valid (node --check)' ($LASTEXITCODE -eq 0)
}
$empty = Dict
[APKEasyTool.ExternalTexts]::SaveWeb($p, $empty, $false, 'fa', $false) | Out-Null
Check 'web: empty translations restore the original' ([IO.File]::ReadAllText("$web\index-abc.js") -eq $js)

# ---------- Fonts (AET-10) ----------
$p = NewProject
$fontDir = Join-Path $p 'res\font'
New-Item -ItemType Directory -Path $fontDir | Out-Null
[IO.File]::WriteAllBytes("$fontDir\aet_font.ttf", [byte[]](0, 1, 0, 0, 9, 9))
$notFont = Join-Path $p 'not-a-font.ttf'
[IO.File]::WriteAllText($notFont, 'hello')
$threw = $false
try { [APKEasyTool.FontTools]::SetDefaultFont($p, $notFont) | Out-Null } catch { $threw = $true }
Check 'font: non-font file is refused' $threw
Check 'font: previous app font kept after a failed change' (Test-Path "$fontDir\aet_font.ttf")
$threw = $false
try { [APKEasyTool.FontTools]::SetDefaultFont($p, (Join-Path $p 'missing.ttf')) | Out-Null } catch { $threw = $true }
Check 'font: missing source file is refused, old font kept' ($threw -and (Test-Path "$fontDir\aet_font.ttf"))

# ---------- Keystore password at rest (AET-01) ----------
$protected = [APKEasyTool.KeystorePassword]::Protect('s3cret!')
Check 'password: not stored readable'      (-not $protected.Contains('s3cret') -and -not ([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($protected)).Contains('s3cret')))
Check 'password: round trip'               ([APKEasyTool.KeystorePassword]::Unprotect($protected) -eq 's3cret!')

# ---------- Remove ads: code + layouts, repeatable, restorable ----------
Add-Type -Path "$src\AdRemover.cs" -ReferencedAssemblies System.Core
$p = NewProject
$sm = Join-Path $p 'smali\com\google\android\gms\ads'
New-Item -ItemType Directory -Path $sm | Out-Null
$smali = ".class public Lcom/google/android/gms/ads/BaseAdView;`n.super Landroid/view/ViewGroup;`n`n" +
         ".method public loadAd(Lcom/google/android/gms/ads/AdRequest;)V`n    .locals 2`n    invoke-virtual {p0}, Lx;->y()V`n    return-void`n.end method`n`n" +
         ".method public isLoading()Z`n    .locals 1`n    const/4 v0, 0x1`n    return v0`n.end method`n"
[IO.File]::WriteAllText("$sm\BaseAdView.smali", $smali, $utf8)
$lay = Join-Path $p 'res\layout'
New-Item -ItemType Directory -Path $lay | Out-Null
$card = '<?xml version="1.0" encoding="utf-8"?>' + "`n" +
        '<com.google.android.gms.ads.nativead.NativeAdView android:layout_width="match_parent" android:layout_height="match_parent" xmlns:android="http://schemas.android.com/apk/res/android">' +
        '<LinearLayout android:layout_width="match_parent" android:layout_height="100dp"><TextView android:text="Ad" android:layout_width="wrap_content" android:layout_height="wrap_content" /></LinearLayout>' +
        '</com.google.android.gms.ads.nativead.NativeAdView>'
[IO.File]::WriteAllText("$lay\ad_card.xml", $card, $utf8)
$page = '<LinearLayout xmlns:android="http://schemas.android.com/apk/res/android"><TextView android:id="@id/title" /><com.google.android.gms.ads.AdView android:id="@id/adView" android:layout_width="match_parent" android:layout_height="wrap_content" /></LinearLayout>'
[IO.File]::WriteAllText("$lay\page.xml", $page, $utf8)

$sdks = [APKEasyTool.AdRemover]::Detect($p)
Check 'ads: AdMob detected, not yet disabled' ($sdks.Count -eq 1 -and -not $sdks[0].Disabled)
$r1 = [APKEasyTool.AdRemover]::Remove($p, $sdks, $null)
$patched = [IO.File]::ReadAllText("$sm\BaseAdView.smali")
Check 'ads: loadAd made a no-op' ($r1.Methods -ge 1 -and $patched -notmatch 'invoke-virtual \{p0\}, Lx;')
Check 'ads: ad card and banner hidden' ($r1.Layouts -ge 2)
$cardOut = [IO.File]::ReadAllText("$lay\ad_card.xml")
Check 'ads: everything inside the ad card hidden' (([regex]::Matches($cardOut, 'visibility="gone"')).Count -eq 3)
$pageOut = [IO.File]::ReadAllText("$lay\page.xml")
Check 'ads: other views of the page untouched' ($pageOut.Contains('<TextView android:id="@id/title" />'))
Check 'ads: SDK shown as disabled after removal' ([APKEasyTool.AdRemover]::Detect($p)[0].Disabled)
$r2 = [APKEasyTool.AdRemover]::Remove($p, $sdks, $null)
Check 'ads: second run changes nothing' ($r2.Methods -eq 0 -and $r2.Layouts -eq 0 -and $r2.AlreadyDone -ge 1)
[APKEasyTool.AdRemover]::Restore($p) | Out-Null
Check 'ads: restore brings code and layouts back' (([IO.File]::ReadAllText("$sm\BaseAdView.smali") -eq $smali) -and ([IO.File]::ReadAllText("$lay\ad_card.xml") -eq $card) -and ([IO.File]::ReadAllText("$lay\page.xml") -eq $page))
Check 'ads: SDK active again after restore' (-not [APKEasyTool.AdRemover]::Detect($p)[0].Disabled)

if ($script:failures -gt 0) { Write-Host "$script:failures test(s) failed" -ForegroundColor Red; exit 1 }
Write-Host 'All regression tests passed'
