# Use the official ASP.NET Core runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CorvusIAM.csproj", "."]
RUN dotnet restore "CorvusIAM.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "CorvusIAM.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "CorvusIAM.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CorvusIAM.dll"]
