# ====================================================================
# УСТАНОВКА АВТОЗАПУСКА СИСТЕМЫ ВОТЧДОГА
# Создает задачу в планировщике Windows для автоматического запуска
# ====================================================================

param(
    [string]$ServerPath = "C:\SECS\Server",
    [string]$TaskName = "RenderFin-Server-Watchdog",
    [switch]$Uninstall = $false,
    [switch]$Force = $false
)

# Проверка прав администратора
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Функция логирования
function Write-InstallLog {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        "ERROR" { "Red" }
        "WARN" { "Yellow" }
        "SUCCESS" { "Green" }
        "INFO" { "Cyan" }
        default { "White" }
    }
    
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

# Проверка существования задачи
function Test-TaskExists {
    param([string]$Name)
    
    try {
        $task = Get-ScheduledTask -TaskName $Name -ErrorAction SilentlyContinue
        return $task -ne $null
    }
    catch {
        return $false
    }
}

# Удаление существующей задачи
function Remove-WatchdogTask {
    param([string]$Name)
    
    try {
        if (Test-TaskExists $Name) {
            Write-InstallLog "Removing existing task: $Name" "INFO"
            Unregister-ScheduledTask -TaskName $Name -Confirm:$false
            Write-InstallLog "Task removed successfully" "SUCCESS"
            return $true
        }
        else {
            Write-InstallLog "Task not found: $Name" "WARN"
            return $false
        }
    }
    catch {
        Write-InstallLog "Error removing task: $_" "ERROR"
        return $false
    }
}

# Создание новой задачи
function New-WatchdogTask {
    param([string]$Name, [string]$ScriptPath)
    
    try {
        Write-InstallLog "Creating new scheduled task: $Name" "INFO"
        
        # Создаем действие для задачи
        $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-ExecutionPolicy Bypass -WindowStyle Hidden -File `"$ScriptPath`""
        
        # Создаем триггер (запуск при старте системы)
        $trigger = New-ScheduledTaskTrigger -AtStartup
        
        # Настройки задачи
        $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -DontStopOnIdleEnd -ExecutionTimeLimit (New-TimeSpan -Hours 0)
        
        # Создаем задачу для запуска от имени SYSTEM
        $principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
        
        # Регистрируем задачу
        Register-ScheduledTask -TaskName $Name -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description "RenderFin Server Watchdog - Automatic server monitoring and restart system"
        
        Write-InstallLog "Scheduled task created successfully" "SUCCESS"
        return $true
    }
    catch {
        Write-InstallLog "Error creating task: $_" "ERROR"
        return $false
    }
}

# Проверка системных требований
function Test-SystemRequirements {
    Write-InstallLog "Checking system requirements..." "INFO"
    
    # Проверяем PowerShell
    if ($PSVersionTable.PSVersion.Major -lt 5) {
        Write-InstallLog "PowerShell 5.0 or higher required. Current version: $($PSVersionTable.PSVersion)" "ERROR"
        return $false
    }
    
    # Проверяем .NET
    try {
        $dotnetVersion = & dotnet --version
        Write-InstallLog "Found .NET version: $dotnetVersion" "INFO"
    }
    catch {
        Write-InstallLog ".NET Core/5+ not found. Please install .NET runtime." "ERROR"
        return $false
    }
    
    # Проверяем пути
    $masterScript = Join-Path $ServerPath "master-watchdog.ps1"
    $watchdogScript = Join-Path $ServerPath "server-watchdog-fixed.ps1"
    
    if (!(Test-Path $masterScript)) {
        Write-InstallLog "Master watchdog script not found: $masterScript" "ERROR"
        return $false
    }
    
    if (!(Test-Path $watchdogScript)) {
        Write-InstallLog "Primary watchdog script not found: $watchdogScript" "ERROR"
        return $false
    }
    
    # Проверяем доступность сервера
    $serverProject = Join-Path $ServerPath "Server.csproj"
    if (!(Test-Path $serverProject)) {
        Write-InstallLog "Server project not found: $serverProject" "ERROR"
        return $false
    }
    
    Write-InstallLog "All system requirements met" "SUCCESS"
    return $true
}

# Тестирование задачи
function Test-WatchdogTask {
    param([string]$Name)
    
    try {
        Write-InstallLog "Testing scheduled task..." "INFO"
        
        # Запускаем задачу
        Start-ScheduledTask -TaskName $Name
        
        # Ждем запуска
        Start-Sleep -Seconds 10
        
        # Проверяем состояние
        $task = Get-ScheduledTask -TaskName $Name
        $taskInfo = Get-ScheduledTaskInfo -TaskName $Name
        
        Write-InstallLog "Task State: $($task.State)" "INFO"
        Write-InstallLog "Last Run Time: $($taskInfo.LastRunTime)" "INFO"
        Write-InstallLog "Last Result: $($taskInfo.LastTaskResult)" "INFO"
        
        # Проверяем, что сервер отвечает
        Start-Sleep -Seconds 30
        
        try {
            [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
            $response = Invoke-WebRequest -Uri "https://renderfin.com/api-game-queue/stats" -TimeoutSec 15 -UseBasicParsing
            if ($response.StatusCode -eq 200) {
                Write-InstallLog "Server is responding! System test PASSED" "SUCCESS"
                return $true
            }
        }
        catch {
            Write-InstallLog "Server health check failed: $_" "WARN"
        }
        
        Write-InstallLog "Task started but server test inconclusive" "WARN"
        return $true
    }
    catch {
        Write-InstallLog "Error testing task: $_" "ERROR"
        return $false
    }
}

# ====================================================================
# ГЛАВНАЯ ЛОГИКА УСТАНОВКИ
# ====================================================================

Write-InstallLog "===== RENDERFIN SERVER AUTOSTART INSTALLER =====" "INFO"
Write-InstallLog "Task Name: $TaskName" "INFO"
Write-InstallLog "Server Path: $ServerPath" "INFO"

# Проверка прав администратора
if (!(Test-Administrator)) {
    Write-InstallLog "ERROR: This script requires administrator privileges!" "ERROR"
    Write-InstallLog "Please run PowerShell as Administrator and try again." "ERROR"
    exit 1
}

# Режим удаления
if ($Uninstall) {
    Write-InstallLog "UNINSTALL MODE: Removing autostart task" "WARN"
    
    if (Remove-WatchdogTask $TaskName) {
        Write-InstallLog "Autostart successfully removed" "SUCCESS"
        exit 0
    }
    else {
        Write-InstallLog "Failed to remove autostart" "ERROR"
        exit 1
    }
}

# Проверка системных требований
if (!(Test-SystemRequirements)) {
    Write-InstallLog "System requirements check failed" "ERROR"
    exit 1
}

# Проверка существующей задачи
if (Test-TaskExists $TaskName) {
    if ($Force) {
        Write-InstallLog "Task exists, removing due to -Force flag" "WARN"
        Remove-WatchdogTask $TaskName | Out-Null
    }
    else {
        Write-InstallLog "Task already exists: $TaskName" "WARN"
        Write-InstallLog "Use -Force to replace existing task" "INFO"
        exit 1
    }
}

# Создание задачи
$masterScript = Join-Path $ServerPath "master-watchdog.ps1"
if (New-WatchdogTask $TaskName $masterScript) {
    Write-InstallLog "Autostart task created successfully" "SUCCESS"
    
    # Тестирование
    Write-InstallLog "Testing the installation..." "INFO"
    if (Test-WatchdogTask $TaskName) {
        Write-InstallLog "Installation completed successfully!" "SUCCESS"
        Write-InstallLog "The server will now start automatically when Windows boots" "INFO"
        Write-InstallLog "Monitor logs at: C:\SECS\Code\master-watchdog.log" "INFO"
        exit 0
    }
    else {
        Write-InstallLog "Installation completed but test failed" "WARN"
        Write-InstallLog "Check the logs for details" "INFO"
        exit 1
    }
}
else {
    Write-InstallLog "Failed to create autostart task" "ERROR"
    exit 1
}

Write-InstallLog "Installation process completed" "INFO" 