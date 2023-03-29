using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ConfigDataProtectionSample;

public static class ProtectedConfigurationExtensions
{
    public static IServiceCollection ConfigureProtected<TOptions>(this IServiceCollection services, IConfigurationSection section) where TOptions : class
        => services.AddSingleton(provider =>
        {
            var dataProtectionProvider = provider.GetRequiredService<IDataProtectionProvider>();
            section = new ProtectedConfigurationSection(dataProtectionProvider, section);

            var options = section.Get<TOptions>();
            return Options.Create(options);
        });

    private class ProtectedConfigurationSection : IConfigurationSection
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly IConfigurationSection _section;
        private readonly Lazy<IDataProtector> _protector;

        public ProtectedConfigurationSection(IDataProtectionProvider dataProtectionProvider, IConfigurationSection section)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _section = section;

            _protector = new Lazy<IDataProtector>(() => dataProtectionProvider.CreateProtector(section.Path));
        }

        public IConfigurationSection GetSection(string key)
            => new ProtectedConfigurationSection(_dataProtectionProvider, _section.GetSection(key));

        public IEnumerable<IConfigurationSection> GetChildren()
            => _section.GetChildren().Select(section => new ProtectedConfigurationSection(_dataProtectionProvider, section));

        public IChangeToken GetReloadToken() => _section.GetReloadToken();

        public string? this[string key]
        {
            get => GetProtectedValue(_section[key]);
            set => _section[key] = value switch
            {
                string s => _protector.Value.Protect(s),
                _ => null
            };
        }

        public string Key => _section.Key;
        public string Path => _section.Path;

        public string? Value
        {
            get => GetProtectedValue(_section.Value);
            set => _section.Value = value switch
            {
                string s => _protector.Value.Protect(s),
                _ => null
            };
        }

        private string? GetProtectedValue(string? value)
            => value switch
            {
                string s => _protector.Value.Unprotect(s),
                _ => null
            };
    }
}