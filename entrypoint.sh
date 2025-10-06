#!/bin/bash

# Define o diretório das chaves de Data Protection
KEY_DIR="/home/appuser/.aspnet/DataProtection-Keys"

# Garante que o diretório exista e define as permissões corretas
# Isso será executado toda vez que o contêiner iniciar, APÓS a montagem do volume
mkdir -p "$KEY_DIR"
chown -R appuser:appgroup "$KEY_DIR"

# Executa o comando principal da aplicação (o CMD ou ENTRYPOINT original do Dockerfile)
# O "$@" passa adiante quaisquer argumentos, como "dotnet NexusCore.WebApp.dll"
exec "$@"
