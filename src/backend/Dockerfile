# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ClarityBoard.Domain/*.csproj src/ClarityBoard.Domain/
COPY src/ClarityBoard.Application/*.csproj src/ClarityBoard.Application/
COPY src/ClarityBoard.Infrastructure/*.csproj src/ClarityBoard.Infrastructure/
COPY src/ClarityBoard.API/*.csproj src/ClarityBoard.API/
RUN dotnet restore src/ClarityBoard.API/ClarityBoard.API.csproj

COPY . .
RUN dotnet publish src/ClarityBoard.API -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Native dependencies for Docnet.Core (PDFium wrapper for PDF rasterization)
RUN apk add --no-cache libstdc++ libgcc

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ClarityBoard.API.dll"]
