using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csjobshop_flexiblesched
{
    class SetupTime : LongLongToLong
    {
        private IntervalVarVector intervals_;
        public static Dictionary<IntervalVar, int> TaskIntervalToTaskType;
        public static Dictionary<int, long>  TaskTypeSetupTime;
        public SetupTime(IntervalVarVector intervals)
        
        {
            intervals_ = intervals;
        }

        public override long Run(long first_index, long second_index)
        {
            if (first_index == second_index)
                return 0;

            
            //var type_1 = TaskIntervalToTaskType[intervals_[(int)first_index]];

            var type_1 = int.Parse(intervals_[(int)first_index].Name().Split('T')[1]);
            var type_2 = int.Parse(intervals_[(int)second_index].Name().Split('T')[1]);

            //var type_2 = TaskIntervalToTaskType[intervals_[(int)second_index]];
            if (type_1 != type_2) 
                return TaskTypeSetupTime[type_2];
            return 0;


            // TaskIntervalToTaskType (intervals_[first_index])
            //if (intervals_
                
              //  [first_index] != type_[second_index])
          //      return setupTime_[second_index];
           // else
               /// return 5;
        }
        
    }
}
