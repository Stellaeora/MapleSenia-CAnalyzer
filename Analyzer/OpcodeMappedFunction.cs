using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer {
    public class OpcodeMappedFunction {
        int opcode;
        RawFunction function;

        public int Opcode {
            get { return opcode; }
            set { opcode = value; }
        }
        public RawFunction Function {
            get { return function; }
            set { function = value; }
        }

        public OpcodeMappedFunction(int opcode, RawFunction raw) {
            this.opcode = opcode;
            this.function = raw;
        }
    }
}
