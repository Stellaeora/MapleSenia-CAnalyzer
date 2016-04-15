using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Analyzer {
    partial class Program {
        static void runSend(string oldCFilePath, string newCFilePath) {
            int variance;
            int equivalenceThreshold;

            Console.WriteLine("Enter the maximum allowed opcode variance, in percent (0-1000).  The default value is 25.");
            if (!Int32.TryParse(Console.ReadLine(), out variance)) {
                variance = 25;
            }

            Console.WriteLine("Enter the equivalence threshold (1-100).  Opcode guesses with a certainty below this value will be discarded.  The default value is 40.");
            if (!Int32.TryParse(Console.ReadLine(), out equivalenceThreshold)) {
                equivalenceThreshold = 40;
            }

            //Open file streams and begin parsing
            message("Opening file streams...");
            StreamReader oldStream = new StreamReader(new FileStream(oldCFilePath, FileMode.Open));
            StreamReader newStream = new StreamReader(new FileStream(newCFilePath, FileMode.Open));

            //Build lists of the functions and their full text
            message("Filestreams opened.  Building function lists...");
            List<RawFunction> oldFunctionsRaw = generateFunctionList(oldStream);
            List<RawFunction> newFunctionsRaw = generateFunctionList(newStream);
            oldStream.Close();
            newStream.Close();

            //Create maps for functions to opcodes
            Console.WriteLine("Built function lists.  Mapping opcodes...");
            
            List<string> functionRoots = new List<string>() {
                "CWvsContext__OnPacket",
                "CField__OnPacket",
                "CNpcPool__OnPacket",
                "CMobPool__OnMobPacket",
                "CUserPool__OnPacket",
                "CMinionPool__OnPacket",
                "CLogin__OnPacket",
                "CCashShop__OnPacket"
            };
            List<OpcodeMappedFunction> oldFunctions = new List<OpcodeMappedFunction>();
            List<OpcodeMappedFunction> newFunctions = new List<OpcodeMappedFunction>();

            foreach (string functionRootName in functionRoots) {
                message("Creating opcode map for " + functionRootName + "...");
                RawFunction oldRoot = oldFunctionsRaw.Find(elem => elem.Name.Contains(functionRootName));
                RawFunction newRoot = newFunctionsRaw.Find(elem => elem.Name.Contains(functionRootName));
                if (oldRoot == null || newRoot == null) {
                    string functionModifiedRoot = functionRootName.Replace('_', ':');
                    if (oldRoot == null) {
                        oldRoot = oldFunctionsRaw.Find(elem => elem.Name.Contains(functionModifiedRoot));
                    }
                    if (newRoot == null) {
                        newRoot = newFunctionsRaw.Find(elem => elem.Name.Contains(functionModifiedRoot));
                    }

                    if (oldRoot == null || newRoot == null) {
                        message("Could not find root function " + functionRootName + " in decompiled code!  Skipping...");
                        continue;
                    }
                }
                oldFunctions.AddRange(CreateOpcodeMapSend(oldRoot, oldFunctionsRaw));
                newFunctions.AddRange(CreateOpcodeMapSend(newRoot, newFunctionsRaw));
            }

            //Fundamentally, this is the same code as the recvop analyzer; 
            //It uses string similarity against all possible functions and selects the one with the highest similarity as the "match".
            message("Opcodes mapped.  Evaluating certainty, this WILL take a while...");
            List<Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double>> pairedOpcodes = CorrelateFunctions(oldFunctions, newFunctions, variance);

            //Now we have a list of opcode pairs, we can process and output the result.
            message("Successfully built equivalence map.  Cleaning up results...");
            pairedOpcodes = CleanResults(pairedOpcodes, equivalenceThreshold);

            //Generate two output files -- one detailed, and one simple.
            message("Results cleaned.  Saving to disk...");
            WriteToFile(pairedOpcodes, "send");
        }
        static int GetOpcodeFromCaseText(string text) {
            int ret = -1;
            Match hexMatch = Regex.Match(text, "0x[0-9A-Fa-f]+");
            if (hexMatch.Success) {
                ret = Int32.Parse(hexMatch.Value.Substring(2, hexMatch.Value.Length - 2), System.Globalization.NumberStyles.HexNumber);
            }
            Match decimalMatch = Regex.Match(text, "\\s[0-9]+");
            if(decimalMatch.Success){
                ret = Int32.Parse(decimalMatch.Value.Substring(1, decimalMatch.Value.Length - 1));
            }
            return ret;
        }
        static string GetOpcodeVariableName(string[] allFunctionText) {
            foreach (string line in allFunctionText) {
                Match opcodeContainingLine = Regex.Match(line, "if \\( a[1-5] == \\d{3,4} \\)");
                if (opcodeContainingLine.Success) {
                    return opcodeContainingLine.Value.Substring(5, 2);
                }
            }
            return "";
        }
        static List<OpcodeMappedFunction> CreateOpcodeMapSend(RawFunction functionRoot, List<RawFunction> allFunctions) {
            List<OpcodeMappedFunction> ret = new List<OpcodeMappedFunction>();
            string[] allLines = functionRoot.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string opcodeVariableName = GetOpcodeVariableName(allLines);

            for(int i = 0; i < allLines.Length - 1; i++) {
                string line = allLines[i];
                if (line.Contains("case ") || line.Contains("if (" + opcodeVariableName + " == ")) { //Search the root function for all case blocks; these are the functions we are interested in
                    int inc = 1;
                    bool ignore = false;
                    string functionName = GetFunctionNameFromLine(allLines[i + inc]);
                    while (functionName.Length == 0) {
                        inc++;
                        string nextLine = allLines[i + inc];
                        functionName = GetFunctionNameFromLine(nextLine);
                        if (nextLine.Contains("break;") || nextLine.Contains("}") || nextLine.Contains("return;") || 
                            nextLine.Contains("goto") || nextLine.Contains("else")) { 
                            //Reached the end of the case/if block -- give up and move on to the next entry.
                            ignore = true;
                            break;
                        }
                    }
                    if (ignore) {
                        continue;
                    }
                    int opcode = GetOpcodeFromCaseText(line);
                    if (opcode == -1) {
                        continue;
                    }

                    //grab the function body from the function name
                    ret.Add(new OpcodeMappedFunction(opcode, allFunctions.Find(elem => elem.Name.Equals(functionName))));
                }
            }

            return ret;
        }
    }
}
