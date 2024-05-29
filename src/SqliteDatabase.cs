using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Images;
using System.Data.SQLite;
using System.Web.Script.Serialization;

namespace images
{
    internal class SqliteDatabase : IDatabase
    {
        SQLiteConnection connection;

        internal SQLiteDataReader query(string query)
        {
            var cmd = new SQLiteCommand(query, connection);
            return cmd.ExecuteReader();
        }

        internal void run(string query)
        {
            var cmd = new SQLiteCommand(query, connection);
            cmd.ExecuteNonQuery();
        }

        internal string toJson<T>(T obj)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Serialize(obj);
        }
        internal T fromJson<T>(string json)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Deserialize<T>(json);
        }
        internal string toJson(System.Drawing.Rectangle obj)
        {
            return $"{{\"X\":{obj.X},\"Y\":{obj.Y},\"Width\":{obj.Width},\"Height\":{obj.Height}}}";
        }

        public override void SaveObjects(string fileName, IEnumerable<INeuroProcess.NnRes> objects)
        {
            var current = query($"SELECT id FROM files WHERE name = '{fileName}'");
            if (!current.HasRows)
            {
                current = query($"INSERT INTO files (name) VALUES ('{fileName}'); select last_insert_rowid();");
            }
            if (!current.Read())
                return;
            int file_id = current.GetInt32(0);

            // вставляем объекты в бд
            foreach (var obj in objects)
            {
                run($"INSERT INTO objects (file_id, name, value, rectangle) VALUES ('{file_id}', '{obj.label}', '{obj.value}', '{toJson(obj.rect)}');");
            }

        }

        public override string Report()
        {
            // Выдадим всё в json пожалуй 
            var files = new List<string>();
            var rec = query($"SELECT * FROM files");
            var id_id = rec.GetOrdinal("id");
            var name_id = rec.GetOrdinal("name");
            while (rec.Read())
            {
                var file = new List<string>();
                file.Add($"\"file\":\"{rec.GetString(name_id)}\"");

                var objects = new List<string>();
                var objects_rec = query($"SELECT id, name, value, rectangle FROM objects WHERE file_id= {rec.GetInt32(id_id)}");
                while (objects_rec.Read())
                {
                    // •	Координаты центра дефекта по вертикали и горизонтали - координаты дефектов в формате JSON
                    var rect = fromJson<System.Drawing.Rectangle>(objects_rec.GetString(3));
                    objects.Add($"{{\"x\":{rect.X + rect.Width / 2},\"y\":{rect.Y + rect.Height / 2}}}");
                }
                file.Add($"\"count\":\"{objects.Count}\"");
                file.Add($"\"positions\":[{string.Join(",", objects)}]");
                files.Add(string.Join(",", file));
            }
            return $"{{\"Report\":[{{{string.Join("},\r\n{", files)}}}]}}";
        }

        public SqliteDatabase(string dataSource)
        {
            connection = new SQLiteConnection($"Data Source={dataSource}");
            connection.Open();

            // проверим структуру, если её нет то создадим 
            if (!query("SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'files';").HasRows)
            {
                run(
                @"
                    CREATE TABLE files (
                        id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL
                    ); 

                    CREATE TABLE objects (
                        id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        file_id INTEGER NOT NULL REFERENCES files(id) ON DELETE CASCADE,
                        name TEXT NOT NULL, 
                        value FLOAT NOT NULL,
                        rectangle TEXT NOT NULL 
                    ); 
                ");

            }
        }


    }
}
