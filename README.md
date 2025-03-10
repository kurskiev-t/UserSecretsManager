# User Secrets Manager extension

## Description and usage

I've made this extension for myself. At the moment it's pretty rough version, any help will be appreaciated.
Here's github page of the project:
[UserSecretsManager](https://github.com/kurskiev-t/UserSecretsManager)

> After installation, extension can be found in **Views -> Other Windows -> SecretsWindow**

It scans a solution for user secrets files for all projects, categorizes them by each section in those files. Groups those sections by the keys and allows to switch between active sections by chosing radio button options.

At the moment it supports only single-line sections in user secrets file. I'm working on making this more universal

User Secrets file example:
```json
{
  // LOCAL
  //"connectionSettings": {
    //"setting1": "server:local_server,port:some_port"
  //},
   //"enableMigrations": true,

  // DEV
  "connectionSettings": {
    "setting1": "server:dev_server,port:some_port"
  },
   "enableMigrations": false,

  // PROD
  //"connectionSettings": {
    //"setting1": "server:prod_server,port:some_port"
  //},

  // Random section
  //"test": 2

  // Random section v2
  "test": 5,

  //"test2": 10,

   "test2":  11,
}
```
Extension will make based on it two groups:

"connectionSettings" with two sections, 1 active, 1 inactive

"enableMigrations" with two sections, 1 active, 1 inactive

See screenshots below:

![Screenshot 1](1.jpg)

![Screenshot 2](2.jpg)

![Screenshot 3](3.jpg)

## Building from Source
1. Clone the repository: `git clone https://github.com/kurskiev-t/UserSecretsManager`
2. Open in Visual Studio 2019+ (Targeted version of VS - Visual Studio 2022 v17.13+).
3. Build in Release mode (`Ctrl+Shift+B`).
4. Install the `.vsix` from `bin/Release`.
