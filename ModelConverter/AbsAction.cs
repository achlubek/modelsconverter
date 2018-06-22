using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelConverter
{
    abstract class AbsAction
    {
        public abstract string GetName();
        public abstract string GetReadme();
        protected abstract void Initialize(List<string> arguments);
        protected abstract void Execute();
        public AbsAction()
        {

        }

        public void Run(List<string> arguments)
        {
            try
            {
                Initialize(arguments);
            } catch (ArgumentException e)
            {
                Console.WriteLine("Error occured: {0}", e.Message);
                Console.WriteLine(GetReadme());
                return;
            }
            try
            {
                Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured: {0}", e.Message);
            }
        }
    }
}
