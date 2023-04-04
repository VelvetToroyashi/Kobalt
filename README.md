# Kobalt - The bot for everyone

Kobalt is, or at least *attempts* to be a bot for everyone.

From moderation, to fun, to utility, Kobalt has it all.

This project is built on the foundational work of [Silk!](https://silkbot.cc/src)

As it stands right now, this project is not only in a very early stage, but also very proof-of-concept-y.

## Feature checklist (AKA a Roadmap)

- [ ] Kobalt (The bot itself)
    - [ ] Configuration
    - [ ] Entertainment (TBD)
        - [ ] RPG/MUD? (Plays well in Discord)
    - [ ] Moderation (See `Infraction API`)
    - [ ] Utility
        - [ ] ID Search/Info
        - [ ] Reminders
        - [ ] Role Menus
        - [ ] Timezone/Timestamp helper
        - [ ] Push-to-Talk (PTT) Threshold

- [ ] Infraction API  
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
    - [ ] Infraction Logging (Handled by Kobalt)
    - [ ] Infraction Dispatch (Requires support on Kobalt)

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

- [ ] Reminder Microservice
    - [ ] Create Reminder
    - [ ] Get Reminder
    - [ ] Update Reminder
    - [ ] Delete Reminder
    - [ ] Dispatch Reminder (Requires Kobalt Support)

- [ ] Anti-Phish Microservice
    - [ ] Handle aggregated phishing sources
    - [ ] Expose API for Kobalt to query
