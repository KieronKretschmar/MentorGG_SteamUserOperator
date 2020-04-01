# SteamUserOperator
Provides data of Steam users like Steamname, Steamicon. Data is queried from [Steam Web Api](https://developer.valvesoftware.com/wiki/Steam_Web_API), particularly the [GetPlayerSummaries endpoint.](https://developer.valvesoftware.com/wiki/Steam_Web_API#GetPlayerSummaries_.28v0002.29)

## When is steam data updated?
When steam data is requested for multiple users, all their data is updated if at least one of them can not be served from cache.
Expiry is defined by setting the environment variable `EXPIRE_AFTER_DAYS` to the desired number of days (e.g. `7`).

## Usage
Since usage of the Steam Web Api is limited to 100.000 daily calls some measures need to be taken:
- Up to 100 users can be queried with a single API call, therefore querying multiple users at once is encouraged.

## Enviroment Variables
- `STEAM_API_KEY` : Steam API Key, required for [Steam Web Api](https://developer.valvesoftware.com/wiki/Steam_Web_API) [\*]
- `REDIS_CONFIGURATION_STRING` : Configuration string for the redis cache for SteamUser data [\*]
- `EXPIRE_AFTER_DAYS` : Number of days after which redis entries created by SteamUserOperator expire [\*]

[\*] *Required*

## Notes
- There is room for optimization regarding usage of fewer api calls, e.g. aggregating users from different requests and querying them together
