using food_market_narrator_api.DTOs.Auth;
using food_market_narrator_api.Helpers;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;

namespace food_market_narrator_api.Services
{
    public enum ForgotPasswordSendOtpStatus
    {
        Success,
        InvalidInput,
        UsernameNotFound,
        EmailMismatch,
        NotFoundBoth,
        EmailNotFound,
        EmailDeliveryFailed
    }

    public sealed class ForgotPasswordSendOtpResult
    {
        public ForgotPasswordSendOtpStatus Status { get; set; }
        public int ExpiresInSeconds { get; set; }
    }

    public enum ForgotPasswordVerifyOtpStatus
    {
        Success,
        InvalidInput,
        UsernameNotFound,
        EmailMismatch,
        NotFoundBoth,
        EmailNotFound,
        InvalidOtp,
        OtpExpired
    }

    public sealed class ForgotPasswordVerifyOtpResult
    {
        public ForgotPasswordVerifyOtpStatus Status { get; set; }
    }

    public enum ForgotPasswordResetStatus
    {
        Success,
        InvalidInput,
        UsernameNotFound,
        EmailMismatch,
        NotFoundBoth,
        EmailNotFound,
        InvalidOtp,
        OtpExpired,
        InvalidNewPassword
    }

    public sealed class ForgotPasswordResetResult
    {
        public ForgotPasswordResetStatus Status { get; set; }
    }

    public class AuthService
    {
        private const int OtpLifetimeSeconds = 120;
        private readonly UserRepository _userRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<AuthService> _logger;

        private sealed record PasswordOtpEntry(string Code, DateTimeOffset ExpiresAt);

        public AuthService(
            UserRepository userRepository,
            IMemoryCache memoryCache,
            IOptions<SmtpSettings> smtpOptions,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _memoryCache = memoryCache;
            _smtpSettings = smtpOptions.Value;
            _logger = logger;
        }

        public async Task<UserModel?> ValidateCredentialsAsync(string username, string password)
        {
            bool isValid = await _userRepository.ValidateCredentialsAsync(username, password);
            if (!isValid)
            {
                return null;
            }

            return await _userRepository.GetByUsernameAsync(username);
        }

        public async Task<LoginResponse?> LoginAsync(string username, string password)
        {
            UserModel? user = await ValidateCredentialsAsync(username, password);
            if (user == null)
            {
                return null;
            }

            return new LoginResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role,
                IsActive = user.IsActive
            };
        }

        public async Task<MeResponse?> GetMeAsync(string username)
        {
            UserModel? user = await _userRepository.GetByUsernameAsync(username);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            return new MeResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role
            };
        }

        public async Task<MeResponse?> GetMeByUserIdAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            return new MeResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role
            };
        }

        public async Task<ForgotPasswordSendOtpResult> SendForgotPasswordOtpAsync(string username, string email)
        {
            var normalizedUsername = NormalizeValue(username);
            var normalizedEmail = NormalizeValue(email);

            if (string.IsNullOrWhiteSpace(normalizedUsername) || string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return new ForgotPasswordSendOtpResult { Status = ForgotPasswordSendOtpStatus.InvalidInput };
            }

            var user = await _userRepository.GetByUsernameAsync(normalizedUsername);
            var emailExists = await _userRepository.ExistsByEmailAsync(normalizedEmail);

            if (user == null && !emailExists)
            {
                return new ForgotPasswordSendOtpResult { Status = ForgotPasswordSendOtpStatus.NotFoundBoth };
            }

            if (user == null)
            {
                return new ForgotPasswordSendOtpResult { Status = ForgotPasswordSendOtpStatus.UsernameNotFound };
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return new ForgotPasswordSendOtpResult { Status = ForgotPasswordSendOtpStatus.EmailNotFound };
            }

            if (!IsSameEmail(user.Email, normalizedEmail))
            {
                return new ForgotPasswordSendOtpResult { Status = ForgotPasswordSendOtpStatus.EmailMismatch };
            }

            var otpCode = GenerateSixDigitOtp();
            var sent = await TrySendOtpEmailAsync(normalizedEmail, normalizedUsername, otpCode);
            if (!sent)
            {
                return new ForgotPasswordSendOtpResult { Status = ForgotPasswordSendOtpStatus.EmailDeliveryFailed };
            }

            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(OtpLifetimeSeconds);
            _memoryCache.Set(
                BuildOtpCacheKey(normalizedUsername, normalizedEmail),
                new PasswordOtpEntry(otpCode, expiresAt),
                expiresAt);

            return new ForgotPasswordSendOtpResult
            {
                Status = ForgotPasswordSendOtpStatus.Success,
                ExpiresInSeconds = OtpLifetimeSeconds
            };
        }

        public async Task<ForgotPasswordVerifyOtpResult> VerifyForgotPasswordOtpAsync(string username, string email, string otp)
        {
            var normalizedUsername = NormalizeValue(username);
            var normalizedEmail = NormalizeValue(email);
            var normalizedOtp = NormalizeValue(otp);

            if (string.IsNullOrWhiteSpace(normalizedUsername)
                || string.IsNullOrWhiteSpace(normalizedEmail)
                || string.IsNullOrWhiteSpace(normalizedOtp))
            {
                return new ForgotPasswordVerifyOtpResult { Status = ForgotPasswordVerifyOtpStatus.InvalidInput };
            }

            var user = await _userRepository.GetByUsernameAsync(normalizedUsername);
            var emailExists = await _userRepository.ExistsByEmailAsync(normalizedEmail);

            if (user == null && !emailExists)
            {
                return new ForgotPasswordVerifyOtpResult { Status = ForgotPasswordVerifyOtpStatus.NotFoundBoth };
            }

            if (user == null)
            {
                return new ForgotPasswordVerifyOtpResult { Status = ForgotPasswordVerifyOtpStatus.UsernameNotFound };
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return new ForgotPasswordVerifyOtpResult { Status = ForgotPasswordVerifyOtpStatus.EmailNotFound };
            }

            if (!IsSameEmail(user.Email, normalizedEmail))
            {
                return new ForgotPasswordVerifyOtpResult { Status = ForgotPasswordVerifyOtpStatus.EmailMismatch };
            }

            var cacheKey = BuildOtpCacheKey(normalizedUsername, normalizedEmail);
            if (!_memoryCache.TryGetValue(cacheKey, out PasswordOtpEntry? otpEntry) || otpEntry == null)
            {
                return new ForgotPasswordVerifyOtpResult { Status = ForgotPasswordVerifyOtpStatus.OtpExpired };
            }

            if (DateTimeOffset.UtcNow > otpEntry.ExpiresAt)
            {
                _memoryCache.Remove(cacheKey);
                return new ForgotPasswordVerifyOtpResult { Status = ForgotPasswordVerifyOtpStatus.OtpExpired };
            }

            if (!string.Equals(otpEntry.Code, normalizedOtp, StringComparison.Ordinal))
            {
                return new ForgotPasswordVerifyOtpResult { Status = ForgotPasswordVerifyOtpStatus.InvalidOtp };
            }

            return new ForgotPasswordVerifyOtpResult { Status = ForgotPasswordVerifyOtpStatus.Success };
        }

        public async Task<ForgotPasswordResetResult> ResetForgotPasswordAsync(
            string username,
            string email,
            string otp,
            string newPassword)
        {
            var normalizedUsername = NormalizeValue(username);
            var normalizedEmail = NormalizeValue(email);
            var normalizedOtp = NormalizeValue(otp);
            var normalizedPassword = NormalizeValue(newPassword);

            if (string.IsNullOrWhiteSpace(normalizedUsername)
                || string.IsNullOrWhiteSpace(normalizedEmail)
                || string.IsNullOrWhiteSpace(normalizedOtp)
                || string.IsNullOrWhiteSpace(normalizedPassword))
            {
                return new ForgotPasswordResetResult { Status = ForgotPasswordResetStatus.InvalidInput };
            }

            if (normalizedPassword.Length < 6)
            {
                return new ForgotPasswordResetResult { Status = ForgotPasswordResetStatus.InvalidNewPassword };
            }

            var user = await _userRepository.GetByUsernameAsync(normalizedUsername);
            var emailExists = await _userRepository.ExistsByEmailAsync(normalizedEmail);

            if (user == null && !emailExists)
            {
                return new ForgotPasswordResetResult { Status = ForgotPasswordResetStatus.NotFoundBoth };
            }

            if (user == null)
            {
                return new ForgotPasswordResetResult { Status = ForgotPasswordResetStatus.UsernameNotFound };
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return new ForgotPasswordResetResult { Status = ForgotPasswordResetStatus.EmailNotFound };
            }

            if (!IsSameEmail(user.Email, normalizedEmail))
            {
                return new ForgotPasswordResetResult { Status = ForgotPasswordResetStatus.EmailMismatch };
            }

            var cacheKey = BuildOtpCacheKey(normalizedUsername, normalizedEmail);
            if (!_memoryCache.TryGetValue(cacheKey, out PasswordOtpEntry? otpEntry) || otpEntry == null)
            {
                return new ForgotPasswordResetResult { Status = ForgotPasswordResetStatus.OtpExpired };
            }

            if (DateTimeOffset.UtcNow > otpEntry.ExpiresAt)
            {
                _memoryCache.Remove(cacheKey);
                return new ForgotPasswordResetResult { Status = ForgotPasswordResetStatus.OtpExpired };
            }

            if (!string.Equals(otpEntry.Code, normalizedOtp, StringComparison.Ordinal))
            {
                return new ForgotPasswordResetResult { Status = ForgotPasswordResetStatus.InvalidOtp };
            }

            var updated = await _userRepository.UpdatePasswordAsync(user.UserId, PasswordHasher.Hash(normalizedPassword));
            if (!updated)
            {
                return new ForgotPasswordResetResult { Status = ForgotPasswordResetStatus.UsernameNotFound };
            }

            _memoryCache.Remove(cacheKey);
            return new ForgotPasswordResetResult { Status = ForgotPasswordResetStatus.Success };
        }

        private static string NormalizeValue(string? value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static bool IsSameEmail(string? sourceEmail, string incomingEmail)
        {
            return string.Equals(
                NormalizeValue(sourceEmail),
                NormalizeValue(incomingEmail),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildOtpCacheKey(string username, string email)
        {
            return $"forgot-password::{username.ToLowerInvariant()}::{email.ToLowerInvariant()}";
        }

        private static string GenerateSixDigitOtp()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(4);
            var value = BitConverter.ToUInt32(randomBytes, 0) % 1_000_000;
            return value.ToString("D6");
        }

        private async Task<bool> TrySendOtpEmailAsync(string toEmail, string username, string otpCode)
        {
            var smtpHost = NormalizeValue(_smtpSettings.Host);
            var smtpFromEmail = NormalizeValue(_smtpSettings.FromEmail);
            var smtpUsername = NormalizeValue(_smtpSettings.Username);
            var smtpPassword = string.Concat((_smtpSettings.Password ?? string.Empty).Where(c => !char.IsWhiteSpace(c)));

            if (string.IsNullOrWhiteSpace(smtpHost)
                || _smtpSettings.Port <= 0
                || string.IsNullOrWhiteSpace(smtpFromEmail)
                || string.IsNullOrWhiteSpace(smtpUsername)
                || string.IsNullOrWhiteSpace(smtpPassword))
            {
                _logger.LogWarning("SMTP settings are missing. Cannot send OTP email for user {Username}.", username);
                return false;
            }

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(smtpFromEmail, _smtpSettings.FromName),
                    Subject = "[Food Market Narrator] Ma OTP dat lai mat khau",
                    Body = $"Xin chao {username},\n\nMa OTP cua ban la: {otpCode}\nMa co hieu luc trong 2 phut.\n\nNeu ban khong yeu cau, vui long bo qua email nay.",
                    IsBodyHtml = false
                };
                message.To.Add(new MailAddress(toEmail));

                using var client = new SmtpClient(smtpHost, _smtpSettings.Port)
                {
                    EnableSsl = _smtpSettings.EnableSsl,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword)
                };

                await client.SendMailAsync(message);
                return true;
            }
            catch (SmtpFailedRecipientException ex)
            {
                _logger.LogWarning(ex, "Failed recipient when sending OTP email to {Email}.", toEmail);
                return false;
            }
            catch (SmtpException ex)
            {
                _logger.LogWarning(ex, "SMTP exception when sending OTP email to {Email}.", toEmail);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when sending OTP email to {Email}.", toEmail);
                return false;
            }
        }
    }
}
