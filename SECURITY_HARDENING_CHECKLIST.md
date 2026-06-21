# Security Hardening & Pre-Deployment Security Checklist

## 🔐 CRITICAL SECURITY ITEMS (DO THESE FIRST!)

### 1. Secret Management

#### ❌ DO NOT:
```csharp
// ❌ WRONG: Secrets in code
var connectionString = "Server=prod-db;Password=Admin123";  
var apiKey = "sk_live_xyz123abc";
var jwtSecret = "my-secret-key-123";

// ❌ WRONG: Secrets in appsettings.json
{
  "Email": {
	"SmtpPassword": "YourPassword123"
  }
}

// ❌ WRONG: Committing to git
git add appsettings.Production.json
git push
```

#### ✅ DO THIS:
```powershell
# Store secrets in environment variables
$env:ConnectionStrings__DefaultConnection = "Server=prod-db;Password=..."

# Or use Azure Key Vault
az keyvault secret set --vault-name "websiteql-kv" --name "db-connection" --value "..."

# Or use .NET User Secrets (development only)
dotnet user-secrets set "Jwt:Secret" "your-secret-here"

# appsettings.Production.json should be:
{
  "ConnectionStrings": {
	"DefaultConnection": null  # Will use environment variable
  },
  "Jwt": {
	"Secret": null  # Will use environment variable
  }
}
```

### 2. Generate Strong Production Secrets

```powershell
# Generate JWT Secret (min 32 chars, use for production)
$jwtSecret = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(
	(1..32 | ForEach-Object { [char][byte]$((48..90) + (97..122) | Get-Random) }) -join ''
))
Write-Host "JWT Secret: $jwtSecret"

# Generate strong database password
$dbPassword = -join ((65..90) + (97..122) + (48..57) + (33,35,36,37,38,42,43,45,46,47,58,59,61,63,64,95,123,125) | 
	Get-Random -Count 20 | ForEach-Object {[char]$_})
Write-Host "DB Password: $dbPassword"
# Requirement: min 8 chars, uppercase, lowercase, digit, special character
```

### 3. Setup Key Vault (Azure)

```powershell
# Create Key Vault
az keyvault create --name "websiteql-kv" --resource-group $resourceGroup --location $location

# Store secrets
az keyvault secret set --vault-name "websiteql-kv" --name "db-connection" `
	--value "Server=websiteql-db.database.windows.net;..."

az keyvault secret set --vault-name "websiteql-kv" --name "jwt-secret" `
	--value "$jwtSecret"

az keyvault secret set --vault-name "websiteql-kv" --name "sendgrid-api-key" `
	--value "YOUR_SENDGRID_KEY"

# Give App Service access to Key Vault
az identity assign --resource-group $resourceGroup --name $appService

$principalId = az webapp show --resource-group $resourceGroup --name $appService --query identity.principalId -o tsv

az keyvault set-policy --name "websiteql-kv" --object-id $principalId `
	--secret-permissions get list
```

### 4. Update Program.cs for Key Vault

```csharp
// Program.cs - Add Key Vault integration
var builder = WebApplicationBuilder.CreateBuilder(args);

// Add Key Vault (only in production)
if (!builder.Environment.IsDevelopment())
{
	var keyVaultUrl = new Uri(builder.Configuration["KeyVaultUrl"] ?? "");
	var credential = new DefaultAzureCredential();
	builder.Configuration.AddAzureKeyVault(keyVaultUrl, credential);
}

// Now secrets are loaded from Key Vault automatically
// builder.Configuration["ConnectionStrings:DefaultConnection"]
// builder.Configuration["Jwt:Secret"]
```

---

## 🔒 AUTHENTICATION & AUTHORIZATION SECURITY

### 1. Password Policy

```csharp
// Program.cs - Configure strong password requirements
builder.Services.Configure<IdentityOptions>(options =>
{
	// Password settings
	options.Password.RequireDigit = true;
	options.Password.RequireLowercase = true;
	options.Password.RequireNonAlphanumeric = true;
	options.Password.RequireUppercase = true;
	options.Password.RequiredLength = 12;  // Min 12 characters
	options.Password.RequiredUniqueChars = 3;  // At least 3 unique chars

	// Lockout settings
	options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
	options.Lockout.MaxFailedAccessAttempts = 5;  // Lock after 5 failed attempts
	options.Lockout.AllowedForNewUsers = true;

	// User settings
	options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
	options.User.RequireUniqueEmail = true;
});
```

### 2. Two-Factor Authentication (2FA)

```csharp
// Enable 2FA for production
builder.Services.Configure<IdentityOptions>(options =>
{
	options.SignIn.RequireConfirmedEmail = true;
	options.SignIn.RequireConfirmedPhoneNumber = false;  // Optional
});

// In Login page model
public class LoginModel : PageModel
{
	public async Task<IActionResult> OnPostAsync()
	{
		var user = await _userManager.FindByNameAsync(Input.Username);
		if (user != null && await _userManager.IsEnabledAsync(user))
		{
			var result = await _signInManager.PasswordSignInAsync(
				user, Input.Password, Input.RememberMe, lockoutOnFailure: true);

			if (result.Succeeded)
			{
				// Check if 2FA is enabled
				if (await _userManager.GetTwoFactorEnabledAsync(user))
				{
					return RedirectToPage("/Identity/Account/LoginWith2fa", 
						new { returnUrl, rememberMe = Input.RememberMe });
				}
			}
		}
	}
}
```

### 3. Email Verification

```csharp
// Require email confirmation for all new registrations
builder.Services.Configure<IdentityOptions>(options =>
{
	options.SignIn.RequireConfirmedEmail = true;
});

// Send confirmation email during registration
public async Task<IActionResult> OnPostAsync()
{
	var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email };
	var result = await _userManager.CreateAsync(user, Input.Password);

	if (result.Succeeded)
	{
		var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
		var callbackUrl = Url.Page("/Identity/Account/ConfirmEmail", 
			values: new { area = "Identity", userId = user.Id, code = code });

		await _emailSender.SendConfirmationLinkAsync(user.Email, HtmlEncoder.Default.Encode(callbackUrl));
	}
}
```

---

## 🛡️ INJECTION ATTACK PREVENTION

### 1. SQL Injection Protection

```csharp
// ✅ CORRECT: Using EF Core (parameterized queries)
var employees = await _context.Employees
	.Where(e => e.Email == userInput)  // Parameterized automatically
	.ToListAsync();

// ❌ WRONG: String concatenation (SQL Injection!)
var query = $"SELECT * FROM Employees WHERE Email = '{userEmail}'";
_context.Database.ExecuteSqlRaw(query);

// ✅ CORRECT: Using parameters with raw SQL
var query = "SELECT * FROM Employees WHERE Email = @Email";
_context.Database.ExecuteSqlInterpolated(query, userEmail);
```

### 2. XSS Prevention

```html
<!-- ✅ CORRECT: HTML encoding (Razor encodes by default) -->
<p>@Model.User.Email</p>  <!-- Automatically HTML-encoded -->

<!-- ❌ WRONG: Raw HTML without encoding -->
<p>@Html.Raw(Model.User.Comment)</p>  <!-- Dangerous if user input! -->

<!-- ✅ CORRECT: Encode explicitly if needed -->
<p>@Html.Encode(Model.User.Comment)</p>

<!-- ✅ CORRECT: Content Security Policy headers -->
<meta http-equiv="Content-Security-Policy" 
	  content="default-src 'self'; script-src 'self' 'unsafe-inline';">
```

### 3. CSRF Protection

```csharp
// Already built-in to Razor Pages
// Verify token is present
public async Task<IActionResult> OnPostAsync()
{
	if (!ModelState.IsValid)
		return Page();

	// CSRF token automatically validated
	// No manual code needed!
}

// In form, include token
<form method="post">
	<input type="hidden" name="__RequestVerificationToken" value="..." />
	<!-- form fields -->
</form>

// Or in Razor
<form method="post">
	@Html.AntiForgeryToken()
	<!-- form fields -->
</form>
```

---

## 🔑 INPUT VALIDATION & SANITIZATION

### 1. Model Validation Attributes

```csharp
public class Employee
{
	[Required]
	[StringLength(200, MinimumLength = 2)]
	public string FullName { get; set; }

	[Required]
	[EmailAddress]  // Validates email format
	public string Email { get; set; }

	[Phone]  // Validates phone format
	public string Phone { get; set; }

	[RegularExpression(@"^NV\d{4}$")]  // Only NV0001 format
	public string EmployeeCode { get; set; }

	[Range(18, 70)]  // Age between 18-70
	public int Age { get; set; }

	[DataType(DataType.Date)]
	[Display(Name = "Date of Birth")]
	public DateTime DateOfBirth { get; set; }
}
```

### 2. Server-Side Validation

```csharp
public async Task<IActionResult> OnPostAsync(Employee employee)
{
	// 1. Check if model is valid (attributes above)
	if (!ModelState.IsValid)
	{
		return Page();
	}

	// 2. Additional server-side validations
	if (employee.DateOfBirth > DateTime.Today.AddYears(-18))
	{
		ModelState.AddModelError("DateOfBirth", "Must be at least 18 years old");
		return Page();
	}

	// 3. Check for duplicates
	var existing = await _context.Employees
		.FirstOrDefaultAsync(e => e.Email == employee.Email && e.CompanyId == employee.CompanyId);

	if (existing != null)
	{
		ModelState.AddModelError("Email", "Email already exists in this company");
		return Page();
	}

	// 4. Proceed with save
	_context.Employees.Add(employee);
	await _context.SaveChangesAsync();

	return RedirectToPage("./Index");
}
```

### 3. File Upload Validation

```csharp
[AcceptVerbs("GET", "POST")]
public async Task<IActionResult> OnPostUploadAsync(IFormFile file)
{
	// 1. Check file exists
	if (file == null || file.Length == 0)
	{
		return BadRequest("File is required");
	}

	// 2. Validate file size (max 5MB)
	const long maxFileSize = 5 * 1024 * 1024;  // 5MB
	if (file.Length > maxFileSize)
	{
		return BadRequest($"File size exceeds {maxFileSize} bytes");
	}

	// 3. Validate file extension
	var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
	var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
	if (!allowedExtensions.Contains(extension))
	{
		return BadRequest($"File extension {extension} not allowed");
	}

	// 4. Validate MIME type
	var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "application/pdf" };
	if (!allowedMimeTypes.Contains(file.ContentType))
	{
		return BadRequest($"MIME type {file.ContentType} not allowed");
	}

	// 5. Sanitize filename
	var fileName = Path.GetFileNameWithoutExtension(file.FileName);
	fileName = System.Text.RegularExpressions.Regex.Replace(fileName, @"[^a-zA-Z0-9_-]", "");
	if (fileName.Length == 0)
		fileName = "upload";

	var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{fileName}{extension}";

	// 6. Save to safe location only (/uploads/media/)
	var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "media");
	Directory.CreateDirectory(uploadsFolder);

	var filePath = Path.Combine(uploadsFolder, uniqueFileName);

	// Check for path traversal
	var fullPath = Path.GetFullPath(filePath);
	var fullUploadsPath = Path.GetFullPath(uploadsFolder);
	if (!fullPath.StartsWith(fullUploadsPath))
	{
		return BadRequest("Invalid file path");
	}

	using (var stream = new FileStream(filePath, FileMode.Create))
	{
		await file.CopyToAsync(stream);
	}

	return Ok(new { filePath = $"/uploads/media/{uniqueFileName}" });
}
```

---

## 🌐 HTTPS & TRANSPORT SECURITY

### 1. Enforce HTTPS

```csharp
// Program.cs
var app = builder.Build();

if (app.Environment.IsProduction())
{
	// Redirect all HTTP to HTTPS
	app.UseHttpsRedirection();

	// Add HSTS (HTTP Strict Transport Security)
	// Tells browser to always use HTTPS for 1 year
	app.UseHsts();
}

// Configure HSTS
builder.Services.AddHsts(options =>
{
	options.Preload = true;  // Include in HSTS preload list
	options.IncludeSubDomains = true;
	options.MaxAge = TimeSpan.FromDays(365);
});
```

### 2. Security Headers

```csharp
// Program.cs - Add security headers middleware
app.Use(async (context, next) =>
{
	// Content Security Policy: Prevent XSS
	context.Response.Headers.Add("Content-Security-Policy", 
		"default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'");

	// Prevent MIME type sniffing
	context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

	// Prevent clickjacking (frame embedding)
	context.Response.Headers.Add("X-Frame-Options", "DENY");

	// Enable XSS protection in older browsers
	context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

	// Referrer policy
	context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

	// Permissions policy (formerly Feature Policy)
	context.Response.Headers.Add("Permissions-Policy", 
		"geolocation=(), microphone=(), camera=()");

	await next();
});
```

---

## 🚨 RATE LIMITING & BRUTE FORCE PROTECTION

### 1. Implement Rate Limiting

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
	options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

	// Fixed window limiter
	options.AddFixedWindowLimiter(policyName: "fixed", configure: options =>
	{
		options.PermitLimit = 100;  // 100 requests
		options.Window = TimeSpan.FromMinutes(1);  // Per minute
	});

	// Sliding window limiter for login attempts
	options.AddSlidingWindowLimiter(policyName: "login", configure: options =>
	{
		options.PermitLimit = 5;  // 5 attempts
		options.Window = TimeSpan.FromMinutes(15);  // Per 15 minutes
	});
});

app.UseRateLimiter();

// Apply to endpoints
app.MapPost("/identity/account/login", LoginHandler).RequireRateLimiting("login");
```

### 2. Account Lockout

```csharp
// Program.cs - Already configured in password policy section
// After 5 failed login attempts, account locks for 15 minutes

// In login page
public async Task<IActionResult> OnPostAsync()
{
	var result = await _signInManager.PasswordSignInAsync(
		Input.Username, 
		Input.Password, 
		Input.RememberMe, 
		lockoutOnFailure: true);  // ← This enables lockout

	if (result.IsLockedOut)
	{
		ModelState.AddModelError(string.Empty, 
			"Account locked due to too many failed login attempts. Try again later.");
		return Page();
	}
}
```

---

## 📊 LOGGING & MONITORING SECURITY

### 1. Structured Logging with Serilog

```csharp
// Program.cs
builder.Services.AddLogging(logBuilder =>
{
	var logger = new LoggerConfiguration()
		.MinimumLevel.Information()
		.Enrich.FromLogContext()
		.Enrich.WithProperty("Application", "WebsiteQL")
		.Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)

		// Write to file (daily rolling)
		.WriteTo.File(
			"logs/app-.txt",
			rollingInterval: RollingInterval.Day,
			outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")

		// Write important events to console
		.WriteTo.Console()

		// For production: send to Azure Application Insights
		.WriteTo.ApplicationInsights(
			new TelemetryClient(), 
			TelemetryConverter.Events)

		.CreateLogger();

	logBuilder.AddSerilog(logger);
});

// Use in code
logger.Information("User {UserId} logged in successfully", userId);
logger.Warning("Failed login attempt for user {Email}", email);
logger.Error("Database connection failed: {Exception}", ex);
```

### 2. Log Security Events

```csharp
// Log failed login attempts
logger.Warning("Failed login attempt: email={Email}, ip={IpAddress}", 
	email, 
	HttpContext.Connection.RemoteIpAddress);

// Log successful login
logger.Information("User login successful: userId={UserId}, email={Email}", 
	user.Id, 
	user.Email);

// Log data changes
logger.Information("Employee {EmployeeId} created by user {CreatedBy}", 
	employee.Id, 
	currentUser.Id);

// Log admin actions
logger.Warning("Admin {AdminId} deleted employee {EmployeeId}", 
	adminId, 
	employeeId);

// Log security-related actions
logger.Warning("Role changed for user {UserId}: from {OldRole} to {NewRole}", 
	userId, 
	oldRole, 
	newRole);
```

### 3. Monitor for Suspicious Activity

```sql
-- SQL: Find failed login attempts
SELECT Email, COUNT(*) as FailedAttempts, MAX(CreatedAt) as LastAttempt
FROM LoginAttempts
WHERE IsSuccessful = 0 
  AND CreatedAt > DATEADD(hour, -1, GETUTCDATE())
GROUP BY Email
HAVING COUNT(*) > 5
ORDER BY FailedAttempts DESC;

-- SQL: Find bulk data access
SELECT UserId, COUNT(*) as AccessCount, MAX(AccessTime) as LastAccess
FROM AuditLog
WHERE TableName = 'Employees'
  AND Action = 'Read'
  AND AccessTime > DATEADD(hour, -1, GETUTCDATE())
GROUP BY UserId
HAVING COUNT(*) > 100
ORDER BY AccessCount DESC;
```

---

## 🔍 DEPENDENCY SECURITY

### 1. Check for Vulnerable Packages

```powershell
# Install vulnerability scanner
dotnet tool install --global dotnet-outdated-tool
dotnet tool install --global nancy  # Nancy shows vulnerabilities

# Check for outdated/vulnerable packages
dotnet outdated

# Check specifically for vulnerabilities
nancy --list

# Update packages
dotnet package update --minor
```

### 2. Regularly Update Dependencies

```powershell
# Check for updates
dotnet package update --check-outdated --verbosity=quiet

# Update to latest (be careful, may have breaking changes)
dotnet package update

# Or update specific package
dotnet package update EntityFrameworkCore --version 10.0.9

# After update, run tests
dotnet test
```

### 3. Lock File Strategy

```
# .NET uses .csproj for version pinning
# Always commit .csproj files to git
# Avoid using wildcards like "*" or "10.0.*"

<!-- ✅ Good: Exact version -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.9" />

<!-- ❌ Bad: Wildcard -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.*" />
```

---

## 🔐 CORS & Cross-Origin Security

### 1. Configure CORS Properly

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
	options.AddPolicy("ProductionPolicy", builder =>
	{
		builder
			.WithOrigins(
				"https://yourdomain.com",
				"https://www.yourdomain.com",
				"https://api.yourdomain.com"
			)
			.WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
			.WithHeaders("Content-Type", "Authorization")
			.AllowCredentials()  // Allow cookies
			.WithExposedHeaders("X-Total-Count");  // Only expose needed headers
	});

	// DEVELOPMENT ONLY (not for production!)
	if (!app.Environment.IsProduction())
	{
		options.AddPolicy("DevelopmentPolicy", builder =>
		{
			builder
				.AllowAnyOrigin()
				.AllowAnyMethod()
				.AllowAnyHeader();
		});
	}
});

// Use CORS
app.UseCors(app.Environment.IsProduction() ? "ProductionPolicy" : "DevelopmentPolicy");
```

---

## 📋 FINAL SECURITY CHECKLIST

### Authentication & Authorization
- [ ] Password policy: min 12 chars, uppercase, lowercase, digit, special char
- [ ] Account lockout: 5 failed attempts → 15 min lockout
- [ ] Email verification required
- [ ] Session timeout: 30-60 minutes
- [ ] 2FA enabled for admin accounts
- [ ] Roles & permissions properly defined
- [ ] Authorization checks on all protected endpoints

### Secrets & Configuration
- [ ] No secrets in code
- [ ] No secrets in appsettings.json
- [ ] Connection strings in environment variables
- [ ] API keys in Key Vault
- [ ] JWT secret (min 32 chars) generated
- [ ] Database password strong (min 20 chars)
- [ ] All secrets different between dev/prod

### Transport Security
- [ ] HTTPS enforced (HTTP → HTTPS redirect)
- [ ] HSTS header set (1 year)
- [ ] SSL certificate installed
- [ ] TLS 1.2+ enforced
- [ ] Security headers added (CSP, X-Frame-Options, etc.)

### Input Validation
- [ ] Server-side validation on all forms
- [ ] File upload validation (size, type, extension)
- [ ] File path sanitization
- [ ] Email validation
- [ ] Phone validation
- [ ] Length limits enforced

### Database Security
- [ ] Parameterized queries (EF Core handles this)
- [ ] Row-level security implemented
- [ ] Audit logging for sensitive tables
- [ ] Soft deletes for important data
- [ ] Backups scheduled daily
- [ ] Backup tested for restoration

### Injection Attack Prevention
- [ ] SQL Injection: Using EF Core
- [ ] XSS Prevention: HTML encoding
- [ ] CSRF Protection: Token validation
- [ ] Command Injection: Avoid Process.Start

### Rate Limiting & DDoS
- [ ] Rate limiting enabled on login
- [ ] Rate limiting on APIs
- [ ] DDoS protection enabled (Azure DDoS, CloudFlare, etc.)
- [ ] WAF (Web Application Firewall) enabled

### Logging & Monitoring
- [ ] Failed login attempts logged
- [ ] Admin actions logged
- [ ] Error logs monitored
- [ ] Suspicious activity alerts configured
- [ ] Application Insights enabled
- [ ] Email alerts for critical errors

### Access Control
- [ ] Default admin password changed
- [ ] Users have minimal required permissions
- [ ] Admin panel access restricted by IP (optional)
- [ ] Audit log of who accessed what data
- [ ] Regular access reviews

### Dependencies
- [ ] All NuGet packages from official source
- [ ] No vulnerable packages (run nancy/dotnet outdated)
- [ ] Packages updated regularly
- [ ] .csproj files specify exact versions

### CORS
- [ ] CORS only allows trusted origins
- [ ] CORS methods restricted to needed ones
- [ ] CORS credentials handled properly
- [ ] Exposed headers limited

### Production Deployment
- [ ] SSL certificate installed
- [ ] HTTPS only enabled
- [ ] Security headers configured
- [ ] Logs monitored
- [ ] Backups automated
- [ ] Disaster recovery plan documented
- [ ] Team trained on security practices

---

## 🚨 INCIDENT RESPONSE PLAN

### If Breach Suspected

```
1. IMMEDIATE (First 30 minutes):
   - [ ] Take site offline if actively exploited
   - [ ] Alert security team
   - [ ] Start incident log
   - [ ] Preserve logs and evidence

2. INVESTIGATION (Next 2 hours):
   - [ ] Check logs for suspicious activity
   - [ ] Identify what data was accessed
   - [ ] Check database for unauthorized changes
   - [ ] Review file changes
   - [ ] Check admin account access logs

3. CONTAINMENT (Next 4 hours):
   - [ ] Force reset of all admin passwords
   - [ ] Rotate API keys
   - [ ] Rotate JWT secrets
   - [ ] Update database password
   - [ ] Update any exposed API keys

4. RECOVERY (Next 24 hours):
   - [ ] Restore from clean backup
   - [ ] Run full security scan
   - [ ] Re-deploy with patches
   - [ ] Enable enhanced monitoring

5. NOTIFICATION (Regulatory requirement):
   - [ ] Notify affected users
   - [ ] Notify relevant authorities
   - [ ] Public statement (if needed)
   - [ ] Offer free monitoring service

6. POST-INCIDENT (Week 1):
   - [ ] Full security audit
   - [ ] Root cause analysis
   - [ ] Implement preventive measures
   - [ ] Review and update security policies
```

---

**Document Version**: 1.0
**Created**: 2025-06-21
**Classification**: CONFIDENTIAL - Security Sensitive
**Status**: ✅ Required Before Production
