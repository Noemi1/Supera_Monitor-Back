using Supera_Monitor_Back.Models.Email.Constructors;

namespace Supera_Monitor_Back.Models.Email;

public interface IEmailTemplate {
    string Subject { get; }
    string GenerateBody(object model);
}

public class WelcomeEmailTemplate : IEmailTemplate {
    public string Subject => throw new NotImplementedException();

    public string GenerateBody(object model) {
        throw new NotImplementedException();
    }
}

public class VerificationEmailTemplate : IEmailTemplate {
    public string Subject => "Supera - Verificação de conta";

    public string GenerateBody(object model) {
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

    public string GenerateBody(object model) {
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

    public string GenerateBody(object model) {
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

public class ReagendarAulaEmailTemplate : IEmailTemplate {
    public string Subject => "Supera - Aula reagendada";

    public string GenerateBody(object model) {
        var data = model as ReagendarAulaModel ?? throw new ArgumentException("Invalid model");

        string message =
            $@"
                    <p>Olá {data.Name}</p>
                    <p>Uma das aulas em que você está inserido(a) foi reagendada. Confira os detalhes abaixo:</p>
                    <p>Sua aula do dia {data.OldDate:d} às {data.OldDate:t} foi reagendada para o dia <b>{data.NewDate:d} às {data.NewDate:t}</b> com a turma/aula: <b>{data.TurmaName}</b>.</p>
                    <br>                    
                    <p>Compareça na sua unidade Supera nesse novo dia para realizar a aula.</p>
                ";

        return EmailConstants.TemplateBody(
            EmailConstants.HEADER +
            EmailConstants.TemplateMessage(
                EmailConstants.StyledHeading(Subject) +
                message +
                EmailConstants.CONFIDENTIAL_DISCLAIMER
            ) +
            EmailConstants.FOOTER
        );
    }
}

public class AlunoReposicaoEmailTemplate : IEmailTemplate {
    public string Subject => "Supera - Reposição de aula";

    public string GenerateBody(object model) {
        var data = model as AlunoReposicaoEmailModel ?? throw new ArgumentException("Invalid model");

        string message =
            $@"
                    <p>Olá {data.Name}</p>
                    <p>Uma das aulas em que você está inserido(a) foi reagendada. Confira os detalhes abaixo:</p>
                    <p>Sua aula do dia {data.OldDate:d} às {data.OldDate:t} foi reagendada para o dia <b>{data.NewDate:d} às {data.NewDate:t}</b> com a turma/aula: <b>{data.TurmaName}</b>.</p>
                    <br>                    
                    <p>Compareça na sua unidade Supera nesse novo dia para realizar a aula.</p>
                ";

        return EmailConstants.TemplateBody(
            EmailConstants.HEADER +
            EmailConstants.TemplateMessage(
                EmailConstants.StyledHeading(Subject) +
                message +
                EmailConstants.CONFIDENTIAL_DISCLAIMER
            ) +
            EmailConstants.FOOTER
        );
    }
}

public class ProfessorReposicaoEmailTemplate : IEmailTemplate {
    public string Subject => "Supera - Reposição de aula";

    public string GenerateBody(object model) {
        var data = model as ProfessorReposicaoEmailModel ?? throw new ArgumentException("Invalid model");

        string namesTable = $@"
                <table border=""1"" cellspacing=""0"" cellpadding=""5"" style=""border-collapse: collapse; border-spacing: 0; width: 100%; border: 1px solid #ccc; border-radius: 8px; overflow: hidden;"">
                    <tr style=""background-color: #f4f4f4; text-align: center;"">
                        <th style=""padding: 8px; border-bottom: 1px solid #ccc; text-align: left;"">Nome</th>
                        <th style=""padding: 8px; border-bottom: 1px solid #ccc; text-align: left;"">Email</th>
                        <th style=""padding: 8px; border-bottom: 1px solid #ccc; text-align: left;"">Telefone</th>
                    </tr>
                    {string.Join("", data.Pessoas.Select(p => $@"
                    <tr>
                        <td style=""padding: 8px; border-bottom: 1px solid #ccc;"">{p.Nome ?? "Nome não encontrado"}</td>
                        <td style=""padding: 8px; border-bottom: 1px solid #ccc;"">{p.Email ?? "Não informado"}</td>
                        <td style=""padding: 8px; border-bottom: 1px solid #ccc;"">Cel: {p.Celular ?? "Não informado"}    Tel: {p.Telefone ?? "Não informado"}</td>
                    </tr>"))}
                </table>";

        string message =
            $@"
                    <p>Olá {data.Name}</p>
                    <p>O aluno {data.AlunoName} agendou uma reposição para sua aula do dia {data.NewDate:g} com a turma/aula: <b>{data.TurmaName}</b>, que acontece no dia {data.OldDate:g}</p>
                    <p>Segue a lista dos alunos que participarão da aula:</p>
                    {namesTable}
                    <br>
                    <p>Compareça na sua unidade Supera nesse novo dia para realizar a aula.</p>
                ";

        return EmailConstants.TemplateBody(
            EmailConstants.HEADER +
            EmailConstants.TemplateMessage(
                EmailConstants.StyledHeading(Subject) +
                message +
                EmailConstants.CONFIDENTIAL_DISCLAIMER
            ) +
            EmailConstants.FOOTER
        );
    }
}
