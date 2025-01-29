using Supera_Monitor_Back.Services.Email.Models;

namespace Supera_Monitor_Back.Services.Email {
    public interface IEmailTemplate {
        string Subject { get; }
        string GenerateBody(object model);
    }

    public class EmailConstants {
        public const string PASSWORD_DISCLAIMER =
            $@"<br>
               <p>Sua senha é pessoal e intransferível, devendo ser mantida em sigilo e em um ambiente seguro. Não compartilhe sua senha.</p>";

        public const string CONFIDENTIAL_DISCLAIMER =
            $@"<br>
               <p> Aviso: Esta mensagem automática é destinada exclusivamente à(s) pessoa(s) a quem foi endereçada e pode conter informações confidenciais e protegidas por leis. Se você não for o destinatário pretendido desta mensagem, fica notificado a se abster de divulgar, copiar, distribuir, examinar ou, de qualquer forma, usar as informações contidas nesta mensagem, pois isso é ilegal. Se você recebeu esta mensagem por engano, por favor, responda a esta mensagem informando-nos sobre o ocorrido.</p>";
    }

    public class WelcomeEmailTemplate : IEmailTemplate {
        public string Subject => throw new NotImplementedException();

        public string GenerateBody(object model)
        {
            throw new NotImplementedException();
        }
    }

    public class VerificationEmailTemplate : IEmailTemplate {
        public string Subject => "Supera - Verificação de conta";

        public string GenerateBody(object model)
        {
            var data = model as VerificationEmailModel ?? throw new ArgumentException("Invalid model");
            var verifyUrl = $"{data.Url}/accounts/verify-email?token={data.VerificationToken}";

            string message = $@"<p>Foi realizado um registro na plataforma Supera com seu e-mail.</p>
                            <p>Por favor, clique no link abaixo para verificar sua conta.:</p>
                            <p><a href='{verifyUrl}'>{verifyUrl}</a></p>
                            <p>Para realizar o login, insira seu e-mail ({data.Email}) e senha ({data.RandomPassword})</p>
                            <p>Se isso foi um erro, por favor, desconsidere esta mensagem.</p>";

            return $@"<h4>Registro de conta</h4>
                    {message}
                    {EmailConstants.PASSWORD_DISCLAIMER}
                    {EmailConstants.CONFIDENTIAL_DISCLAIMER}";
        }
    }

    public class ForgotPasswordEmailTemplate : IEmailTemplate {
        public string Subject => "Supera - Esqueci minha senha";

        public string GenerateBody(object model)
        {
            var data = model as ForgotPasswordModel ?? throw new ArgumentException("Invalid model");
            var resetUrl = $"{data.Url}/accounts/reset-password?token={data.ResetToken}";

            string message = $" <p>Por favor, siga o link abaixo para redefinir a senha:</p>"
             + $"<p><a href='{resetUrl}'>{resetUrl}</a></p>"
             + $"<p style='color: red'>Obs.: O link é válido por 1 dia.</p>";

            return $@"<h4>Password Reset Email.</h4> 
                    {message}
                    {EmailConstants.PASSWORD_DISCLAIMER}
                    {EmailConstants.CONFIDENTIAL_DISCLAIMER}";
        }
    }

    public class PasswordResetEmailTemplate : IEmailTemplate {
        public string Subject => "Supera - Senha resetada";

        public string GenerateBody(object model)
        {
            var data = model as PasswordResetModel ?? throw new ArgumentException("Invalid model");

            var message = $"<p>Sua senha foi resetada por um administrador.</p>"
                        + $"<p>Use sua nova senha abaixo para realizar o login.</p>"
                        + $"<p>Nova senha: <b> {data.RandomPassword} </b></p>";

            return @$"<h4>E-mail para reset de senha</h4>
                    {message}
                    {EmailConstants.PASSWORD_DISCLAIMER}
                    {EmailConstants.CONFIDENTIAL_DISCLAIMER}";
        }
    }
}
