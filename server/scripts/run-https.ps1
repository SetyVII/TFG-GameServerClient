param(
    [string]$JavaHome = $env:JAVA_HOME
)

# Detectar sistema operativo
$IsWindows = $PSVersionTable.PSEdition -eq 'Desktop' -or $PSVersionTable.Platform -eq 'Win32NT'
$IsLinux = $PSVersionTable.Platform -eq 'Unix' -and -not $IsMacOS
$IsMacOS = $PSVersionTable.Platform -eq 'Unix' -and (uname) -eq 'Darwin'

$sep = if ($IsWindows) { '\' } else { '/' }
$exeExt = if ($IsWindows) { '.exe' } else { '' }

$projectRoot = Split-Path -Parent $PSScriptRoot
$keystorePath = Join-Path $projectRoot ("src" + $sep + "main" + $sep + "resources" + $sep + "local-dev.p12")

function Test-JavaPath {
    param([string]$Path)
    if (-not $Path) { return $false }
    $javaBin = Join-Path $Path ("bin" + $sep + "java" + $exeExt)
    return Test-Path $javaBin
}

function Find-JavaHome {
    # 1. Intentar JAVA_HOME
    if (Test-JavaPath $env:JAVA_HOME) {
        return $env:JAVA_HOME
    }

    # 2. Buscar en rutas comunes según SO
    $searchPaths = @()

    if ($IsWindows) {
        $searchPaths += "C:\Program Files\Java"
        $searchPaths += "C:\Program Files (x86)\Java"
        $searchPaths += Join-Path $env:USERPROFILE ".jdks"
    }
    else {
        $searchPaths += "/usr/lib/jvm"
        $searchPaths += "/usr/java"
        $searchPaths += "/opt/java"
        $searchPaths += "/opt/jdk"
        $searchPaths += "/Library/Java/JavaVirtualMachines"
    }

    foreach ($basePath in $searchPaths) {
        if (Test-Path $basePath) {
            $jdks = Get-ChildItem -Path $basePath -Directory -ErrorAction SilentlyContinue | Where-Object {
                Test-JavaPath $_.FullName
            } | Sort-Object Name -Descending

            if ($jdks.Count -gt 0) {
                return $jdks[0].FullName
            }
        }
    }

    # 3. Intentar con which/java en Linux/Mac
    if (-not $IsWindows) {
        try {
            $javaPath = (which java 2>$null) -or (Get-Command java -ErrorAction SilentlyContinue).Source
            if ($javaPath) {
                $resolvedPath = (Resolve-Path $javaPath).Path
                if ($resolvedPath) {
                    # Subir dos niveles: bin/java -> /java/home
                    $javaHome = Split-Path -Parent (Split-Path -Parent $resolvedPath)
                    if (Test-JavaPath $javaHome) {
                        return $javaHome
                    }
                }
            }
        }
        catch {
            # Ignorar errores
        }
    }

    return $null
}

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
$keytoolPath = Join-Path $JavaHome ("bin" + $sep + "keytool" + $exeExt)

if (-not (Test-Path $keytoolPath)) {
    throw "No se encontró keytool en $keytoolPath"
}

if (-not (Test-Path $keystorePath)) {
    if ($IsWindows) {
        $localIp = (Get-NetIPAddress -AddressFamily IPv4 -PrefixOrigin Dhcp,Manual `
            | Where-Object {
                $_.IPAddress -notlike "169.254.*" -and
                $_.IPAddress -ne "127.0.0.1"
            } | Select-Object -First 1 -ExpandProperty IPAddress)
    }
    else {
        # Linux/Mac: obtener IP local
        try {
            $localIp = (hostname -I 2>/dev/null).Trim().Split()[0]
        }
        catch {
            $localIp = $null
        }
        if (-not $localIp) {
            try {
                $localIp = (ip route get 1.1.1.1 2>/dev/null | awk '{print $7; exit}')
            }
            catch {
                $localIp = $null
            }
        }
    }

    if (-not $localIp) {
        $localIp = "127.0.0.1"
    }

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
}

Set-Location $projectRoot

# Ejecutar Maven según el sistema operativo
if ($IsWindows) {
    & .\mvnw.cmd spring-boot:run "-Dspring-boot.run.profiles=https"
}
else {
    & ./mvnw spring-boot:run "-Dspring-boot.run.profiles=https"
}
