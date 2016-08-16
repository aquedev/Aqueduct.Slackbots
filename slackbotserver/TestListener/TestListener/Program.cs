using Nancy;
using Nancy.Hosting.Self;
using Nancy.ModelBinding;
using Newtonsoft.Json;
using ServiceStack.Text;
using Slack.Webhooks;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Threading;
using System.IO;

namespace TestListener
{
    //----
    class Program
    {
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string ApplicationName = "TestListener";

        static void Main(string[] args)
        {
            JsConfig.EmitLowercaseUnderscoreNames = true;
            JsConfig.IncludeNullValues = false;
            JsConfig.PropertyConvention = JsonPropertyConvention.Lenient;
            HostConfiguration hostConfigs = new HostConfiguration()
            {
                UrlReservations = new UrlReservations() { CreateAutomatically = true }
            };
            String urlWithAccessToken = "https://hooks.slack.com/services/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";
            SlackSend client = new SlackSend(urlWithAccessToken);
            sendClient slackout = new sendClient();
            slackout.process(1);
            int i = 0;
            var test = new WebhookModule();
            var buuuuList = new List<developers>();
            var breakfastList = new List<developers>();
            var datewords = "";
            String[] done = new String[2];
            String name = "";

            //access google doc
            //authenticate
            //stuff for google docs---------------------------------------------------
            UserCredential credential;

            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/sheets.googleapis.com-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define request parameters.
            String spreadsheetId = "1YMLuQ1tJnTJs1FQN0yruMHAS41nIRm1FHT87pP3GCV0";
            String range = "Sheet1!A2:D";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            // Prints the slacknames and paid dates of devs in a spreadsheet:
            // https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit
            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;



            if (values != null && values.Count > 0)
            {
                Console.WriteLine("Name, Date");
                foreach (var row in values)
                {
                    name = (String)row[1];
                    datewords = (String)row[2];
                    done = datewords.Split('/');
                    breakfastList.Add(new developers
                    {
                        slackname = name,
                        lastpay = new date { day = done[0], month = done[1], year = done[2] }
                    });
                    // Print columns B and C, which correspond to indices 1 and 2.
                    Console.WriteLine("{0}, {1}", row[1], row[2]);


                }
            }
            else
            {
                Console.WriteLine("No data found.");
            }
            Console.Read();
           




            //------------------------------------------------------------------------------
            //adding dev team


            //end of dev team
            using (var host = new NancyHost(new Uri("http://localhost:1234"), new DefaultNancyBootstrapper(), hostConfigs))
            {
                host.Start();
                slackout.send(ref breakfastList);
                System.Threading.Thread.Sleep(20000);
                //System.Threading.Thread.Sleep(3600000);//1 hour
                test.update(ref buuuuList,ref breakfastList);

            }
            //end of process message
            slackout.process(0);
            //end console result
            foreach(var dev in buuuuList)
            {
                Console.Write(buuuuList[i].slackname + " ");
                Console.Write(buuuuList[i].lastpay.day + "/");
                Console.Write(buuuuList[i].lastpay.month + "/");
                Console.WriteLine(buuuuList[i].lastpay.year);
                i++;
            }
            i = 0;
            foreach(var dev in breakfastList)
            {
                Console.Write(breakfastList[i].slackname + " ");
                Console.Write(breakfastList[i].lastpay.day + "/");
                Console.Write(breakfastList[i].lastpay.month + "/");
                Console.Write(breakfastList[i].lastpay.year);
                i++;
            }
            Console.ReadLine();
            //order by dates - whos next to pay
            i = 0;
            //temp dev
            developers lastpayer = new developers();
            if (breakfastList.Count != 0)
            {
                lastpayer = breakfastList[i];
                foreach (var cooldev in breakfastList)
                {
                    if (Int32.Parse(lastpayer.lastpay.year) < Int32.Parse(breakfastList[i].lastpay.year))
                    {
                        if (Int32.Parse(lastpayer.lastpay.month) < Int32.Parse(breakfastList[i].lastpay.month))
                        {
                            if (Int32.Parse(lastpayer.lastpay.day) < Int32.Parse(breakfastList[i].lastpay.day))
                            {
                                lastpayer = breakfastList[i];
                            }
                        }
                    }

                    i++;
                }//end foreach
                client.PostMessage(text: "It is @" + lastpayer.slackname + " turn to pay",
                    channel: "#general");

                //appending the doc sheets 
                 //spreadsheetId = "1YMLuQ1tJnTJs1FQN0yruMHAS41nIRm1FHT87pP3GCV0";
                 //range = "Sheet1!A2:D";
                 //ValueRange test1234 = request.Execute();
                 //request = service.Spreadsheets.Values.Update();




            } else
            {
                client.PostMessage(text: "no breakky?? :(",
                    channel: "#general");
            }
            //closing message
            client.PostMessage(text: "Better luck next time for breky time!",
                channel: "#general");
        }
    }
    //-------
    //------ sending mesasages
    public class sendClient
    {
        string urlWithAccessToken;
        SlackSend client;
        int i;

        public sendClient()
        {
            urlWithAccessToken = "https://hooks.slack.com/services/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";
            client = new SlackSend(urlWithAccessToken);
        }
        public string step(int step)
        {
            String message;
            if (step == 1)
                message = "start";
             else
                message = "stop";
            return "Process " + message + "!";
        }
        public int send (ref List<developers> breakfastList)
        {
            i = 0;
            foreach (var dev in breakfastList)
            {
                client.PostMessage(text: "@" + breakfastList[i].slackname + " can you make it for breakfast",
                       channel: "#general");
                i++;
            }

            return 0;
        }
        public void process(int part)
        {
            String a = step(part);
            client.PostMessage(text: a,
                channel: "#general");
        }
    }
    //---receiving messages module
    public class WebhookModule : Nancy.NancyModule
    {
        public static List<developers> nolist = new List<developers>();
        public static List<developers> yeslist = new List<developers>();
        public WebhookModule()
        {
            Post["/"] = _ =>
            {
                var model = this.Bind<HookMessage>();
                var message = string.Empty;
                Console.WriteLine(model.text.ToLower());
                if (model.token != "5jZxYUF2PKJgh9zDnaomRJ0V") { 

                    Console.WriteLine("Invalid Token\n Ignored!");
                    return null;
                }
                if (model.text.ToLower().StartsWith("breky yes"))
                {
                    message = string.Format("@" + model.user_name + " Recieved!");
                    yeslist.Add(new developers
                    {
                        slackname = model.user_name,
                        lastpay = {}
                    });
                    Console.WriteLine("'" + message + "' sent back to @" + model.user_name);
                }
                if(model.text.ToLower().StartsWith("breky no"))
                {
                    message = string.Format("@" + model.user_name + " Recieved! removed from this weeks breky list");
                    String name = model.user_name;
                    nolist.Add(new developers
                    {
                        slackname = model.user_name,
                        lastpay = {}
                    });
                    Console.WriteLine("'" + message + "' sent back to @" + model.user_name);
                }
                if (!string.IsNullOrWhiteSpace(message))
                    return new SlackMessage { Text = message, Username = "breky bot"};
                return null;
            };
        }
        public void update(ref List<developers> buuuuList, ref List<developers> breakfastList)
        {
            var tempdevs = new List<developers>();
            int i = 0;
            //temp store all the devs
            foreach (var dev in breakfastList)
            {
                tempdevs.Add(breakfastList[i]);
                i++;
            }

            //put names into both lists
            buuuuList = nolist;
            breakfastList = yeslist;
            //put dates back in for each relevant name
            for (int a = 0; a < yeslist.Count; a++)
            {
                if (breakfastList[a].slackname == yeslist[a].slackname)
                {
                    for (int b = 0; b < tempdevs.Count; b++)
                    {
                        if (breakfastList[a].slackname == tempdevs[b].slackname)
                        {
                            breakfastList[a] = tempdevs[b];
                        }
                    }//end foreach
                }//end if 
            }//end for , for yes list
            //sorting no list
            for (int a = 0; a < nolist.Count; a++)
            {
                if (buuuuList[a].slackname == nolist[a].slackname)
                {
                    for (int b = 0; b < tempdevs.Count; b++)
                    {
                        if (buuuuList[a].slackname == tempdevs[b].slackname)
                        {
                            buuuuList[a] = tempdevs[b];
                        }
                    }//end foreach
                }//end if 
            }//end for , for no list



        }//end update
    }
    //------
    public class developers
    {
        public string slackname { get; set; }
        public date lastpay { get; set; }
    }
    //------
    public class date
    {
        public string day { get; set; }
        public string month { get; set; }
        public string year { get; set; }
    }
    //------
    public class HookMessage
    {
        public string token { get; set; }
        public string team_id { get; set; }
        public string channel_id { get; set; }
        public string channel_name { get; set; }
        public string user_id { get; set; }
        public string user_name { get; set; }
        public string text { get; set; }
        public string trigger_word { get; set; }
    }
    //-------
    public class TitleCaseFieldNameConverter : IFieldNameConverter
    {
        public string Convert(string fieldName)
        {
            return fieldName.ToTitleCase();
        }
    }
    //------
    public class Bootstrapper : Nancy.DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(Nancy.TinyIoc.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
        {
            container.Register<IFieldNameConverter, TitleCaseFieldNameConverter>();
            base.ApplicationStartup(container, pipelines);
        }
    }
    //-----sending stuff
    public class SlackSend
    {
        private readonly Uri _uri;
        private readonly Encoding _encoding = new UTF8Encoding();

        public SlackSend(string urlWithAccessToken)
        {
            _uri = new Uri(urlWithAccessToken);
        }

        //Post a message using simple strings
        public void PostMessage(string text, string channel)
        {
            Payload payload = new Payload()
            {
                Channel = channel,
                Text = text
            };
            PostMessage(payload);
        }

        //Post a message using a Payload object
        public void PostMessage(Payload payload)
        {
            string payloadJson = JsonConvert.SerializeObject(payload);

            using (WebClient client = new WebClient())
            {
                NameValueCollection data = new NameValueCollection();
                data["payload"] = payloadJson;

                var response = client.UploadValues(_uri, "POST", data);

                //The response text is usually "ok"
                string responseText = _encoding.GetString(response);
            }
        }
    }

    //This class serializes into the Json payload required by Slack Incoming WebHooks
    public class Payload
    {
        [JsonProperty("channel")]
        public String Channel { get; set; }

        [JsonProperty("text")]
        public String Text { get; set; }
    }
}
