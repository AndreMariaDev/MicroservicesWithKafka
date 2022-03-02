#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TimeOff.csproj", "TimeOff/"]
RUN dotnet restore "TimeOff/TimeOff.csproj"
COPY . .
WORKDIR "/src/TimeOff"
RUN dotnet build "TimeOff.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TimeOff.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TimeOff.dll"]