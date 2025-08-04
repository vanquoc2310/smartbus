namespace Backend_SmartBus.Services
{
    using System.Net;
    using System.Net.Mail;
    using Microsoft.Extensions.Configuration;

    public class OtpService
    {
        private readonly IConfiguration _config;
        private readonly Dictionary<string, string> _otpStore = new(); // Cache OTP theo email

        public OtpService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<bool> SendOtpAsync(string email)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            _otpStore[email] = otp;

            var mail = new MailMessage();
            mail.From = new MailAddress(_config["Email:From"]);
            mail.To.Add(email);
            mail.Subject = "SmartBus OTP Verification";
            mail.Body = $"Your OTP code is: {otp}";

            using var smtpClient = new SmtpClient(_config["Email:Smtp"], int.Parse(_config["Email:Port"]))
            {
                Credentials = new NetworkCredential(_config["Email:Username"], _config["Email:Password"]),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(mail);
            return true;
        }

        public bool VerifyOtp(string email, string inputOtp)
        {
            return _otpStore.TryGetValue(email, out var correctOtp) && inputOtp == correctOtp;
        }
    }
}
