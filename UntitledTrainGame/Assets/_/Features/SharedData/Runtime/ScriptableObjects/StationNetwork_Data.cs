using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SharedData.Runtime
{
    [CreateAssetMenu(fileName = "StationNetwork_Data", menuName = "Scriptable Objects/StationNetwork_Data")]
    public class StationNetwork_Data : ScriptableObject
    {
        #region Variables
        
        public string Name;
        public StationPrefix LinePrefix;
        //public Station_Data[] Stations; // StationDB Reference
        public StationNode[] Connections;
        
        #endregion

        #region Main Methods
        
        public List<Station_Data> CalculatePath(Station_Data from, Station_Data to)
        {
            if(from == null || to == null) return null;
            
            // Dictionary to store the previous station in the shortest path
            var previousStation = new Dictionary<Station_Data, Station_Data>();
            
            // Queue for BFS (Breadth-First Search)
            var queue = new Queue<Station_Data>();
            
            // Set of visited stations
            var visited = new HashSet<Station_Data>();
            
            queue.Enqueue(from);
            visited.Add(from);

            while (queue.Count > 0)
            {
                var currentStation = queue.Dequeue();
                //Debug.Log($"Processing {currentStation.DisplayName}");
                
                // If destination reached
                if (currentStation == to)
                {
                    //Debug.Log($"Path found: {ReconstructPath(previousStation, from, to)}");
                    return ReconstructPath(previousStation, from, to);
                }
                
                // Get all nodes (both directions)
                foreach (var connection in Connections)
                {
                    Station_Data nextStation = null;
                    
                    // check forward direction
                    if (connection.From == currentStation)
                    {
                        nextStation = connection.To;
                        //Debug.Log($"Found connection: {connection.From.DisplayName} -> {connection.To.DisplayName}");
                    }
                    // check backward direction
                    else if (connection.To == currentStation)
                    {
                        nextStation = connection.From;
                        //Debug.Log($"Found connection: {connection.To.DisplayName} -> {connection.From.DisplayName}");   
                    }
                    
                    // if connection found and next station hasn't been visited
                    if (nextStation == null || visited.Contains(nextStation)) continue;
                    //Debug.Log($"Adding {nextStation.DisplayName} to queue (came from {currentStation.DisplayName})");
                    visited.Add(nextStation);
                    previousStation[nextStation] = currentStation;
                    queue.Enqueue(nextStation);

                }
            }
            
            // no path found
            return null;
        }

        #endregion
        
        #region Utils
        
        private List<Station_Data> ReconstructPath(Dictionary<Station_Data, Station_Data> previousStation,
            Station_Data start, Station_Data end)
        {
            var path = new List<Station_Data>();
            var current = end;
            
            // create path from end to start
            while (current != null)
            {
                path.Add(current);
                previousStation.TryGetValue(current, out current);
                
                // if current is start, path found
                if (current != start) continue;
                path.Add(start);
                break;
            }
            path.Reverse();
            return path;
        }
        
        // Helper method to get travel time between two stations
        public float GetTravelTime(Station_Data from, Station_Data to)
        {
            var connection = Connections.FirstOrDefault(c => 
                (c.From == from && c.To == to) || (c.From == to && c.To == from));
            
            return connection.TravelTime;
        }

        #endregion
    }

    [Serializable]
    public struct StationNode
    {
        public Station_Data From;
        public Station_Data To;
        public float TravelTime;
        
        public StationNode(Station_Data from, Station_Data to, float travelTime)
        {
            From = from;
            To = to;
            TravelTime = travelTime;
        }
    }

    public enum StationPrefix
    {
        A,
        B,
        C,
        D,
    }
}