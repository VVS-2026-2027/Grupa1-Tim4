using ezZkvi.Models;
using ezZkvi.Services;
using ezZkvi.Tests.Infrastruktura;
using Microsoft.Extensions.Options;

namespace ezZkvi.Tests.Services;

/// <summary>
/// Testovi slanja emaila: validacija SMTP konfiguracije i pretvaranje grešaka slanja u InvalidOperationException.
/// Pravi SMTP server se ne koristi: za "ispravne" postavke cilja se lokalni port na kojem niko ne sluša.
/// Data-driven: XML sa neispravnim postavkama.
/// Autor: Amir Džafić (C)
/// </summary>
[TestClass]
public sealed class EmailServiceTests
{
    private static EmailService Servis(string server, int port, string senderEmail, string username, string password)
    {
        return new EmailService(Options.Create(new EmailSettings
        {
            SmtpServer = server,
            SmtpPort = port,
            SenderName = "eZkvi",
            SenderEmail = senderEmail,
            Username = username,
            Password = password
        }));
    }

    public static IEnumerable<object[]> NeispravnePostavke =>
        TestPodaci.Xml("email_postavke.xml", "slucaj").Select(e => new object[]
        {
            (string)e.Attribute("server")!,
            TestPodaci.Int((string)e.Attribute("port")!),
            (string)e.Attribute("senderEmail")!,
            (string)e.Attribute("username")!,
            (string)e.Attribute("password")!
        });

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Exception verifier")]
    [DynamicData(nameof(NeispravnePostavke), DynamicDataSourceType.Property)]
    public async Task SendEmailAsync_NepotpunaKonfiguracija_BacaInvalidOperationException(
        string server, int port, string senderEmail, string username, string password)
    {
        var servis = Servis(server, port, senderEmail, username, password);

        var izuzetak = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => servis.SendEmailAsync("student@etf.ba", "Naslov", "Poruka"));

        StringAssert.StartsWith(izuzetak.Message, "Email konfiguracija nije ispravno podešena.");
        Assert.IsNull(izuzetak.InnerException);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Exception verifier")]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task SendEmailAsync_BezLozinke_BacaInvalidOperationException()
    {
        var servis = Servis("smtp.test.ba", 587, "ezkvi@test.ba", "ezkvi", "");

        await servis.SendEmailAsync("student@etf.ba", "Naslov", "Poruka");
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Exception verifier")]
    [Timeout(30000)]
    public async Task SendEmailAsync_SmtpServerNedostupan_OmotavaGreskuUInvalidOperationException()
    {
        var servis = Servis("127.0.0.1", 1, "ezkvi@test.ba", "ezkvi", "tajna");

        var izuzetak = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => servis.SendEmailAsync("student@etf.ba", "Naslov <b>", "Prva linija\nDruga linija"));

        StringAssert.StartsWith(izuzetak.Message, "Email nije poslan.");
        Assert.IsNotNull(izuzetak.InnerException);
    }
}
