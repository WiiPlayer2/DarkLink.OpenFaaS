using System;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace DarkLink.OpenFaaS;

internal class SecretsConfigurationProvider : ConfigurationProvider
{
    private readonly IReadOnlyDictionary<string, string> mapping;

    private const string SECRETS_PATH = "/var/openfaas/secrets/";

    private readonly Encoding utf8 = new UTF8Encoding(false);

    public SecretsConfigurationProvider(IReadOnlyDictionary<string, string> mapping)
    {
        this.mapping = mapping;
    }

    public override void Load()
    {
        var secretsDirectory = new DirectoryInfo(SECRETS_PATH);
        if (!secretsDirectory.Exists)
            return;

        try
        {
            foreach (var fileInfo in secretsDirectory.EnumerateFiles())
            {
                var value = utf8.GetString(File.ReadAllBytes(fileInfo.FullName));
                Data.Add(fileInfo.Name, value);
                if (mapping.TryGetValue(fileInfo.Name, out var mappedKey))
                {
                    Data.Add(mappedKey, value);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}