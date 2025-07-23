# ====================================================================
# 🔧 WINDOWS SERVICE INSTALLER FOR SERVER WATCHDOG
# Устанавливает вотчдог как Windows службу для автозапуска
# ====================================================================

param(
    [switch]$Install,
    [switch]$Uninstall,
    [switch]$Start,
    [switch]$Stop,
    [switch]$Status
)

$ServiceName = "ServerWatchdog"
$ServiceDisplayName = "Server Watchdog Service"
$ServiceDescription = "Monitors and automatically restarts the game server"
$ServicePath = "C:\SECS\Server"
$WatchdogScript = "C:\SECS\Server\server-watchdog.ps1"
$LogPath = "C:\SECS\Code\service-installer.log"

function Write-ServiceLog {
    param([string]$Message, [string]$Level = "INFO")
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "$timestamp [$Level] $Message"
    Write-Host $logEntry
    Add-Content -Path $LogPath -Value $logEntry -Encoding UTF8
}

function Test-AdminRights {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Install-WatchdogService {
    Write-ServiceLog "🔧 Installing Server Watchdog as Windows Service..." "INFO"
    
    if (!(Test-AdminRights)) {
        Write-ServiceLog "❌ Administrator rights required for service installation" "ERROR"
        return $false
    }
    
    # Проверяем, существует ли уже служба
    $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-ServiceLog "⚠️ Service '$ServiceName' already exists. Uninstalling first..." "WARN"
        Uninstall-WatchdogService
    }
    
    try {
        # Создаем служебный скрипт
        $serviceScriptContent = @"
# Windows Service Wrapper for Server Watchdog
Set-Location "$ServicePath"
& powershell.exe -ExecutionPolicy Bypass -File "$WatchdogScript" -CheckInterval 30
"@
        
        $serviceScriptPath = "$ServicePath\watchdog-service-wrapper.ps1"
        Set-Content -Path $serviceScriptPath -Value $serviceScriptContent -Encoding UTF8
        
        # Создаем службу с помощью sc.exe
        $binPath = "powershell.exe -ExecutionPolicy Bypass -File `"$serviceScriptPath`""
        
        $result = & sc.exe create $ServiceName binPath= $binPath start= auto displayName= "$ServiceDisplayName"
        
        if ($LASTEXITCODE -eq 0) {
            # Устанавливаем описание службы
            & sc.exe description $ServiceName "$ServiceDescription"
            
            # Настраиваем перезапуск при сбое
            & sc.exe failure $ServiceName reset= 86400 actions= restart/30000/restart/60000/restart/120000
            
            Write-ServiceLog "✅ Service installed successfully" "SUCCESS"
            return $true
        }
        else {
            Write-ServiceLog "❌ Failed to create service: $result" "ERROR"
            return $false
        }
    }
    catch {
        Write-ServiceLog "❌ Error installing service: $_" "ERROR"
        return $false
    }
}

function Uninstall-WatchdogService {
    Write-ServiceLog "🗑️ Uninstalling Server Watchdog service..." "INFO"
    
    if (!(Test-AdminRights)) {
        Write-ServiceLog "❌ Administrator rights required for service uninstallation" "ERROR"
        return $false
    }
    
    try {
        # Останавливаем службу если она запущена
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($service -and $service.Status -eq "Running") {
            Write-ServiceLog "🛑 Stopping service..." "INFO"
            Stop-Service -Name $ServiceName -Force
            Start-Sleep -Seconds 5
        }
        
        # Удаляем службу
        $result = & sc.exe delete $ServiceName
        
        if ($LASTEXITCODE -eq 0) {
            Write-ServiceLog "✅ Service uninstalled successfully" "SUCCESS"
            
            # Удаляем служебный скрипт
            $serviceScriptPath = "$ServicePath\watchdog-service-wrapper.ps1"
            if (Test-Path $serviceScriptPath) {
                Remove-Item $serviceScriptPath -Force
            }
            
            return $true
        }
        else {
            Write-ServiceLog "❌ Failed to delete service: $result" "ERROR"
            return $false
        }
    }
    catch {
        Write-ServiceLog "❌ Error uninstalling service: $_" "ERROR"
        return $false
    }
}

function Start-WatchdogService {
    Write-ServiceLog "🚀 Starting Server Watchdog service..." "INFO"
    
    try {
        Start-Service -Name $ServiceName
        Write-ServiceLog "✅ Service started successfully" "SUCCESS"
        return $true
    }
    catch {
        Write-ServiceLog "❌ Error starting service: $_" "ERROR"
        return $false
    }
}

function Stop-WatchdogService {
    Write-ServiceLog "🛑 Stopping Server Watchdog service..." "INFO"
    
    try {
        Stop-Service -Name $ServiceName -Force
        Write-ServiceLog "✅ Service stopped successfully" "SUCCESS"
        return $true
    }
    catch {
        Write-ServiceLog "❌ Error stopping service: $_" "ERROR"
        return $false
    }
}

function Get-WatchdogServiceStatus {
    Write-ServiceLog "📊 Checking Server Watchdog service status..." "INFO"
    
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    
    if ($service) {
        Write-ServiceLog "✅ Service Status: $($service.Status)" "INFO"
        Write-ServiceLog "📋 Service Name: $($service.Name)" "INFO"
        Write-ServiceLog "📋 Display Name: $($service.DisplayName)" "INFO"
        Write-ServiceLog "📋 Start Type: $($service.StartType)" "INFO"
        return $true
    }
    else {
        Write-ServiceLog "❌ Service not found" "ERROR"
        return $false
    }
}

# ====================================================================
# MAIN EXECUTION
# ====================================================================

Write-ServiceLog "🔧 ===== WATCHDOG SERVICE INSTALLER =====" "INFO"

# Если параметры не переданы, показываем меню
if (!$Install -and !$Uninstall -and !$Start -and !$Stop -and !$Status) {
    Write-Host "🐕‍🦺 Server Watchdog Service Manager"
    Write-Host "=================================="
    Write-Host ""
    Write-Host "Usage:"
    Write-Host "  -Install    Install watchdog as Windows service"
    Write-Host "  -Uninstall  Remove watchdog service"
    Write-Host "  -Start      Start the service"
    Write-Host "  -Stop       Stop the service"
    Write-Host "  -Status     Check service status"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\install-watchdog-service.ps1 -Install"
    Write-Host "  .\install-watchdog-service.ps1 -Start"
    Write-Host "  .\install-watchdog-service.ps1 -Status"
    exit 0
}

if ($Install) {
    if (Install-WatchdogService) {
        Write-ServiceLog "🎉 Installation completed! You can now start the service." "SUCCESS"
    }
}

if ($Uninstall) {
    if (Uninstall-WatchdogService) {
        Write-ServiceLog "🎉 Uninstallation completed!" "SUCCESS"
    }
}

if ($Start) {
    Start-WatchdogService
}

if ($Stop) {
    Stop-WatchdogService
}

if ($Status) {
    Get-WatchdogServiceStatus
}

Write-ServiceLog "🔧 ===== OPERATION COMPLETED =====" "INFO" 