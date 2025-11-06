FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ./backend/src/CompanyIntelligence.Api/ ./CompanyIntelligence.Api/
RUN dotnet restore CompanyIntelligence.Api/CompanyIntelligence.Api.csproj
RUN dotnet publish CompanyIntelligence.Api/CompanyIntelligence.Api.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CompanyIntelligence.Api.dll"]
