# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Repcount/Repcount.csproj src/Repcount/
RUN dotnet restore src/Repcount/Repcount.csproj
COPY src/Repcount/ src/Repcount/
RUN dotnet publish src/Repcount/Repcount.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=5109
EXPOSE 5109
ENTRYPOINT ["dotnet", "Repcount.dll"]
