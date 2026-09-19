using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace tacoticketbot_dotnet9
{
    public class AppSettings 
    {
        public const string DEFAULT_FILENAME = "ttb_settings.json";

        /*
        public void Save(string fileName = DEFAULT_FILENAME)
        {
            //File.WriteAllText(fileName, FormatOutput(JsonSerializer.Serialize(this)));
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(fileName, JsonSerializer.Serialize(this, options));
        }
        */

        public static void Save(BotSettings pSettings, string fileName = DEFAULT_FILENAME)
        {
            //File.WriteAllText(fileName, FormatOutput(JsonSerializer.Serialize(pSettings)));
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(fileName, JsonSerializer.Serialize(pSettings, options));
        }

        public static BotSettings Load(string fileName = DEFAULT_FILENAME)
        {
            BotSettings t = new BotSettings();
            if (File.Exists(fileName))
            {
                string fileData = File.ReadAllText(fileName);
                t = JsonSerializer.Deserialize<BotSettings>(fileData);
            }
            return t;
        }

        public static string FormatOutput(string jsonString)
        {
            var stringBuilder = new StringBuilder();

            bool escaping = false;
            bool inQuotes = false;
            int indentation = 0;

            foreach (char character in jsonString)
            {
                if (escaping)
                {
                    escaping = false;
                    stringBuilder.Append(character);
                }
                else
                {
                    if (character == '\\')
                    {
                        escaping = true;
                        stringBuilder.Append(character);
                    }
                    else if (character == '\"')
                    {
                        inQuotes = !inQuotes;
                        stringBuilder.Append(character);
                    }
                    else if (!inQuotes)
                    {
                        if (character == ',')
                        {
                            stringBuilder.Append(character);
                            stringBuilder.Append("\r\n");
                            stringBuilder.Append('\t', indentation);
                        }
                        else if (character == '[' || character == '{')
                        {
                            stringBuilder.Append(character);
                            stringBuilder.Append("\r\n");
                            stringBuilder.Append('\t', ++indentation);
                        }
                        else if (character == ']' || character == '}')
                        {
                            stringBuilder.Append("\r\n");
                            stringBuilder.Append('\t', --indentation);
                            stringBuilder.Append(character);
                        }
                        else if (character == ':')
                        {
                            stringBuilder.Append(character);
                            stringBuilder.Append('\t');
                        }
                        else if (!Char.IsWhiteSpace(character))
                        {
                            stringBuilder.Append(character);
                        }
                    }
                    else
                    {
                        stringBuilder.Append(character);
                    }
                }
            }

            return stringBuilder.ToString();
        }
    }
}

