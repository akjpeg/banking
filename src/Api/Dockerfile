FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Api/Api.csproj", "src/Api/"]
COPY ["src/Accounts/Accounts.Domain/Accounts.Domain.csproj", "src/Accounts/Accounts.Domain/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/Accounts/Accounts.Infrastructure/Accounts.Infrastructure.csproj", "src/Accounts/Accounts.Infrastructure/"]
COPY ["src/Accounts/Accounts.Application/Accounts.Application.csproj", "src/Accounts/Accounts.Application/"]
COPY ["src/AccountTransactions/AccountTransactions.Infrastructure/AccountTransactions.Infrastructure.csproj", "src/AccountTransactions/AccountTransactions.Infrastructure/"]
COPY ["src/AccountTransactions/AccountTransactions.Application/AccountTransactions.Application.csproj", "src/AccountTransactions/AccountTransactions.Application/"]
COPY ["src/AccountTransactions/AccountTransactions.Domain/AccountTransactions.Domain.csproj", "src/AccountTransactions/AccountTransactions.Domain/"]
COPY ["src/Transfers/Transfers.Infrastructure/Transfers.Infrastructure.csproj", "src/Transfers/Transfers.Infrastructure/"]
COPY ["src/Transfers/Transfers.Application/Transfers.Application.csproj", "src/Transfers/Transfers.Application/"]
RUN dotnet restore "src/Api/Api.csproj"
COPY . .
WORKDIR "/src/src/Api"
RUN dotnet build "./Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
