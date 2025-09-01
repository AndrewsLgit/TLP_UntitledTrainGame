using System;
using UnityEngine;

namespace Tools.Runtime
{
    [Serializable]
    public struct GameTime : IEquatable<GameTime>
    {
        private int hours;
        private int minutes;

        public GameTime(int hours, int minutes)
        {
            this.hours = (hours % 24 + 24) % 24;
            this.minutes = (minutes % 60 + 60) % 60;
            // if minutes overflow, normalize via FromTotalMinutes()
        }
        
        // convert to absolute minutes
        public int ToTotalMinutes()
        {
            return hours * 60 + minutes;
        }
        
        public static GameTime FromTotalMinutes(int totalMinutes)
        {
            var total = ((totalMinutes % 1440) + 1440) % 1440;
            int hours = total / 60;
            int minutes = total % 60;
            return new GameTime(hours, minutes);
        }
        public GameTime AddMinutes(int deltaMin)
        {
            return FromTotalMinutes(ToTotalMinutes() + deltaMin);
        }

        public bool Equals(GameTime other)
        {
            return hours == other.hours && minutes == other.minutes;
        }

        public override string ToString()
        {
            return $"{hours:D2}:{minutes:D2}";
            // return $"{hours:00}:{minutes:00}";
        }
    }
}
