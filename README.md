# Microsoft Rewards Points Farmer  
Allows you to farm Microsoft Rewards points by one click  

Please read the <a href="https://github.com/Tom60chat/Microsoft-Rewards-Farmer-Sharp#legal"><b>Legal Notice</b></a> before use

Work on Windows, linux and macOS  

## Dependencies:
For `All` platform package, you need [.NET Core 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0/runtime) (Run console apps), otherwise the platform-specific packages are standalone and don't need it.

## Installation:  

1 - Download the [Zip file](https://github.com/Tom60chat/Microsoft-Rewards-Farmer-Sharp/releases) and extract it somewhere.  


2 - Edit the `Settings.json` file inside the extracted folder by providing your Microsoft account information to the `Accounts` section.  
<!--You can put reward goals, if you want to the `Rewards` section. -->

This should look like:

```json
{
  "Accounts": [{
      "Username": "you@domain.com",
      "Password": "yourPassword"
    },
    {
      "Username": "youSecond@domain.com",
      "Password": "yourSecondPassword"
    }]
}
```

## Running

Start `Microsoft Rewards Farmer.exe` inside the extracted folder.
  
Arguments:
 - Headless: `-h`, `-headless` - Hides web browser windows, reducing CPU impact.
 - No session `-ns`, `-nosession` - Disables the session caching system.

## <a name='legal'>Legal Notice</a>
I am not responsible for what you do with this program, USE IT AT YOUR OWN RISK!  
This program is made for education, and I learned a lot about using puppetter 😋  

Using this application on Microsoft services may cause Microsoft to disqualify you; deactivate your access to the Program or to your Rewards account.  
Please read https://www.microsoft.com/en-us/servicesagreement/ in particular the Microsoft Rewards section.  


Licensed under WTFPL  
