# --- Estágio 1: Build ---
# Use a imagem SDK do .NET correspondente à versão do seu projeto (ex: 8.0)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copiar arquivos de projeto/solução da raiz do contexto
# O contexto é a pasta onde o Dockerfile está (CAJUTALKAPI/)
COPY *.sln .
COPY CajuTalkAPI.csproj .  # <-- CORRIGIDO: Copia da raiz para /source

# Restaurar dependências (referencia o csproj em /source)
RUN dotnet restore "./CajuTalkAPI.csproj" # <-- CORRIGIDO: Caminho direto

# Copiar todo o resto do código fonte da raiz do contexto para /source
COPY . .

# Publicar a aplicação (referencia o csproj em /source)
# Certifique-se que o WORKDIR ainda é /source
RUN dotnet publish "./CajuTalkAPI.csproj" -c Release -o /app/publish --no-restore # <-- CORRIGIDO: Caminho direto

# --- Estágio 2: Runtime ---
# Use a imagem de runtime ASP.NET Core correspondente
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copiar a saída publicada do estágio de build
COPY --from=build /app/publish .

# Expor a porta padrão do Kestrel em contêineres
EXPOSE 8080

# Ponto de entrada para rodar a aplicação
# Certifique-se que "CajuTalkAPI.dll" corresponde ao nome do assembly de saída
ENTRYPOINT ["dotnet", "CajuTalkAPI.dll"]