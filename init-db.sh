docker exec -i sqlserver \
    /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U sa \
    -P '12345aA!' \
    -C \
    -l 5 \
    -b \
    <<'SQL'
IF DB_ID(N'AuthenticationFailures_LAB') IS NULL CREATE DATABASE AuthenticationFailures_LAB;
GO
IF DB_ID(N'BrokenAccessControl_LAB') IS NULL CREATE DATABASE BrokenAccessControl_LAB;
GO
IF DB_ID(N'CryptographicFailures_LAB') IS NULL CREATE DATABASE CryptographicFailures_LAB;
GO
IF DB_ID(N'Injection_LAB') IS NULL CREATE DATABASE Injection_LAB;
GO
IF DB_ID(N'InsecureDesign_LAB') IS NULL CREATE DATABASE InsecureDesign_LAB;
GO
IF DB_ID(N'MishandlingOfExceptionalConditions_LAB') IS NULL CREATE DATABASE MishandlingOfExceptionalConditions_LAB;
GO
IF DB_ID(N'SecurityLoggingAndAlertingFailures_LAB') IS NULL CREATE DATABASE SecurityLoggingAndAlertingFailures_LAB;
GO
IF DB_ID(N'SecurityMisconfiguration_LAB') IS NULL CREATE DATABASE SecurityMisconfiguration_LAB;
GO
IF DB_ID(N'SoftwareOrDataIntegrityFailures_LAB') IS NULL CREATE DATABASE SoftwareOrDataIntegrityFailures_LAB;
GO
IF DB_ID(N'SoftwareSupplyChainFailures_LAB') IS NULL CREATE DATABASE SoftwareSupplyChainFailures_LAB;
GO
SQL

status=$?

if (( status != 0 )); then
    echo "Error occurred: exit code $status"
else
    echo "Database created successfully."
fi
