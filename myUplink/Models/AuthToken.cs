using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace myUplink
{
    internal class AuthToken
    {
        public AuthToken()
        {
            created = DateTime.Now;
        }

        public DateTime created { get; set; }

        public string access_token { get; set; }

        public int expires_in { get; set; }

        public string token_type { get; set; }

        public string scope { get; set; }   


        [JsonIgnore]
        public bool IsExpired 
        { 
            get
            {
                if (created == DateTime.MinValue)
                    return true;

                var expires = created.AddSeconds(expires_in);
                if (expires < DateTime.Now)
                    return true;

                return false;
            }
        }
    }
}
