# Kobalt — A privacy-first Discord bot.

First and foremost, we **respect your privacy**. All data collection is opt-*in*.

This project is built on the foundational work of [Silk!](https://silkbot.cc/src)

As it stands right now, this project is not only in a very early stage, but also very proof-of-concept-y.

Here's a topological view of the layout of the project from a service point-of-view.
![image](https://user-images.githubusercontent.com/42438262/235146588-d9f82610-665d-404c-a7b5-995bbd0ba23e.png)

## Development & Debugging / Running the bot

Due to the highly-interconnected nature of microservices, several docker-compose files under the `build` directory have been provided to run various services and dependencies.. 
`services.docker-compose.yml` runs Postgres, Redis, and RabbitMQ, which are required for core functionality of the bot and some microservices, however the bot itself does not rely on microservices unless you add plugins.

Running the bot is just as simple as building and running, but you will need to provide some configuration. For testing purposes, of if you're just running Kobalt for a small server, [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0) are probably fine.

`appsettings.json` also works, but if you're planning on opening a PR, ensusure you don't accidentally commit sensitive data.

Your configuration will look something like this, however this format may change in the future. All settings for Kobalt itself are under the `Kobalt` root object. Connection strings and logging are configured in their respective top-level sections.

You can acquire your bot token and public key from the [Discord Developer Dashboard](https://discord.com/developers/applications). The public key is only necessary if you enable HTTP interactions.

A more complete `appsettings.json` example for the main bot (`Kobalt.Bot` project) is shown below:

```json
{
  "Kobalt": {
    "Bot": {
      "OwnerIDs": [ 12345678901234567 ], // List of Discord User IDs for bot owners
      "DefaultActivityType": "Playing",   // Bot's presence: Playing, Streaming, Listening, Watching, Competing
      "DefaultActivityName": "Kobalt",    // Text for the bot's presence
      "EnableHTTPInteractions": true,     // Whether to use HTTP interactions (requires PublicKey)
      "EnableReminders": true,            // Enable/disable the reminder feature
      "RemindersUrl": "http://localhost:5010", // URL for the Reminders microservice
      "EnableInfractions": true,          // Enable/disable the infractions/moderation feature
      "InfractionsUrl": "http://localhost:5020", // URL for the Infractions microservice
      "EnablePhishing": true,             // Enable/disable anti-phishing feature
      "PhishingUrl": "http://localhost:5030"   // URL for the Phishing microservice
    },
    "Discord": {
      "Token": "YOUR_DISCORD_BOT_TOKEN",    // Your Discord bot token (required)
      "PublicKey": "YOUR_DISCORD_PUBLIC_KEY", // Your bot's public key (only if HttpInteractionsEnabled is true)
      "ShardCount": 1                       // Number of shards for the bot
    }
  },
  "ConnectionStrings": {
    "Kobalt": "Server=localhost;Port=5432;Database=kobalt;Username=kobalt;Password=kobalt;", // PostgreSQL
    "Redis": "localhost:6379",                                                              // Redis
    "RabbitMQ": "amqp://kobalt:kobalt@localhost:5672"                                       // RabbitMQ
  },
  "Serilog": { // Optional: Configure logging levels and outputs
    "MinimumLevel": {
      "Default": "Information", // Overall minimum logging level
      "Override": { // Specific overrides for different sources
        "Microsoft": "Warning",
        "System": "Warning",
        "Remora": "Information", // Logging from the Discord library
        "Kobalt": "Debug"      // Logging from Kobalt's own code
      }
    },
    "WriteTo": [
      { "Name": "Console" } // Log to the console
      // Example: Log to a file, daily rolling
      // {
      //   "Name": "File",
      //   "Args": {
      //     "path": "logs/kobalt-.log",
      //     "rollingInterval": "Day",
      //     "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
      //   }
      // }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ], // Add extra info to logs
    "Properties": { // Global properties to add to all log events
      "Application": "KobaltBot"
    }
  }
}
```

> [!NOTE]
> The `Kobalt:Discord:PublicKey` can be omitted if you set `Kobalt:Bot:EnableHTTPInteractions` to `false` (or the corresponding environment variable `Kobalt__Bot__EnableHTTPInteractions=false`).
> API URLs for microservices (`RemindersUrl`, `InfractionsUrl`, `PhishingUrl`) are only needed if their respective `Enable` flags are `true`.

The configuration system uses .NET's built-in mechanisms. This means you can also set these values using:
1.  User Secrets (especially for `Token` during development).
2.  Environment Variables (e.g., `Kobalt__Discord__Token=YOUR_TOKEN`, `Kobalt__Bot__OwnerIDs__0=12345`). Note the double underscore `__` for nesting and array indexing.

## Feature checklist (AKA a Roadmap)

- [x] Kobalt (The bot itself)
    - [x] Configuration
    - [ ] Entertainment (TBD)
        - [ ] RPG/MUD? (Plays well in Discord)
    - [x] Moderation (See `Infraction API`)* (Cases need an update command, but this is otherwise complete)
    - [ ] Utility
        - [ ] ID Search/Info
        - [x] Reminders
        - [x] Role Menus
        - [x] Timezone/Timestamp helper
        - [x] Push-to-Talk (PTT) Threshold

- [x] Infraction API  
    - [x] Infractions
        - [x] Create infraction (`PUT /infractions/guilds/{guildID}`)
        - [x] Get guild infractions (`GET /infractions/guilds/{guildID}`)
        - [x] Update infraction (`PATCH /infractions/guilds/{guildID}/{id}`)
        - [x] Get user infractions (`GET /infractions/guilds/{guildID}/users/{id}`)
    - [x] Infraction Rules
        - [x] Get guild infraction rules (`GET /infractions/guilds/{guildID}/rules`)
        - [x] Create infraction rule (`POST /infractions/guilds/{guildID}/rules`)
        - [x] Update infraction rule (`PATCH /infractions/guilds/{guildID}/rules/{id}`)
        - [x] Delete infraction rule (`DELETE /infractions/guilds/{guildID}/rules/{id}`)
    - [x] Infraction History
    - [ ] Infraction Exemptions (TBD?)
    - [x] Infraction Logging (Handled by Kobalt)
    - [x] Infraction Dispatch (Requires support on Kobalt)

- [ ] Artist Authentication API
    - [ ] Reverse Image Search
    - [ ] Artist Verification (Likely a manual process)

- [ ] Dashboard
    - [ ] Manage Reminders
    - [ ] Manage Guild Configuration
    - [ ] Manage Infraction Rules
    - [ ] Manage Infraction Exemptions
    - [ ] Manage Infraction Logging
    - [ ] Artist Verification Registration

- [x] Reminder Microservice
    - [x] Create Reminder
    - [x] Get Reminder
    - [x] Update Reminder
    - [x] Delete Reminder
    - [x] Dispatch Reminder (Requires Kobalt Support)

- [x] Anti-Phish Microservice
    - [x] Handle aggregated phishing sources
    - [x] Expose API for Kobalt to query
