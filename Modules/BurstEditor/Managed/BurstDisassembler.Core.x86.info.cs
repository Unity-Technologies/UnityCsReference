// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Burst.Editor
{
    internal partial class BurstDisassembler
    {
        /// <summary>
        /// Instruction information provider for x86-64.
        /// </summary>
        internal class X86AsmInstructionInfo
        {
            internal static bool GetX86InstructionInfo(string instructionName, out string instructionInfo)
            {
                var returnValue = true;

                switch (instructionName)
                {
                    case "aaa":
                        instructionInfo = "Adjusts the sum of two unpacked BCD (Binary-Coded Decimal) values to create an unpacked BCD result." +
                                          " The AL register is the implied source and destination operand for this instruction. " +
                                          "The AAA instruction is only useful when it follows an ADD instruction that adds " +
                                          "(binary addition) two unpacked BCD values and stores a byte result in the AL register. " +
                                          "The AAA instruction then adjusts the contents of the AL register to contain " +
                                          "the correct 1-digit unpacked BCD result.";
                        break;
                    case "aad":
                        instructionInfo = "Adjusts two unpacked BCD (Binary-Coded Decimal) digits (the least-significant digit in the AL register " +
                                          "and the most-significant digit in the AH register) so that a division operation " +
                                          "performed on the result will yield a correct unpacked BCD value. The AAD instruction " +
                                          "is only useful when it precedes a DIV instruction that divides (binary division) " +
                                          "the adjusted value in the AX register by an unpacked BCD value.";
                        break;
                    case "aam":
                        instructionInfo = "Adjusts the result of the multiplication of two unpacked BCD (Binary-Coded Decimal) values to create " +
                                          "a pair of unpacked (base 10) BCD values. The AX register is the implied source " +
                                          "and destination operand for this instruction. The AAM instruction is only useful " +
                                          "when it follows an MUL instruction that multiplies (binary multiplication) " +
                                          "two unpacked BCD values and stores a word result in the AX register. " +
                                          "The AAM instruction then adjusts the contents of the AX register to contain " +
                                          "the correct 2-digit unpacked (base 10) BCD result.";
                        break;
                    case "aas":
                        instructionInfo = "Adjusts the result of the subtraction of two unpacked BCD (Binary-Coded Decimal) values to create a " +
                                          "unpacked BCD result. The AL register is the implied source and destination " +
                                          "operand for this instruction. The AAS instruction is only useful when it " +
                                          "follows a SUB instruction that subtracts (binary subtraction) one unpacked " +
                                          "BCD value from another and stores a byte result in the AL register. " +
                                          "The AAA instruction then adjusts the contents of the AL register to contain " +
                                          "the correct 1-digit unpacked BCD result.";
                        break;
                    case "adc":
                        instructionInfo = "Adds the destination operand (first operand), the source operand (second operand), " +
                                          "and the carry (CF) flag and stores the result in the destination operand. " +
                                          "The destination operand can be a register or a memory location; the source " +
                                          "operand can be an immediate, a register, or a memory location. " +
                                          "(However, two memory operands cannot be used in one instruction.) " +
                                          "The state of the CF flag represents a carry from a previous addition. " +
                                          "When an immediate value is used as an operand, it is sign-extended to the " +
                                          "length of the destination operand format.";
                        break;
                    case "adcx":
                        instructionInfo = "Performs an unsigned addition of the destination operand (first operand), " +
                                          "the source operand (second operand) and the carry-flag (CF) and stores the " +
                                          "result in the destination operand. The destination operand is a " +
                                          "general-purpose register, whereas the source operand can be a general-purpose " +
                                          "register or memory location. The state of CF can represent a carry from a " +
                                          "previous addition. The instruction sets the CF flag with the carry generated " +
                                          "by the unsigned addition of the operands.";
                        break;
                    case "add":
                        instructionInfo = "Adds the destination operand (first operand) and the source operand " +
                                          "(second operand) and then stores the result in the destination operand. " +
                                          "The destination operand can be a register or a memory location; the source " +
                                          "operand can be an immediate, a register, or a memory location. " +
                                          "(However, two memory operands cannot be used in one instruction.) When an " +
                                          "immediate value is used as an operand, it is sign-extended to the length of " +
                                          "the destination operand format.";
                        break;
                    case "addpd":
                    case "vaddpd":
                        instructionInfo = "Add two, four or eight packed double-precision floating-point values from the " +
                                          "first source operand to the second source operand, and stores the " +
                                          "packed double-precision floating-point results in the destination operand.";
                        break;
                    case "addps":
                    case "vaddps":
                        instructionInfo = "Add four, eight or sixteen packed single-precision floating-point values " +
                                          "from the first source operand with the second source operand, and stores the " +
                                          "packed single-precision floating-point results in the destination operand.";
                        break;
                    case "addsd":
                    case "vaddsd":
                        instructionInfo = "Adds the low double-precision floating-point values from the second source " +
                                          "operand and the first source operand and stores the double-precision " +
                                          "floating-point result in the destination operand.";
                        break;
                    case "addss":
                    case "vaddss":
                        instructionInfo = "Adds the low single-precision floating-point values from the second source " +
                                          "operand and the first source operand, and stores the double-precision " +
                                          "floating-point result in the destination operand.";
                        break;
                    case "addsubpd":
                    case "vaddsubpd":
                        instructionInfo = "Adds odd-numbered double-precision floating-point values of the first source " +
                                          "operand (second operand) with the corresponding double-precision floating-point " +
                                          "values from the second source operand (third operand); stores the result in " +
                                          "the odd-numbered values of the destination operand (first operand). " +
                                          "Subtracts the even-numbered double-precision floating-point values from the " +
                                          "second source operand from the corresponding double-precision floating values " +
                                          "in the first source operand; stores the result into the even-numbered values " +
                                          "of the destination operand.";
                        break;
                    case "addsubps":
                    case "vaddsubps":
                        instructionInfo = "Adds odd-numbered single-precision floating-point values of the first source " +
                                          "operand (second operand) with the corresponding single-precision floating-point " +
                                          "values from the second source operand (third operand); stores the result in " +
                                          "the odd-numbered values of the destination operand (first operand). " +
                                          "Subtracts the even-numbered single-precision floating-point values from the " +
                                          "second source operand from the corresponding single-precision floating " +
                                          "values in the first source operand; stores the result into the even-numbered " +
                                          "values of the destination operand.";
                        break;
                    case "adox":
                        instructionInfo = "Performs an unsigned addition of the destination operand (first operand), " +
                                          "the source operand (second operand) and the overflow-flag (OF) and stores " +
                                          "the result in the destination operand. The destination operand is a " +
                                          "general-purpose register, whereas the source operand can be a " +
                                          "general-purpose register or memory location. The state of OF represents " +
                                          "a carry from a previous addition. The instruction sets the OF flag with " +
                                          "the carry generated by the unsigned addition of the operands.";
                        break;
                    case "aesdec":
                    case "vaesdec":
                        instructionInfo = "This instruction performs a single round of the AES decryption flow using " +
                                          "the Equivalent Inverse Cipher, with the round key from the second source " +
                                          "operand, operating on a 128-bit data (state) from the first source operand, " +
                                          "and store the result in the destination operand.";
                        break;
                    case "aesdeclast":
                    case "vaesdeclast":
                        instructionInfo = "This instruction performs the last round of the AES decryption flow using " +
                                          "the Equivalent Inverse Cipher, with the round key from the second source " +
                                          "operand, operating on a 128-bit data (state) from the first source operand, " +
                                          "and store the result in the destination operand.";
                        break;
                    case "aesenc":
                    case "vaesenc":
                        instructionInfo = "This instruction performs a single round of an AES encryption flow using a " +
                                          "round key from the second source operand, operating on 128-bit data (state) " +
                                          "from the first source operand, and store the result in the destination operand.";
                        break;
                    case "aesenclast":
                    case "vaesenclast":
                        instructionInfo = "This instruction performs the last round of an AES encryption flow using a " +
                                          "round key from the second source operand, operating on 128-bit data (state) " +
                                          "from the first source operand, and store the result in the destination operand.";
                        break;
                    case "aesimc":
                    case "vaesimc":
                        instructionInfo = "Perform the InvMixColumns transformation on the source operand and store " +
                                          "the result in the destination operand. The destination operand is an XMM register. " +
                                          "The source operand can be an XMM register or a 128-bit memory location.";
                        break;
                    case "aeskeygenassist":
                    case "vaeskeygenassist":
                        instructionInfo = "Assist in expanding the AES cipher key, by computing steps towards generating " +
                                          "a round key for encryption, using 128-bit data specified in the source operand " +
                                          "and an 8-bit round constant specified as an immediate, store the result in the " +
                                          "destination operand.";
                        break;
                    case "and":
                        instructionInfo = "Performs a bitwise AND operation on the destination (first) and source " +
                                          "(second) operands and stores the result in the destination operand location. " +
                                          "The source operand can be an immediate, a register, or a memory location; " +
                                          "the destination operand can be a register or a memory location. " +
                                          "(However, two memory operands cannot be used in one instruction.) " +
                                          "Each bit of the result is set to 1 if both corresponding bits of the first and " +
                                          "second operands are 1; otherwise, it is set to 0.";
                        break;
                    case "andn":
                        instructionInfo = "Performs a bitwise logical AND of inverted second operand " +
                                          "(the first source operand) with the third operand (the";
                        break;
                    case "andnpd":
                    case "vandnpd":
                        instructionInfo = "Performs a bitwise logical AND NOT of the two, four or eight packed " +
                                          "double-precision floating-point values from the first source operand and " +
                                          "the second source operand, and stores the result in the destination operand.";
                        break;
                    case "andnps":
                    case "vandnps":
                        instructionInfo = "Performs a bitwise logical AND NOT of the four, eight or sixteen packed " +
                                          "single-precision floating-point values from the first source operand and " +
                                          "the second source operand, and stores the result in the destination operand.";
                        break;
                    case "andpd":
                    case "vandpd":
                        instructionInfo = "Performs a bitwise logical AND of the two, four or eight packed double-precision " +
                                          "floating-point values from the first source operand and the second source " +
                                          "operand, and stores the result in the destination operand.";
                        break;
                    case "andps":
                    case "vandps":
                        instructionInfo = "Performs a bitwise logical AND of the four, eight or sixteen packed " +
                                          "single-precision floating-point values from the first source operand and " +
                                          "the second source operand, and stores the result in the destination operand.";
                        break;
                    case "arpl":
                        instructionInfo = "Compares the RPL (Requester Privilege Level) fields of two segment selectors. The first operand " +
                                          "(the destination operand) contains one segment selector and the second operand " +
                                          "(source operand) contains the other. If the RPL field of the destination operand is less than the " +
                                          "RPL field of the source operand, the ZF flag is set and the RPL field of the " +
                                          "destination operand is increased to match that of the source operand. " +
                                          "Otherwise, the ZF flag is cleared and no change is made to the destination " +
                                          "operand. (The destination operand can be a word register or a memory location; " +
                                          "the source operand must be a word register.)";
                        break;
                    case "bextr":
                        instructionInfo = "Extracts contiguous bits from the first source operand (the second operand) " +
                                          "using an index value and length value specified in the second source operand " +
                                          "(the third operand). Bit 7:0 of the second source operand specifies the " +
                                          "starting bit position of bit extraction. Bit 15:8 of " +
                                          "the second source operand specifies the maximum number of bits (LENGTH) " +
                                          "beginning at the START position to extract. Only bit positions up to " +
                                          "(OperandSize -1) of the first source operand are extracted. The extracted " +
                                          "bits are written to the destination register, starting from the least " +
                                          "significant bit. All higher order bits in the destination operand " +
                                          "(starting at bit position LENGTH) are zeroed. The destination register is " +
                                          "cleared if no bits are extracted.";
                        break;
                    case "blendpd":
                    case "vblendpd":
                        instructionInfo = "Double-precision floating-point values from the second source operand " +
                                          "(third operand) are conditionally merged with values from the first source " +
                                          "operand (second operand) and written to the destination operand (first operand). " +
                                          "The immediate bits [3:0] determine whether the corresponding double-precision " +
                                          "floating-point value in the destination is copied from the second source or " +
                                          "first source. If a bit in the mask, corresponding to a word, is " +
                                          "\"1\", then the double-precision floating-point value in " +
                                          "the second source operand is copied, else the value in the first source operand is copied.";
                        break;
                    case "blendps":
                    case "vblendps":
                        instructionInfo = "Packed single-precision floating-point values from the second source operand " +
                                          "(third operand) are conditionally merged with values from the first source " +
                                          "operand (second operand) and written to the destination operand (first operand). " +
                                          "The immediate bits [7:0] determine whether the corresponding single precision " +
                                          "floating-point value in the destination is copied from the second source or " +
                                          "first source. If a bit in the mask, corresponding to a word, is \"1\", " +
                                          "then the single-precision floating-point value in the second source operand " +
                                          "is copied, else the value in the first source operand is copied.";
                        break;
                    case "blendvpd":
                    case "vblendvpd":
                        instructionInfo = "Conditionally copy each quadword data element of double-precision " +
                                          "floating-point value from the second source operand and the first source " +
                                          "operand depending on mask bits defined in the mask register operand. " +
                                          "The mask bits are the most significant bit in each quadword element of the mask register.";
                        break;
                    case "blendvps":
                    case "vblendvps":
                        instructionInfo = "Conditionally copy each dword data element of single-precision floating-point " +
                                          "value from the second source operand and the first source operand depending " +
                                          "on mask bits defined in the mask register operand. The mask bits are the most " +
                                          "significant bit in each dword element of the mask register.";
                        break;
                    case "blsi":
                        instructionInfo = "Extracts the lowest set bit from the source operand and set the corresponding " +
                                          "bit in the destination register. All other bits in the destination operand " +
                                          "are zeroed. If no bits are set in the source operand, BLSI sets all the bits " +
                                          "in the destination to 0 and sets ZF and CF.";
                        break;
                    case "blsmsk":
                        instructionInfo = "Sets all the lower bits of the destination operand to \"1\" up to " +
                                          "and including lowest set bit (=1) in the source operand. If source operand is " +
                                          "zero, BLSMSK sets all bits of the destination operand to 1 and also sets CF to 1.";
                        break;
                    case "blsr":
                        instructionInfo = "Copies all bits from the source operand to the destination operand and resets " +
                                          "(=0) the bit position in the destination operand that corresponds to the lowest " +
                                          "set bit of the source operand. If the source operand is zero BLSR sets CF.";
                        break;
                    case "bndcl":
                        instructionInfo = "Compare the address in the second operand with the lower bound in bnd. " +
                                          "The second operand can be either a register or memory operand. If the address " +
                                          "is lower than the lower bound in bnd.LB, it will set BNDSTATUS to 01H and " +
                                          "signal a #BR exception.";
                        break;
                    case "bndcu":
                    case "bndcn":
                        instructionInfo = "Compare the address in the second operand with the upper bound in bnd. " +
                                          "The second operand can be either a register or a memory operand. If the " +
                                          "address is higher than the upper bound in bnd.UB, it will set BNDSTATUS to " +
                                          "01H and signal a #BR exception.";
                        break;
                    case "bndldx":
                        instructionInfo = "BNDLDX uses the linear address constructed from the base register and " +
                                          "displacement of the SIB-addressing form of the memory operand (mib) to " +
                                          "perform address translation to access a bound table entry and conditionally " +
                                          "load the bounds in the BTE to the destination. The destination register is " +
                                          "updated with the bounds in the BTE, if the content of the index register of " +
                                          "mib matches the pointer value stored in the BTE.";
                        break;
                    case "bndmk":
                        instructionInfo = "Makes bounds from the second operand and stores the lower and upper bounds in " +
                                          "the bound register bnd. The second operand must be a memory operand. " +
                                          "The content of the base register from the memory operand is stored in the " +
                                          "lower bound bnd.LB. The 1\'s complement of the effective address of m32/m64 " +
                                          "is stored in the upper bound b.UB. Computation of m32/m64 has identical behavior to LEA.";
                        break;
                    case "bndmov":
                        instructionInfo = "BNDMOV moves a pair of lower and upper bound values from the source operand " +
                                          "(the second operand) to the destination (the first operand). " +
                                          "Each operation is 128-bit move. The exceptions are same as the MOV instruction.";
                        break;
                    case "bndstx":
                        instructionInfo = "BNDSTX uses the linear address constructed from the displacement and base " +
                                          "register of the SIB-addressing form of the memory operand (mib) to perform " +
                                          "address translation to store to a bound table entry. The bounds in the source " +
                                          "operand bnd are written to the lower and upper bounds in the BTE. " +
                                          "The content of the index register of mib is written to the pointer value field in the BTE.";
                        break;
                    case "bound":
                        instructionInfo = "BOUND determines if the first operand (array index) is within the bounds of " +
                                          "an array specified the second operand (bounds operand). The array index is " +
                                          "a signed integer located in a register. The bounds operand is a memory " +
                                          "location that contains a pair of signed doubleword-integers " +
                                          "(when the operand-size attribute is 32) or a pair of signed word-integers " +
                                          "(when the operand-size attribute is 16). If the index is not within bounds, " +
                                          "a BOUND range exceeded exception " +
                                          "(#BR) is signaled. When this exception is generated, the saved return " +
                                          "instruction pointer points to the BOUND instruction.";
                        break;
                    case "bsf":
                        instructionInfo = "Searches the source operand (second operand) for the least significant set bit " +
                                          "(1 bit). If a least significant 1 bit is found, its bit index is stored in " +
                                          "the destination operand (first operand). The source operand can be a register " +
                                          "or a memory location; the destination operand is a register. The bit index " +
                                          "is an unsigned offset from bit 0 of the source operand. If the content of the " +
                                          "source operand is 0, the content of the destination operand is undefined.";
                        break;
                    case "bsr":
                        instructionInfo = "Searches the source operand (second operand) for the most significant set " +
                                          "bit (1 bit). If a most significant 1 bit is found, its bit index is stored " +
                                          "in the destination operand (first operand). The source operand can be a " +
                                          "register or a memory location; the destination operand is a register. The " +
                                          "bit index is an unsigned offset from bit 0 of the source operand. If the " +
                                          "content source operand is 0, the content of the destination operand is undefined.";
                        break;
                    case "bswap":
                        instructionInfo = "Reverses the byte order of a 32-bit or 64-bit (destination) register. " +
                                          "This instruction is provided for converting little-endian values to big-endian " +
                                          "format and vice versa. To swap bytes in a word value (16-bit register), " +
                                          "use the XCHG instruction. When the BSWAP instruction references a 16-bit " +
                                          "register, the result is undefined.";
                        break;
                    case "bt":
                        instructionInfo = "Selects the bit in a bit string (specified with the first operand, " +
                                          "called the bit base) at the bit-position designated by the bit offset " +
                                          "(specified by the second operand) and stores the value of the bit in the " +
                                          "CF flag. The bit base operand can be a register or a memory location; " +
                                          "the bit offset operand can be a register or an immediate value:";
                        break;
                    case "btc":
                        instructionInfo = "Selects the bit in a bit string (specified with the first operand, called " +
                                          "the bit base) at the bit-position designated by the bit offset operand " +
                                          "(second operand), stores the value of the bit in the CF flag, and complements " +
                                          "the selected bit in the bit string. The bit base operand can be a register " +
                                          "or a memory location; the bit offset operand can be a register or an immediate value:";
                        break;
                    case "btr":
                        instructionInfo = "Selects the bit in a bit string (specified with the first operand, " +
                                          "called the bit base) at the bit-position designated by the bit offset operand " +
                                          "(second operand), stores the value of the bit in the CF flag, and clears the " +
                                          "selected bit in the bit string to 0. The bit base operand can be a register " +
                                          "or a memory location; the bit offset operand can be a register or an immediate value:";
                        break;
                    case "bts":
                        instructionInfo = "Selects the bit in a bit string (specified with the first operand, " +
                                          "called the bit base) at the bit-position designated by the bit offset " +
                                          "operand (second operand), stores the value of the bit in the CF flag, and " +
                                          "sets the selected bit in the bit string to 1. The bit base operand can be " +
                                          "a register or a memory location; the bit offset operand can be a register " +
                                          "or an immediate value:";
                        break;
                    case "bzhi":
                        instructionInfo = "BZHI copies the bits of the first source operand (the second operand) into " +
                                          "the destination operand (the first operand) and clears the higher bits in " +
                                          "the destination according to the INDEX value specified by the second source " +
                                          "operand (the third operand). The INDEX is specified by bits 7:0 of the " +
                                          "second source operand. The INDEX value is saturated at the value of " +
                                          "OperandSize -1. CF is set, if the number contained in the 8 low bits of " +
                                          "the third operand is greater than OperandSize -1.";
                        break;
                    case "call":
                        instructionInfo = "Saves procedure linking information on the stack and branches to the called " +
                                          "procedure specified using the target operand. The target operand specifies " +
                                          "the address of the first instruction in the called procedure. The operand can " +
                                          "be an immediate value, a general-purpose register, or a memory location.";
                        break;
                    case "cbw":
                    case "cwde":
                    case "cdqe":
                        instructionInfo = "Double the size of the source operand by means of sign extension. The CBW " +
                                          "(convert byte to word) instruction copies the sign (bit 7) in the source " +
                                          "operand into every bit in the AH register. The CWDE (convert word to " +
                                          "double-word) instruction copies the sign (bit 15) of the word in the AX " +
                                          "register into the high 16 bits of the EAX register.";
                        break;
                    case "cwd":
                    case "cdq":
                    case "cqo":
                        instructionInfo = "Doubles the size of the operand in register AX, EAX, or RAX " +
                                          "(depending on the operand size) by means of sign extension and stores " +
                                          "the result in registers DX:AX, EDX:EAX, or RDX:RAX, respectively. " +
                                          "The CWD instruction copies the sign (bit 15) of the value in the AX " +
                                          "register into every bit position in the DX register. The CDQ instruction " +
                                          "copies the sign (bit 31) of the value in the EAX register into every bit " +
                                          "position in the EDX register.";
                        break;
                    case "clac":
                        instructionInfo = "Clears the AC flag bit in EFLAGS register. This disables any alignment " +
                                          "checking of user-mode data accesses. If the SMAP bit is set in the CR4 " +
                                          "register, this disallows explicit supervisor-mode data accesses to user-mode pages.";
                        break;
                    case "clc":
                        instructionInfo = "Clears the CF flag in the EFLAGS register. Operation is the same in all modes.";
                        break;
                    case "cld":
                        instructionInfo = "Clears the DF flag in the EFLAGS register. When the DF flag is set to 0, " +
                                          "string operations increment the index registers (ESI and/or EDI). " +
                                          "Operation is the same in all modes.";
                        break;
                    case "cldemote":
                        instructionInfo = "Hints to hardware that the cache line that contains the linear address " +
                                          "specified with the memory operand should be moved (\"demoted\") " +
                                          "from the cache(s) closest to the processor core to a level more distant " +
                                          "from the processor core. This may accelerate subsequent accesses to the " +
                                          "line by other cores in the same coherence domain, especially if the line " +
                                          "was written by the core that demotes the line. Moving the line in such a " +
                                          "manner is a performance optimization, i.e., it is a hint which does not " +
                                          "modify architectural state. Hardware may choose which level in the cache " +
                                          "hierarchy to retain the line (e.g., L3 in typical server designs). " +
                                          "The source operand is a byte memory location.";
                        break;
                    case "clflush":
                        instructionInfo = "Invalidates from every level of the cache hierarchy in the cache " +
                                          "coherence domain the cache line that contains the linear address " +
                                          "specified with the memory operand. If that cache line contains " +
                                          "modified data at any level of the cache hierarchy, that data is " +
                                          "written back to memory. The source operand is a byte memory location.";
                        break;
                    case "clflushopt":
                        instructionInfo = "Invalidates from every level of the cache hierarchy in the cache " +
                                          "coherence domain the cache line that contains the linear address " +
                                          "specified with the memory operand. If that cache line contains " +
                                          "modified data at any level of the cache hierarchy, that data is " +
                                          "written back to memory. The source operand is a byte memory location.";
                        break;
                    case "cli":
                        instructionInfo = "In most cases, CLI clears the IF flag in the EFLAGS register and no other " +
                                          "flags are affected. Clearing the IF flag causes the processor to ignore " +
                                          "maskable external interrupts. The IF flag and the CLI and STI instruction " +
                                          "have no effect on the generation of exceptions and NMI interrupts.";
                        break;
                    case "clts":
                        instructionInfo = "Clears the task-switched (TS) flag in the CR0 register. This instruction " +
                                          "is intended for use in operating-system procedures. It is a privileged " +
                                          "instruction that can only be executed at a CPL of 0. It is allowed to be " +
                                          "executed in real-address mode to allow initialization for protected mode.";
                        break;
                    case "clwb":
                        instructionInfo = "Writes back to memory the cache line (if modified) that contains the " +
                                          "linear address specified with the memory operand from any level of the " +
                                          "cache hierarchy in the cache coherence domain. The line may be retained " +
                                          "in the cache hierarchy in non-modified state.";
                        break;
                    case "cmc":
                        instructionInfo = "Complements the CF flag in the EFLAGS register. CMC operation is the same " +
                                          "in non-64-bit modes and 64-bit mode.";
                        break;
                    case "cmova":
                    case "cmovae":
                    case "cmovb":
                    case "cmovbe":
                    case "cmovc":
                    case "cmove":
                    case "cmovg":
                    case "cmovge":
                    case "cmovl":
                    case "cmovle":
                    case "cmovna":
                    case "cmovnae":
                    case "cmovnb":
                    case "cmovnbe":
                    case "cmovnc":
                    case "cmovne":
                    case "cmovng":
                    case "cmovnge":
                    case "cmovnl":
                    case "cmovnle":
                    case "cmovno":
                    case "cmovnp":
                    case "cmovns":
                    case "cmovnz":
                    case "cmovo":
                    case "cmovp":
                    case "cmovpe":
                    case "cmovpo":
                    case "cmovs":
                    case "cmovz":
                        instructionInfo = "The CMOVcc instructions check the state of one or more of the " +
                                          "status flags in the EFLAGS register (CF, OF, PF, SF, and ZF) and perform " +
                                          "a move operation if the flags are in a specified state (or condition). " +
                                          "A condition code (cc) is associated with each instruction to " +
                                          "indicate the condition being tested for. If the condition is not satisfied, " +
                                          "a move is not performed and execution continues with the instruction " +
                                          "following the CMOVcc instruction.";
                        break;
                    case "cmp":
                        instructionInfo = "Compares the first source operand with the second source operand and " +
                                          "sets the status flags in the EFLAGS register according to the results. " +
                                          "The comparison is performed by subtracting the second operand from the " +
                                          "first operand and then setting the status flags in the same manner as " +
                                          "the SUB instruction. When an immediate value is used as an operand, it " +
                                          "is sign-extended to the length of the first operand.";
                        break;
                    case "cmppd":
                        instructionInfo = "Performs a SIMD compare of the packed double-precision floating-point " +
                                          "values in the second source operand and the first source operand and " +
                                          "returns the results of the comparison to the destination operand. " +
                                          "The comparison predicate operand (immediate byte) specifies the type " +
                                          "of comparison performed on each pair of packed values in the two source " +
                                          "operands. Uses 3 bits for comparison predicate.";
                        break;
                    case "cmpeqpd":
                        instructionInfo = "Performs a SIMD compare equal of the packed double-precision floating-point " +
                                          "values in the second source operand and the first source operand and " +
                                          "returns the results of the comparison to the destination operand.";
                        break;
                    case "cmpltpd":
                        instructionInfo = "Performs a SIMD compare less than of the packed double-precision " +
                                          "floating-point values in the second source operand and the first source " +
                                          "operand and returns the results of the comparison to the destination operand.";
                        break;
                    case "cmplepd":
                        instructionInfo = "Performs a SIMD compare less or equal of the packed double-precision " +
                                          "floating-point values in the second source operand and the first source " +
                                          "operand and returns the results of the comparison to the destination operand.";
                        break;
                    case "cmpunordpd":
                        instructionInfo = "Performs a SIMD compare unordered of the packed double-precision " +
                                          "floating-point values in the second source operand and the first source " +
                                          "operand and returns the results of the comparison to the destination operand.";
                        break;
                    case "cmpneqpd":
                        instructionInfo = "Performs a SIMD compare not equal of the packed double-precision " +
                                          "floating-point values in the second source operand and the first source " +
                                          "operand and returns the results of the comparison to the destination operand.";
                        break;
                    case "cmpnltpd":
                        instructionInfo = "Performs a SIMD compare not less than of the packed double-precision " +
                                          "floating-point values in the second source operand and the first source " +
                                          "operand and returns the results of the comparison to the destination operand.";
                        break;
                    case "cmpnlepd":
                        instructionInfo = "Performs a SIMD compare not less than or equal of the packed " +
                                          "double-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "cmpordpd":
                        instructionInfo = "Performs a SIMD compare orderd of the packed double-precision " +
                                          "floating-point values in the second source operand and the first source " +
                                          "operand and returns the results of the comparison to the destination operand.";
                        break;
                    case "vcmppd":
                        instructionInfo = "Performs a SIMD compare of the packed double-precision floating-point " +
                                          "values in the second source operand and the first source operand and " +
                                          "returns the results of the comparison to the destination operand. " +
                                          "The comparison predicate operand (immediate byte) specifies the type of " +
                                          "comparison performed on each pair of packed values in the two source operands. " +
                                          "Using 5 bits for comparison predicate.";
                        break;
                    case "vcmpeqpd":
                        instructionInfo = "Performs a SIMD equal (ordered, non-signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "vcmpltpd":
                        instructionInfo = "Performs a SIMD less-than (ordered, signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "vcmplepd":
                        instructionInfo = "Performs a SIMD less-than-or-equal (ordered, signaling) compare of the " +
                                          "packed double-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpunordpd":
                        instructionInfo = "Performs a SIMD unordered (non-signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "vcmpneqpd":
                        instructionInfo = "Performs a SIMD not-equal (unordered, non-signaling) compare of the " +
                                          "packed double-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpnltpd":
                        instructionInfo = "Performs a SIMD not-less-than (unordered, signaling) compare of the " +
                                          "packed double-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpnlepd":
                        instructionInfo = "Performs a SIMD not-less-than-or-equal (unordered, signaling) compare of " +
                                          "the packed double-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpordpd":
                        instructionInfo = "Performs a SIMD ordered (non-signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "vcmpeq_uqpd":
                        instructionInfo = "Performs a SIMD equal (unordered, non-signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "vcmpngepd":
                        instructionInfo = "Performs a SIMD not-greater-than-or-equal (unordered, signaling) compare " +
                                          "of the packed double-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpngtpd":
                        instructionInfo = "Performs a SIMD not-greater-than (unordered, signaling) compare of the " +
                                          "packed double-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpfalsepd":
                        instructionInfo = "Performs a SIMD false (ordered, non-signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpneq_oqpd":
                        instructionInfo = "Performs a SIMD not-equal (ordered, non-signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "vcmpgepd":
                        instructionInfo = "Performs a SIMD greater-than-or-equal (ordered, signaling) compare of the " +
                                          "packed double-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpgtpd":
                        instructionInfo = "Performs a SIMD greater-than (ordered, signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "vcmptruepd":
                        instructionInfo = "Performs a SIMD true (unordered, non-signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "vcmpeq_ospd":
                        instructionInfo = "Performs a SIMD equal (ordered, signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "vcmplt_oqpd":
                        instructionInfo = "Performs a SIMD less-than (ordered, nonsignaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "vcmple_oqpd":
                        instructionInfo = "Performs a SIMD less-than-or-equal (ordered, nonsignaling) compare of the " +
                                          "packed double-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpunord_spd":
                        instructionInfo = "Performs a SIMD unordered (signaling) compare of the packed double-precision " +
                                          "floating-point values in the second source operand and the first source " +
                                          "operand and returns the results of the comparison to the destination operand.";
                        break;
                    case "vcmpneq_uspd":
                        instructionInfo = "Performs a SIMD not-equal (unordered, signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "vcmpnlt_uqpd":
                        instructionInfo = "Performs a SIMD not-less-than (unordered, nonsignaling) compare of the " +
                                          "packed double-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpnle_uqpd":
                        instructionInfo = "Performs a SIMD not-less-than-or-equal (unordered, nonsignaling) compare " +
                                          "of the packed double-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpord_spd":
                        instructionInfo = "Performs a SIMD ordered (signaling) compare of the packed double-precision " +
                                          "floating-point values in the second source operand and the first source " +
                                          "operand and returns the results of the comparison to the destination operand.";
                        break;
                    case "vcmpeq_uspd":
                        instructionInfo = "Performs a SIMD equal (unordered, signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpnge_uqpd":
                        instructionInfo = "Performs a SIMD not-greater-than-or-equal (unordered, non-signaling) " +
                                          "compare of the packed double-precision floating-point values in the second " +
                                          "source operand and the first source operand and returns the results of " +
                                          "the comparison to the destination operand.";
                        break;
                    case "vcmpngt_uqpd":
                        instructionInfo = "Performs a SIMD not-greater-than (unordered, nonsignaling) compare of the " +
                                          "packed double-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpfalse_ospd":
                        instructionInfo = "Performs a SIMD false (ordered, signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "vcmpneq_ospd":
                        instructionInfo = "Performs a SIMD not-equal (ordered, signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpge_oqpd":
                        instructionInfo = "Performs a SIMD greater-than-or-equal (ordered, nonsignaling) compare of " +
                                          "the packed double-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpgt_oqpd":
                        instructionInfo = "Performs a SIMD greater-than (ordered, nonsignaling) compare of the " +
                                          "packed double-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "vcmptrue_usp":
                        instructionInfo = "Performs a SIMD true (unordered, signaling) compare of the packed " +
                                          "double-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "cmpps":
                        instructionInfo = "Performs a SIMD compare of the packed single-precision floating-point " +
                                          "values in the second source operand and the first source operand and " +
                                          "returns the results of the comparison to the destination operand. " +
                                          "The comparison predicate operand (immediate byte) specifies the type " +
                                          "of comparison performed on each of the pairs of packed values. Uses 3 " +
                                          "bit for comparison predicate.";
                        break;
                    case "cmpeqps":
                        instructionInfo = "Performs a SIMD equal (ordered, non-signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "cmpltps":
                        instructionInfo = "Performs a SIMD less-than (ordered, signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "cmpleps":
                        instructionInfo = "Performs a SIMD less-than-or-equal (ordered, signaling) compare of the " +
                                          "packed single-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "cmpunordps":
                        instructionInfo = "Performs a SIMD unordered (non-signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "cmpneqps":
                        instructionInfo = "Performs a SIMD not-equal (unordered, non-signaling) compare of the " +
                                          "packed single-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "cmpnltps":
                        instructionInfo = "Performs a SIMD not-less-than (unordered, signaling) compare of the " +
                                          "packed single-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "cmpnleps":
                        instructionInfo = "Performs a SIMD not-less-than-or-equal (unordered, signaling) compare " +
                                          "of the packed single-precision floating-point values in the second " +
                                          "source operand and the first source operand and returns the results of " +
                                          "the comparison to the destination operand.";
                        break;
                    case "cmpordps":
                        instructionInfo = "Performs a SIMD ordered (non-signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "vcmpps":
                        instructionInfo = "Performs a SIMD compare of the packed single-precision floating-point " +
                                          "values in the second source operand and the first source operand and " +
                                          "returns the results of the comparison to the destination operand. The " +
                                          "comparison predicate operand (immediate byte) specifies the type of " +
                                          "comparison performed on each of the pairs of packed values. Uses 5 bits " +
                                          "for comparison predicate.";
                        break;
                    case "vcmpeqps":
                        instructionInfo = "Performs a SIMD equal (ordered, non-signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpltps":
                        instructionInfo = "Performs a SIMD less-than (ordered, signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "vcmpleps":
                        instructionInfo = "Performs a SIMD less-than-or-equal (ordered, signaling) compare of the " +
                                          "packed single-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpunordps":
                        instructionInfo = "Performs a SIMD unordered (non-signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "vcmpneqps":
                        instructionInfo = "Performs a SIMD not-equal (unordered, non-signaling) compare of the " +
                                          "packed single-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpnltps":
                        instructionInfo = "Performs a SIMD not-less-than (unordered, signaling) compare of the " +
                                          "packed single-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpnleps":
                        instructionInfo = "Performs a SIMD not-less-than-or-equal (unordered, signaling) compare " +
                                          "of the packed single-precision floating-point values in the second " +
                                          "source operand and the first source operand and returns the results of " +
                                          "the comparison to the destination operand.";
                        break;
                    case "vcmpordps":
                        instructionInfo = "Performs a SIMD ordered (non-signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "vcmpeq_uqps":
                        instructionInfo = "Performs a SIMD equal (unordered, non-signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpngeps":
                        instructionInfo = "Performs a SIMD not-greater-than-or-equal (unordered, signaling) compare " +
                                          "of the packed single-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpngtps":
                        instructionInfo = "Performs a SIMD not-greater-than (unordered, signaling) compare of the " +
                                          "packed single-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpfalseps":
                        instructionInfo = "Performs a SIMD false (ordered, non-signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "vcmpneq_oqps":
                        instructionInfo = "Performs a SIMD not-equal (ordered, non-signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "vcmpgeps":
                        instructionInfo = "Performs a SIMD greater-than-or-equal (ordered, signaling) compare of " +
                                          "the packed single-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpgtps":
                        instructionInfo = "Performs a SIMD greater-than (ordered, signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmptrueps":
                        instructionInfo = "Performs a SIMD true (unordered, non-signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpeq_osps":
                        instructionInfo = "Performs a SIMD equal (ordered, signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "vcmplt_oqps":
                        instructionInfo = "Performs a SIMD less-than (ordered, nonsignaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmple_oqps":
                        instructionInfo = "Performs a SIMD less-than-or-equal (ordered, nonsignaling) compare of " +
                                          "the packed single-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpunord_sps":
                        instructionInfo = "Performs a SIMD unordered (signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpneq_usps":
                        instructionInfo = "Performs a SIMD not-equal (unordered, signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpnlt_uqps":
                        instructionInfo = "Performs a SIMD not-less-than (unordered, nonsignaling) compare of the " +
                                          "packed single-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpnle_uqps":
                        instructionInfo = "Performs a SIMD not-less-than-or-equal (unordered, nonsignaling) compare " +
                                          "of the packed single-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpord_sps":
                        instructionInfo = "Performs a SIMD ordered (signaling) compare of the packed single-precision " +
                                          "floating-point values in the second source operand and the first source " +
                                          "operand and returns the results of the comparison to the destination operand.";
                        break;
                    case "vcmpeq_usps":
                        instructionInfo = "Performs a SIMD equal (unordered, signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "vcmpnge_uqps":
                        instructionInfo = "Performs a SIMD not-greater-than-or-equal (unordered, non-signaling) " +
                                          "compare of the packed single-precision floating-point values in the " +
                                          "second source operand and the first source operand and returns the " +
                                          "results of the comparison to the destination operand.";
                        break;
                    case "vcmpngt_uqps":
                        instructionInfo = "Performs a SIMD not-greater-than (unordered, nonsignaling) compare of the " +
                                          "packed single-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpfalse_osps":
                        instructionInfo = "Performs a SIMD false (ordered, signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to " +
                                          "the destination operand.";
                        break;
                    case "vcmpneq_osps":
                        instructionInfo = "Performs a SIMD not-equal (ordered, signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison " +
                                          "to the destination operand.";
                        break;
                    case "vcmpge_oqps":
                        instructionInfo = "Performs a SIMD greater-than-or-equal (ordered, nonsignaling) compare of " +
                                          "the packed single-precision floating-point values in the second source " +
                                          "operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand.";
                        break;
                    case "vcmpgt_oqps":
                        instructionInfo = "Performs a SIMD greater-than (ordered, nonsignaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "vcmptrue_uss":
                        instructionInfo = "Performs a SIMD true (unordered, signaling) compare of the packed " +
                                          "single-precision floating-point values in the second source operand and " +
                                          "the first source operand and returns the results of the comparison to the " +
                                          "destination operand.";
                        break;
                    case "cmps":
                    case "cmpsb":
                    case "cmpsw":
                    case "cmpsd":
                    case "vcmpsd":
                    case "cmpsq":
                        instructionInfo = "Compares the byte, word, doubleword, or quadword specified with the first " +
                                          "source operand with the byte, word, doubleword, or quadword specified with " +
                                          "the second source operand and sets the status flags in the EFLAGS register " +
                                          "according to the results.";
                        break;
                    case "vcmpngesd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand, using not greater than or " +
                                          "equal, and returns the results in of the comparison to the destination " +
                                          "operand. The comparison predicate operand (immediate operand) specifies " +
                                          "the type of comparison performed.";
                        break;
                    case "vcmpngtsd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand, using not greater than, and " +
                                          "returns the results in of the comparison to the destination operand. " +
                                          "The comparison predicate operand (immediate operand) specifies the type of " +
                                          "comparison performed.";
                        break;
                    case "vcmpfalsesd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand, using false, and returns " +
                                          "the results in of the comparison to the destination operand. The comparison " +
                                          "predicate operand (immediate operand) specifies the type of comparison performed.";
                        break;
                    case "vcmptruesd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand, using true, and returns the " +
                                          "results in of the comparison to the destination operand. The comparison " +
                                          "predicate operand (immediate operand) specifies the type of comparison performed.";
                        break;
                    case "vcmpgtsd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand, using greater than, and returns " +
                                          "the results in of the comparison to the destination operand. The comparison " +
                                          "predicate operand (immediate operand) specifies the type of comparison performed.";
                        break;
                    case "vcmpgesd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand, using greater than or equal, " +
                                          "and returns the results in of the comparison to the destination operand. " +
                                          "The comparison predicate operand (immediate operand) specifies the type of " +
                                          "comparison performed.";
                        break;

                    case "cmpss":
                    case "vcmpss":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand and returns the results of the " +
                                          "comparison to the destination operand. The comparison predicate operand " +
                                          "(immediate operand) specifies the type of comparison performed.";
                        break;
                    case "cmpeqsd":
                    case "vcmpeqsd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand with , and returns the results " +
                                          "of the comparison to the destination operand.";
                        break;
                    case "cmpeqss":
                    case "vcmpeqss":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with equal, and returns the " +
                                          "results of the comparison to the destination operand.";
                        break;
                    case "cmplesd":
                    case "vcmplesd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand with less than or equal, and " +
                                          "returns the results of the comparison to the destination operand.";
                        break;
                    case "cmpless":
                    case "vcmpless":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with less than or equal, and " +
                                          "returns the results of the comparison to the destination operand.";
                        break;
                    case "cmpltsd":
                    case "vcmpltsd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand with less than, and returns " +
                                          "the results of the comparison to the destination operand.";
                        break;
                    case "cmpltss":
                    case "vcmpltss":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with kess than, and returns " +
                                          "the results of the comparison to the destination operand.";
                        break;
                    case "cmpneqsd":
                    case "vcmpneqsd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand with not equal, and returns " +
                                          "the results of the comparison to the destination operand.";
                        break;
                    case "cmpneqss":
                    case "vcmpneqss":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with not equal, and returns " +
                                          "the results of the comparison to the destination operand.";
                        break;
                    case "cmpnlesd":
                    case "vcmpnlesd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand with not less than or equal, " +
                                          "and returns the results of the comparison to the destination operand.";
                        break;
                    case "cmpnless":
                    case "vcmpnless":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with not less than or equal, " +
                                          "and returns the results of the comparison to the destination operand.";
                        break;
                    case "cmpnltsd":
                    case "vcmpnltsd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand with not less than, and " +
                                          "returns the results of the comparison to the destination operand.";
                        break;
                    case "cmpnltss":
                    case "vcmpnltss":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with not less than, and returns " +
                                          "the results of the comparison to the destination operand.";
                        break;
                    case "cmpordsd":
                    case "vcmpordsd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand with ordered, and returns the " +
                                          "results of the comparison to the destination operand.";
                        break;
                    case "cmpordss":
                    case "vcmpordss":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with ordered, and returns the " +
                                          "results of the comparison to the destination operand.";
                        break;
                    case "vcmpngess":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with not greater than or equal, " +
                                          "and returns the results of the comparison to the destination operand.";
                        break;
                    case "vcmpngtss":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with not greater than, and " +
                                          "returns the results of the comparison to the destination operand.";
                        break;
                    case "vcmpfalsess":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with false, and returns the " +
                                          "results of the comparison to the destination operand.";
                        break;
                    case "vcmpgess":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with greater than or equal, " +
                                          "and returns the results of the comparison to the destination operand.";
                        break;
                    case "vcmpgtss":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with greater than, and " +
                                          "returns the results of the comparison to the destination operand.";
                        break;
                    case "vcmptruess":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with true, and returns the " +
                                          "results of the comparison to the destination operand.";
                        break;
                    case "cmpunordsd":
                    case "vcmpunordsd":
                        instructionInfo = "Compares the low double-precision floating-point values in the second " +
                                          "source operand and the first source operand with unordered, and returns " +
                                          "the results of the comparison to the destination operand.";
                        break;
                    case "cmpunordss":
                    case "vcmpunordss":
                        instructionInfo = "Compares the low single-precision floating-point values in the second " +
                                          "source operand and the first source operand with unordered, and returns " +
                                          "the results of the comparison to the destination operand.";
                        break;
                    case "cmpxchg":
                        instructionInfo = "Compares the value in the AL, AX, EAX, or RAX register with the first " +
                                          "operand (destination operand). If the two values are equal, the second " +
                                          "operand (source operand) is loaded into the destination operand. " +
                                          "Otherwise, the destination operand is loaded into the AL, AX, EAX or RAX " +
                                          "register. RAX register is available only in 64-bit mode.";
                        break;
                    case "cmpxchg8b":
                    case "cmpxchg16b":
                        instructionInfo = "Compares the 64-bit value in EDX:EAX (or 128-bit value in RDX:RAX if " +
                                          "operand size is 128 bits) with the operand (destination operand). " +
                                          "If the values are equal, the 64-bit value in ECX:EBX (or 128-bit value in " +
                                          "RCX:RBX) is stored in the destination operand. Otherwise, the value in the " +
                                          "destination operand is loaded into EDX:EAX (or RDX:RAX). The destination " +
                                          "operand is an 8-byte memory location (or 16-byte memory location if " +
                                          "operand size is 128 bits). For the EDX:EAX and ECX:EBX register pairs, EDX " +
                                          "and ECX contain the high-order 32 bits and EAX and EBX contain the " +
                                          "low-order 32 bits of a 64-bit value. For the RDX:RAX and RCX:RBX register " +
                                          "pairs, RDX and RCX contain the high-order 64 bits and RAX and RBX contain " +
                                          "the low-order 64bits of a 128-bit value.";
                        break;
                    case "comisd":
                    case "vcomisd":
                        instructionInfo = "Compares the double-precision floating-point values in the low quadwords " +
                                          "of operand 1 (first operand) and operand 2 (second operand), and sets the " +
                                          "ZF, PF, and CF flags in the EFLAGS register according to the result " +
                                          "(unordered, greater than, less than, or equal). The OF, SF and AF flags in " +
                                          "the EFLAGS register are set to 0. The unordered result is returned if " +
                                          "either source operand is a NaN (QNaN or SNaN).";
                        break;
                    case "comiss":
                    case "vcomiss":
                        instructionInfo = "Compares the single-precision floating-point values in the low quadwords " +
                                          "of operand 1 (first operand) and operand 2 (second operand), and sets the " +
                                          "ZF, PF, and CF flags in the EFLAGS register according to the result " +
                                          "(unordered, greater than, less than, or equal). The OF, SF and AF flags in " +
                                          "the EFLAGS register are set to 0. The unordered result is returned if " +
                                          "either source operand is a NaN (QNaN or SNaN).";
                        break;
                    case "cpuid":
                        instructionInfo = "The ID flag (bit 21) in the EFLAGS register indicates support for the " +
                                          "CPUID instruction. If a software procedure can set and clear this flag, " +
                                          "the processor executing the procedure supports the CPUID instruction. " +
                                          "This instruction operates the same in non-64-bit modes and 64-bit mode.";
                        break;
                    case "crc32":
                        instructionInfo = "Starting with an initial value in the first operand (destination operand), " +
                                          "accumulates a CRC32 (polynomial 11EDC6F41H) value for the second operand " +
                                          "(source operand) and stores the result in the destination operand. " +
                                          "The source operand can be a register or a memory location. The destination " +
                                          "operand must be an r32 or r64 register. If the destination is an r64 " +
                                          "register, then the 32-bit result is stored in the least significant double " +
                                          "word and 00000000H is stored in the most significant double word of the r64 register.";
                        break;
                    case "cvtdq2pd":
                    case "vcvtdq2pd":
                        instructionInfo = "Converts two, four or eight packed signed doubleword integers in the source " +
                                          "operand (the second operand) to two, four or eight packed double-precision " +
                                          "floating-point values in the destination operand (the first operand).";
                        break;
                    case "cvtdq2ps":
                    case "vcvtdq2ps":
                        instructionInfo = "Converts four, eight or sixteen packed signed doubleword integers in the " +
                                          "source operand to four, eight or sixteen packed single-precision " +
                                          "floating-point values in the destination operand.";
                        break;
                    case "cvtpd2dq":
                    case "vcvtpd2dq":
                        instructionInfo = "Converts packed double-precision floating-point values in the source " +
                                          "operand (second operand) to packed signed doubleword integers in the " +
                                          "destination operand (first operand).";
                        break;
                    case "cvtpd2pi":
                        instructionInfo = "Converts two packed double-precision floating-point values in the source " +
                                          "operand (second operand) to two packed signed doubleword integers in the " +
                                          "destination operand (first operand).";
                        break;
                    case "cvtpd2ps":
                    case "vcvtpd2ps":
                        instructionInfo = "Converts two, four or eight packed double-precision floating-point values " +
                                          "in the source operand (second operand) to two, four or eight packed " +
                                          "single-precision floating-point values in the destination operand (first operand).";
                        break;
                    case "cvtpi2pd":
                    case "vcvtpi2pd":
                        instructionInfo = "Converts two packed signed doubleword integers in the source operand " +
                                          "(second operand) to two packed double-precision floating-point values in " +
                                          "the destination operand (first operand).";
                        break;
                    case "cvtpi2ps":
                    case "vcvtpi2ps":
                        instructionInfo = "Converts two packed signed doubleword integers in the source operand " +
                                          "(second operand) to two packed single-precision floating-point values in " +
                                          "the destination operand (first operand).";
                        break;
                    case "cvtps2dq":
                    case "vcvtps2dq":
                        instructionInfo = "Converts four, eight or sixteen packed single-precision floating-point " +
                                          "values in the source operand to four, eight or sixteen signed doubleword " +
                                          "integers in the destination operand.";
                        break;
                    case "cvtps2pd":
                    case "vcvtps2pd":
                        instructionInfo = "Converts two, four or eight packed single-precision floating-point values " +
                                          "in the source operand (second operand) to two, four or eight packed " +
                                          "double-precision floating-point values in the destination operand (first operand).";
                        break;
                    case "cvtps2pi":
                    case "vcvtps2pi":
                        instructionInfo = "Converts two packed single-precision floating-point values in the source " +
                                          "operand (second operand) to two packed signed doubleword integers in the " +
                                          "destination operand (first operand).";
                        break;
                    case "cvtsd2si":
                    case "vcvtsd2si":
                        instructionInfo = "Converts a double-precision floating-point value in the source operand " +
                                          "(the second operand) to a signed double-word integer in the destination " +
                                          "operand (first operand). The source operand can be an XMM register or a " +
                                          "64-bit memory location. The destination operand is a general-purpose " +
                                          "register. When the source operand is an XMM register, the double-precision " +
                                          "floating-point value is contained in the low quadword of the register.";
                        break;
                    case "cvtsd2ss":
                    case "vcvtsd2ss":
                        instructionInfo = "Converts a double-precision floating-point value in the \"convert-from\" " +
                                          "source operand (the second operand in SSE2 version, otherwise the third " +
                                          "operand) to a single-precision floating-point value in the destination operand.";
                        break;
                    case "cvtsi2sd":
                    case "vcvtsi2sd":
                        instructionInfo = "Converts a signed doubleword integer (or signed quadword integer if " +
                                          "operand size is 64 bits) in the \"convert-from\" source " +
                                          "operand to a double-precision floating-point value in the destination " +
                                          "operand. The result is stored in the low quadword of the destination " +
                                          "operand, and the high quadword left unchanged. When conversion is inexact, " +
                                          "the value returned is rounded according to the rounding control bits in the " +
                                          "MXCSR register.";
                        break;
                    case "cvtsi2ss":
                    case "vcvtsi2ss":
                        instructionInfo = "Converts a signed doubleword integer (or signed quadword integer if " +
                                          "operand size is 64 bits) in the \"convert-from\" source " +
                                          "operand to a single-precision floating-point value in the destination " +
                                          "operand (first operand). The \"convert-from\" source " +
                                          "operand can be a general-purpose register or a memory location. The " +
                                          "destination operand is an XMM register. The result is stored in the low " +
                                          "doubleword of the destination operand, and the upper three doublewords are " +
                                          "left unchanged. When a conversion is inexact, the value returned is rounded " +
                                          "according to the rounding control bits in the MXCSR register or the " +
                                          "embedded rounding control bits.";
                        break;
                    case "cvtss2sd":
                    case "vcvtss2sd":
                        instructionInfo = "Converts a single-precision floating-point value in the \"convert-from\" " +
                                          "source operand to a double-precision floating-point value in the " +
                                          "destination operand. When the \"convert-from\" source " +
                                          "operand is an XMM register, the single-precision floating-point value is " +
                                          "contained in the low doubleword of the register. The result is stored in " +
                                          "the low quadword of the destination operand.";
                        break;
                    case "cvtss2si":
                    case "vcvtss2si":
                        instructionInfo = "Converts a single-precision floating-point value in the source operand (the " +
                                          "second operand) to a signed doubleword integer (or signed quadword integer " +
                                          "if operand size is 64 bits) in the destination operand (the first operand). " +
                                          "The source operand can be an XMM register or a memory location. The " +
                                          "destination operand is a general-purpose register. When the source operand " +
                                          "is an XMM register, the single-precision floating-point value is contained " +
                                          "in the low doubleword of the register.";
                        break;
                    case "cvttpd2dq":
                    case "vcvttpd2dq":
                        instructionInfo = "Converts two, four or eight packed double-precision floating-point values " +
                                          "in the source operand (second operand) to two, four or eight packed signed " +
                                          "doubleword integers in the destination operand (first operand).";
                        break;
                    case "cvttpd2pi":
                    case "vcvttpd2pi":
                        instructionInfo = "Converts two packed double-precision floating-point values in the source " +
                                          "operand (second operand) to two packed signed doubleword integers in the " +
                                          "destination operand (first operand). The source operand can be an XMM " +
                                          "register or a 128-bit memory location. The destination operand is an MMX " +
                                          "technology register.";
                        break;
                    case "cvttps2dq":
                    case "vcvttps2dq":
                        instructionInfo = "Converts four, eight or sixteen packed single-precision floating-point " +
                                          "values in the source operand to four, eight or sixteen signed doubleword " +
                                          "integers in the destination operand.";
                        break;
                    case "cvttps2pi":
                    case "vcvttps2pi":
                        instructionInfo = "Converts two packed single-precision floating-point values in the source " +
                                          "operand (second operand) to two packed signed doubleword integers in the " +
                                          "destination operand (first operand). The source operand can be an XMM " +
                                          "register or a 64-bit memory location. The destination operand is an MMX " +
                                          "technology register. When the source operand is an XMM register, the two " +
                                          "single-precision floating-point values are contained in the low quadword " +
                                          "of the register.";
                        break;
                    case "cvttsd2si":
                    case "vcvttsd2si":
                        instructionInfo = "Converts a double-precision floating-point value in the source operand " +
                                          "(the second operand) to a signed double-word integer (or signed quadword " +
                                          "integer if operand size is 64 bits) in the destination operand (the first " +
                                          "operand). The source operand can be an XMM register or a 64-bit memory " +
                                          "location. The destination operand is a general purpose register. When the " +
                                          "source operand is an XMM register, the double-precision floating-point " +
                                          "value is contained in the low quadword of the register.";
                        break;
                    case "cvttss2si":
                    case "vcvttss2si":
                        instructionInfo = "Converts a single-precision floating-point value in the source operand " +
                                          "(the second operand) to a signed double-word integer (or signed quadword " +
                                          "integer if operand size is 64 bits) in the destination operand (the first " +
                                          "operand). The source operand can be an XMM register or a 32-bit memory " +
                                          "location. The destination operand is a general purpose register. When the " +
                                          "source operand is an XMM register, the single-precision floating-point " +
                                          "value is contained in the low doubleword of the register.";
                        break;
                    case "daa":
                        instructionInfo = "Adjusts the sum of two packed BCD values to create a packed BCD result. " +
                                          "The AL register is the implied source and destination operand. The DAA " +
                                          "instruction is only useful when it follows an ADD instruction that adds " +
                                          "(binary addition) two 2-digit, packed BCD values and stores a byte result " +
                                          "in the AL register. The DAA instruction then adjusts the contents of the " +
                                          "AL register to contain the correct 2-digit, packed BCD result. If a decimal " +
                                          "carry is detected, the CF and AF flags are set accordingly.";
                        break;
                    case "das":
                        instructionInfo = "Adjusts the result of the subtraction of two packed BCD values to create " +
                                          "a packed BCD result. The AL register is the implied source and destination " +
                                          "operand. The DAS instruction is only useful when it follows a SUB " +
                                          "instruction that subtracts (binary subtraction) one 2-digit, packed BCD " +
                                          "value from another and stores a byte result in the AL register. The DAS " +
                                          "instruction then adjusts the contents of the AL register to contain the " +
                                          "correct 2-digit, packed BCD result. If a decimal borrow is detected, the CF " +
                                          "and AF flags are set accordingly.";
                        break;
                    case "dec":
                        instructionInfo = "Subtracts 1 from the destination operand, while preserving the state of " +
                                          "the CF flag. The destination operand can be a register or a memory location. " +
                                          "This instruction allows a loop counter to be updated without disturbing the " +
                                          "CF flag. (To perform a decrement operation that updates the CF flag, use a " +
                                          "SUB instruction with an immediate operand of 1.)";
                        break;
                    case "div":
                        instructionInfo = "Divides unsigned the value in the AX, DX:AX, EDX:EAX, or RDX:RAX registers " +
                                          "(dividend) by the source operand (divisor) and stores the result in the AX " +
                                          "(AH:AL), DX:AX, EDX:EAX, or RDX:RAX registers. The source operand can be a " +
                                          "general-purpose register or a memory location. The action of this " +
                                          "instruction depends on the operand size (dividend/divisor). Division using " +
                                          "64-bit operand is available only in 64-bit mode.";
                        break;
                    case "divpd":
                    case "vdivpd":
                        instructionInfo = "Performs a SIMD divide of the double-precision floating-point values in the " +
                                          "first source operand by the floating-point values in the second source " +
                                          "operand (the third operand). Results are written to the destination operand " +
                                          "(the first operand).";
                        break;
                    case "divps":
                    case "vdivps":
                        instructionInfo = "Performs a SIMD divide of the four, eight or sixteen packed single-precision " +
                                          "floating-point values in the first source operand (the second operand) by " +
                                          "the four, eight or sixteen packed single-precision floating-point values in " +
                                          "the second source operand (the third operand). Results are written to the " +
                                          "destination operand (the first operand).";
                        break;
                    case "divsd":
                    case "vdivsd":
                        instructionInfo = "Divides the low double-precision floating-point value in the first source " +
                                          "operand by the low double-precision floating-point value in the second " +
                                          "source operand, and stores the double-precision floating-point result in " +
                                          "the destination operand. The second source operand can be an XMM register " +
                                          "or a 64-bit memory location. The first source and destination are XMM registers.";
                        break;
                    case "divss":
                    case "vdivss":
                        instructionInfo = "Divides the low single-precision floating-point value in the first source " +
                                          "operand by the low single-precision floating-point value in the second " +
                                          "source operand, and stores the single-precision floating-point result in " +
                                          "the destination operand. The second source operand can be an XMM register " +
                                          "or a 32-bit memory location.";
                        break;
                    case "dppd":
                    case "vdppd":
                        instructionInfo = "Conditionally multiplies the packed double-precision floating-point values " +
                                          "in the destination operand (first operand) with the packed double-precision " +
                                          "floating-point values in the source (second operand) depending on a mask " +
                                          "extracted from bits [5:4] of the immediate operand (third operand). If a " +
                                          "condition mask bit is zero, the corresponding multiplication is replaced by " +
                                          "a value of 0.0 in the manner described by Section 12.8.4 of Intel " +
                                          "64 and IA-32 Architectures Software Developers Manual, Volume 1.";
                        break;
                    case "dpps":
                    case "vdpps":
                        instructionInfo = "Conditionally multiplies the packed single precision floating-point values " +
                                          "in the destination operand (first operand) with the packed single-precision " +
                                          "floats in the source (second operand) depending on a mask extracted from " +
                                          "the high 4 bits of the immediate byte (third operand). If a condition mask " +
                                          "bit in Imm8[7:4] is zero, the corresponding multiplication is replaced by a " +
                                          "value of 0.0 in the manner described by Section 12.8.4 of Intel " +
                                          "64 and IA-32 Architectures Software Developers Manual, Volume 1.";
                        break;
                    case "emms":
                        instructionInfo = "Sets the values of all the tags in the x87 FPU tag word to empty (all 1s). " +
                                          "This operation marks the x87 FPU data registers (which are aliased to the " +
                                          "MMX technology registers) as available for use by x87 FPU floating-point " +
                                          "instructions. All other MMX instructions (other than the EMMS instruction) " +
                                          "set all the tags in x87 FPU tag word to valid (all 0s).";
                        break;
                    case "enter":
                        instructionInfo = "Creates a stack frame (comprising of space for dynamic storage and 1-32 " +
                                          "frame pointer storage) for a procedure. The first operand (imm16) specifies " +
                                          "the size of the dynamic storage in the stack frame (that is, the number of " +
                                          "bytes of dynamically allocated on the stack for the procedure). The second " +
                                          "operand (imm8) gives the lexical nesting level (0 to 31) of the procedure. " +
                                          "The nesting level (imm8 mod 32) and the OperandSize attribute determine the " +
                                          "size in bytes of the storage space for frame pointers.";
                        break;
                    case "extractps":
                    case "vextractps":
                        instructionInfo = "Extracts a single-precision floating-point value from the source operand " +
                                          "(second operand) at the 32-bit offset specified from imm8. Immediate bits " +
                                          "higher than the most significant offset for the vector length are ignored.";
                        break;
                    case "f2xm1":
                        instructionInfo = "Computes the exponential value of 2 to the power of the source operand " +
                                          "minus 1. The source operand is located in register ST(0) and the result is " +
                                          "also stored in ST(0). The value of the source operand must lie in the range " +
                                          "-1.0 to +1.0. If the source value is outside this range, the " +
                                          "result is undefined.";
                        break;
                    case "fabs":
                        instructionInfo = "Clears the sign bit of ST(0) to create the absolute value of the operand. " +
                                          "The following table shows the results obtained when creating the absolute " +
                                          "value of various classes of numbers.";
                        break;
                    case "fadd":
                    case "faddp":
                    case "fiadd":
                        instructionInfo = "Adds the destination and source operands and stores the sum in the " +
                                          "destination location. The destination operand is always an FPU register; " +
                                          "the source operand can be a register or a memory location. Source operands " +
                                          "in memory can be in single-precision or double-precision floating-point " +
                                          "format or in word or doubleword integer format.";
                        break;
                    case "fbld":
                        instructionInfo = "Converts the BCD source operand into double extended-precision " +
                                          "floating-point format and pushes the value onto the FPU stack. The " +
                                          "source operand is loaded without rounding errors. The sign of the source " +
                                          "operand is preserved, including that of \xe2\x88\x920.";
                        break;
                    case "fbstp":
                        instructionInfo = "Converts the value in the ST(0) register to an 18-digit packed BCD integer, " +
                                          "stores the result in the destination operand, and pops the register stack. " +
                                          "If the source value is a non-integral value, it is rounded to an integer " +
                                          "value, according to rounding mode specified by the RC field of the FPU " +
                                          "control word. To pop the register stack, the processor marks the ST(0) " +
                                          "register as empty and increments the stack pointer (TOP) by 1.";
                        break;
                    case "fchs":
                        instructionInfo = "Complements the sign bit of ST(0). This operation changes a positive value " +
                                          "into a negative value of equal magnitude or vice versa. The following table " +
                                          "shows the results obtained when changing the sign of various classes of numbers.";
                        break;
                    case "fclex":
                    case "fnclex":
                        instructionInfo = "Clears the floating-point exception flags (PE, UE, OE, ZE, DE, and IE), " +
                                          "the exception summary status flag (ES), the stack fault flag (SF), and the " +
                                          "busy flag (B) in the FPU status word. The FCLEX instruction checks for and " +
                                          "handles any pending unmasked floating-point exceptions before clearing the " +
                                          "exception flags; the FNCLEX instruction does not.";
                        break;
                    case "fcmovb":
                    case "fcmove":
                    case "fcmovbe":
                    case "fcmovu":
                    case "fcmovnb":
                    case "fcmovne":
                    case "fcmovnbe":
                    case "fcmovnu":
                        instructionInfo = "Tests the status flags in the EFLAGS register and moves the source operand " +
                                          "(second operand) to the destination operand (first operand) if the given " +
                                          "test condition is true. The condition for each mnemonic os given in the " +
                                          "Description column above and in Chapter 8 in the Intel 64 and " +
                                          "IA-32 Architectures Software Developers Manual, Volume 1. " +
                                          "The source operand is always in the ST(i) register and the destination " +
                                          "operand is always ST(0).";
                        break;
                    case "fcom":
                    case "fcomp":
                    case "fcompp":
                        instructionInfo = "Compares the contents of register ST(0) and source value and sets condition " +
                                          "code flags C0, C2, and C3 in the FPU status word according to the results. " +
                                          "The source operand can be a data register or a memory location. " +
                                          "If no source operand is given, the value in ST(0) is compared with the " +
                                          "value in ST(1). The sign of zero is ignored, so that -0.0 is " +
                                          "equal to +0.0.";
                        break;
                    case "fcomi":
                    case "fcomip":
                    case "fucomi":
                    case "fucomip":
                        instructionInfo = "Performs an unordered comparison of the contents of registers ST(0) and " +
                                          "ST(i) and sets the status flags ZF, PF, and CF in the EFLAGS register " +
                                          "according to the results. The sign of zero is ignored " +
                                          "for comparisons, so that -0.0 is equal to +0.0.";
                        break;
                    case "fcos":
                        instructionInfo = "Computes the approximate cosine of the source operand in register ST(0) " +
                                          "and stores the result in ST(0). The source operand must be given in " +
                                          "radians and must be within the range -2^63 to " +
                                          "+2^63.";
                        break;
                    case "fdecstp":
                        instructionInfo = "Subtracts one from the TOP field of the FPU status word (decrements the " +
                                          "top-of-stack pointer). If the TOP field contains a 0, it is set to 7. The " +
                                          "effect of this instruction is to rotate the stack by one position. " +
                                          "The contents of the FPU data registers and tag register are not affected.";
                        break;
                    case "fdiv":
                    case "fdivp":
                    case "fidiv":
                        instructionInfo = "Divides the destination operand by the source operand and stores the result " +
                                          "in the destination location. The destination operand (dividend) is always " +
                                          "in an FPU register; the source operand (divisor) can be a register or a " +
                                          "memory location. Source operands in memory can be in single-precision or " +
                                          "double-precision floating-point format, word or doubleword integer format.";
                        break;
                    case "fdivr":
                    case "fdivrp":
                    case "fidivr":
                        instructionInfo = "Divides the source operand by the destination operand and stores the " +
                                          "result in the destination location. The destination operand (divisor) is " +
                                          "always in an FPU register; the source operand (dividend) can be a register " +
                                          "or a memory location. Source operands in memory can be in single-precision " +
                                          "or double-precision floating-point format, word or doubleword integer format.";
                        break;
                    case "ffree":
                        instructionInfo = "Sets the tag in the FPU tag register associated with register ST(i) to " +
                                          "empty (11B). The contents of ST(i) and the FPU stack-top pointer (TOP) are " +
                                          "not affected.";
                        break;
                    case "ficom":
                    case "ficomp":
                        instructionInfo = "Compares the value in ST(0) with an integer source operand and sets the " +
                                          "condition code flags C0, C2, and C3 in the FPU status word according to " +
                                          "the results. The integer value is converted to double " +
                                          "extended-precision floating-point format before the comparison is made.";
                        break;
                    case "fild":
                        instructionInfo = "Converts the signed-integer source operand into double extended-precision " +
                                          "floating-point format and pushes the value onto the FPU register stack. " +
                                          "The source operand can be a word, doubleword, or quadword integer. It is " +
                                          "loaded without rounding errors. The sign of the source operand is preserved.";
                        break;
                    case "fmul":
                    case "fmulp":
                    case "fimul":
                        instructionInfo = "Multiplies the destination and source operands and stores the product in " +
                                          "the destination location. The destination operand is always an FPU data " +
                                          "register; the source operand can be an FPU data register or a memory " +
                                          "location. Source operands in memory can be in single-precision or " +
                                          "double-precision floating-point format or in word or doubleword integer format.";
                        break;
                    case "fincstp":
                        instructionInfo = "Adds one to the TOP field of the FPU status word (increments the " +
                                          "top-of-stack pointer). If the TOP field contains a 7, it is set to 0. " +
                                          "The effect of this instruction is to rotate the stack by one position. " +
                                          "The contents of the FPU data registers and tag register are not affected. " +
                                          "This operation is not equivalent to popping the stack, because the tag for " +
                                          "the previous top-of-stack register is not marked empty.";
                        break;
                    case "finit":
                    case "fninit":
                        instructionInfo = "Sets the FPU control, status, tag, instruction pointer, and data pointer " +
                                          "registers to their default states. The FPU control word is set to 037FH " +
                                          "(round to nearest, all exceptions masked, 64-bit precision). The status word " +
                                          "is cleared (no exception flags set, TOP is set to 0). The data registers " +
                                          "in the register stack are left unchanged, but they are all tagged as empty " +
                                          "(11B). Both the instruction and data pointers are cleared.";
                        break;
                    case "fist":
                    case "fistp":
                        instructionInfo = "The FIST instruction converts the value in the ST(0) register to a signed " +
                                          "integer and stores the result in the destination operand. Values can be " +
                                          "stored in word or doubleword integer format. The destination operand " +
                                          "specifies the address where the first byte of the destination value is to " +
                                          "be stored.";
                        break;
                    case "fisttp":
                        instructionInfo = "FISTTP converts the value in ST into a signed integer using truncation " +
                                          "(chop) as rounding mode, transfers the result to the destination, and pop ST. " +
                                          "FISTTP accepts word, short integer, and long integer destinations.";
                        break;
                    case "fsub":
                    case "fsubp":
                    case "fisub":
                        instructionInfo = "Subtracts the source operand from the destination operand and stores the " +
                                          "difference in the destination location. The destination operand is always " +
                                          "an FPU data register; the source operand can be a register or a memory " +
                                          "location. Source operands in memory can be in single-precision or " +
                                          "double-precision floating-point format or in word or doubleword integer format.";
                        break;
                    case "fsubr":
                    case "fsubrp":
                    case "fisubr":
                        instructionInfo = "Subtracts the destination operand from the source operand and stores the " +
                                          "difference in the destination location. The destination operand is always " +
                                          "an FPU register; the source operand can be a register or a memory location. " +
                                          "Source operands in memory can be in single-precision or double-precision " +
                                          "floating-point format or in word or doubleword integer format.";
                        break;
                    case "fld":
                        instructionInfo = "Pushes the source operand onto the FPU register stack. The source operand " +
                                          "can be in single-precision, double-precision, or double extended-precision " +
                                          "floating-point format. If the source operand is in single-precision or " +
                                          "double-precision floating-point format, it is automatically converted to " +
                                          "the double extended-precision floating-point format before being pushed on " +
                                          "the stack.";
                        break;
                    case "fld1":
                    case "fldl2t":
                    case "fldl2e":
                    case "fldpi":
                    case "fldlg2":
                    case "fldln2":
                    case "fldz":
                        instructionInfo = "Push one of seven commonly used constants (in double extended-precision " +
                                          "floating-point format) onto the FPU register stack. The constants that can " +
                                          "be loaded with these instructions include +1.0, +0.0, log10^2, " +
                                          "loge^2, pi, log2^10, and log2^e. For " +
                                          "each constant, an internal 66-bit constant is rounded (as specified by the " +
                                          "RC field in the FPU control word) to double extended-precision " +
                                          "floating-point format. The inexact-result exception (#P) is not generated " +
                                          "as a result of the rounding, nor is the C1 flag set in the x87 FPU status " +
                                          "word if the value is rounded up.";
                        break;
                    case "fldcw":
                        instructionInfo = "Loads the 16-bit source operand into the FPU control word. The source " +
                                          "operand is a memory location. This instruction is typically used to " +
                                          "establish or change the FPUs mode of operation.";
                        break;
                    case "fldenv":
                        instructionInfo = "Loads the complete x87 FPU operating environment from memory into the " +
                                          "FPU registers. The source operand specifies the first byte of the " +
                                          "operating-environment data in memory. This data is typically written to " +
                                          "the specified memory location by a FSTENV or FNSTENV instruction.";
                        break;
                    case "fnop":
                        instructionInfo = "Performs no FPU operation. This instruction takes up space in the " +
                                          "instruction stream but does not affect the FPU or machine context, except " +
                                          "the EIP register and the FPU Instruction Pointer.";
                        break;
                    case "fsave":
                    case "fnsave":
                        instructionInfo = "Stores the current FPU state (operating environment and register stack) " +
                                          "at the specified destination in memory, and then re-initializes the FPU. " +
                                          "The FSAVE instruction checks for and handles pending unmasked floating-point " +
                                          "exceptions before storing the FPU state; the FNSAVE instruction does not.";
                        break;
                    case "fstcw":
                    case "fnstcw":
                        instructionInfo = "Stores the current value of the FPU control word at the specified " +
                                          "destination in memory. The FSTCW instruction checks for and handles " +
                                          "pending unmasked floating-point exceptions before storing the control " +
                                          "word; the FNSTCW instruction does not.";
                        break;
                    case "fstenv":
                    case "fnstenv":
                        instructionInfo = "Saves the current FPU operating environment at the memory location specified " +
                                          "with the destination operand, and then masks all floating-point exceptions. " +
                                          "The FPU operating environment consists of the FPU control word, status word, " +
                                          "tag word, instruction pointer, data pointer, and last opcode.";
                        break;
                    case "fstsw":
                    case "fnstsw":
                        instructionInfo = "Stores the current value of the x87 FPU status word in the destination " +
                                          "location. The destination operand can be either a two-byte memory location " +
                                          "or the AX register. The FSTSW instruction checks for and handles pending " +
                                          "unmasked floating-point exceptions before storing the status word; the " +
                                          "FNSTSW instruction does not.";
                        break;
                    case "fpatan":
                        instructionInfo = "Computes the arctangent of the source operand in register ST(1) divided by " +
                                          "the source operand in register ST(0), stores the result in ST(1), and pops " +
                                          "the FPU register stack. The result in register ST(0) has the same sign as " +
                                          "the source operand ST(1) and a magnitude less than +\xcf\x80.";
                        break;
                    case "fprem":
                        instructionInfo = "Computes the remainder obtained from dividing the value in the ST(0) " +
                                          "register (the dividend) by the value in the ST(1) register (the divisor or " +
                                          "modulus), and stores the result in ST(0). The remainder " +
                                          "represents the following value:";
                        break;
                    case "fprem1":
                        instructionInfo = "Computes the IEEE remainder obtained from dividing the value in the ST(0) " +
                                          "register (the dividend) by the value in the ST(1) register (the divisor or " +
                                          "modulus), and stores the result in ST(0). The remainder " +
                                          "represents the following value:";
                        break;
                    case "fptan":
                        instructionInfo = "Computes the approximate tangent of the source operand in register ST(0), " +
                                          "stores the result in ST(0), and pushes a 1.0 onto the FPU register stack. " +
                                          "The source operand must be given in radians and must be less than " +
                                          "+-2^63. The following table shows the unmasked results " +
                                          "obtained when computing the partial tangent of various classes of numbers, " +
                                          "assuming that underflow does not occur.";
                        break;
                    case "frndint":
                        instructionInfo = "Rounds the source value in the ST(0) register to the nearest integral value, " +
                                          "depending on the current rounding mode (setting of the RC field of the FPU " +
                                          "control word), and stores the result in ST(0).";
                        break;
                    case "frstor":
                        instructionInfo = "Loads the FPU state (operating environment and register stack) from the " +
                                          "memory area specified with the source operand. This state data is typically " +
                                          "written to the specified memory location by a previous FSAVE/FNSAVE instruction.";
                        break;
                    case "fscale":
                        instructionInfo = "Truncates the value in the source operand (toward 0) to an integral value " +
                                          "and adds that value to the exponent of the destination operand. The " +
                                          "destination and source operands are floating-point values located in " +
                                          "registers ST(0) and ST(1), respectively. This instruction provides rapid " +
                                          "multiplication or division by integral powers of 2.";
                        break;
                    case "fsin":
                        instructionInfo = "Computes an approximation of the sine of the source operand in register " +
                                          "ST(0) and stores the result in ST(0). The source operand must be given in " +
                                          "radians and must be within the range -2^63 to " +
                                          "+2^63.";
                        break;
                    case "fsincos":
                        instructionInfo = "Computes both the approximate sine and the cosine of the source operand in " +
                                          "register ST(0), stores the sine in ST(0), and pushes the cosine onto the " +
                                          "top of the FPU register stack. (This instruction is faster than executing " +
                                          "the FSIN and FCOS instructions in succession.)";
                        break;
                    case "fsqrt":
                        instructionInfo = "Computes the square root of the source value in the ST(0) register and " +
                                          "stores the result in ST(0).";
                        break;
                    case "fst":
                    case "fstp":
                        instructionInfo = "The FST instruction copies the value in the ST(0) register to the " +
                                          "destination operand, which can be a memory location or another register in " +
                                          "the FPU register stack. When storing the value in memory, the value is " +
                                          "converted to single-precision or double-precision floating-point format.";
                        break;
                    case "ftst":
                        instructionInfo = "Compares the value in the ST(0) register with 0.0 and sets the condition " +
                                          "code flags C0, C2, and C3 in the FPU status word according to the results.";
                        break;
                    case "fucom":
                    case "fucomp":
                    case "fucompp":
                        instructionInfo = "Performs an unordered comparison of the contents of register ST(0) and " +
                                          "ST(i) and sets condition code flags C0, C2, and C3 in the FPU status word " +
                                          "according to the results (see the table below). If no operand is specified, " +
                                          "the contents of registers ST(0) and ST(1) are compared. The sign of zero " +
                                          "is ignored, so that -0.0 is equal to +0.0.";
                        break;
                    case "wait":
                    case "fwait":
                        instructionInfo = "Causes the processor to check for and handle pending, unmasked, " +
                                          "floating-point exceptions before proceeding. (FWAIT is an alternate " +
                                          "mnemonic for WAIT.)";
                        break;
                    case "fxam":
                        instructionInfo = "Examines the contents of the ST(0) register and sets the condition code " +
                                          "flags C0, C2, and C3 in the FPU status word to indicate the class of value " +
                                          "or number in the register (see the table below).";
                        break;
                    case "fxch":
                        instructionInfo = "Exchanges the contents of registers ST(0) and ST(i). If no source operand " +
                                          "is specified, the contents of ST(0) and ST(1) are exchanged.";
                        break;
                    case "fxrstor":
                    case "fxrstor64":
                        instructionInfo = "Reloads the x87 FPU, MMX technology, XMM, and MXCSR registers from the " +
                                          "512-byte memory image specified in the source operand. This data should " +
                                          "have been written to memory previously using the FXSAVE instruction, and in " +
                                          "the same format as required by the operating modes. The first byte of the " +
                                          "data should be located on a 16-byte boundary. There are three distinct " +
                                          "layouts of the FXSAVE state map: one for legacy and compatibility mode, a " +
                                          "second format for 64-bit mode FXSAVE/FXRSTOR with REX.W=0, and the third " +
                                          "format is for 64-bit mode with FXSAVE64/FXRSTOR64.";
                        break;
                    case "fxsave":
                    case "fxsave64":
                        instructionInfo = "Saves the current state of the x87 FPU, MMX technology, XMM, and MXCSR " +
                                          "registers to a 512-byte memory location specified in the destination " +
                                          "operand. The content layout of the 512 byte region depends on whether the " +
                                          "processor is operating in non-64-bit operating modes or 64-bit sub-mode of " +
                                          "IA-32e mode.";
                        break;
                    case "fxtract":
                        instructionInfo = "Separates the source value in the ST(0) register into its exponent and " +
                                          "significand, stores the exponent in ST(0), and pushes the significand onto " +
                                          "the register stack. Following this operation, the new top-of-stack register " +
                                          "ST(0) contains the value of the original significand expressed as a " +
                                          "floating-point value. The sign and significand of this value are the same " +
                                          "as those found in the source operand, and the exponent is 3FFFH (biased " +
                                          "value for a true exponent of zero). The ST(1) register contains the value " +
                                          "of the original operands true (unbiased) exponent expressed as " +
                                          "a floating-point value. (The operation performed by this instruction is a " +
                                          "superset of the IEEE-recommended logb x) function.)";
                        break;
                    case "fyl2x":
                        instructionInfo = "Computes (ST(1) \xe2\x88\x97 log<sub>2</sub> (ST(0))), stores the result " +
                                          "in register ST(1), and pops the FPU register stack. The source operand in " +
                                          "ST(0) must be a non-zero positive number.";
                        break;
                    case "fyl2xp1":
                        instructionInfo = "Computes (ST(1) * log(ST(0) + 1.0))^2, stores the " +
                                          "result in register ST(1), and pops the FPU register stack. The source " +
                                          "operand in ST(0) must be in the range:";
                        break;
                    case "gf2p8affineinvqb":
                        instructionInfo = "The AFFINEINVB instruction computes an affine transformation in the Galois " +
                                          "Field 2^8. For this instruction, an affine transformation is " +
                                          "defined by A * inv(x) + b where \"A\" is an 8 by 8 bit " +
                                          "matrix, and \"x\" and \"b\" are " +
                                          "8-bit vectors. The inverse of the bytes in x is defined with respect to the " +
                                          "reduction polynomial x^8 + x^4 + x^3 + x + 1.";
                        break;
                    case "gf2p8affineqb":
                        instructionInfo = "The AFFINEB instruction computes an affine transformation in the Galois " +
                                          "Field 2^8. For this instruction, an affine transformation is " +
                                          "defined by A * x + b where \"A\" is an 8 by 8 bit " +
                                          "matrix, and \"x\" and \"b\" are " +
                                          "8-bit vectors. One SIMD register (operand 1) holds \"x\" " +
                                          "as either 16, 32 or 64 8-bit vectors. A second SIMD (operand 2) register or " +
                                          "memory operand contains 2, 4, or 8 \"A\" values, which " +
                                          "are operated upon by the correspondingly aligned 8 \"x\" " +
                                          "values in the first register. The \"b\" vector is " +
                                          "constant for all calculations and contained in the immediate byte.";
                        break;
                    case "gf2p8mulb":
                        instructionInfo = "The instruction multiplies elements in the finite field GF(2^8), " +
                                          "operating on a byte (field element) in the first source operand and the " +
                                          "corresponding byte in a second source operand. The field GF(2^8) " +
                                          "is represented in polynomial representation with the reduction polynomial " +
                                          "x^8 + x^4 + x^3 + x + 1.";
                        break;
                    case "haddpd":
                    case "vhaddpd":
                        instructionInfo = "Adds the double-precision floating-point values in the high and low " +
                                          "quadwords of the destination operand and stores the result in the low " +
                                          "quadword of the destination operand.";
                        break;
                    case "haddps":
                    case "vhaddps":
                        instructionInfo = "Adds the single-precision floating-point values in the first and second " +
                                          "dwords of the destination operand and stores the result in the first dword " +
                                          "of the destination operand.";
                        break;
                    case "hlt":
                        instructionInfo = "Stops instruction execution and places the processor in a HALT state. " +
                                          "An enabled interrupt (including NMI and SMI), a debug exception, the " +
                                          "BINIT# signal, the INIT# signal, or the RESET# signal will resume execution. " +
                                          "If an interrupt (including NMI) is used to resume execution after a HLT " +
                                          "instruction, the saved instruction pointer (CS:EIP) points to the " +
                                          "instruction following the HLT instruction.";
                        break;
                    case "hsubpd":
                    case "vhsubpd":
                        instructionInfo = "The HSUBPD instruction subtracts horizontally the packed DP FP numbers " +
                                          "of both operands.";
                        break;
                    case "hsubps":
                    case "vhsubps":
                        instructionInfo = "Subtracts the single-precision floating-point value in the second dword of " +
                                          "the destination operand from the first dword of the destination operand and " +
                                          "stores the result in the first dword of the destination operand.";
                        break;
                    case "idiv":
                        instructionInfo = "Divides the (signed) value in the AX, DX:AX, or EDX:EAX (dividend) by the " +
                                          "source operand (divisor) and stores the result in the AX (AH:AL), DX:AX, " +
                                          "or EDX:EAX registers. The source operand can be a general-purpose register " +
                                          "or a memory location. The action of this instruction depends on the operand " +
                                          "size (dividend/divisor).";
                        break;
                    case "imul":
                        instructionInfo = "Performs a signed multiplication of two operands. This instruction has " +
                                          "three forms, depending on the number of operands.";
                        break;
                    case "in":
                        instructionInfo = "Copies the value from the I/O port specified with the second operand " +
                                          "(source operand) to the destination operand (first operand). The source " +
                                          "operand can be a byte-immediate or the DX register; the destination operand " +
                                          "can be register AL, AX, or EAX, depending on the size of the port being " +
                                          "accessed (8, 16, or 32 bits, respectively). Using the DX register as a " +
                                          "source operand allows I/O port addresses from 0 to 65,535 to be accessed; " +
                                          "using a byte immediate allows I/O port addresses 0 to 255 to be accessed.";
                        break;
                    case "inc":
                        instructionInfo = "Adds 1 to the destination operand, while preserving the state of the CF " +
                                          "flag. The destination operand can be a register or a memory location. " +
                                          "This instruction allows a loop counter to be updated without disturbing " +
                                          "the CF flag. (Use a ADD instruction with an immediate operand of 1 to " +
                                          "perform an increment operation that does updates the CF flag.)";
                        break;
                    case "ins":
                    case "insb":
                    case "insw":
                    case "insd":
                    case "vinsd":
                        instructionInfo = "Copies the data from the I/O port specified with the source operand " +
                                          "(second operand) to the destination operand (first operand). The source " +
                                          "operand is an I/O port address (from 0 to 65,535) that is read from the DX " +
                                          "register. The destination operand is a memory location, the address of " +
                                          "which is read from either the ES:DI, ES:EDI or the RDI registers (depending " +
                                          "on the address-size attribute of the instruction, 16, 32 or 64, " +
                                          "respectively). (The ES segment cannot be overridden with a segment override " +
                                          "prefix.) The size of the I/O port being accessed (that is, the size of the " +
                                          "source and destination operands) is determined by the opcode for an 8-bit " +
                                          "I/O port or by the operand-size attribute of the instruction for a 16- or " +
                                          "32-bit I/O port.";
                        break;
                    case "insertps":
                    case "vinsertps":
                        instructionInfo = "Copy a single-precision scalar floating-point element into a 128-bit vector " +
                                          "register. The immediate operand has three fields, where the ZMask bits " +
                                          "specify which elements of the destination will be set to zero, the Count_D " +
                                          "bits specify which element of the destination will be overwritten with the " +
                                          "scalar value, and for vector register sources the Count_S bits specify " +
                                          "which element of the source will be copied. When the scalar source is a " +
                                          "memory operand the Count_S bits are ignored.";
                        break;
                    case "int":
                    case "into":
                    case "int3":
                    case "int1":
                        instructionInfo = "The INTn instruction generates a call to the interrupt or " +
                                          "exception handler specified with the destination operand. The destination operand " +
                                          "specifies a vector from 0 to 255, encoded as an 8-bit unsigned intermediate " +
                                          "value. Each vector provides an index to a gate descriptor in the IDT. The " +
                                          "first 32 vectors are reserved by Intel for system use. Some of these " +
                                          "vectors are used for internally generated exceptions.";
                        break;
                    case "invd":
                        instructionInfo = "Invalidates (flushes) the processors internal caches and issues " +
                                          "a special-function bus cycle that directs external caches to also flush " +
                                          "themselves. Data held in internal caches is not written back to main memory.";
                        break;
                    case "invlpg":
                        instructionInfo = "Invalidates any translation lookaside buffer (TLB) entries specified with " +
                                          "the source operand. The source operand is a memory address. The processor " +
                                          "determines the page that contains that address and flushes all TLB entries " +
                                          "for that page.";
                        break;
                    case "invpcid":
                        instructionInfo = "Invalidates mappings in the translation lookaside buffers (TLBs) and " +
                                          "paging-structure caches based on process-context identifier (PCID). " +
                                          "Invalidation is based on the INVPCID type specified in the register operand " +
                                          "and the INVPCID descriptor specified in the memory operand.";
                        break;
                    case "iret":
                    case "iretd":
                        instructionInfo = "Returns program control from an exception or interrupt handler to a program " +
                                          "or procedure that was interrupted by an exception, an external interrupt, " +
                                          "or a software-generated interrupt. These instructions are also used to " +
                                          "perform a return from a nested task. (A nested task is created when a CALL " +
                                          "instruction is used to initiate a task switch or when an interrupt or " +
                                          "exception causes a task switch to an interrupt or exception handler.)";
                        break;
                    case "jmp":
                        instructionInfo = "Transfers program control to a different point in the instruction stream " +
                                          "without recording return information. The destination (target) operand " +
                                          "specifies the address of the instruction being jumped to. This operand can " +
                                          "be an immediate value, a general-purpose register, or a memory location.";
                        break;
                    case "ja":
                    case "jae":
                    case "jb":
                    case "jbe":
                    case "jc":
                    case "jcxz":
                    case "jecxz":
                    case "jrcxz":
                    case "je":
                    case "jg":
                    case "jge":
                    case "jl":
                    case "jle":
                    case "jna":
                    case "jnae":
                    case "jnb":
                    case "jnbe":
                    case "jnc":
                    case "jne":
                    case "jng":
                    case "jnge":
                    case "jnl":
                    case "jnle":
                    case "jno":
                    case "jnp":
                    case "jns":
                    case "jnz":
                    case "jo":
                    case "jp":
                    case "jpe":
                    case "jpo":
                    case "js":
                    case "jz":
                        instructionInfo = "Checks the state of one or more of the status flags in the EFLAGS register " +
                                          "(CF, OF, PF, SF, and ZF) and, if the flags are in the specified state " +
                                          "(condition), performs a jump to the target instruction specified by the " +
                                          "destination operand. A condition code (cc) is associated with each " +
                                          "instruction to indicate the condition being tested for. If the condition is " +
                                          "not satisfied, the jump is not performed and execution continues with the " +
                                          "instruction following the Jcc instruction.";
                        break;
                    case "kaddw":
                    case "kaddb":
                    case "kaddq":
                    case "kaddd":
                        instructionInfo = "Adds the vector mask k2 and the vector mask k3, and writes the result into " +
                                          "vector mask k1.";
                        break;
                    case "kandw":
                    case "kandb":
                    case "kandq":
                    case "kandd":
                        instructionInfo = "Performs a bitwise AND between the vector mask k2 and the vector mask k3, " +
                                          "and writes the result into vector mask k1.";
                        break;
                    case "kandnw":
                    case "kandnb":
                    case "kandnq":
                    case "kandnd":
                        instructionInfo = "Performs a bitwise AND NOT between the vector mask k2 and the vector mask " +
                                          "k3, and writes the result into vector mask k1.";
                        break;
                    case "kmovw":
                    case "kmovb":
                    case "kmovq":
                    case "kmovd":
                        instructionInfo = "Copies values from the source operand (second operand) to the destination " +
                                          "operand (first operand). The source and destination operands can be mask " +
                                          "registers, memory location or general purpose. The instruction cannot be " +
                                          "used to transfer data between general purpose registers and or memory locations.";
                        break;
                    case "knotw":
                    case "knotb":
                    case "knotq":
                    case "knotd":
                        instructionInfo = "Performs a bitwise NOT of vector mask k2 and writes the result into vector mask k1.";
                        break;
                    case "korw":
                    case "korb":
                    case "korq":
                    case "kord":
                        instructionInfo = "Performs a bitwise OR between the vector mask k2 and the vector mask k3, " +
                                          "and writes the result into vector mask k1 (three-operand form).";
                        break;
                    case "kortestw":
                    case "kortestb":
                    case "kortestq":
                    case "kortestd":
                        instructionInfo = "Performs a bitwise OR between the vector mask register k2, and the vector " +
                                          "mask register k1, and sets CF and ZF based on the operation result.";
                        break;
                    case "kshiftlw":
                    case "kshiftlb":
                    case "kshiftlq":
                    case "kshiftld":
                        instructionInfo = "Shifts 8/16/32/64 bits in the second operand (source operand) left by the " +
                                          "count specified in immediate byte and place the least significant " +
                                          "8/16/32/64 bits of the result in the destination operand. The higher bits " +
                                          "of the destination are zero-extended. The destination is set to zero if the " +
                                          "count value is greater than 7 (for byte shift), 15 (for word shift), 31 " +
                                          "(for doubleword shift) or 63 (for quadword shift).";
                        break;
                    case "kshiftrw":
                    case "kshiftrb":
                    case "kshiftrq":
                    case "kshiftrd":
                        instructionInfo = "Shifts 8/16/32/64 bits in the second operand (source operand) right by the " +
                                          "count specified in immediate and place the least significant 8/16/32/64 " +
                                          "bits of the result in the destination operand. The higher bits of the " +
                                          "destination are zero-extended. The destination is set to zero if the count " +
                                          "value is greater than 7 (for byte shift), 15 (for word shift), 31 (for " +
                                          "doubleword shift) or 63 (for quadword shift).";
                        break;
                    case "ktestw":
                    case "ktestb":
                    case "ktestq":
                    case "ktestd":
                        instructionInfo = "Performs a bitwise comparison of the bits of the first source operand and " +
                                          "corresponding bits in the second source operand. If the AND operation " +
                                          "produces all zeros, the ZF is set else the ZF is clear. If the bitwise " +
                                          "AND operation of the inverted first source operand with the second source " +
                                          "operand produces all zeros the CF is set else the CF is clear. Only the " +
                                          "EFLAGS register is updated.";
                        break;
                    case "kunpckbw":
                    case "kunpckwd":
                    case "kunpckdq":
                        instructionInfo = "Unpacks the lower 8/16/32 bits of the second and third operands (source " +
                                          "operands) into the low part of the first operand (destination operand), " +
                                          "starting from the low bytes. The result is zero-extended in the destination.";
                        break;
                    case "kxnorw":
                    case "kxnorb":
                    case "kxnorq":
                    case "kxnord":
                        instructionInfo = "Performs a bitwise XNOR between the vector mask k2 and the vector mask k3, " +
                                          "and writes the result into vector mask k1 (three-operand form).";
                        break;
                    case "kxorw":
                    case "kxorb":
                    case "kxorq":
                    case "kxord":
                        instructionInfo = "Performs a bitwise XOR between the vector mask k2 and the vector mask k3, " +
                                          "and writes the result into vector mask k1 (three-operand form).";
                        break;
                    case "lahf":
                        instructionInfo = "This instruction executes as described above in compatibility mode and " +
                                          "legacy mode. It is valid in 64-bit mode only if " +
                                          "CPUID.80000001H:ECX.LAHF-SAHF[bit 0] = 1.";
                        break;
                    case "lar":
                        instructionInfo = "Loads the access rights from the segment descriptor specified by the second " +
                                          "operand (source operand) into the first operand (destination operand) and " +
                                          "sets the ZF flag in the flag register. The source operand (which can be a " +
                                          "register or a memory location) contains the segment selector for the " +
                                          "segment descriptor being accessed. If the source operand is a memory " +
                                          "address, only 16 bits of data are accessed. The destination operand is a " +
                                          "general-purpose register.";
                        break;
                    case "lddqu":
                    case "vlddqu":
                        instructionInfo = "The instruction is functionally similar to (V)MOVDQU ymm/xmm, " +
                                          "m256/m128 for loading from memory. That is: 32/16 bytes of data starting " +
                                          "at an address specified by the source memory operand (second operand) are " +
                                          "fetched from memory and placed in a destination register (first operand). " +
                                          "The source operand need not be aligned on a 32/16-byte boundary. Up to 64/32 " +
                                          "bytes may be loaded from memory; this is implementation dependent.";
                        break;
                    case "ldmxcsr":
                    case "vldmxcsr":
                        instructionInfo = "Loads the source operand into the MXCSR control/status register. The source " +
                                          "operand is a 32-bit memory location.";
                        break;
                    case "lds":
                    case "les":
                    case "lfs":
                    case "lgs":
                    case "lss":
                        instructionInfo = "Loads a far pointer (segment selector and offset) from the second operand " +
                                          "(source operand) into a segment register and the first operand (destination " +
                                          "operand). The source operand specifies a 48-bit or a 32-bit pointer in " +
                                          "memory depending on the current setting of the operand-size attribute (32 " +
                                          "bits or 16 bits, respectively). The instruction opcode and the destination " +
                                          "operand specify a segment register/general-purpose register pair. The 16-bit " +
                                          "segment selector from the source operand is loaded into the segment " +
                                          "register specified with the opcode (DS, SS, ES, FS, or GS). The 32-bit or " +
                                          "16-bit offset is loaded into the register specified with the destination operand.";
                        break;
                    case "lea":
                        instructionInfo = "Computes the effective address of the second operand (the source operand) " +
                                          "and stores it in the first operand (destination operand). The source operand " +
                                          "is a memory address (offset part) specified with one of the processors " +
                                          "addressing modes; the destination operand is a general-purpose register. " +
                                          "The address-size and operand-size attributes affect the action performed by " +
                                          "this instruction, as shown in the following table. The operand-size " +
                                          "attribute of the instruction is determined by the chosen register; the " +
                                          "address-size attribute is determined by the attribute of the code segment.";
                        break;
                    case "leave":
                        instructionInfo = "Releases the stack frame set up by an earlier ENTER instruction. The LEAVE " +
                                          "instruction copies the frame pointer (in the EBP register) into the stack " +
                                          "pointer register (ESP), which releases the stack space allocated to the " +
                                          "stack frame. The old frame pointer (the frame pointer for the calling " +
                                          "procedure that was saved by the ENTER instruction) is then popped from the " +
                                          "stack into the EBP register, restoring the calling procedures " +
                                          "stack frame.";
                        break;
                    case "lfence":
                        instructionInfo = "Performs a serializing operation on all load-from-memory instructions that " +
                                          "were issued prior the LFENCE instruction. Specifically, LFENCE does not " +
                                          "execute until all prior instructions have completed locally, and no later " +
                                          "instruction begins execution until LFENCE completes. In particular, an " +
                                          "instruction that loads from memory and that precedes an LFENCE receives data " +
                                          "from memory prior to completion of the LFENCE. (An LFENCE that follows an " +
                                          "instruction that stores to memory might complete before the " +
                                          "data being stored have become globally visible.) Instructions following an " +
                                          "LFENCE may be fetched from memory before the LFENCE, but they will not " +
                                          "execute (even speculatively) until the LFENCE completes.";
                        break;
                    case "lgdt":
                    case "lidt":
                        instructionInfo = "Loads the values in the source operand into the global descriptor table " +
                                          "register (GDTR) or the interrupt descriptor table register (IDTR). " +
                                          "The source operand specifies a 6-byte memory location that contains the base " +
                                          "address (a linear address) and the limit (size of table in bytes) of the " +
                                          "global descriptor table (GDT) or the interrupt descriptor table (IDT). If " +
                                          "operand-size attribute is 32 bits, a 16-bit limit (lower 2 bytes of the " +
                                          "6-byte data operand) and a 32-bit base address (upper 4 bytes of the data " +
                                          "operand) are loaded into the register. If the operand-size attribute is 16 " +
                                          "bits, a 16-bit limit (lower 2 bytes) and a 24-bit base address (third, " +
                                          "fourth, and fifth byte) are loaded. Here, the high-order byte of the operand " +
                                          "is not used and the high-order byte of the base address in the GDTR or IDTR " +
                                          "is filled with zeros.";
                        break;
                    case "lldt":
                        instructionInfo = "Loads the source operand into the segment selector field of the local " +
                                          "descriptor table register (LDTR). The source operand (a general-purpose " +
                                          "register or a memory location) contains a segment selector that points to a " +
                                          "local descriptor table (LDT). After the segment selector is loaded in the " +
                                          "LDTR, the processor uses the segment selector to locate the segment " +
                                          "descriptor for the LDT in the global descriptor table (GDT). It then loads " +
                                          "the segment limit and base address for the LDT from the segment descriptor " +
                                          "into the LDTR. The segment registers DS, ES, SS, FS, GS, and CS are not " +
                                          "affected by this instruction, nor is the LDTR field in the task state " +
                                          "segment (TSS) for the current task.";
                        break;
                    case "lmsw":
                        instructionInfo = "Loads the source operand into the machine status word, bits 0 through 15 of " +
                                          "register CR0. The source operand can be a 16-bit general-purpose register " +
                                          "or a memory location. Only the low-order 4 bits of the source operand " +
                                          "(which contains the PE, MP, EM, and TS flags) are loaded into CR0. The PG, " +
                                          "CD, NW, AM, WP, NE, and ET flags of CR0 are not affected. The operand-size " +
                                          "attribute has no effect on this instruction.";
                        break;
                    case "lock":
                        instructionInfo = "Causes the processors LOCK# signal to be asserted during " +
                                          "execution of the accompanying instruction (turns the instruction into an " +
                                          "atomic instruction). In a multiprocessor environment, the LOCK# signal " +
                                          "ensures that the processor has exclusive use of any shared memory while the " +
                                          "signal is asserted.";
                        break;
                    case "lods":
                    case "lodsb":
                    case "lodsw":
                    case "lodsd":
                    case "lodsq":
                        instructionInfo = "Loads a byte, word, or doubleword from the source operand into the AL, AX, " +
                                          "or EAX register, respectively. The source operand is a memory location, the " +
                                          "address of which is read from the DS:ESI or the DS:SI registers (depending " +
                                          "on the address-size attribute of the instruction, 32 or 16, respectively). " +
                                          "The DS segment may be overridden with a segment override prefix.";
                        break;
                    case "loop":
                    case "loope":
                    case "loopne":
                    case "loopnz":
                    case "loopz":
                        instructionInfo = "Performs a loop operation using the RCX, ECX or CX register as a counter " +
                                          "(depending on whether address size is 64 bits, 32 bits, or 16 bits). Note " +
                                          "that the LOOP instruction ignores REX.W; but 64-bit address size can be " +
                                          "over-ridden using a 67H prefix. LOOPcc also accept the ZF flag as a " +
                                          "condition for terminating the loop before the count reaches zero. With " +
                                          "these forms of the instruction, a condition code (cc) is associated with " +
                                          "each instruction to indicate the condition being tested for.";
                        break;
                    case "lsl":
                        instructionInfo = "Loads the unscrambled segment limit from the segment descriptor specified " +
                                          "with the second operand (source operand) into the first operand (destination " +
                                          "operand) and sets the ZF flag in the EFLAGS register. The source operand " +
                                          "(which can be a register or a memory location) contains the segment selector " +
                                          "for the segment descriptor being accessed. The destination operand is a " +
                                          "general-purpose register.";
                        break;
                    case "ltr":
                        instructionInfo = "Loads the source operand into the segment selector field of the task " +
                                          "register. The source operand (a general-purpose register or a memory " +
                                          "location) contains a segment selector that points to a task state segment " +
                                          "(TSS). After the segment selector is loaded in the task register, the " +
                                          "processor uses the segment selector to locate the segment descriptor for the " +
                                          "TSS in the global descriptor table (GDT). It then loads the segment limit " +
                                          "and base address for the TSS from the segment descriptor into the task " +
                                          "register. The task pointed to by the task register is marked busy, but a " +
                                          "switch to the task does not occur.";
                        break;
                    case "lzcnt":
                        instructionInfo = "Counts the number of leading most significant zero bits in a source operand " +
                                          "(second operand) returning the result into a destination (first operand).";
                        break;
                    case "maskmovdqu":
                        instructionInfo = "Stores selected bytes from the source operand (first operand) into an " +
                                          "128-bit memory location. The mask operand (second operand) selects which " +
                                          "bytes from the source operand are written to memory. The source and mask " +
                                          "operands are XMM registers. The memory location specified by the effective " +
                                          "address in the DI/EDI/RDI register (the default segment register is DS, but " +
                                          "this may be overridden with a segment-override prefix). The memory location " +
                                          "does not need to be aligned on a natural boundary. (The size of the store " +
                                          "address depends on the address-size attribute.)";
                        break;
                    case "maskmovq":
                        instructionInfo = "Stores selected bytes from the source operand (first operand) into a 64-bit " +
                                          "memory location. The mask operand (second operand) selects which bytes from " +
                                          "the source operand are written to memory. The source and mask operands are " +
                                          "MMX technology registers. The memory location specified by the effective " +
                                          "address in the DI/EDI/RDI register (the default segment register is DS, but " +
                                          "this may be overridden with a segment-override prefix). The memory location " +
                                          "does not need to be aligned on a natural boundary. (The size of the store " +
                                          "address depends on the address-size attribute.)";
                        break;
                    case "maxpd":
                    case "vmaxpd":
                        instructionInfo = "Performs a SIMD compare of the packed double-precision floating-point values " +
                                          "in the first source operand and the second source operand and returns the " +
                                          "maximum value for each pair of values to the destination operand.";
                        break;
                    case "maxps":
                    case "vmaxps":
                        instructionInfo = "Performs a SIMD compare of the packed single-precision floating-point values " +
                                          "in the first source operand and the second source operand and returns the " +
                                          "maximum value for each pair of values to the destination operand.";
                        break;
                    case "maxsd":
                    case "vmaxsd":
                        instructionInfo = "Compares the low double-precision floating-point values in the first source " +
                                          "operand and the second source operand, and returns the maximum value to the " +
                                          "low quadword of the destination operand. The second source operand can be an " +
                                          "XMM register or a 64-bit memory location. The first source and destination " +
                                          "operands are XMM registers. When the second source operand is a memory " +
                                          "operand, only 64 bits are accessed.";
                        break;
                    case "maxss":
                    case "vmaxss":
                        instructionInfo = "Compares the low single-precision floating-point values in the first source " +
                                          "operand and the second source operand, and returns the maximum value to the " +
                                          "low doubleword of the destination operand.";
                        break;
                    case "mfence":
                        instructionInfo = "Performs a serializing operation on all load-from-memory and store-to-memory " +
                                          "instructions that were issued prior the MFENCE instruction. This serializing " +
                                          "operation guarantees that every load and store instruction that precedes the " +
                                          "MFENCE instruction in program order becomes globally visible before any load " +
                                          "or store instruction that follows the MFENCE instruction. The " +
                                          "MFENCE instruction is ordered with respect to all load and store " +
                                          "instructions, other MFENCE instructions, any LFENCE and SFENCE instructions, " +
                                          "and any serializing instructions (such as the CPUID instruction). MFENCE " +
                                          "does not serialize the instruction stream.";
                        break;
                    case "minpd":
                    case "vminpd":
                        instructionInfo = "Performs a SIMD compare of the packed double-precision floating-point values " +
                                          "in the first source operand and the second source operand and returns the " +
                                          "minimum value for each pair of values to the destination operand.";
                        break;
                    case "minps":
                    case "vminps":
                        instructionInfo = "Performs a SIMD compare of the packed single-precision floating-point " +
                                          "values in the first source operand and the second source operand and returns " +
                                          "the minimum value for each pair of values to the destination operand.";
                        break;
                    case "minsd":
                    case "vminsd":
                        instructionInfo = "Compares the low double-precision floating-point values in the first source " +
                                          "operand and the second source operand, and returns the minimum value to the " +
                                          "low quadword of the destination operand. When the source operand is a memory " +
                                          "operand, only the 64 bits are accessed.";
                        break;
                    case "minss":
                    case "vminss":
                        instructionInfo = "Compares the low single-precision floating-point values in the first source " +
                                          "operand and the second source operand and returns the minimum value to the " +
                                          "low doubleword of the destination operand.";
                        break;
                    case "monitor":
                        instructionInfo = "The MONITOR instruction arms address monitoring hardware using an address " +
                                          "specified in EAX (the address range that the monitoring hardware checks for " +
                                          "store operations can be determined by using CPUID). A store to an address " +
                                          "within the specified address range triggers the monitoring hardware. The " +
                                          "state of monitor hardware is used by MWAIT.";
                        break;
                    case "mov":
                        instructionInfo = "Copies the second operand (source operand) to the first operand (destination " +
                                          "operand). The source operand can be an immediate value, general-purpose " +
                                          "register, segment register, or memory location; the destination register can " +
                                          "be a general-purpose register, segment register, or memory location. Both " +
                                          "operands must be the same size, which can be a byte, a word, a doubleword, " +
                                          "or a quadword.";
                        break;
                    case "movabs":
                        instructionInfo = "Moves 4, 8 or 16 single-precision floating-point values from the source " +
                                          "operand (second operand) to the destina-tion operand (first operand).";
                        break;
                    case "movapd":
                    case "vmovapd":
                        instructionInfo = "Moves 2, 4 or 8 double-precision floating-point values from the source " +
                                          "operand (second operand) to the destination operand (first operand). This " +
                                          "instruction can be used to load an XMM, YMM or ZMM register from an 128-bit, " +
                                          "256-bit or 512-bit memory location, to store the contents of an XMM, YMM or " +
                                          "ZMM register into a 128-bit, 256-bit or 512-bit memory location, or to move " +
                                          "data between two XMM, two YMM or two ZMM registers.";
                        break;
                    case "movaps":
                    case "vmovaps":
                        instructionInfo = "Moves 4, 8 or 16 single-precision floating-point values from the source " +
                                          "operand (second operand) to the destination operand (first operand). This " +
                                          "instruction can be used to load an XMM, YMM or ZMM register from an 128-bit, " +
                                          "256-bit or 512-bit memory location, to store the contents of an XMM, YMM or " +
                                          "ZMM register into a 128-bit, 256-bit or 512-bit memory location, or to move " +
                                          "data between two XMM, two YMM or two ZMM registers.";
                        break;
                    case "movbe":
                        instructionInfo = "Performs a byte swap operation on the data copied from the second operand " +
                                          "(source operand) and store the result in the first operand (destination " +
                                          "operand). The source operand can be a general-purpose register, or memory " +
                                          "location; the destination register can be a general-purpose register, or a " +
                                          "memory location; however, both operands can not be registers, and only one " +
                                          "operand can be a memory location. Both operands must be the same size, which " +
                                          "can be a word, a doubleword or quadword.";
                        break;
                    case "movd":
                    case "vmovd":
                        instructionInfo = "Copies a doubleword from the source operand (second operand) to the " +
                                          "destination operand (first operand). The source and destination operands can " +
                                          "be general-purpose registers, MMX technology registers, XMM registers, or " +
                                          "32-bit memory locations. This instruction can be used to move a doubleword " +
                                          "to and from the low doubleword of an MMX technology register and a " +
                                          "general-purpose register or a 32-bit memory location, or to and from the low " +
                                          "doubleword of an XMM register and a general-purpose register or a 32-bit " +
                                          "memory location. The instruction cannot be used to transfer data between MMX " +
                                          "technology registers, between XMM registers, between general-purpose " +
                                          "registers, or between memory locations.";
                        break;
                    case "movddup":
                    case "vmovddup":
                        instructionInfo = "For 256-bit or higher versions: Duplicates even-indexed double-precision " +
                                          "floating-point values from the source operand (the second operand) and into " +
                                          "adjacent pair and store to the destination operand (the first operand).";
                        break;
                    case "movdir64b":
                        instructionInfo = "Moves 64-bytes as direct-store with 64-byte write atomicity from source " +
                                          "memory address to destination memory address. The source operand is a " +
                                          "normal memory operand. The destination operand is a memory location " +
                                          "specified in a general-purpose register. The register content is interpreted " +
                                          "as an offset into ES segment without any segment override. In 64-bit mode, " +
                                          "the register operand width is 64-bits (32-bits with 67H prefix). Outside of " +
                                          "64-bit mode, the register width is 32-bits when CS.D=1 (16-bits with 67H " +
                                          "prefix), and 16-bits when CS.D=0 (32-bits with 67H prefix). MOVDIR64B " +
                                          "requires the destination address to be 64-byte aligned. No alignment " +
                                          "restriction is enforced for source operand.";
                        break;
                    case "movdiri":
                        instructionInfo = "Moves the doubleword integer in the source operand (second operand) to the " +
                                          "destination operand (first operand) using a direct-store operation. The " +
                                          "source operand is a general purpose register. The destination operand is a " +
                                          "32-bit memory location. In 64-bit mode, the instructions default " +
                                          "operation size is 32 bits. Use of the REX.R prefix permits access to " +
                                          "additional registers (R8-R15). Use of the REX.W prefix promotes operation " +
                                          "to 64 bits. See summary chart at the beginning of this section for encoding " +
                                          "data and limits.";
                        break;
                    case "movdq2q":
                        instructionInfo = "Moves the low quadword from the source operand (second operand) to the " +
                                          "destination operand (first operand). The source operand is an XMM register " +
                                          "and the destination operand is an MMX technology register.";
                        break;
                    case "movdqa":
                    case "vmovdqa":
                    case "vmovdqa32":
                    case "vmovdqa64":
                        instructionInfo = "Note: VEX.vvvv and EVEX.vvvv are reserved and must be 1111b otherwise instructions will #UD.";
                        break;
                    case "movdqu":
                    case "vmovdqu":
                    case "vmovdqu8":
                    case "vmovdqu16":
                    case "vmovdqu32":
                    case "vmovdqu64":
                        instructionInfo = "Note: VEX.vvvv and EVEX.vvvv are reserved and must be 1111b otherwise instructions will #UD.";
                        break;
                    case "movhlps":
                    case "vmovhlps":
                        instructionInfo = "This instruction cannot be used for memory to register moves.";
                        break;
                    case "movhpd":
                    case "vmovhpd":
                        instructionInfo = "This instruction cannot be used for register to register or memory to memory moves.";
                        break;
                    case "movhps":
                    case "vmovhps":
                        instructionInfo = "This instruction cannot be used for register to register or memory to memory moves.";
                        break;
                    case "movlhps":
                    case "vmovlhps":
                        instructionInfo = "This instruction cannot be used for memory to register moves.";
                        break;
                    case "movlpd":
                    case "vmovlpd":
                        instructionInfo = "This instruction cannot be used for register to register or memory to memory moves.";
                        break;
                    case "movlps":
                    case "vmovlps":
                        instructionInfo = "This instruction cannot be used for register to register or memory to memory moves.";
                        break;
                    case "movmskpd":
                    case "vmovmskpd":
                        instructionInfo = "Extracts the sign bits from the packed double-precision floating-point " +
                                          "values in the source operand (second operand), formats them into a 2-bit " +
                                          "mask, and stores the mask in the destination operand (first operand). The " +
                                          "source operand is an XMM register, and the destination operand is a " +
                                          "general-purpose register. The mask is stored in the 2 low-order bits of the " +
                                          "destination operand. Zero-extend the upper bits of the destination.";
                        break;
                    case "movmskps":
                    case "vmovmskps":
                        instructionInfo = "Extracts the sign bits from the packed single-precision floating-point " +
                                          "values in the source operand (second operand), formats them into a 4- or " +
                                          "8-bit mask, and stores the mask in the destination operand (first operand). " +
                                          "The source operand is an XMM or YMM register, and the destination operand is " +
                                          "a general-purpose register. The mask is stored in the 4 or 8 low-order bits " +
                                          "of the destination operand. The upper bits of the destination operand beyond " +
                                          "the mask are filled with zeros.";
                        break;
                    case "movntdq":
                    case "vmovntdq":
                        instructionInfo = "Moves the packed integers in the source operand (second operand) to the " +
                                          "destination operand (first operand) using a non-temporal hint to prevent " +
                                          "caching of the data during the write to memory. The source operand is an XMM " +
                                          "register, YMM register or ZMM register, which is assumed to contain integer " +
                                          "data (packed bytes, words, double-words, or quadwords). The destination " +
                                          "operand is a 128-bit, 256-bit or 512-bit memory location. The memory operand " +
                                          "must be aligned on a 16-byte (128-bit version), 32-byte (VEX.256 encoded " +
                                          "version) or 64-byte (512-bit version) boundary otherwise a " +
                                          "general-protection exception (#GP) will be generated.";
                        break;
                    case "movntdqa":
                    case "vmovntdqa":
                        instructionInfo = "MOVNTDQA loads a double quadword from the source operand (second operand) to " +
                                          "the destination operand (first operand) using a non-temporal hint if the " +
                                          "memory source is WC (write combining) memory type. For WC memory type, the " +
                                          "nontemporal hint may be implemented by loading a temporary internal buffer " +
                                          "with the equivalent of an aligned cache line without filling this data to " +
                                          "the cache. Any memory-type aliased lines in the cache will be snooped and " +
                                          "flushed. Subsequent MOVNTDQA reads to unread portions of the WC cache line " +
                                          "will receive data from the temporary internal buffer if data is available. " +
                                          "The temporary internal buffer may be flushed by the processor at any time " +
                                          "for any reason, for example:";
                        break;
                    case "movnti":
                        instructionInfo = "Moves the doubleword integer in the source operand (second operand) to the " +
                                          "destination operand (first operand) using a non-temporal hint to minimize " +
                                          "cache pollution during the write to memory. The source operand is a " +
                                          "general-purpose register. The destination operand is a 32-bit memory location.";
                        break;
                    case "movntpd":
                    case "vmovntpd":
                        instructionInfo = "Moves the packed double-precision floating-point values in the source " +
                                          "operand (second operand) to the destination operand (first operand) using a " +
                                          "non-temporal hint to prevent caching of the data during the write to memory. " +
                                          "The source operand is an XMM register, YMM register or ZMM register, which " +
                                          "is assumed to contain packed double-precision, floating-pointing data. The " +
                                          "destination operand is a 128-bit, 256-bit or 512-bit memory location. The " +
                                          "memory operand must be aligned on a 16-byte (128-bit version), 32-byte " +
                                          "(VEX.256 encoded version) or 64-byte (EVEX.512 encoded version) boundary " +
                                          "otherwise a general-protection exception (#GP) will be generated.";
                        break;
                    case "movntps":
                    case "vmovntps":
                        instructionInfo = "Moves the packed single-precision floating-point values in the source " +
                                          "operand (second operand) to the destination operand (first operand) using a " +
                                          "non-temporal hint to prevent caching of the data during the write to memory. " +
                                          "The source operand is an XMM register, YMM register or ZMM register, which " +
                                          "is assumed to contain packed single-precision, floating-pointing. The " +
                                          "destination operand is a 128-bit, 256-bit or 512-bit memory location. The " +
                                          "memory operand must be aligned on a 16-byte (128-bit version), 32-byte " +
                                          "(VEX.256 encoded version) or 64-byte (EVEX.512 encoded version) boundary " +
                                          "otherwise a general-protection exception (#GP) will be generated.";
                        break;
                    case "movntq":
                        instructionInfo = "Moves the quadword in the source operand (second operand) to the destination " +
                                          "operand (first operand) using a non-temporal hint to minimize cache " +
                                          "pollution during the write to memory. The source operand is an MMX " +
                                          "technology register, which is assumed to contain packed integer data (packed " +
                                          "bytes, words, or doublewords). The destination operand is a 64-bit memory location.";
                        break;
                    case "movq":
                    case "vmovq":
                        instructionInfo = "Copies a quadword from the source operand (second operand) to the " +
                                          "destination operand (first operand). The source and destination operands can " +
                                          "be MMX technology registers, XMM registers, or 64-bit memory locations. This " +
                                          "instruction can be used to move a quadword between two MMX technology " +
                                          "registers or between an MMX technology register and a 64-bit memory " +
                                          "location, or to move data between two XMM registers or between an XMM " +
                                          "register and a 64-bit memory location. The instruction cannot be used to " +
                                          "transfer data between memory locations.";
                        break;
                    case "movq2dq":
                        instructionInfo = "Moves the quadword from the source operand (second operand) to the low " +
                                          "quadword of the destination operand (first operand). The source operand is " +
                                          "an MMX technology register and the destination operand is an XMM register.";
                        break;
                    case "movs":
                    case "movsb":
                    case "movsw":
                    case "movsq":
                        instructionInfo = "Moves the byte, word, or doubleword specified with the second operand " +
                                          "(source operand) to the location specified with the first operand " +
                                          "(destination operand). Both the source and destination operands are located " +
                                          "in memory. The address of the source operand is read from the DS:ESI or the " +
                                          "DS:SI registers (depending on the address-size attribute of the instruction, " +
                                          "32 or 16, respectively). The address of the destination operand is read from " +
                                          "the ES:EDI or the ES:DI registers (again depending on the address-size " +
                                          "attribute of the instruction). The DS segment may be overridden with a " +
                                          "segment override prefix, but the ES segment cannot be overridden.";
                        break;
                    case "movsd":
                    case "vmovsd":
                        instructionInfo = "Moves a scalar double-precision floating-point value from the source operand " +
                                          "(second operand) to the destination operand (first operand). The source and " +
                                          "destination operands can be XMM registers or 64-bit memory locations. This " +
                                          "instruction can be used to move a double-precision floating-point value to " +
                                          "and from the low quadword of an XMM register and a 64-bit memory location, " +
                                          "or to move a double-precision floating-point value between the low quadwords " +
                                          "of two XMM registers. The instruction cannot be used to transfer data " +
                                          "between memory locations.";
                        break;
                    case "movshdup":
                    case "vmovshdup":
                        instructionInfo = "Duplicates odd-indexed single-precision floating-point values from the " +
                                          "source operand (the second operand) to adjacent element pair in the " +
                                          "destination operand (the first operand). The source operand is an XMM, YMM " +
                                          "or ZMM register or 128, 256 or 512-bit memory location and the destination " +
                                          "operand is an XMM, YMM or ZMM register.";
                        break;
                    case "movsldup":
                    case "vmovsldup":
                        instructionInfo = "Duplicates even-indexed single-precision floating-point values from the " +
                                          "source operand (the second operand). The source operand is an XMM, YMM or " +
                                          "ZMM register or 128, 256 or 512-bit memory location and the destination " +
                                          "operand is an XMM, YMM or ZMM register.";
                        break;
                    case "movss":
                    case "vmovss":
                        instructionInfo = "Moves a scalar single-precision floating-point value from the source operand " +
                                          "(second operand) to the destination operand (first operand). The source and " +
                                          "destination operands can be XMM registers or 32-bit memory locations. This " +
                                          "instruction can be used to move a single-precision floating-point value to " +
                                          "and from the low doubleword of an XMM register and a 32-bit memory location, " +
                                          "or to move a single-precision floating-point value between the low " +
                                          "doublewords of two XMM registers. The instruction cannot be used to transfer " +
                                          "data between memory locations.";
                        break;
                    case "movsx":
                    case "movsxd":
                        instructionInfo = "Copies the contents of the source operand (register or memory location) to " +
                                          "the destination operand (register) and sign extends the value to 16 or 32 " +
                                          "bits. The size of the converted value depends on the operand-size attribute.";
                        break;
                    case "movupd":
                    case "vmovupd":
                        instructionInfo = "Note: VEX.vvvv and EVEX.vvvv is reserved and must be 1111b otherwise instructions will #UD.";
                        break;
                    case "movups":
                    case "vmovups":
                        instructionInfo = "Note: VEX.vvvv and EVEX.vvvv is reserved and must be 1111b otherwise instructions will #UD.";
                        break;
                    case "movzx":
                        instructionInfo = "Copies the contents of the source operand (register or memory location) to " +
                                          "the destination operand (register) and zero extends the value. The size of " +
                                          "the converted value depends on the operand-size attribute.";
                        break;
                    case "mpsadbw":
                    case "vmpsadbw":
                        instructionInfo = "(V)MPSADBW calculates packed word results of sum-absolute-difference (SAD) " +
                                          "of unsigned bytes from two blocks of 32-bit dword elements, using two select " +
                                          "fields in the immediate byte to select the offsets of the two blocks within " +
                                          "the first source operand and the second operand. Packed SAD word results are " +
                                          "calculated within each 128-bit lane. Each SAD word result is calculated " +
                                          "between a stationary block_2 (whose offset within the second source operand " +
                                          "is selected by a two bit select control, multiplied by 32 bits) and a " +
                                          "sliding block_1 at consecutive byte-granular position within the first source " +
                                          "operand. The offset of the first 32-bit block of block_1 is selectable using " +
                                          "a one bit select control, multiplied by 32 bits.";
                        break;
                    case "mul":
                        instructionInfo = "Performs an unsigned multiplication of the first operand (destination " +
                                          "operand) and the second operand (source operand) and stores the result in the " +
                                          "destination operand. The destination operand is an implied operand located in " +
                                          "register AL, AX or EAX (depending on the size of the operand); the source " +
                                          "operand is located in a general-purpose register or a memory location. The " +
                                          "action of this instruction and the location of the result depends on the " +
                                          "opcode and the operand size.";
                        break;
                    case "mulpd":
                    case "vmulpd":
                        instructionInfo = "Multiply packed double-precision floating-point values from the first source " +
                                          "operand with corresponding values in the second source operand, and stores " +
                                          "the packed double-precision floating-point results in the destination operand.";
                        break;
                    case "mulps":
                    case "vmulps":
                        instructionInfo = "Multiply the packed single-precision floating-point values from the first " +
                                          "source operand with the corresponding values in the second source operand, " +
                                          "and stores the packed double-precision floating-point results in the " +
                                          "destination operand.";
                        break;
                    case "mulsd":
                    case "vmulsd":
                        instructionInfo = "Multiplies the low double-precision floating-point value in the second source " +
                                          "operand by the low double-precision floating-point value in the first source " +
                                          "operand, and stores the double-precision floating-point result in the " +
                                          "destination operand. The second source operand can be an XMM register or a " +
                                          "64-bit memory location. The first source operand and the destination operands " +
                                          "are XMM registers.";
                        break;
                    case "mulss":
                    case "vmulss":
                        instructionInfo = "Multiplies the low single-precision floating-point value from the second " +
                                          "source operand by the low single-precision floating-point value in the first " +
                                          "source operand, and stores the single-precision floating-point result in the " +
                                          "destination operand. The second source operand can be an XMM register or a " +
                                          "32-bit memory location. The first source operand and the destination operands " +
                                          "are XMM registers.";
                        break;
                    case "mulx":
                        instructionInfo = "Performs an unsigned multiplication of the implicit source operand (EDX/RDX) " +
                                          "and the specified source operand (the third operand) and stores the low half " +
                                          "of the result in the second destination (second operand), the high half of " +
                                          "the result in the first destination operand (first operand), without reading " +
                                          "or writing the arithmetic flags. This enables efficient programming where " +
                                          "the software can interleave add with carry operations and multiplications.";
                        break;
                    case "mwait":
                        instructionInfo = "MWAIT instruction provides hints to allow the processor to enter an " +
                                          "implementation-dependent optimized state. There are two principal targeted " +
                                          "usages: address-range monitor and advanced power management. Both usages of " +
                                          "MWAIT require the use of the MONITOR instruction.";
                        break;
                    case "neg":
                        instructionInfo = "Replaces the value of operand (the destination operand) with its two\'s " +
                                          "complement. (This operation is equivalent to subtracting the operand from 0.) " +
                                          "The destination operand is located in a general-purpose register or a memory " +
                                          "location.";
                        break;
                    case "nop":
                        instructionInfo = "This instruction performs no operation. It is a one-byte or multi-byte NOP " +
                                          "that takes up space in the instruction stream but does not impact machine " +
                                          "context, except for the EIP register.";
                        break;
                    case "not":
                        instructionInfo = "Performs a bitwise NOT operation (each 1 is set to 0, and each 0 is set to 1) " +
                                          "on the destination operand and stores the result in the destination operand " +
                                          "location. The destination operand can be a register or a memory location.";
                        break;
                    case "or":
                        instructionInfo = "Performs a bitwise inclusive OR operation between the destination (first) " +
                                          "and source (second) operands and stores the result in the destination operand " +
                                          "location. The source operand can be an immediate, a register, or a memory " +
                                          "location; the destination operand can be a register or a memory location. " +
                                          "(However, two memory operands cannot be used in one instruction.) Each bit " +
                                          "of the result of the OR instruction is set to 0 if both corresponding bits " +
                                          "of the first and second operands are 0; otherwise, each bit is set to 1.";
                        break;
                    case "orpd":
                    case "vorpd":
                        instructionInfo = "Performs a bitwise logical OR of the two, four or eight packed " +
                                          "double-precision floating-point values from the first source operand and the " +
                                          "second source operand, and stores the result in the destination operand.";
                        break;
                    case "orps":
                    case "vorps":
                        instructionInfo = "Performs a bitwise logical OR of the four, eight or sixteen packed " +
                                          "single-precision floating-point values from the first source operand and the " +
                                          "second source operand, and stores the result in the destination operand";
                        break;
                    case "out":
                        instructionInfo = "Copies the value from the second operand (source operand) to the I/O port " +
                                          "specified with the destination operand (first operand). The source operand " +
                                          "can be register AL, AX, or EAX, depending on the size of the port being " +
                                          "accessed (8, 16, or 32 bits, respectively); the destination operand can be a " +
                                          "byte-immediate or the DX register. Using a byte immediate allows I/O port " +
                                          "addresses 0 to 255 to be accessed; using the DX register as a source operand " +
                                          "allows I/O ports from 0 to 65,535 to be accessed.";
                        break;
                    case "outs":
                    case "outsb":
                    case "outsw":
                    case "outsd":
                        instructionInfo = "Copies data from the source operand (second operand) to the I/O port " +
                                          "specified with the destination operand (first operand). The source operand " +
                                          "is a memory location, the address of which is read from either the DS:SI, " +
                                          "DS:ESI or the RSI registers (depending on the address-size attribute of the " +
                                          "instruction, 16, 32 or 64, respectively). (The DS segment may be overridden " +
                                          "with a segment override prefix.) The destination operand is an I/O port " +
                                          "address (from 0 to 65,535) that is read from the DX register. The size of " +
                                          "the I/O port being accessed (that is, the size of the source and destination " +
                                          "operands) is determined by the opcode for an 8-bit I/O port or by the " +
                                          "operand-size attribute of the instruction for a 16- or 32-bit I/O port.";
                        break;
                    case "pabsb":
                    case "vpabsb":
                    case "pabsw":
                    case "vpabsw":
                    case "pabsd":
                    case "vpabsd":
                    case "pabsq":
                        instructionInfo = "PABSB/W/D computes the absolute value of each data element of the source " +
                                          "operand (the second operand) and stores the UNSIGNED results in the " +
                                          "destination operand (the first operand). PABSB operates on signed bytes, " +
                                          "PABSW operates on signed 16-bit words, and PABSD operates on signed 32-bit integers.";
                        break;
                    case "packsswb":
                    case "vpacksswb":
                    case "packssdw":
                    case "vpackssdw":
                        instructionInfo = "Converts packed signed word integers into packed signed byte integers " +
                                          "(PACKSSWB) or converts packed signed doubleword integers into packed signed " +
                                          "word integers (PACKSSDW), using saturation to handle overflow conditions.";
                        break;
                    case "packusdw":
                    case "vpackusdw":
                        instructionInfo = "Converts packed signed doubleword integers in the first and second source " +
                                          "operands into packed unsigned word integers using unsigned saturation to " +
                                          "handle overflow conditions. If the signed doubleword value is beyond the " +
                                          "range of an unsigned word (that is, greater than FFFFH or less than 0000H), " +
                                          "the saturated unsigned word integer value of FFFFH or 0000H, respectively, " +
                                          "is stored in the destination.";
                        break;
                    case "packuswb":
                    case "vpackuswb":
                        instructionInfo = "Converts 4, 8, 16 or 32 signed word integers from the destination operand " +
                                          "(first operand) and 4, 8, 16 or 32 signed word integers from the source " +
                                          "operand (second operand) into 8, 16, 32 or 64 unsigned byte integers and " +
                                          "stores the result in the destination operand. If a signed word integer value " +
                                          "is beyond the range of an unsigned byte integer (that is, greater than FFH " +
                                          "or less than 00H), the saturated unsigned byte integer value of FFH or 00H, " +
                                          "respectively, is stored in the destination.";
                        break;
                    case "paddb":
                    case "vpaddb":
                    case "paddw":
                    case "vpaddw":
                    case "paddd":
                    case "vpaddd":
                    case "paddq":
                    case "vpaddq":
                        instructionInfo = "Performs a SIMD add of the packed integers from the source operand (second " +
                                          "operand) and the destination operand (first operand), and stores the packed " +
                                          "integer results in the destination operand. Overflow is handled with " +
                                          "wraparound, as described in the following paragraphs.";
                        break;
                    case "paddsb":
                    case "vpaddsb":
                    case "paddsw":
                    case "vpaddsw":
                        instructionInfo = "Performs a SIMD add of the packed signed integers from the source operand " +
                                          "(second operand) and the destination operand (first operand), and stores the " +
                                          "packed integer results in the destination operand. Overflow is handled with " +
                                          "signed saturation, as described in the following paragraphs.";
                        break;
                    case "paddusb":
                    case "vpaddusb":
                    case "paddusw":
                    case "vpaddusw":
                        instructionInfo = "Performs a SIMD add of the packed unsigned integers from the source operand " +
                                          "(second operand) and the destination operand (first operand), and stores the " +
                                          "packed integer results in the destination operand. Overflow is handled with " +
                                          "unsigned saturation, as described in the following paragraphs.";
                        break;
                    case "palignr":
                    case "vpalignr":
                        instructionInfo = "(V)PALIGNR concatenates the destination operand (the first operand) and the " +
                                          "source operand (the second operand) into an intermediate composite, shifts " +
                                          "the composite at byte granularity to the right by a constant immediate, and " +
                                          "extracts the right-aligned result into the destination. The first and the " +
                                          "second operands can be an MMX,";
                        break;
                    case "pand":
                    case "vpand":
                        instructionInfo = "Performs a bitwise logical AND operation on the first source operand and " +
                                          "second source operand and stores the result in the destination operand. Each " +
                                          "bit of the result is set to 1 if the corresponding bits of the first and " +
                                          "second operands are 1, otherwise it is set to 0.";
                        break;
                    case "pandn":
                    case "vpandn":
                        instructionInfo = "Performs a bitwise logical NOT operation on the first source operand, then " +
                                          "performs bitwise AND with second source operand and stores the result in the " +
                                          "destination operand. Each bit of the result is set to 1 if the corresponding " +
                                          "bit in the first operand is 0 and the corresponding bit in the second " +
                                          "operand is 1, otherwise it is set to 0.";
                        break;
                    case "pause":
                        instructionInfo = "Improves the performance of spin-wait loops. When executing a " +
                                          "\"spin-wait loop,\" processors will suffer a severe " +
                                          "performance penalty when exiting the loop because it detects a possible " +
                                          "memory order violation. The PAUSE instruction provides a hint to the " +
                                          "processor that the code sequence is a spin-wait loop. The processor uses this " +
                                          "hint to avoid the memory order violation in most situations, which greatly " +
                                          "improves processor performance. For this reason, it is recommended that a " +
                                          "PAUSE instruction be placed in all spin-wait loops.";
                        break;
                    case "pavgb":
                    case "vpavgb":
                    case "pavgw":
                    case "vpavgw":
                        instructionInfo = "Performs a SIMD average of the packed unsigned integers from the source " +
                                          "operand (second operand) and the destination operand (first operand), and " +
                                          "stores the results in the destination operand. For each corresponding pair " +
                                          "of data elements in the first and second operands, the elements are added " +
                                          "together, a 1 is added to the temporary sum, and that result is shifted " +
                                          "right one bit position.";
                        break;
                    case "pblendvb":
                    case "vpblendvb":
                        instructionInfo = "Conditionally copies byte elements from the source operand (second operand) " +
                                          "to the destination operand (first operand) depending on mask bits defined in " +
                                          "the implicit third register argument, XMM0. The mask bits are the most " +
                                          "significant bit in each byte element of the XMM0 register.";
                        break;
                    case "pblendw":
                    case "vpblendw":
                        instructionInfo = "Words from the source operand (second operand) are conditionally written to " +
                                          "the destination operand (first operand) depending on bits in the immediate " +
                                          "operand (third operand). The immediate bits (bits 7:0) form a mask that " +
                                          "determines whether the corresponding word in the destination is copied from " +
                                          "the source. If a bit in the mask, corresponding to a word, is \"1\", then " +
                                          "the word is copied, else the word element in the destination operand is unchanged.";
                        break;
                    case "pclmulqdq":
                    case "vpclmulqdq":
                        instructionInfo = "Performs a carry-less multiplication of two quadwords, selected from the " +
                                          "first source and second source operand according to the value of the " +
                                          "immediate byte. Bits 4 and 0 are used to select which 64-bit half of each " +
                                          "operand to use, other bits of the immediate byte are ignored.";
                        break;
                    case "pcmpeqb":
                    case "vpcmpeqb":
                    case "pcmpeqw":
                    case "vpcmpeqw":
                    case "pcmpeqd":
                    case "vpcmpeqd":
                        instructionInfo = "Performs a SIMD compare for equality of the packed bytes, words, or " +
                                          "doublewords in the destination operand (first operand) and the source " +
                                          "operand (second operand). If a pair of data elements is equal, the " +
                                          "corresponding data element in the destination operand is set to all 1s; " +
                                          "otherwise, it is set to all 0s.";
                        break;
                    case "pcmpeqq":
                    case "vpcmpeqq":
                        instructionInfo = "Performs an SIMD compare for equality of the packed quadwords in the " +
                                          "destination operand (first operand) and the source operand (second operand). " +
                                          "If a pair of data elements is equal, the corresponding data element in the " +
                                          "destination is set to all 1s; otherwise, it is set to 0s.";
                        break;
                    case "pcmpestri":
                    case "vpcmpestri":
                        instructionInfo = "The instruction compares and processes data from two string fragments based " +
                                          "on the encoded value in the Imm8 Control Byte, and generates an index stored " +
                                          "to the count register (ECX).";
                        break;
                    case "pcmpestrm":
                    case "vpcmpestrm":
                        instructionInfo = "The instruction compares data from two string fragments based on the encoded " +
                                          "value in the imm8 contol byte, " +
                                          "and generates a mask stored to XMM0.";
                        break;
                    case "pcmpgtb":
                    case "vpcmpgtb":
                    case "pcmpgtw":
                    case "vpcmpgtw":
                    case "pcmpgtd":
                    case "vpcmpgtd":
                        instructionInfo = "Performs an SIMD signed compare for the greater value of the packed byte, " +
                                          "word, or doubleword integers in the destination operand (first operand) and " +
                                          "the source operand (second operand). If a data element in the destination " +
                                          "operand is greater than the corresponding date element in the source operand, " +
                                          "the corresponding data element in the destination operand is set to all 1s; " +
                                          "otherwise, it is set to all 0s.";
                        break;
                    case "pcmpgtq":
                    case "vpcmpgtq":
                        instructionInfo = "Performs an SIMD signed compare for the packed quadwords in the destination " +
                                          "operand (first operand) and the source operand (second operand). If the data " +
                                          "element in the first (destination) operand is greater than the corresponding " +
                                          "element in the second (source) operand, the corresponding data element in " +
                                          "the destination is set to all 1s; otherwise, it is set to 0s.";
                        break;
                    case "pcmpistri":
                    case "vpcmpistri":
                        instructionInfo = "The instruction compares data from two strings based on the encoded value in " +
                                          "the Imm8 Control Byte, " +
                                          "and generates an index stored to ECX.";
                        break;
                    case "pcmpistrm":
                    case "vpcmpistrm":
                        instructionInfo = "The instruction compares data from two strings based on the encoded value in " +
                                          "the imm8 byte generating a mask stored to XMM0.";
                        break;
                    case "pdep":
                        instructionInfo = "PDEP uses a mask in the second source operand (the third operand) to " +
                                          "transfer/scatter contiguous low order bits in the first source operand " +
                                          "(the second operand) into the destination (the first operand). PDEP takes " +
                                          "the low bits from the first source operand and deposit them in the " +
                                          "destination operand at the corresponding bit locations that are set in the " +
                                          "second source operand (mask). All other bits (bits not set in mask) in " +
                                          "destination are set to zero.";
                        break;
                    case "pext":
                        instructionInfo = "PEXT uses a mask in the second source operand (the third operand) to transfer " +
                                          "either contiguous or non-contiguous bits in the first source operand (the " +
                                          "second operand) to contiguous low order bit positions in the destination " +
                                          "(the first operand). For each bit set in the MASK, PEXT extracts the " +
                                          "corresponding bits from the first source operand and writes them into " +
                                          "contiguous lower bits of destination operand. The remaining upper bits of " +
                                          "destination are zeroed.";
                        break;
                    case "pextrb":
                    case "vpextrb":
                    case "pextrd":
                    case "vpextrd":
                    case "pextrq":
                    case "vpextrq":
                        instructionInfo = "Extract a byte/dword/qword integer value from the source XMM register at a " +
                                          "byte/dword/qword offset determined from imm8[3:0]. The destination can be a " +
                                          "register or byte/dword/qword memory location. If the destination is a " +
                                          "register, the upper bits of the register are zero extended.";
                        break;
                    case "pextrw":
                    case "vpextrw":
                        instructionInfo = "Copies the word in the source operand (second operand) specified by the count " +
                                          "operand (third operand) to the destination operand (first operand). The " +
                                          "source operand can be an MMX technology register or an XMM register. The " +
                                          "destination operand can be the low word of a general-purpose register or a " +
                                          "16-bit memory address. The count operand is an 8-bit immediate. When " +
                                          "specifying a word location in an MMX technology register, the 2 " +
                                          "least-significant bits of the count operand specify the location; for an XMM " +
                                          "register, the 3 least-significant bits specify the location. The content of " +
                                          "the destination register above bit 16 is cleared (set to all 0s).";
                        break;
                    case "phaddw":
                    case "vphaddw":
                    case "phaddd":
                    case "vphaddd":
                        instructionInfo = "(V)PHADDW adds two adjacent 16-bit signed integers horizontally from the " +
                                          "source and destination operands and packs the 16-bit signed results to the " +
                                          "destination operand (first operand). (V)PHADDD adds two adjacent 32-bit " +
                                          "signed integers horizontally from the source and destination operands and " +
                                          "packs the 32-bit signed results to the destination operand (first operand). " +
                                          "When the source operand is a 128-bit memory operand, the operand must be " +
                                          "aligned on a 16-byte boundary or a general-protection exception (#GP) will be " +
                                          "generated.";
                        break;
                    case "phaddsw":
                    case "vphaddsw":
                        instructionInfo = "(V)PHADDSW adds two adjacent signed 16-bit integers horizontally from the " +
                                          "source and destination operands and saturates the signed results; packs the " +
                                          "signed, saturated 16-bit results to the destination operand (first operand) " +
                                          "When the source operand is a 128-bit memory operand, the operand must be " +
                                          "aligned on a 16-byte boundary or a general-protection exception (#GP) will be generated.";
                        break;
                    case "phminposuw":
                    case "vphminposuw":
                        instructionInfo = "Determine the minimum unsigned word value in the source operand (second operand) " +
                                          "and place the unsigned word in the low word (bits 0-15) of the destination " +
                                          "operand (first operand). The word index of the minimum value is stored in " +
                                          "bits 16-18 of the destination operand. The remaining upper bits of the " +
                                          "destination are set to zero.";
                        break;
                    case "phsubw":
                    case "vphsubw":
                    case "phsubd":
                    case "vphsubd":
                        instructionInfo = "(V)PHSUBW performs horizontal subtraction on each adjacent pair of 16-bit " +
                                          "signed integers by subtracting the most significant word from the least " +
                                          "significant word of each pair in the source and destination operands, and " +
                                          "packs the signed 16-bit results to the destination operand (first operand). " +
                                          "(V)PHSUBD performs horizontal subtraction on each adjacent pair of 32-bit " +
                                          "signed integers by subtracting the most significant doubleword from the " +
                                          "least significant doubleword of each pair, and packs the signed 32-bit " +
                                          "result to the destination operand. When the source operand is a 128-bit " +
                                          "memory operand, the operand must be aligned on a 16-byte boundary or a " +
                                          "general-protection exception (#GP) will be generated.";
                        break;
                    case "phsubsw":
                    case "vphsubsw":
                        instructionInfo = "(V)PHSUBSW performs horizontal subtraction on each adjacent pair of 16-bit " +
                                          "signed integers by subtracting the most significant word from the least " +
                                          "significant word of each pair in the source and destination operands. The " +
                                          "signed, saturated 16-bit results are packed to the destination operand " +
                                          "(first operand). When the source operand is a 128-bit memory operand, the " +
                                          "operand must be aligned on a 16-byte boundary or a general-protection " +
                                          "exception (#GP) will be generated.";
                        break;
                    case "pinsrb":
                    case "vpinsrb":
                    case "pinsrd":
                    case "vpinsrd":
                    case "pinsrq":
                    case "vpinsrq":
                        instructionInfo = "Copies a byte/dword/qword from the source operand (second operand) and " +
                                          "inserts it in the destination operand (first operand) at the location " +
                                          "specified with the count operand (third operand). (The other elements in the " +
                                          "destination register are left untouched.) The source operand can be a " +
                                          "general-purpose register or a memory location. (When the source operand is a " +
                                          "general-purpose register, PINSRB copies the low byte of the register.) " +
                                          "The destination operand is an XMM register. The count operand is an 8-bit " +
                                          "immediate. When specifying a qword[dword, byte] location in an XMM register, " +
                                          "the [2, 4] least-significant bit(s) of the count operand specify the location.";
                        break;
                    case "pinsrw":
                    case "vpinsrw":
                        instructionInfo = "Copies a word from the source operand (second operand) and inserts it in the " +
                                          "destination operand (first operand) at the location specified with the count " +
                                          "operand (third operand). (The other words in the destination register are " +
                                          "left untouched.) The source operand can be a general-purpose register or a " +
                                          "16-bit memory location. (When the source operand is a general-purpose " +
                                          "register, the low word of the register is copied.) The destination operand " +
                                          "can be an MMX technology register or an XMM register. The count operand is " +
                                          "an 8-bit immediate. When specifying a word location in an MMX technology " +
                                          "register, the 2 least-significant bits of the count operand specify the " +
                                          "location; for an XMM register, the 3 least-significant bits specify the location.";
                        break;
                    case "pmaddubsw":
                    case "vpmaddubsw":
                        instructionInfo = "(V)PMADDUBSW multiplies vertically each unsigned byte of the destination " +
                                          "operand (first operand) with the corresponding signed byte of the source " +
                                          "operand (second operand), producing intermediate signed 16-bit integers. " +
                                          "Each adjacent pair of signed words is added and the saturated result is " +
                                          "packed to the destination operand. For example, the lowest-order bytes " +
                                          "(bits 7-0) in the source and destination operands are multiplied and the " +
                                          "intermediate signed word result is added with the corresponding intermediate " +
                                          "result from the 2nd lowest-order bytes (bits 15-8) of the operands; the " +
                                          "sign-saturated result is stored in the lowest word of the destination " +
                                          "register (15-0). The same operation is performed on the other pairs of " +
                                          "adjacent bytes. Both operands can be MMX register or XMM registers. When the " +
                                          "source operand is a 128-bit memory operand, the operand must be aligned on a " +
                                          "16-byte boundary or a general-protection exception (#GP) will be generated.";
                        break;
                    case "pmaddwd":
                    case "vpmaddwd":
                        instructionInfo = "Multiplies the individual signed words of the destination operand " +
                                          "(first operand) by the corresponding signed words of the source operand " +
                                          "(second operand), producing temporary signed, doubleword results. The " +
                                          "adjacent double-word results are then summed and stored in the destination " +
                                          "operand. For example, the corresponding low-order words (15-0) and (31-16) " +
                                          "in the source and destination operands are multiplied by one another and the " +
                                          "double-word results are added together and stored in the low doubleword of " +
                                          "the destination register (31-0). The same operation is performed on the " +
                                          "other pairs of adjacent words.";
                        break;
                    case "pmaxsb":
                    case "vpmaxsb":
                    case "pmaxsw":
                    case "vpmaxsw":
                    case "pmaxsd":
                    case "vpmaxsd":
                    case "pmaxsq":
                        instructionInfo = "Performs a SIMD compare of the packed signed byte, word, dword or qword " +
                                          "integers in the second source operand and the first source operand and " +
                                          "returns the maximum value for each pair of integers to the destination operand.";
                        break;
                    case "pmaxub":
                    case "vpmaxub":
                    case "pmaxuw":
                    case "vpmaxuw":
                        instructionInfo = "Performs a SIMD compare of the packed unsigned byte, word integers in the " +
                                          "second source operand and the first source operand and returns the maximum " +
                                          "value for each pair of integers to the destination operand.";
                        break;
                    case "pmaxud":
                    case "vpmaxud":
                    case "pmaxuq":
                        instructionInfo = "Performs a SIMD compare of the packed unsigned dword or qword integers in " +
                                          "the second source operand and the first source operand and returns the " +
                                          "maximum value for each pair of integers to the destination operand.";
                        break;
                    case "pminsb":
                    case "vpminsb":
                    case "pminsw":
                    case "vpminsw":
                        instructionInfo = "Performs a SIMD compare of the packed signed byte, word, or dword integers " +
                                          "in the second source operand and the first source operand and returns the " +
                                          "minimum value for each pair of integers to the destination operand.";
                        break;
                    case "pminsd":
                    case "vpminsd":
                    case "pminsq":
                        instructionInfo = "Performs a SIMD compare of the packed signed dword or qword integers in the " +
                                          "second source operand and the first source operand and returns the minimum " +
                                          "value for each pair of integers to the destination operand.";
                        break;
                    case "pminub":
                    case "vpminub":
                    case "pminuw":
                    case "vpminuw":
                        instructionInfo = "Performs a SIMD compare of the packed unsigned byte or word integers in the " +
                                          "second source operand and the first source operand and returns the minimum " +
                                          "value for each pair of integers to the destination operand.";
                        break;
                    case "pminud":
                    case "vpminud":
                    case "pminuq":
                        instructionInfo = "Performs a SIMD compare of the packed unsigned dword/qword integers in the " +
                                          "second source operand and the first source operand and returns the minimum " +
                                          "value for each pair of integers to the destination operand.";
                        break;
                    case "pmovmskb":
                    case "vpmovmskb":
                        instructionInfo = "Creates a mask made up of the most significant bit of each byte of the source " +
                                          "operand (second operand) and stores the result in the low byte or word of " +
                                          "the destination operand (first operand).";
                        break;
                    case "pmovsx":
                        instructionInfo = "Legacy and VEX encoded versions: Packed byte, word, or dword integers in the " +
                                          "low bytes of the source operand (second operand) are sign extended to word, " +
                                          "dword, or quadword integers and stored in packed signed bytes the destination operand.";
                        break;
                    case "vpmovsxbw":
                        instructionInfo = "Sign extend 8 packed 8-bit integers in the low 8 bytes of xmm2/m64 to 8 " +
                                          "packed 16-bit integers in xmm1.";
                        break;
                    case "vpmovsxbd":
                        instructionInfo = "Sign extend 4 packed 8-bit integers in the low 4 bytes of xmm2/m32 to 4 " +
                                          "packed 32-bit integers in xmm1.";
                        break;
                    case "vpmovsxbq":
                        instructionInfo = "Sign extend 2 packed 8-bit integers in the low 2 bytes of xmm2/m16 to 2 " +
                                          "packed 64-bit integers in xmm1.";
                        break;
                    case "pmovsxwd":
                        instructionInfo = "Sign extend 4 packed 16-bit integers in the low 8 bytes of xmm2/m64 to 4 " +
                                          "packed 32-bit integers in xmm1.";
                        break;
                    case "pmovsxwq":
                        instructionInfo = "Sign extend 2 packed 16-bit integers in the low 4 bytes of xmm2/m32 to 2 " +
                                          "packed 64-bit integers in xmm1.";
                        break;
                    case "vpmovsxdq":
                        instructionInfo = "Sign extend 2 packed 32-bit integers in the low 8 bytes of xmm2/m64 to 2 " +
                                          "packed 64-bit integers in xmm1.";
                        break;
                    case "vpmovsxwd":
                        instructionInfo = "Sign extend packed 16-bit integers in the low bytes of xmm2/m128 to packed " +
                                          "32-bit integers in ymm1.";
                        break;
                    case "vpmovsxwq":
                        instructionInfo = "Sign extend packed 16-bit integers in the low bytes of xmm2/m64 to packed " +
                                          "64-bit integers in ymm1.";
                        break;
                    case "pmovzx":
                        instructionInfo = "Legacy, VEX and EVEX encoded versions: Packed byte, word, or dword integers " +
                                          "starting from the low bytes of the source operand (second operand) are zero " +
                                          "extended to word, dword, or quadword integers and stored in packed signed " +
                                          "bytes the destination operand.";
                        break;
                    case "pmovzxbw":
                    case "pmovzxbd":
                    case "pmovzxbq":
                    case "pmovzxwd":
                    case "pmovzxwq":
                    case "pmovzxdq":
                    case "vpmovzxbw":
                    case "vpmovzxbd":
                    case "vpmovzxbq":
                    case "vpmovzxwd":
                    case "vpmovzxwq":
                    case "vpmovzxdq":
                        instructionInfo = "Packed move with zero extend.";
                        break;
                    case "pmuldq":
                    case "vpmuldq":
                        instructionInfo = "Multiplies packed signed doubleword integers in the even-numbered " +
                                          "(zero-based reference) elements of the first source operand with the packed " +
                                          "signed doubleword integers in the corresponding elements of the second source " +
                                          "operand and stores packed signed quadword results in the destination operand.";
                        break;
                    case "pmulhrsw":
                    case "vpmulhrsw":
                        instructionInfo = "PMULHRSW multiplies vertically each signed 16-bit integer from the " +
                                          "destination operand (first operand) with the corresponding signed 16-bit " +
                                          "integer of the source operand (second operand), producing intermediate, " +
                                          "signed 32-bit integers. Each intermediate 32-bit integer is truncated to the " +
                                          "18 most significant bits. Rounding is always performed by adding 1 to the " +
                                          "least significant bit of the 18-bit intermediate result. The final result is " +
                                          "obtained by selecting the 16 bits immediately to the right of the most " +
                                          "significant bit of each 18-bit intermediate result and packed to the destination operand.";
                        break;
                    case "pmulhuw":
                    case "vpmulhuw":
                        instructionInfo = "Performs a SIMD unsigned multiply of the packed unsigned word integers in the " +
                                          "destination operand (first operand) and the source operand (second operand), " +
                                          "and stores the high 16 bits of each 32-bit intermediate results in the destination operand.";
                        break;
                    case "pmulhw":
                    case "vpmulhw":
                        instructionInfo = "Performs a SIMD signed multiply of the packed signed word integers in the " +
                                          "destination operand (first operand) and the source operand (second operand), " +
                                          "and stores the high 16 bits of each intermediate 32-bit result in the destination operand.";
                        break;
                    case "pmulld":
                    case "vpmulld":
                    case "pmullq":
                        instructionInfo = "Performs a SIMD signed multiply of the packed signed dword/qword integers " +
                                          "from each element of the first source operand with the corresponding element " +
                                          "in the second source operand. The low 32/64 bits of each 64/128-bit " +
                                          "intermediate results are stored to the destination operand.";
                        break;
                    case "pmullw":
                    case "vpmullw":
                        instructionInfo = "Performs a SIMD signed multiply of the packed signed word integers in the " +
                                          "destination operand (first operand) and the source operand (second operand), " +
                                          "and stores the low 16 bits of each intermediate 32-bit result in the destination operand.";
                        break;
                    case "pmuludq":
                    case "vpmuludq":
                        instructionInfo = "Multiplies the first operand (destination operand) by the second operand " +
                                          "(source operand) and stores the result in the destination operand.";
                        break;
                    case "pop":
                        instructionInfo = "Loads the value from the top of the stack to the location specified with the " +
                                          "destination operand (or explicit opcode) and then increments the stack pointer. " +
                                          "The destination operand can be a general-purpose register, memory location, or segment register.";
                        break;
                    case "popa":
                    case "popad":
                        instructionInfo = "Pops doublewords (POPAD) or words (POPA) from the stack into the " +
                                          "general-purpose registers. The registers are loaded in the following order: " +
                                          "EDI, ESI, EBP, EBX, EDX, ECX, and EAX (if the operand-size attribute is 32) " +
                                          "and DI, SI, BP, BX, DX, CX, and AX (if the operand-size attribute is 16). " +
                                          "(These instructions reverse the operation of the PUSHA/PUSHAD instructions.) " +
                                          "The value on the stack for the ESP or SP register is ignored. Instead, the " +
                                          "ESP or SP register is incremented after each register is loaded.";
                        break;
                    case "popcnt":
                        instructionInfo = "This instruction calculates the number of bits set to 1 in the second " +
                                          "operand (source) and returns the count in the first operand (a destination " +
                                          "register).";
                        break;
                    case "popf":
                    case "popfd":
                    case "popfq":
                        instructionInfo = "Pops a doubleword (POPFD) from the top of the stack (if the current " +
                                          "operand-size attribute is 32) and stores the value in the EFLAGS register, " +
                                          "or pops a word from the top of the stack (if the operand-size attribute is 16) " +
                                          "and stores it in the lower 16 bits of the EFLAGS register (that is, the FLAGS " +
                                          "register). These instructions reverse the operation of the PUSHF/PUSHFD/PUSHFQ instructions.";
                        break;
                    case "por":
                    case "vpor":
                        instructionInfo = "Performs a bitwise logical OR operation on the source operand " +
                                          "(second operand) and the destination operand (first operand) and stores the " +
                                          "result in the destination operand. Each bit of the result is set to 1 if " +
                                          "either or both of the corresponding bits of the first and second operands are " +
                                          "1; otherwise, it is set to 0.";
                        break;
                    case "prefetchw":
                        instructionInfo = "Fetches the cache line of data from memory that contains the byte specified " +
                                          "with the source operand to a location in the 1st or 2nd level cache and " +
                                          "invalidates other cached instances of the line.";
                        break;
                    case "prefetcht0":
                    case "prefetcht1":
                    case "prefetcht2":
                    case "prefetchnta":
                        instructionInfo = "Fetches the line of data from memory that contains the byte specified with " +
                                          "the source operand to a location in the cache hierarchy specified by a locality hint:";
                        break;
                    case "psadbw":
                    case "vpsadbw":
                        instructionInfo = "Computes the absolute value of the difference of 8 unsigned byte integers " +
                                          "from the source operand (second operand) and from the destination operand " +
                                          "(first operand). These 8 differences are then summed to produce an unsigned " +
                                          "word integer result that is stored in the destination operand.";
                        break;
                    case "pshufb":
                    case "vpshufb":
                        instructionInfo = "PSHUFB performs in-place shuffles of bytes in the destination operand (the " +
                                          "first operand) according to the shuffle control mask in the source operand " +
                                          "(the second operand). The instruction permutes the data in the destination " +
                                          "operand, leaving the shuffle mask unaffected. If the most significant bit " +
                                          "(bit[7]) of each byte of the shuffle control mask is set, then constant zero " +
                                          "is written in the result byte. Each byte in the shuffle control mask forms an " +
                                          "index to permute the corresponding byte in the destination operand. The value " +
                                          "of each index is the least significant 4 bits (128-bit operation) or 3 bits " +
                                          "(64-bit operation) of the shuffle control byte. When the source operand is a " +
                                          "128-bit memory operand, the operand must be aligned on a 16-byte boundary or " +
                                          "a general-protection exception (#GP) will be generated.";
                        break;
                    case "pshufd":
                    case "vpshufd":
                        instructionInfo = "Copies doublewords from source operand (second operand) and inserts them in " +
                                          "the destination operand (first operand) at the locations selected with the " +
                                          "order operand (third operand). Each 2-bit field in the order operand selects " +
                                          "the contents of one doubleword location within a 128-bit lane and copy to the " +
                                          "target element in the destination operand. For example, bits 0 and 1 of the " +
                                          "order operand targets the first doubleword element in the low and high 128-bit " +
                                          "lane of the destination operand for 256-bit VPSHUFD. The encoded value of bits " +
                                          "1:0 of the order operand determines which doubleword element (from the " +
                                          "respective 128-bit lane) of the source operand will be copied to doubleword 0 " +
                                          "of the destination operand.";
                        break;
                    case "pshufhw":
                    case "vpshufhw":
                        instructionInfo = "Copies words from the high quadword of a 128-bit lane of the source operand " +
                                          "and inserts them in the high quadword of the destination operand at word " +
                                          "locations (of the respective lane) selected with the immediate operand. This " +
                                          "256-bit operation is similar to the in-lane operation used by the 256-bit " +
                                          "VPSHUFD instruction. For 128-bit operation, only the low 128-bit lane is " +
                                          "operative. Each 2-bit field in the immediate operand selects the contents of " +
                                          "one word location in the high quadword of the destination operand. The binary " +
                                          "encodings of the immediate operand fields select words (0, 1, 2 or 3, 4) from " +
                                          "the high quadword of the source operand to be copied to the destination operand. " +
                                          "The low quadword of the source operand is copied to the low quadword of the " +
                                          "destination operand, for each 128-bit lane.";
                        break;
                    case "pshuflw":
                    case "vpshuflw":
                        instructionInfo = "Copies words from the low quadword of a 128-bit lane of the source operand " +
                                          "and inserts them in the low quadword of the destination operand at word " +
                                          "locations (of the respective lane) selected with the immediate operand. The " +
                                          "256-bit operation is similar to the in-lane operation used by the 256-bit " +
                                          "VPSHUFD instruction. For 128-bit operation, only the low 128-bit lane is " +
                                          "operative. Each 2-bit field in the immediate operand selects the contents of " +
                                          "one word location in the low quadword of the destination operand. The binary " +
                                          "encodings of the immediate operand fields select words (0, 1, 2 or 3) from the " +
                                          "low quadword of the source operand to be copied to the destination operand. " +
                                          "The high quadword of the source operand is copied to the high quadword of the " +
                                          "destination operand, for each 128-bit lane.";
                        break;
                    case "pshufw":
                        instructionInfo = "Copies words from the source operand (second operand) and inserts them in the " +
                                          "destination operand (first operand) at word locations selected with the order " +
                                          "operand (third operand). This operation is similar to the operation used by " +
                                          "the PSHUFD instruction. For the PSHUFW instruction, each 2-bit field in the " +
                                          "order operand selects the contents of one word location in the destination " +
                                          "operand. The encodings of the order operand fields select words from the source " +
                                          "operand to be copied to the destination operand.";
                        break;
                    case "psignb":
                    case "vpsignb":
                    case "psignw":
                    case "vpsignw":
                    case "psignd":
                    case "vpsignd":
                        instructionInfo = "(V)PSIGNB/(V)PSIGNW/(V)PSIGND negates each data element of the destination " +
                                          "operand (the first operand) if the signed integer value of the corresponding " +
                                          "data element in the source operand (the second operand) is less than zero. " +
                                          "If the signed integer value of a data element in the source operand is positive, " +
                                          "the corresponding data element in the destination operand is unchanged. " +
                                          "If a data element in the source operand is zero, the corresponding data element " +
                                          "in the destination operand is set to zero.";
                        break;
                    case "psllw":
                    case "vpsllw":
                    case "pslld":
                    case "vpslld":
                    case "psllq":
                    case "vpsllq":
                        instructionInfo = "Shifts the bits in the individual data elements (words, doublewords, or " +
                                          "quadword) in the destination operand (first operand) to the left by the " +
                                          "number of bits specified in the count operand (second operand). As the bits " +
                                          "in the data elements are shifted left, the empty low-order bits are cleared " +
                                          "(set to 0). If the value specified by the count operand is greater than 15 " +
                                          "(for words), 31 (for doublewords), or 63 (for a quadword), then the destination " +
                                          "operand is set to all 0s.";
                        break;
                    case "pslldq":
                    case "vpslldq":
                        instructionInfo = "Shifts the destination operand (first operand) to the left by the number of " +
                                          "bytes specified in the count operand (second operand). The empty low-order " +
                                          "bytes are cleared (set to all 0s). If the value specified by the count operand " +
                                          "is greater than 15, the destination operand is set to all 0s. The count " +
                                          "operand is an 8-bit immediate.";
                        break;
                    case "psraw":
                    case "vpsraw":
                    case "psrad":
                    case "vpsrad":
                    case "psraq":
                        instructionInfo = "Shifts the bits in the individual data elements (words, doublewords or quadwords) " +
                                          "in the destination operand (first operand) to the right by the number of bits " +
                                          "specified in the count operand (second operand). As the bits in the data " +
                                          "elements are shifted right, the empty high-order bits are filled with the initial " +
                                          "value of the sign bit of the data element. If the value specified by the count " +
                                          "operand is greater than 15 (for words), 31 (for doublewords), or 63 " +
                                          "(for quadwords), each destination data element is filled with the initial value " +
                                          "of the sign bit of the element.)";
                        break;
                    case "psrlw":
                    case "vpsrlw":
                    case "psrld":
                    case "vpsrld":
                    case "psrlq":
                    case "vpsrlq":
                        instructionInfo = "Shifts the bits in the individual data elements (words, doublewords, or " +
                                          "quadword) in the destination operand (first operand) to the right by the number " +
                                          "of bits specified in the count operand (second operand). As the bits in the " +
                                          "data elements are shifted right, the empty high-order bits are cleared (set to 0). " +
                                          "If the value specified by the count operand is greater than 15 (for words), " +
                                          "31 (for doublewords), or 63 (for a quadword), then the destination operand is set to all 0s.";
                        break;
                    case "psrldq":
                    case "vpsrldq":
                        instructionInfo = "Shifts the destination operand (first operand) to the right by the number of " +
                                          "bytes specified in the count operand (second operand). The empty high-order " +
                                          "bytes are cleared (set to all 0s). If the value specified by the count operand " +
                                          "is greater than 15, the destination operand is set to all 0s. The count " +
                                          "operand is an 8-bit immediate.";
                        break;
                    case "psubb":
                    case "vpsubb":
                    case "psubw":
                    case "vpsubw":
                    case "psubd":
                    case "vpsubd":
                        instructionInfo = "Performs a SIMD subtract of the packed integers of the source operand " +
                                          "(second operand) from the packed integers of the destination operand " +
                                          "(first operand), and stores the packed integer results in the destination operand. " +
                                          "Overflow is handled with wraparound, as described in the following paragraphs.";
                        break;
                    case "psubq":
                    case "vpsubq":
                        instructionInfo = "Subtracts the second operand (source operand) from the first operand " +
                                          "(destination operand) and stores the result in the destination operand. " +
                                          "When packed quadword operands are used, a SIMD subtract is performed. " +
                                          "When a quadword result is too large to be represented in 64 bits (overflow), " +
                                          "the result is wrapped around and the low 64 bits are written to the " +
                                          "destination element (that is, the carry is ignored).";
                        break;
                    case "psubsb":
                    case "vpsubsb":
                    case "psubsw":
                    case "vpsubsw":
                        instructionInfo = "Performs a SIMD subtract of the packed signed integers of the source operand " +
                                          "(second operand) from the packed signed integers of the destination operand " +
                                          "(first operand), and stores the packed integer results in the destination operand. " +
                                          "Overflow is handled with signed saturation, as described in the following paragraphs.";
                        break;
                    case "psubsiw":
                        instructionInfo = "Word packed subtract second operand from the first operand with saturaiont using implied destination.";
                        break;
                    case "psubusb":
                    case "vpsubusb":
                    case "psubusw":
                    case "vpsubusw":
                        instructionInfo = "Performs a SIMD subtract of the packed unsigned integers of the source operand " +
                                          "(second operand) from the packed unsigned integers of the destination operand " +
                                          "(first operand), and stores the packed unsigned integer results in the destination operand. " +
                                          "Overflow is handled with unsigned saturation, as described in the following paragraphs.";
                        break;
                    case "ptest":
                    case "vptest":
                        instructionInfo = "PTEST and VPTEST set the ZF flag if all bits in the result are 0 of the " +
                                          "bitwise AND of the first source operand (first operand) and the second source " +
                                          "operand (second operand). VPTEST sets the CF flag if all bits in the result " +
                                          "are 0 of the bitwise AND of the second source operand (second operand) and the " +
                                          "logical NOT of the destination operand.";
                        break;
                    case "ptwrite":
                        instructionInfo = "This instruction reads data in the source operand and sends it to the Intel " +
                                          "Processor Trace hardware to be encoded in a PTW packet if TriggerEn, " +
                                          "ContextEn, FilterEn, and PTWEn are all set to 1. The size of data is 64-bit " +
                                          "if using REX.W in 64-bit mode, otherwise 32-bits of data are copied from the source operand.";
                        break;
                    case "punpckhbw":
                    case "vpunpckhbw":
                    case "punpckhwd":
                    case "vpunpckhwd":
                    case "punpckhdq":
                    case "vpunpckhdq":
                    case "punpckhqdq":
                    case "vpunpckhqdq":
                        instructionInfo = "Unpacks and interleaves the high-order data elements (bytes, words, " +
                                          "doublewords, or quadwords) of the destination operand (first operand) and " +
                                          "source operand (second operand) into the destination operand. The low-order " +
                                          "data elements are ignored.";
                        break;
                    case "punpcklbw":
                    case "vpunpcklbw":
                    case "punpcklwd":
                    case "vpunpcklwd":
                    case "punpckldq":
                    case "vpunpckldq":
                    case "punpcklqdq":
                    case "vpunpcklqdq":
                        instructionInfo = "Unpacks and interleaves the low-order data elements (bytes, words, " +
                                          "doublewords, and quadwords) of the destination operand (first operand) and " +
                                          "source operand (second operand) into the destination operand. The high-order " +
                                          "data elements are ignored.";
                        break;
                    case "push":
                        instructionInfo = "Decrements the stack pointer and then stores the source operand on the top of " +
                                          "the stack.";
                        break;
                    case "pusha":
                    case "pushad":
                        instructionInfo = "Pushes the contents of the general-purpose registers onto the stack. " +
                                          "The registers are stored on the stack in the following order: EAX, ECX, EDX, " +
                                          "EBX, ESP (original value), EBP, ESI, and EDI (if the current operand-size " +
                                          "attribute is 32) and AX, CX, DX, BX, SP (original value), BP, SI, and DI " +
                                          "(if the operand-size attribute is 16). These instructions perform the reverse " +
                                          "operation of the POPA/POPAD instructions. The value pushed for the ESP or SP " +
                                          "register is its value before prior to pushing the first register.";
                        break;
                    case "pushf":
                    case "pushfd":
                    case "pushfq":
                        instructionInfo = "Decrements the stack pointer by 4 (if the current operand-size attribute is 32) " +
                                          "and pushes the entire contents of the EFLAGS register onto the stack, or " +
                                          "decrements the stack pointer by 2 (if the operand-size attribute is 16) and " +
                                          "pushes the lower 16 bits of the EFLAGS register (that is, the FLAGS register) " +
                                          "onto the stack. These instructions reverse the operation of the POPF/POPFD instructions.";
                        break;
                    case "pxor":
                    case "vpxor":
                        instructionInfo = "Performs a bitwise logical exclusive-OR (XOR) operation on the source " +
                                          "operand (second operand) and the destination operand (first operand) and " +
                                          "stores the result in the destination operand. Each bit of the result is 1 if " +
                                          "the corresponding bits of the two operands are different; each bit is 0 if " +
                                          "the corresponding bits of the operands are the same.";
                        break;
                    case "rcl":
                    case "rcr":
                    case "rol":
                    case "ror":
                        instructionInfo = "Shifts (rotates) the bits of the first operand (destination operand) the " +
                                          "number of bit positions specified in the second operand (count operand) and " +
                                          "stores the result in the destination operand. The destination operand can be " +
                                          "a register or a memory location; the count operand is an unsigned integer that " +
                                          "can be an immediate or a value in the CL register. The count is masked to 5 " +
                                          "bits (or 6 bits if in 64-bit mode and REX.W = 1).";
                        break;
                    case "rcpps":
                    case "vrcpps":
                        instructionInfo = "Performs a SIMD computation of the approximate reciprocals of the four packed " +
                                          "single-precision floating-point values in the source operand (second operand) " +
                                          "stores the packed single-precision floating-point results in the destination " +
                                          "operand. The source operand can be an XMM register or a 128-bit memory location. " +
                                          "The destination operand is an XMM register.";
                        break;
                    case "rcpss":
                    case "vrcpss":
                        instructionInfo = "Computes of an approximate reciprocal of the low single-precision " +
                                          "floating-point value in the source operand (second operand) and stores the " +
                                          "single-precision floating-point result in the destination operand. The source " +
                                          "operand can be an XMM register or a 32-bit memory location. The destination " +
                                          "operand is an XMM register. The three high-order doublewords of the destination " +
                                          "operand remain unchanged.";
                        break;
                    case "rdfsbase":
                    case "rdgsbase":
                        instructionInfo = "Loads the general-purpose register indicated by the modR/M:r/m field with the " +
                                          "FS or GS segment base address.";
                        break;
                    case "rdmsr":
                        instructionInfo = "Reads the contents of a 64-bit model specific register (MSR) specified in the " +
                                          "ECX register into registers EDX:EAX. (On processors that support the Intel 64 " +
                                          "architecture, the high-order 32 bits of RCX are ignored.) The EDX register is " +
                                          "loaded with the high-order 32 bits of the MSR and the EAX register is loaded " +
                                          "with the low-order 32 bits. (On processors that support the Intel 64 architecture, " +
                                          "the high-order 32 bits of each of RAX and RDX are cleared.) If fewer than 64 bits " +
                                          "are implemented in the MSR being read, the values returned to EDX:EAX in " +
                                          "unimplemented bit locations are undefined.";
                        break;
                    case "rdpid":
                        instructionInfo = "Reads the value of the IA32_TSC_AUX MSR (address C0000103H) into the destination " +
                                          "register. The value of CS.D and operand-size prefixes (66H and REX.W) do not " +
                                          "affect the behavior of the RDPID instruction.";
                        break;
                    case "rdpkru":
                        instructionInfo = "Reads the value of PKRU into EAX and clears EDX. ECX must be 0 when RDPKRU " +
                                          "is executed; otherwise, a general-protection exception (#GP) occurs.";
                        break;
                    case "rdpmc":
                        instructionInfo = "The EAX register is loaded with the low-order 32 bits. The EDX register is " +
                                          "loaded with the supported high-order bits of the counter. The number of " +
                                          "high-order bits loaded into EDX is implementation specific on processors that " +
                                          "do no support architectural performance monitoring. The width of fixed-function " +
                                          "and general-purpose performance counters on processors supporting architectural " +
                                          "performance monitoring are reported by CPUID 0AH leaf. See below for the " +
                                          "treatment of the EDX register for \xe2\x80\x9cfast\xe2\x80\x9d reads.";
                        break;
                    case "rdrand":
                        instructionInfo = "Loads a hardware generated random value and store it in the destination register. " +
                                          "The size of the random value is determined by the destination register size and " +
                                          "operating mode. The Carry Flag indicates whether a random value is available at " +
                                          "the time the instruction is executed. CF=1 indicates that the data in the " +
                                          "destination is valid. Otherwise CF=0 and the data in the destination operand " +
                                          "will be returned as zeros for the specified width. All other flags are forced " +
                                          "to 0 in either situation. Software must check the state of CF=1 for determining " +
                                          "if a valid random value has been returned, otherwise it is expected to loop " +
                                          "and retry execution of RDRAND.";
                        break;
                    case "rdseed":
                        instructionInfo = "Loads a hardware generated random value and store it in the destination register. " +
                                          "The random value is generated from an Enhanced NRBG (Non Deterministic " +
                                          "Random Bit Generator) that is compliant to NIST SP800-90B and NIST SP800-90C " +
                                          "in the XOR construction mode. The size of the random value is determined by " +
                                          "the destination register size and operating mode. The Carry Flag indicates " +
                                          "whether a random value is available at the time the instruction is executed. " +
                                          "CF=1 indicates that the data in the destination is valid. Otherwise CF=0 and " +
                                          "the data in the destination operand will be returned as zeros for the specified width. " +
                                          "All other flags are forced to 0 in either situation. Software must check the " +
                                          "state of CF=1 for determining if a valid random seed value has been returned, " +
                                          "otherwise it is expected to loop and retry execution of RDSEED (see Section 1.2).";
                        break;
                    case "rdtsc":
                        instructionInfo = "Reads the current value of the processor\xe2\x80\x99s time-stamp counter " +
                                          "(a 64-bit MSR) into the EDX:EAX registers. The EDX register is loaded with " +
                                          "the high-order 32 bits of the MSR and the EAX register is loaded with the " +
                                          "low-order 32 bits. (On processors that support the Intel 64 architecture, " +
                                          "the high-order 32 bits of each of RAX and RDX are cleared.)";
                        break;
                    case "rdtscp":
                        instructionInfo = "Reads the current value of the processor\xe2\x80\x99s time-stamp counter " +
                                          "(a 64-bit MSR) into the EDX:EAX registers and also reads the value of the " +
                                          "IA32_TSC_AUX MSR (address C0000103H) into the ECX register. The EDX register " +
                                          "is loaded with the high-order 32 bits of the IA32_TSC MSR; the EAX register " +
                                          "is loaded with the low-order 32 bits of the IA32_TSC MSR; and the ECX register " +
                                          "is loaded with the low-order 32-bits of IA32_TSC_AUX MSR. On processors that " +
                                          "support the Intel 64 architecture, the high-order 32 bits of each of RAX, RDX, " +
                                          "and RCX are cleared.";
                        break;
                    case "rep":
                    case "repe":
                    case "repz":
                    case "repne":
                    case "repnz":
                        instructionInfo = "Repeats a string instruction the number of times specified in the count " +
                                          "register or until the indicated condition of the ZF flag is no longer met. " +
                                          "The REP (repeat), REPE (repeat while equal), REPNE (repeat while not equal), " +
                                          "REPZ (repeat while zero), and REPNZ (repeat while not zero) mnemonics are " +
                                          "prefixes that can be added to one of the string instructions. The REP prefix " +
                                          "can be added to the INS, OUTS, MOVS, LODS, and STOS instructions, and the REPE, " +
                                          "REPNE, REPZ, and REPNZ prefixes can be added to the CMPS and SCAS instructions. " +
                                          "(The REPZ and REPNZ prefixes are synonymous forms of the REPE and REPNE prefixes, " +
                                          "respectively.) The F3H prefix is defined for the following instructions and undefined for the rest:";
                        break;
                    case "ret":
                        instructionInfo = "Transfers program control to a return address located on the top of the stack. " +
                                          "The address is usually placed on the stack by a CALL instruction, and the " +
                                          "return is made to the instruction that follows the CALL instruction.";
                        break;
                    case "rorx":
                        instructionInfo = "Rotates the bits of second operand right by the count value specified in imm8 " +
                                          "without affecting arithmetic flags. The RORX instruction does not read or " +
                                          "write the arithmetic flags.";
                        break;
                    case "roundpd":
                    case "vroundpd":
                        instructionInfo = "Round the 2 double-precision floating-point values in the source operand " +
                                          "(second operand) using the rounding mode specified in the immediate operand " +
                                          "(third operand) and place the results in the destination operand (first operand). " +
                                          "The rounding process rounds each input floating-point value to an integer value " +
                                          "and returns the integer result as a double-precision floating-point value.";
                        break;
                    case "roundps":
                    case "vroundps":
                        instructionInfo = "Round the 4 single-precision floating-point values in the source operand " +
                                          "(second operand) using the rounding mode specified in the immediate operand " +
                                          "(third operand) and place the results in the destination operand (first operand). " +
                                          "The rounding process rounds each input floating-point value to an integer " +
                                          "value and returns the integer result as a single-precision floating-point value.";
                        break;
                    case "roundsd":
                    case "vroundsd":
                        instructionInfo = "Round the DP FP value in the lower qword of the source operand (second operand) " +
                                          "using the rounding mode specified in the immediate operand (third operand) " +
                                          "and place the result in the destination operand (first operand). The rounding " +
                                          "process rounds a double-precision floating-point input to an integer value " +
                                          "and returns the integer result as a double precision floating-point value in " +
                                          "the lowest position. The upper double precision floating-point value in the " +
                                          "destination is retained.";
                        break;
                    case "roundss":
                    case "vroundss":
                        instructionInfo = "Round the single-precision floating-point value in the lowest dword of the " +
                                          "source operand (second operand) using the rounding mode specified in the " +
                                          "immediate operand (third operand) and place the result in the destination " +
                                          "operand (first operand). The rounding process rounds a single-precision " +
                                          "floating-point input to an integer value and returns the result as a " +
                                          "single-precision floating-point value in the lowest position. The upper three " +
                                          "single-precision floating-point values in the destination are retained.";
                        break;
                    case "rsm":
                        instructionInfo = "Returns program control from system management mode (SMM) to the application " +
                                          "program or operating-system procedure that was interrupted when the processor " +
                                          "received an SMM interrupt. The processor\xe2\x80\x99s state is restored from " +
                                          "the dump created upon entering SMM. If the processor detects invalid state " +
                                          "information during state restoration, it enters the shutdown state. " +
                                          "The following invalid information can cause a shutdown:";
                        break;
                    case "rsqrtps":
                    case "vrsqrtps":
                        instructionInfo = "Performs a SIMD computation of the approximate reciprocals of the square " +
                                          "roots of the four packed single-precision floating-point values in the source " +
                                          "operand (second operand) and stores the packed single-precision floating-point " +
                                          "results in the destination operand. The source operand can be an XMM register " +
                                          "or a 128-bit memory location. The destination operand is an XMM register.";
                        break;
                    case "rsqrtss":
                    case "vrsqrtss":
                        instructionInfo = "Computes an approximate reciprocal of the square root of the low single-precision " +
                                          "floating-point value in the source operand (second operand) stores the " +
                                          "single-precision floating-point result in the destination operand. " +
                                          "The source operand can be an XMM register or a 32-bit memory location. " +
                                          "The destination operand is an XMM register. The three high-order doublewords " +
                                          "of the destination operand remain unchanged.";
                        break;
                    case "sahf":
                        instructionInfo = "Loads the SF, ZF, AF, PF, and CF flags of the EFLAGS register with values " +
                                          "from the corresponding bits in the AH register (bits 7, 6, 4, 2, and 0, respectively). " +
                                          "Bits 1, 3, and 5 of register AH are ignored; the corresponding reserved bits " +
                                          "(1, 3, and 5) in the EFLAGS register remain.";
                        break;
                    case "sal":
                    case "sar":
                    case "shl":
                    case "shr":
                        instructionInfo = "Shifts the bits in the first operand (destination operand) to the left or " +
                                          "right by the number of bits specified in the second operand (count operand). " +
                                          "Bits shifted beyond the destination operand boundary are first shifted into " +
                                          "the CF flag, then discarded. At the end of the shift operation, the CF flag " +
                                          "contains the last bit shifted out of the destination operand.";
                        break;
                    case "sarx":
                    case "shlx":
                    case "shrx":
                        instructionInfo = "Shifts the bits of the first source operand (the second operand) to the left " +
                                          "or right by a COUNT value specified in the second source operand (the third operand). " +
                                          "The result is written to the destination operand (the first operand).";
                        break;
                    case "sbb":
                        instructionInfo = "Adds the source operand (second operand) and the carry (CF) flag, and subtracts " +
                                          "the result from the destination operand (first operand). The result of the " +
                                          "subtraction is stored in the destination operand. The destination operand can " +
                                          "be a register or a memory location; the source operand can be an immediate, a " +
                                          "register, or a memory location. (However, two memory operands cannot be used " +
                                          "in one instruction.) The state of the CF flag represents a borrow from a previous subtraction.";
                        break;
                    case "scas":
                    case "scasb":
                    case "scasw":
                    case "scasd":
                        instructionInfo = "In non-64-bit modes and in default 64-bit mode: this instruction compares a " +
                                          "byte, word, doubleword or quadword specified using a memory operand with the " +
                                          "value in AL, AX, or EAX. It then sets status flags in EFLAGS recording the " +
                                          "results. The memory operand address is read from ES:(E)DI register (depending " +
                                          "on the address-size attribute of the instruction and the current operational mode). " +
                                          "Note that ES cannot be overridden with a segment override prefix.";
                        break;
                    case "seta":
                    case "setae":
                    case "setb":
                    case "setbe":
                    case "setc":
                    case "sete":
                    case "setg":
                    case "setge":
                    case "setl":
                    case "setle":
                    case "setna":
                    case "setnae":
                    case "setnb":
                    case "setnbe":
                    case "setnc":
                    case "setne":
                    case "setng":
                    case "setnge":
                    case "setnl":
                    case "setnle":
                    case "setno":
                    case "setnp":
                    case "setns":
                    case "setnz":
                    case "seto":
                    case "setp":
                    case "setpe":
                    case "setpo":
                    case "sets":
                    case "setz":
                        instructionInfo = "Sets the destination operand to 0 or 1 depending on the settings of the status " +
                                          "flags (CF, SF, OF, ZF, and PF) in the EFLAGS register. The destination operand " +
                                          "points to a byte register or a byte in memory. " +
                                          "The condition code suffix (cc) indicates the condition being tested for.";
                        break;
                    case "sfence":
                        instructionInfo = "Orders processor execution relative to all memory stores prior to the SFENCE instruction. " +
                                          "The processor ensures that every store prior to SFENCE is globally visible before " +
                                          "any store after SFENCE becomes globally visible. The SFENCE instruction is " +
                                          "ordered with respect to memory stores, other SFENCE instructions, MFENCE instructions, " +
                                          "and any serializing instructions (such as the CPUID instruction). It is not " +
                                          "ordered with respect to memory loads or the LFENCE instruction.";
                        break;
                    case "sgdt":
                        instructionInfo = "Stores the content of the global descriptor table register (GDTR) in the " +
                                          "destination operand. The destination operand specifies a memory location.";
                        break;
                    case "sha1msg1":
                        instructionInfo = "The SHA1MSG1 instruction is one of two SHA1 message scheduling instructions. " +
                                          "The instruction performs an intermediate calculation for the next four SHA1 message dwords.";
                        break;
                    case "sha1msg2":
                        instructionInfo = "The SHA1MSG2 instruction is one of two SHA1 message scheduling instructions. " +
                                          "The instruction performs the final calculation to derive the next four SHA1 message dwords.";
                        break;
                    case "sha1nexte":
                        instructionInfo = "The SHA1NEXTE calculates the SHA1 state variable E after four rounds of " +
                                          "operation from the current SHA1 state variable A in the destination operand. " +
                                          "The calculated value of the SHA1 state variable E is added to the source operand, " +
                                          "which contains the scheduled dwords.";
                        break;
                    case "sha1rnds4":
                        instructionInfo = "The SHA1RNDS4 instruction performs four rounds of SHA1 operation using an " +
                                          "initial SHA1 state (A,B,C,D) from the first operand (which is a source operand " +
                                          "and the destination operand) and some pre-computed sum of the next 4 round " +
                                          "message dwords, and state variable E from the second operand (a source operand). " +
                                          "The updated SHA1 state (A,B,C,D) after four rounds of processing is stored in the destination operand.";
                        break;
                    case "sha256msg1":
                        instructionInfo = "The SHA256MSG1 instruction is one of two SHA256 message scheduling instructions. " +
                                          "The instruction performs an intermediate calculation for the next four SHA256 message dwords.";
                        break;
                    case "sha256msg2":
                        instructionInfo = "The SHA256MSG2 instruction is one of two SHA2 message scheduling instructions. " +
                                          "The instruction performs the final calculation for the next four SHA256 message dwords.";
                        break;
                    case "sha256rnds2":
                        instructionInfo = "The SHA256RNDS2 instruction performs 2 rounds of SHA256 operation using an " +
                                          "initial SHA256 state (C,D,G,H) from the first operand, an initial SHA256 state " +
                                          "(A,B,E,F) from the second operand, and a pre-computed sum of the next 2 round " +
                                          "message dwords and the corresponding round constants from the implicit operand xmm0. " +
                                          "Note that only the two lower dwords of XMM0 are used by the instruction.";
                        break;
                    case "shld":
                        instructionInfo = "The SHLD instruction is used for multi-precision shifts of 64 bits or more.";
                        break;
                    case "shrd":
                        instructionInfo = "The SHRD instruction is useful for multi-precision shifts of 64 bits or more.";
                        break;
                    case "shufpd":
                    case "vshufpd":
                        instructionInfo = "Selects a double-precision floating-point value of an input pair using a bit " +
                                          "control and move to a designated element of the destination operand. " +
                                          "The low-to-high order of double-precision element of the destination operand " +
                                          "is interleaved between the first source operand and the second source operand " +
                                          "at the granularity of input pair of 128 bits. Each bit in the imm8 byte, " +
                                          "starting from bit 0, is the select control of the corresponding element of the " +
                                          "destination to received the shuffled result of an input pair.";
                        break;
                    case "shufps":
                    case "vshufps":
                        instructionInfo = "Selects a single-precision floating-point value of an input quadruplet using " +
                                          "a two-bit control and move to a designated element of the destination operand. " +
                                          "Each 64-bit element-pair of a 128-bit lane of the destination operand is " +
                                          "interleaved between the corresponding lane of the first source operand and the " +
                                          "second source operand at the granularity 128 bits. Each two bits in the imm8 byte, " +
                                          "starting from bit 0, is the select control of the corresponding element of a " +
                                          "128-bit lane of the destination to received the shuffled result of an input quadruplet. " +
                                          "The two lower elements of a 128-bit lane in the destination receives shuffle " +
                                          "results from the quadruple of the first source operand. The next two elements " +
                                          "of the destination receives shuffle results from the quadruple of the second source operand.";
                        break;
                    case "sidt":
                        instructionInfo = "Stores the content the interrupt descriptor table register (IDTR) in the " +
                                          "destination operand. The destination operand specifies a 6-byte memory location.";
                        break;
                    case "sldt":
                        instructionInfo = "Stores the segment selector from the local descriptor table register (LDTR) " +
                                          "in the destination operand. The destination operand can be a general-purpose " +
                                          "register or a memory location. The segment selector stored with this instruction " +
                                          "points to the segment descriptor (located in the GDT) for the current LDT. " +
                                          "This instruction can only be executed in protected mode.";
                        break;
                    case "smsw":
                        instructionInfo = "Stores the machine status word (bits 0 through 15 of control register CR0) " +
                                          "into the destination operand. The destination operand can be a general-purpose " +
                                          "register or a memory location.";
                        break;
                    case "sqrtpd":
                    case "vsqrtpd":
                        instructionInfo = "Performs a SIMD computation of the square roots of the two, four or eight " +
                                          "packed double-precision floating-point values in the source operand " +
                                          "(the second operand) stores the packed double-precision floating-point results " +
                                          "in the destination operand (the first operand).";
                        break;
                    case "sqrtps":
                    case "vsqrtps":
                        instructionInfo = "Performs a SIMD computation of the square roots of the four, eight or sixteen " +
                                          "packed single-precision floating-point values in the source operand " +
                                          "(second operand) stores the packed single-precision floating-point results in the destination operand.";
                        break;
                    case "sqrtsd":
                    case "vsqrtsd":
                        instructionInfo = "Computes the square root of the low double-precision floating-point value in " +
                                          "the second source operand and stores the double-precision floating-point result " +
                                          "in the destination operand. The second source operand can be an XMM register " +
                                          "or a 64-bit memory location. The first source and destination operands are XMM registers.";
                        break;
                    case "sqrtss":
                    case "vsqrtss":
                        instructionInfo = "Computes the square root of the low single-precision floating-point value in " +
                                          "the second source operand and stores the single-precision floating-point result " +
                                          "in the destination operand. The second source operand can be an XMM register " +
                                          "or a 32-bit memory location. The first source and destination operands is an XMM register.";
                        break;
                    case "stac":
                        instructionInfo = "Sets the AC flag bit in EFLAGS register. This may enable alignment checking " +
                                          "of user-mode data accesses. This allows explicit supervisor-mode data accesses " +
                                          "to user-mode pages even if the SMAP bit is set in the CR4 register.";
                        break;
                    case "stc":
                        instructionInfo = "Sets the CF flag in the EFLAGS register. Operation is the same in all modes.";
                        break;
                    case "std":
                        instructionInfo = "Sets the DF flag in the EFLAGS register. When the DF flag is set to 1, string " +
                                          "operations decrement the index registers (ESI and/or EDI). Operation is the same in all modes.";
                        break;
                    case "sti":
                        instructionInfo = "In most cases, STI sets the interrupt flag (IF) in the EFLAGS register. " +
                                          "This allows the processor to respond to maskable hardware interrupts.";
                        break;
                    case "stmxcsr":
                    case "vstmxcsr":
                        instructionInfo = "Stores the contents of the MXCSR control and status register to the destination " +
                                          "operand. The destination operand is a 32-bit memory location. The reserved bits " +
                                          "in the MXCSR register are stored as 0s.";
                        break;
                    case "stos":
                    case "stosb":
                    case "stosw":
                    case "stosd":
                    case "stosq":
                        instructionInfo = "In non-64-bit and default 64-bit mode; stores a byte, word, or doubleword " +
                                          "from the AL, AX, or EAX register (respectively) into the destination operand. " +
                                          "The destination operand is a memory location, the address of which is read " +
                                          "from either the ES:EDI or ES:DI register (depending on the address-size " +
                                          "attribute of the instruction and the mode of operation). The ES segment cannot " +
                                          "be overridden with a segment override prefix.";
                        break;
                    case "str":
                        instructionInfo = "Stores the segment selector from the task register (TR) in the destination operand. " +
                                          "The destination operand can be a general-purpose register or a memory location. " +
                                          "The segment selector stored with this instruction points to the task state segment " +
                                          "(TSS) for the currently running task.";
                        break;
                    case "sub":
                        instructionInfo = "Subtracts the second operand (source operand) from the first operand " +
                                          "(destination operand) and stores the result in the destination operand. " +
                                          "The destination operand can be a register or a memory location; the source " +
                                          "operand can be an immediate, register, or memory location. (However, two memory " +
                                          "operands cannot be used in one instruction.) When an immediate value is used " +
                                          "as an operand, it is sign-extended to the length of the destination operand format.";
                        break;
                    case "subpd":
                    case "vsubpd":
                        instructionInfo = "Performs a SIMD subtract of the two, four or eight packed double-precision " +
                                          "floating-point values of the second Source operand from the first Source " +
                                          "operand, and stores the packed double-precision floating-point results in the " +
                                          "destination operand.";
                        break;
                    case "subps":
                    case "vsubps":
                        instructionInfo = "Performs a SIMD subtract of the packed single-precision floating-point values " +
                                          "in the second Source operand from the First Source operand, and stores the " +
                                          "packed single-precision floating-point results in the destination operand.";
                        break;
                    case "subsd":
                    case "vsubsd":
                        instructionInfo = "Subtract the low double-precision floating-point value in the second source " +
                                          "operand from the first source operand and stores the double-precision " +
                                          "floating-point result in the low quadword of the destination operand.";
                        break;
                    case "subss":
                    case "vsubss":
                        instructionInfo = "Subtract the low single-precision floating-point value from the second source " +
                                          "operand and the first source operand and store the double-precision " +
                                          "floating-point result in the low doubleword of the destination operand.";
                        break;
                    case "swapgs":
                        instructionInfo = "SWAPGS exchanges the current GS base register value with the value contained " +
                                          "in MSR address C0000102H (IA32_KERNEL_GS_BASE). The SWAPGS instruction is a " +
                                          "privileged instruction intended for use by system software.";
                        break;
                    case "syscall":
                        instructionInfo = "SYSCALL invokes an OS system-call handler at privilege level 0. It does so " +
                                          "by loading RIP from the IA32_LSTAR MSR (after saving the address of the " +
                                          "instruction following SYSCALL into RCX). (The WRMSR instruction ensures that " +
                                          "the IA32_LSTAR MSR always contain a canonical address.)";
                        break;
                    case "sysenter":
                        instructionInfo = "Executes a fast call to a level 0 system procedure or routine. SYSENTER is a " +
                                          "companion instruction to SYSEXIT. The instruction is optimized to provide the " +
                                          "maximum performance for system calls from user code running at privilege level " +
                                          "3 to operating system or executive procedures running at privilege level 0.";
                        break;
                    case "sysexit":
                        instructionInfo = "Executes a fast return to privilege level 3 user code. SYSEXIT is a companion " +
                                          "instruction to the SYSENTER instruction. The instruction is optimized to provide " +
                                          "the maximum performance for returns from system procedures executing at " +
                                          "protections levels 0 to user procedures executing at protection level 3. It must " +
                                          "be executed from code executing at privilege level 0.";
                        break;
                    case "sysret":
                        instructionInfo = "SYSRET is a companion instruction to the SYSCALL instruction. It returns from " +
                                          "an OS system-call handler to user code at privilege level 3. It does so by " +
                                          "loading RIP from RCX and loading RFLAGS from R11.<sup>1</sup> With a 64-bit " +
                                          "operand size, SYSRET remains in 64-bit mode; otherwise, it enters compatibility " +
                                          "mode and only the low 32 bits of the registers are loaded.";
                        break;
                    case "test":
                        instructionInfo = "Computes the bit-wise logical AND of first operand (source 1 operand) and the " +
                                          "second operand (source 2 operand) and sets the SF, ZF, and PF status flags " +
                                          "according to the result. The result is then discarded.";
                        break;
                    case "tpause":
                        instructionInfo = "TPAUSE instructs the processor to enter an implementation-dependent optimized state. " +
                                          "There are two such optimized states to choose from: light-weight power/performance " +
                                          "optimized state, and improved power/performance optimized state. " +
                                          "The selection between the two is governed by the explicit input register bit[0] source operand.";
                        break;
                    case "tzcnt":
                        instructionInfo = "TZCNT counts the number of trailing least significant zero bits in source " +
                                          "operand (second operand) and returns the result in destination operand (first operand). " +
                                          "TZCNT is an extension of the BSF instruction. The key difference between " +
                                          "TZCNT and BSF instruction is that TZCNT provides operand size as output when " +
                                          "source operand is zero while in the case of BSF instruction, if source operand " +
                                          "is zero, the content of destination operand are undefined. On processors that " +
                                          "do not support TZCNT, the instruction byte encoding is executed as BSF.";
                        break;
                    case "ucomisd":
                    case "vucomisd":
                        instructionInfo = "Performs an unordered compare of the double-precision floating-point values " +
                                          "in the low quadwords of operand 1 (first operand) and operand 2 (second operand), " +
                                          "and sets the ZF, PF, and CF flags in the EFLAGS register according to the result " +
                                          "(unordered, greater than, less than, or equal). The OF, SF and AF flags in the " +
                                          "EFLAGS register are set to 0. The unordered result is returned if either source " +
                                          "operand is a NaN (QNaN or SNaN).";
                        break;
                    case "ucomiss":
                    case "vucomiss":
                        instructionInfo = "Compares the single-precision floating-point values in the low doublewords of " +
                                          "operand 1 (first operand) and operand 2 (second operand), and sets the ZF, PF, " +
                                          "and CF flags in the EFLAGS register according to the result (unordered, " +
                                          "greater than, less than, or equal). The OF, SF and AF flags in the EFLAGS " +
                                          "register are set to 0. The unordered result is returned if either source " +
                                          "operand is a NaN (QNaN or SNaN).";
                        break;
                    case "ud":
                        instructionInfo = "Generates an invalid opcode exception. This instruction is provided for " +
                                          "software testing to explicitly generate an invalid opcode exception. The " +
                                          "opcodes for this instruction are reserved for this purpose.";
                        break;
                    case "umonitor":
                        instructionInfo = "The UMONITOR instruction arms address monitoring hardware using an address " +
                                          "specified in the source register (the address range that the monitoring " +
                                          "hardware checks for store operations can be determined by using the CPUID " +
                                          "monitor leaf function, EAX=05H). A store to an address within the specified " +
                                          "address range triggers the monitoring hardware. The state of monitor hardware " +
                                          "is used by UMWAIT.";
                        break;
                    case "umwait":
                        instructionInfo = "UMWAIT instructs the processor to enter an implementation-dependent optimized " +
                                          "state while monitoring a range of addresses. The optimized state may be " +
                                          "either a light-weight power/performance optimized state or an improved " +
                                          "power/performance optimized state. The selection between the two states is " +
                                          "governed by the explicit input register bit[0] source operand.";
                        break;
                    case "unpckhpd":
                    case "vunpckhpd":
                        instructionInfo = "Performs an interleaved unpack of the high double-precision floating-point " +
                                          "values from the first source operand and the second source operand.";
                        break;
                    case "unpckhps":
                    case "vunpckhps":
                        instructionInfo = "Performs an interleaved unpack of the high single-precision floating-point " +
                                          "values from the first source operand and the second source operand.";
                        break;
                    case "unpcklpd":
                    case "vunpcklpd":
                        instructionInfo = "Performs an interleaved unpack of the low double-precision floating-point " +
                                          "values from the first source operand and the second source operand.";
                        break;
                    case "unpcklps":
                    case "vunpcklps":
                        instructionInfo = "Performs an interleaved unpack of the low single-precision floating-point " +
                                          "values from the first source operand and the second source operand.";
                        break;
                    case "valignd":
                    case "valignq":
                        instructionInfo = "Concatenates and shifts right doubleword/quadword elements of the first " +
                                          "source operand (the second operand) and the second source operand " +
                                          "(the third operand) into a 1024/512/256-bit intermediate vector. " +
                                          "The low 512/256/128-bit of the intermediate vector is written to the " +
                                          "destination operand (the first operand) using the writemask k1. " +
                                          "The destination and first source operands are ZMM/YMM/XMM registers. " +
                                          "The second source operand can be a ZMM/YMM/XMM register, a 512/256/128-bit " +
                                          "memory location or a 512/256/128-bit vector broadcasted from a 32/64-bit memory location.";
                        break;
                    case "vblendmpd":
                    case "vblendmps":
                        instructionInfo = "Performs an element-by-element blending between float64/float32 elements in " +
                                          "the first source operand (the second operand) with the elements in the second " +
                                          "source operand (the third operand) using an opmask register as select control. " +
                                          "The blended result is written to the destination register.";
                        break;
                    case "vbroadcast":
                    case "vbroadcastsd":
                    case "vbroadcastss":
                    case "vbroadcastf128":
                    case "vbroadcasti128":
                        instructionInfo = "Load floating-point values as one tuple from the source operand " +
                                          "(second operand) in memory and broadcast to all elements of the destination " +
                                          "operand (first operand).";
                        break;
                    case "vcompresspd":
                        instructionInfo = "Compress (store) up to 8 double-precision floating-point values from the " +
                                          "source operand (the second operand) as a contiguous vector to the destination " +
                                          "operand (the first operand) The source operand is a ZMM/YMM/XMM register, " +
                                          "the destination operand can be a ZMM/YMM/XMM register or a 512/256/128-bit memory location.";
                        break;
                    case "vcompressps":
                        instructionInfo = "Compress (stores) up to 16 single-precision floating-point values from the " +
                                          "source operand (the second operand) to the destination operand (the first operand). " +
                                          "The source operand is a ZMM/YMM/XMM register, the destination operand can be a " +
                                          "ZMM/YMM/XMM register or a 512/256/128-bit memory location.";
                        break;
                    case "vcvtpd2qq":
                        instructionInfo = "Converts packed double-precision floating-point values in the source operand " +
                                          "(second operand) to packed quadword integers in the destination operand (first operand).";
                        break;
                    case "vcvtpd2udq":
                        instructionInfo = "Converts packed double-precision floating-point values in the source operand " +
                                          "(the second operand) to packed unsigned doubleword integers in the destination " +
                                          "operand (the first operand).";
                        break;
                    case "vcvtpd2uqq":
                        instructionInfo = "Converts packed double-precision floating-point values in the source operand " +
                                          "(second operand) to packed unsigned quadword integers in the destination " +
                                          "operand (first operand).";
                        break;
                    case "vcvtph2ps":
                        instructionInfo = "Converts packed half precision (16-bits) floating-point values in the " +
                                          "low-order bits of the source operand (the second operand) to packed " +
                                          "single-precision floating-point values and writes the converted values into " +
                                          "the destination operand (the first operand).";
                        break;
                    case "vcvtps2ph":
                        instructionInfo = "Convert packed single-precision floating values in the source operand to " +
                                          "half-precision (16-bit) floating-point values and store to the destination operand. " +
                                          "The rounding mode is specified using the immediate field (imm8).";
                        break;
                    case "vcvtps2qq":
                        instructionInfo = "Converts eight packed single-precision floating-point values in the source " +
                                          "operand to eight signed quadword integers in the destination operand.";
                        break;
                    case "vcvtps2udq":
                        instructionInfo = "Converts sixteen packed single-precision floating-point values in the source " +
                                          "operand to sixteen unsigned double-word integers in the destination operand.";
                        break;
                    case "vcvtps2uqq":
                        instructionInfo = "Converts up to eight packed single-precision floating-point values in the " +
                                          "source operand to unsigned quadword integers in the destination operand.";
                        break;
                    case "vcvtqq2pd":
                        instructionInfo = "Converts packed quadword integers in the source operand (second operand) to " +
                                          "packed double-precision floating-point values in the destination operand (first operand).";
                        break;
                    case "vcvtqq2ps":
                        instructionInfo = "Converts packed quadword integers in the source operand (second operand) to " +
                                          "packed single-precision floating-point values in the destination operand (first operand).";
                        break;
                    case "vcvtsd2usi":
                        instructionInfo = "Converts a double-precision floating-point value in the source operand " +
                                          "(the second operand) to an unsigned doubleword integer in the destination " +
                                          "operand (the first operand). The source operand can be an XMM register or a " +
                                          "64-bit memory location. The destination operand is a general-purpose register. " +
                                          "When the source operand is an XMM register, the double-precision floating-point " +
                                          "value is contained in the low quadword of the register.";
                        break;
                    case "vcvtss2usi":
                        instructionInfo = "Converts a single-precision floating-point value in the source operand " +
                                          "(the second operand) to an unsigned double-word integer (or unsigned quadword " +
                                          "integer if operand size is 64 bits) in the destination operand (the first operand). " +
                                          "The source operand can be an XMM register or a memory location. " +
                                          "The destination operand is a general-purpose register. When the source operand " +
                                          "is an XMM register, the single-precision floating-point value is contained " +
                                          "in the low doubleword of the register.";
                        break;
                    case "vcvttpd2qq":
                        instructionInfo = "Converts with truncation packed double-precision floating-point values in " +
                                          "the source operand (second operand) to packed quadword integers in the " +
                                          "destination operand (first operand).";
                        break;
                    case "vcvttpd2udq":
                        instructionInfo = "Converts with truncation packed double-precision floating-point values in " +
                                          "the source operand (the second operand) to packed unsigned doubleword integers " +
                                          "in the destination operand (the first operand).";
                        break;
                    case "vcvttpd2uqq":
                        instructionInfo = "Converts with truncation packed double-precision floating-point values in " +
                                          "the source operand (second operand) to packed unsigned quadword integers in " +
                                          "the destination operand (first operand).";
                        break;
                    case "vcvttps2qq":
                        instructionInfo = "Converts with truncation packed single-precision floating-point values in " +
                                          "the source operand to eight signed quadword integers in the destination operand.";
                        break;
                    case "vcvttps2udq":
                        instructionInfo = "Converts with truncation packed single-precision floating-point values in " +
                                          "the source operand to sixteen unsigned doubleword integers in the destination operand.";
                        break;
                    case "vcvttps2uqq":
                        instructionInfo = "Converts with truncation up to eight packed single-precision floating-point " +
                                          "values in the source operand to unsigned quadword integers in the destination operand.";
                        break;
                    case "vcvttsd2usi":
                        instructionInfo = "Converts with truncation a double-precision floating-point value in the " +
                                          "source operand (the second operand) to an unsigned doubleword integer " +
                                          "(or unsigned quadword integer if operand size is 64 bits) in the destination " +
                                          "operand (the first operand). The source operand can be an XMM register or a " +
                                          "64-bit memory location. The destination operand is a general-purpose register. " +
                                          "When the source operand is an XMM register, the double-precision floating-point " +
                                          "value is contained in the low quadword of the register.";
                        break;
                    case "vcvttss2usi":
                        instructionInfo = "Converts with truncation a single-precision floating-point value in the " +
                                          "source operand (the second operand) to an unsigned doubleword integer " +
                                          "(or unsigned quadword integer if operand size is 64 bits) in the destination " +
                                          "operand (the first operand). The source operand can be an XMM register or a " +
                                          "memory location. The destination operand is a general-purpose register. " +
                                          "When the source operand is an XMM register, the single-precision floating-point " +
                                          "value is contained in the low doubleword of the register.";
                        break;
                    case "vcvtudq2pd":
                        instructionInfo = "Converts packed unsigned doubleword integers in the source operand (second " +
                                          "operand) to packed double-precision floating-point values in the destination " +
                                          "operand (first operand).";
                        break;
                    case "vcvtudq2ps":
                        instructionInfo = "Converts packed unsigned doubleword integers in the source operand (second " +
                                          "operand) to single-precision floating-point values in the destination operand " +
                                          "(first operand).";
                        break;
                    case "vcvtuqq2pd":
                        instructionInfo = "Converts packed unsigned quadword integers in the source operand (second " +
                                          "operand) to packed double-precision floating-point values in the destination " +
                                          "operand (first operand).";
                        break;
                    case "vcvtuqq2ps":
                        instructionInfo = "Converts packed unsigned quadword integers in the source operand (second " +
                                          "operand) to single-precision floating-point values in the destination operand " +
                                          "(first operand).";
                        break;
                    case "vcvtusi2sd":
                        instructionInfo = "Converts an unsigned doubleword integer (or unsigned quadword integer if " +
                                          "operand size is 64 bits) in the second source operand to a double-precision " +
                                          "floating-point value in the destination operand. The result is stored in the " +
                                          "low quadword of the destination operand. When conversion is inexact, the value " +
                                          "returned is rounded according to the rounding control bits in the MXCSR register.";
                        break;
                    case "vcvtusi2ss":
                        instructionInfo = "Converts a unsigned doubleword integer (or unsigned quadword integer if " +
                                          "operand size is 64 bits) in the source operand (second operand) to a " +
                                          "single-precision floating-point value in the destination operand (first operand). " +
                                          "The source operand can be a general-purpose register or a memory location. " +
                                          "The destination operand is an XMM register. The result is stored in the low " +
                                          "doubleword of the destination operand. When a conversion is inexact, the value " +
                                          "returned is rounded according to the rounding control bits in the MXCSR " +
                                          "register or the embedded rounding control bits.";
                        break;
                    case "vdbpsadbw":
                        instructionInfo = "Compute packed SAD (sum of absolute differences) word results of unsigned " +
                                          "bytes from two 32-bit dword elements. Packed SAD word results are calculated " +
                                          "in multiples of qword superblocks, producing 4 SAD word results in each " +
                                          "64-bit superblock of the destination register.";
                        break;
                    case "verr":
                    case "verw":
                        instructionInfo = "Verifies whether the code or data segment specified with the source operand " +
                                          "is readable (VERR) or writable (VERW) from the current privilege level (CPL). " +
                                          "The source operand is a 16-bit register or a memory location that contains " +
                                          "the segment selector for the segment to be verified. If the segment is " +
                                          "accessible and readable (VERR) or writable (VERW), the ZF flag is set; " +
                                          "otherwise, the ZF flag is cleared. Code segments are never verified as writable. " +
                                          "This check cannot be performed on system segments.";
                        break;
                    case "vexpandpd":
                        instructionInfo = "Expand (load) up to 8/4/2, contiguous, double-precision floating-point values " +
                                          "of the input vector in the source operand (the second operand) to sparse " +
                                          "elements in the destination operand (the first operand) selected by the writemask k1.";
                        break;
                    case "vexpandps":
                        instructionInfo = "Expand (load) up to 16/8/4, contiguous, single-precision floating-point values " +
                                          "of the input vector in the source operand (the second operand) to sparse " +
                                          "elements of the destination operand (the first operand) selected by the writemask k1.";
                        break;
                    case "vextractf128":
                    case "vextractf32x4":
                    case "vextractf64x2":
                    case "vextractf32x8":
                    case "vextractf64x4":
                        instructionInfo = "VEXTRACTF128/VEXTRACTF32x4 and VEXTRACTF64x2 extract 128-bits of single-precision " +
                                          "floating-point values from the source operand (the second operand) and store " +
                                          "to the low 128-bit of the destination operand (the first operand). The 128-bit " +
                                          "data extraction occurs at an 128-bit granular offset specified by imm8[0] " +
                                          "(256-bit) or imm8[1:0] as the multiply factor. The destination may be either " +
                                          "a vector register or an 128-bit memory location.";
                        break;
                    case "vextracti128":
                    case "vextracti32x4":
                    case "vextracti64x2":
                    case "vextracti32x8":
                    case "vextracti64x4":
                        instructionInfo = "VEXTRACTI128/VEXTRACTI32x4 and VEXTRACTI64x2 extract 128-bits of doubleword " +
                                          "integer values from the source operand (the second operand) and store to the " +
                                          "low 128-bit of the destination operand (the first operand). The 128-bit data " +
                                          "extraction occurs at an 128-bit granular offset specified by imm8[0] (256-bit) " +
                                          "or imm8[1:0] as the multiply factor. The destination may be either a vector " +
                                          "register or an 128-bit memory location.";
                        break;
                    case "vfixupimmpd":
                        instructionInfo = "Perform fix-up of quad-word elements encoded in double-precision floating-point " +
                                          "format in the first source operand (the second operand) using a 32-bit, " +
                                          "two-level look-up table specified in the corresponding quadword element of " +
                                          "the second source operand (the third operand) with exception reporting specifier " +
                                          "imm8. The elements that are fixed-up are selected by mask bits of 1 specified " +
                                          "in the opmask k1. Mask bits of 0 in the opmask k1 or table response action of " +
                                          "0000b preserves the corresponding element of the first operand. The fixed-up " +
                                          "elements from the first source operand and the preserved element in the first " +
                                          "operand are combined as the final results in the destination operand (the first operand).";
                        break;
                    case "vfixupimmps":
                        instructionInfo = "Perform fix-up of doubleword elements encoded in single-precision floating-point " +
                                          "format in the first source operand (the second operand) using a 32-bit, " +
                                          "two-level look-up table specified in the corresponding doubleword element of " +
                                          "the second source operand (the third operand) with exception reporting specifier imm8. " +
                                          "The elements that are fixed-up are selected by mask bits of 1 specified in " +
                                          "the opmask k1. Mask bits of 0 in the opmask k1 or table response action of 0000b " +
                                          "preserves the corresponding element of the first operand. The fixed-up elements " +
                                          "from the first source operand and the preserved element in the first operand " +
                                          "are combined as the final results in the destination operand (the first operand).";
                        break;
                    case "vfixupimmsd":
                        instructionInfo = "Perform a fix-up of the low quadword element encoded in double-precision " +
                                          "floating-point format in the first source operand (the second operand) using " +
                                          "a 32-bit, two-level look-up table specified in the low quadword element of the " +
                                          "second source operand (the third operand) with exception reporting specifier imm8. " +
                                          "The element that is fixed-up is selected by mask bit of 1 specified in the " +
                                          "opmask k1. Mask bit of 0 in the opmask k1 or table response action of 0000b " +
                                          "preserves the corresponding element of the first operand. The fixed-up element " +
                                          "from the first source operand or the preserved element in the first operand " +
                                          "becomes the low quadword element of the destination operand (the first operand). " +
                                          "Bits 127:64 of the destination operand is copied from the corresponding bits " +
                                          "of the first source operand. The destination and first source operands are " +
                                          "XMM registers. The second source operand can be a XMM register or a 64- bit " +
                                          "memory location.";
                        break;
                    case "vfixupimmss":
                        instructionInfo = "Perform a fix-up of the low doubleword element encoded in single-precision " +
                                          "floating-point format in the first source operand (the second operand) using " +
                                          "a 32-bit, two-level look-up table specified in the low doubleword element of " +
                                          "the second source operand (the third operand) with exception reporting specifier " +
                                          "imm8. The element that is fixed-up is selected by mask bit of 1 specified in " +
                                          "the opmask k1. Mask bit of 0 in the opmask k1 or table response action of 0000b " +
                                          "preserves the corresponding element of the first operand. The fixed-up element " +
                                          "from the first source operand or the preserved element in the first operand " +
                                          "becomes the low doubleword element of the destination operand (the first " +
                                          "operand) Bits 127:32 of the destination operand is copied from the corresponding " +
                                          "bits of the first source operand. The destination and first source operands " +
                                          "are XMM registers. The second source operand can be a XMM register or a 32-bit " +
                                          "memory location.";
                        break;
                    case "vfmadd123pd":
                    case "vfmadd132pd":
                    case "vfmadd213pd":
                    case "vfmadd231pd":
                    case "vfmadd321pd":
                    case "vfmadd312pd":
                        instructionInfo = "Performs a set of SIMD multiply-add computation on packed double-precision " +
                                          "floating-point values using three source operands and writes the multiply-add " +
                                          "results in the destination operand. The destination operand is also the first " +
                                          "source operand. The second operand must be a SIMD register. The third source " +
                                          "operand can be a SIMD register or a memory location.";
                        break;
                    case "vfmadd123ps":
                    case "vfmadd132ps":
                    case "vfmadd213ps":
                    case "vfmadd231ps":
                    case "vfmadd321ps":
                    case "vfmadd312ps":
                        instructionInfo = "Performs a set of SIMD multiply-add computation on packed single-precision " +
                                          "floating-point values using three source operands and writes the multiply-add " +
                                          "results in the destination operand. The destination operand is also the first " +
                                          "source operand. The second operand must be a SIMD register. The third source " +
                                          "operand can be a SIMD register or a memory location.";
                        break;
                    case "vfmadd123sd":
                    case "vfmadd132sd":
                    case "vfmadd213sd":
                    case "vfmadd231sd":
                    case "vfmadd321sd":
                    case "vfmadd312sd":
                        instructionInfo = "Performs a SIMD multiply-add computation on the low double-precision " +
                                          "floating-point values using three source operands and writes the multiply-add " +
                                          "result in the destination operand. The destination operand is also the first " +
                                          "source operand. The first and second operand are XMM registers. The third " +
                                          "source operand can be an XMM register or a 64-bit memory location.";
                        break;
                    case "vfmadd123ss":
                    case "vfmadd132ss":
                    case "vfmadd213ss":
                    case "vfmadd231ss":
                    case "vfmadd321ss":
                    case "vfmadd312ss":
                        instructionInfo = "Performs a SIMD multiply-add computation on single-precision floating-point " +
                                          "values using three source operands and writes the multiply-add results in the " +
                                          "destination operand. The destination operand is also the first source operand. " +
                                          "The first and second operands are XMM registers. The third source operand can " +
                                          "be a XMM register or a 32-bit memory location.";
                        break;
                    case "vfmaddsub132pd":
                    case "vfmaddsub213pd":
                    case "vfmaddsub231pd":
                    case "vfmaddsub123pd":
                    case "vfmaddsub312pd":
                    case "vfmaddsub321pd":
                        instructionInfo = "VFMADDSUB132PD: Multiplies the two, four, or eight packed double-precision " +
                                          "floating-point values from the first source operand to the two or four packed " +
                                          "double-precision floating-point values in the third source operand. From the " +
                                          "infinite precision intermediate result, adds the odd double-precision " +
                                          "floating-point elements and subtracts the even double-precision floating-point " +
                                          "values in the second source operand, performs rounding and stores the resulting " +
                                          "two or four packed double-precision floating-point values to the destination " +
                                          "operand (first source operand).";
                        break;
                    case "vfmaddsub132ps":
                    case "vfmaddsub213ps":
                    case "vfmaddsub231ps":
                    case "vfmaddsub123ps":
                    case "vfmaddsub312ps":
                    case "vfmaddsub321ps":
                        instructionInfo = "VFMADDSUB132PS: Multiplies the four, eight or sixteen packed single-precision " +
                                          "floating-point values from the first source operand to the corresponding packed " +
                                          "single-precision floating-point values in the third source operand. From the " +
                                          "infinite precision intermediate result, adds the odd single-precision " +
                                          "floating-point elements and subtracts the even single-precision floating-point " +
                                          "values in the second source operand, performs rounding and stores the resulting " +
                                          "packed single-precision floating-point values to the destination operand " +
                                          "(first source operand).";
                        break;
                    case "vfmsub132pd":
                    case "vfmsub213pd":
                    case "vfmsub231pd":
                    case "vfmsub123pd":
                    case "vfmsub312pd":
                    case "vfmsub321pd":
                        instructionInfo = "Performs a set of SIMD multiply-subtract computation on packed double-precision " +
                                          "floating-point values using three source operands and writes the multiply-subtract " +
                                          "results in the destination operand. The destination operand is also the first " +
                                          "source operand. The second operand must be a SIMD register. The third source " +
                                          "operand can be a SIMD register or a memory location.";
                        break;
                    case "vfmsub132ps":
                    case "vfmsub213ps":
                    case "vfmsub231ps":
                    case "vfmsub123ps":
                    case "vfmsub312ps":
                    case "vfmsub321ps":
                        instructionInfo = "Performs a set of SIMD multiply-subtract computation on packed single-precision " +
                                          "floating-point values using three source operands and writes the " +
                                          "multiply-subtract results in the destination operand. The destination operand " +
                                          "is also the first source operand. The second operand must be a SIMD register. " +
                                          "The third source operand can be a SIMD register or a memory location.";
                        break;
                    case "vfmsub132sd":
                    case "vfmsub213sd":
                    case "vfmsub231sd":
                    case "vfmsub123sd":
                    case "vfmsub312sd":
                    case "vfmsub321sd":
                        instructionInfo = "Performs a SIMD multiply-subtract computation on the low packed double-precision " +
                                          "floating-point values using three source operands and writes the multiply-subtract " +
                                          "result in the destination operand. The destination operand is also the first " +
                                          "source operand. The second operand must be a XMM register. The third source " +
                                          "operand can be a XMM register or a 64-bit memory location.";
                        break;
                    case "vfmsub132ss":
                    case "vfmsub213ss":
                    case "vfmsub231ss":
                    case "vfmsub123ss":
                    case "vfmsub312ss":
                    case "vfmsub321ss":
                        instructionInfo = "Performs a SIMD multiply-subtract computation on the low packed single-precision " +
                                          "floating-point values using three source operands and writes the multiply-subtract result in the destination operand. The destination operand is also the first source operand. The second operand must be a XMM register. The third source operand can be a XMM register or a 32-bit memory location.";
                        break;
                    case "vfmsubadd132pd":
                    case "vfmsubadd213pd":
                    case "vfmsubadd231pd":
                    case "vfmsubadd123pd":
                    case "vfmsubadd312pd":
                    case "vfmsubadd321pd":
                        instructionInfo = "VFMSUBADD132PD: Multiplies the two, four, or eight packed double-precision " +
                                          "floating-point values from the first source operand to the two or four packed " +
                                          "double-precision floating-point values in the third source operand. From the " +
                                          "infinite precision intermediate result, subtracts the odd double-precision " +
                                          "floating-point elements and adds the even double-precision floating-point " +
                                          "values in the second source operand, performs rounding and stores the " +
                                          "resulting two or four packed double-precision floating-point values to the " +
                                          "destination operand (first source operand).";
                        break;
                    case "vfmsubadd132ps":
                    case "vfmsubadd213ps":
                    case "vfmsubadd231ps":
                    case "vfmsubadd123ps":
                    case "vfmsubadd312ps":
                    case "vfmsubadd321ps":
                        instructionInfo = "VFMSUBADD132PS: Multiplies the four, eight or sixteen packed single-precision " +
                                          "floating-point values from the first source operand to the corresponding packed " +
                                          "single-precision floating-point values in the third source operand. From the " +
                                          "infinite precision intermediate result, subtracts the odd single-precision " +
                                          "floating-point elements and adds the even single-precision floating-point " +
                                          "values in the second source operand, performs rounding and stores the resulting " +
                                          "packed single-precision floating-point values to the destination operand " +
                                          "(first source operand).";
                        break;
                    case "vfnmadd132pd":
                    case "vfnmadd213pd":
                    case "vfnmadd231pd":
                    case "vfnmadd123pd":
                    case "vfnmadd312pd":
                    case "vfnmadd321pd":
                        instructionInfo = "VFNMADD132PD: Multiplies the two, four or eight packed double-precision " +
                                          "floating-point values from the first source operand to the two, four or " +
                                          "eight packed double-precision floating-point values in the third source " +
                                          "operand, adds the negated infinite precision intermediate result to the two, " +
                                          "four or eight packed double-precision floating-point values in the second " +
                                          "source operand, performs rounding and stores the resulting two, four or eight " +
                                          "packed double-precision floating-point values to the destination operand " +
                                          "(first source operand).";
                        break;
                    case "vfnmadd132ps":
                    case "vfnmadd213ps":
                    case "vfnmadd231ps":
                    case "vfnmadd123ps":
                    case "vfnmadd312ps":
                    case "vfnmadd321ps":
                        instructionInfo = "VFNMADD132PS: Multiplies the four, eight or sixteen packed single-precision " +
                                          "floating-point values from the first source operand to the four, eight or " +
                                          "sixteen packed single-precision floating-point values in the third source " +
                                          "operand, adds the negated infinite precision intermediate result to the four, " +
                                          "eight or sixteen packed single-precision floating-point values in the second " +
                                          "source operand, performs rounding and stores the resulting four, eight or " +
                                          "sixteen packed single-precision floating-point values to the destination " +
                                          "operand (first source operand).";
                        break;
                    case "vfnmadd132sd":
                    case "vfnmadd213sd":
                    case "vfnmadd231sd":
                    case "vfnmadd123sd":
                    case "vfnmadd312sd":
                    case "vfnmadd321sd":
                        instructionInfo = "VFNMADD132SD: Multiplies the low packed double-precision floating-point value " +
                                          "from the first source operand to the low packed double-precision floating-point " +
                                          "value in the third source operand, adds the negated infinite precision " +
                                          "intermediate result to the low packed double-precision floating-point values " +
                                          "in the second source operand, performs rounding and stores the resulting packed " +
                                          "double-precision floating-point value to the destination operand (first source operand).";
                        break;
                    case "vfnmadd132ss":
                    case "vfnmadd213ss":
                    case "vfnmadd231ss":
                    case "vfnmadd123ss":
                    case "vfnmadd312ss":
                    case "vfnmadd321ss":
                        instructionInfo = "VFNMADD132SS: Multiplies the low packed single-precision floating-point value " +
                                          "from the first source operand to the low packed single-precision floating-point " +
                                          "value in the third source operand, adds the negated infinite precision " +
                                          "intermediate result to the low packed single-precision floating-point value " +
                                          "in the second source operand, performs rounding and stores the resulting " +
                                          "packed single-precision floating-point value to the destination operand (first source operand).";
                        break;
                    case "vfnmsub132pd":
                    case "vfnmsub213pd":
                    case "vfnmsub231pd":
                    case "vfnmsub123pd":
                    case "vfnmsub312pd":
                    case "vfnmsub321pd":
                        instructionInfo = "VFNMSUB132PD: Multiplies the two, four or eight packed double-precision " +
                                          "floating-point values from the first source operand to the two, four or " +
                                          "eight packed double-precision floating-point values in the third source operand. " +
                                          "From negated infinite precision intermediate results, subtracts the two, " +
                                          "four or eight packed double-precision floating-point values in the second " +
                                          "source operand, performs rounding and stores the resulting two, four or eight " +
                                          "packed double-precision floating-point values to the destination operand (first source operand).";
                        break;
                    case "vfnmsub132ps":
                    case "vfnmsub213ps":
                    case "vfnmsub231ps":
                    case "vfnmsub123ps":
                    case "vfnmsub312ps":
                    case "vfnmsub321ps":
                        instructionInfo = "VFNMSUB132PS: Multiplies the four, eight or sixteen packed single-precision " +
                                          "floating-point values from the first source operand to the four, eight or " +
                                          "sixteen packed single-precision floating-point values in the third source operand. " +
                                          "From negated infinite precision intermediate results, subtracts the four, " +
                                          "eight or sixteen packed single-precision floating-point values in the second " +
                                          "source operand, performs rounding and stores the resulting four, eight or " +
                                          "sixteen packed single-precision floating-point values to the destination " +
                                          "operand (first source operand).";
                        break;
                    case "vfnmsub132sd":
                    case "vfnmsub213sd":
                    case "vfnmsub231sd":
                    case "vfnmsub123sd":
                    case "vfnmsub312sd":
                    case "vfnmsub321sd":
                        instructionInfo = "VFNMSUB132SD: Multiplies the low packed double-precision floating-point value " +
                                          "from the first source operand to the low packed double-precision floating-point " +
                                          "value in the third source operand. From negated infinite precision " +
                                          "intermediate result, subtracts the low double-precision floating-point value " +
                                          "in the second source operand, performs rounding and stores the resulting " +
                                          "packed double-precision floating-point value to the destination operand (first source operand).";
                        break;
                    case "vfnmsub132ss":
                    case "vfnmsub213ss":
                    case "vfnmsub231ss":
                    case "vfnmsub123ss":
                    case "vfnmsub312ss":
                    case "vfnmsub321ss":
                        instructionInfo = "VFNMSUB132SS: Multiplies the low packed single-precision floating-point " +
                                          "value from the first source operand to the low packed single-precision " +
                                          "floating-point value in the third source operand. From negated infinite " +
                                          "precision intermediate result, the low single-precision floating-point " +
                                          "value in the second source operand, performs rounding and stores the " +
                                          "resulting packed single-precision floating-point value to the destination " +
                                          "operand (first source operand).";
                        break;
                    case "vfpclasspd":
                        instructionInfo = "The FPCLASSPD instruction checks the packed double precision floating point " +
                                          "values for special categories, specified by the set bits in the imm8 byte. " +
                                          "Each set bit in imm8 specifies a category of floating-point values that the " +
                                          "input data element is classified against. The classified results of all " +
                                          "specified categories of an input value are ORed together to form the final " +
                                          "boolean result for the input element. The result of each element is written " +
                                          "to the corresponding bit in a mask register k2 according to the writemask k1. " +
                                          "Bits [MAX_KL-1:8/4/2] of the destination are cleared.";
                        break;
                    case "vfpclassps":
                        instructionInfo = "The FPCLASSPS instruction checks the packed single-precision floating point " +
                                          "values for special categories, specified by the set bits in the imm8 byte. " +
                                          "Each set bit in imm8 specifies a category of floating-point values that the " +
                                          "input data element is classified against. The classified results of all " +
                                          "specified categories of an input value are ORed together to form the final " +
                                          "boolean result for the input element. The result of each element is written " +
                                          "to the corresponding bit in a mask register k2 according to the writemask k1. " +
                                          "Bits [MAX_KL-1:16/8/4] of the destination are cleared.";
                        break;
                    case "vfpclasssd":
                        instructionInfo = "The FPCLASSSD instruction checks the low double precision floating point value " +
                                          "in the source operand for special categories, specified by the set bits in " +
                                          "the imm8 byte. Each set bit in imm8 specifies a category of floating-point " +
                                          "values that the input data element is classified against. The classified " +
                                          "results of all specified categories of an input value are ORed together to " +
                                          "form the final boolean result for the input element. The result is written to " +
                                          "the low bit in a mask register k2 according to the writemask k1. Bits " +
                                          "MAX_KL-1: 1 of the destination are cleared.";
                        break;
                    case "vfpclassss":
                        instructionInfo = "The FPCLASSSS instruction checks the low single-precision floating point " +
                                          "value in the source operand for special categories, specified by the set bits " +
                                          "in the imm8 byte. Each set bit in imm8 specifies a category of floating-point " +
                                          "values that the input data element is classified against. The classified " +
                                          "results of all specified categories of an input value are ORed together to " +
                                          "form the final boolean result for the input element. The result is written " +
                                          "to the low bit in a mask register k2 according to the writemask k1. Bits " +
                                          "MAX_KL-1: 1 of the destination are cleared.";
                        break;
                    case "vgatherdpd":
                    case "vgatherqpd":
                        instructionInfo = "The instruction conditionally loads up to 2 or 4 double-precision " +
                                          "floating-point values from memory addresses specified by the memory operand " +
                                          "(the second operand) and using qword indices. The memory operand uses the " +
                                          "VSIB form of the SIB byte to specify a general purpose register operand as " +
                                          "the common base, a vector register for an array of indices relative to the " +
                                          "base and a constant scale factor.";
                        break;
                    case "vgatherdps":
                    case "vgatherqps":
                        instructionInfo = "The instruction conditionally loads up to 4 or 8 single-precision floating-point " +
                                          "values from memory addresses specified by the memory operand (the second operand) " +
                                          "and using dword indices. The memory operand uses the VSIB form of the SIB byte to " +
                                          "specify a general purpose register operand as the common base, a vector register " +
                                          "for an array of indices relative to the base and a constant scale factor.";
                        break;
                    case "vgetexppd":
                        instructionInfo = "Extracts the biased exponents from the normalized DP FP representation of " +
                                          "each qword data element of the source operand (the second operand) as unbiased " +
                                          "signed integer value, or convert the denormal representation of input data to " +
                                          "unbiased negative integer values. Each integer value of the unbiased exponent " +
                                          "is converted to double-precision FP value and written to the corresponding " +
                                          "qword elements of the destination operand (the first operand) as DP FP numbers.";
                        break;
                    case "vgetexpps":
                        instructionInfo = "Extracts the biased exponents from the normalized SP FP representation of " +
                                          "each dword element of the source operand (the second operand) as unbiased " +
                                          "signed integer value, or convert the denormal representation of input data to " +
                                          "unbiased negative integer values. Each integer value of the unbiased exponent " +
                                          "is converted to single-precision FP value and written to the corresponding " +
                                          "dword elements of the destination operand (the first operand) as SP FP numbers.";
                        break;
                    case "vgetexpsd":
                        instructionInfo = "Extracts the biased exponent from the normalized DP FP representation of the " +
                                          "low qword data element of the source operand (the third operand) as unbiased " +
                                          "signed integer value, or convert the denormal representation of input data to " +
                                          "unbiased negative integer values. The integer value of the unbiased exponent " +
                                          "is converted to double-precision FP value and written to the destination operand " +
                                          "(the first operand) as DP FP numbers. Bits (127:64) of the XMM register " +
                                          "destination are copied from corresponding bits in the first source operand.";
                        break;
                    case "vgetexpss":
                        instructionInfo = "Extracts the biased exponent from the normalized SP FP representation of the " +
                                          "low doubleword data element of the source operand (the third operand) as " +
                                          "unbiased signed integer value, or convert the denormal representation of input " +
                                          "data to unbiased negative integer values. The integer value of the unbiased " +
                                          "exponent is converted to single-precision FP value and written to the destination " +
                                          "operand (the first operand) as SP FP numbers. Bits (127:32) of the XMM register " +
                                          "destination are copied from corresponding bits in the first source operand.";
                        break;
                    case "vgetmantpd":
                        instructionInfo = "Convert double-precision floating values in the source operand (the second " +
                                          "operand) to DP FP values with the mantissa normalization and sign control " +
                                          "specified by the imm8 byte. The converted results are written to the destination " +
                                          "operand (the first operand) using writemask k1. The normalized mantissa is " +
                                          "specified by interv (imm8[1:0]) and the sign control (sc) is specified by " +
                                          "bits 3:2 of the immediate byte.";
                        break;
                    case "vgetmantps":
                        instructionInfo = "Convert single-precision floating values in the source operand (the second " +
                                          "operand) to SP FP values with the mantissa normalization and sign control " +
                                          "specified by the imm8 byte. The converted results are written to the destination " +
                                          "operand (the first operand) using writemask k1. The normalized mantissa is " +
                                          "specified by interv (imm8[1:0]) and the sign control (sc) is specified by " +
                                          "bits 3:2 of the immediate byte.";
                        break;
                    case "vgetmantsd":
                        instructionInfo = "Convert the double-precision floating values in the low quadword element of " +
                                          "the second source operand (the third operand) to DP FP value with the mantissa " +
                                          "normalization and sign control specified by the imm8 byte. The converted " +
                                          "result is written to the low quadword element of the destination operand " +
                                          "(the first operand) using writemask k1. Bits (127:64) of the XMM register " +
                                          "destination are copied from corresponding bits in the first source operand. " +
                                          "The normalized mantissa is specified by interv (imm8[1:0]) and the sign " +
                                          "control (sc) is specified by bits 3:2 of the immediate byte.";
                        break;
                    case "vgetmantss":
                        instructionInfo = "Convert the single-precision floating values in the low doubleword element " +
                                          "of the second source operand (the third operand) to SP FP value with the " +
                                          "mantissa normalization and sign control specified by the imm8 byte. " +
                                          "The converted result is written to the low doubleword element of the destination " +
                                          "operand (the first operand) using writemask k1. Bits (127:32) of the XMM " +
                                          "register destination are copied from corresponding bits in the first source " +
                                          "operand. The normalized mantissa is specified by interv (imm8[1:0]) and the " +
                                          "sign control (sc) is specified by bits 3:2 of the immediate byte.";
                        break;
                    case "vinsertf128":
                    case "vinsertf32x4":
                    case "vinsertf64x2":
                    case "vinsertf32x8":
                    case "vinsertf64x4":
                        instructionInfo = "VINSERTF128/VINSERTF32x4 and VINSERTF64x2 insert 128-bits of packed " +
                                          "floating-point values from the second source operand (the third operand) " +
                                          "into the destination operand (the first operand) at an 128-bit granularity " +
                                          "offset multiplied by imm8[0] (256-bit) or imm8[1:0]. The remaining portions " +
                                          "of the destination operand are copied from the corresponding fields of the " +
                                          "first source operand (the second operand). The second source operand can be " +
                                          "either an XMM register or a 128-bit memory location. The destination and " +
                                          "first source operands are vector registers.";
                        break;
                    case "vinserti128":
                    case "vinserti32x4":
                    case "vinserti64x2":
                    case "vinserti32x8":
                    case "vinserti64x4":
                        instructionInfo = "VINSERTI32x4 and VINSERTI64x2 inserts 128-bits of packed integer values from " +
                                          "the second source operand (the third operand) into the destination operand " +
                                          "(the first operand) at an 128-bit granular offset multiplied by imm8[0] " +
                                          "(256-bit) or imm8[1:0]. The remaining portions of the destination are copied " +
                                          "from the corresponding fields of the first source operand (the second operand). " +
                                          "The second source operand can be either an XMM register or a 128-bit memory " +
                                          "location. The high 6/7bits of the immediate are ignored. The destination " +
                                          "operand is a ZMM/YMM register and updated at 32 and 64-bit granularity " +
                                          "according to the writemask.";
                        break;
                    case "vmaskmov":
                        instructionInfo = "Conditionally moves packed data elements from the second source operand into " +
                                          "the corresponding data element of the destination operand, depending on the " +
                                          "mask bits associated with each data element. The mask bits are specified in " +
                                          "the first source operand.";
                        break;
                    case "vpblendd":
                        instructionInfo = "Dword elements from the source operand (second operand) are conditionally " +
                                          "written to the destination operand (first operand) depending on bits in the " +
                                          "immediate operand (third operand). The immediate bits (bits 7:0) form a mask " +
                                          "that determines whether the corresponding word in the destination is copied " +
                                          "from the source. If a bit in the mask, corresponding to a word, is \"1\", " +
                                          "then the word is copied, else the word is unchanged.";
                        break;
                    case "vpblendmb":
                    case "vpblendmw":
                        instructionInfo = "Performs an element-by-element blending of byte/word elements between the " +
                                          "first source operand byte vector register and the second source operand byte " +
                                          "vector from memory or register, using the instruction mask as selector. " +
                                          "The result is written into the destination byte vector register.";
                        break;
                    case "vpblendmd":
                    case "vpblendmq":
                        instructionInfo = "Performs an element-by-element blending of dword/qword elements between the " +
                                          "first source operand (the second operand) and the elements of the second " +
                                          "source operand (the third operand) using an opmask register as select control. " +
                                          "The blended result is written into the destination.";
                        break;
                    case "vpbroadcast":
                        instructionInfo = "Load integer data from the source operand (the second operand) and broadcast " +
                                          "to all elements of the destination operand (the first operand).";
                        break;
                    case "vpbroadcastb":
                    case "vpbroadcastw":
                    case "vpbroadcastd":
                    case "vpbroadcastq":
                        instructionInfo = "Broadcasts a 8-bit, 16-bit, 32-bit or 64-bit value from a general-purpose " +
                                          "register (the second operand) to all the locations in the destination vector " +
                                          "register (the first operand) using the writemask k1.";
                        break;
                    case "vpbroadcastm":
                        instructionInfo = "Broadcasts the zero-extended 64/32 bit value of the low byte/word of the " +
                                          "source operand (the second operand) to each 64/32 bit element of the " +
                                          "destination operand (the first operand). The source operand is an opmask register. " +
                                          "The destination operand is a ZMM register (EVEX.512), YMM register (EVEX.256), " +
                                          "or XMM register (EVEX.128).";
                        break;
                    case "vpcmpb":
                    case "vpcmpub":
                        instructionInfo = "Performs a SIMD compare of the packed byte values in the second source operand " +
                                          "and the first source operand and returns the results of the comparison to the " +
                                          "mask destination operand. The comparison predicate operand (immediate byte) " +
                                          "specifies the type of comparison performed on each pair of packed values in " +
                                          "the two source operands. The result of each comparison is a single mask bit " +
                                          "result of 1 (comparison true) or 0 (comparison false).";
                        break;
                    case "vpcmpd":
                    case "vpcmpud":
                        instructionInfo = "Performs a SIMD compare of the packed integer values in the second source " +
                                          "operand and the first source operand and returns the results of the comparison " +
                                          "to the mask destination operand. The comparison predicate operand (immediate byte) " +
                                          "specifies the type of comparison performed on each pair of packed values in " +
                                          "the two source operands. The result of each comparison is a single mask bit " +
                                          "result of 1 (comparison true) or 0 (comparison false).";
                        break;
                    case "vpcmpq":
                    case "vpcmpuq":
                        instructionInfo = "Performs a SIMD compare of the packed integer values in the second source " +
                                          "operand and the first source operand and returns the results of the comparison " +
                                          "to the mask destination operand. The comparison predicate operand (immediate " +
                                          "byte) specifies the type of comparison performed on each pair of packed values " +
                                          "in the two source operands. The result of each comparison is a single mask " +
                                          "bit result of 1 (comparison true) or 0 (comparison false).";
                        break;
                    case "vpcmpw":
                    case "vpcmpuw":
                        instructionInfo = "Performs a SIMD compare of the packed integer word in the second source " +
                                          "operand and the first source operand and returns the results of the comparison " +
                                          "to the mask destination operand. The comparison predicate operand (immediate " +
                                          "byte) specifies the type of comparison performed on each pair of packed values " +
                                          "in the two source operands. The result of each comparison is a single mask " +
                                          "bit result of 1 (comparison true) or 0 (comparison false).";
                        break;
                    case "vpcompressd":
                        instructionInfo = "Compress (store) up to 16/8/4 doubleword integer values from the source " +
                                          "operand (second operand) to the destination operand (first operand). " +
                                          "The source operand is a ZMM/YMM/XMM register, the destination operand can " +
                                          "be a ZMM/YMM/XMM register or a 512/256/128-bit memory location.";
                        break;
                    case "vpcompressq":
                        instructionInfo = "Compress (stores) up to 8/4/2 quadword integer values from the source operand " +
                                          "(second operand) to the destination operand (first operand). The source " +
                                          "operand is a ZMM/YMM/XMM register, the destination operand can be a ZMM/YMM/XMM " +
                                          "register or a 512/256/128-bit memory location.";
                        break;
                    case "vpconflictd":
                    case "vpconflictq":
                        instructionInfo = "Test each dword/qword element of the source operand (the second operand) for " +
                                          "equality with all other elements in the source operand closer to the least " +
                                          "significant element. Each element\xe2\x80\x99s comparison results form a bit " +
                                          "vector, which is then zero extended and written to the destination according " +
                                          "to the writemask.";
                        break;
                    case "vperm2f128":
                        instructionInfo = "Permute 128 bit floating-point-containing fields from the first source operand " +
                                          "(second operand) and second source operand (third operand) using bits in the " +
                                          "8-bit immediate and store results in the destination operand (first operand). " +
                                          "The first source operand is a YMM register, the second source operand is a YMM " +
                                          "register or a 256-bit memory location, and the destination operand is a YMM register.";
                        break;
                    case "vperm2i128":
                        instructionInfo = "Permute 128 bit integer data from the first source operand (second operand) " +
                                          "and second source operand (third operand) using bits in the 8-bit immediate " +
                                          "and store results in the destination operand (first operand). The first source " +
                                          "operand is a YMM register, the second source operand is a YMM register or a " +
                                          "256-bit memory location, and the destination operand is a YMM register.";
                        break;
                    case "vpermb":
                        instructionInfo = "Copies bytes from the second source operand (the third operand) to the " +
                                          "destination operand (the first operand) according to the byte indices in the " +
                                          "first source operand (the second operand). Note that this instruction permits " +
                                          "a byte in the source operand to be copied to more than one location in the " +
                                          "destination operand.";
                        break;
                    case "vpermd":
                    case "vpermw":
                        instructionInfo = "Copies doublewords (or words) from the second source operand (the third " +
                                          "operand) to the destination operand (the first operand) according to the " +
                                          "indices in the first source operand (the second operand). Note that this " +
                                          "instruction permits a doubleword (word) in the source operand to be copied " +
                                          "to more than one location in the destination operand.";
                        break;
                    case "vpermi2b":
                        instructionInfo = "Permutes byte values in the second operand (the first source operand) and " +
                                          "the third operand (the second source operand) using the byte indices in the " +
                                          "first operand (the destination operand) to select byte elements from the " +
                                          "second or third source operands. The selected byte elements are written to " +
                                          "the destination at byte granularity under the writemask k1.";
                        break;
                    case "vpermi2w":
                    case "vpermi2d":
                    case "vpermi2q":
                    case "vpermi2ps":
                    case "vpermi2pd":
                        instructionInfo = "Permutes 16-bit/32-bit/64-bit values in the second operand (the first source " +
                                          "operand) and the third operand (the second source operand) using indices in " +
                                          "the first operand to select elements from the second and third operands. " +
                                          "The selected elements are written to the destination operand (the first " +
                                          "operand) according to the writemask k1.";
                        break;
                    case "vpermilpd":
                        instructionInfo = "Permute pairs of double-precision floating-point values in the first source " +
                                          "operand (second operand), each using a 1-bit control field residing in the " +
                                          "corresponding quadword element of the second source operand (third operand). " +
                                          "Permuted results are stored in the destination operand (first operand).";
                        break;
                    case "vpermilps":
                        instructionInfo = "Permute quadruples of single-precision floating-point values in the first " +
                                          "source operand (second operand), each quadruplet using a 2-bit control field " +
                                          "in the corresponding dword element of the second source operand. Permuted " +
                                          "results are stored in the destination operand (first operand).";
                        break;
                    case "vpermpd":
                        instructionInfo = "The imm8 version: Copies quadword elements of double-precision floating-point " +
                                          "values from the source operand (the second operand) to the destination " +
                                          "operand (the first operand) according to the indices specified by the " +
                                          "immediate operand (the third operand). Each two-bit value in the immediate " +
                                          "byte selects a qword element in the source operand.";
                        break;
                    case "vpermps":
                        instructionInfo = "Copies doubleword elements of single-precision floating-point values from " +
                                          "the second source operand (the third operand) to the destination operand " +
                                          "(the first operand) according to the indices in the first source operand " +
                                          "(the second operand). Note that this instruction permits a doubleword in " +
                                          "the source operand to be copied to more than one location in the destination " +
                                          "operand.";
                        break;
                    case "vpermq":
                        instructionInfo = "The imm8 version: Copies quadwords from the source operand (the second " +
                                          "operand) to the destination operand (the first operand) according to the " +
                                          "indices specified by the immediate operand (the third operand). Each two-bit " +
                                          "value in the immediate byte selects a qword element in the source operand.";
                        break;
                    case "vpermt2b":
                        instructionInfo = "Permutes byte values from two tables, comprising of the first operand (also " +
                                          "the destination operand) and the third operand (the second source operand). " +
                                          "The second operand (the first source operand) provides byte indices to select " +
                                          "byte results from the two tables. The selected byte elements are written to " +
                                          "the destination at byte granularity under the writemask k1.";
                        break;
                    case "vpermt2w":
                    case "vpermt2d":
                    case "vpermt2q":
                    case "vpermt2ps":
                    case "vpermt2pd":
                        instructionInfo = "Permutes 16-bit/32-bit/64-bit values in the first operand and the third " +
                                          "operand (the second source operand) using indices in the second operand " +
                                          "(the first source operand) to select elements from the first and third operands. " +
                                          "The selected elements are written to the destination operand (the first operand) " +
                                          "according to the writemask k1.";
                        break;
                    case "vpexpandd":
                        instructionInfo = "Expand (load) up to 16 contiguous doubleword integer values of the input " +
                                          "vector in the source operand (the second operand) to sparse elements in the " +
                                          "destination operand (the first operand), selected by the writemask k1. " +
                                          "The destination operand is a ZMM register, the source operand can be a ZMM " +
                                          "register or memory location.";
                        break;
                    case "vpexpandq":
                        instructionInfo = "Expand (load) up to 8 quadword integer values from the source operand (the " +
                                          "second operand) to sparse elements in the destination operand (the first " +
                                          "operand), selected by the writemask k1. The destination operand is a ZMM " +
                                          "register, the source operand can be a ZMM register or memory location.";
                        break;
                    case "vpgatherdd":
                    case "vpgatherqd":
                        instructionInfo = "The instruction conditionally loads up to 4 or 8 dword values from memory " +
                                          "addresses specified by the memory operand (the second operand) and using " +
                                          "dword indices. The memory operand uses the VSIB form of the SIB byte to " +
                                          "specify a general purpose register operand as the common base, a vector " +
                                          "register for an array of indices relative to the base and a constant scale factor.";
                        break;
                    case "vpgatherdq":
                        instructionInfo = "A set of 16 or 8 doubleword/quadword memory locations pointed to by base " +
                                          "address BASE_ADDR and index vector VINDEX with scale SCALE are gathered. " +
                                          "The result is written into vector zmm1. The elements are specified via the " +
                                          "VSIB (i.e., the index register is a zmm, holding packed indices). " +
                                          "Elements will only be loaded if their corresponding mask bit is one. If an " +
                                          "element\xe2\x80\x99s mask bit is not set, the corresponding element of the " +
                                          "destination register (zmm1) is left unchanged. The entire mask register will " +
                                          "be set to zero by this instruction unless it triggers an exception.";
                        break;
                    case "vpgatherqq":
                        instructionInfo = "A set of 8 doubleword/quadword memory locations pointed to by base address " +
                                          "BASE_ADDR and index vector VINDEX with scale SCALE are gathered. The result " +
                                          "is written into a vector register. The elements are specified via the VSIB " +
                                          "(i.e., the index register is a vector register, holding packed indices). " +
                                          "Elements will only be loaded if their corresponding mask bit is one. If an " +
                                          "element\xe2\x80\x99s mask bit is not set, the corresponding element of the " +
                                          "destination register is left unchanged. The entire mask register will be set " +
                                          "to zero by this instruction unless it triggers an exception.";
                        break;
                    case "vplzcntd":
                    case "vplzcntq":
                        instructionInfo = "Counts the number of leading most significant zero bits in each dword or " +
                                          "qword element of the source operand (the second operand) and stores the " +
                                          "results in the destination register (the first operand) according to the " +
                                          "writemask. If an element is zero, the result for that element is the operand " +
                                          "size of the element.";
                        break;
                    case "vpmadd52huq":
                        instructionInfo = "Multiplies packed unsigned 52-bit integers in each qword element of the first " +
                                          "source operand (the second operand) with the packed unsigned 52-bit integers " +
                                          "in the corresponding elements of the second source operand (the third operand) " +
                                          "to form packed 104-bit intermediate results. The high 52-bit, unsigned integer " +
                                          "of each 104-bit product is added to the corresponding qword unsigned integer " +
                                          "of the destination operand (the first operand) under the writemask k1.";
                        break;
                    case "vpmadd52luq":
                        instructionInfo = "Multiplies packed unsigned 52-bit integers in each qword element of the first " +
                                          "source operand (the second operand) with the packed unsigned 52-bit integers " +
                                          "in the corresponding elements of the second source operand (the third operand) " +
                                          "to form packed 104-bit intermediate results. The low 52-bit, unsigned integer " +
                                          "of each 104-bit product is added to the corresponding qword unsigned integer " +
                                          "of the destination operand (the first operand) under the writemask k1.";
                        break;
                    case "vpmaskmov":
                    case "vpmaskmovd":
                    case "vpmaskmovq":
                        instructionInfo = "Conditionally moves packed data elements from the second source operand into " +
                                          "the corresponding data element of the destination operand, depending on the " +
                                          "mask bits associated with each data element. The mask bits are specified in " +
                                          "the first source operand.";
                        break;
                    case "vpmovb2m":
                    case "vpmovw2m":
                    case "vpmovd2m":
                    case "vpmovq2m":
                        instructionInfo = "Converts a vector register to a mask register. Each element in the destination " +
                                          "register is set to 1 or 0 depending on the value of most significant bit of " +
                                          "the corresponding element in the source register.";
                        break;
                    case "vpmovdb":
                    case "vpmovsdb":
                    case "vpmovusdb":
                        instructionInfo = "VPMOVDB down converts 32-bit integer elements in the source operand (the " +
                                          "second operand) into packed bytes using truncation. VPMOVSDB converts signed " +
                                          "32-bit integers into packed signed bytes using signed saturation. VPMOVUSDB " +
                                          "convert unsigned double-word values into unsigned byte values using unsigned " +
                                          "saturation.";
                        break;
                    case "vpmovdw":
                    case "vpmovsdw":
                    case "vpmovusdw":
                        instructionInfo = "VPMOVDW down converts 32-bit integer elements in the source operand (the " +
                                          "second operand) into packed words using truncation. VPMOVSDW converts signed " +
                                          "32-bit integers into packed signed words using signed saturation. VPMOVUSDW " +
                                          "convert unsigned double-word values into unsigned word values using unsigned " +
                                          "saturation.";
                        break;
                    case "vpmovm2b":
                    case "vpmovm2w":
                    case "vpmovm2d":
                    case "vpmovm2q":
                        instructionInfo = "Converts a mask register to a vector register. Each element in the destination " +
                                          "register is set to all 1\xe2\x80\x99s or all 0\xe2\x80\x99s depending on the " +
                                          "value of the corresponding bit in the source mask register.";
                        break;
                    case "vpmovqb":
                    case "vpmovsqb":
                    case "vpmovusqb":
                        instructionInfo = "VPMOVQB down converts 64-bit integer elements in the source operand (the " +
                                          "second operand) into packed byte elements using truncation. VPMOVSQB converts " +
                                          "signed 64-bit integers into packed signed bytes using signed saturation. " +
                                          "VPMOVUSQB convert unsigned quad-word values into unsigned byte values using " +
                                          "unsigned saturation. The source operand is a vector register. The destination " +
                                          "operand is an XMM register or a memory location.";
                        break;
                    case "vpmovqd":
                    case "vpmovsqd":
                    case "vpmovusqd":
                        instructionInfo = "VPMOVQW down converts 64-bit integer elements in the source operand (the " +
                                          "second operand) into packed double-words using truncation. VPMOVSQW converts " +
                                          "signed 64-bit integers into packed signed doublewords using signed saturation. " +
                                          "VPMOVUSQW convert unsigned quad-word values into unsigned double-word values " +
                                          "using unsigned saturation.";
                        break;
                    case "vpmovqw":
                    case "vpmovsqw":
                    case "vpmovusqw":
                        instructionInfo = "VPMOVQW down converts 64-bit integer elements in the source operand (the " +
                                          "second operand) into packed words using truncation. VPMOVSQW converts signed " +
                                          "64-bit integers into packed signed words using signed saturation. VPMOVUSQW " +
                                          "convert unsigned quad-word values into unsigned word values using unsigned " +
                                          "saturation.";
                        break;
                    case "vpmovwb":
                    case "vpmovswb":
                    case "vpmovuswb":
                        instructionInfo = "VPMOVWB down converts 16-bit integers into packed bytes using truncation. " +
                                          "VPMOVSWB converts signed 16-bit integers into packed signed bytes using signed " +
                                          "saturation. VPMOVUSWB convert unsigned word values into unsigned byte values " +
                                          "using unsigned saturation.";
                        break;
                    case "vpmultishiftqb":
                        instructionInfo = "This instruction selects eight unaligned bytes from each input qword element " +
                                          "of the second source operand (the third operand) and writes eight assembled " +
                                          "bytes for each qword element in the destination operand (the first operand). " +
                                          "Each byte result is selected using a byte-granular shift control within the " +
                                          "corresponding qword element of the first source operand (the second operand). " +
                                          "Each byte result in the destination operand is updated under the writemask k1.";
                        break;
                    case "vprold":
                    case "vprolvd":
                    case "vprolq":
                    case "vprolvq":
                        instructionInfo = "Rotates the bits in the individual data elements (doublewords, or quadword) " +
                                          "in the first source operand to the left by the number of bits specified in " +
                                          "the count operand. If the value specified by the count operand is greater " +
                                          "than 31 (for doublewords), or 63 (for a quadword), then the count operand " +
                                          "modulo the data size (32 or 64) is used.";
                        break;
                    case "vprord":
                    case "vprorvd":
                    case "vprorq":
                    case "vprorvq":
                        instructionInfo = "Rotates the bits in the individual data elements (doublewords, or quadword) " +
                                          "in the first source operand to the right by the number of bits specified in " +
                                          "the count operand. If the value specified by the count operand is greater than " +
                                          "31 (for doublewords), or 63 (for a quadword), then the count operand modulo " +
                                          "the data size (32 or 64) is used.";
                        break;
                    case "vpscatterdd":
                    case "vpscatterdq":
                    case "vpscatterqd":
                    case "vpscatterqq":
                        instructionInfo = "Stores up to 16 elements (8 elements for qword indices) in doubleword vector " +
                                          "or 8 elements in quadword vector to the memory locations pointed by base address " +
                                          "BASE_ADDR and index vector VINDEX, with scale SCALE. The elements are specified " +
                                          "via the VSIB (i.e., the index register is a vector register, holding packed " +
                                          "indices). Elements will only be stored if their corresponding mask bit is one. " +
                                          "The entire mask register will be set to zero by this instruction unless it triggers an exception.";
                        break;
                    case "vpsllvw":
                    case "vpsllvd":
                    case "vpsllvq":
                        instructionInfo = "Shifts the bits in the individual data elements (words, doublewords or quadword) " +
                                          "in the first source operand to the left by the count value of respective data " +
                                          "elements in the second source operand. As the bits in the data elements are " +
                                          "shifted left, the empty low-order bits are cleared (set to 0).";
                        break;
                    case "vpsravw":
                    case "vpsravd":
                    case "vpsravq":
                        instructionInfo = "Shifts the bits in the individual data elements (word/doublewords/quadword) " +
                                          "in the first source operand (the second operand) to the right by the number " +
                                          "of bits specified in the count value of respective data elements in the second " +
                                          "source operand (the third operand). As the bits in the data elements are " +
                                          "shifted right, the empty high-order bits are set to the MSB (sign extension).";
                        break;
                    case "vpsrlvw":
                    case "vpsrlvd":
                    case "vpsrlvq":
                        instructionInfo = "Shifts the bits in the individual data elements (words, doublewords or " +
                                          "quadword) in the first source operand to the right by the count value of " +
                                          "respective data elements in the second source operand. As the bits in the " +
                                          "data elements are shifted right, the empty high-order bits are cleared (set to 0).";
                        break;
                    case "vpternlogd":
                    case "vpternlogq":
                        instructionInfo = "VPTERNLOGD/Q takes three bit vectors of 512-bit length (in the first, second " +
                                          "and third operand) as input data to form a set of 512 indices, each index is " +
                                          "comprised of one bit from each input vector. The imm8 byte specifies a boolean " +
                                          "logic table producing a binary value for each 3-bit index value. The final " +
                                          "512-bit boolean result is written to the destination operand (the first operand) " +
                                          "using the writemask k1 with the granularity of doubleword element or quadword " +
                                          "element into the destination.";
                        break;
                    case "vptestmb":
                    case "vptestmw":
                    case "vptestmd":
                    case "vptestmq":
                        instructionInfo = "Performs a bitwise logical AND operation on the first source operand (the " +
                                          "second operand) and second source operand (the third operand) and stores the " +
                                          "result in the destination operand (the first operand) under the writemask. " +
                                          "Each bit of the result is set to 1 if the bitwise AND of the corresponding " +
                                          "elements of the first and second src operands is non-zero; otherwise it is " +
                                          "set to 0.";
                        break;
                    case "vptestnmb":
                    case "vptestnmw":
                    case "vptestnmd":
                    case "vptestnmq":
                        instructionInfo = "Performs a bitwise logical NAND operation on the byte/word/doubleword/quadword " +
                                          "element of the first source operand (the second operand) with the corresponding " +
                                          "element of the second source operand (the third operand) and stores the " +
                                          "logical comparison result into each bit of the destination operand (the first " +
                                          "operand) according to the writemask k1. Each bit of the result is set to 1 if " +
                                          "the bitwise AND of the corresponding elements of the first and second src " +
                                          "operands is zero; otherwise it is set to 0.";
                        break;
                    case "vrangepd":
                        instructionInfo = "This instruction calculates 2/4/8 range operation outputs from two sets of " +
                                          "packed input double-precision FP values in the first source operand (the second " +
                                          "operand) and the second source operand (the third operand). The range outputs " +
                                          "are written to the destination operand (the first operand) under the writemask " +
                                          "k1.";
                        break;
                    case "vrangeps":
                        instructionInfo = "This instruction calculates 4/8/16 range operation outputs from two sets of " +
                                          "packed input single-precision FP values in the first source operand (the second " +
                                          "operand) and the second source operand (the third operand). The range outputs " +
                                          "are written to the destination operand (the first operand) under the writemask k1.";
                        break;
                    case "vrangesd":
                        instructionInfo = "This instruction calculates a range operation output from two input " +
                                          "double-precision FP values in the low qword element of the first source " +
                                          "operand (the second operand) and second source operand (the third operand). " +
                                          "The range output is written to the low qword element of the destination operand " +
                                          "(the first operand) under the writemask k1.";
                        break;
                    case "vrangess":
                        instructionInfo = "This instruction calculates a range operation output from two input " +
                                          "single-precision FP values in the low dword element of the first source operand " +
                                          "(the second operand) and second source operand (the third operand). The range " +
                                          "output is written to the low dword element of the destination operand (the " +
                                          "first operand) under the writemask k1.";
                        break;
                    case "vrcp14pd":
                        instructionInfo = "This instruction performs a SIMD computation of the approximate reciprocals " +
                                          "of eight/four/two packed double-precision floating-point values in the source " +
                                          "operand (the second operand) and stores the packed double-precision " +
                                          "floating-point results in the destination operand. The maximum relative error " +
                                          "for this approximation is less than 2^-14.";
                        break;
                    case "vrcp14ps":
                        instructionInfo = "This instruction performs a SIMD computation of the approximate reciprocals " +
                                          "of the packed single-precision floating-point values in the source operand " +
                                          "(the second operand) and stores the packed single-precision floating-point " +
                                          "results in the destination operand (the first operand). The maximum relative " +
                                          "error for this approximation is less than 2^-14.";
                        break;
                    case "vrcp14sd":
                        instructionInfo = "This instruction performs a SIMD computation of the approximate reciprocal " +
                                          "of the low double-precision floating-point value in the second source operand " +
                                          "(the third operand) stores the result in the low quadword element of the " +
                                          "destination operand (the first operand) according to the writemask k1. Bits " +
                                          "(127:64) of the XMM register destination are copied from corresponding bits " +
                                          "in the first source operand (the second operand). The maximum relative error " +
                                          "for this approximation is less than 2^-14. The source operand can " +
                                          "be an XMM register or a 64-bit memory location. The destination operand is an " +
                                          "XMM register.";
                        break;
                    case "vrcp14ss":
                        instructionInfo = "This instruction performs a SIMD computation of the approximate reciprocal " +
                                          "of the low single-precision floating-point value in the second source operand " +
                                          "(the third operand) and stores the result in the low quadword element of the " +
                                          "destination operand (the first operand) according to the writemask k1. Bits " +
                                          "(127:32) of the XMM register destination are copied from corresponding bits " +
                                          "in the first source operand (the second operand). The maximum relative error " +
                                          "for this approximation is less than 2<sup>-14</sup>. The source operand can " +
                                          "be an XMM register or a 32-bit memory location. The destination operand is an " +
                                          "XMM register.";
                        break;
                    case "vreducepd":
                        instructionInfo = "Perform reduction transformation of the packed binary encoded double-precision " +
                                          "FP values in the source operand (the second operand) and store the reduced " +
                                          "results in binary FP format to the destination operand (the first operand) " +
                                          "under the writemask k1.";
                        break;
                    case "vreduceps":
                        instructionInfo = "Perform reduction transformation of the packed binary encoded single-precision " +
                                          "FP values in the source operand (the second operand) and store the reduced " +
                                          "results in binary FP format to the destination operand (the first operand) " +
                                          "under the writemask k1.";
                        break;
                    case "vreducesd":
                        instructionInfo = "Perform a reduction transformation of the binary encoded double-precision FP " +
                                          "value in the low qword element of the second source operand (the third operand) " +
                                          "and store the reduced result in binary FP format to the low qword element of " +
                                          "the destination operand (the first operand) under the writemask k1. Bits 127:64 " +
                                          "of the destination operand are copied from respective qword elements of the " +
                                          "first source operand (the second operand).";
                        break;
                    case "vreducess":
                        instructionInfo = "Perform a reduction transformation of the binary encoded single-precision FP " +
                                          "value in the low dword element of the second source operand (the third operand) " +
                                          "and store the reduced result in binary FP format to the low dword element of " +
                                          "the destination operand (the first operand) under the writemask k1. Bits 127:32 " +
                                          "of the destination operand are copied from respective dword elements of the " +
                                          "first source operand (the second operand).";
                        break;
                    case "vrndscalepd":
                        instructionInfo = "Round the double-precision floating-point values in the source operand by the " +
                                          "rounding mode specified in the immediate operand and places the result in the " +
                                          "destination operand.";
                        break;
                    case "vrndscaleps":
                        instructionInfo = "Round the single-precision floating-point values in the source operand by the " +
                                          "rounding mode specified in the immediate operand and places the result in " +
                                          "the destination operand.";
                        break;
                    case "vrndscalesd":
                        instructionInfo = "Rounds a double-precision floating-point value in the low quadword element of " +
                                          "the second source operand (the third operand) by the rounding mode specified " +
                                          "in the immediate operand and places the result in the corresponding element of " +
                                          "the destination operand (the first operand) according to the writemask. " +
                                          "The quadword element at bits 127:64 of the destination is copied from the " +
                                          "first source operand (the second operand).";
                        break;
                    case "vrndscaless":
                        instructionInfo = "Rounds the single-precision floating-point value in the low doubleword element " +
                                          "of the second source operand (the third operand) by the rounding mode specified " +
                                          "in the immediate operand and places the result in the corresponding element of " +
                                          "the destination operand (the first operand) according to the writemask. " +
                                          "The double-word elements at bits 127:32 of the destination are copied from " +
                                          "the first source operand (the second operand).";
                        break;
                    case "vrsqrt14pd":
                        instructionInfo = "This instruction performs a SIMD computation of the approximate reciprocals " +
                                          "of the square roots of the eight packed double-precision floating-point values " +
                                          "in the source operand (the second operand) and stores the packed double-precision " +
                                          "floating-point results in the destination operand (the first operand) " +
                                          "according to the writemask. The maximum relative error for this approximation " +
                                          "is less than 2^-14.";
                        break;
                    case "vrsqrt14ps":
                        instructionInfo = "This instruction performs a SIMD computation of the approximate reciprocals " +
                                          "of the square roots of 16 packed single-precision floating-point values in " +
                                          "the source operand (the second operand) and stores the packed single-precision " +
                                          "floating-point results in the destination operand (the first operand) according " +
                                          "to the writemask. The maximum relative error for this approximation is less " +
                                          "than 2-14.";
                        break;
                    case "vrsqrt14sd":
                        instructionInfo = "Computes the approximate reciprocal of the square roots of the scalar " +
                                          "double-precision floating-point value in the low quadword element of the " +
                                          "source operand (the second operand) and stores the result in the low quadword " +
                                          "element of the destination operand (the first operand) according to the writemask. " +
                                          "The maximum relative error for this approximation is less than 2^-14. " +
                                          "The source operand can be an XMM register or a 32-bit memory location. " +
                                          "The destination operand is an XMM register.";
                        break;
                    case "vrsqrt14ss":
                        instructionInfo = "Computes of the approximate reciprocal of the square root of the scalar " +
                                          "single-precision floating-point value in the low doubleword element of the " +
                                          "source operand (the second operand) and stores the result in the low doubleword " +
                                          "element of the destination operand (the first operand) according to the writemask. " +
                                          "The maximum relative error for this approximation is less than 2^-14. " +
                                          "The source operand can be an XMM register or a 32-bit memory location. " +
                                          "The destination operand is an XMM register.";
                        break;
                    case "vscalefpd":
                        instructionInfo = "Performs a floating-point scale of the packed double-precision floating-point " +
                                          "values in the first source operand by multiplying it by 2 power of the " +
                                          "double-precision floating-point values in second source operand.";
                        break;
                    case "vscalefps":
                        instructionInfo = "Performs a floating-point scale of the packed single-precision floating-point " +
                                          "values in the first source operand by multiplying it by 2 power of the float32 " +
                                          "values in second source operand.";
                        break;
                    case "vscalefsd":
                        instructionInfo = "Performs a floating-point scale of the packed double-precision floating-point " +
                                          "value in the first source operand by multiplying it by 2 power of the " +
                                          "double-precision floating-point value in second source operand.";
                        break;
                    case "vscalefss":
                        instructionInfo = "Performs a floating-point scale of the scalar single-precision floating-point " +
                                          "value in the first source operand by multiplying it by 2 power of the " +
                                          "float32 value in second source operand.";
                        break;
                    case "vscatterdps":
                    case "vscatterdpd":
                    case "vscatterqps":
                    case "vscatterqpd":
                        instructionInfo = "Stores up to 16 elements (or 8 elements) in doubleword/quadword vector zmm1 " +
                                          "to the memory locations pointed by base address BASE_ADDR and index vector " +
                                          "VINDEX, with scale SCALE. The elements are specified via the VSIB (i.e., the " +
                                          "index register is a vector register, holding packed indices). Elements will " +
                                          "only be stored if their corresponding mask bit is one. The entire mask register " +
                                          "will be set to zero by this instruction unless it triggers an exception.";
                        break;
                    case "vshuff32x4":
                    case "vshuff64x2":
                    case "vshufi32x4":
                    case "vshufi64x2":
                        instructionInfo = "256-bit Version: Moves one of the two 128-bit packed single-precision " +
                                          "floating-point values from the first source operand (second operand) into the " +
                                          "low 128-bit of the destination operand (first operand); moves one of the two " +
                                          "packed 128-bit floating-point values from the second source operand (third " +
                                          "operand) into the high 128-bit of the destination operand. The selector operand " +
                                          "(third operand) determines which values are moved to the destination operand.";
                        break;
                    case "vtestpd":
                    case "vtestps":
                        instructionInfo = "VTESTPS performs a bitwise comparison of all the sign bits of the packed " +
                                          "single-precision elements in the first source operation and corresponding " +
                                          "sign bits in the second source operand. If the AND of the source sign bits " +
                                          "with the dest sign bits produces all zeros, the ZF is set else the ZF is clear. " +
                                          "If the AND of the source sign bits with the inverted dest sign bits produces " +
                                          "all zeros the CF is set else the CF is clear. An attempt to execute VTESTPS " +
                                          "with VEX.W=1 will cause #UD.";
                        break;
                    case "vzeroall":
                        instructionInfo = "The instruction zeros contents of all XMM or YMM registers.";
                        break;
                    case "vzeroupper":
                        instructionInfo = "The instruction zeros the bits in position 128 and higher of all YMM registers. " +
                                          "The lower 128-bits of the registers (the corresponding XMM registers) are unmodified.";
                        break;
                    case "wbinvd":
                        instructionInfo = "Writes back all modified cache lines in the processors internal " +
                                          "cache to main memory and invalidates (flushes) the internal caches. " +
                                          "The instruction then issues a special-function bus cycle that directs external " +
                                          "caches to also write back modified data and another bus cycle to indicate that " +
                                          "the external caches should be invalidated.";
                        break;
                    case "wrfsbase":
                    case "wrgsbase":
                        instructionInfo = "Loads the FS or GS segment base address with the general-purpose register " +
                                          "indicated by the modR/M:r/m field.";
                        break;
                    case "wrmsr":
                        instructionInfo = "Writes the contents of registers EDX:EAX into the 64-bit model specific " +
                                          "register (MSR) specified in the ECX register. (On processors that support the " +
                                          "Intel 64 architecture, the high-order 32 bits of RCX are ignored.) " +
                                          "The contents of the EDX register are copied to high-order 32 bits of the " +
                                          "selected MSR and the contents of the EAX register are copied to low-order 32 " +
                                          "bits of the MSR. (On processors that support the Intel 64 architecture, the " +
                                          "high-order 32 bits of each of RAX and RDX are ignored.) Undefined or reserved " +
                                          "bits in an MSR should be set to values previously read.";
                        break;
                    case "wrpkru":
                        instructionInfo = "Writes the value of EAX into PKRU. ECX and EDX must be 0 when WRPKRU is executed; " +
                                          "otherwise, a general-protection exception (#GP) occurs.";
                        break;
                    case "xabort":
                        instructionInfo = "XABORT forces an RTM abort. Following an RTM abort, the logical processor " +
                                          "resumes execution at the fallback address computed through the outermost " +
                                          "XBEGIN instruction. The EAX register is updated to reflect an XABORT instruction " +
                                          "caused the abort, and the imm8 argument will be provided in bits 31:24 of EAX.";
                        break;
                    case "xacquire":
                    case "xrelease":
                        instructionInfo = "The XACQUIRE prefix is a hint to start lock elision on the memory address " +
                                          "specified by the instruction and the XRELEASE prefix is a hint to end lock " +
                                          "elision on the memory address specified by the instruction.";
                        break;
                    case "xadd":
                        instructionInfo = "Exchanges the first operand (destination operand) with the second operand " +
                                          "(source operand), then loads the sum of the two values into the destination " +
                                          "operand. The destination operand can be a register or a memory location; the source operand is a register.";
                        break;
                    case "xbegin":
                        instructionInfo = "The XBEGIN instruction specifies the start of an RTM code region. If the " +
                                          "logical processor was not already in transactional execution, then the XBEGIN " +
                                          "instruction causes the logical processor to transition into transactional " +
                                          "execution. The XBEGIN instruction that transitions the logical processor into " +
                                          "transactional execution is referred to as the outermost XBEGIN instruction. " +
                                          "The instruction also specifies a relative offset to compute the address of the " +
                                          "fallback code path following a transactional abort.";
                        break;
                    case "xchg":
                        instructionInfo = "Exchanges the contents of the destination (first) and source (second) operands. " +
                                          "The operands can be two general-purpose registers or a register and a memory " +
                                          "location. If a memory operand is referenced, the processors locking " +
                                          "protocol is automatically implemented for the duration of the exchange operation, " +
                                          "regardless of the presence or absence of the LOCK prefix or of the value of the IOPL.";
                        break;
                    case "xend":
                        instructionInfo = "The instruction marks the end of an RTM code region. If this corresponds to " +
                                          "the outermost scope (that is, including this XEND instruction, the number of " +
                                          "XBEGIN instructions is the same as number of XEND instructions), the logical " +
                                          "processor will attempt to commit the logical processor state atomically. " +
                                          "If the commit fails, the logical processor will rollback all architectural " +
                                          "register and memory updates performed during the RTM execution. The logical " +
                                          "processor will resume execution at the fallback address computed from the " +
                                          "outermost XBEGIN instruction. The EAX register is updated to reflect RTM abort " +
                                          "information.";
                        break;
                    case "xgetbv":
                        instructionInfo = "Reads the contents of the extended control register (XCR) specified in the " +
                                          "ECX register into registers EDX:EAX. (On processors that support the Intel 64 " +
                                          "architecture, the high-order 32 bits of RCX are ignored.) The EDX register is " +
                                          "loaded with the high-order 32 bits of the XCR and the EAX register is loaded " +
                                          "with the low-order 32 bits. (On processors that support the Intel 64 architecture, " +
                                          "the high-order 32 bits of each of RAX and RDX are cleared.) If fewer than 64 bits " +
                                          "are implemented in the XCR being read, the values returned to EDX:EAX in " +
                                          "unimplemented bit locations are undefined.";
                        break;
                    case "xlat":
                    case "xlatb":
                        instructionInfo = "Locates a byte entry in a table in memory, using the contents of the AL " +
                                          "register as a table index, then copies the contents of the table entry back " +
                                          "into the AL register. The index in the AL register is treated as an unsigned integer. " +
                                          "The XLAT and XLATB instructions get the base address of the table in memory " +
                                          "from either the DS:EBX or the DS:BX registers (depending on the address-size " +
                                          "attribute of the instruction, 32 or 16, respectively). (The DS segment may be " +
                                          "overridden with a segment override prefix.)";
                        break;
                    case "xor":
                        instructionInfo = "Performs a bitwise exclusive OR (XOR) operation on the destination (first) " +
                                          "and source (second) operands and stores the result in the destination operand " +
                                          "location. The source operand can be an immediate, a register, or a memory " +
                                          "location; the destination operand can be a register or a memory location. " +
                                          "(However, two memory operands cannot be used in one instruction.) Each bit of " +
                                          "the result is 1 if the corresponding bits of the operands are different; each " +
                                          "bit is 0 if the corresponding bits are the same.";
                        break;
                    case "xorpd":
                    case "vxorpd":
                        instructionInfo = "Performs a bitwise logical XOR of the two, four or eight packed double-precision " +
                                          "floating-point values from the first source operand and the second source " +
                                          "operand, and stores the result in the destination operand";
                        break;
                    case "xorps":
                    case "vxorps":
                        instructionInfo = "Performs a bitwise logical XOR of the four, eight or sixteen packed " +
                                          "single-precision floating-point values from the first source operand and the " +
                                          "second source operand, and stores the result in the destination operand";
                        break;
                    case "xrstor":
                    case "xrstor64":
                        instructionInfo = "Performs a full or partial restore of processor state components from the " +
                                          "XSAVE area located at the memory address specified by the source operand. " +
                                          "The implicit EDX:EAX register pair specifies a 64-bit instruction mask. " +
                                          "The specific state components restored correspond to the bits set in the " +
                                          "requested-feature bitmap (RFBM), which is the logical-AND of EDX:EAX and XCR0.";
                        break;
                    case "xrstors":
                    case "xrstors64":
                        instructionInfo = "Performs a full or partial restore of processor state components from the " +
                                          "XSAVE area located at the memory address specified by the source operand. " +
                                          "The implicit EDX:EAX register pair specifies a 64-bit instruction mask. " +
                                          "The specific state components restored correspond to the bits set in the " +
                                          "requested-feature bitmap (RFBM), which is the logical-AND of EDX:EAX and the " +
                                          "logical-OR of XCR0 with the IA32_XSS MSR. XRSTORS may be executed only if CPL = 0.";
                        break;
                    case "xsave":
                    case "xsave64":
                        instructionInfo = "Performs a full or partial save of processor state components to the XSAVE " +
                                          "area located at the memory address specified by the destination operand. " +
                                          "The implicit EDX:EAX register pair specifies a 64-bit instruction mask. " +
                                          "The specific state components saved correspond to the bits set in the " +
                                          "requested-feature bitmap (RFBM), which is the logical-AND of EDX:EAX and XCR0.";
                        break;
                    case "xsavec":
                    case "xsavec64":
                        instructionInfo = "Performs a full or partial save of processor state components to the XSAVE " +
                                          "area located at the memory address specified by the destination operand. " +
                                          "The implicit EDX:EAX register pair specifies a 64-bit instruction mask. " +
                                          "The specific state components saved correspond to the bits set in the " +
                                          "requested-feature bitmap (RFBM), which is the logical-AND of EDX:EAX and XCR0.";
                        break;
                    case "xsaveopt":
                    case "xsaveopt64":
                        instructionInfo = "Performs a full or partial save of processor state components to the XSAVE " +
                                          "area located at the memory address specified by the destination operand. " +
                                          "The implicit EDX:EAX register pair specifies a 64-bit instruction mask. " +
                                          "The specific state components saved correspond to the bits set in the " +
                                          "requested-feature bitmap (RFBM), which is the logical-AND of EDX:EAX and XCR0.";
                        break;
                    case "xsaves":
                    case "xsaves64":
                        instructionInfo = "Performs a full or partial save of processor state components to the XSAVE " +
                                          "area located at the memory address specified by the destination operand. " +
                                          "The implicit EDX:EAX register pair specifies a 64-bit instruction mask. " +
                                          "The specific state components saved correspond to the bits set in the " +
                                          "requested-feature bitmap (RFBM), the logicalAND of EDX:EAX and the logical-OR " +
                                          "of XCR0 with the IA32_XSS MSR. XSAVES may be executed only if CPL = 0.";
                        break;
                    case "xsetbv":
                        instructionInfo = "Writes the contents of registers EDX:EAX into the 64-bit extended control " +
                                          "register (XCR) specified in the ECX register. (On processors that support " +
                                          "the Intel 64 architecture, the high-order 32 bits of RCX are ignored.) " +
                                          "The contents of the EDX register are copied to high-order 32 bits of the " +
                                          "selected XCR and the contents of the EAX register are copied to low-order " +
                                          "32 bits of the XCR. (On processors that support the Intel 64 architecture, " +
                                          "the high-order 32 bits of each of RAX and RDX are ignored.) Undefined or " +
                                          "reserved bits in an XCR should be set to values previously read.";
                        break;
                    case "xtest":
                        instructionInfo = "The XTEST instruction queries the transactional execution status. If the " +
                                          "instruction executes inside a transactionally executing RTM region or a " +
                                          "transactionally executing HLE region, then the ZF flag is cleared, else it is set.";
                        break;
                    case "invept":
                        instructionInfo = "Invalidates mappings in the translation lookaside buffers (TLBs) and paging-structure " +
                                          "caches that were derived from extended page tables (EPT). Invalidation is based on the " +
                                          "INVEPT type specified in the register operand and the INVEPT descriptor specified in the memory operand.";
                        break;
                    case "invvpid":
                        instructionInfo = "Invalidates mappings in the translation lookaside buffers (TLBs) and " +
                                          "paging-structure caches based on virtualprocessor identifier (VPID). " +
                                          "Invalidation is based on the INVVPID type specified in the register " +
                                          "operand and the INVVPID descriptor specified in the memory operand.";
                        break;
                    case "vmcall":
                        instructionInfo = "This instruction allows guest software can make a call for service into an " +
                                          "underlying VM monitor. The details of the programming interface for such calls " +
                                          "are VMM-specific; this instruction does nothing more than cause a VM exit, " +
                                          "registering the appropriate exit reason.";
                        break;
                    case "vmclear":
                        instructionInfo = "This instruction applies to the VMCS whose VMCS region resides at the physical " +
                                          "address contained in the instruction operand. The instruction ensures that " +
                                          "VMCS data for that VMCS (some of these data may be currently maintained on " +
                                          "the processor) are copied to the VMCS region in memory. It also initializes " +
                                          "parts of the VMCS region (for example, it sets the launch state of that VMCS to clear).";
                        break;
                    case "vmfunc":
                        instructionInfo = "This instruction allows software in VMX non-root operation to invoke a VM " +
                                          "function, which is processor functionality enabled and configured by software " +
                                          "in VMX root operation. The value of EAX selects the specific VM function being " +
                                          "invoked.";
                        break;
                    case "vmlaunch":
                        instructionInfo = "Effects a VM entry managed by the current VMCS. VMLAUNCH fails if the launch " +
                                          "state of current VMCS is not \"clear\". If the instruction is successful, it " +
                                          "sets the launch state to \"launched.\"";
                        break;
                    case "vmresume":
                        instructionInfo = "Effects a VM entry managed by the current VMCS. VMRESUME fails if the launch " +
                                          "state of the current VMCS is not \"launched.\"";
                        break;
                    case "vmptrld":
                        instructionInfo = "Marks the current-VMCS pointer valid and loads it with the physical address " +
                                          "in the instruction operand. The instruction fails if its operand is not properly " +
                                          "aligned, sets unsupported physical-address bits, or is equal to the VMXON " +
                                          "pointer. In addition, the instruction fails if the 32 bits in memory referenced " +
                                          "by the operand do not match the VMCS revision identifier supported by this processor.";
                        break;
                    case "vmptrst":
                        instructionInfo = "Stores the current-VMCS pointer into a specified memory address. The operand " +
                                          "of this instruction is always 64 bits and is always in memory.";
                        break;
                    case "vmread":
                        instructionInfo = "Reads a specified field from a VMCS and stores it into a specified destination " +
                                          "operand (register or memory). In VMX root operation, the instruction reads " +
                                          "from the current VMCS. If executed in VMX non-root operation, the instruction " +
                                          "reads from the VMCS referenced by the VMCS link pointer field in the current VMCS.";
                        break;
                    case "vmwrite":
                        instructionInfo = "Writes the contents of a primary source operand (register or memory) to a " +
                                          "specified field in a VMCS. In VMX root operation, the instruction writes to " +
                                          "the current VMCS. If executed in VMX non-root operation, the instruction writes " +
                                          "to the VMCS referenced by the VMCS link pointer field in the current VMCS.";
                        break;
                    case "vmxoff":
                        instructionInfo = "Takes the logical processor out of VMX operation, unblocks INIT signals, " +
                                          "conditionally re-enables A20M, and clears any address-range monitoring.";
                        break;
                    case "vmxon":
                        instructionInfo = "Puts the logical processor in VMX operation with no current VMCS, blocks INIT " +
                                          "signals, disables A20M, and clears any address-range monitoring established by " +
                                          "the MONITOR instruction.";
                        break;
                    case "prefetchwt1":
                        instructionInfo = "Fetches the line of data from memory that contains the byte specified with the " +
                                          "source operand to a location in the cache hierarchy specified by an intent to " +
                                          "write hint (so that data is brought into ‘Exclusive’ state via a request for " +
                                          "ownership) and a locality hint: T1 (temporal data with respect to first level " +
                                          "cache)—prefetch data into the second level cache.";
                        break;
                    case "ud2":
                        instructionInfo = "Generates an invalid opcode exception. This instruction is provided for software " +
                                          "testing to explicitly generate an invalid opcode. The opcode for this instruction " +
                                          "is reserved for this purpose.";
                        break;
                    case "rex64":
                        instructionInfo = "Specify that the instruction is 64 bit.";
                        break;
                    case "extrq":
                        instructionInfo = "Extract field from register.";
                        break;
                    case "insertq":
                        instructionInfo = "Insert field.";
                        break;
                    case "movntsd":
                        instructionInfo = "Move non-temporal scalar double-precision floating point.";
                        break;
                    case "movntss":
                    case "vmovntss":
                        instructionInfo = "Move non-temporal scalar single-presicion floating point.";
                        break;
                    case "femms":
                        instructionInfo = "Faster enter/exit of the mmx or floating-point state.";
                        break;
                    case "paddsiw":
                        instructionInfo = "This instruction adds the signed words of the source operand to the signed " +
                                          "words of the destination operand and writes the results to the implied MMX register. " +
                                          "The purpose of this instruction is the same as the PADDSW instruction, except " +
                                          "that it preserves both source operands.";
                        break;
                    case "paveb":
                        instructionInfo = "	The PAVEB insruction calculates the average of the unsigned bytes of the " +
                                          "source operand and the unsigned bytes of the destination operand and writes " +
                                          "the result to the MMX register.";
                        break;
                    case "pavgusb":
                        instructionInfo = "Average of unsigned packed 8-bit values.";
                        break;
                    case "pdistib":
                        instructionInfo = "The PDISTIB instruction calculates the distance between the unsigned bytes " +
                                          "of two source operands, adds the result to the unsigned byte in the implied " +
                                          "destination operand, and saturates the result. The result is written to the " +
                                          "implied MMX register. The DEST must be an MMX register. The SRC must be a " +
                                          "64-bit memory operand. The accumulator and destination is an MMX register which " +
                                          "depends on the DEST.";
                        break;
                    case "pf2id":
                        instructionInfo = "Converts packed floating-point operand to packed 32-bit integer.";
                        break;
                    case "pfacc":
                        instructionInfo = "Floating-point accumulate";
                        break;
                    case "pfadd":
                        instructionInfo = "Packed, floating-point addition";
                        break;
                    case "pfcmpeq":
                        instructionInfo = "Packed floating-point comparison, equal to";
                        break;
                    case "pfcmpge":
                        instructionInfo = "Packed floating-point comparison, greater than or equal to";
                        break;
                    case "pfcmpgt":
                        instructionInfo = "Packed floating-point comparison, greater than";
                        break;
                    case "pfmax":
                        instructionInfo = "Packed floating-point maximum";
                        break;
                    case "pfmin":
                        instructionInfo = "Packed floating-point minimum";
                        break;
                    case "pfmul":
                        instructionInfo = "Packed floating-point multiplication";
                        break;
                    case "pfrcp":
                        instructionInfo = "Floating-point reciprocal approximation";
                        break;
                    case "pfrcpit1":
                        instructionInfo = "Packed floating-point reciprocal, first iteration step";
                        break;
                    case "pfrcpit2":
                        instructionInfo = "Packed floating-point reciprocal/reciprocal square root, second iteration step";
                        break;
                    case "pfrsqit1":
                        instructionInfo = "Packed floating-point reciprocal square root, first iteration step";
                        break;
                    case "pfrsqrt":
                        instructionInfo = "Floating-point reciprocal square root approximation";
                        break;
                    case "pfsub":
                        instructionInfo = "Packed floating-point subtraction";
                        break;
                    case "pfsubr":
                        instructionInfo = "Packed floating-point reverse subtraction";
                        break;
                    case "pi2fd":
                        instructionInfo = "Packed 32-bit integer to floating-point conversion";
                        break;
                    case "pmulhrw":
                        instructionInfo = "Multiply signed packed 16-bit values with rounding and store the high 16 bits";
                        break;
                    case "prefetch":
                        instructionInfo = "Prefetch processor cache line into L1 data cache";
                        break;
                    case "pmachriw":
                        instructionInfo = "Multiplies the two source operands using the method described for PMULHRW, " +
                                          "and then accumulates the result with the value in the implied destination " +
                                          "register using wrap-around arithmetic. The final result is placed in the implied " +
                                          "DEST register. The DEST must be an MMX register. The SRC must be a 64-bit memory " +
                                          "operand. The destination operand is an implied MMX register that depends on the DEST.";
                        break;
                    case "pmagw":
                        instructionInfo = "Compares the absolute value of the packed words in first and second register, " +
                                          "and stores the largest word in the first register.";
                        break;
                    case "pmulhriw":
                        instructionInfo = "Multiply the packed words in the two registers.";
                        break;
                    case "pmvzb":
                        instructionInfo = "Packed conditional move each byte from soruce register to destination register, " +
                                          "when the corresponding byte in the MMX register is zero.";
                        break;
                    case "pmvgezb":
                        instructionInfo = "Packed conditional move each byte from source register to destination reqister, " +
                                          "when the corresponding byte in the MMX register is greather than or equal to zero.";
                        break;
                    case "pmvlzb":
                        instructionInfo = "Packed conditional move each byte from source register to destination reqister, " +
                                          "when the corresponding byte in the MMX register is less than zero.";
                        break;
                    case "pmvnzb":
                        instructionInfo = "Packed conditional move each byte from source register to destination reqister," +
                                          " when the corresponding byte in the MMX register is not zero.";
                        break;
                    case "pmovsxbd":
                        instructionInfo = "Sign extend the lower 8-bit integer of each packed dword element into packed signed dword integers.";
                        break;
                    case "pmovsxbq":
                        instructionInfo = "Sign extend the lower 8-bit integer of each packed qword element into packed signed qword integers.";
                        break;
                    case "pmovsxbw":
                        instructionInfo = "Sign extend the lower 8-bit integer of each packed word element into packed signed word integers.";
                        break;
                    case "pmovsxdq":
                        instructionInfo = "Sign extend the lower 32-bit integer of each packed qword element into packed signed qword integers.";
                        break;
                    case "pmulhrwa":
                        instructionInfo = "Aligned high multiply of word packed registers with rounding.";
                        break;
                    case "pmulhrwc":
                        instructionInfo = "Packed high multiply of word packed complex numbers with rounding.";
                        break;
                    case "vmaskmovps":
                        instructionInfo = "Conditionally load packed single-precision values from third operand using " +
                                          "mask in second operand and store in first operand.";
                        break;
                    case "vmaskmovpd":
                        instructionInfo = "Conditionally load packed double-precision values from third operand using " +
                                          "mask in second operand and store in first operand.";
                        break;
                    case "vmaskmovdqu":
                        instructionInfo = "Selectively write bytes from first operand to memory location using the byte " +
                                          "mask in second operand. The default memory location is specified by DS:DI/EDI/RDI";
                        break;
                    case "vldqqu":
                        instructionInfo = "The instruction is a special 128-bit unaligned load designed to avoid cache " +
                                          "line splits. If the address of a 16- byte load is on a 16-byte boundary, it " +
                                          "loads the bytes requested. If the address of the load is not aligned on a " +
                                          "16-byte boundary, it loads a 32-byte block starting at the 16-byte aligned " +
                                          "address immediately below the load request. It then extracts the requested 16 byte";
                        break;
                    case "vmovntqq":
                        instructionInfo = "Store 256 bits of data from ymm register into memory using non-temporal hint.";
                        break;
                    case "vmovqqa":
                        instructionInfo = "Move 256 bits of data aligned either from memory to register ymm, or the other way.";
                        break;
                    case "vmovqqu":
                        instructionInfo = "Move 256 bits of data unaligned either from memory to register ymm or the other way.";
                        break;
                    default:
                        instructionInfo = string.Empty;
                        returnValue = false;
                        break;
                }

                return returnValue;
            }
        }
    }
}
