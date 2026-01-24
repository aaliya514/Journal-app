using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Journal_app.Models
{
    public static class MoodConstants
    {
        public static readonly Dictionary<string, List<string>> MoodCategories = new()
        {
            { "Positive", new List<string> { "Happy", "Excited", "Relaxed", "Grateful", "Confident" } },
            { "Neutral", new List<string> { "Calm", "Thoughtful", "Curious", "Nostalgic", "Bored" } },
            { "Negative", new List<string> { "Sad", "Angry", "Stressed", "Lonely", "Anxious" } }
        };

        public static List<string> GetAllMoods()
        {
            return MoodCategories.Values.SelectMany(x => x).ToList();
        }

        public static string GetMoodCategory(string mood)
        {
            foreach (var category in MoodCategories)
            {
                if (category.Value.Contains(mood))
                    return category.Key;
            }
            return "Unknown";
        }

        public static string GetMoodEmoji(string mood)
        {
            return mood switch
            {
                "Happy" => "😊",
                "Excited" => "🤩",
                "Relaxed" => "😌",
                "Grateful" => "🙏",
                "Confident" => "💪",
                "Calm" => "😐",
                "Thoughtful" => "🤔",
                "Curious" => "🧐",
                "Nostalgic" => "💭",
                "Bored" => "😑",
                "Sad" => "😢",
                "Angry" => "😠",
                "Stressed" => "😰",
                "Lonely" => "😔",
                "Anxious" => "😟",
                _ => "📝"
            };
        }
    }
}