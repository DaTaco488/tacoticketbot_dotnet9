using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tacoticketbot_dotnet9
{
    public class BotSettings
    {
        public string Token { get; set; } = "";
        public ulong guildId { get; set; } = 0;
        public ulong catId { get; set; } = 0;
    }
}
