# Kobalt — A privacy-first Discord bot.

First and foremost, we **respect your privacy**. All data collection is opt-*in*.

This project is built on the foundational work of [Silk!](https://silkbot.cc/src)

As it stands right now, this project is not only in a very early stage, but also very proof-of-concept-y.

Here's a topological view of the layout of the project from a service point-of-view.
![image](https://user-images.githubusercontent.com/42438262/235146588-d9f82610-665d-404c-a7b5-995bbd0ba23e.png)

## Development & Debugging / Running the bot

Given the inherently...*convoluted* nature of Microservices, this bot isn't trivial to work with, but I've provided a few compose files under the `build` directory. 
`services.docker-compose.yml` runs Postgres, Redis, and RabbitMQ, which are required for core functionality of the bot and some microservices, however the bot itself does not rely on microservices unless you add plugins.

Running the bot is just as simple as building and running, but you will need to provide some configuration. For testing purposes, of if you're just running Kobalt for a small server, [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0) are probably fine.

`appsettings.json` also works, but if you're planning on opening a PR, ensusure you don't accidentally commit sensitive data.

Your configuration will look something like this, however this format may change in the future. 

You can acquire your bot token and public key from the [Discord Developer Dashboard](https://discord.com/developers/applications), however the latter is only necessary if you configure an HTTP endpoint for interactions. This provides better performance for commands, but is wholly unneccessary in most situations.

```json
{
  "Plugins": {
    "Reminders":
    {
      "ApiUrl": "http://localhost:5010"
    },
    "Infractions":
    {
      "ApiUrl": "http://localhost:5020"
    }
  },
  "ConnectionStrings": {
    "Kobalt": "Server=localhost;Database=kobalt;Username=kobalt;Password=kobalt;",
    "RabbitMQ": "rabbitmq://kobalt:kobalt@localhost:5672"
  },
  "Discord": {
    "Token": "Your bot token",
    "ShardCount": 1,
    "PublicKey": "Your Public Key"
  }
}
```

## Feature checklist (AKA a Roadmap)

- [ ] Kobalt (The bot itself)
    - [ ] Configuration
    - [ ] Entertainment (TBD)
        - [ ] RPG/MUD? (Plays well in Discord)
    - [x] Moderation (See `Infraction API`)* (Cases need an update command, but this is otherwise complete)
    - [ ] Utility
        - [ ] ID Search/Info
        - [x] Reminders
        - [ ] Role Menus
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
