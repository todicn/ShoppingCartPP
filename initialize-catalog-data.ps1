# Initialize Product Catalog Data for local development
# This script creates database structures and populates sample data 
# for Cosmos DB and Redis backends
# Optimized for both VS Code terminal and external PowerShell execution

#Requires -Version 5.1

[CmdletBinding()]
param(
    [ValidateSet("CosmosDB", "Redis", "Both")]
    [string]$Backend = "Both",
    [switch]$SkipSampleData,
    [switch]$ResetData,
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
    Write-Host "Product Catalog Data Initialization Script" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "USAGE:" -ForegroundColor Yellow
    Write-Host "  .\initialize-catalog-data.ps1 [OPTIONS]"
    Write-Host ""
    Write-Host "OPTIONS:" -ForegroundColor Yellow
    Write-Host "  -Backend          Storage backend to initialize (CosmosDB, Redis, Both)"
    Write-Host "                    Default: Both"
    Write-Host "  -SkipSampleData   Skip populating sample data"
    Write-Host "  -ResetData        Reset/recreate all data (delete existing)"
    Write-Host "  -VerboseOutput    Enable verbose output"
    Write-Host "  -Help             Show this help message"
    Write-Host ""
    Write-Host "EXAMPLES:" -ForegroundColor Yellow
    Write-Host "  .\initialize-catalog-data.ps1"
    Write-Host "  .\initialize-catalog-data.ps1 -Backend CosmosDB"
    Write-Host "  .\initialize-catalog-data.ps1 -Backend Redis -SkipSampleData"
    Write-Host "  .\initialize-catalog-data.ps1 -ResetData -VerboseOutput"
    Write-Host ""
    Write-Host "DESCRIPTION:" -ForegroundColor Yellow
    Write-Host "  This script initializes Product Catalog database structures and sample data."
    Write-Host "  For Cosmos DB: Creates database, container, and indexes."
    Write-Host "  For Redis: Verifies connectivity and optionally populates sample data."
    Write-Host ""
    Write-Host "PREREQUISITES:" -ForegroundColor Yellow
    Write-Host "  - Run .\initialize-emulators.ps1 first to start emulators"
    Write-Host "  - Cosmos DB Emulator should be running and accessible"
    Write-Host "  - Redis server should be running (Windows or WSL)"
    Write-Host "  - .NET SDK (for running data initialization app)"
    Write-Host ""
    Write-Host "TROUBLESHOOTING:" -ForegroundColor Yellow
    Write-Host "  If you have issues running this script:"
    Write-Host "  - Ensure emulators are running first (.\initialize-emulators.ps1)"
    Write-Host "  - Use: powershell -ExecutionPolicy Bypass -File initialize-catalog-data.ps1"
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

# Enhanced logging functions
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

# Function to test Cosmos DB Emulator connectivity
function Test-CosmosDbEmulator {
    try {
        Write-Verbose "Testing Cosmos DB Emulator connectivity..."
        $response = Invoke-WebRequest -Uri "https://localhost:8081/_explorer/index.html" -UseBasicParsing -TimeoutSec 10 -SkipCertificateCheck 2>$null
        if ($response.StatusCode -eq 200) {
            Write-Success "Cosmos DB Emulator is accessible"
            return $true
        }
    } catch {
        Write-Warning "Cosmos DB Emulator connectivity test failed: $($_.Exception.Message)"
    }
    return $false
}

# Function to test Redis connectivity
function Test-RedisConnectivity {
    param([bool]$UseWSL = $false)
    
    try {
        Write-Verbose "Testing Redis connectivity..."
        if ($UseWSL) {
            $pingResult = wsl redis-cli ping 2>$null
        } else {
            $redisCmd = Get-Command redis-cli -ErrorAction SilentlyContinue
            if ($redisCmd) {
                $pingResult = redis-cli ping 2>$null
            } else {
                return $false
            }
        }
        
        if ($pingResult -eq "PONG") {
            Write-Success "Redis is accessible"
            return $true
        }
    } catch {
        Write-Warning "Redis connectivity test failed: $($_.Exception.Message)"
    }
    return $false
}

# Function to initialize Cosmos DB structures
function Initialize-CosmosDbStructures {
    param([bool]$ResetData = $false)
    
    Write-Step "Initializing Cosmos DB database and container structures..."
    
    try {
        # We'll use a simple .NET console app approach to create DB structures
        $initCode = @"
using Microsoft.Azure.Cosmos;
using System;
using System.Threading.Tasks;

namespace CatalogDataInit 
{
    class Program 
    {
        private static readonly string ConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private static readonly string DatabaseName = "ProductCatalogDB";
        private static readonly string ContainerName = "Products";
        
        static async Task Main(string[] args)
        {
            bool resetData = args.Length > 0 && args[0].Equals("reset", StringComparison.OrdinalIgnoreCase);
            
            try 
            {
                using var cosmosClient = new CosmosClient(ConnectionString);
                
                // Create or get database
                Console.WriteLine("Creating/getting database: " + DatabaseName);
                var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
                var database = databaseResponse.Database;
                Console.WriteLine("Database ready: " + databaseResponse.StatusCode);
                
                // Delete container if reset requested
                if (resetData) 
                {
                    try 
                    {
                        Console.WriteLine("Resetting container: " + ContainerName);
                        await database.GetContainer(ContainerName).DeleteContainerAsync();
                        Console.WriteLine("Container deleted for reset");
                    } catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) 
                    {
                        Console.WriteLine("Container didn't exist, proceeding with creation");
                    }
                }
                
                // Create container with proper partitioning
                Console.WriteLine("Creating/getting container: " + ContainerName);
                var containerProperties = new ContainerProperties(ContainerName, "/id");
                
                // Configure indexing policy for better performance
                containerProperties.IndexingPolicy.IncludedPaths.Clear();
                containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/name/?" });
                containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/category/?" });
                containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/tags/[]/?", Indexes = { new RangeIndex(DataType.String) { Precision = -1 } } });
                containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/quantity/?" });
                
                var containerResponse = await database.CreateContainerIfNotExistsAsync(containerProperties, 400);
                var container = containerResponse.Container;
                Console.WriteLine("Container ready: " + containerResponse.StatusCode);
                
                Console.WriteLine("SUCCESS: Cosmos DB structures initialized");
            } 
            catch (Exception ex) 
            {
                Console.WriteLine("ERROR: " + ex.Message);
                Environment.Exit(1);
            }
        }
    }
}
"@

        # Create temporary directory for initialization app
        $tempDir = Join-Path $env:TEMP "ProductCatalogInit"
        if (Test-Path $tempDir) {
            Remove-Item $tempDir -Recurse -Force
        }
        New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
        
        # Create project file
        $projectContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.Cosmos" Version="3.35.4" />
  </ItemGroup>
</Project>
"@

        $projectFile = Join-Path $tempDir "CatalogDataInit.csproj"
        $programFile = Join-Path $tempDir "Program.cs"
        
        Set-Content -Path $projectFile -Value $projectContent -Encoding UTF8
        Set-Content -Path $programFile -Value $initCode -Encoding UTF8
        
        Write-Verbose "Created temporary initialization project in: $tempDir"
        
        # Build and run the initialization app
        Write-Step "Building Cosmos DB initialization app..."
        $buildResult = dotnet build $projectFile --nologo --verbosity quiet 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMsg "Failed to build initialization app: $buildResult"
            return $false
        }
        
        Write-Step "Running Cosmos DB structure initialization..."
        $runArgs = if ($ResetData) { "reset" } else { "" }
        $runResult = dotnet run --project $projectFile --no-build --nologo -- $runArgs 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Cosmos DB structures initialized successfully"
            Write-Info "Database: ProductCatalogDB"
            Write-Info "Container: Products"
            Write-Info "Partition Key: /id"
            Write-Info "Indexing: Optimized for name, category, tags, quantity"
            $result = $true
        } else {
            Write-ErrorMsg "Failed to initialize Cosmos DB structures: $runResult"
            $result = $false
        }
        
        # Cleanup
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        return $result
        
    } catch {
        Write-ErrorMsg "Exception during Cosmos DB initialization: $($_.Exception.Message)"
        return $false
    }
}

# Function to populate sample data using the application
function Initialize-SampleData {
    param(
        [string]$Backend,
        [bool]$ResetData = $false
    )
    
    if ($SkipSampleData) {
        Write-Info "Skipping sample data population"
        return $true
    }
    
    Write-Step "Populating sample data for $Backend backend..."
    
    try {
        # Use the actual Product Catalog application to populate data
        $appPath = Join-Path $PSScriptRoot "ProductCatalog"
        
        if (-not (Test-Path $appPath)) {
            Write-ErrorMsg "Product Catalog application not found at: $appPath"
            return $false
        }
        
        # Create a sample data script
        $sampleDataScript = @"
using ProductCatalog.Services;
using ProductCatalog.Exceptions;
using System;

namespace ProductCatalog.DataInit
{
    class SampleDataInitializer
    {
        static void Main(string[] args)
        {
            if (args.Length == 0) {
                Console.WriteLine("Usage: SampleDataInitializer <backend>");
                Environment.Exit(1);
            }
            
            string backend = args[0];
            
            try 
            {
                StorageConfiguration config = backend.ToLower() switch
                {
                    "cosmosdb" => StorageConfigurationHelper.CreateCosmosDbConfiguration(),
                    "redis" => StorageConfigurationHelper.CreateRedisConfiguration(),
                    _ => throw new ArgumentException("Invalid backend: " + backend)
                };

                var factory = new ProductCatalogServiceFactory(config);
                var catalog = factory.CreateService();

                Console.WriteLine("Populating sample data for " + backend + " backend...");
                
                // Add diverse sample products
                catalog.AddProduct("Laptop", 10, "Electronics", new[] { "computer", "portable", "work" });
                catalog.AddProduct("Gaming Mouse", 25, "Electronics", new[] { "computer", "wireless", "gaming" });
                catalog.AddProduct("Mechanical Keyboard", 15, "Electronics", new[] { "computer", "mechanical", "gaming" });
                catalog.AddProduct("4K Monitor", 8, "Electronics", new[] { "computer", "display", "work", "4k" });
                catalog.AddProduct("Wireless Headphones", 20, "Audio", new[] { "wireless", "music", "gaming", "bluetooth" });
                catalog.AddProduct("Coffee Mug", 50, "Office", new[] { "drink", "ceramic", "office" });
                catalog.AddProduct("Adjustable Desk Lamp", 12, "Furniture", new[] { "light", "work", "adjustable" });
                catalog.AddProduct("Ergonomic Chair", 5, "Furniture", new[] { "office", "ergonomic", "comfort" });
                catalog.AddProduct("USB-C Hub", 30, "Electronics", new[] { "computer", "connectivity", "usb" });
                catalog.AddProduct("Bluetooth Speaker", 18, "Audio", new[] { "wireless", "music", "bluetooth", "portable" });
                
                var productCount = catalog.GetProductCount();
                Console.WriteLine("Successfully added " + productCount + " sample products");
                Console.WriteLine("Sample data initialization completed for " + backend);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                Environment.Exit(1);
            }
        }
    }
}
"@

        # Create temporary sample data initializer
        $tempDir = Join-Path $env:TEMP "ProductCatalogSampleData"
        if (Test-Path $tempDir) {
            Remove-Item $tempDir -Recurse -Force
        }
        New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
        
        # Copy the main project references
        $projectContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.Cosmos" Version="3.35.4" />
    <PackageReference Include="StackExchange.Redis" Version="2.6.122" />
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include="$appPath\Models\*.cs" />
    <Compile Include="$appPath\Services\*.cs" />
    <Compile Include="$appPath\Exceptions\*.cs" />
  </ItemGroup>
</Project>
"@

        $projectFile = Join-Path $tempDir "SampleDataInit.csproj"
        $programFile = Join-Path $tempDir "Program.cs"
        
        Set-Content -Path $projectFile -Value $projectContent -Encoding UTF8
        Set-Content -Path $programFile -Value $sampleDataScript -Encoding UTF8
        
        Write-Verbose "Created sample data initializer in: $tempDir"
        
        # Build the sample data app
        Write-Step "Building sample data initializer..."
        $buildResult = dotnet build $projectFile --nologo --verbosity quiet 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to build sample data initializer, using simple approach"
            # Fallback: just run the main app and let user add data manually
            Write-Info "You can add sample data by running the application and selecting the $Backend backend"
            return $true
        }
        
        # Run sample data initialization
        Write-Step "Running sample data initialization for $Backend..."
        $runResult = dotnet run --project $projectFile --no-build --nologo -- $Backend 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Sample data populated successfully for $Backend"
        } else {
            Write-Warning "Sample data initialization had issues: $runResult"
            Write-Info "You can manually add sample data by running the main application"
        }
        
        # Cleanup
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        return $true
        
    } catch {
        Write-Warning "Exception during sample data initialization: $($_.Exception.Message)"
        Write-Info "You can manually add sample data by running the main application"
        return $true  # Don't fail the whole process for sample data issues
    }
}

# Main execution
try {
    Write-Host ""
    Write-Host "=== Product Catalog Data Initialization ===" -ForegroundColor Magenta
    Write-Host "This script will initialize database structures and sample data" -ForegroundColor Cyan
    Write-Host "Backend(s): $Backend" -ForegroundColor Cyan
    Write-Host "Start time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
    Write-Host ""

    # Prerequisites check
    Write-Section "Prerequisites Check"
    
    # Check .NET SDK
    $dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnetCmd) {
        try {
            $dotnetVersion = dotnet --version 2>$null
            Write-Success ".NET SDK available: $dotnetVersion"
        } catch {
            Write-Warning ".NET SDK available but version check failed"
        }
    } else {
        Write-ErrorMsg ".NET SDK not found. Please install .NET SDK 8.0 or later"
        Write-Info "Download from: https://dotnet.microsoft.com/download"
        exit 1
    }
    
    # Check if project exists
    $projectPath = Join-Path $PSScriptRoot "ProductCatalog\ProductCatalog.csproj"
    if (-not (Test-Path $projectPath)) {
        Write-ErrorMsg "Product Catalog project not found at: $projectPath"
        Write-Info "Ensure you're running this script from the solution root directory"
        exit 1
    }
    Write-Success "Product Catalog project found"

    $allSuccess = $true

    # Initialize Cosmos DB if requested
    if ($Backend -eq "CosmosDB" -or $Backend -eq "Both") {
        Write-Section "Cosmos DB Initialization"
        
        if (Test-CosmosDbEmulator) {
            $cosmosSuccess = Initialize-CosmosDbStructures -ResetData $ResetData
            if ($cosmosSuccess) {
                $sampleSuccess = Initialize-SampleData -Backend "CosmosDB" -ResetData $ResetData
                if (-not $sampleSuccess -and -not $SkipSampleData) {
                    Write-Warning "Sample data initialization had issues for Cosmos DB"
                }
            } else {
                Write-ErrorMsg "Failed to initialize Cosmos DB structures"
                $allSuccess = $false
            }
        } else {
            Write-ErrorMsg "Cosmos DB Emulator is not accessible"
            Write-Info "Please run .\initialize-emulators.ps1 first to start the emulator"
            $allSuccess = $false
        }
    }
    
    # Initialize Redis if requested
    if ($Backend -eq "Redis" -or $Backend -eq "Both") {
        Write-Section "Redis Initialization"
        
        $redisAccessible = $false
        $useWSL = $false
        
        # Check Windows Redis first
        if (Test-RedisConnectivity) {
            $redisAccessible = $true
        } else {
            # Check WSL Redis
            $wslCmd = Get-Command wsl -ErrorAction SilentlyContinue
            if ($wslCmd) {
                if (Test-RedisConnectivity -UseWSL $true) {
                    $redisAccessible = $true
                    $useWSL = $true
                }
            }
        }
        
        if ($redisAccessible) {
            Write-Success "Redis is accessible $(if ($useWSL) { "(via WSL)" } else { "(Windows)" })"
            
            if ($ResetData) {
                Write-Step "Clearing Redis data..."
                try {
                    if ($useWSL) {
                        wsl redis-cli FLUSHDB 2>$null
                    } else {
                        redis-cli FLUSHDB 2>$null
                    }
                    Write-Success "Redis data cleared"
                } catch {
                    Write-Warning "Could not clear Redis data: $($_.Exception.Message)"
                }
            }
            
            # Redis doesn't need structure initialization, just sample data
            $sampleSuccess = Initialize-SampleData -Backend "Redis" -ResetData $ResetData
            if (-not $sampleSuccess -and -not $SkipSampleData) {
                Write-Warning "Sample data initialization had issues for Redis"
            }
        } else {
            Write-ErrorMsg "Redis is not accessible"
            Write-Info "Please run .\initialize-emulators.ps1 first to start Redis"
            $allSuccess = $false
        }
    }

    # Summary
    Write-Section "Initialization Summary"
    
    if ($allSuccess) {
        Write-Success "Data initialization completed successfully"
    } else {
        Write-Warning "Data initialization completed with some issues"
    }
    
    Write-Info ""
    Write-Info "Next steps:"
    Write-Info "1. Run the Product Catalog application: dotnet run --project ProductCatalog"
    Write-Info "2. Select your preferred backend when prompted"
    Write-Info "3. Explore the initialized data and add more products as needed"
    
    if ($Backend -eq "CosmosDB" -or $Backend -eq "Both") {
        Write-Info ""
        Write-Info "Cosmos DB Resources:"
        Write-Info "  Data Explorer: https://localhost:8081/_explorer/index.html"
        Write-Info "  Database: ProductCatalogDB"
        Write-Info "  Container: Products"
    }
    
    if ($Backend -eq "Redis" -or $Backend -eq "Both") {
        Write-Info ""
        Write-Info "Redis Configuration:"
        Write-Info "  Connection: localhost:6379"
        Write-Info "  Key Prefix: productcatalog:"
        if ($useWSL) {
            Write-Info "  Access via: wsl redis-cli"
        } else {
            Write-Info "  Access via: redis-cli"
        }
    }
    
    Write-Host ""
    Write-Host "End time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
    Write-Host ""

} catch {
    Write-ErrorMsg "Data initialization failed with error: $($_.Exception.Message)"
    Write-ErrorMsg "Stack trace: $($_.ScriptStackTrace)"
    Write-Host ""
    Write-Info "Troubleshooting options:"
    Write-Info "1. Ensure emulators are running: .\initialize-emulators.ps1"
    Write-Info "2. Check .NET SDK installation: dotnet --version"
    Write-Info "3. Run as Administrator if needed"
    Write-Info "4. Use execution policy bypass: powershell -ExecutionPolicy Bypass -File initialize-catalog-data.ps1"
    Write-Info "5. See TROUBLESHOOTING.md for detailed solutions"
    Write-Host ""
    exit 1
}
