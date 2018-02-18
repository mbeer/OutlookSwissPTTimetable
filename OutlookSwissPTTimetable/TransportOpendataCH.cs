using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;

namespace TransportOpendataCH
{

    public class ConnectionsRequest
    {
        private string from;
        private string to;
        private string[] via = Array.Empty<string>();
        private DateTime dateTime = DateTime.Now;
        private bool isArrivalTime = false;
        private uint limit;

        public ConnectionsRequest(string from, string to, string[] via = null, DateTime dateTime = default(DateTime), bool isArrivalTime = false, ushort limit = 4)
        {
            this.from = from ?? throw new ArgumentNullException(nameof(from));
            this.to = to ?? throw new ArgumentNullException(nameof(to));
            if (via != null) this.via = via;
            if (dateTime != default(DateTime)) this.dateTime = dateTime;
            this.isArrivalTime = isArrivalTime;
            this.limit = limit;
        }

        private HttpWebRequest PrepareWebRequest()
        {
            List<string> parameters = new List<string>
            {
                "from=" + Uri.EscapeUriString(from.Trim()),
                "to=" + Uri.EscapeUriString(to.Trim()),
                "date=" + dateTime.ToString("yyyy-MM-dd"),
                "time=" + dateTime.ToString("HH:mm"),
                "isArrivalTime=" + Convert.ToString(Convert.ToInt32(isArrivalTime))
            };
            foreach (string v in via)
            {
                parameters.Add("via[]=" + Uri.EscapeUriString(v.Trim()));
            }
            if (limit > 0)
            {
                parameters.Add("limit=" + Convert.ToString(limit));
            }

            string Webstring = @"https://transport.opendata.ch/v1/connections?" + String.Join("&", parameters);

            Uri Webaddress = new Uri(Webstring);
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
        [JsonProperty("connections")]
        public Connection[] Connections { get; set; }

        [JsonProperty("from")]
        public Location From { get; set; }

        [JsonProperty("to")]
        public Location To { get; set; }

        [JsonProperty("stations")]
        public Stations Stations { get; set; }

    }

    public class Walk
    {
        [JsonProperty("duration")]
        public string Duration { get; set; }
    }

    public class Connection
    {
        private TimeSpan _duration;

        [JsonProperty("from")]
        public Checkpoint From { get; set; }

        [JsonProperty("to")]
        public Checkpoint To { get; set; }

        [JsonProperty("duration")]
        public string Duration
        {
            get { return _duration.ToString(@"dd\dhh\:mm\:ss"); }
            set
            {
                Regex rgx = new Regex(@"^(\d+)d(\d{2}):(\d{2}):(\d{2})$");
                if (rgx.IsMatch(value))
                {
                    Match match = rgx.Match(value);
                    _duration = new TimeSpan(Int32.Parse(match.Groups[1].Value),
                        Int32.Parse(match.Groups[2].Value),
                        Int32.Parse(match.Groups[3].Value),
                        Int32.Parse(match.Groups[4].Value));
                }

            }
        }

        public TimeSpan DurationTS
        {
            get { return _duration; }
            set
            {
                _duration = value;
            }
        }


        [JsonProperty("transfers")]
        public long Transfers { get; set; }

        [JsonProperty("service")]
        public Service Service { get; set; }

        [JsonProperty("products")]
        public string[] Products { get; set; }

        [JsonProperty("capacity1st")]
        public int Capacity1st { get; set; }

        [JsonProperty("capacity2nd")]
        public int Capacity2nd { get; set; }

        [JsonProperty("sections")]
        public Section[] Sections { get; set; }

        public string ToShortString()
        {
            return this.From.Location.Name + " (" + this.From.Departure.ToString("HH:mm") + ")-[" + this.Transfers + "]-" + this.To.Location.Name + " (" + this.To.Arrival.ToString("HH:mm") + ") (" + this.Duration + ")";
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

            foreach (Section s in Sections)
            {
                if (s.Departure.Location.Name != station)
                {
                    if (station != "")
                    {
                        rtf.Add(RTFTabRow(station, arr, "", ""));
                    }
                }

                if (s.Journey != null)
                {
                    journey = s.Journey.Category;
                    if (journey.Length > 1) { journey += " "; }
                    journey += s.Journey.Number;
                    dep = s.Departure.Departure.ToString("HH:mm");
                }
                else
                {
                    dep = "";
                    if (s.Walk != null)
                    {
                        journey = String.Format("Fussweg {0} s", s.Walk.Duration);
                    }
                    else
                    {
                        journey = "";
                    }
                }
                rtf.Add(RTFTabRow(s.Departure.Location.Name, arr, dep, journey));
                station = s.Arrival.Location.Name;
                if (dep != "")
                {
                    arr = s.Arrival.Arrival.ToString("HH:mm");
                }
                else
                {
                    arr = "";
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

    public class Checkpoint
    {
        [JsonProperty("station")]
        public Location Station { get; set; }

        [JsonProperty("arrival")]
        public DateTime Arrival { get; set; }

        [JsonProperty("arrivalTimestamp")]
        public long ArrivalTimestamp { get; set; }

        [JsonProperty("departure")]
        public DateTime Departure { get; set; }

        [JsonProperty("departureTimestamp")]
        public long DepartureTimestamp { get; set; }

        [JsonProperty("delay")]
        public long Delay { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("prognosis")]
        public Prognosis Prognosis { get; set; }

        [JsonProperty("realtimeAvailability")]
        public object RealtimeAvailability { get; set; }

        [JsonProperty("location")]
        public Location Location { get; set; }
    }

    public class Location
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("score")]
        public float Score { get; set; }

        [JsonProperty("coordinate")]
        public Coordinates Coordinates { get; set; }

        [JsonProperty("distance")]
        public float Distance { get; set; }
    }

    public class Coordinates
    {
        [JsonProperty("type")]
        public string CoordinateType { get; set; }

        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }
    }

    public class Service
    {
        [JsonProperty("regular")]
        public string Regular { get; set; }

        [JsonProperty("irregular")]
        public string Irregular { get; set; }
    }

    public class Prognosis
    {
        [JsonProperty("platform")]
        public object Platform { get; set; }

        [JsonProperty("arrival")]
        public string Arrival { get; set; }

        [JsonProperty("departure")]
        public string Departure { get; set; }

        [JsonProperty("capacity1st")]
        public int Capacity1st { get; set; }

        [JsonProperty("capacity2nd")]
        public int Capacity2nd { get; set; }
    }

    public class Section
    {
        [JsonProperty("journey")]
        public Journey Journey { get; set; }

        [JsonProperty("walk")]
        public Walk Walk { get; set; }

        [JsonProperty("departure")]
        public Checkpoint Departure { get; set; }

        [JsonProperty("arrival")]
        public Checkpoint Arrival { get; set; }
    }


    public class Journey
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("subcategory")]
        public string Subcategory { get; set; }

        [JsonProperty("categoryCode")]
        public string CategoryCode { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("operator")]
        public string Operator { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("passList")]
        public Checkpoint[] PassList { get; set; }

        [JsonProperty("capacity1st")]
        public int Capacity1st { get; set; }

        [JsonProperty("capacity2nd")]
        public int Capacity2nd { get; set; }
    }

    public class Stations
    {
        [JsonProperty("from")]
        public Location[] From { get; set; }

        [JsonProperty("to")]
        public Location[] To { get; set; }
    }

    public class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
        };
    }
}
