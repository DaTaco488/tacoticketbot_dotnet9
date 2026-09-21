using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.VisualBasic.ApplicationServices;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Threading;

namespace tacoticketbot_dotnet9
{
    public partial class Form1 : Form
    {
        private DiscordSocketClient _client = new DiscordSocketClient();
        private BotSettings _settings = new BotSettings();
        private readonly SynchronizationContext _uiContext;
        private bool callbacksAssigned = false;
        private bool loggyinny = false;
        private bool isPaused = false;

        private Dictionary<ulong, SocketSlashCommand> openMessages = new Dictionary<ulong, SocketSlashCommand>();

        public Form1()
        {
            InitializeComponent();
            this.Icon = new System.Drawing.Icon(@"C:\Users\DaTaco\Downloads\New Piskel-1.png.ico");
            _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            _settings = AppSettings.Load();
        }

        private Task Log(LogMessage msg)
        {
            if (!loggyinny) return Task.CompletedTask;
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

            if (callbacksAssigned == false)
            {
                callbacksAssigned = true;
                _client.Log += Log;
                _client.Ready += Client_Ready;
                _client.SlashCommandExecuted += SlashCommandHandler;
                _client.SelectMenuExecuted += SelectMenuHandler;
                _client.ButtonExecuted += ButtonHandler;
            }

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
            if (isPaused)
            {
                isPaused = false;
                button2.Text = "PAUSE";
                Log("Bot resumed.");
            }
            else
            {
                isPaused = true;
                button2.Text = "RESUME";
                Log("Bot paused.");
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            //stop button pressed
            _client.LogoutAsync();
            button1.Enabled = true;
            loggyinny = false;
        }

        public async Task Client_Ready()
        {
            loggyinny = true;
            // Let's build a guild command! We're going to need a guild so lets just put that in a variable.
            var guild = _client.GetGuild(_settings.guildId);

            // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
            var ticketCreate = new SlashCommandBuilder();

            // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
            ticketCreate.WithName("ticket");

            // Descriptions can have a max length of 100.
            ticketCreate.WithDescription("Create a new support ticket");

            // Remove ticket command
            var removeTicket = new SlashCommandBuilder();
            removeTicket.WithName("remove_ticket");
            removeTicket.WithDescription("Remove a support ticket");

            // Database List Command
            var databaseList = new SlashCommandBuilder();
            databaseList.WithName("database_list");
            databaseList.WithDescription("List the current tickets and their properties");
            databaseList.WithDefaultMemberPermissions(GuildPermission.Administrator); // Only allow admins to use this command

            try
            {
                //register slash commands to the discord server
                await guild.CreateApplicationCommandAsync(ticketCreate.Build());

                await guild.CreateApplicationCommandAsync(removeTicket.Build());

                await guild.CreateApplicationCommandAsync(databaseList.Build());
            }
            catch (HttpException exception)
            {
                // If our command was invalid, we should catch an HttpException. This exception contains the Discord error code, the request object that was sent, the reason of the exception and a list of of errors to explain what went wrong with the request. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (isPaused)
            {
                await command.RespondAsync("The bot is currently paused. Please try again later.", ephemeral: true);
                return;
            }
            // Let's add a switch statement for the command name so we can handle multiple commands in one event.
            switch (command.Data.Name)
            {
                case "ticket":
                    await HandleTicketCommand(command);
                    break;
                //case "remove_ticket":
                //    await HandleRemoveTicketCommand(command);
                //    break;
                case "database_list":
                    await HandleDatabaseListCommand(command);
                    break;
            }
        }

        private async Task HandleDatabaseListCommand(SocketSlashCommand command)
        {
            // 1. Cast the user to a SocketGuildUser to get server-specific data
            if (command.User is SocketGuildUser guildUser)
            {
                // 2. Check for a specific permission (e.g., Administrator)
                if (guildUser.GuildPermissions.Administrator)
                {
                    string response = "";
                    if (_settings.createdTickets.Count == 0)
                    {
                        response = "No tickets have been created yet.";
                    }
                    else
                    {
                        foreach (var ticket in _settings.createdTickets)
                        {
                            response += $"User Name: {ticket.userName}, User ID: {ticket.userId}, Channel Name: {ticket.channelName}, Category: {ticket.category} Ticket ID: {ticket.ticketId},  Channel ID: {ticket.channelId}\n";
                        }
                    }
                    await command.RespondAsync(response, ephemeral: true);
                }
                else
                {
                    await command.RespondAsync("You dont have permission to run this command.");
                }
            }
            else
            {
                await command.RespondAsync("You dont have permission to run this command.");
            }
        }

        private async Task HandleTicketCommand(SocketSlashCommand command)
        {
            // 1. Build the Select Menu with your ticket categories
            var menuBuilder = new SelectMenuBuilder()
                .WithCustomId("ticket_category_select")
                .WithPlaceholder("Select a category")
                .AddOption("Support", "category_support", "Open Support ticket when needed support!")
                .AddOption("Buy Spawners", "category_spawners", "Open Buy Spawners ticket when buying any spawners!")
                .AddOption("Sell Spawners", "category_sell_spawners", "Open Sell Spawners ticket when selling any spawners!")
                .AddOption("Giveaway Claim", "category_giveaway", "Open Giveaway Claim ticket when claiming a giveaway!")
                .AddOption("Building Service", "category_building", "Open Building Service ticket when trying to buy a farm or build!")
                .AddOption("Middleman Service", "category_middleman", "Open Middleman Service ticket when trading with a user!");

            // 2. Build the "Create Ticket" Button (Initially Disabled & Grey/Secondary until a choice is made)
            var buttonBuilder = new ButtonBuilder()
                .WithCustomId("create_ticket_btn")
                .WithLabel("Create Ticket")
                .WithStyle(ButtonStyle.Secondary)
                .WithDisabled(true); // Starts disabled until selection happens

            // 3. Combine them into a ComponentBuilder
            var componentBuilder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder)
                .WithButton(buttonBuilder);

            // 4. Send the message containing the embed/text and components
            await command.RespondAsync(
                "**Open a Ticket!**\n" +
                "• Open Support ticket when needed support!\n" +
                "• Open Buy Spawners ticket when buying any spawners!\n" +
                "• Open Sell Spawners ticket when selling any spawners!\n" +
                "• Open Giveaway Claim ticket when claiming a giveaway but be ready with proof and check the giveaway time before claiming!\n" +
                "• Open Building Service ticket when trying to buy a farm or a build!\n" +
                "• Open Middleman Service ticket when trying to trade something with a user but your not sure of trusting them!",
                components: componentBuilder.Build(),
                ephemeral: true
            );

            // 5. Store the command for later reference if needed (e.g., to update the message later)
            openMessages[command.User.Id] = command;
        }

        private async Task SelectMenuHandler(SocketMessageComponent command)
        {
            // Let's add a switch statement for the command name so we can handle multiple commands in one event.
            switch (command.Data.CustomId)
            {
                case "ticket_category_select":
                    await HandleSelectMenuAsync(command);
                    break;
                    //case "remove_ticket":
                    //    await HandleRemoveTicketCommand(command);
                    //    break;
            }
        }

        private async Task HandleSelectMenuAsync(SocketMessageComponent command)
        {
            var selectedCategory = command.Data.Values.FirstOrDefault();

            // Re-enable the components, but turn the button Green (Success) and store the selected category 
            // (You can store the chosen category temporarily using user-specific state or prefixing the button's custom ID, e.g., `create_ticket_{selectedCategory}`)

            string msg = $"Selected: {selectedCategory.Replace("category_", "").Replace("_", " ")}";
            if (msg.Length > 25)
            {
                msg = msg.Substring(0, 25);
            }
            var updatedMenu = new SelectMenuBuilder()
                .WithCustomId("ticket_category_select")
                .WithPlaceholder(msg)
                // Re-add options here...
                .AddOption("Support", "category_support", "Open Support ticket when needed support!")
                .AddOption("Buy Spawners", "category_spawners", "Open Buy Spawners ticket when buying any spawners!")
                .AddOption("Sell Spawners", "category_sell_spawners", "Open Sell Spawners ticket when selling any spawners!")
                .AddOption("Giveaway Claim", "category_giveaway", "Open Giveaway Claim ticket when claiming a giveaway!")
                .AddOption("Building Service", "category_building", "Open Building Service ticket when trying to buy a farm or build!")
                .AddOption("Middleman Service", "category_middleman", "Open Middleman Service ticket when trading with a user!");
            ;

            var updatedButton = new ButtonBuilder()
                .WithCustomId($"create_ticket_{selectedCategory}") // Passes the chosen category to the button action
                .WithLabel("Create Ticket")
                .WithStyle(ButtonStyle.Success) // Lights up Green!
                .WithDisabled(false);

            var components = new ComponentBuilder()
                .WithSelectMenu(updatedMenu)
                .WithButton(updatedButton)
                .Build();

            // Update the existing message with the new green button state
            SocketSlashCommand originalCommand = openMessages[command.User.Id];
            if (originalCommand != null)
            {
                await originalCommand.ModifyOriginalResponseAsync(msg =>
                {
                    msg.Components = components;
                });
                await command.DeferAsync();
            }
            else
            {
                await command.RespondAsync("", components: components, ephemeral: true);
            }
        }

        private async Task ButtonHandler(SocketMessageComponent command)
        {
            if (command.Data.CustomId.StartsWith("create_ticket_"))
            {
                var selectedCategory = command.Data.CustomId.Replace("create_ticket_", "");
                // Handle ticket creation logic here based on the selected category
                var guild = _client.GetGuild(_settings.guildId);
                // 1. Define permission overwrites
                var permissions = new List<Overwrite>
                {
                    // Deny the @everyone role from viewing the channel
                    new Overwrite(
                        guild.EveryoneRole.Id,
                        PermissionTarget.Role,
                        new OverwritePermissions(viewChannel: PermValue.Deny)
                    ),
                    // Allow the target user to view the channel
                    new Overwrite(
                        command.User.Id,
                        PermissionTarget.User,
                        new OverwritePermissions(viewChannel: PermValue.Allow)
                    )
                };

                // 2. Create the channel with the defined permissions \u2800
                ulong ticketNum = _settings.ticketNum++;
                string channelName = $"ticket{ticketNum}⋅{command.User.Username}s⋅{selectedCategory.Replace("category_", "").Replace("_", "⋅")}⋅ticket";
                var newChannel = await guild.CreateTextChannelAsync(channelName, tcp =>
                {
                    tcp.CategoryId = _settings.catId;
                    tcp.PermissionOverwrites = permissions;
                });

                string category = selectedCategory.Replace("category_", "").Replace("_", " ");

                // Remember to add the ticket to the list of created tickets
                _settings.createdTickets.Add(new CreatedTicket
                {
                    userId = command.User.Id,
                    userName = command.User.Username,
                    channelId = newChannel.Id,
                    channelName = newChannel.Name,
                    category = category,
                    ticketId = ticketNum
                });
                AppSettings.Save(_settings);

                
                await newChannel.SendMessageAsync($"Hello {command.User.Mention}, this is your ticket for **{category}**. Please describe your issue or request, and a staff member will assist you shortly.\nUse /remove_ticket {ticketNum} to close/delete this ticket.");

                // Update the existing message with the new green button state
                SocketSlashCommand originalCommand = openMessages[command.User.Id];
                if (originalCommand != null)
                {
                    await originalCommand.ModifyOriginalResponseAsync(msg =>
                    {
                        msg.Content = $"Ticket created for category: {category}";
                        msg.Components = new ComponentBuilder().Build(); // Remove components after ticket creation
                    });
                    await command.DeferAsync();
                    openMessages.Remove(command.User.Id); // Remove it from the dictionary after updating
                }
                else
                {
                    await command.RespondAsync($"Ticket created for category: {category}", ephemeral: true);
                }
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (loggyinny == true)
            {
                loggyinny = false;
                _client.LogoutAsync();
            }
        }
    }
}
