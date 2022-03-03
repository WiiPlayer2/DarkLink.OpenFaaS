using System;
using Microsoft.Extensions.Configuration;

namespace DarkLink.OpenFaaS;

internal class SecretsConfigurationSource : IConfigurationSource
{
    private readonly IReadOnlyDictionary<string, string> mapping;

    public SecretsConfigurationSource(IReadOnlyDictionary<string, string> mapping)
    {
        this.mapping = mapping;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new SecretsConfigurationProvider(mapping);
}
