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
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# TODO: FIX PARA PROD: Os métodos AddDevelopment...Certificate() não são recomendados para produção. (DI)
# Define variáveis de ambiente para o diretório home do usuário appuser.
ENV HOME=/home/appuser
RUN mkdir -p /home/appuser && chown -R appuser:appgroup /home/appuser

# Define o usuário que irá rodar o processo
USER appuser

# Documenta a porta que a aplicação escuta, conforme definido em ASPNETCORE_URLS.
EXPOSE 8080

ENTRYPOINT ["dotnet", "NexusCore.WebApp.dll"]

# --- ALTERAÇÕES ABAIXO ---
# Copia o novo script de entrypoint para dentro da imagem e define suas permissões
#COPY --chown=appuser:appgroup entrypoint.sh /usr/local/bin/entrypoint.sh

# ADICIONE ESTA LINHA para dar permissão de execução ao script DENTRO da imagem
# RUN chmod +x /usr/local/bin/entrypoint.sh

# Define o script como o ponto de entrada que será executado ao iniciar o contêiner
# ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]

# Define o comando padrão que o entrypoint irá executar após ajustar as permissões
CMD ["dotnet", "NexusCore.WebApp.dll"]
