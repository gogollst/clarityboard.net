# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ClarityBoard.sln .
COPY src/ClarityBoard.Domain/*.csproj src/ClarityBoard.Domain/
COPY src/ClarityBoard.Application/*.csproj src/ClarityBoard.Application/
COPY src/ClarityBoard.Infrastructure/*.csproj src/ClarityBoard.Infrastructure/
COPY src/ClarityBoard.API/*.csproj src/ClarityBoard.API/
RUN dotnet restore

COPY . .
RUN dotnet publish src/ClarityBoard.API -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ClarityBoard.API.dll"]
