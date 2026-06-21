# Web Deployment Guide - Website Quản Lý Nhân Sự

## 1. Deployment Architecture Options

### Option A: Azure App Service (Recommended for beginners)
✅ **Ưu điểm**:
- Quản lý tự động (auto-scaling, backups)
- SSL/TLS miễn phí
- CDN tích hợp
- Dễ dàng update & rollback
- Giám sát tích hợp

❌ **Nhược điểm**:
- Chi phí cao hơn (từ $10/tháng)

### Option B: VPS (Virtual Private Server)
✅ **Ưu điểm**:
- Giá rẻ ($3-10/tháng)
- Toàn quyền kiểm soát
- Linh hoạt cấu hình

❌ **Nhược điểm**:
- Quản lý phức tạp
- Cần kỹ năng DevOps
- Phải tự quản lý security, backups

### Option C: Shared Hosting
✅ **Ưu điểm**:
- Giá rẻ nhất ($2-5/tháng)
- Dễ setup

❌ **Nhược điểm**:
- Hiệu năng kém
- Hỗ trợ .NET kém
- Khó mở rộng

---

## 2. Pre-Deployment Checklist (QUAN TRỌNG)

### 2.1 Code & Database Preparation

- [ ] ✅ Build solution - không có compilation errors
- [ ] ✅ Run migrations locally - thực hiện `dotnet ef database update`
- [ ] ✅ Test seeding - kiểm tra dữ liệu demo load thành công
- [ ] ✅ Run unit tests - không có failing tests
- [ ] ✅ Remove debug code - không có `Console.WriteLine()` hoặc `DEBUG`
- [ ] ✅ Code review - kiểm tra logic & security
- [ ] ✅ Database backup - backup local DB trước khi deploy

### 2.2 Configuration & Secrets

- [ ] ✅ appsettings.json - kiểm tra development-only settings
- [ ] ✅ Remove sensitive data - không commit secrets, API keys
- [ ] ✅ Environment-specific configs - chuẩn bị appsettings.Production.json
- [ ] ✅ Connection strings - chuẩn bị production connection string
- [ ] ✅ Email credentials - nếu có gửi email, chuẩn bị SMTP credentials
- [ ] ✅ JWT/Auth keys - generate production keys khác development
- [ ] ✅ CORS settings - configure cho production domain
- [ ] ✅ Logging - setup structured logging cho production

### 2.3 Security Review

- [ ] ✅ SQL Injection prevention - sử dụng parameterized queries (EF Core đã handle)
- [ ] ✅ XSS prevention - encode output, CSP headers
- [ ] ✅ CSRF protection - verify token middleware
- [ ] ✅ Password hashing - ASP.NET Identity đã handle
- [ ] ✅ HTTPS only - cấu hình redirect HTTP → HTTPS
- [ ] ✅ Input validation - validate mọi user input
- [ ] ✅ Authentication - verify login flow
- [ ] ✅ Authorization - kiểm tra role-based access
- [ ] ✅ Rate limiting - prevent brute force attacks
- [ ] ✅ Headers security - add security headers (X-Frame-Options, etc.)

### 2.4 Performance Optimization

- [ ] ✅ Database indexes - verify tất cả indexes đã tạo
- [ ] ✅ Query optimization - không có N+1 queries, use `Include()`
- [ ] ✅ Caching - enable output caching nếu cần
- [ ] ✅ Static files - setup minification/compression
- [ ] ✅ Connection pooling - enable database connection pooling
- [ ] ✅ Logging level - set appropriate log level (Warning/Error in prod)

### 2.5 Infrastructure Preparation

- [ ] ✅ Domain name - đăng ký domain
- [ ] ✅ SSL certificate - prepare SSL/TLS cert
- [ ] ✅ Email service - setup (SendGrid, Office 365, Gmail App Password)
- [ ] ✅ CDN - if needed
- [ ] ✅ Monitoring - setup Application Insights or similar
- [ ] ✅ Backup strategy - plan database backup schedule
- [ ] ✅ DNS - configure DNS records

---

## 3. Chuẩn Bị Configuration Files

### 3.1 appsettings.Production.json (New File)

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=YOUR_PRODUCTION_DB_SERVER;Database=WebsiteQL_NhanSu;User Id=sa;Password=YOUR_STRONG_PASSWORD;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"
  },
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft": "Warning",
	  "Microsoft.EntityFrameworkCore": "Warning"
	}
  },
  "AllowedHosts": "yourdomain.com,www.yourdomain.com",
  "Jwt": {
	"Secret": "YOUR_PRODUCTION_JWT_SECRET_MIN_32_CHARS_LONG_VERY_SECURE",
	"Issuer": "yourdomain.com",
	"Audience": "yourdomain.com",
	"ExpirationMinutes": 60
  },
  "Email": {
	"Provider": "SendGrid",
	"SendGridApiKey": "YOUR_SENDGRID_API_KEY",
	"FromAddress": "noreply@yourdomain.com",
	"FromName": "Website Quản Lý Nhân Sự"
  },
  "Cors": {
	"AllowedOrigins": ["https://yourdomain.com", "https://www.yourdomain.com"]
  },
  "Security": {
	"EnableHttps": true,
	"HttpsRedirect": true
  }
}
```

### 3.2 Program.cs - Production Configuration

```csharp
// Add to Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

// Load environment-specific appsettings
builder.Configuration
	.SetBasePath(builder.Environment.ContentRootPath)
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
	.AddEnvironmentVariables();

// Configure database
if (builder.Environment.IsProduction())
{
	// Production: SQL Server
	var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
	builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
		options.UseSqlServer(
			connectionString,
			sqlOptions => sqlOptions.CommandTimeout(30)
		)
	);
}

// Add security headers middleware
builder.Services.AddHsts(options =>
{
	options.Preload = true;
	options.IncludeSubDomains = true;
	options.MaxAge = TimeSpan.FromDays(365);
});

// ... rest of configuration

var app = builder.Build();

// Middleware order (IMPORTANT)
if (app.Environment.IsProduction())
{
	// HTTPS redirect
	app.UseHttpsRedirection();

	// Security headers
	app.UseHsts();

	// Add security headers manually
	app.Use(async (context, next) =>
	{
		context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
		context.Response.Headers.Add("X-Frame-Options", "DENY");
		context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
		context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
		await next();
	});
}
else
{
	app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.Run();
```

### 3.3 .gitignore - Never commit secrets

```
# Secrets & Keys
appsettings.*.json
*.pem
*.pfx
*.key
.env
.env.local
.env.*.local

# Build outputs
bin/
obj/
*.dll
*.exe

# Database files
*.mdf
*.ldf
*.db
*.sqlite

# IDE
.vs/
.vscode/
*.user
*.sln.docstates

# Logs
logs/
*.log
```

---

## 4. Deployment Scenario A: Azure App Service (Recommended)

### 4.1 Create Azure App Service

**Step 1: Portal Setup**
```
1. Go to portal.azure.com
2. Create resource → App Service
3. Fill details:
   - Resource Group: Create new (RG-WebsiteQL)
   - Name: websiteql-nhansu (must be globally unique)
   - Runtime stack: .NET 10
   - Region: Choose nearest (Southeast Asia = Singapore)
   - Pricing tier: B1 (Basic) - $10/month minimum
4. Click Create
```

**Step 2: Setup Database**
```
1. Create resource → SQL Server
2. Configure:
   - Server name: websiteql-db (globally unique)
   - Location: Same as App Service (Southeast Asia)
   - Authentication: SQL Authentication
   - Admin login: dbadmin
   - Password: YOUR_STRONG_PASSWORD (20+ chars, mix case/numbers/symbols)
3. Firewall: Add Current IP or "0.0.0.0" for all (less secure)
4. Create database: WebsiteQL_NhanSu
5. Note: Connection string from "Connection strings" blade
```

**Step 3: Configure Connection String in App Service**
```
1. Go to App Service → Settings → Configuration
2. Add Connection Strings:
   - Name: DefaultConnection
   - Value: Server=tcp:websiteql-db.database.windows.net,1433;Initial Catalog=WebsiteQL_NhanSu;Persist Security Info=False;User ID=dbadmin;Password=YOUR_STRONG_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;Connection Timeout=30;
   - Type: SQLAzure
3. Click Save
```

**Step 4: Deploy from Visual Studio**

```powershell
# Terminal: Publish from Visual Studio
cd "C:\Users\nguye\source\repos\Website Quản Lý Nhân Sự"

# Method 1: Right-click in Visual Studio
# Project (Website Quản Lý Nhân Sự) → Publish...
# Select "Azure"
# Select "Azure App Service (Windows)"
# Choose existing service (websiteql-nhansu)
# Finish

# Method 2: Command Line
dotnet publish -c Release -o .\publish

# Then upload publish folder to Azure App Service via:
# Deployment Center → GitHub/Local Git/Zip Upload
```

**Step 5: Run EF Migrations on Azure**

```powershell
# Install Azure CLI if not done
# Then authenticate
az login

# Option 1: Using SQL Server connection
$connectionString = "Server=tcp:websiteql-db.database.windows.net,1433;Initial Catalog=WebsiteQL_NhanSu;Persist Security Info=False;User ID=dbadmin;Password=YOUR_STRONG_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;"

# Run migrations
cd "C:\Users\nguye\source\repos\Website Quản Lý Nhân Sự\Website Quản Lý Nhân Sự"
dotnet ef database update -c ApplicationDbContext --connection $connectionString

# Option 2: Via Kudu console (webapp SSH)
# App Service → Development tools → SSH → Go
# cd site\wwwroot
# dotnet ef database update -c ApplicationDbContext
```

**Step 6: Configure HTTPS & Custom Domain**

```
1. App Service → Settings → TLS/SSL settings
2. Upload certificate OR use Azure-managed cert (free)
3. Add custom domain:
   - Add custom domain blade
   - Verify domain ownership
   - Configure DNS records:
	 - CNAME: www.yourdomain.com → websiteql-nhansu.azurewebsites.net
	 - A record: yourdomain.com → Azure IP (from blade)
4. Bind HTTPS certificate to domain
5. Enable "HTTPS only" in Configuration
```

**Step 7: Verify Deployment**

```
1. Go to https://websiteql-nhansu.azurewebsites.net
2. Check app loads
3. Test login functionality
4. Check database connectivity
5. View logs: App Service → Log stream
```

---

## 5. Deployment Scenario B: VPS (DigitalOcean / Linode / Vultr)

### 5.1 VPS Setup (Example: DigitalOcean)

**Step 1: Create Droplet**
```
1. Create → Droplets
2. Choose image: Windows Server 2022
3. Choose size: $12/month (2GB RAM, 1 vCPU minimum for .NET)
4. Region: Singapore
5. Create Droplet
6. Note: IP address & root password
```

**Step 2: Initial Server Setup**

```powershell
# RDP Connect to your droplet
mstsc.exe
# Server: YOUR_DROPLET_IP
# User: Administrator
# Password: PROVIDED_PASSWORD

# Once logged in, run PowerShell as Admin:

# 1. Update Windows
Start-Process "ms-settings:" -Wait
# Check for updates

# 2. Install required software
# Install .NET 10 Runtime
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile dotnet-install.ps1
.\dotnet-install.ps1 -Channel 10.0

# 3. Install SQL Server Express (or use Azure SQL)
# Download from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
# Or use SQL Server in Docker

# 4. Install IIS (for hosting .NET Core)
Install-WindowsFeature -Name Web-Server, Web-Asp-Net45

# 5. Configure IIS
# Add IIS URL Rewrite module
# Add IIS Application Initialization module
```

**Step 3: Deploy Application**

```powershell
# On your local machine
cd "C:\Users\nguye\source\repos\Website Quản Lý Nhân Sự"

# Publish release build
dotnet publish -c Release -o .\publish

# Upload to VPS (via FTP/SCP or RDP copy)
# Create folder on VPS: C:\inetpub\wwwroot\WebsiteQL

# Copy publish folder contents to C:\inetpub\wwwroot\WebsiteQL
```

**Step 4: Configure IIS Application Pool & Site**

```
1. Open IIS Manager
2. Create new Application Pool:
   - Name: WebsiteQL
   - .NET CLR version: No Managed Code (for .NET Core)
   - Managed pipeline mode: Integrated
3. Create new Website:
   - Name: WebsiteQL
   - Physical path: C:\inetpub\wwwroot\WebsiteQL
   - Binding: http/https, port 80/443, your domain
   - Application pool: WebsiteQL
```

**Step 5: Database Configuration**

```powershell
# Option 1: SQL Server Express on same VPS
# Run migrations:
cd C:\inetpub\wwwroot\WebsiteQL
dotnet ef database update -c ApplicationDbContext --connection "Server=.;Database=WebsiteQL_NhanSu;Integrated Security=true;"

# Option 2: Azure SQL Database
# Use connection string from appsettings.Production.json
dotnet ef database update -c ApplicationDbContext
```

**Step 6: Configure Firewall & SSL**

```powershell
# 1. Allow HTTP/HTTPS through Windows Firewall
New-NetFirewallRule -DisplayName "HTTP" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 80
New-NetFirewallRule -DisplayName "HTTPS" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 443

# 2. Install SSL certificate (Let's Encrypt - free)
# Use Certbot or Win-ACME:
# Download from: https://www.win-acme.com/

# 3. Bind SSL in IIS:
# Website → Edit Bindings → Add HTTPS binding with certificate
```

**Step 7: Setup Reverse Proxy (Optional but Recommended)**

```
# Use Nginx or Application Request Routing (ARR) in IIS
# This proxies requests to your .NET Core app

# In IIS:
1. Install URL Rewrite
2. Create rewrite rules to forward to http://localhost:5000 (your app)
3. This isolates app from direct internet access
```

---

## 6. Deployment Scenario C: Docker + Docker Compose

### 6.1 Create Dockerfile

**Create file: `Dockerfile` in project root**

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Website Quản Lý Nhân Sự/Website Quản Lý Nhân Sự.csproj", "Website Quản Lý Nhân Sự/"]
RUN dotnet restore "Website Quản Lý Nhân Sự/Website Quản Lý Nhân Sự.csproj"

COPY . .
WORKDIR "/src/Website Quản Lý Nhân Sự"
RUN dotnet build "Website Quản Lý Nhân Sự.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Website Quản Lý Nhân Sự.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "Website Quản Lý Nhân Sự.dll"]
```

### 6.2 Create docker-compose.yml

```yaml
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
```

### 6.3 Deploy with Docker

```powershell
# Build & start containers
docker-compose up -d

# Run migrations inside container
docker exec websiteql-web dotnet ef database update -c ApplicationDbContext

# View logs
docker-compose logs -f web

# Stop
docker-compose down
```

---

## 7. Post-Deployment Configuration

### 7.1 Environment Variables Setup

**For Azure App Service:**
```
App Service → Configuration → Application settings

Add:
- ASPNETCORE_ENVIRONMENT = Production
- DOTNET_ENVIRONMENT = Production
- Logging__LogLevel__Default = Information
- Email__SendGridApiKey = YOUR_KEY
- Jwt__Secret = YOUR_SECRET
```

**For VPS (Windows):**
```powershell
# System properties → Environment variables
# Or in appsettings.Production.json
```

### 7.2 Database Backup Setup

**For Azure SQL:**
```
SQL Server → Automated backups
- Retention: 35 days (free)
- Backup redundancy: Geo-redundant
```

**For VPS (Manual backup):**
```powershell
# Create scheduled task
$action = New-ScheduledTaskAction -Execute "Backup-SqlDatabase.ps1"
$trigger = New-ScheduledTaskTrigger -Daily -At 02:00AM
Register-ScheduledTask -Action $action -Trigger $trigger -TaskName "BackupDB"
```

### 7.3 Monitoring & Logging

**Azure Application Insights:**
```
1. Create Application Insights resource
2. Link to App Service
3. View real-time metrics, exceptions, performance
4. Set up alerts for errors
```

**For VPS (Using Serilog):**
```csharp
// Add to Program.cs
builder.Services.AddLogging(logBuilder =>
{
	logBuilder.AddSerilog(new LoggerConfiguration()
		.MinimumLevel.Information()
		.WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
		.WriteTo.Console()
		.CreateLogger()
	);
});
```

---

## 8. SSL/TLS Certificate Setup

### 8.1 Option 1: Let's Encrypt (Free)

**For IIS on Windows VPS:**
```
1. Download and install Win-ACME from https://www.win-acme.com/
2. Run wacs.exe
3. Choose "Create new certificate"
4. Select IIS site
5. Configure HTTPS binding
6. Auto-renews every 60 days
```

**For Docker:**
```yaml
# Add to docker-compose.yml
  certbot:
	image: certbot/certbot
	volumes:
	  - ./certbot/conf:/etc/letsencrypt
	  - ./certbot/www:/var/www/certbot
	entrypoint: "/bin/sh -c 'trap exit TERM; while :; do certbot renew; sleep 12h & wait $${!}; done;'"
```

### 8.2 Option 2: Azure Managed Certificate (Free)

```
App Service → TLS/SSL settings → Private Key Certificates
- Add App Service Managed Certificate
- Select your custom domain
- Create
```

### 8.3 Option 3: Purchase Certificate

```
1. Buy from: Namecheap, GoDaddy, Let's Encrypt Sponsored
2. Download .pfx file
3. Upload to:
   - Azure: App Service → TLS/SSL settings
   - IIS: Certificates → Import
4. Bind to website
```

---

## 9. Custom Domain Setup

### 9.1 DNS Configuration

**For yourdomain.com:**

| Type | Name | Value | TTL |
|------|------|-------|-----|
| A | @ | YOUR_SERVER_IP or Azure IP | 3600 |
| CNAME | www | yourdomain.com | 3600 |
| MX | @ | mail.yourdomain.com (if email) | 3600 |
| TXT | @ | Verification code (from host) | 3600 |

**For Azure App Service:**
```
Type: CNAME
Name: www
Value: websiteql-nhansu.azurewebsites.net

Then add www.yourdomain.com in App Service custom domain
```

### 9.2 Verification

```powershell
# Test DNS resolution
nslookup yourdomain.com
# Should return your server IP

# Test HTTPS
curl https://yourdomain.com
# Should load without SSL warnings
```

---

## 10. Performance Optimization After Deployment

### 10.1 Enable Compression

```csharp
// Program.cs
builder.Services.AddResponseCompression(options =>
{
	options.Providers.Add<GzipCompressionProvider>();
	options.Providers.Add<BrotliCompressionProvider>();
});

app.UseResponseCompression();
```

### 10.2 Enable Output Caching

```csharp
// For static pages
builder.Services.AddOutputCache(options =>
{
	options.AddPolicy("CacheLongTime", p =>
		p.Expire(TimeSpan.FromHours(1))
		 .SetVaryByRouteValue("id"));
});

app.UseOutputCache();
```

### 10.3 Database Connection Pooling

```csharp
// Already in Program.cs from earlier:
services.AddDbContextPool<ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString)
);
```

### 10.4 CDN Setup (Optional)

**For Azure:**
```
1. Create Azure CDN resource
2. Link to your blob storage
3. Configure origin: App Service URL
4. Update DNS to point to CDN endpoint
```

---

## 11. Health Check & Monitoring

### 11.1 Add Health Check Endpoint

```csharp
// Program.cs
builder.Services.AddHealthChecks()
	.AddSqlServer(connectionString, name: "Database")
	.AddApplicationInsightsPublisher();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
	Predicate = registration => registration.Tags.Contains("ready")
});
```

### 11.2 Setup Alerts

**For Azure App Service:**
```
1. Create → Monitor → Alert
2. Scope: Your App Service
3. Condition: 
   - Metric: HTTP 5xx errors
   - Operator: Greater than
   - Threshold: 10 in 5 minutes
4. Action: Send email notification
```

---

## 12. Rollback & Emergency Recovery

### 12.1 Azure App Service Rollback

```powershell
# View deployment history
az webapp deployment list --resource-group RG-WebsiteQL --name websiteql-nhansu

# Rollback to previous version
az webapp deployment slot swap --resource-group RG-WebsiteQL --name websiteql-nhansu
```

### 12.2 Database Backup Recovery

```sql
-- Azure SQL Backup
-- Automatic backups kept for 35 days
-- Restore in Azure Portal → Backups → Restore

-- Manual restore
RESTORE DATABASE [WebsiteQL_NhanSu] 
FROM URL = 'https://yourstorageaccount.blob.core.windows.net/backups/backup.bak'
WITH CREDENTIAL = [BackupCredential];
```

### 12.3 Version Control Backup

```powershell
# Tag production release
git tag -a v1.0.0 -m "Production Release v1.0.0"
git push origin v1.0.0

# Return to tagged version if needed
git checkout v1.0.0
```

---

## 13. Troubleshooting Common Issues

### Issue: "502 Bad Gateway"
**Causes**:
- App not running
- Connection string incorrect
- Database unreachable

**Solution**:
```powershell
# Check app is running
az webapp show --resource-group RG-WebsiteQL --name websiteql-nhansu --query state

# View logs
az webapp log tail --resource-group RG-WebsiteQL --name websiteql-nhansu

# Restart app
az webapp restart --resource-group RG-WebsiteQL --name websiteql-nhansu
```

### Issue: "Connection string not recognized"
**Cause**: Environment variable not set

**Solution**:
```
App Service → Configuration → Connection Strings
Verify DefaultConnection is set correctly
```

### Issue: "Database migration failed"
**Cause**: Migration hasn't run on production

**Solution**:
```powershell
# SSH into container/app
az webapp create-remote-connection --resource-group RG-WebsiteQL --name websiteql-nhansu

# Run migrations manually
dotnet ef database update -c ApplicationDbContext
```

### Issue: "HTTPS not working / Mixed content warning"
**Cause**: HTTP requests inside HTTPS page

**Solution**:
```csharp
// Program.cs - Force HTTPS
if (app.Environment.IsProduction())
{
	app.UseHttpsRedirection();
	app.UseHsts();
}

// In views, use protocol-relative URLs
<img src="//cdn.example.com/image.png" />
```

### Issue: "Slow page load / Timeout"
**Causes**:
- Missing indexes
- N+1 queries
- Undersized server

**Solution**:
```csharp
// Use Include() for related data
var employees = await context.Employees
	.Include(e => e.Department)
	.Include(e => e.Position)
	.AsNoTracking()
	.ToListAsync();

// Add indexes on frequently filtered columns
CREATE INDEX IX_Employees_WorkStatus ON Employees(WorkStatus);
```

---

## 14. Checklist: Deployment Day

### Before Going Live (Days before)
- [ ] All tests passing
- [ ] Database backup successful
- [ ] SSL certificate ready
- [ ] DNS records prepared
- [ ] Team notification sent
- [ ] Rollback plan documented

### Day Of Deployment (Morning)
- [ ] Backup production database
- [ ] Deploy to staging environment
- [ ] Test staging thoroughly
- [ ] Prepare production deployment script
- [ ] Notify users of maintenance window (if needed)

### Deployment (Execute)
- [ ] Enable maintenance page (optional)
- [ ] Run migrations
- [ ] Deploy application
- [ ] Verify connectivity
- [ ] Run smoke tests
- [ ] Check logs for errors
- [ ] Disable maintenance page

### Post-Deployment (Verification)
- [ ] Test main functionality
- [ ] Check login/authentication
- [ ] Verify database data
- [ ] Monitor error logs
- [ ] Check performance metrics
- [ ] Notify team deployment complete

### Week 1 (Monitoring)
- [ ] Monitor error logs daily
- [ ] Check performance metrics
- [ ] Gather user feedback
- [ ] Fix any critical issues immediately
- [ ] Plan non-critical fixes for next release

---

## 15. Monthly Maintenance Tasks

```
Week 1:
  - Review error logs & analytics
  - Check database size & growth
  - Update security patches

Week 2:
  - Database maintenance (rebuild indexes)
  - Update NuGet packages (if patches available)
  - Test backup restoration

Week 3:
  - Performance analysis
  - User feedback review
  - Plan improvements for next sprint

Week 4:
  - Security audit
  - Cost analysis
  - Capacity planning
```

---

## 16. Cost Estimation

### Azure App Service (Recommended)
```
App Service Plan (B1): $10-15/month
SQL Database (S0): $15-20/month
Storage (Backups): $2-5/month
Total: ~$30-40/month
```

### VPS (DigitalOcean)
```
Droplet (2GB): $12/month
SQL Server License: Free (Express) or $165+ (Standard)
Backups: $1-2/month
Total: ~$13-180/month (depending on SQL Server)
```

### Docker + Hosting
```
Heroku/Render: $7-50/month
AWS Lightsail: $3.50-40/month
DigitalOcean App Platform: $5+/month
```

---

## 17. Security Best Practices

### Before Going Live
- [ ] Change all default passwords
- [ ] Enable two-factor authentication (2FA) for admin accounts
- [ ] Configure WAF (Web Application Firewall)
- [ ] Enable DDoS protection
- [ ] Setup rate limiting
- [ ] Enable CORS only for trusted domains
- [ ] Use HTTPS everywhere
- [ ] Remove sensitive information from error messages
- [ ] Enable request logging & monitoring
- [ ] Setup alerts for suspicious activity

### Ongoing
- [ ] Regular security audits
- [ ] Keep software updated
- [ ] Monitor for vulnerabilities
- [ ] Regular penetration testing
- [ ] Review access logs monthly
- [ ] Rotate secrets every 90 days
- [ ] Backup testing & recovery drills

---

## 18. Reference Links

### Azure
- Portal: https://portal.azure.com
- Docs: https://docs.microsoft.com/azure
- Pricing: https://azure.microsoft.com/pricing

### Domain & DNS
- Namecheap: https://www.namecheap.com
- GoDaddy: https://www.godaddy.com
- Cloudflare: https://www.cloudflare.com

### Hosting VPS
- DigitalOcean: https://www.digitalocean.com
- Linode: https://www.linode.com
- Vultr: https://www.vultr.com

### Certificates
- Let's Encrypt: https://letsencrypt.org
- Win-ACME: https://www.win-acme.com
- Certbot: https://certbot.eff.org

### Monitoring
- Application Insights: https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview
- Serilog: https://serilog.net
- New Relic: https://newrelic.com

### .NET Deployment
- Deploy to Azure: https://docs.microsoft.com/dotnet/azure
- Deploy to IIS: https://docs.microsoft.com/aspnet/core/host-and-deploy/iis
- Docker: https://docs.microsoft.com/dotnet/core/docker/build-container

---

## 19. Quick Start Summary

### FASTEST DEPLOYMENT PATH (Azure)

```powershell
# 1. Prepare code (5 min)
cd "C:\Users\nguye\source\repos\Website Quản Lý Nhân Sự"
dotnet build -c Release

# 2. Create Azure resources (10 min)
# Go to portal.azure.com → Create App Service + SQL Database

# 3. Configure connection string (2 min)
# App Service → Configuration → Connection Strings

# 4. Publish (5 min)
# In Visual Studio:
# Project → Publish → Select Azure App Service → Finish

# 5. Migrate database (2 min)
# App Service → SSH → run: dotnet ef database update

# 6. Verify (5 min)
# Open https://yourdomain.azurewebsites.net
```

**Total Time: ~30 minutes to live deployment!**

---

**Version**: 1.0
**Date**: 2025-06-21
**Last Updated**: 2025-06-21
**Status**: Ready for Production ✅
