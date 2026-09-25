using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tacoticketbot_dotnet9
{
    public interface ILogger
    {
        void Log(string message);
    }

    public class TacoTicketBot
    {
        private DiscordSocketClient _client = new DiscordSocketClient();
        private BotSettings _settings = new BotSettings();
        private bool callbacksAssigned = false;
        private bool loggyinny = false;
        private bool isPaused = false;
        private ILogger _logger;

        private Dictionary<ulong, SocketSlashCommand> openMessages = new Dictionary<ulong, SocketSlashCommand>();

        public TacoTicketBot(ILogger logger)
        {
            _logger = logger;
            _settings = AppSettings.Load();
        }

        private Task Log(LogMessage msg)
        {
            if (!loggyinny) return Task.CompletedTask;
            _logger.Log(msg.ToString());
            return Task.CompletedTask;
        }
        private void Log(string msg)
        {
            _logger.Log(msg);
        }

        public async void start()
        {

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

        public void pause()
        {
            //pause button pressed
            if (!isPaused)
            {
                isPaused = true;
                Log("Bot paused.");
            }

        }

        public void unpause()
        {
            if (isPaused)
            {
                isPaused = false;
                Log("Bot resumed.");
            }
        }

        public bool isBotPaused()
        {
            return isPaused;
        }

        public void stop()
        {
            //stop button pressed
            _client.LogoutAsync();
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
            removeTicket.AddOption("ticket_number", ApplicationCommandOptionType.Integer, "The ticket number to remove", isRequired: false);

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
                case "remove_ticket":
                    await HandleRemoveTicketCommand(command);
                    break;
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
                    if (response.Length < 2000)
                    {
                        await command.RespondAsync(response, ephemeral: true);
                    }
                    else
                    {
                        await command.RespondAsync("The ticket list is too long to display here, so I've sent it to you in DMs.", ephemeral: true);
                        string[] text = response.Split('\n');
                        string msg = "";
                        foreach (string line in text)
                        {
                            msg += line + "\n";
                            if (msg.Length > 1000)
                            {
                                await command.User.SendMessageAsync(msg);
                                msg = "";
                            }

                        }

                    }
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
                .AddOption("Buy Spawners", "category_buy_spawners", "Open Buy Spawners ticket when buying any spawners!")
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

        private async Task HandleRemoveTicketCommand(SocketSlashCommand command)
        {
            // Check if the user provided a ticket number
            if (command.Data.Options.Count == 0 || command.Data.Options.First().Type != ApplicationCommandOptionType.Integer)
            {
                //await command.RespondAsync("Please provide a valid ticket number.", ephemeral: true);
                SendRemoveTicketComboBox(command);
                return;
            }
            Int64 ticketNumber = (Int64)command.Data.Options.First().Value;
            // Find the ticket in the created tickets list
            CreatedTicket? ticketToRemove = _settings.createdTickets.FirstOrDefault(t => t.ticketId == ticketNumber);
            if (ticketToRemove == null)
            {
                await command.RespondAsync($"No ticket found with number {ticketNumber}.", ephemeral: true);
                return;
            }
            // Get the guild and channel
            var guild = _client.GetGuild(_settings.guildId);
            if (command.User.Id != ticketToRemove.userId && !((SocketGuildUser)command.User).GuildPermissions.Administrator)
            {
                await command.RespondAsync("You do not have permission to remove this ticket.", ephemeral: true);
                return;
            }

            await RemoveTicket(ticketNumber, guild, command.User.GlobalName);
            await command.RespondAsync($"Ticket number {ticketNumber} has been removed.", ephemeral: true);
        }

        private async Task RemoveTicket(long ticketNumber, SocketGuild guild, string closedBy)
        {
            CreatedTicket? ticketToRemove = _settings.createdTickets.FirstOrDefault(t => t.ticketId == ticketNumber);
            if (ticketToRemove == null)
            {
                Log($"No ticket found with number {ticketNumber}.");
                return;
            }
            var channel = guild.GetTextChannel(ticketToRemove.channelId);
            if (channel != null)
            {
                // Delete the channel
                await channel.DeleteAsync();
                Log($"Deleted channel: {ticketToRemove.channelName} for user: {ticketToRemove.userName}");
            }
            else
            {
                Log($"Channel not found for ticket number {ticketNumber}.");
            }
            // Remove the ticket from the list and save settings
            _settings.createdTickets.Remove(ticketToRemove);
            AppSettings.Save(_settings);
            await HandleTicketTranscripts(ticketToRemove, closedBy);
        }

        private async void SendRemoveTicketComboBox(SocketSlashCommand command)
        {
            // 1. Build the Select Menu with your ticket categories
            var menuBuilder = new SelectMenuBuilder()
                .WithCustomId("ticket_remove_select")
                .WithPlaceholder("Select a ticket to close/remove");
            ulong userId = command.User.Id;
            bool foundTickets = false;
            if (command.User is SocketGuildUser guildUser && guildUser.GuildPermissions.Administrator)
            {
                // Admins can see all tickets
                foreach (var ticket in _settings.createdTickets)
                {
                    menuBuilder.AddOption($"Ticket {ticket.ticketId} - {ticket.userName} - {ticket.category}", $"remove_ticket_{ticket.ticketId}");
                    foundTickets = true;
                }

            }
            else
            {
                // Regular users can only see their own tickets
                foreach (var ticket in _settings.createdTickets.Where(t => t.userId == userId))
                {
                    menuBuilder.AddOption($"Ticket {ticket.ticketId} - {ticket.category}", $"remove_ticket_{ticket.ticketId}");
                    foundTickets = true;
                }
            }
            if (!foundTickets)
            {
                await command.RespondAsync("You have no tickets to remove.", ephemeral: true);
                return;
            }

            // 2. Build the "Create Ticket" Button (Initially Disabled & Grey/Secondary until a choice is made)
            var buttonBuilder = new ButtonBuilder()
                .WithCustomId("remove_ticket_btn")
                .WithLabel("Remove Ticket")
                .WithStyle(ButtonStyle.Secondary)
                .WithDisabled(true); // Starts disabled until selection happens

            // 3. Combine them into a ComponentBuilder
            var componentBuilder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder)
                .WithButton(buttonBuilder);

            // 4. Send the message containing the embed/text and components
            await command.RespondAsync(
                "**Choose a ticket to remove **\n",
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
                    await HandleCreateSelectMenuAsync(command);
                    break;
                case "ticket_remove_select":
                    await HandleRemoveSelectMenuAsync(command);
                    break;
            }
        }

        private async Task HandleCreateSelectMenuAsync(SocketMessageComponent command)
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
                .AddOption("Buy Spawners", "category_buy_spawners", "Open Buy Spawners ticket when buying any spawners!")
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

        private async Task HandleRemoveSelectMenuAsync(SocketMessageComponent command)
        {
            var selectedCategory = command.Data.Values.FirstOrDefault();


            // Re-enable the components, but turn the button Green (Success) and store the selected category 
            // (You can store the chosen category temporarily using user-specific state or prefixing the button's custom ID, e.g., `create_ticket_{selectedCategory}`)

            string msg = $"Selected: Ticket #{selectedCategory.Replace("remove_ticket_", "")}";
            // 1. Build the Select Menu with your ticket categories
            var menuBuilder = new SelectMenuBuilder()
                .WithCustomId("ticket_remove_select")
                .WithPlaceholder(msg);
            ulong userId = command.User.Id;
            if (command.User is SocketGuildUser guildUser && guildUser.GuildPermissions.Administrator)
            {
                // Admins can see all tickets
                foreach (var ticket in _settings.createdTickets)
                {
                    menuBuilder.AddOption($"Ticket {ticket.ticketId} - {ticket.userName} - {ticket.category}", $"remove_ticket_{ticket.ticketId}");
                }

            }
            else
            {
                // Regular users can only see their own tickets
                foreach (var ticket in _settings.createdTickets.Where(t => t.userId == userId))
                {
                    menuBuilder.AddOption($"Ticket {ticket.ticketId} - {ticket.category}", $"remove_ticket_{ticket.ticketId}");
                }
            }
            // 2. Update the "Remove Ticket" Button to be Red (Danger) and enabled
            var buttonBuilder = new ButtonBuilder()
                .WithCustomId($"remove_ticket_{selectedCategory}") // Passes the chosen category to the button action
                .WithLabel("Remove Ticket")
                .WithStyle(ButtonStyle.Danger)
                .WithDisabled(false); // Enabled once a selection is made

            // 3. Combine them into a ComponentBuilder
            var componentBuilder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder)
                .WithButton(buttonBuilder);

            // Update the existing message with the new green button state
            SocketSlashCommand originalCommand = openMessages[command.User.Id];
            if (originalCommand != null)
            {
                await originalCommand.ModifyOriginalResponseAsync(msg =>
                {
                    msg.Components = componentBuilder.Build();
                });
                await command.DeferAsync();
            }
            else
            {
                await command.RespondAsync("", components: componentBuilder.Build(), ephemeral: true);
            }
        }

        private async Task ButtonHandler(SocketMessageComponent command)
        {
            switch (command.Data.CustomId)
            {
                case var id when id.StartsWith("create_ticket_"):
                    await HandleCreateTicketButtonAsync(command);
                    break;
                case var id when id.StartsWith("remove_ticket_"):
                    await HandleRemoveTicketButtonAsync(command);
                    break;
            }
        }
        private async Task HandleCreateTicketButtonAsync(SocketMessageComponent command)
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
                Int64 ticketNum = _settings.ticketNum++;
                string channelName = $"ticket{ticketNum}⋅{command.User.GlobalName}s⋅{selectedCategory.Replace("category_", "").Replace("_", "⋅")}⋅ticket";
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
                    userName = command.User.GlobalName,
                    channelId = newChannel.Id,
                    channelName = newChannel.Name,
                    category = category,
                    ticketId = ticketNum,
                    createdAt = DateTime.Now
                });
                AppSettings.Save(_settings);


                //await newChannel.SendMessageAsync($"Hello {command.User.Mention}, this is your ticket for **{category}**. Please describe your issue or request, and a staff member will assist you shortly.\nUse /remove_ticket {ticketNum} to close/delete this ticket.");

                //// Respond to the user in the new channel with a message in a embed
                var embed = new EmbedBuilder()
                    .WithTitle($"Ticket #{ticketNum} - {category}")
                    .WithDescription($"Hello {command.User.Mention}, Please describe your issue or request, and a staff member will assist you shortly.\nUse /remove_ticket {ticketNum} to close/delete this ticket.")
                    .WithColor(Discord.Color.Green)
                    .AddField("🎟️ Ticket ID:", ticketNum.ToString(), inline: true)
                    .AddField("🟢 Opened By:", command.User.GlobalName, inline: true)
                    .AddField("📂 Category:", category, inline: true)
                    .WithFooter("Powered By: Taco Ticket Bot, Created By: DaTaco and TroZ")
                    .Build();

                await newChannel.SendMessageAsync(embed: embed);


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

        public static string GetClockEmoji(DateTime dateTime)
        {
            // Convert the time into total minutes past the hour (0 to 719 for a 12-hour cycle)
            int hour = dateTime.Hour % 12;
            int minute = dateTime.Minute;
            int totalMinutes = (hour * 60) + minute;

            // Round to the nearest 30-minute interval (each interval is 30 minutes)
            // 720 minutes total / 30 minutes = 24 possible intervals
            int interval = (int)Math.Round(totalMinutes / 30.0) % 24;

            // Array matching the 24 intervals starting from 12:00 AM/PM
            string[] clockEmojis = new string[]
            {
            "🕛", // 12:00
            "🕧", // 12:30
            "🕐", // 1:00
            "🕜", // 1:30
            "🕑", // 2:00
            "🕝", // 2:30
            "🕒", // 3:00
            "🕞", // 3:30
            "🕓", // 4:00
            "🕟", // 4:30
            "🕔", // 5:00
            "🕠", // 5:30
            "🕕", // 6:00
            "🕡", // 6:30
            "🕖", // 7:00
            "🕧", // 7:30
            "🕗", // 8:00
            "🕣", // 8:30
            "🕘", // 9:00
            "🕤", // 9:30
            "🕙", // 10:00
            "🕥", // 10:30
            "🕚", // 11:00
            "🕦"  // 11:30
            };

            return clockEmojis[interval];
        }

        private async Task HandleTicketTranscripts(CreatedTicket ticket, string closedBy)
        {
            ulong transcriptChannelId = _settings.transcriptChannelID;
            DateTime closedAt = DateTime.Now;
            long createdAtUnix = ((DateTimeOffset)ticket.createdAt).ToUnixTimeSeconds();
            long closedAtUnix = ((DateTimeOffset)closedAt).ToUnixTimeSeconds();
            var embed = new EmbedBuilder()
                .WithTitle($"Ticket Transcript - Ticket #{ticket.ticketId}")
                .WithDescription("All times are in your local time zone.")
                .AddField("🎟️ Ticket ID:", ticket.ticketId.ToString(), inline: true)
                .AddField("📂 Category:", ticket.category, inline: true)
                .AddField("\u200b", "\u200b", inline: true) // The invisible breaker
                .AddField("🟢 Opened By:", ticket.userName, inline: true)
                .AddField("🟥 Closed By:", closedBy, inline: true)
                .AddField("\u200b", "\u200b", inline: true) // The invisible breaker
                .AddField(GetClockEmoji(ticket.createdAt) + " Created At:", $"<t:{createdAtUnix}:d> <t:{createdAtUnix}:T>", inline: true)
                .AddField(GetClockEmoji(closedAt) + " Closed At:", $"<t:{closedAtUnix}:d> <t:{closedAtUnix}:T>", inline: true)
                .AddField("\u200b", "\u200b", inline: true) // The invisible breaker
                .WithColor(Discord.Color.Gold)
                .WithFooter("Powered By: Taco Ticket Bot, Created By: DaTaco and TroZ")
                .Build();
            var channel = _client.GetChannel(transcriptChannelId) as IMessageChannel;
            if (channel == null)
            {
                Log($"Transcript channel with ID {transcriptChannelId} not found.");
                return;
            }
            await channel.SendMessageAsync(embed: embed);
        }

        private async Task HandleRemoveTicketButtonAsync(SocketMessageComponent command)
        {
            if (command.Data.CustomId.StartsWith("remove_ticket_"))
            {
                var selectedCategory = command.Data.CustomId.Replace("remove_ticket_", "");
                // Handle ticket deletion logic here based on the selected category
                var guild = _client.GetGuild(_settings.guildId);
                Int64 ticketNumber = Int64.TryParse(selectedCategory, out var num) ? num : -1;
                if (ticketNumber <= 0)
                {
                    await command.RespondAsync("Invalid ticket number.", ephemeral: true);
                    return;
                }
                await RemoveTicket(ticketNumber, guild, command.User.GlobalName);
                SocketSlashCommand originalCommand = openMessages[command.User.Id];
                if (originalCommand != null)
                {
                    await originalCommand.ModifyOriginalResponseAsync(msg =>
                    {
                        msg.Content = $"Removed ticket: #{ticketNumber}";
                        msg.Components = new ComponentBuilder().Build(); // Remove components after ticket creation
                    });
                    await command.DeferAsync();
                    openMessages.Remove(command.User.Id); // Remove it from the dictionary after updating
                }
                else
                {
                    await command.RespondAsync($"Removed ticket: #{ticketNumber}", ephemeral: true);
                }
            }
        }
    }
}
