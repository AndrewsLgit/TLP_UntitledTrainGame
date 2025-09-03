using System;

namespace SharedData.Runtime
{
    /// <summary>
    /// Represents in-game time as Hours and Minutes.
    /// This is used both for the clock and for durations (advance by X Hours/Minutes).
    /// </summary>
    [Serializable]
    public struct GameTime : IComparable<GameTime>
    {
        // Variables need to be public for serialization
        public int Hours;
        public int Minutes;

        public GameTime(int hours, int minutes)
        {
            // Hours = (hours % 24 + 24) % 24;
            // Minutes = (minutes % 60 + 60) % 60;
            // if Minutes overflow, normalize via FromTotalMinutes()
            Hours = hours;
            Minutes = minutes;
            Normalize();
        }

        /// <summary>
        /// Normalize Hours/Minutes into valid ranges:
        /// - Minutes 0..59
        /// - Hours 0..23 (wraps around if needed)
        /// </summary>
        public void Normalize()
        {
            // Carry over if Minutes overflow
            if (Minutes >= 60)
            {
                Hours += Minutes / 60;
                Minutes %= 60;
            }
            // borrow if minutes underflow
            else if (Minutes < 0)
            {
                int borrow = (Math.Abs(Minutes) + 59) / 60;
                Hours -= borrow;
                Minutes += borrow * 60;
            }
            
            // Wrap hours into 0-23
            if (Hours >= 24) 
                Hours %= 24;
            if (Hours < 0) 
                Hours = (24 + (Hours % 24)) % 24;
        }
        
        /// <summary>
        /// Add another GameTime (duration) to this time.
        /// Example: 10:30 + 1:45 = 12:15
        /// </summary>
        public GameTime AddTime(GameTime delta)
        {
            int newHours = delta.Hours + Hours;
            int newMinutes = delta.Minutes + Minutes;
            return new GameTime(newHours, newMinutes); // Constructor calls Normalize()
        }
        
        /// <summary>
        /// Convert this time to total minutes since 00:00.
        /// </summary> 
        public int ToTotalMinutes()
        {
            return Hours * 60 + Minutes;
        }
        
        public static GameTime FromTotalMinutes(int totalMinutes)
        {
            var total = ((totalMinutes % 1440) + 1440) % 1440;
            int hours = total / 60;
            int minutes = total % 60;
            return new GameTime(hours, minutes);
        }


        public int CompareTo(GameTime other)
        {
            return ToTotalMinutes().CompareTo(other.ToTotalMinutes());
        }

        public override string ToString()
        {
            return $"{Hours:D2}:{Minutes:D2}";
            // return $"{Hours:00}:{Minutes:00}";
        }
    }
}