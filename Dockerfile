# Giai đoạn build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Sao chép các file csproj và restore
COPY *.csproj ./
RUN dotnet restore

# Sao chép toàn bộ mã nguồn
COPY . ./

# Giai đoạn publish
RUN dotnet publish -c Release -o out

# Giai đoạn runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Backend_SmartBus.dll"]