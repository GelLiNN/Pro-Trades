# Pro-Trades

Microsoft ASP.NET CORE C# implementation of Pro-Trades Investment Guidance.
Here is some text

```bash
# docker psql setup
docker-compose up
docker exec -it pro-trades-postgres-1 /bin/bash
psql -h localhost -p 5432 -d ptdb -U root
CREATE USER postgres WITH PASSWORD 'root';
ALTER ROLE postgres WITH SUPERUSER;
\quit
exit

# dotnet migration
sudo su
apt update && apt upgrade dotnet-sdk-7.0
dotnet tool install --global dotnet-ef --version 7.0.7
echo "export PATH=\"$PATH:/home/root/.dotnet/tools\"" >> ~/.bash_profile
source ~/.bash_profile
dotnet ef database update
dotnet clean && dotnet build && dotnet run
exit
```
