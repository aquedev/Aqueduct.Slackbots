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
        //static variables
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "TestListener";

        static void Main(string[] args)
        {
            //variables
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
            int i = 0;
            var test = new WebhookModule();
            var buuuuList = new List<developers>();
            var breakfastList = new List<developers>();
            var datewords = "";
            String[] done = new String[2];
            String name = "";
            UserCredential credential;
            //start of breakfast process
            slackout.process(1);
            //loof for and read credentials for accessing and updating dev table
            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/sheets.googleapis.com-dotnet-quickstart.json");
                //credentials for accessing table
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
            ValueRange response = request.Execute();//fetch everything from range(in worksheet : Sheet1, with a range of A2:all of D(should be 14))
            IList<IList<Object>> values = response.Values;//put into indexed list of individualised objects
            //if a list of values has been found
            if (values.Count > 0)
            {
                //for each person in the list, create a new developer for them and add them to ateending list
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
            {//if nothing was found print to server console that nothing was found. - would be a error
                Console.WriteLine("No data found.");
            }
            //start listening to channels host
            using (var host = new NancyHost(new Uri("http://localhost:1234"), new DefaultNancyBootstrapper(), hostConfigs))
            {
                host.Start();
                slackout.send(ref breakfastList);
                //System.Threading.Thread.Sleep(300000);//30 mins
                System.Threading.Thread.Sleep(3600000);//1 hour
                test.update(ref buuuuList,ref breakfastList);
            }
            //end of process message
            slackout.process(0);
            //end server console result of table fetch
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
            //order by dates - whos next to pay
            i = 0;
            //temp dev
            developers lastpayer = new developers();
            developers lastpayer2 = new developers(); // incase 2 payers are needed
            //find last person to pay
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
                //if there are 10 people or more attending find another person to help pay
                //the next person would be the next person who would pay
                if (breakfastList.Count >= 10)
                {
                    foreach (var otherdev in breakfastList)
                    {
                        int j = 0;
                        if (Int32.Parse(lastpayer2.lastpay.year) < Int32.Parse(breakfastList[j].lastpay.year))
                        {
                            if (Int32.Parse(lastpayer2.lastpay.month) < Int32.Parse(breakfastList[j].lastpay.month))
                            {
                                if (Int32.Parse(lastpayer2.lastpay.day) < Int32.Parse(breakfastList[j].lastpay.day))
                                {
                                    if (lastpayer.slackname != breakfastList[j].slackname)
                                    {
                                        lastpayer2 = breakfastList[j];
                                    }
                                }
                            }
                        }
                        j++;
                    }//end foreach otherdev
                }
                //posting messages to channel on results of proccess
                client.PostMessage(text: "It is @" + lastpayer.slackname + " turn to pay",
                    channel: "#breakfastmeet");
                if (!(lastpayer2.slackname == null))
                {
                    client.PostMessage(text: "and @" + lastpayer2.slackname + " has to pay aswell\n because of too many people!",
                        channel: "#breakfastmeet");
                }
                //appending the doc sheets 
                client.PostMessage(text: "there are a total of " + breakfastList.Count + " people attending breakfast!",
                    channel: "#breakfastmeet");
                for (i = 0; i < response.Values.Count; i++)
                {
                    if ((string)response.Values[i][1] == lastpayer.slackname)
                    {
                        response.Values[i][2] = DateTime.Now.ToString("dd/MM/yyyy");
                    }
                    if (lastpayer2.slackname != null || lastpayer2.slackname == "")
                    {
                        if((string)response.Values[i][1] == lastpayer2.slackname)
                        {
                            response.Values[i][2] = DateTime.Now.ToString("dd/MM/yyyy");
                        }
                    }
                }
                //update table with new last pay date of devs who just payed for breakfast
                String spreadsheetId2 = "1YMLuQ1tJnTJs1FQN0yruMHAS41nIRm1FHT87pP3GCV0";
                String range2 = "Sheet1!A2:D14";
                SpreadsheetsResource.ValuesResource.UpdateRequest request2 = 
                    service.Spreadsheets.Values.Update(response,spreadsheetId2, range2);
                ///execute order 666
                request2.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                request2.Execute();
            } else
            {//if no one wants to go :( 
                client.PostMessage(text: "no breakky?? :(",
                    channel: "#breakfastmeet");
            }
            //closing message
            //for those that couldnt make it
            client.PostMessage(text: "Better luck next time for breky time!",
                channel: "#breakfastmeet");
            //closes
        }
    }
    //------ sending mesasages
    public class sendClient
    {
        string urlWithAccessToken;
        SlackSend client;
        int i;
        //send client ( kinda obsolute might delete later ?)
        public sendClient()
        {
            urlWithAccessToken = "https://hooks.slack.com/services/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";
            client = new SlackSend(urlWithAccessToken);
        }
        public string step(int step)
        {//methods for determining which message to post 
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
            {//method for asking a bunch of people if they can make breakfast
                client.PostMessage(text: "@" + breakfastList[i].slackname + " can you make it for breakfast \n to reply type 'breaky yes/no'",
                       channel: "@" + breakfastList[i].slackname);
                i++;
            }
            return 0;
        }
        public void process(int part)
        {//method for posting which process stage we are at
            String a = step(part);
            client.PostMessage(text: a,
                channel: "#breakfastmeet");
        }
    }
    //---receiving messages module
    public class WebhookModule : Nancy.NancyModule
    {
        //list of people as a inbetween list for connewcting main method with this module ( cant ref since its a webhook post method)
        public static List<developers> nolist = new List<developers>();
        public static List<developers> yeslist = new List<developers>();
        public WebhookModule()
        {//post mwethod
            Post["/"] = _ =>
            {
                var model = this.Bind<HookMessage>();
                var message = string.Empty;
                Console.WriteLine(model.text.ToLower());
                if (model.token != "8bRYHHpblrCz5oe9AfhwUTKN") { //checks the token of incoming message if found
                    Console.WriteLine("Invalid Token\n Ignored!");
                    return null;//ignored if not recognised
                }
                if (model.text.ToLower().StartsWith("breaky yes"))
                {//if the trigger word was found - written in correct format and response was yes
                    message = string.Format("" + model.user_name + " Recieved! Added to breakfast list");
                    yeslist.Add(new developers//add them ti yeslist -> goes into breakfastList in update method
                    {
                        slackname = model.user_name,
                        lastpay = {}
                    });
                    Console.WriteLine("'" + message + "' sent back to " + model.user_name);
                }
                if(model.text.ToLower().StartsWith("breaky no"))
                {//if response is no
                    message = string.Format("" + model.user_name + " Recieved! removed from this weeks breky list");
                    String name = model.user_name;
                    nolist.Add(new developers//adds them to nolist -> goes into buuuuList in update method
                    {
                        slackname = model.user_name,
                        lastpay = {}
                    });
                    Console.WriteLine("'" + message + "' sent back to " + model.user_name);
                }
                if (!string.IsNullOrWhiteSpace(message))//if message is not empty
                    return new SlackMessage { Text = message, Username = "breaky bot"};
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
    //------developer var
    public class developers
    {
        public string slackname { get; set; }
        public date lastpay { get; set; }
    }
    //------date var
    public class date
    {
        public string day { get; set; }
        public string month { get; set; }
        public string year { get; set; }
    }
    //------hookmessage var
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
