using Supera_Monitor_Back.Services.Email.Models;

namespace Supera_Monitor_Back.Services.Email {
    public interface IEmailTemplate {
        string Subject { get; }
        string GenerateBody(object model);
    }

    public class EmailConstants {
        public const string PASSWORD_DISCLAIMER =
            $@"<br>
               <p>Your password is personal and non-transferable and must be kept confidential and in a secure environment. Do not share your password.</p>";

        public const string CONFIDENTIAL_DISCLAIMER =
            $@"<br>
               <p> Warning: This automatic message is intended exclusively for the person(s) to whom it is addressed, and may contain confidential and legally protected information. If you are not the intended recipient of this Message, you are hereby notified to refrain from disclosing, copying, distributing, examining or, in any way, using the information contained in this Message, as it is illegal. If you have received this Message by mistake, please reply to this Message informing us of what happened.</p>";
    }

    public class WelcomeEmailTemplate : IEmailTemplate {
        public string Subject => throw new NotImplementedException();

        public string GenerateBody(object model)
        {
            throw new NotImplementedException();
        }
    }

    public class VerificationEmailTemplate : IEmailTemplate {
        public string Subject => "Supera - Account Verification";

        public string GenerateBody(object model)
        {
            var data = model as VerificationEmailModel ?? throw new ArgumentException("Invalid model");
            var verifyUrl = $"{data.Url}/accounts/verify-email?token={data.VerificationToken}";

            string message = $@"<p>A registration was made on Supera_Back with your email.</p>
                            <p>Please click the link below to verify your account.:</p>
                            <p><a href='{verifyUrl}'>{verifyUrl}</a></p>
                            <p>To login, enter your e-mail ({data.Email}) and password ({data.RandomPassword})</p>
                            <p>If this was a mistake, please disregard this message.</p>";

            return $@"<h4>Account Registration</h4>
                    {message}
                    {EmailConstants.PASSWORD_DISCLAIMER}
                    {EmailConstants.CONFIDENTIAL_DISCLAIMER}";
        }
    }

    public class ForgotPasswordEmailTemplate : IEmailTemplate {
        public string Subject => "Supera - Forgot password";

        public string GenerateBody(object model)
        {
            var data = model as ForgotPasswordModel ?? throw new ArgumentException("Invalid model");
            var resetUrl = $"{data.Url}/accounts/reset-password?token={data.ResetToken}";

            string message = $" <p>Please, follow link below to reset password:</p>"
             + $"<p><a href='{resetUrl}'>{resetUrl}</a></p>"
             + $"<p style='color: red'>Obs.: The link is valid for 1 day.</p>";

            return $@"<h4>Password Reset Email.</h4> 
                    {message}
                    {EmailConstants.PASSWORD_DISCLAIMER}
                    {EmailConstants.CONFIDENTIAL_DISCLAIMER}";
        }
    }

    public class PasswordResetEmailTemplate : IEmailTemplate {
        public string Subject => "Supera - Password Reset";

        public string GenerateBody(object model)
        {
            var data = model as PasswordResetModel ?? throw new ArgumentException("Invalid model");

            var message = $"<p>Your password has been reseted by an admin.</p>"
                        + $"<p>Use your new password below to login.</p>"
                        + $"<p>New password: <b> {data.RandomPassword} </b></p>";

            return @$"<h4>Password reset e-mail</h4>
                    {message}
                    {EmailConstants.PASSWORD_DISCLAIMER}
                    {EmailConstants.CONFIDENTIAL_DISCLAIMER}";
        }
    }
}
