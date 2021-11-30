# Microsoft Rewards Points Farmer
Allows you to farm Microsoft Rewards points by one click

Work on Windows, linux, macOS

<!--## Dependencies:

 - [.NET Core 3.1 Runtime (Console apps)](https://dotnet.microsoft.com/download/dotnet/3.1/runtime)-->

## Installation:

1 - Download the [Zip file](https://github.com/Tom60chat/Microsoft-Rewards-Farmer-Sharp/releases) and extract it somewhere.


2 - Edit the `Settings.json` file by providing your Microsoft account information to the `Accounts` section.
You can put reward goals, if you want to the `Rewards` section. 

This should look like:

```json
{
  "Accounts": [{
      "Username": "you@domain.com",
      "Password": "yourPassword"
    }],
  "Rewards": [{
      "Title": "Your Reward",
      "Cost": 10000,
      "Discounted": 6000
    }]
}
```

## Running

Start `Microsoft Rewards Farmer.exe`

## Legal Notice
I am not responsible for what you do with this program, USE IT AT YOUR OWN RISK!
This program is made for education, and I learned a lot about using puppetter 😋

Using this application on Microsoft services may cause Microsoft to disqualify you; deactivate your access to the Program or to your Rewards account.
Please read https://www.microsoft.com/en-us/servicesagreement/ in particular the Microsoft Rewards section.


Licensed under WTFPL
