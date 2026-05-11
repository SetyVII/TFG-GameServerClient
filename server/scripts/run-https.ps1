param(
    [string]$JavaHome = $env:JAVA_HOME
)

$ErrorActionPreference = "Stop"

# ── Platform detection (PS 5.1 + PS Core 6+) ──────────────────────────
if (Test-Path variable:IsWindows) {
    # PowerShell Core 6+ provides $IsWindows / $IsLinux / $IsMacOS
    $IsWindows = $IsWindows
    $IsLinux   = $IsLinux
    $IsMacOS   = $IsMacOS
}
else {
    # PowerShell 5.1 fallback
    $IsWindows = [System.Environment]::OSVersion.Platform -eq 'Win32NT'
    $IsLinux   = $false
    $IsMacOS   = $false
}

$sep    = if ($IsWindows) { '\' } else { '/' }
$exeExt = if ($IsWindows) { '.exe' } else { '' }

$projectRoot  = Split-Path -Parent $PSScriptRoot
$keystorePath = Join-Path $projectRoot ("src" + $sep + "main" + $sep +
                                        "resources" + $sep + "local-dev.p12")

# ── JDK helpers ────────────────────────────────────────────────────────
function Test-JavaPath {
    param([string]$Path)
    if (-not $Path) { return $false }
    $javaBin = Join-Path $Path ("bin" + $sep + "java" + $exeExt)
    return Test-Path $javaBin
}

function Find-JavaHome {
    # 1. JAVA_HOME env
    if (Test-JavaPath $env:JAVA_HOME) { return $env:JAVA_HOME }

    # 2. Scan common install directories
    $searchPaths = @()

    if ($IsWindows) {
        $searchPaths += "C:\Program Files\Java"                  # Oracle JDK
        $searchPaths += "C:\Program Files (x86)\Java"            # Oracle JDK 32-bit
        $searchPaths += Join-Path $env:USERPROFILE ".jdks"       # IntelliJ IDEA
        $searchPaths += "C:\Program Files\Eclipse Adoptium"      # Temurin
        $searchPaths += "C:\Program Files\Amazon Corretto"       # Corretto
        $searchPaths += "C:\Program Files\Microsoft"             # Microsoft OpenJDK
    }
    else {
        $searchPaths += "/usr/lib/jvm"                          # Debian / Ubuntu / Fedora
        $searchPaths += "/usr/java"                             # Oracle JDK (manual)
        $searchPaths += "/opt/java"                             # custom
        $searchPaths += "/opt/jdk"                              # custom
        if ($IsMacOS) {
            $searchPaths += "/Library/Java/JavaVirtualMachines"  # macOS standard
        }
    }

    foreach ($basePath in $searchPaths) {
        if (Test-Path $basePath) {
            $jdks = Get-ChildItem -Path $basePath -Directory `
                        -ErrorAction SilentlyContinue `
                | Where-Object { Test-JavaPath $_.FullName } `
                | Sort-Object Name -Descending

            if ($jdks.Count -gt 0) {
                Write-Host "JDK encontrado en $basePath`:" $jdks[0].Name
                return $jdks[0].FullName
            }
        }
    }

    # 3. Resolve 'java' from PATH
    try {
        $javaCmd = (Get-Command java -ErrorAction SilentlyContinue).Source
        if ($javaCmd) {
            $resolved  = (Resolve-Path $javaCmd -ErrorAction Stop).Path
            $javaBin   = Split-Path -Parent $resolved
            $candidate = Split-Path -Parent $javaBin
            if (Test-JavaPath $candidate) { return $candidate }
        }
    }
    catch {}

    return $null
}

# ── Resolve JAVA_HOME ──────────────────────────────────────────────────
if (-not (Test-JavaPath $JavaHome)) {
    $foundJava = Find-JavaHome
    if ($foundJava) {
        $JavaHome = $foundJava
        Write-Host "JDK encontrado automáticamente: $JavaHome" -ForegroundColor Green
    }
}

if (-not (Test-JavaPath $JavaHome)) {
    throw "No se encontró JAVA_HOME válido. Configura JAVA_HOME y vuelve a ejecutar."
}

$env:JAVA_HOME = $JavaHome
$env:Path = (Join-Path $JavaHome "bin") + [IO.Path]::PathSeparator + $env:Path

# ── Verify keytool ─────────────────────────────────────────────────────
$keytoolPath = Join-Path $JavaHome ("bin" + $sep + "keytool" + $exeExt)
if (-not (Test-Path $keytoolPath)) {
    throw "No se encontró keytool en $keytoolPath"
}

# ── Certificate generation ─────────────────────────────────────────────
if (-not (Test-Path $keystorePath)) {
    $localIp = $null

    # --- Platform-specific IP detection ---
    if ($IsWindows) {
        try {
            $localIp = (Get-NetIPAddress -AddressFamily IPv4 `
                            -PrefixOrigin Dhcp, Manual `
                | Where-Object {
                    $_.IPAddress -notlike "169.254.*" -and
                    $_.IPAddress -ne "127.0.0.1"
                } | Select-Object -First 1 -ExpandProperty IPAddress)
        } catch {}
    }
    else {
        # hostname -I (GNU coreutils)
        try {
            $raw = ((hostname -I 2>$null) -split '\s+') | Where-Object {
                $_ -match '\d+\.\d+\.\d+\.\d+' -and
                $_ -notlike '169.254.*' -and
                $_ -ne '127.0.0.1'
            }
            if ($raw.Count -gt 0) { $localIp = $raw[0] }
        } catch {}

        # ip addr (iproute2)
        if (-not $localIp) {
            try {
                $output = & ip -4 -o addr show scope global 2>$null
                if ($output -match 'inet\s+(\d+\.\d+\.\d+\.\d+)') {
                    $candidate = $matches[1]
                    if ($candidate -notlike '169.254.*' -and $candidate -ne '127.0.0.1') {
                        $localIp = $candidate
                    }
                }
            } catch {}
        }

        # ifconfig (BSD / macOS fallback)
        if (-not $localIp) {
            try {
                $output = & ifconfig 2>$null
                if ($output -match 'inet\s+(\d+\.\d+\.\d+\.\d+)') {
                    $candidate = $matches[1]
                    if ($candidate -notlike '127.*' -and $candidate -notlike '169.254.*') {
                        $localIp = $candidate
                    }
                }
            } catch {}
        }
    }

    # --- Cross-platform .NET fallback ---
    if (-not $localIp) {
        try {
            $hostName  = [System.Net.Dns]::GetHostName()
            $hostEntry = [System.Net.Dns]::GetHostEntry($hostName)
            $localIp = ($hostEntry.AddressList | Where-Object {
                $_.AddressFamily -eq 'InterNetwork' -and
                -not $_.IPAddressToString.StartsWith('127.') -and
                -not $_.IPAddressToString.StartsWith('169.254.')
            } | Select-Object -First 1).IPAddressToString
        } catch {}
    }

    if (-not $localIp) { $localIp = "127.0.0.1" }

    Write-Host "Generando certificado autofirmado para IP: $localIp"

    & $keytoolPath -genkeypair `
        -alias tfg-local `
        -keyalg RSA `
        -keysize 2048 `
        -validity 3650 `
        -storetype PKCS12 `
        -keystore $keystorePath `
        -storepass changeit `
        -keypass changeit `
        -dname "CN=$localIp, OU=TFG, O=TFG, L=Local, ST=Local, C=ES" `
        -ext "SAN=dns:localhost,ip:127.0.0.1,ip:$localIp"

    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo generar el certificado local."
    }

    Write-Host "Certificado generado en $keystorePath"
}

# ── Launch server ──────────────────────────────────────────────────────
Set-Location $projectRoot

if ($IsWindows) {
    & .\mvnw.cmd spring-boot:run "-Dspring-boot.run.profiles=https"
}
else {
    & ./mvnw spring-boot:run "-Dspring-boot.run.profiles=https"
}
