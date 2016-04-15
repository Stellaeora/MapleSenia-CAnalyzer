# MapleSenia-CAnalyzer
One of the in-house utilities used for the (now defunct) MapleSenia Server Project, done in C*#*.

## Description
Used to automatically analyze and print out a list of the changed network protocol opcodes for MapleStory, from version to version given two IDA-produced databases (one for each version being compared).

Specifically, this tool implements the Levenshtein string similarity algorithm to compare the pseudocode decompiled packet handler for a given opcode to all other handlers within a certain range.  It will reduce the results of the similarity comparison into a single number representing percent similarity, which is then compared to find the best match.  This is repeated for every opcode found, and at the end of all the comparisons, the results are printed into a small plaintext file.

This tool is uploaded for archiving purposes **only**.  No new bug submissions, feature requests, patches, or pull requests will be accepted.

## Usage
The program will prompt will ask you for all of the needed information, though for more clarification:
- **Opcode Variance** (0-1000%): This is the maximum variance, in percent, that will be allowed when checking opcodes.  Higher values increase the amount of time required to perform the comparison as more text will need to be compared.  The recommended value varies depending on the version, but anywhere from 10 to 20 is generally a good value.  For large version jumps, however, you will want to use a higher value.
- **Equivalence Threshold** (1-100): When the program computes the Levenshtein similarity for any two opcodes, if the similarity (match ratio) is lower than this value, the opcode match will be discarded.  30 to 50 is the recommended value.  You can lower this to obtain more results, though at the cost of accuracy.