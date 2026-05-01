using Npgsql;

var connStr = "Host=localhost;Port=5432;Database=LABWEB7;Username=usubanco;Password=ususenha;SSL Mode=require;Trust Server Certificate=true;";
using var conn = new NpgsqlConnection(connStr);
conn.Open();

using var cmd = new NpgsqlCommand(@"
    SELECT 
        NOW()                                   AS utc_now,
        NOW() AT TIME ZONE 'America/Sao_Paulo'  AS brasilia_now,
        current_setting('TimeZone')             AS tz_banco
", conn);

using var reader = cmd.ExecuteReader();
reader.Read();
Console.WriteLine("UTC NOW          : " + reader["utc_now"]);
Console.WriteLine("Brasilia NOW     : " + reader["brasilia_now"]);
Console.WriteLine("TimeZone do banco: " + reader["tz_banco"]);
