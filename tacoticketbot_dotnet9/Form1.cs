using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Threading;

namespace tacoticketbot_dotnet9
{
    public partial class Form1 : Form
    {
        private DiscordSocketClient _client = new DiscordSocketClient();
        private BotSettings _settings = new BotSettings();
        private readonly SynchronizationContext _uiContext;

        public Form1()
        {
            InitializeComponent();
            _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            _settings = AppSettings.Load();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            var line = msg.ToString() + Environment.NewLine;
            if (_uiContext != null)
            {
                _uiContext.Post(_ => richTextBox1.AppendText(line), null);
            }
            else
            {
                richTextBox1.AppendText(line);
            }
            return Task.CompletedTask;
        }
        private void Log(string msg)
        {
            Console.WriteLine(msg);
            var line = msg + Environment.NewLine;
            if (_uiContext != null)
            {
                _uiContext.Post(_ => richTextBox1.AppendText(line), null);
            }
            else
            {
                richTextBox1.AppendText(line);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            //start button pressed
            button1.Enabled = false;

            richTextBox1.Clear();
            Log("Starting bot...");
            
            _client.Log += Log;
            _client.Ready += Client_Ready;

            //  You can assign your bot token to a string, and pass that in to connect.
            //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
            var token = _settings.Token;

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            // var token = File.ReadAllText("token.txt");
            // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            //await Task.Delay(-1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //pause button pressed
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //stop button pressed
            _client.LogoutAsync();
            button1.Enabled = true;
        }

        public async Task Client_Ready()
        {
            // Let's build a guild command! We're going to need a guild so lets just put that in a variable.
            var guild = _client.GetGuild(_settings.guildId);

            // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
            var guildCommand = new SlashCommandBuilder();

            // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
            guildCommand.WithName("first-command");

            // Descriptions can have a max length of 100.
            guildCommand.WithDescription("This is my first guild slash command!");

            // Let's do our global command
            var globalCommand = new SlashCommandBuilder();
            globalCommand.WithName("first-global-command");
            globalCommand.WithDescription("This is my first global slash command");

            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                await guild.CreateApplicationCommandAsync(guildCommand.Build());

                // With global commands we don't need the guild.
                await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
                // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
                // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
            }
            catch (HttpException exception)
            {
                // If our command was invalid, we should catch an HttpException. This exception contains the Discord error code, the request object that was sent, the reason of the exception and a list of of errors to explain what went wrong with the request. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
        }

    }
}
