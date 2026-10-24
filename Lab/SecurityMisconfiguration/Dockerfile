FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet
WORKDIR /app
COPY ./src ./
EXPOSE 5000
EXPOSE 5001 
CMD ["dotnet", "run", "--project", "Backend/Backend.csproj", "--no-launch-profile"]
