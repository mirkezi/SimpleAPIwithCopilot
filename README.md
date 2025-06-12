# Simple API app with Copilot 
Hi! This repo contains a simple web API application made in ASP.NET CORE.
The scope of this project was to work and understand HTTP methods related to CRUD operations and the use of middleware, either built-in or custom.
The course "Back-End development with .NET" was entirely focus on building those essential parts of the web back-end development and the approach on CoPilot to debug, refactor or create code.

# Projects requirement:
1. Functional: The application should allow access to api only to authorized personel and provide CRUD operation power on a list of users.

# Application Design:
For this application, also taking in consideration the use of CoPilot as peer, the code sits entirely on the Program.cs File.
1. Models definition to further use.
2. List creation for memory storage in session.
3. Middleware -> Built-in: authentication, authorization, Custom: global error-handling and logging
4. Minimal APIs one for each http method.
5. JWT Bearer token auth applied as extension of methods.

