
using EatTogether.API.Models.EfModels;
using EatTogether.API.Models.Infra;
using EatTogether.API.Models.Repositories;
using EatTogether.API.Models.Services;
using EatTogether.Models.Repositories;
using EatTogether.Models.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

namespace EatTogether.API
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// 設定 CORS
			builder.Services.AddCors(options =>
			{
				options.AddPolicy("FrontendPolicy", policy =>
				{
					var origins = builder.Configuration["AllowedOrigins"]!.Split(",");
					policy.WithOrigins(origins)
						  .AllowAnyMethod()
						  .AllowAnyHeader()
						  .AllowCredentials();
				});
			});

			// 設定 JWT 認證
			var jwtKey = builder.Configuration["Jwt:SecretKey"]!;
			builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuer = true,
						ValidateAudience = true,
						ValidateLifetime = true,
						ValidateIssuerSigningKey = true,
						ValidIssuer = builder.Configuration["Jwt:Issuer"],
						ValidAudience = builder.Configuration["Jwt:Audience"],
						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
						ClockSkew = TimeSpan.Zero  // Token 到期時間不容許誤差
					};

					// 從 HttpOnly Cookie 取得 Token
					options.Events = new JwtBearerEvents
					{
						OnMessageReceived = context =>
						{
							context.Token = context.Request.Cookies["access_token"];
							return Task.CompletedTask;
						}
					};
				});

			builder.Services.AddAuthorization();

			// 設定 Rate Limiting
			builder.Services.AddRateLimiter(options =>
			{
				// 敏感驗證端點：每 IP 每分鐘最多 5 次（登入/註冊/忘記密碼/重送驗證信）
				options.AddPolicy("AuthPolicy", httpContext =>
					RateLimitPartition.GetFixedWindowLimiter(
						partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
						factory: _ => new FixedWindowRateLimiterOptions
						{
							Window = TimeSpan.FromMinutes(1),
							PermitLimit = 5,
							QueueLimit = 0,
						}
					)
				);

				// 一般 API 端點：每 IP 每分鐘最多 60 次
				options.AddPolicy("GeneralPolicy", httpContext =>
					RateLimitPartition.GetFixedWindowLimiter(
						partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
						factory: _ => new FixedWindowRateLimiterOptions
						{
							Window = TimeSpan.FromMinutes(1),
							PermitLimit = 60,
							QueueLimit = 0,
						}
					)
				);

				options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
			});

			// 註冊 DbContext
			builder.Services.AddDbContext<EatTogetherDBContext>(options =>
				options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

			// 註冊 IHttpContextAccessor
			builder.Services.AddHttpContextAccessor();

			// 註冊 Repository
			builder.Services.AddScoped<ICouponRepository, CouponRepository>();
			builder.Services.AddScoped<IEventRepository, EventRepository>();
			builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
			builder.Services.AddScoped<IDishRepository, DishRepository>();
			builder.Services.AddScoped<IProductRepository, ProductRepository>();
			builder.Services.AddScoped<ISetMealRepository, SetMealRepository>();
			builder.Services.AddScoped<ITableRepository, TableRepository>();
			builder.Services.AddScoped<IMemberCouponRepository, MemberCouponRepository>();
			builder.Services.AddScoped<IOrderRepository, OrderRepository>();
			builder.Services.AddScoped<IPreOrderRepository, PreOrderRepository>();
			builder.Services.AddScoped<IProductRepository, ProductRepository>();
			builder.Services.AddScoped<IUserRepository, UserRepository>();
			builder.Services.AddScoped<IMemberRepository, MemberRepository>();
			builder.Services.AddScoped<IMemberFavoriteRepository, MemberFavoriteRepository>();
			builder.Services.AddScoped<INewsRepository, NewsRepository>();
			builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
			builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

			// 註冊 Service
			builder.Services.AddScoped<IAuthService, AuthService>();
			builder.Services.AddScoped<IMemberService, MemberService>();
			builder.Services.AddScoped<IOrderService, OrderService>();
			builder.Services.AddScoped<INewsService, NewsService>();
			builder.Services.AddScoped<INotificationService, NotificationService>();

			// 註冊 JWT Helper：生成 JWT Token 的 infra 工具
			builder.Services.AddSingleton<JwtHelper>();

			// 註冊 ImageUrlResolver：圖片 URL 解析器
			builder.Services.AddSingleton<ImageUrlResolver>();

			// 註冊菜單
			builder.Services.AddScoped<CategoryService>();
			builder.Services.AddScoped<DishService>();
			builder.Services.AddScoped<SetMealService>();


			// 結帳相關
			builder.Services.AddMemoryCache();
			builder.Services.AddHttpClient();

			// 前台寄信服務
			builder.Services.AddScoped<IEmailService, EmailService>();

			// ── 訂位模組 ──
			builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
			builder.Services.AddScoped<ReservationService>();

			// ── 候位模組 ──
			builder.Services.AddScoped<IWalkInQueueRepository, WalkInQueueRepository>();
			builder.Services.AddScoped<WalkInQueueService>();

			// ── 優惠券前台 Service ──
			builder.Services.AddScoped<CouponService>();

			// ── BackgroundServices ──
			builder.Services.AddHostedService<ReservationReminderBackgroundService>();
			builder.Services.AddHostedService<NoShowMarkingBackgroundService>();
			builder.Services.AddHostedService<CouponExpiryNotifyBackgroundService>();

			// ── 限定餐點提醒 ──
			builder.Services.AddScoped<LimitedReminderService>();
			builder.Services.AddHostedService<LimitedReminderBackgroundService>();


			// Add services to the container.

			// 排除 class library（EatTogether.dll）的 MVC Controllers，避免與 API Controllers 路由衝突
			builder.Services.AddControllers()
				.ConfigureApplicationPartManager(apm =>
				{
					var libParts = apm.ApplicationParts
						.Where(p => p.Name == "EatTogether")
						.ToList();
					foreach (var part in libParts)
						apm.ApplicationParts.Remove(part);
				});
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(options =>
			{
				// Swashbuckle 預設不支援 DateOnly / TimeOnly，需手動映射
				options.MapType<DateOnly>(() => new Microsoft.OpenApi.Models.OpenApiSchema
				{ Type = "string", Format = "date" });
				options.MapType<DateOnly?>(() => new Microsoft.OpenApi.Models.OpenApiSchema
				{ Type = "string", Format = "date", Nullable = true });
				options.MapType<TimeOnly>(() => new Microsoft.OpenApi.Models.OpenApiSchema
				{ Type = "string", Format = "time" });
				options.MapType<TimeOnly?>(() => new Microsoft.OpenApi.Models.OpenApiSchema
				{ Type = "string", Format = "time", Nullable = true });

				// class library 內有多個 action 共用同一 method/path，取第一筆避免 Swagger 500
				options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
			});

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			// 強制瀏覽器未來一律使用 HTTPS，防止降級攻擊（開發環境不啟用）
			if (!app.Environment.IsDevelopment())
			{
				app.UseHsts();
			}

			app.UseHttpsRedirection();


			// 靜態檔案：優先用設定的外部 wwwroot（MVC 專案），找不到再用預設
			var staticRoot = builder.Configuration["StaticFilesRoot"];
			if (!string.IsNullOrEmpty(staticRoot) && Directory.Exists(staticRoot))
			{
				app.UseStaticFiles(new StaticFileOptions
				{
					FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(staticRoot)
				});
			}
			else
			{
				app.UseStaticFiles();
			}
			// CSP Header：縱深防禦，限制瀏覽器可載入的資源來源
			app.Use(async (ctx, next) =>
			{
				// connect-src：
				// 開發環境 → 前端透過 Vite proxy（/api）轉發，瀏覽器看到的是同源，'self' 即可
				// 正式環境 → 若前端與 API 不同域，請將下方網域替換為實際 API 網域
				var connectSrc = app.Environment.IsDevelopment()
					? "'self'"
					: "'self' https://eattogether-api-brh5aha4gfdvbmac.westus3-01.azurewebsites.net";  // ← 正式部署前請替換為實際 API 網域

				// 1. Content-Security-Policy
				//    即使存在 XSS 漏洞，也能透過瀏覽器政策大幅限制攻擊者能做的事（縱深防禦）
				ctx.Response.Headers.Append(
					"Content-Security-Policy",
					$"default-src 'self'; " +  // 預設只允許同源資源
					$"script-src 'self'; " +  // JS 只允許同源（防止載入外部惡意腳本）
					$"connect-src {connectSrc}; " +  // fetch/XHR 目標（開發走 Vite proxy；正式視部署架構）
					$"style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +  // Bootstrap inline style + Google Fonts CSS
					$"img-src 'self' data: https:; " +  // 同源、Base64、HTTPS 外部圖（商品圖/頭像）
					$"font-src 'self' data: https://fonts.gstatic.com; " +  // Bootstrap 字體本地打包 + Google Fonts 字體檔
					$"frame-ancestors 'none';"                                             // 禁止被任何網站 iframe 嵌入（防 Clickjacking）
				);

				// 2. X-Content-Type-Options
				//    防止瀏覽器猜測回應的 Content-Type（MIME Sniffing 攻擊）
				//    例如：攻擊者上傳副檔名 .jpg 但內容是 JS，瀏覽器不會自行判斷並執行
				ctx.Response.Headers.Append(
					"X-Content-Type-Options",
					"nosniff"
				);

				// 3. Referrer-Policy
				//    控制 Referer Header 的內容，防止 URL 中的敏感參數（如 reset token）洩漏給外部網站
				//    strict-origin-when-cross-origin：
				//      同源請求 → 帶完整 URL
				//      跨域請求 → 只帶 Origin（不帶路徑與參數）
				ctx.Response.Headers.Append(
					"Referrer-Policy",
					"strict-origin-when-cross-origin"
				);

				// 4. Permissions-Policy
				//    主動關閉專案不需要的瀏覽器功能，即使 XSS 攻擊成功也無法利用這些 API
				//    義起吃不需要攝影機、麥克風、地理位置，全部禁用
				ctx.Response.Headers.Append(
					"Permissions-Policy",
					"camera=(), microphone=(), geolocation=()"
				);

				await next();
			});
			app.UseCors("FrontendPolicy");   // 順序：CORS → RateLimit → Auth
			app.UseRateLimiter();
			app.UseAuthentication();
			app.UseAuthorization();


			app.MapControllers();

			app.Run();
		}
	}
}
