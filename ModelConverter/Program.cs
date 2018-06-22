using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            List<AbsAction> actions = new List<AbsAction>();
            actions.Add(new Obj2SingleRawAction());
            actions.Add(new Obj2MultiRawAction());

            if (args.Length < 1)
            {
                Console.WriteLine("Please provide action name. List of all possible actions:");
                foreach (var action in actions)
                {
                    Console.WriteLine(action.GetReadme());
                }
                return;
            }
            string actionName = args[0];
            List<string> arguments = args.Skip(1).ToList();
            foreach (var action in actions)
            {
                if(action.GetName() == actionName)
                {
                    action.Run(arguments);
                    return;
                }
            }
            Console.WriteLine("Action named {0} not found. List of all possible actions:", actionName);
            foreach (var action in actions)
            {
                Console.WriteLine(action.GetReadme());
            }
        }
    }
}
