## Description

#### Demo Solution comprising 2 Projects:

1. CodeBaseOne - API codebase in .NET 6 with Authentication and Authorization built from scratch - with JWT + Refresh token (received by client as HttpOnly cookie. Runs against PostgreSQL db.
2. CodeBaseTests - tests using xUnit framework.

#### Features:

- xUnit Tests. See below.
- Code-First with migrations. Could equally be db Schema-First using e.g. Scaffold-DbContext.
- Runs against PostgreSQL db.
- Repository Pattern.
- Dependency Injection.
- Basic logging - to file for now but could be to service.
- Exception Middleware - exceptions logged - minimal info returned to client.
- Custom Services Response model.
- Password hashing - HMACSHA512. Hash is stored in db.
- Authentication & Role Based Authorization using Auth + Refresh tokens - role is currently hardcoded but will be derived from User table. Login returns Auth and Refresh JWT tokens - see below.
- Token validation + POC for using GetPrincipalFromToken to extract User Id from Auth token.
- Docker - the API can be deployed as a Docker container.
- Automapper - mapping Entity classes to DTOs.
- Swagger - documenting API endpoints (enabled in Dev mode only).
- Repo includes basic Postman Collection for manual testing.

<br/>

## xUnit Tests

Tests the Auth service class interface - i.e. the 4 Public methods. These in-turn call all of the Private methods ensuring code coverage. These Auth layer tests are using an in-memory db - created fresh for each separate test. The best current advise including MS seems to be against mocking DBContext. Arguably therefore these are Integration rather than Unit tests.

<br/>

## Code Coverage

Using Coverlet to calculated coverage.

**N.B. in this demo I have focused on testing the Auth service layer and have deliberately excluded much of the code from coverage analysis**

| Modules     | Line   | Branch | Method |
| ----------- | ------ | ------ | ------ |
| CodeBaseOne | 84.95% | 70.96% | 100%   |

<br/>

## Appsettings

Will need to provide appsettings to run this project. In a live deployment we would be using a secrets store.

<br/>

## Auth & Refresh Tokens

**Register** `/api/auth/register`

Client submits email address and password. Email format is verified. Checks that User email does not already exist. Validates password strength. Creates hash of password. Creates new User row. The hash is stored.

**N.B. in a deployment there would be an additiobnal series of steps requiring the user to verify the email address. Have implemented similar in other projects**

<br/>

**Login:** `/api/auth/login`

The email address and password are passed as a DTO. If the account exists and the password hash matches then and Auth JWT with a short expiry is returned + a Refresh token with a longer expiry. The Refresh token is returned only as an HttpOnly Secure cookie. Details of the Refresh token are stored in the db. Including a field which allows for them to be invalidated / cancelled if necessary.

If login fails for any reason the Client will receive only Unauthorized 401. The underlying reason is logged.
<br/>

**Refresh:** `/api/auth/refesh-token`

The current Refresh token comes from Request.Cookies. This is first validated - its signature + whether expiry date and User Id match the db record. Also - the token must not previously have been used and must not have been invalidated / cancelled. If token validation fails then the Client will receive only Unauthorized 401. The underlying reason is logged.

A new Auth JWT and a new Refresh token is returned. The db is updated to show that the token has been used.

Any unexpected Exceptions during login and refresh are caught by the Exception Middleware. The exceptions are logged. The client will receive only a 500 error - with no potentially pertinent information.

**N.B. The Controller methods are not additionally wrapped in `Try / Catch` - the Exception Middleware is already doing that.**

<br/>

## Authorization

Products is a demo entity - with associated Controller endpoints and Services layer.
The Products Controller uses attribute routing - and Role based Authorization - for now Users must be Admin - as defined in the token claims. With the exception of `/healthcheck` which is AllowAnonymous.

Additionally - in `/getproducts` there is POC code to demo getting the User Id from the bearer token. User Id is a typical starting point. For example, in a multi-tenancy application we might typically need to know the which Company's the User is a member of.

<br/>

## Swagger

This is only enabled for development builds.

`/swagger/index.html` -- basic documentation of endpoints and operations + schemas for API endpoints. Calls can also be tested.

`/swagger/v1/swagger.json` -- OpenAPI specification document for CodeBaseOne API

<br/>

## Misc Hints

#### Running tests from bash console

`dotnet tests`

`dotnet test /p:CollectCoverage=true`

`dotnet test /p:CollectCoverage=true /p:ExcludeByFile="**/program.cs"`

#### Postman

when testing in Postman either use Authorization Tab (select OAuth 2.0) or Headers tab (add Authorization key and add `Bearer xxxetc` as value.

#### Debugging in VSCode

See launch.json for profiles

#### Console run (bash)

`dotnet run --launch-profile CodeBaseOneDevelopment`

#### Docker Build

(From folder with .sln)
Under Linux:

`docker build -f PostrgresSQLAPI01/Dockerfile --force-rm -t postgresqlapi01 . `
