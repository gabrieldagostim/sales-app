# Build context = raiz do repositório (precisa do Directory.Build.props)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/PagamentoService/ src/PagamentoService/
RUN dotnet publish src/PagamentoService/PagamentoService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
# O Render injeta PORT; o Program.cs já escuta em http://0.0.0.0:$PORT
ENTRYPOINT ["dotnet", "PagamentoService.dll"]
