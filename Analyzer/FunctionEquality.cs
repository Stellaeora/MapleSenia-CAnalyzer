using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer {
    public class FunctionEquality : IEqualityComparer<Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double>> {
        public bool Equals(Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double> x, Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double> y) {
            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            return x.Item1.Opcode == y.Item1.Opcode && x.Item2.Opcode == y.Item2.Opcode;
        }

        public int GetHashCode(Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double> x) {
            return (int)((x.Item1.Opcode + (x.Item2.Opcode * 1000)) * x.Item3);
        }
    }
}
