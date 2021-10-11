# Microsoft Rewards Points Farmer
Allows you to farm Microsoft Rewards points by one click

Work on Windows, linux, macOS

## Dependencies:

 - [.NET Core 3.1 Runtime (Console apps)](https://dotnet.microsoft.com/download/dotnet/3.1/runtime)

## Installation:

1 - Download the Zip file and extract it somewhere
[Download the app](https://github.com/Tom60chat/Microsoft-Rewards-Farmer-Sharp/releases)

1 - Edit the `settings.json` file by providing your Microsoft account information to the `accounts` section.
You can put reward goals, if you want to the `rewards` section. 

This should look like:

```json
{
  "Accounts": [
    {
      "Username": "you@domain.com",
      "Password": "yourPassword"
    }],
  "Rewards": [
    {
      "Title": "Your Reward",
      "Cost": 10000,
      "Discounted": 6000
    }]
}
```

## Running

Start `Microsoft Rewards Farmer.exe`

Licensed under WTFPL
