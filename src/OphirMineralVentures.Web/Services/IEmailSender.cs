using OphirMineralVentures.Web.Models;

namespace OphirMineralVentures.Web.Services;

public interface IEmailSender
{
    Task SendAsync(string to, ContactFormModel model, CancellationToken cancellationToken = default);
}
