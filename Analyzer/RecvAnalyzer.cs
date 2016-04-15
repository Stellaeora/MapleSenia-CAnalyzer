using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Analyzer {
    partial class Program {
        //for recvops
        static void runRecv(string oldCFilePath, string newCFilePath) {
            int variance;
            int equivalenceThreshold;

            Console.WriteLine("Enter the maximum allowed opcode variance, in percent (0-1000).  The default value is 15.");
            if (!Int32.TryParse(Console.ReadLine(), out variance)) {
                variance = 15;
            }

            Console.WriteLine("Enter the equivalence threshold (1-100).  Opcode guesses with a certainty below this value will be discarded.  The default value is 30.");
            if (!Int32.TryParse(Console.ReadLine(), out equivalenceThreshold)) {
                equivalenceThreshold = 30;
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

            //Built the function list.  Now, remove any functions from the list that do not have a call to COutPacket__COutPacket_0 in them.
            message("Function lists built.  Removing extraneous functions...");
            int oldRemoved = oldFunctionsRaw.RemoveAll(checkExtraneousRecv);
            int newRemoved = newFunctionsRaw.RemoveAll(checkExtraneousRecv);

            //Map the opcodes to the functions.
            //A single function may have multiple calls to COutPacket_0 in it, which will result in multiple duplicate entries.
            //This is fine for now; we can clean them up later.
            Console.WriteLine();
            Console.WriteLine("Removed {0} extraneous functions from old list and {1} extraneous functions from new list.", oldRemoved, newRemoved);
            Console.WriteLine("Mapping opcodes...");
            List<OpcodeMappedFunction> oldFunctions = new List<OpcodeMappedFunction>();
            List<OpcodeMappedFunction> newFunctions = new List<OpcodeMappedFunction>();
            foreach (RawFunction rf in oldFunctionsRaw) {
                oldFunctions.AddRange(mapFunctionRecv(rf));
            }
            foreach (RawFunction rf in newFunctionsRaw) {
                newFunctions.AddRange(mapFunctionRecv(rf));
            }

            //Created the opcode maps, now begin using heuristics to determine certainty of change
            message("Opcodes mapped.  Evaluating certainty, this WILL take a while...");
            List<Tuple<OpcodeMappedFunction, OpcodeMappedFunction, double>> pairedOpcodes = CorrelateFunctions(oldFunctions, newFunctions, variance);

            //Now we have a list of opcode pairs, we can process and output the result.
            message("Successfully built equivalence map.  Cleaning up results...");
            pairedOpcodes = CleanResults(pairedOpcodes, equivalenceThreshold);

            //Generate two output files -- one detailed, and one simple.
            message("Results cleaned.  Saving to disk...");
            WriteToFile(pairedOpcodes, "recv");
        }

        static List<OpcodeMappedFunction> mapFunctionRecv(RawFunction rf) {
            rf.Text = trimFunctionText(rf.Text);
            List<OpcodeMappedFunction> ret = new List<OpcodeMappedFunction>();
            foreach (Match match in Regex.Matches(rf.Text, "COutPacket[_:]+COutPacket_0\\((.*, )?[0-9A-Fa-fuhdx]+\\);")) {
                string[] splittedMatch = match.Value.Split(new string[] { "(" }, StringSplitOptions.None);
                string op = splittedMatch[splittedMatch.Length - 1];
                string rawop = op.Substring(0, op.Length - 2);
                if (rawop.Contains(",")) {
                    string[] splitted = rawop.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    rawop = splitted[splitted.Length - 1];
                }
                int opcode;
                if (rawop.Contains("0x")) { //hex number, parse it as such
                    rawop = rawop.Substring(2, rawop.Length - 2);
                    if(rawop.Contains("u") || rawop.Contains("h")){
                        rawop = rawop.Substring(0, rawop.Length - 1);
                    }
                    opcode = Int32.Parse(rawop, System.Globalization.NumberStyles.HexNumber);
                } else {
                    opcode = Int32.Parse(rawop);
                }
                ret.Add(new OpcodeMappedFunction(opcode, rf));
            }
            return ret;
        }

        static bool checkExtraneousRecv(RawFunction data) {
            return !Regex.IsMatch(data.Text, "COutPacket[_:]+COutPacket_0\\((.*, )?[0-9A-Fa-fuhdx]+\\);");
        }

        //TODO account for functions with multiple calls to COutPacket__COutPacket_0
        static string trimFunctionText(string text) {
            int maxBlockSize = 1000;
            if (text.Length < (maxBlockSize * 2)) {
                return text;
            }
            int index = text.IndexOf("COutPacket__COutPacket_0");
            int index2 = text.IndexOf("COutPacket::COutPacket_0");
            if (index == -1 && index2 == -1) {
                return "";
            } else if (index == -1 && index2 != -1) {
                index = index2;
            }
            int afterChars = text.Length - index;
            int prevChars = text.Length - afterChars;
            return text.Substring(index - maxBlockSize < 0 ? 0 : index - maxBlockSize, maxBlockSize > afterChars ? afterChars : maxBlockSize);
        }
    }
}
