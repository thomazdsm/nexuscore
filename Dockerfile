# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia todos os arquivos .csproj e restaura as dependências primeiro
COPY ["NexusCore.sln", "./"]
COPY ["NexusCore.WebApp/NexusCore.WebApp.csproj", "NexusCore.WebApp/"]
COPY ["NexusCore.Domain/NexusCore.Domain.csproj", "NexusCore.Domain/"]
COPY ["NexusCore.Application/NexusCore.Application.csproj", "NexusCore.Application/"]
COPY ["NexusCore.Infra.Data/NexusCore.Infra.Data.csproj", "NexusCore.Infra.Data/"]
COPY ["NexusCore.Infra.IoC/NexusCore.Infra.IoC.csproj", "NexusCore.Infra.IoC/"]
RUN dotnet restore "NexusCore.sln"

# Copia o restante do código fonte
COPY . .
WORKDIR "/src/NexusCore.WebApp"
RUN dotnet build "NexusCore.WebApp.csproj" -c Release -o /app/build

# Estágio de Publicação
FROM build AS publish
RUN dotnet publish "NexusCore.WebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio Final (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Cria um grupo e um usuário não-root para executar a aplicação.
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

# Define o usuário que irá rodar o processo
USER appuser

# Documenta a porta que a aplicação escuta, conforme definido em ASPNETCORE_URLS.
EXPOSE 8080

ENTRYPOINT ["dotnet", "NexusCore.WebApp.dll"]
