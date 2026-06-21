# Deployment Quick Checklist & Commands

## 🚀 CHOOSE YOUR DEPLOYMENT PATH

### Path 1: Azure App Service (Easiest & Recommended) ⭐
### Path 2: VPS (DigitalOcean/Linode) (Cheapest)
### Path 3: Docker (Most Flexible)

---

## ✅ PHASE 1: PRE-DEPLOYMENT (Before Deploying)

### Code Quality Check
```powershell
# Clean build
dotnet clean
dotnet build -c Release

# Check for compilation errors
if ($LASTEXITCODE -ne 0) { Write-Host "❌ Build failed!"; exit 1 }
else { Write-Host "✅ Build successful" }

# Run tests (if you have any)
dotnet test

# Check no Debug code remains
$debugFiles = Get-ChildItem -Recurse -Include "*.cs" | 
	Select-String -Pattern "DEBUG|Console.WriteLine|throw new NotImplementedException" |
	Where-Object { $_.Path -notlike "*bin*" -and $_.Path -notlike "*obj*" }
if ($debugFiles) { Write-Host "⚠️ Remove debug code from: $($debugFiles | Select Path -Unique)" }
```

### Configuration Files Prep
```powershell
# Create appsettings.Production.json
$content = @{
	"ConnectionStrings" = @{
		"DefaultConnection" = "YOUR_PRODUCTION_CONNECTION_STRING"
	}
	"Logging" = @{
		"LogLevel" = @{
			"Default" = "Information"
			"Microsoft" = "Warning"
		}
	}
	"AllowedHosts" = "yourdomain.com,www.yourdomain.com"
} | ConvertTo-Json

$content | Out-File -Encoding UTF8 "Website Quản Lý Nhân Sự\appsettings.Production.json"
Write-Host "✅ Created appsettings.Production.json"
```

### Database Backup (Local)
```powershell
# Backup your development database first!
# SQL Server Management Studio → Right-click database → Tasks → Backup
# Or via T-SQL:
$query = @"
BACKUP DATABASE [WebsiteQL_NhanSu] 
TO DISK = 'C:\Backups\WebsiteQL_NhanSu_PreDeployment.bak'
WITH FORMAT, INIT;
"@

# Save and execute in SSMS
```

### Security Review Checklist
```
[ ] No API keys in code
[ ] No passwords in config files  
[ ] No connection strings with credentials in appsettings.json
[ ] No DEBUG #if statements left
[ ] HTTPS enabled in production
[ ] SQL Injection: Using EF Core (parameterized queries)
[ ] XSS: Input validation added
[ ] CSRF: Token middleware enabled
[ ] Authentication: Login page working
[ ] Authorization: Role-based access implemented
[ ] CORS: Only trusted origins allowed
[ ] Secrets: Stored in environment variables/Key Vault
```

---

## 🎯 PHASE 2A: DEPLOYMENT TO AZURE (Recommended)

### Step 1: Create Azure Resources

```powershell
# Install Azure CLI if not done
# Download from: https://aka.ms/installazurecliwindows

# Or use choco:
choco install azure-cli

# Login
az login

# Set variables
$resourceGroup = "RG-WebsiteQL"
$appServicePlan = "AppPlan-WebsiteQL"
$appService = "websiteql-nhansu"
$sqlServer = "websiteql-db"
$database = "WebsiteQL_NhanSu"
$location = "southeastasia"  # Singapore
$sku = "B1"  # Basic tier, $10/month
$sqlAdmin = "dbadmin"
$sqlPassword = "YOUR_STRONG_PASSWORD_MIN_20_CHARS"

# Create Resource Group
az group create --name $resourceGroup --location $location
Write-Host "✅ Resource Group created"

# Create App Service Plan
az appservice plan create `
	--name $appServicePlan `
	--resource-group $resourceGroup `
	--sku $sku `
	--is-linux
Write-Host "✅ App Service Plan created"

# Create App Service
az webapp create `
	--resource-group $resourceGroup `
	--plan $appServicePlan `
	--name $appService `
	--runtime "DOTNET|10.0"
Write-Host "✅ App Service created"

# Create SQL Server
az sql server create `
	--name $sqlServer `
	--resource-group $resourceGroup `
	--location $location `
	--admin-user $sqlAdmin `
	--admin-password $sqlPassword
Write-Host "✅ SQL Server created"

# Create SQL Database
az sql db create `
	--resource-group $resourceGroup `
	--server $sqlServer `
	--name $database
Write-Host "✅ SQL Database created"

# Get connection string
$connectionString = az sql db show-connection-string `
	--client ado.net `
	--name $database `
	--server $sqlServer `
	--output tsv | ForEach-Object { $_ -replace "<username>", $sqlAdmin -replace "<password>", $sqlPassword }

Write-Host "📝 Connection String: $connectionString"
```

### Step 2: Configure App Service Settings

```powershell
# Set connection string
az webapp config connection-string set `
	--resource-group $resourceGroup `
	--name $appService `
	--connection-string-type SQLAzure `
	--settings DefaultConnection=$connectionString
Write-Host "✅ Connection string set"

# Set application settings (environment variables)
az webapp config appsettings set `
	--resource-group $resourceGroup `
	--name $appService `
	--settings `
		ASPNETCORE_ENVIRONMENT="Production" `
		DOTNET_ENVIRONMENT="Production" `
		JWT__SECRET="YOUR_PRODUCTION_JWT_SECRET_MIN_32_CHARS" `
		Email__SendGridApiKey="YOUR_SENDGRID_KEY" `
		Logging__LogLevel__Default="Information"
Write-Host "✅ Application settings configured"

# Enable HTTPS only
az webapp update `
	--resource-group $resourceGroup `
	--name $appService `
	--https-only true
Write-Host "✅ HTTPS only enabled"
```

### Step 3: Build & Publish

```powershell
cd "C:\Users\nguye\source\repos\Website Quản Lý Nhân Sự"

# Publish release build
dotnet publish -c Release -o .\publish
Write-Host "✅ Published to .\publish folder"

# Compress for upload (optional)
Compress-Archive -Path .\publish\* -DestinationPath .\publish.zip
Write-Host "✅ Created publish.zip"

# Deploy via zip
az webapp deployment source config-zip `
	--resource-group $resourceGroup `
	--name $appService `
	--src .\publish.zip
Write-Host "✅ Deployed to Azure App Service"

# Clean up
Remove-Item .\publish -Recurse -Force
Remove-Item .\publish.zip -Force
```

### Step 4: Run Database Migrations

```powershell
# Get the kudu URL (SSH connection)
$kuduUrl = "https://$($appService).scm.azurewebsites.net"
Write-Host "Kudu URL: $kuduUrl"

# SSH into app (alternative methods)

# Method 1: Using Azure Portal
# App Service → Advanced Tools → Go
# Or SSH directly in terminal:

az webapp remote-connection create `
	--resource-group $resourceGroup `
	--name $appService

# Once connected via SSH, run:
# cd site/wwwroot
# dotnet ef database update -c ApplicationDbContext

# Method 2: From local machine with app settings
# cd "C:\Users\nguye\source\repos\Website Quản Lý Nhân Sự\Website Quản Lý Nhân Sự"
# dotnet ef database update -c ApplicationDbContext --connection $connectionString

Write-Host "✅ Database migrations applied"
```

### Step 5: Verify Deployment

```powershell
# Get app URL
$appUrl = "https://$appService.azurewebsites.net"
Write-Host "🌐 App URL: $appUrl"

# Test connectivity
$response = Invoke-WebRequest -Uri $appUrl -SkipCertificateCheck
if ($response.StatusCode -eq 200) {
	Write-Host "✅ App is responding (HTTP 200)"
} else {
	Write-Host "❌ App returned: $($response.StatusCode)"
}

# View logs
Write-Host "📋 Recent logs:"
az webapp log tail --resource-group $resourceGroup --name $appService --lines 50
```

### Step 6: Setup Custom Domain

```powershell
# 1. Update DNS records at your domain registrar
# CNAME: www.yourdomain.com → websiteql-nhansu.azurewebsites.net
# A record: yourdomain.com → Azure IP (from portal)

# 2. Add custom domain to App Service
az webapp config hostname add `
	--resource-group $resourceGroup `
	--webapp-name $appService `
	--hostname www.yourdomain.com
Write-Host "✅ Custom domain added"

# 3. Setup SSL binding (Azure-managed certificate - free)
# In Azure Portal:
# App Service → TLS/SSL settings → Add App Service Managed Certificate
# Then bind to custom domain

Write-Host "✅ HTTPS ready for custom domain"
```

---

## 🎯 PHASE 2B: DEPLOYMENT TO VPS (DigitalOcean Example)

### Step 1: Create VPS Droplet

```powershell
# Using DigitalOcean CLI (doctl)
# Install: choco install doctl

# Set variables
$vpsName = "WebsiteQL"
$region = "sgp1"  # Singapore
$size = "s-2vcpu-4gb"  # 2GB RAM, ~$20/month
$image = "windows-server-2022-x64-standard"

# Create droplet (requires DigitalOcean account & token)
doctl compute droplet create $vpsName `
	--region $region `
	--size $size `
	--image $image
Write-Host "✅ VPS Droplet created, wait 2-3 minutes for startup"

# Get IP address
$vpsIP = doctl compute droplet list --format Name,PublicIPv4 --no-header | 
	Select-String $vpsName | 
	Select-Object -First 1 | 
	ForEach-Object { ($_ -split '\s+')[1] }

Write-Host "🌐 VPS IP: $vpsIP"
```

### Step 2: Initial VPS Setup

```powershell
# RDP to your VPS
Write-Host "Connecting via RDP to $vpsIP"
mstsc /v:$vpsIP

# Once connected, run in PowerShell as Admin:

# 1. Install .NET 10 Runtime
$dotnetUrl = "https://dot.net/v1/dotnet-install.ps1"
Invoke-WebRequest -Uri $dotnetUrl -OutFile dotnet-install.ps1
.\dotnet-install.ps1 -Channel 10.0
Remove-Item dotnet-install.ps1

# Verify installation
dotnet --version
Write-Host "✅ .NET 10 installed"

# 2. Install Hosting Bundle
$hostingUrl = "https://download.visualstudio.microsoft.com/download/pr/YOUR-URL/dotnet-hosting-10.0.0-win.exe"
# Download from: https://dotnet.microsoft.com/download/dotnet/10.0
# (Hosting Bundle for Windows Server)

# 3. Install IIS
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-CommonHttpFeatures, IIS-Management, IIS-ManagementConsole, NetFx4Extended-ASPNET45, IIS-NetFxExtensibility45, IIS-ASPNET45, IIS-Rewrite, IIS-URLRewrite -All
Write-Host "✅ IIS installed"

# 4. Restart server
Restart-Computer -Force
```

### Step 3: Deploy Application

```powershell
cd "C:\Users\nguye\source\repos\Website Quản Lý Nhân Sự"

# Publish release build
dotnet publish -c Release -o .\publish

# Copy to VPS (via RDP or SCP)
# Option 1: RDP File Manager
# Copy .\publish folder to VPS: C:\inetpub\wwwroot\WebsiteQL

# Option 2: PowerShell SCP (requires SSH)
# scp -r .\publish Administrator@$vpsIP:C:\inetpub\wwwroot\WebsiteQL

Write-Host "✅ Application uploaded to VPS"

# On VPS in IIS Manager:
# 1. Create Application Pool "WebsiteQL"
#    - Runtime version: No Managed Code
#    - Managed pipeline: Integrated
# 2. Create Website "WebsiteQL"
#    - Physical path: C:\inetpub\wwwroot\WebsiteQL
#    - Binding: HTTP/HTTPS port 80/443
#    - App pool: WebsiteQL
# 3. Configure app pool identity (Application Pool → Advanced Settings)
#    - Identity: ApplicationPoolIdentity
```

### Step 4: Database Setup on VPS

```powershell
# Option A: SQL Server Express (free, on VPS)
# Download from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
# Install with default settings

# Option B: Azure SQL Database (recommended for VPS)
# Use the connection string from Phase 2A

# Run migrations:
# Connect to VPS via RDP
# Open PowerShell as Admin:

cd C:\inetpub\wwwroot\WebsiteQL
dotnet ef database update -c ApplicationDbContext --connection "YOUR_CONNECTION_STRING"

Write-Host "✅ Database initialized"
```

### Step 5: Setup SSL Certificate

```powershell
# Download Win-ACME (free Let's Encrypt client)
$winAcmeUrl = "https://www.win-acme.com/download"
# Or: choco install win-acme

# On VPS:
# Run: wacs.exe
# Follow prompts:
# 1. N - Create new certificate
# 2. I - IIS Web Server
# 3. Select your domain
# 4. Y - Add HTTPS binding
# Auto-renews every 60 days

Write-Host "✅ SSL Certificate installed"
```

### Step 6: Configure Firewall

```powershell
# On VPS, open Windows Firewall for HTTP/HTTPS:

New-NetFirewallRule -DisplayName "Allow HTTP" `
	-Direction Inbound -Action Allow -Protocol TCP -LocalPort 80

New-NetFirewallRule -DisplayName "Allow HTTPS" `
	-Direction Inbound -Action Allow -Protocol TCP -LocalPort 443

Write-Host "✅ Firewall configured"
```

### Step 7: Test VPS Deployment

```powershell
# From your local machine:
$vpsAppUrl = "https://$vpsIP"

$response = Invoke-WebRequest -Uri $vpsAppUrl -SkipCertificateCheck
if ($response.StatusCode -eq 200) {
	Write-Host "✅ VPS app responding"
} else {
	Write-Host "❌ VPS app error: $($response.StatusCode)"
}
```

---

## 🎯 PHASE 2C: DEPLOYMENT WITH DOCKER

### Step 1: Create Docker Files

```powershell
# In project root, create Dockerfile:
$dockerfile = @"
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Website Quản Lý Nhân Sự/Website Quản Lý Nhân Sự.csproj", "Website Quản Lý Nhân Sự/"]
RUN dotnet restore "Website Quản Lý Nhân Sự/Website Quản Lý Nhân Sự.csproj"
COPY . .
WORKDIR "/src/Website Quản Lý Nhân Sự"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80 443
ENTRYPOINT ["dotnet", "Website Quản Lý Nhân Sự.dll"]
"@

$dockerfile | Out-File -Encoding UTF8 "Dockerfile"
Write-Host "✅ Created Dockerfile"

# Create docker-compose.yml:
$compose = @"
version: '3.8'

services:
  web:
	build: .
	container_name: websiteql-web
	ports:
	  - "80:80"
	  - "443:443"
	environment:
	  - ASPNETCORE_ENVIRONMENT=Production
	  - ConnectionStrings__DefaultConnection=Server=db;Database=WebsiteQL_NhanSu;User Id=sa;Password=YourStrong@Pass123;Encrypt=false;
	depends_on:
	  - db
	volumes:
	  - ./uploads:/app/wwwroot/uploads
	restart: unless-stopped

  db:
	image: mcr.microsoft.com/mssql/server:2022-latest
	container_name: websiteql-db
	environment:
	  - SA_PASSWORD=YourStrong@Pass123
	  - ACCEPT_EULA=Y
	ports:
	  - "1433:1433"
	volumes:
	  - mssql_data:/var/opt/mssql
	restart: unless-stopped

volumes:
  mssql_data:
"@

$compose | Out-File -Encoding UTF8 "docker-compose.yml"
Write-Host "✅ Created docker-compose.yml"
```

### Step 2: Build & Deploy Docker

```powershell
# Install Docker Desktop if not done
# Download: https://www.docker.com/products/docker-desktop

# Build and start containers
docker-compose up -d
Write-Host "✅ Docker containers started"

# Wait for services to be ready
Start-Sleep -Seconds 10

# Run migrations
docker exec websiteql-web dotnet ef database update -c ApplicationDbContext
Write-Host "✅ Database initialized"

# Check logs
docker-compose logs -f web
Write-Host "✅ View app logs above"

# Access app
# Browser: http://localhost or https://localhost (if SSL configured)
```

### Step 3: Deploy to Docker Container Hosting

```powershell
# Option A: Deploy to Heroku
# Install Heroku CLI
# heroku login
# heroku create websiteql
# heroku container:push web
# heroku container:release web

# Option B: Deploy to Azure Container Registry
az acr create --resource-group $resourceGroup --name websiteqlacr --sku Basic
docker tag websiteql:latest websiteqlacr.azurecr.io/websiteql:latest
az acr build --registry websiteqlacr --image websiteql:latest .

Write-Host "✅ Container pushed to registry"
```

---

## ✅ PHASE 3: POST-DEPLOYMENT VERIFICATION

```powershell
# Test main features
$appUrl = "https://yourdomain.com"  # or your app URL

# 1. Homepage loads
Write-Host "Testing homepage..."
$response = Invoke-WebRequest -Uri $appUrl
if ($response.StatusCode -eq 200) { Write-Host "✅ Homepage loads" }

# 2. Check HTTPS redirect
$httpUrl = "http://yourdomain.com"
$response = Invoke-WebRequest -Uri $httpUrl -SkipHttpErrorCheck -FollowRelLink
if ($response.BaseResponse.RequestMessage.RequestUri.Scheme -eq "https") { 
	Write-Host "✅ HTTP redirects to HTTPS" 
}

# 3. Check database connectivity
# Login with test account and verify data loads

# 4. Check logs for errors
Write-Host "✅ All smoke tests passed"
```

---

## 🔧 TROUBLESHOOTING COMMANDS

### Azure App Service Issues

```powershell
# Check app status
az webapp show --resource-group $resourceGroup --name $appService --query state

# Restart app
az webapp restart --resource-group $resourceGroup --name $appService

# View logs
az webapp log tail --resource-group $resourceGroup --name $appService

# Clear deployment cache
az webapp deployment slot config set --resource-group $resourceGroup --name $appService --auto-swap-slot-name

# Check app settings
az webapp config appsettings list --resource-group $resourceGroup --name $appService

# Rollback last deployment
az webapp deployment slot swap --resource-group $resourceGroup --name $appService --slot staging
```

### VPS/IIS Issues

```powershell
# Check if app pool is running
Get-WebAppPoolState "WebsiteQL"

# Restart app pool
Restart-WebAppPool -Name "WebsiteQL"

# Check app pool logs
Get-Content C:\inetpub\logs\LogFiles\W3SVC1\u_ex*.log | Select-Object -Last 20

# Check IIS bindings
Get-WebBinding -Name "WebsiteQL"

# Check Windows Event Viewer
Get-EventLog -LogName Application -Newest 20 | Where-Object { $_.Source -like "*ASP*" -or $_.Source -like "*IIS*" }
```

### Docker Issues

```powershell
# Check container status
docker ps -a

# View container logs
docker logs websiteql-web

# Restart containers
docker-compose restart

# Stop and remove
docker-compose down

# Rebuild
docker-compose build --no-cache
docker-compose up -d
```

### Database Issues

```powershell
# Test connection
sqlcmd -S SERVER_NAME -U USERNAME -P PASSWORD -Q "SELECT 1"

# Run migrations (if failed)
dotnet ef database update -c ApplicationDbContext --verbose

# Check migration history
sqlcmd -S SERVER_NAME -U USERNAME -P PASSWORD -Q "SELECT * FROM __EFMigrationsHistory"

# Rollback last migration (if needed)
dotnet ef migrations remove --project "Website Quản Lý Nhân Sự"
```

---

## 📊 MONITORING & MAINTENANCE

### Daily Checks

```powershell
# Check application health
$appUrl = "https://yourdomain.com/health"
Invoke-RestMethod -Uri $appUrl

# Check error logs
az webapp log tail --resource-group $resourceGroup --name $appService --lines 100

# Monitor from Azure Portal
# App Service → Metrics → Look for:
#   - CPU %
#   - Memory %
#   - HTTP errors
#   - Response time
```

### Weekly Maintenance

```powershell
# Database maintenance
sqlcmd -S SERVER_NAME -U USERNAME -P PASSWORD -Q @"
-- Rebuild indexes
ALTER INDEX ALL ON [Employees] REBUILD;
ALTER INDEX ALL ON [Attendances] REBUILD;

-- Update statistics
UPDATE STATISTICS [Employees];
UPDATE STATISTICS [Attendances];
"@

# Check storage usage
az sql db show --resource-group $resourceGroup --server $sqlServer --name $database

# Review security alerts
# Azure Portal → Security Center → Recommendations
```

### Monthly Tasks

```powershell
# Database backup
$backupUrl = "https://YOUR_BACKUP_STORAGE.blob.core.windows.net"

# Cost analysis
az costmanagement query --timeframe Last30Days --type actual

# Update packages
dotnet outdated  # Install: dotnet tool install --global dotnet-outdated-tool

# Security audit
# Review access logs
# Check for failed login attempts
# Verify all users still need access
```

---

## 🆘 EMERGENCY PROCEDURES

### Rollback if Deployment Breaks

```powershell
# Azure App Service
az webapp deployment slot swap --resource-group $resourceGroup --name $appService --slot staging

# VPS - Restore from backup
# Restore backup to app folder

# Docker - Revert to previous image
docker-compose down
docker pull websiteqlacr.azurecr.io/websiteql:previous-tag
docker-compose up -d
```

### Restore Database from Backup

```powershell
# Azure SQL
# Portal → Backups → Restore to point-in-time

# VPS/Local
sqlcmd -S SERVER -U USERNAME -P PASSWORD -Q @"
RESTORE DATABASE [WebsiteQL_NhanSu] 
FROM DISK = 'C:\Backups\backup.bak'
WITH REPLACE;
"@
```

### Scale Up if Performance Issues

```powershell
# Azure App Service - Increase tier
az appservice plan update --name $appServicePlan --resource-group $resourceGroup --sku B2

# VPS - Increase droplet size
doctl compute droplet resize $vpsName --size s-4vcpu-8gb
```

---

## ✅ FINAL DEPLOYMENT CHECKLIST

```
BEFORE DEPLOYMENT:
[ ] Code compiled successfully
[ ] All tests passing
[ ] No debug code left
[ ] appsettings.Production.json created
[ ] Connection string validated
[ ] Secrets stored in environment
[ ] Database backup created
[ ] Team notified

DEPLOYMENT:
[ ] Publish build created
[ ] Infrastructure provisioned
[ ] Application deployed
[ ] Database migrations run
[ ] SSL certificate configured
[ ] Domain DNS configured
[ ] Custom domain verified

POST-DEPLOYMENT:
[ ] App loads successfully
[ ] Login/auth working
[ ] Database connected
[ ] Logs monitored
[ ] Performance acceptable
[ ] Users notified
[ ] Team on standby

WEEK 1 MONITORING:
[ ] Error logs reviewed daily
[ ] Performance metrics normal
[ ] User feedback collected
[ ] Critical issues fixed
[ ] Non-critical issues logged
```

---

**Document Version**: 1.0
**Created**: 2025-06-21
**Status**: Ready for Production ✅
