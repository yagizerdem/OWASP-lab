docker exec sqlserver \
    /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U sa \
    -P '12345aA!' \
    -C \
    -l 5 \
    -b \
    -Q "CREATE DATABASE OWASP_LAB;"

status=$?

if (( status != 0 )); then
    echo "Error occurred: exit code $status"
else
    echo "Database created successfully."
fi
