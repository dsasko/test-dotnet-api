# Run server
```sh
dotnet run
```

# How the project was set up

1. Initializing the project
```sh
dotnet new webapi
```

2. Adding the .gitignore
```sh
dotnet new gitignore
```

3. Added the `.env` file for Aadeus API secret keys

4. Installed the `dotenv` package for reading `.env` files
```sh
dotnet add package dotenv.net
```

5. Initialize git

# TODO
- [ ] expose city-search endpoint
- [ ] cache results
- [ ] add proper error handling