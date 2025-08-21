using Bogus;
using Supera_Monitor_Back.Entities;
using Supera_Monitor_Back.Helpers;
using BC = BCrypt.Net.BCrypt;

namespace Tests.IntegrationTesting.Factories;

static class AccountFactory {
    public static Account Create(DataContext db, Account? account) {
        var mockAccountGenerator = new Faker<Account>()
            .RuleFor(a => a.Name, f => account?.Name ?? f.Person.FullName)
            .RuleFor(a => a.Email, f => account?.Email ?? f.Internet.Email())
            .RuleFor(a => a.Phone, f => account?.Phone ?? f.Phone.PhoneNumber())
            .RuleFor(a => a.PasswordHash, f => string.IsNullOrEmpty(account?.PasswordHash) ? BC.HashPassword("12345") : BC.HashPassword(account.PasswordHash))
            .RuleFor(a => a.Role_Id, f => account?.Role_Id ?? (int)Role.Assistant)
            .RuleFor(a => a.AcceptTerms, true)
            .RuleFor(a => a.Verified, TimeFunctions.HoraAtualBR())
            .RuleFor(a => a.Created, TimeFunctions.HoraAtualBR());

        var mockAccount = mockAccountGenerator.Generate();

        db.Accounts.Add(mockAccount);
        db.SaveChanges();

        return mockAccount;
    }
}
