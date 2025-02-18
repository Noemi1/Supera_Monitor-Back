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
               <p>Aviso: Esta mensagem automática é destinada exclusivamente à(s) pessoa(s) a quem foi endereçada e pode conter informações confidenciais e protegidas por leis. Se você não for o destinatário pretendido desta mensagem, fica notificado a se abster de divulgar, copiar, distribuir, examinar ou, de qualquer forma, usar as informações contidas nesta mensagem, pois isso é ilegal. Se você recebeu esta mensagem por engano, por favor, responda a esta mensagem informando-nos sobre o ocorrido.</p>";

        public const string HEADER =
            $@"
              <header
                  style=""
                    width: 100%;
                    color: white;
                    margin: auto;
                    text-align: center;
                  ""
              >
                <img style=""height: 96px; padding: 16px 0px;"" src=""cid:logoImage"" role=""presentation"" />
              </header>
            ";

        public const string FOOTER =
            $@"
                <footer
                  style=""
                    color: white;

                    margin: auto;
                    text-align: center;
                    padding: 24px;
                    font-variant: small-caps;
                    font-weight: 500;
                  ""
                >
                  <a
                    href=""tel:+1199999999""
                    style=""
                      text-decoration: none;
                      color: white;
                      font-size: 16px;
                      margin-right: 12px;
                    ""
                    >
                        <img style=""height: 20px; width: 20px; margin-right: 4px; vertical-align: middle;"" src=""cid:phoneIcon"" />
                        (11) 9999-9999
                  </a>
                  <a
                    href=""mailto:supera@brigadeiro.com.br""
                    style=""
                      text-decoration: none;
                      color: white;
                      font-size: 16px;
                      margin-left: 12px;
                    ""
                    >
                        <img style=""height: 20px; width: 20px; margin-right: 4px; vertical-align: middle;"" src=""cid:emailIcon"" />
                        supera@brigadeiro.com.br
                  </a>
                </footer>
            ";

        public static string StyledHeading(string subject)
        {
            return $"<h1 style=\"font-weight: 700; font-size: 32px\">{subject}</h1>";
        }

        public static string TemplateBody(string content)
        {
            return
                @$"
                    <div
                        style=""
                          padding: 0;
                          margin: 0;
                          font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI',
                          Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue',
                          sans-serif;
                          text-align: center;
                          margin: auto;
                        ""
                    >
                      <div
                        style=""
                          margin: 8px;
                          box-shadow: rgba(0, 0, 0, 0.24) 0px 3px 8px;
                          border-radius: 20px;
                          background-color: #f37435;
                        ""
                      >
                        {content}
                      </div>
                    </div>
                ";
        }

        public static string TemplateMessage(string content)
        {
            return
                $@"
                    <div
                      style=""
                        color: #1c1b1c;
                        text-align: center;
                        background-color: #fafafa;
                        border: 2px solid rgba(0, 0, 0, 0.1);
                        border-top: none;
                        border-bottom: none;    
                        margin: auto;
                        padding: 36px 24px;
                      ""
                    >
                        {content}
                    </div>
                ";
        }
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

            return EmailConstants.TemplateBody(
                EmailConstants.HEADER +
                EmailConstants.TemplateMessage(
                    EmailConstants.StyledHeading(Subject) +
                    message +
                    EmailConstants.PASSWORD_DISCLAIMER
                ) +
                EmailConstants.FOOTER
            );
        }
    }

    public class ForgotPasswordEmailTemplate : IEmailTemplate {
        public string Subject => "Supera - Esqueci minha senha";

        public string GenerateBody(object model)
        {
            var data = model as ForgotPasswordModel ?? throw new ArgumentException("Invalid model");
            var resetUrl = $"{data.Url}/accounts/reset-password?token={data.ResetToken}";


            string message = $@"
                <div style=""font-size: 16px"">
                    <p style=""margin-bottom: 40px"">
                        Por favor, siga as instruções abaixo para redefinir a sua senha:
                    </p>

                    <a
                        href=""{resetUrl}""
                        style=""
                        text-decoration: none;
                        color: white;
                        background-color: #f37435;
                        padding: 12px 24px;
                        border-radius: 1000px;
                        font-weight: 700;
                        white-space: nowrap;
                        ""
                    >
                        CLIQUE AQUI
                    </a>

                    <p style=""font-size: 14px; margin: 32px 0"">OU ACESSE:</p>

                    <a
                        href=""{resetUrl}""
                        style=""
                        text-decoration: none;
                        color: #f37435;
                        font-style: italic;
                        font-weight: 500;
                        word-wrap: break-word;
                        ""
                    >
                        {resetUrl}
                    </a>
                </div>
            ";



            return EmailConstants.TemplateBody(
                EmailConstants.HEADER +
                EmailConstants.TemplateMessage(
                    EmailConstants.StyledHeading(Subject) +
                    message +
                    EmailConstants.PASSWORD_DISCLAIMER
                ) +
                EmailConstants.FOOTER
            );
        }
    }

    public class PasswordResetEmailTemplate : IEmailTemplate {
        public string Subject => "Supera - Senha resetada";

        public string GenerateBody(object model)
        {
            var data = model as PasswordResetModel ?? throw new ArgumentException("Invalid model");

            string message =
                $@"
                    <p>Sua senha foi resetada por um administrador.</p>
                    <p>Use sua nova senha abaixo para realizar o login.</p>
                    <p>Nova senha: <b> {data.RandomPassword} </b></p>
                ";

            return EmailConstants.TemplateBody(
                EmailConstants.HEADER +
                EmailConstants.TemplateMessage(
                    EmailConstants.StyledHeading(Subject) +
                    message +
                    EmailConstants.PASSWORD_DISCLAIMER
                ) +
                EmailConstants.FOOTER
            );
        }
    }
}
