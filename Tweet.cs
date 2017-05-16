using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TwitterChallenge.Models
{
    public class Tweet
    {
        public string UserName { get; set; }

        public string ScreenName { get; set; }

        public string ProfileImage { get; set; }

        public string TweetContent { get; set; }

        public int TimesReTweeted { get; set; }

        public string TweetDate { get; set; }

    }
}