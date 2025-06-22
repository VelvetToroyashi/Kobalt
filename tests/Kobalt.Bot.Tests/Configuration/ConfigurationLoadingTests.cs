using System.IO;
using Kobalt.Infrastructure.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework; // Changed from Xunit
using System.Collections.Generic; // For IReadOnlyList

namespace Kobalt.Bot.Tests.Configuration;

[TestFixture] // Changed from public class
public class ConfigurationLoadingTests
{
    private KobaltConfig LoadTestConfiguration(string jsonConfig)
    {
        var memoryStream = new MemoryStream();
        var writer = new StreamWriter(memoryStream);
        writer.Write(jsonConfig);
        writer.Flush();
        memoryStream.Position = 0;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(memoryStream)
            .Build();

        var services = new ServiceCollection();
        // Ensure we are binding the "Kobalt" section from the JSON to the KobaltConfig class
        services.Configure<KobaltConfig>(configuration.GetSection("Kobalt"));
        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider.GetRequiredService<IOptions<KobaltConfig>>().Value;
    }

    [Test] // Changed from Fact
    public void Should_Load_Bot_Activity_Settings_Correctly()
    {
        var json = """
        {
          "Kobalt": {
            "Bot": {
              "DefaultActivityType": "Listening",
              "DefaultActivityName": "to your commands"
            }
          }
        }
        """;

        var config = LoadTestConfiguration(json);

        Assert.That(config.Bot, Is.Not.Null);
        Assert.That(config.Bot.DefaultActivityType, Is.EqualTo("Listening"));
        Assert.That(config.Bot.DefaultActivityName, Is.EqualTo("to your commands"));
    }

    [Test] // Changed from Fact
    public void Should_Load_OwnerIDs_Correctly()
    {
        var json = """
        {
          "Kobalt": {
            "Bot": {
              "OwnerIDs": [ 111222333444555, 666777888999000 ]
            }
          }
        }
        """;

        var config = LoadTestConfiguration(json);

        Assert.That(config.Bot, Is.Not.Null);
        Assert.That(config.Bot.OwnerIDs, Is.Not.Null);
        Assert.That(config.Bot.OwnerIDs.Count, Is.EqualTo(2));
        CollectionAssert.Contains(config.Bot.OwnerIDs, 111222333444555UL);
        CollectionAssert.Contains(config.Bot.OwnerIDs, 666777888999000UL);
    }

    [Test] // Changed from Fact
    public void Should_Use_Default_Activity_Settings_When_Not_Provided()
    {
        var jsonWithDiscord = """
        {
          "Kobalt": {
            "Bot": {
            },
            "Discord": {
              "Token": "TestToken",
              "ShardCount": 1
            }
          }
        }
        """;

        var config = LoadTestConfiguration(jsonWithDiscord);

        Assert.That(config.Bot, Is.Not.Null);
        // Default values from KobaltBotConfig record definition
        Assert.That(config.Bot.DefaultActivityType, Is.EqualTo("Watching"));
        Assert.That(config.Bot.DefaultActivityName, Is.EqualTo("Code being written"));
    }

    [Test] // Changed from Fact
    public void Should_Handle_Empty_OwnerIDs_When_Not_Provided()
    {
        var jsonWithDiscord = """
        {
          "Kobalt": {
            "Bot": {
            },
            "Discord": {
              "Token": "TestToken",
              "ShardCount": 1
            }
          }
        }
        """;
        var config = LoadTestConfiguration(jsonWithDiscord);

        Assert.That(config.Bot, Is.Not.Null);
        Assert.That(config.Bot.OwnerIDs, Is.Null); // Default value is null from record definition
    }

    [Test] // Changed from Fact
    public void Should_Load_Full_Example_Configuration_Segment_Correctly()
    {
        var json = """
        {
          "Kobalt": {
            "Bot": {
              "OwnerIDs": [ 9876543210, 1234567890 ],
              "DefaultActivityType": "Playing",
              "DefaultActivityName": "a game",
              "EnableReminders": false,
              "InfractionsUrl": "http://localhost:1234"
            },
            "Discord": {
              "Token": "FakeToken",
              "ShardCount": 2,
              "PublicKey": "FakeKey"
            }
          }
        }
        """;

        var config = LoadTestConfiguration(json);

        Assert.That(config.Bot, Is.Not.Null);
        Assert.That(config.Bot.DefaultActivityType, Is.EqualTo("Playing"));
        Assert.That(config.Bot.DefaultActivityName, Is.EqualTo("a game"));
        Assert.That(config.Bot.EnableReminders, Is.False);
        Assert.That(config.Bot.InfractionsUrl, Is.EqualTo("http://localhost:1234"));
        Assert.That(config.Bot.OwnerIDs, Is.Not.Null);
        CollectionAssert.Contains(config.Bot.OwnerIDs, 9876543210UL);
        CollectionAssert.Contains(config.Bot.OwnerIDs, 1234567890UL);

        Assert.That(config.Discord, Is.Not.Null);
        Assert.That(config.Discord.Token, Is.EqualTo("FakeToken"));
        Assert.That(config.Discord.ShardCount, Is.EqualTo(2));
        Assert.That(config.Discord.PublicKey, Is.EqualTo("FakeKey"));
    }
}
