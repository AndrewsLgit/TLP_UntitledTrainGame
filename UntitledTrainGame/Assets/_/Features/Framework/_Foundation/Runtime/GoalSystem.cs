using System.Collections.Generic;
using Database.Runtime;

namespace Foundation.Runtime
{
    public class GoalSystem : FMono
    {

        #region Publics

        List<Goal> AllGoals { get; set; } = new List<Goal>();

        #endregion


        #region Unity API

        //

        #endregion


        #region Main Methods

        public void AddGoal(Goal goal)
        {
            SetFact<Goal>(goal.Id, goal, true);
            AllGoals.Add(goal);
        }

        public void EvaluateGoals(FactDictionary goalsFacts)
        {
            //
        }

        public List<Goal> GetGoalsByState(GoalState state)
        {
            return AllGoals != null ? AllGoals.FindAll(goal => goal.State == state) : new List<Goal>();
        }

        #endregion


        #region Utils

        /* Fonctions privées utiles */

        #endregion


        #region Privates and Protected

        // Variables privées

        #endregion
    }
}

