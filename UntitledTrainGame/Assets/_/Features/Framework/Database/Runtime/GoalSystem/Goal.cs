using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Database.Runtime
{
    public class Goal
    {

        #region Publics

        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] RequiredFacts { get; set; }
        public GoalType Type { get; set; }
        public GoalState State { get; set; }
        public bool IsCompleted { get; set; }
        public List<Goal> SubGoals { get; set; } = new List<Goal>();

        public Goal(string name, string description, string[] requiredFacts, GoalType type)
        {
            Id = $"{name}_{Time.deltaTime}";
            Name = name;
            Description = description;
            RequiredFacts = requiredFacts;
            Type = type;
            State = GoalState.Pending;
            IsCompleted = false;
        }

        #endregion


        #region Main Methods

        public bool Evaluate()
        {
            if (SubGoals.Any())
            {
                if (SubGoals.All(goal => goal.IsCompleted))
                {
                    CompleteGoal();
                    return true;
                }
                return false;
            }
            return State == GoalState.Completed;
        }

        public void StartGoal()
        {
            if (State == GoalState.Pending)
            {
                State = GoalState.InProgress;
                foreach (Goal goal in SubGoals)
                {
                    goal.StartGoal();
                }
            }
        }

        public bool CompleteGoal()
        {
            // Vérifie si l'objectif peut être complété (par exemple, toutes les conditions sont remplies)
            // Dans une implémentation réelle, on ajouterait ici plus de validation
            if (State != GoalState.Completed)
            {
                State = GoalState.Completed;
                IsCompleted = true;
                return true;
            }
            return false;
        }

        public void AddSubGoal(Goal goal)
        {
            SubGoals.Add(goal);
        }
        
        #endregion
    }
}

