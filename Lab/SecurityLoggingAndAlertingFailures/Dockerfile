FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet
WORKDIR /app
COPY ./Shared ./Shared
COPY ./SecurityLoggingAndAlertingFailures/src ./SecurityLoggingAndAlertingFailures/src
EXPOSE 5000
EXPOSE 5001 
CMD ["dotnet", "run", "--project", "SecurityLoggingAndAlertingFailures/src/Backend/Backend.csproj", "--no-launch-profile"]
