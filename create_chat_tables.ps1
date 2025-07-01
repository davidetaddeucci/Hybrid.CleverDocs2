# Create chat tables in PostgreSQL cleverdocs database
$env:PGPASSWORD = "MiaPassword123"
psql -h 192.168.1.4 -p 5433 -U admin -d cleverdocs -f create_chat_tables.sql
