using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwitterChallenge.Controllers;
using TwitterChallenge.Models;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Dynamic;

namespace TwitterChallengeTest.Tests
{
    [TestClass]
    public class TestGetTopTenTweets : GetTopTenTweets
    {
        dynamic dynUser = new ExpandoObject();
        dynamic dynTweets1 = new ExpandoObject();
        dynamic dynTweets2 = new ExpandoObject();

        public TestGetTopTenTweets()
        {
            //build mock data

            dynUser.name = "user1";
            dynUser.screen_name = "user1";
            dynUser.profile_image_url_https = "Image1";

            dynTweets1.name = dynUser.name;
            dynTweets1.created_at = "2017-01-01";
            dynTweets1.text = "content";
            dynTweets1.screen_name = dynUser.screen_name;
            dynTweets1.ProfileImage = dynUser.profile_image_url_https;
            dynTweets1.retweet_count = 2;

            dynTweets2.name = dynUser.name;
            dynTweets2.created_at = "2017-01-01";
            dynTweets2.text = "content 2";
            dynTweets2.screen_name = dynUser.screen_name;
            dynTweets2.ProfileImage = dynUser.profile_image_url_https;
            dynTweets2.retweet_count = 2;
        }

        [TestMethod]
        public void Test_PopulateTweets()
        {
            dynamic[] dynTweets = new dynamic[2];
            dynTweets[0] = dynTweets1;
            dynTweets[1] = dynTweets2;
            
            dynamic[] dynUsers = new dynamic[1];
            dynUsers[0] = dynUser;

            var enumerableTweets = (dynTweets as IEnumerable<dynamic>);
            bool bRet = PopulateTweets(enumerableTweets, dynUser);
            
            Assert.AreEqual(GetTopTenTweets.Instance.PopulateTweets(enumerableTweets, dynUser), false, "PopulateTweets method working correctly for correct inputs");

        }
    }
}
