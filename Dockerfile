# 使用 .NET SDK 镜像构建后端
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# 定义代理变量
ARG HTTP_PROXY
ARG HTTPS_PROXY
ENV http_proxy=$HTTP_PROXY \
    https_proxy=$HTTPS_PROXY
# 复制项目文件
COPY ["FlashBlazor/FlashBlazor.csproj", "."]
RUN dotnet restore "FlashBlazor.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "FlashBlazor/FlashBlazor.csproj" -c Release -o /app/build

# 发布 .NET 应用
FROM build AS publish
RUN dotnet publish "FlashBlazor/FlashBlazor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 使用基础 ASP.NET 镜像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 9000

# 将构建的文件复制到最终镜像中
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlashBlazor.dll"]