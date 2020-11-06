using System;
using System.Collections.Generic;
using System.Text;

namespace Shimakaze
{
    class FunConsts
    {
        public static readonly string[] EightBallChoices = new string[]
        {
            "Signs point to yes",
            "Yes",
            "Reply hazy, try again",
            "Without a doubt",
            "My sources say no",
            "As I see it, yes",
            "You may rely on it",
            "Concentrate and ask again",
            "Outlook not so good",
            "It is decidedly so",
            "Better not tell you now",
            "Very doubtful",
            "Yes - definitely",
            "It is certain",
            "Cannot predict now",
            "Most likely",
            "Ask again later",
            "My reply is no",
            "Outlook good",
            "Don't count on it",
            "Who cares?",
            "Never, ever, ever",
            "Possibly",
            "There is a small chance"
        };

        public static readonly Dictionary<char, string> LeetKeyChars = new Dictionary<char, string>()
        {
            {'a', "4"},
            {'e', "3"},
            {'f', "ph"},
            {'g', "9"},
            {'l', "1"},
            {'o', "0"},
            {'s', "5"},
            {'t', "7"},
            {'y', "\\`/"}
        };

        public static string Random8BallChoice()
        {
            return EightBallChoices[ThreadSafeRandom.ThisThreadsRandom.Next(0, EightBallChoices.Length)];
        }
    }
}
