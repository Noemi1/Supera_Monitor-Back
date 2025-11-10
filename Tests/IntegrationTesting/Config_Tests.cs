using Xunit;

namespace Tests.IntegrationTesting;
public class Config_Tests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory) {
    [Fact]
    public void Seeding_Worked() {
        int countPerfilCognitivos = _db.PerfilCognitivos.ToList().Count;
        Assert.Equal(11, countPerfilCognitivos);

        int countProfessors = _db.Professor.ToList().Count;
        Assert.Equal(3, countProfessors);

        int countEventoTipos = _db.Evento_Tipos.ToList().Count;
        Assert.Equal(6, countEventoTipos);

        int countApostilaKitRels = _db.Apostila_Kit_Rels.ToList().Count;
        Assert.Equal(8, countApostilaKitRels);
    }

    [Fact]
    public void Views_Worked() {
        var professorsList = _db.ProfessorLists.ToList();
        Assert.Equal(3, professorsList.Count);
    }
}
