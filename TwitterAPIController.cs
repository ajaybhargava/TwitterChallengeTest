using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TwitterChallenge.Models;

namespace TwitterChallenge.Controllers
{
    public class TwitterAPIController : ApiController
    {
        // GET: api/TwitterAPI
        public IEnumerable<string> Get()
        {
            
            Logger.LogWrite("Received API request to return top ten tweets.");

            var QueueTweets = GetTopTenTweets.Instance.GetQueueOfTweets();
            string[] arrTweets = new string[10];
            int iTweetCount = 0;
            foreach (var tweetMsg in QueueTweets)
            {
                if (iTweetCount < 10)
                {
                    var json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(tweetMsg);
                    arrTweets[iTweetCount] = json;
                    ++iTweetCount;
                }
                else
                {
                    break;
                }
            }

            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendFormat("Response with last {0} tweets sent successfully.", iTweetCount);
            Logger.LogWrite(strBuilder.ToString());

            Logger.LogWrite("Response with last ten tweets sent successfully.");
            return arrTweets;
        }

        // GET: api/TwitterAPI/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/TwitterAPI
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/TwitterAPI/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/TwitterAPI/5
        public void Delete(int id)
        {
        }
    }
}
