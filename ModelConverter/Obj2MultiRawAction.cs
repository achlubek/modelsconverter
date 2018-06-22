using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ModelConverter
{
    class Obj2MultiRawAction : AbsAction
    {
        string SourceFile = "";

        protected override void Execute()
        {
            var elements = Object3dManager.LoadFromObj(SourceFile);
            foreach (var element in elements)
            {
                element.SaveRawWithTangents(element.Name + ".raw");
            }
        }

        public override string GetName()
        {
            return "obj2multiraw";
        }

        public override string GetReadme()
        {
            return "Usage: modelc " + GetName() + " source target";
        }

        protected override void Initialize(List<string> arguments)
        {
            if(arguments.Count != 1)
            {
                throw new ArgumentException("Invalid arguments count");
            }
            SourceFile = arguments[0];
            if (!File.Exists(SourceFile))
            {
                throw new ArgumentException("Source file does not exist");
            }
        }
    }
}
