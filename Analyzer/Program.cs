using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Analyzer {
    partial class Program {
        static void Main(string[] args) {

            Console.WriteLine("Automatic opcode extractor (c) 2014/2015 WLiang@MapleSenia");
            Console.WriteLine("This program takes in two files exported from IDA and produce a list of opcode changes between versions.");
            Console.WriteLine();

            Console.WriteLine("Enter the filename of the input file of the LOWER version:");
            string oldCFilename = Console.ReadLine();
            FileInfo oldCFile = new FileInfo(oldCFilename);
            while (!oldCFile.Exists) {
                Console.WriteLine("That file does not exist.  Please make sure the filename is correct.");
                Console.WriteLine("Enter the filename of the input file of the LOWER version:");
                oldCFilename = Console.ReadLine();
                oldCFile = new FileInfo(oldCFilename);
            }

            Console.WriteLine("Enter the filename of the input file of the NEWER version:");
            string newCFilename = Console.ReadLine();
            FileInfo newCFile = new FileInfo(newCFilename);
            while (!newCFile.Exists) {
                Console.WriteLine("That file does not exist.  Please make sure the filename is correct.");
                Console.WriteLine("Enter the filename of the input file of the NEWER version:");
                newCFilename = Console.ReadLine();
                newCFile = new FileInfo(newCFilename);
            }

            Console.WriteLine("Enter the mode: r to analyze recvops, s to analyze sendops, b to analyze both:");
            string mode = Console.ReadLine();
            while (mode != "r" && mode != "s" && mode != "b") {
                Console.WriteLine("That mode is not valid.");
                mode = Console.ReadLine();
            }
            switch (mode) {
                case "r":
                    runRecv(oldCFile.FullName, newCFile.FullName);
                    break;
                case "s":
                    runSend(oldCFile.FullName, newCFile.FullName);
                    break;
                case "b":
                    runRecv(oldCFile.FullName, newCFile.FullName);
                    runSend(oldCFile.FullName, newCFile.FullName);
                    break;
            }

            message("Analysis completed.  Press any key to exit...");
            Console.ReadKey(true);
        }

        static bool invalidPair(Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double> elem, int threshold) {
            /*if (elem.Item1.Opcode > (elem.Item2.Opcode + 10)) { //Opcode has gone down by a margin of more than -10 -- this usually never happens, so discard it
                return true;
            }*/
            if (elem.Item3 < threshold) { //Discard entries with less than the desired similarity
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether an opcode is within the specified variance of another opcode.
        /// </summary>
        /// <returns></returns>
        static bool isWithinVariance(int baseOpcode, int opcodeToTest, int variance) {
            //+2 and -2 leeway for low opcodes
            int upper = baseOpcode + (int)(baseOpcode * (variance / 100.0f)) + 2;
            int lower = baseOpcode - (int)(baseOpcode * (variance / 100.0f)) - 2;
            return upper >= opcodeToTest && lower <= opcodeToTest;
        }

        static List<RawFunction> generateFunctionList(StreamReader stream) {
            List<RawFunction> ret = new List<RawFunction>();
            int lineCounter = 0;
            bool EOF = false;
            while (true) {
                string currentLine = "";
                string previousLine = "";
                while (!currentLine.Contains("{")) { //go to the next function start
                    previousLine = currentLine;
                    currentLine = stream.ReadLine();
                    lineCounter++;

                    //Check for end of file
                    if (currentLine.Contains("decompilation failure(s)")) {
                        EOF = true;
                        break;
                    }
                }
                if (EOF) {
                    break;
                }
                //Reached function start, now get the function name
                string functionName = GetFunctionNameFromLine(previousLine);

                //Read the rest of the function
                StringBuilder functionData = new StringBuilder();
                int internalLineCounter = 0;
                while (stream.Peek() != '}') {
                    functionData.AppendLine(stream.ReadLine());
                    internalLineCounter++;
                }

                //Add a new function entry
                ret.Add(new RawFunction(lineCounter, functionName, previousLine, functionData.ToString()));

                //Reached end of function
                stream.ReadLine(); //skip the ending '}'
                lineCounter += internalLineCounter + 1;
            }
            return ret;
        }

        /// <summary>
        /// Determines the most plausible match for all given opcodes from one version to the next.
        /// </summary>
        /// <param name="oldFunctions">A list of mapped functions from the lower version.</param>
        /// <param name="newFunctions">A list of mapped functions from the higher version.</param>
        /// <param name="variance">The maximum allowed opcode variance in percent, from 0 to 1000.</param>
        static List<Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double>> CorrelateFunctions(List<OpcodeMappedFunction> oldFunctions, List<OpcodeMappedFunction> newFunctions, int variance) {
            List<Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double>> pairedOpcodes = new List<Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double>>();

            foreach (OpcodeMappedFunction omf in oldFunctions) {

                //First, we take each old opcode and pair it up with any new opcodes within the set variance.
                List<OpcodeMappedFunction> relevantFunctions = newFunctions.FindAll(elem => isWithinVariance(omf.Opcode, elem.Opcode, variance));

                //Now, go over each possible pair and select the one with the highest equivalence.
                int highestIndex = 0;
                double highestEquivalence = 0;
                for (int i = 0; i < relevantFunctions.Count; i++) {
                    OpcodeMappedFunction toTestAgainst = relevantFunctions[i];
                    if (omf.Function == null || toTestAgainst.Function == null) {
                        continue;
                    }
                    double equivalence = StringSimilarity.Compute(omf.Function.Text, toTestAgainst.Function.Text) * 100.0d;
                    if (equivalence > highestEquivalence) {
                        highestIndex = i;
                        highestEquivalence = equivalence;
                    }
                }
                OpcodeMappedFunction selectedMatch = relevantFunctions[highestIndex];

                //Selected the function -- mark it as a correct pair.
                Console.WriteLine("Marked opcode pair (0x" + omf.Opcode.ToString("X") + " -> 0x" + selectedMatch.Opcode.ToString("X") + ")");
                pairedOpcodes.Add(new Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double>(omf, selectedMatch, highestEquivalence));
            }
            return pairedOpcodes;
        }

        static string GetFunctionNameFromLine(string line) {
            string functionName = Regex.Match(line, "\\s[a-zA-Z0-9_:<>]+\\(").Value;
            if (functionName.Length == 0) {
                return ""; //could not find
            }
            return functionName.Substring(1, functionName.Length - 2);
        }

        static List<Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double>> CleanResults(List<Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double>> pairedOpcodes, int equivalenceThreshold) {
            //Sort the two lists
            pairedOpcodes.Sort(delegate(Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double> t1, Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double> t2) {
                if (t1.Item1.Opcode == t2.Item1.Opcode) {
                    return 0;
                } else if (t1.Item1.Opcode > t2.Item1.Opcode) {
                    return 1;
                } else {
                    return -1;
                }
            });

            //Remove incorrect pairs
            pairedOpcodes.RemoveAll(elem => invalidPair(elem, equivalenceThreshold));

            //Remove duplicate pairs
            return pairedOpcodes.Distinct(new FunctionEquality()).ToList();
        }

        static void WriteToFile(List<Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double>> pairedOpcodes, string prefix) {
            StreamWriter detailedStream = new StreamWriter(new FileStream(prefix + "_detailed.log", FileMode.Create));
            StreamWriter simpleStream = new StreamWriter(new FileStream(prefix + "_simple.log", FileMode.Create));

            foreach (Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double> result in pairedOpcodes) {
                //Detailed info -- function names, line numbers, and certainty
                StringBuilder sb = new StringBuilder();
                int diff = result.Item2.Opcode - result.Item1.Opcode;

                sb.Append("Opcode pair (");
                sb.Append(result.Item1.Opcode.ToString());
                sb.Append(" -> ");
                sb.Append(result.Item2.Opcode.ToString());
                sb.Append(")[");
                sb.Append(diff >= 0 ? "+" : "-");
                sb.Append(Math.Abs(diff));
                sb.Append("]");
                sb.Append(" with certainty ");
                sb.Append((int)result.Item3);
                sb.AppendLine("%");
                sb.Append("Old function: ");
                sb.Append(result.Item1.Function.Name);
                sb.Append(", new function ");
                sb.Append(result.Item2.Function.Name);
                sb.AppendLine();
                detailedStream.WriteLine(sb.ToString());

                sb = new StringBuilder();
                sb.Append(result.Item1.Opcode.ToString());
                sb.Append(" -> ");
                sb.Append(result.Item2.Opcode.ToString());
                sb.Append(" [");
                sb.Append(diff >= 0 ? "+" : "-");
                sb.Append(Math.Abs(diff));
                sb.Append("]");
                sb.Append(" (");
                sb.Append((int)result.Item3);
                sb.Append("%)");
                simpleStream.WriteLine(sb.ToString());
            }
            detailedStream.Close();
            simpleStream.Close();
        }

        static void message(string s) {
            Console.WriteLine();
            Console.WriteLine(s);
        }
    }
}
