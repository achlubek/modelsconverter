using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ModelConverter
{
    class Obj2SingleRawAction : AbsAction
    {
        string SourceFile = "";
        string TargetFile = "";

        protected override void Execute()
        {
            var element = Object3dManager.LoadFromObjSingle(SourceFile);
            element.SaveRawWithTangents(TargetFile);
        }

        public override string GetName()
        {
            return "obj2raw";
        }

        public override string GetReadme()
        {
            return "Usage: modelc " + GetName() + " source target";
        }

        protected override void Initialize(List<string> arguments)
        {
            if(arguments.Count != 2)
            {
                throw new ArgumentException("Invalid arguments count");
            }
            SourceFile = arguments[0];
            TargetFile = arguments[1];
            if (!File.Exists(SourceFile))
            {
                throw new ArgumentException("Source file does not exist");
            }
        }
    }
}
