using OphirMineralVentures.Web.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace OphirMineralVentures.Web.Composers;

public class ContactFormComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<EmailSettings>(builder.Config.GetSection("Email"));
        builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();
        builder.Services.AddScoped<ContactFormProcessor>();
    }
}
