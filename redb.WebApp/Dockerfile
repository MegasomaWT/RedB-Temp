FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
WORKDIR /app
ENV DB_TYPE=UseNpgsql
EXPOSE 8080
EXPOSE 8081
RUN apt-get -y update 
RUN apt-get -y install curl 
RUN apt-get install -y iputils-ping 
RUN apt-get install -y telnet 


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["redb.WebApp/redb.WebApp.csproj", "redb.WebApp/"]
RUN dotnet restore "./redb.WebApp/./redb.WebApp.csproj"
COPY . .
WORKDIR "/src/redb.WebApp"
RUN dotnet build "./redb.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./redb.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "redb.WebApp.dll"]