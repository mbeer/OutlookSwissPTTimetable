using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FahrplanSearchCH
{

    public class ConnectionsRequest
    {
        private string from;
        private string to;
        private string[] via = Array.Empty<string>();
        private DateTime dateTime = DateTime.Now;
        private bool isArrivalTime = false;
        private uint limit;

        public ConnectionsRequest(string from, string to, string[] via = null, DateTime dateTime = default, bool isArrivalTime = false, ushort limit = 4)
        {
            this.from = from ?? throw new ArgumentNullException(nameof(from));
            this.to = to ?? throw new ArgumentNullException(nameof(to));
            if (via != null) this.via = via;
            if (dateTime != default) this.dateTime = dateTime;
            this.isArrivalTime = isArrivalTime;
            this.limit = limit;
        }

        private HttpWebRequest PrepareWebRequest()
        {
            List<string> parameters = new List<string>
            {
                "from=" + Uri.EscapeUriString(from.Trim()),
                "to=" + Uri.EscapeUriString(to.Trim()),
                "date=" + dateTime.ToString("dd.MM.yyyy"),
                "time=" + dateTime.ToString("HH:mm")
            };
            if (isArrivalTime)
            {
                parameters.Add("time_type=arrival");
                parameters.Add("num=1");
                parameters.Add("pre=" + limit.ToString());
            } else
            {
                parameters.Add("time_type=depart");
                parameters.Add("num=" + limit.ToString());
                parameters.Add("pre=1");
            }
            foreach (string v in via)
            {
                parameters.Add("via[]=" + Uri.EscapeUriString(v.Trim()));
            }

            string Webstring = @"https://fahrplan.search.ch/api/route.json?" + String.Join("&", parameters);
            
            Uri Webaddress = new Uri(Webstring);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpWebRequest request = WebRequest.Create(Webaddress) as HttpWebRequest;
            request.Method = "GET";
            request.UserAgent = "Swiss public transport timetable add-in for Microsoft Outlook";
            return request;
        }

        public ConnectionsResponse GetConnections()
        {
            HttpWebRequest request = PrepareWebRequest();

            string JSON = string.Empty;
            HttpWebResponse response;
            using (response = request.GetResponse() as HttpWebResponse)
            {
                StreamReader reader = new StreamReader(response.GetResponseStream());
                JSON = reader.ReadToEnd();
            }
            return JsonConvert.DeserializeObject<ConnectionsResponse>(JSON, Converter.Settings);
        }

        public async Task<ConnectionsResponse> GetConnectionsAsync()
        {
            HttpWebRequest request = PrepareWebRequest();

            string JSON = string.Empty;
            HttpWebResponse response;
            using (response = await request.GetResponseAsync() as HttpWebResponse)
            {
                StreamReader reader = new StreamReader(response.GetResponseStream());
                JSON = reader.ReadToEnd();
            }
            return JsonConvert.DeserializeObject<ConnectionsResponse>(JSON, Converter.Settings);
        }
    }


    public class ConnectionsResponse
    {

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("connections")]
        public Connection[] Connections { get; set; }

    }

    public class Connection
    {
        private TimeSpan _duration;

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("departure")]
        public DateTime Departure { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("arrival")]
        public DateTime Arrival { get; set; }

        [JsonProperty("duration")]
        public double Duration
        {
            get { return _duration.TotalSeconds; }
            set { _duration = TimeSpan.FromSeconds(value); }
        }

        public TimeSpan DurationTS
        {
            get { return _duration; }
            set
            {
                _duration = value;
            }
        }

        [JsonProperty("legs")]
        public Leg[] Legs { get; set; }

        public int Transfers
        {
            get 
            {
                int number = -1;
                foreach(Leg leg in Legs)
                {
                    if(leg.Type != "walk" & leg.Exit != null)
                    {
                        number = number + 1;
                    }
                }
                return number;
            }
        }

        public string ToShortString()
        {
            return this.From + " (" + this.Departure.ToString("HH:mm") + ")-[" + this.Legs.Length + "]-" + this.To + " (" + this.Arrival.ToString("HH:mm") + ") (" + this.Duration + ")";
        }

        public string GetViaString()
        {
            string vias = String.Empty;
            string station = String.Empty;

            if (Legs.Length == 2)
            {
                return "direkt";
            }
            else
            {
                vias = "via ";
                foreach (Leg l in Legs)
                {
                    if (l.Exit != null)
                    {
                        if (l.Name == station)
                        {
                            if (station != "")
                            {
                                vias += "/";
                                vias += l.Departure.ToString("HH:mm");
                                vias += ")";
                            }
                        }
                        station = l.Exit.Name;
                        if (station != this.To)
                        {
                            if (l.Name != this.From)
                            {
                                vias += "-";
                            }
                            vias += station;
                            vias += " (";
                            vias += l.Exit.Arrival.ToString("HH:mm");
                        }
                    }
                }
            }
            return vias;
        }

        public string ViaString
        {
            get { return GetViaString(); }
        }

        private string RTFTabRow(string station, string arr, string dep, string journey) => $@"\trowd\cellx3000\cellx3800\cellx4600\cellx6000 {station}\intbl\cell {arr}\intbl\cell {dep}\intbl\cell {journey}\intbl\cell\row";

        public string ToRTF(string font = "Calibri Light")
        {

            string station = "";
            string arr = "";
            string dep = "";
            string journey = "";

            List<string> rtf = new List<string>();
            rtf.Add(String.Format("{{\\rtf1\\ansi\\deff0 {{\\fonttbl {{\\f0 {0};}}}}\\fs20", font));
            rtf.Add(@"\trowd\cellx3000\cellx3800\cellx4600\cellx6000\b Haltestelle\intbl\cell An\intbl\cell Ab\intbl\cell Verkehrsmittel\intbl\cell\b0\row");

            foreach (Leg l in Legs)
            {
                if (l.Exit != null)
                {
                    if (l.Name != station)
                    {
                        if (station != "")
                        {
                            rtf.Add(RTFTabRow(station, arr, "", ""));
                        }
                    }

                    if (l.Line != null)
                    {
                        journey = l.TypeName + " " + l.Line;
                        dep = l.Departure.ToString("HH:mm");
                    }
                    else
                    {
                        dep = "";
                        if (l.Type == "walk")
                        {
                            journey = String.Format("Fussweg {0} s", l.Runningtime);
                        }
                        else
                        {
                            journey = "";
                        }
                    }
                    rtf.Add(RTFTabRow(l.Name, arr, dep, journey));
                    station = l.Exit.Name;
                    if (dep != "")
                    {
                        arr = l.Exit.Arrival.ToString("HH:mm");
                    }
                    else
                    {
                        arr = "";
                    }
                }
            }
            if (station != "")
            {
                rtf.Add(RTFTabRow(station, arr, "", ""));
            }
            rtf.Add("}");

            return String.Concat(rtf);

        }
    }

    public class Leg
    {
        [JsonProperty("departure")]
        public DateTime Departure { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("line")]
        public string Line { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("type_name")]
        public string TypeName { get; set; }

        [JsonProperty("exit")]
        public Exit Exit { get; set; }

        [JsonProperty("runningtime")]
        public int Runningtime { get; set; }

    }

    public class Exit
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("arrival")]
        public DateTime Arrival { get; set; }

    }

    public class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.DateTime,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatString = @"yyyy'-'MM'-'dd' 'HH':'mm':'ss",
        };
    }
}
