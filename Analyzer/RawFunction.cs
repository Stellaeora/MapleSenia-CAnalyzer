using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer {
    public class RawFunction {
        string fullName;
        string text;
        string name;
        int lineNumber;

        public RawFunction(int sourceLineNumber, string shortName, string longName, string text) {
            this.lineNumber = sourceLineNumber;
            this.text = text;
            this.fullName = longName;
            this.name = shortName;
        }

        /// <summary>
        /// The name of the function.
        /// </summary>
        public string Name {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// The fully qualified function name, including parameters and return type.
        /// </summary>
        public string FullName {
            get { return fullName; }
            set { fullName = value; }
        }
        
        /// <summary>
        /// The full plaintext of the function body.
        /// </summary>
        public string Text {
            get { return text; }
            set { text = value; }
        }
    }
}
