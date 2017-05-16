using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web.Script.Serialization;
using System.Configuration;
using System.Timers;
using System.Diagnostics;

namespace TwitterChallenge.Models
{
    public class GetTopTenTweets
    {
        private string oAuthConsumerSecret { get; set; }
        private string oAuthConsumerKey { get; set; }
        private string oAuthUrl { get; set; }
        private string oAuthScreenName { get; set; }
        private string oAccessToken { get; set; }

        private Queue<Tweet> queueTweets = new Queue<Tweet>();

        private Timer timer;

        private static readonly Lazy<GetTopTenTweets> lazy =
            new Lazy<GetTopTenTweets>(() => new GetTopTenTweets());

        public static GetTopTenTweets Instance { get { return lazy.Value; } }
        
        private GetTopTenTweets() //constructor
        {
            oAuthConsumerKey = ConfigurationManager.AppSettings["ConsumerKey"];
            oAuthConsumerSecret = ConfigurationManager.AppSettings["ConsumerSecret"];
            oAuthUrl = "https://api.twitter.com/oauth2/token";
            oAuthScreenName = "asbhrgava";
            
            //create a timer that fires every minute
            timer = new Timer(6000);
            timer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
            timer.AutoReset = false;
            timer.Start();

            lock (timer)
            {
                if (GetTweets("salesforce", "salesforce").Result)
                {
                    Logger.LogWrite("Initialized queue of tweets.");
                }
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Do stuff
            lock(timer)
            {
                if (GetTweets().Result)
                {
                    Logger.LogWrite("Refreshed tweets queue successfully.");
                }
                else
                {
                    Logger.LogWrite("Tweets queue can not be refreshed successfully.");
                }
                timer.Start(); // Restart next timer only when current job is done 
            }
        }

        public Queue<Tweet> GetQueueOfTweets()
        {
            return queueTweets;
        }

        private bool GetAccessTokenInfo()
        {
            try
            {
                if (!String.IsNullOrEmpty(oAccessToken))
                {
                    Logger.LogWrite("Access token already fetched.");
                    return false;
                }
                else
                {
                    Logger.LogWrite("Attempting to retrieve access token.");

                    // Do the Authenticate
                    var authHeaderFormat = "Basic {0}";
                    
                    var authHeader = string.Format(authHeaderFormat,
                        Convert.ToBase64String(Encoding.UTF8.GetBytes(Uri.EscapeDataString(oAuthConsumerKey) + ":" +
                        Uri.EscapeDataString((oAuthConsumerSecret)))
                    ));

                    var postBody = "grant_type=client_credentials";

                    System.Net.HttpWebRequest authRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(oAuthUrl);
                    authRequest.Headers.Add("Authorization", authHeader);
                    authRequest.Method = "POST";
                    authRequest.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
                    authRequest.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;

                    using (System.IO.Stream stream = authRequest.GetRequestStream())
                    {
                        byte[] content = ASCIIEncoding.ASCII.GetBytes(postBody);
                        stream.Write(content, 0, content.Length);
                    }

                    authRequest.Headers.Add("Accept-Encoding", "gzip");

                    System.Net.WebResponse authResponse = authRequest.GetResponse();

                    Logger.LogWrite("Response received for authentication request for access token.");

                    // deserialize into an object
                    using (authResponse)
                    {
                        using (var reader = new System.IO.StreamReader(authResponse.GetResponseStream()))
                        {
                            var serializer = new JavaScriptSerializer();
                            var json = reader.ReadToEnd();
                            dynamic item = serializer.Deserialize<object>(json);
                            oAccessToken = item["access_token"];
                            if(oAccessToken.Length > 0)
                            {
                                Logger.LogWrite("Access token retrieved successfully.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception happened {0}", ex);
            }

            return true;
        }
        
        private async Task<bool> CheckForNewTweets(string accessToken, string screenName = "salesforce")
        {
            try
            {
                if (String.IsNullOrEmpty(accessToken))
                {
                    throw new ArgumentNullException("accessToken");
                }

                var requestUserTimeline = new HttpRequestMessage(HttpMethod.Get,
                                                                 string.Format("https://api.twitter.com/1.1/statuses/user_timeline.json?count={0}&screen_name={1}&trim_user=1&exclude_replies=1",
                                                                 1,
                                                                 screenName));

                requestUserTimeline.Headers.Add("Authorization", "Bearer " + accessToken);

                var httpClient = new HttpClient();
                HttpResponseMessage responseUserTimeLine = await httpClient.SendAsync(requestUserTimeline).ConfigureAwait(false);
                var serializer = new JavaScriptSerializer();
                dynamic json = serializer.Deserialize<object>(await responseUserTimeLine.Content.ReadAsStringAsync());
                var enumerableTweets = (json as IEnumerable<dynamic>);
                if (enumerableTweets == null)
                {
                    Logger.LogWrite("Empty response received for User object query API.");
                    throw new ArgumentNullException("enumerableTweets");
                }

                Logger.LogWrite("Received response for UserTimeLine object query API.");

                foreach (dynamic tweetItem in enumerableTweets)
                {
                    if (queueTweets.Count > 0)
                    {
                        var queueTweetItem = queueTweets.ElementAt(0);

                        //check if created_at value for the latest tweet in queue matches that of latest tweet posted by the user.
                        if (tweetItem.ContainsKey("created_at") &&
                            queueTweetItem.TweetContent.ToString().CompareTo(tweetItem["created_at"]) == 0)
                        {
                            //No new tweets added
                            Logger.LogWrite("No new tweets added than what we already have.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder strBuilder = new StringBuilder();
                strBuilder.AppendFormat("Exception happened {0}", ex);
                Logger.LogWrite(strBuilder.ToString());
            }
            return true;
        }

        private async Task<dynamic> GetUserDetails(string userName = "salesforce",
                                                    string screenName = "salesforce")
        {
            var requestUser = new HttpRequestMessage(HttpMethod.Get, string.Format("https://api.twitter.com/1.1/users/show.json?screen_name={0}&user_id={1}", screenName, oAuthScreenName));

            requestUser.Headers.Add("Authorization", "Bearer " + oAccessToken);

            //Call User API
            var httpClient = new HttpClient();
            HttpResponseMessage responseUser = await httpClient.SendAsync(requestUser).ConfigureAwait(false);
            var serializerUser = new JavaScriptSerializer();
            dynamic jsonUserDetails = serializerUser.Deserialize<object>(await responseUser.Content.ReadAsStringAsync());
            Logger.LogWrite("Received response for User object query API.");
            return jsonUserDetails;
        }

        private async Task<bool> GetTweets(string userName = "salesforce", 
                                            string screenName = "salesforce", 
                                            int count = 10)
        {
            try
            {
                GetAccessTokenInfo();

                if(!CheckForNewTweets(oAccessToken, screenName).Result)
                {
                    //if no new tweets happened since last we fetched then return
                    return false;
                }

                //get user details that go with each tweet.
                //user profile image can change so we better get it each time.
                dynamic jsonUser = GetUserDetails().Result;

                //Call UserTimeLine API
                var requestUserTimeline = new HttpRequestMessage(HttpMethod.Get, string.Format("https://api.twitter.com/1.1/statuses/user_timeline.json?count={0}&screen_name={1}&trim_user=1&exclude_replies=1", count, userName));
                requestUserTimeline.Headers.Add("Authorization", "Bearer " + oAccessToken);
                
                var httpClient = new HttpClient();
                HttpResponseMessage responseUserTimeLine = await httpClient.SendAsync(requestUserTimeline).ConfigureAwait(false);
                var serializer = new JavaScriptSerializer();

                dynamic json = serializer.Deserialize<object>(await responseUserTimeLine.Content.ReadAsStringAsync());
                var enumerableTweets = (json as IEnumerable<dynamic>);
                if (enumerableTweets == null)
                {
                    Logger.LogWrite("Empty response received for User object query API.");
                    return false;
                }

                Logger.LogWrite("Received response for UserTimeLine object query API.");

                //Since we know that we have responses, lets clear the queue to be filled up again
                queueTweets.Clear();

                foreach (dynamic tweetItem in enumerableTweets)
                {
                    Tweet tweet = new Tweet();
                    tweet.UserName = jsonUser["name"];
                    tweet.ScreenName = jsonUser["screen_name"];
                    tweet.ProfileImage = jsonUser["profile_image_url_https"];
                    
                    //iterate over collection and look for desired keys
                    if (tweetItem.ContainsKey("created_at"))
                        tweet.TweetDate = tweetItem["created_at"];
                    if (tweetItem.ContainsKey("retweet_count"))
                        tweet.TimesReTweeted = tweetItem["retweet_count"];
                    if (tweetItem.ContainsKey("text"))
                        tweet.TweetContent = tweetItem["text"];
                    
                    //Enqueue latest tweets in a queue
                    queueTweets.Enqueue(tweet);
                    StringBuilder strBuilder = new StringBuilder();
                    strBuilder.AppendFormat("Enqueued tweet {0}", tweet.ToString());
                    Logger.LogWrite(strBuilder.ToString());
                }
            }

            catch (Exception ex)
            {
                StringBuilder strBuilder = new StringBuilder();
                strBuilder.AppendFormat("Exception happened {0}", ex);
                Logger.LogWrite(strBuilder.ToString());
            }

            return true;
        }
    }
}
