# --- Estágio 1: Build ---
# Use a imagem SDK do .NET correspondente à versão do seu projeto (ex: 8.0, 7.0, 6.0)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copiar arquivos de projeto/solução primeiro para aproveitar o cache do Docker
# Ajuste o nome "CajuTalkAPI.csproj" se o seu arquivo .csproj tiver outro nome ou estiver em outra pasta
COPY *.sln .
COPY CajuTalkAPI/*.csproj ./CajuTalkAPI/
# Adicione linhas COPY extras se tiver outros projetos na solução

# Restaurar dependências
RUN dotnet restore "./CajuTalkAPI/CajuTalkAPI.csproj"
# Se restaurar a solução for mais fácil: RUN dotnet restore "./CajuTalkAPI.sln"

# Copiar todo o resto do código fonte
COPY . .

# Publicar a aplicação em modo Release
# Ajuste o caminho do .csproj se necessário
WORKDIR /source/CajuTalkAPI
RUN dotnet publish "./CajuTalkAPI.csproj" -c Release -o /app/publish --no-restore

# --- Estágio 2: Runtime ---
# Use a imagem de runtime ASP.NET Core correspondente (mais leve que a SDK)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copiar a saída publicada do estágio de build
COPY --from=build /app/publish .

# Definir a variável de ambiente para a porta (Render usa a variável PORT, mas ASP.NET Core 8+ usa 8080 por padrão)
# Kestrel geralmente escuta em http://+:8080 por padrão em contêineres .NET 8+
# Você pode descomentar a linha abaixo se quiser ser explícito ou se o Render exigir especificamente URLs
# ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Ponto de entrada para rodar a aplicação
# Certifique-se que "CajuTalkAPI.dll" corresponde ao nome do assembly de saída do seu projeto
ENTRYPOINT ["dotnet", "CajuTalkAPI.dll"]