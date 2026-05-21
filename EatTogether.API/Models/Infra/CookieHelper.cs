using System.Net;

namespace EatTogether.API.Models.Infra
{
	public static class CookieHelper
	{
		//接收 env 參數用來判斷目前執行環境（開發 / 正式）。回傳型別 CookieOptions 是 ASP.NET Core 內建的 Cookie 設定物件。
		private static CookieOptions BuildCookieOptions(IWebHostEnvironment env, DateTimeOffset? expires = null)
		{
			return new CookieOptions
			{
				HttpOnly = true, // 設定 Cookie 為 HttpOnly，防禦 XSS 攻擊
				Secure = env.IsProduction(), // 正式環境 true，Cookie 只允許 HTTPS 傳送；開發環境 false，允許 HTTP
				SameSite = env.IsProduction() ? SameSiteMode.None : SameSiteMode.Strict, // 跨站限制為最嚴格模式。從其他網站發出的請求，都不會附帶 Cookie，是防禦 CSRF 攻擊
				Path = "/",
				Expires = expires
			};
		}


		public static void SetAccessTokenCookie(HttpResponse response, string token, IWebHostEnvironment env)
		{
			// 將 JWT 寫入名為 access_token 的 Cookie，並套用 BuildCookieOptions(env) 產生的安全設定
			var options = BuildCookieOptions(env, DateTimeOffset.UtcNow.AddHours(1));  // Access Token 1 小時
			response.Cookies.Append("access_token", token, options);
		}

		public static void SetRefreshTokenCookie(HttpResponse response, string token, IWebHostEnvironment env)
		{
			var options = BuildCookieOptions(env, DateTimeOffset.UtcNow.AddDays(7));   // Refresh Token 7 天
			response.Cookies.Append("refresh_token", token, options);
		}

		// 清除兩個 Cookie（傳入 env 讓刪除選項與設定一致，確保瀏覽器正確識別並清除）
		public static void ClearAuthCookies(HttpResponse response, IWebHostEnvironment env)
		{
			var options = BuildCookieOptions(env, DateTimeOffset.UnixEpoch);  // 清除時設為過去時間
			response.Cookies.Delete("access_token", options);
			response.Cookies.Delete("refresh_token", options);
		}
	}
}
