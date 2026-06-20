# Command
# docker build --tag backend .
# docker run backend

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the frontend project
# https://docs.docker.com/guides/angular/containerize/
FROM node:24-alpine3.24 AS frontend
WORKDIR /app
COPY "Frontend/package.json" "Frontend/package-lock.json*" ./
RUN --mount=type=cache,target=/root/.npm npm ci
COPY "Frontend" .
RUN npm run build 

# This stage is used to build the backend project
# https://aka.ms/customizecontainer
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Backend/Backend/Backend.csproj", "Backend/"]
RUN dotnet restore "./Backend/Backend.csproj"
WORKDIR "/src/Backend"
COPY --from=frontend "/app/dist/frontend/browser" "wwwroot"
COPY "Backend/Backend" .
RUN dotnet build "./Backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the backend project to be copied to the final stage
FROM backend AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Backend.dll"]