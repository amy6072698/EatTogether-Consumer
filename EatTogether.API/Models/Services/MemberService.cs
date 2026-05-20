using EatTogether.API.Models.DTOs;
using EatTogether.API.Models.EfModels;
using EatTogether.API.Models.Infra;
using EatTogether.API.Models.Repositories;
using EatTogether.API.Models.ViewModels;
using Google.Apis.Auth;
using System.Text.Json;

namespace EatTogether.API.Models.Services
{
	public interface IMemberService
	{
		Task<Result<MemberViewModel>> GetMeAsync(int memberId);
		Task<Result> UpdateProfileAsync(int memberId, UpdateProfileDto dto);
		Task<Result<string>> UploadAvatarAsync(int memberId, IFormFile file);
		Task<Result> ChangePasswordAsync(int memberId, ChangePasswordDto dto);
		Task<Result> RequestEmailChangeAsync(int memberId, RequestEmailChangeDto dto);
		Task<Result> CreateAccountAsync(int memberId, CreateAccountDto dto);
		Task<Result> UnlinkGoogleAsync(int memberId);
		Task<Result> LinkGoogleAsync(int memberId, string code);

		// 刪除帳號為兩步驟
		Task<Result> RequestDeleteAccountAsync(int memberId, DeleteAccountDto dto);  // 第一步:申請刪除
		Task<Result> ConfirmDeleteAccountAsync(ConfirmDeleteAccountDto dto);        // 第二步:確認刪除
		Task<Result> ValidateDeleteTokenAsync(string token);
	}

	public class MemberService : IMemberService
	{
		private readonly IMemberRepository _memberRepo;
		private readonly IEmailService _emailService;
		private readonly IConfiguration _config;
		private readonly IWebHostEnvironment _env;
		private readonly IHttpContextAccessor _httpContextAccessor;

		public MemberService(
			IMemberRepository memberRepo,
			IEmailService emailService,
			IConfiguration config,
			IWebHostEnvironment env,
			IHttpContextAccessor httpContextAccessor)
		{
			_memberRepo = memberRepo;
			_emailService = emailService;
			_config = config;
			_env = env;
			_httpContextAccessor = httpContextAccessor;
		}

		// 嚴格驗證(需檢查黑名單+信箱驗證)
		private Result ValidateMemberStatusStrict(Member? member)
		{
			if (member == null || member.IsDeleted)
				return Result.Fail("member_not_found");

			if (member.IsBlacklisted)
				return Result.Fail("member_blacklisted");

			if (!member.IsConfirmed)
				return Result.Fail("email_not_confirmed");

			return Result.Success();
		}


		// 寬鬆驗證(只檢查黑名單)
		private Result ValidateMemberStatus(Member? member)
		{
			if (member == null || member.IsDeleted)
				return Result.Fail("member_not_found");

			if (member.IsBlacklisted)
				return Result.Fail("member_blacklisted");

			return Result.Success();
		}

		public async Task<Result<MemberViewModel>> GetMeAsync(int memberId)
		{
			var member = await _memberRepo.GetMemberByIdAsync(memberId);
			if (member == null || member.IsDeleted)
				return Result<MemberViewModel>.Fail("member_not_found");

			var externalLogins = await _memberRepo.GetAllExternalLoginsByMemberIdAsync(memberId);
			var googleLogin = externalLogins.FirstOrDefault(e => e.Provider == "google");

			//未來第三方登入擴充預留
			//var lineLogin = externalLogins.FirstOrDefault(e => e.Provider == "line");
			//var facebookLogin = externalLogins.FirstOrDefault(e => e.Provider == "facebook");

			return Result<MemberViewModel>.Success(new MemberViewModel
			{
				Id = member.Id,
				// 純 Google 帳號（EXTERNAL_LOGIN_NO_PASSWORD）不回傳 Account（前端顯示「建立一般帳號」）
				Account = member.HashedPassword == HashUtility.EXTERNAL_LOGIN_NO_PASSWORD ? null : member.Account,
				Name = member.Name,
				Email = member.Email,
				Phone = member.Phone,
				BirthDate = member.BirthDate,
				AvatarFileName = member.AvatarFileName,
				GoogleAvatarUrl = googleLogin?.AvatarUrl,
				GoogleEmail = googleLogin?.ProviderEmail,
				HashedPasswordStatus = member.HashedPassword == HashUtility.EXTERNAL_LOGIN_NO_PASSWORD
					? "EXTERNAL_LOGIN_NO_PASSWORD"
					: "HAS_PASSWORD",
				// 第三方連結狀態
				GoogleLinked = googleLogin != null,

				//未來第三方登入擴充預留
				//LineLinked = lineLogin != null,
				//FacebookLinked = facebookLogin != null,
			});
		}

		public async Task<Result> UpdateProfileAsync(int memberId, UpdateProfileDto dto)
		{
			var member = await _memberRepo.GetMemberByIdAsync(memberId);

			var validation = ValidateMemberStatusStrict(member);
			if (!validation.IsSuccess)
				return validation;

			// 生日不可為未來日期
			if (dto.BirthDate.HasValue && dto.BirthDate.Value > DateOnly.FromDateTime(DateTime.Today))
				return Result.Fail("invalid_birth_date");

			// 生日已設定過，不允許修改（含清空）
			if (member.BirthDate.HasValue && dto.BirthDate != member.BirthDate)
				return Result.Fail("birth_date_locked");

			bool success = await _memberRepo.UpdateProfileAsync(memberId, dto);
			if (!success)
				return Result.Fail("update_failed");

			return Result.Success();
		}

		public async Task<Result<string>> UploadAvatarAsync(int memberId, IFormFile file)
		{
			// 1. 查詢會員（確認存在）
			var member = await _memberRepo.GetMemberByIdAsync(memberId);
			var validation = ValidateMemberStatusStrict(member);
			if (!validation.IsSuccess)
				return Result<string>.Fail(validation.ErrorMessage);

			// 2. 驗證格式（ContentType 正規化為小寫比對）
			var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
			if (!allowedTypes.Contains(file.ContentType.ToLower()))
				return Result<string>.Fail("invalid_format");

			// 3. 驗證大小（2MB）
			if (file.Length > 2 * 1024 * 1024)
				return Result<string>.Fail("file_too_large");

			// 4. 取得上傳目錄（存到前台 API 自己的 wwwroot，不受 StaticFilesRoot 影響）
			var baseDir = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
			var uploadDir = Path.Combine(baseDir, "uploads", "avatars");
			Directory.CreateDirectory(uploadDir); // 不存在時自動建立

			// 5. 刪除舊檔案（若有）
			if (!string.IsNullOrEmpty(member.AvatarFileName))
			{
				var oldPath = Path.Combine(uploadDir, member.AvatarFileName);
				if (File.Exists(oldPath))
					File.Delete(oldPath);
			}

			// 6. 產生唯一檔名並存檔
			var ext = file.ContentType.ToLower() switch
			{
				"image/jpeg" => ".jpg",
				"image/png" => ".png",
				"image/webp" => ".webp",
				_ => throw new InvalidOperationException($"Unexpected ContentType: {file.ContentType}"),
			};
			var newFileName = $"{Guid.NewGuid():N}{ext}";
			var newFilePath = Path.Combine(uploadDir, newFileName);

			using (var stream = new FileStream(newFilePath, FileMode.Create))
				await file.CopyToAsync(stream);

			// 7. 更新 DB
			await _memberRepo.UpdateAvatarAsync(memberId, newFileName);

			return Result<string>.Success(newFileName);
		}

		public async Task<Result> ChangePasswordAsync(int memberId, ChangePasswordDto dto)
		{
			var member = await _memberRepo.GetMemberByIdAsync(memberId);
			var validation = ValidateMemberStatusStrict(member);
			if (!validation.IsSuccess)
				return validation;

			// 純 Google 帳號沒有密碼可以修改
			if (member.HashedPassword == HashUtility.EXTERNAL_LOGIN_NO_PASSWORD)
				return Result.Fail("no_password");

			// 驗證目前密碼
			if (!HashUtility.VerifyPassword(dto.CurrentPassword, member.HashedPassword))
				return Result.Fail("wrong_password");

			var hashed = HashUtility.HashPassword(dto.NewPassword);
			await _memberRepo.UpdateMemberPasswordAsync(memberId, hashed);
			return Result.Success();
		}

		public async Task<Result> RequestEmailChangeAsync(int memberId, RequestEmailChangeDto dto)
		{
			var member = await _memberRepo.GetMemberByIdAsync(memberId);
			var validation = ValidateMemberStatusStrict(member);
			if (!validation.IsSuccess)
				return validation;

			// 純 Google 帳號不允許修改 Email（避免下次 Google 登入找不到帳號）
			if (member.HashedPassword == HashUtility.EXTERNAL_LOGIN_NO_PASSWORD)
				return Result.Fail("cannot_change_email_external_login");

			// 不可與目前 Email 相同
			if (member.Email.Equals(dto.NewEmail, StringComparison.OrdinalIgnoreCase))
				return Result.Fail("same_email");

			// 新 Email 是否被他人使用
			if (await _memberRepo.IsEmailExistsForOtherAsync(memberId, dto.NewEmail))
				return Result.Fail("email_taken");

			// 先將同一會員舊的 Email 變更 token 標為已使用，確保同時間只有最新連結有效
			await _memberRepo.MarkOldEmailChangeTokensUsedAsync(memberId);

			// 建立 ConfirmToken（NewEmail 有值 → VerifyEmailAsync 會走 UpdateEmailAndMarkTokenUsedAsync 分支）
			var tokenStr = Guid.NewGuid().ToString("N");
			await _memberRepo.CreateConfirmTokenAsync(new MemberConfirmToken
			{
				MemberId = memberId,
				Token = tokenStr,
				NewEmail = dto.NewEmail,
				ExpiresAt = DateTime.Now.AddMinutes(15),
				IsUsed = false,
				CreatedAt = DateTime.Now,
			});

			// 寄驗證信至新信箱
			var frontendBase = _config["FrontendBaseUrl"];
			var verifyUrl = $"{frontendBase}/verify-email?token={tokenStr}";
			await _emailService.SendEmailChangeVerifyAsync(dto.NewEmail, verifyUrl);

			return Result.Success();
		}

		public async Task<Result> CreateAccountAsync(int memberId, CreateAccountDto dto)
		{
			var member = await _memberRepo.GetMemberByIdAsync(memberId);
			var validation = ValidateMemberStatusStrict(member);
			if (!validation.IsSuccess)
				return validation;

			// 只有純第三方帳號才能建立一般帳號
			if (member.HashedPassword != HashUtility.EXTERNAL_LOGIN_NO_PASSWORD)
				return Result.Fail("already_has_account");

			// 帳號唯一性（全站）
			if (await _memberRepo.IsAccountExistsAsync(dto.Account))
				return Result.Fail("account_taken");

			var hashedPassword = HashUtility.HashPassword(dto.Password);
			await _memberRepo.UpdateAccountAsync(memberId, dto.Account, hashedPassword);
			return Result.Success();
		}

		public async Task<Result> UnlinkGoogleAsync(int memberId)
		{
			var member = await _memberRepo.GetMemberByIdAsync(memberId);
			var validation = ValidateMemberStatusStrict(member);
			if (!validation.IsSuccess)
				return validation;

			// 純第三方帳號無其他第三方綁定不得取消連結（需先建立一般帳號）
			if (member.HashedPassword == HashUtility.EXTERNAL_LOGIN_NO_PASSWORD)
			{
				// 檢查是否還有其他第三方登入(方便未來擴充)
				var externalLogins = await _memberRepo.GetAllExternalLoginsByMemberIdAsync(memberId);
				if (externalLogins.Count == 1) // 只剩一個
					return Result.Fail("cannot_unlink_only_login");
			}

			var googleLogin = await _memberRepo.GetExternalLoginByMemberAndProviderAsync(memberId, "google");
			if (googleLogin == null)
				return Result.Fail("google_not_linked");

			await _memberRepo.DeleteExternalLoginAsync(memberId, "google");
			await _emailService.SendSecurityNoticeAsync(member.Email, "您已取消與 Google 帳號的連結");
			return Result.Success();
		}

		// 會員中心連結 Google（只綁定，絕不切換登入身份）
		public async Task<Result> LinkGoogleAsync(int memberId, string code)
		{
			// 1. 驗證當前登入會員狀態
			var member = await _memberRepo.GetMemberByIdAsync(memberId);
			var validation = ValidateMemberStatusStrict(member);
			if (!validation.IsSuccess)
				return validation;

			// 2. 用 code 向 Google 換取 id_token
			var clientId = _config["Google:ClientId"]!;
			var clientSecret = _config["Google:ClientSecret"]!;
			var redirectUri = _config["Google:RedirectUri"]!;

			using var httpClient = new HttpClient();
			var tokenResponse = await httpClient.PostAsync(
				"https://oauth2.googleapis.com/token",
				new FormUrlEncodedContent(new Dictionary<string, string>
				{
					["code"] = code,
					["client_id"] = clientId,
					["client_secret"] = clientSecret,
					["redirect_uri"] = redirectUri,
					["grant_type"] = "authorization_code",
				}));

			if (!tokenResponse.IsSuccessStatusCode)
				return Result.Fail("google_auth_failed");

			var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
			using var doc = JsonDocument.Parse(tokenJson);
			if (!doc.RootElement.TryGetProperty("id_token", out var idTokenElement))
				return Result.Fail("google_auth_failed");

			var idToken = idTokenElement.GetString()!;

			// 3. 驗證 id_token，解析 sub 與 avatarUrl
			GoogleJsonWebSignature.Payload payload;
			try
			{
				payload = await GoogleJsonWebSignature.ValidateAsync(
					idToken,
					new GoogleJsonWebSignature.ValidationSettings
					{
						Audience = new[] { clientId }
					});
			}
			catch
			{
				return Result.Fail("google_auth_failed");
			}

			var providerUserId = payload.Subject;
			var email = payload.Email;
			var avatarUrl = payload.Picture;

			// 4. 檢查這個 Google sub 是否已被任何帳號綁定（不限當前會員）
			var existingLogin = await _memberRepo.GetExternalLoginByProviderAsync("google", providerUserId);
			if (existingLogin != null)
				return Result.Fail("google_already_linked_to_other");

			// 5. 寫入綁定（MemberId = 當前登入者，絕不切換身份、不簽發 JWT）
			await _memberRepo.CreateExternalLoginAsync(new MemberExternalLogin
			{
				MemberId = memberId,
				Provider = "google",
				ProviderUserId = providerUserId,
				AvatarUrl = avatarUrl,
				ProviderEmail = email,
				CreatedAt = DateTime.Now,
			});

			// 6. 寄安全通知信
			await _emailService.SendSecurityNoticeAsync(
				member.Email,
				"您的帳號已新增 Google 登入，若非本人操作請立即修改密碼");

			return Result.Success();
		}

		// 第一步: 申請刪除
		public async Task<Result> RequestDeleteAccountAsync(int memberId, DeleteAccountDto dto)
		{
			var member = await _memberRepo.GetMemberByIdAsync(memberId);
			var validation = ValidateMemberStatus(member);
			if (!validation.IsSuccess)
				return validation;

			// 一般帳號需先驗證密碼
			if (member.HashedPassword != HashUtility.EXTERNAL_LOGIN_NO_PASSWORD)
			{
				if (string.IsNullOrEmpty(dto.Password))
					return Result.Fail("password_required");
				if (!HashUtility.VerifyPassword(dto.Password, member.HashedPassword))
					return Result.Fail("wrong_password");
			}
			// 純第三方帳號無密碼,直接發確認信

			// 先將同一會員舊的刪除帳號 token 標為已使用
			await _memberRepo.MarkOldDeleteAccountTokensUsedAsync(memberId);

			// todo: 新增 DeleteAccountTokens 資料表
			// 建立刪除確認 token (重用 MemberConfirmTokens,用特殊標記 "DELETE_ACCOUNT")
			var deleteToken = Guid.NewGuid().ToString("N");
			await _memberRepo.CreateConfirmTokenAsync(new MemberConfirmToken
			{
				MemberId = memberId,
				Token = deleteToken,
				NewEmail = "DELETE_ACCOUNT",  // ← 特殊標記,表示這是刪除帳號 token
				ExpiresAt = DateTime.Now.AddMinutes(15),
				IsUsed = false,
				CreatedAt = DateTime.Now,
			});

			// 寄送確認信
			var frontendBase = _config["FrontendBaseUrl"];
			var confirmUrl = $"{frontendBase}/confirm-delete-account?token={deleteToken}";
			await _emailService.SendDeleteAccountConfirmAsync(member.Email, member.Name, confirmUrl);

			return Result.Success();
		}

		// 第二步: 確認刪除, 不需登入
		public async Task<Result> ConfirmDeleteAccountAsync(ConfirmDeleteAccountDto dto)
		{
			// 1. 驗證 token
			var token = await _memberRepo.GetConfirmTokenAsync(dto.Token);
			if (token == null || token.IsUsed || token.ExpiresAt <= DateTime.Now)
				return Result.Fail("invalid_or_expired_token");

			// 2. 確認是刪除帳號 token
			if (token.NewEmail != "DELETE_ACCOUNT")
				return Result.Fail("invalid_token_type");

			// 3. 查詢會員
			var member = await _memberRepo.GetMemberByIdAsync(token.MemberId);
			var validation = ValidateMemberStatus(member);
			if (!validation.IsSuccess)
				return validation;

			// 4. 一般帳號在確認時仍需再次驗證密碼(雙重保護)
			if (member.HashedPassword != HashUtility.EXTERNAL_LOGIN_NO_PASSWORD)
			{
				if (string.IsNullOrEmpty(dto.Password))
					return Result.Fail("password_required");
				if (!HashUtility.VerifyPassword(dto.Password, member.HashedPassword))
					return Result.Fail("wrong_password");
			}

			// 5. 標記 token 已使用
			await _memberRepo.MarkConfirmTokenUsedAsync(token.Id);

			// 6. 撤銷所有 Refresh Token（踢除所有裝置）
			await _memberRepo.RevokeAllRefreshTokensByMemberIdAsync(member.Id);

			// 7. 軟刪除
			await _memberRepo.SoftDeleteMemberAsync(member.Id);

			// 8. 發送刪除完成通知信
			await _emailService.SendAccountDeletedNoticeAsync(member.Email, member.Name);

			return Result.Success();
		}

		public async Task<Result> ValidateDeleteTokenAsync(string token)
		{
			var confirmToken = await _memberRepo.GetConfirmTokenAsync(token);

			if (confirmToken == null || confirmToken.IsUsed || confirmToken.ExpiresAt <= DateTime.Now)
				return Result.Fail("invalid_or_expired_token");

			if (confirmToken.NewEmail != "DELETE_ACCOUNT")
				return Result.Fail("invalid_token_type");

			return Result.Success();
		}

	}
}
