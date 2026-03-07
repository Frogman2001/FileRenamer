using System;

namespace FileRenamer
{
    internal static class Compliments
    {
        private static readonly string[] Messages = new[]
        {
            "Your attention to detail is genuinely impressive.",
            "You bring such clarity and structure to every project you touch.",
            "Your colleagues are lucky to have someone so dependable on their team.",
            "You have a real talent for turning complexity into something manageable.",
            "Your ability to stay organized under pressure is exceptional.",
            "You consistently deliver work that is polished and thoughtful.",
            "You have a remarkable knack for seeing three steps ahead.",
            "Your professionalism sets a high standard for everyone around you.",
            "You make even challenging tasks feel under control.",
            "Your judgment and decision-making are consistently spot on.",
            "You balance efficiency and quality in a way that is rare.",
            "You communicate with such calm, confident precision.",
            "Your reliability makes it easy for others to trust your leadership.",
            "You’re excellent at anticipating what a project will need next.",
            "You bring order and focus wherever you go.",
            "Your work ethic and consistency are truly admirable.",
            "You have an impressive ability to keep moving forward, even on tough days.",
            "You make smart, thoughtful choices that clearly reflect your experience.",
            "Your sense of responsibility is a major asset to any team.",
            "You’re incredibly good at transforming plans into real, tangible results.",
            "You manage your time with the confidence of a true pro.",
            "Your calm, steady presence keeps things on track.",
            "You bring a reassuring sense of competence to every situation.",
            "You have a great instinct for organizing both people and information.",
            "You’re the kind of person others can always count on to follow through.",
            "Your consistency and follow-through are deeply impressive.",
            "You combine experience and practicality in a really powerful way.",
            "You make complex work look gracefully simple.",
            "Your standards are high in the best possible way.",
            "You navigate competing priorities with real skill and focus."
        };

        private static readonly Random Random = new();

        public static string GetRandomCompliment()
        {
            if (Messages.Length == 0)
            {
                return "You are doing excellent work.";
            }

            lock (Random)
            {
                var index = Random.Next(Messages.Length);
                return Messages[index];
            }
        }
    }
}

