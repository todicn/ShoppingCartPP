# Initialize Storage Emulators for Product Catalog local development
# This script sets up Cosmos DB Emulator and Redis for local development
# Optimized for both VS Code terminal and external PowerShell execution

#Requires -Version 5.1

[CmdletBinding()]
param(
    [switch]$SkipCosmosDB,
    [switch]$SkipRedis,
    [switch]$VerboseOutput,
    [switch]$Help
)

# Set strict mode for better error handling
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Set console encoding to UTF-8 for better compatibility (PowerShell 6+)
if ($PSVersionTable.PSVersion.Major -ge 6) {
    try {
        [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
        [Console]::InputEncoding = [System.Text.Encoding]::UTF8
    } catch {
        # Ignore encoding errors in older environments
    }
}

# Function to show help
function Show-Help {
    Write-Host ""
    Write-Host "Product Catalog Storage Emulators Initialization Script" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "USAGE:" -ForegroundColor Yellow
    Write-Host "  .\initialize-emulators.ps1 [OPTIONS]"
    Write-Host ""
    Write-Host "OPTIONS:" -ForegroundColor Yellow
    Write-Host "  -SkipCosmosDB     Skip Cosmos DB Emulator initialization"
    Write-Host "  -SkipRedis        Skip Redis initialization"
    Write-Host "  -VerboseOutput    Enable verbose output"
    Write-Host "  -Help             Show this help message"
    Write-Host ""
    Write-Host "EXAMPLES:" -ForegroundColor Yellow
    Write-Host "  .\initialize-emulators.ps1"
    Write-Host "  .\initialize-emulators.ps1 -VerboseOutput"
    Write-Host "  .\initialize-emulators.ps1 -SkipCosmosDB"
    Write-Host "  .\initialize-emulators.ps1 -SkipRedis -VerboseOutput"
    Write-Host ""
    Write-Host "DESCRIPTION:" -ForegroundColor Yellow
    Write-Host "  This script initializes storage emulators only."
    Write-Host "  After running this script, use 'initialize-catalog-data.ps1' to set up database structures."
    Write-Host ""
    Write-Host "TROUBLESHOOTING:" -ForegroundColor Yellow
    Write-Host "  If you have issues running this script outside VS Code:"
    Write-Host "  - Use: powershell -ExecutionPolicy Bypass -File initialize-emulators.ps1"
    Write-Host "  - See TROUBLESHOOTING.md for more solutions"
    Write-Host ""
    exit 0
}

if ($Help) {
    Show-Help
}

# Set verbose preference if requested
if ($VerboseOutput) {
    $VerbosePreference = "Continue"
}

# Enhanced logging functions without Unicode characters for better compatibility
function Write-Section {
    param([string]$Title)
    try {
        Write-Host ""
        Write-Host "--- $Title ---" -ForegroundColor Yellow
    } catch {
        Write-Output ""
        Write-Output "--- $Title ---"
    }
}

function Write-Success {
    param([string]$Message)
    try {
        Write-Host "[OK] $Message" -ForegroundColor Green
    } catch {
        Write-Output "[SUCCESS] $Message"
    }
}

function Write-ErrorMsg {
    param([string]$Message)
    try {
        Write-Host "[ERROR] $Message" -ForegroundColor Red
    } catch {
        Write-Output "[ERROR] $Message"
    }
}

function Write-Warning {
    param([string]$Message)
    try {
        Write-Host "[WARN] $Message" -ForegroundColor Yellow
    } catch {
        Write-Output "[WARNING] $Message"
    }
}

function Write-Info {
    param([string]$Message)
    try {
        Write-Host "[INFO] $Message" -ForegroundColor Cyan
    } catch {
        Write-Output "[INFO] $Message"
    }
}

function Write-Step {
    param([string]$Message)
    try {
        Write-Host "[STEP] $Message" -ForegroundColor White
    } catch {
        Write-Output "[STEP] $Message"
    }
}

# Main execution with comprehensive error handling
try {
    Write-Host ""
    Write-Host "=== Product Catalog Storage Emulators Initialization ===" -ForegroundColor Magenta
    Write-Host "This script will initialize Cosmos DB Emulator and Redis for local development" -ForegroundColor Cyan
    Write-Host "Start time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
    Write-Host ""

    # Enhanced prerequisite checks
    Write-Section "Environment Checks"
    
    # PowerShell version check
    Write-Verbose "Checking PowerShell version..."
    $psVersion = $PSVersionTable.PSVersion
    if ($psVersion.Major -ge 5) {
        Write-Success "PowerShell version $psVersion is supported"
    } else {
        Write-ErrorMsg "PowerShell version $psVersion is not supported. Please upgrade to PowerShell 5.1 or later."
        exit 1
    }
    
    # Administrator check
    Write-Verbose "Checking administrator privileges..."
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    if ($isAdmin) {
        Write-Success "Running as administrator"
    } else {
        Write-Warning "Not running as administrator. Some operations may fail."
        Write-Info "Consider running PowerShell as Administrator for best results"
    }
    
    # Network connectivity test (simplified and robust)
    Write-Verbose "Testing basic network connectivity..."
    try {
        $null = [System.Net.NetworkInformation.Ping]::new().Send("127.0.0.1", 1000)
        Write-Success "Network connectivity OK"
    } catch {
        Write-Warning "Network connectivity test failed: $($_.Exception.Message)"
        Write-Info "This may not affect local emulator functionality"
    }

    # Additional environment checks
    Write-Verbose "Checking additional prerequisites..."
    
    # Check for WSL (for Redis option)
    $wslCheck = Get-Command wsl -ErrorAction SilentlyContinue
    if ($wslCheck) {
        Write-Success "WSL is available"
        try {
            $ubuntuCheck = wsl -l -q 2>$null | Where-Object { $_ -match "Ubuntu" }
            if ($ubuntuCheck) {
                Write-Success "Ubuntu distribution found in WSL"
            } else {
                Write-Info "Ubuntu distribution not found in WSL (optional for Redis)"
            }
        } catch {
            Write-Info "Could not check WSL distributions"
        }
    } else {
        Write-Info "WSL not available (optional for Redis setup)"
    }

    # Cosmos DB Emulator section
    if (-not $SkipCosmosDB) {
        Write-Section "Cosmos DB Emulator Setup"
        
        $cosmosPath = "${env:ProgramFiles}\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe"
        
        if (-not (Test-Path $cosmosPath)) {
            Write-ErrorMsg "Cosmos DB Emulator not found at: $cosmosPath"
            Write-Info "Please install Cosmos DB Emulator from: https://docs.microsoft.com/azure/cosmos-db/local-emulator"
            Write-Info "Download link: https://aka.ms/cosmosdb-emulator"
        } else {
            Write-Success "Cosmos DB Emulator found"
            
            # Enhanced process detection
            Write-Verbose "Checking if Cosmos DB Emulator is running..."
            $cosmosProcess = Get-Process -Name "CosmosDB.Emulator" -ErrorAction SilentlyContinue
            if ($cosmosProcess) {
                Write-Info "Cosmos DB Emulator is already running (PID: $($cosmosProcess.Id))"
                
                # Check if emulator is responding
                Write-Verbose "Testing emulator responsiveness..."
                try {
                    $response = Invoke-WebRequest -Uri "https://localhost:8081/_explorer/index.html" -UseBasicParsing -TimeoutSec 5 -SkipCertificateCheck 2>$null
                    if ($response.StatusCode -eq 200) {
                        Write-Success "Cosmos DB Emulator is responding"
                        Write-Info "Data Explorer: https://localhost:8081/_explorer/index.html"
                    } else {
                        Write-Warning "Cosmos DB Emulator may still be starting up"
                    }
                } catch {
                    Write-Info "Cosmos DB Emulator is starting up (may take 1-2 minutes)"
                }
            } else {
                Write-Step "Starting Cosmos DB Emulator..."
                try {
                    # Enhanced startup with better error handling
                    $startArgs = @("/NoExplorer", "/NoUI")
                    
                    # Add partition count if running as admin
                    if ($isAdmin) {
                        $startArgs += "/PartitionCount=10"
                    }
                    
                    Write-Verbose "Starting with arguments: $($startArgs -join ' ')"
                    $startProcess = Start-Process -FilePath $cosmosPath -ArgumentList $startArgs -PassThru -WindowStyle Hidden
                    
                    if ($startProcess) {
                        Write-Success "Cosmos DB Emulator startup initiated (PID: $($startProcess.Id))"
                        Write-Info "The emulator will take 1-2 minutes to fully initialize"
                        Write-Info "Check status at: https://localhost:8081/_explorer/index.html"
                        
                        # Optional: Wait for a short time and check if process is still running
                        Start-Sleep -Seconds 3
                        if (-not $startProcess.HasExited) {
                            Write-Success "Emulator process is running successfully"
                        } else {
                            Write-Warning "Emulator process exited quickly (Exit code: $($startProcess.ExitCode))"
                            Write-Info "The emulator may already be running or there might be a configuration issue"
                        }
                    } else {
                        Write-ErrorMsg "Failed to start Cosmos DB Emulator process"
                    }
                } catch {
                    Write-ErrorMsg "Failed to start Cosmos DB Emulator: $($_.Exception.Message)"
                    Write-Info "You can try starting it manually from the Start menu"
                }
            }
        }
    } else {
        Write-Info "Skipping Cosmos DB Emulator initialization"
    }

    # Redis section
    if (-not $SkipRedis) {
        Write-Section "Redis Setup"
        
        # Check for Redis in multiple ways
        $redisFound = $false
        $redisPath = ""
        $useWSL = $false
        
        # Method 1: Check PATH for Windows Redis
        $redisCmd = Get-Command redis-server -ErrorAction SilentlyContinue
        if ($redisCmd) {
            $redisFound = $true
            $redisPath = $redisCmd.Source
            Write-Success "Redis found in PATH: $redisPath"
        } else {
            # Method 2: Check common Windows installation locations
            $commonPaths = @(
                "${env:ProgramFiles}\Redis\redis-server.exe",
                "${env:ProgramFiles(x86)}\Redis\redis-server.exe",
                "C:\tools\redis\redis-server.exe"
            )
            
            foreach ($path in $commonPaths) {
                if (Test-Path $path) {
                    $redisFound = $true
                    $redisPath = $path
                    Write-Success "Redis found at: $redisPath"
                    break
                }
            }
        }
        
        # Method 3: Check WSL if Windows Redis not found
        if (-not $redisFound) {
            Write-Info "Windows Redis not found, checking WSL..."
            $wslCmd = Get-Command wsl -ErrorAction SilentlyContinue
            if ($wslCmd) {
                try {
                    # Check if Redis is installed in WSL
                    $wslRedisCheck = wsl which redis-server 2>$null
                    if ($wslRedisCheck) {
                        $redisFound = $true
                        $useWSL = $true
                        Write-Success "Redis found in WSL: $wslRedisCheck"
                    } else {
                        Write-Info "Redis not found in WSL, attempting installation..."
                        try {
                            Write-Step "Installing Redis in WSL..."
                            wsl sudo apt update
                            wsl sudo apt install -y redis-server
                            
                            # Verify installation
                            $wslRedisVerify = wsl which redis-server 2>$null
                            if ($wslRedisVerify) {
                                $redisFound = $true
                                $useWSL = $true
                                Write-Success "Redis successfully installed in WSL"
                            } else {
                                Write-Warning "Redis installation in WSL may have failed"
                            }
                        } catch {
                            Write-ErrorMsg "Failed to install Redis in WSL: $($_.Exception.Message)"
                        }
                    }
                } catch {
                    Write-Warning "Error checking WSL: $($_.Exception.Message)"
                }
            } else {
                Write-Info "WSL not available for Redis installation"
            }
        }
        
        if ($redisFound) {
            if ($useWSL) {
                # WSL Redis management
                Write-Verbose "Managing Redis in WSL..."
                try {
                    # Check if Redis is running in WSL
                    $wslRedisStatus = wsl pgrep redis-server 2>$null
                    if ($wslRedisStatus) {
                        Write-Info "Redis is already running in WSL (PID: $wslRedisStatus)"
                        
                        # Test connectivity
                        try {
                            $wslPingTest = wsl redis-cli ping 2>$null
                            if ($wslPingTest -eq "PONG") {
                                Write-Success "Redis is responding to ping in WSL"
                            } else {
                                Write-Warning "Redis process running in WSL but not responding to ping"
                            }
                        } catch {
                            Write-Info "Could not test Redis connectivity in WSL"
                        }
                    } else {
                        Write-Step "Starting Redis server in WSL..."
                        try {
                            # Start Redis in WSL as daemon
                            wsl nohup redis-server --daemonize yes 2>$null
                            Start-Sleep -Seconds 3
                            
                            # Verify it started
                            $wslRedisVerify = wsl pgrep redis-server 2>$null
                            if ($wslRedisVerify) {
                                Write-Success "Redis server started successfully in WSL (PID: $wslRedisVerify)"
                                
                                # Test connectivity
                                $wslPingTest = wsl redis-cli ping 2>$null
                                if ($wslPingTest -eq "PONG") {
                                    Write-Success "Redis responding to ping in WSL"
                                }
                            } else {
                                Write-Warning "Redis may not have started properly in WSL"
                                Write-Info "Try manually: wsl redis-server --daemonize yes"
                            }
                        } catch {
                            Write-ErrorMsg "Failed to start Redis in WSL: $($_.Exception.Message)"
                            Write-Info "You can try starting Redis manually in WSL"
                        }
                    }
                } catch {
                    Write-ErrorMsg "Error managing Redis in WSL: $($_.Exception.Message)"
                }
            } else {
                # Windows Redis management
                Write-Verbose "Managing Redis on Windows..."
                $redisProcess = Get-Process -Name "redis-server" -ErrorAction SilentlyContinue
                if ($redisProcess) {
                    Write-Info "Redis is already running (PID: $($redisProcess.Id))"
                    
                    # Test Redis connectivity
                    try {
                        $testRedis = Get-Command redis-cli -ErrorAction SilentlyContinue
                        if ($testRedis) {
                            $pingResult = redis-cli ping 2>$null
                            if ($pingResult -eq "PONG") {
                                Write-Success "Redis is responding to ping"
                            } else {
                                Write-Warning "Redis process running but not responding to ping"
                            }
                        } else {
                            Write-Info "Redis-cli not available for connectivity test"
                        }
                    } catch {
                        Write-Info "Could not test Redis connectivity"
                    }
                } else {
                    Write-Step "Starting Redis server..."
                    try {
                        if ($redisCmd) {
                            Start-Process -FilePath "redis-server" -WindowStyle Hidden
                        } else {
                            Start-Process -FilePath $redisPath -WindowStyle Hidden
                        }
                        
                        Start-Sleep -Seconds 2
                        
                        $redisCheck = Get-Process -Name "redis-server" -ErrorAction SilentlyContinue
                        if ($redisCheck) {
                            Write-Success "Redis server started successfully (PID: $($redisCheck.Id))"
                        } else {
                            Write-Warning "Redis may not have started properly"
                            Write-Info "Try starting Redis manually or check for configuration issues"
                        }
                    } catch {
                        Write-ErrorMsg "Failed to start Redis: $($_.Exception.Message)"
                        Write-Info "You can try starting Redis manually"
                    }
                }
            }
        } else {
            Write-ErrorMsg "Redis not found"
            Write-Info "Redis installation options:"
            Write-Info "1. Download Redis for Windows from: https://github.com/tporadowski/redis/releases"
            Write-Info "2. Install WSL and Ubuntu: wsl --install -d Ubuntu"
            Write-Info "3. Use Docker: docker run --name redis -p 6379:6379 -d redis:alpine"
            Write-Info "4. Use Chocolatey: choco install redis-64"
        }
    } else {
        Write-Info "Skipping Redis initialization"
    }

    # Summary and next steps
    Write-Section "Emulator Initialization Summary"
    Write-Success "Storage emulators initialization completed successfully"
    Write-Info ""
    Write-Info "Next Steps:"
    Write-Info "1. Run 'initialize-catalog-data.ps1' to set up database structures"
    Write-Info "2. Or run 'dotnet run --project ProductCatalog' to start the application"
    Write-Info ""
    
    if (-not $SkipCosmosDB) {
        Write-Info "Cosmos DB Resources:"
        Write-Info "  Data Explorer: https://localhost:8081/_explorer/index.html"
        Write-Info "  The application will create database and container automatically"
    }
    
    if (-not $SkipRedis -and $redisFound) {
        Write-Info ""
        if ($useWSL) {
            Write-Info "Redis is configured and running in WSL"
            Write-Info "WSL Redis commands: wsl redis-cli ping"
        } else {
            Write-Info "Redis is configured and running on Windows"
        }
    }
    
    Write-Host ""
    Write-Host "End time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
    Write-Host ""

} catch {
    Write-ErrorMsg "Emulator initialization failed with error: $($_.Exception.Message)"
    Write-ErrorMsg "Stack trace: $($_.ScriptStackTrace)"
    Write-Host ""
    Write-Info "Troubleshooting options:"
    Write-Info "1. Run as Administrator: Right-click PowerShell -> Run as administrator"
    Write-Info "2. Use execution policy bypass: powershell -ExecutionPolicy Bypass -File initialize-emulators.ps1"
    Write-Info "3. See TROUBLESHOOTING.md for detailed solutions"
    Write-Host ""
    exit 1
}
