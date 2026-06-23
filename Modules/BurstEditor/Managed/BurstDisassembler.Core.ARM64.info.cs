// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace Unity.Burst.Editor
{
    internal partial class BurstDisassembler
    {
        internal class ARM64InstructionInfo
        {
            internal static bool GetARM64Info(string instructionName, out string instructionInfo)
            {
                var instr = ARM64AsmTokenKindProvider.TryRemoveT(new StringSlice(instructionName));
                var retVal = TryFindInstructionInfo(instr, out instructionInfo);

                if (retVal)
                {
                    return retVal;
                }

                // Could not find info, so try and remove possible instruction condition code.
                instr = ARM64AsmTokenKindProvider.TryRemoveCond(instr);
                return TryFindInstructionInfo(instr, out instructionInfo);
            }

            private static bool TryFindInstructionInfo(StringSlice instr, out string instructionInfo)
            {
                var returnValue = true;

                switch (instr.ToString())
                {
                    case "bkpt":
                        instructionInfo = "Functions as breakpoint by causing the core to enter Debug state.";
                        break;
                    case "usat":
                        instructionInfo = "Unsigned saturate to any bit position, with optional shift before saturating.";
                        break;
                    case "stmia":
                    case "stm":
                        instructionInfo = "Store multiple registers incrementing address after each transfer.";
                        break;
                    case "stmib":
                        instructionInfo = "Store multiple registers incrementing address before each transfer.";
                        break;
                    case "stmda":
                        instructionInfo = "Store multiple registers decrement address after each transfer.";
                        break;
                    case "stmdb":
                        instructionInfo = "Store multiple registers decrement address before each transfer.";
                        break;
                    case "ldmia":
                    case "ldm":
                        instructionInfo = "Load multiple registers incrementing address after each transfer.";
                        break;
                    case "ldmib":
                        instructionInfo = "Load multiple registers incrementing address before each transfer.";
                        break;
                    case "ldmda":
                        instructionInfo = "Load multiple registers decrement address after each transfer.";
                        break;
                    case "ldmdb":
                        instructionInfo = "Load multiple registers decrement addres before each transfer.";
                        break;
                    case "adc":
                        instructionInfo = "Add with Carry.";
                        break;
                    case "add":
                    case "addw":
                        instructionInfo = "Add.";
                        break;
                    case "adds":
                        instructionInfo = "Add, setting flags.";
                        break;
                    case "vadd":
                        instructionInfo = "Adds corresponding elements of two vectors.";
                        break;
                    case "vaddl":
                        instructionInfo = "Vector add long.";
                        break;
                    case "vaddw":
                        instructionInfo = "Vector add wide.";
                        break;
                    case "adr":
                        instructionInfo = "Form PC-relative address.";
                        break;
                    case "adrl":
                        instructionInfo = "Loads a program or register-relative address.";
                        break;
                    case "adrp":
                        instructionInfo = "Form PC-relative address to 4KB page.";
                        break;
                    case "and":
                        instructionInfo = "Bitwise AND.";
                        break;
                    case "asr":
                        instructionInfo = "Arithmetic Shift Right.";
                        break;
                    case "asrs":
                        instructionInfo = "Arithmetic Shift Right, setting flags.";
                        break;
                    case "uxtab":
                        instructionInfo = "Zero extend Byte and Add.";
                        break;
                    case "uxtah":
                        instructionInfo = "Zero extend Halfword and Add. Extends a 16-bit value to a 32-bit value.";
                        break;
                    case "at":
                        instructionInfo = "Address Translate.";
                        break;
                    case "it":
                    case "itt":
                    case "ittt":
                    case "itttt":
                        instructionInfo = "If-Then condition for the next #t instruction(s).";
                        break;
                    case "ite":
                    case "itte":
                    case "itee":
                    case "iteee":
                    case "ittee":
                    case "ittte":
                        instructionInfo = "If-Then-Else condition running next #t instruction(s) if true and then " +
                                          "the instruction thereafter if condition is false.";
                        break;
                    case "b":
                        instructionInfo = "Branch.";
                        break;
                    case "bfi":
                        instructionInfo = "Bitfield Insert.";
                        break;
                    case "bfm":
                        instructionInfo = "Bitfield Move.";
                        break;
                    case "bfxil":
                        instructionInfo = "Bitfield extract and insert at low end.";
                        break;
                    case "bic":
                        instructionInfo = "Bitwise Bit Clear.";
                        break;
                    case "bl":
                        instructionInfo = "Branch with Link.";
                        break;
                    case "blr":
                        instructionInfo = "Branch with Link to Register.";
                        break;
                    case "br":
                        instructionInfo = "Branch to Register.";
                        break;
                    case "brk":
                        instructionInfo = "Breakpoint instruction.";
                        break;
                    case "cbnz":
                        instructionInfo = "Compare and Branch on Nonzero.";
                        break;
                    case "cbz":
                        instructionInfo = "Compare and Branch on Zero.";
                        break;
                    case "ccmn":
                        instructionInfo = "Conditional Compare Negative.";
                        break;
                    case "ccmp":
                        instructionInfo = "Conditional Compare.";
                        break;
                    case "clrex":
                        instructionInfo = "Clear Exclusive.";
                        break;
                    case "cls":
                        instructionInfo = "Count leading sign bits.";
                        break;
                    case "clz":
                        instructionInfo = "Count leading zero bits.";
                        break;
                    case "cmn":
                        instructionInfo = "Compare Negative.";
                        break;
                    case "cmp":
                        instructionInfo = "Compare.";
                        break;
                    case "crc32b":
                    case "crc32h":
                    case "crc32w":
                    case "crc32x":
                        instructionInfo = "CRC32 checksum performs a cyclic redundancy check (CRC) calculation on a value held in a general-purpose register.";
                        break;
                    case "crc32cb":
                    case "crc32ch":
                    case "crc32cw":
                    case "crc32cx":
                        instructionInfo = "CRC32C checksum performs a cyclic redundancy check (CRC) calculation on a value held in a general-purpose register.";
                        break;
                    case "csel":
                        instructionInfo = "Conditional Select.";
                        break;
                    case "csinc":
                        instructionInfo = "Conditional Select Increment.";
                        break;
                    case "csinv":
                        instructionInfo = "Conditional Select Invert.";
                        break;
                    case "csneg":
                        instructionInfo = "Conditional Select Negation.";
                        break;
                    case "dc":
                        instructionInfo = "Data Cache operation.";
                        break;
                    case "dcps1":
                        instructionInfo = "Debug Change PE State to EL1.";
                        break;
                    case "dcps2":
                        instructionInfo = "Debug Change PE State to EL2.";
                        break;
                    case "dcps3":
                        instructionInfo = "Debug Change PE State to EL3.";
                        break;
                    case "dmb":
                        instructionInfo = "Data Memory Barrier.";
                        break;
                    case "drps":
                        instructionInfo = "Debug restore process state.";
                        break;
                    case "dsb":
                        instructionInfo = "Data Synchronization Barrier.";
                        break;
                    case "eon":
                        instructionInfo = "Bitwise Exclusive OR NOT.";
                        break;
                    case "eor":
                        instructionInfo = "Bitwise Exclusive OR.";
                        break;
                    case "eors":
                        instructionInfo = "Bitwise Exclusive OR, setting flags.";
                        break;
                    case "eret":
                        instructionInfo = "Returns from an exception.";
                        break;
                    case "extr":
                        instructionInfo = "Extract register.";
                        break;
                    case "hint":
                        instructionInfo = "Hint instruction.";
                        break;
                    case "hlt":
                        instructionInfo = "Halt instruction.";
                        break;
                    case "hvc":
                        instructionInfo = "Hypervisor call to allow OS code to call the Hypervisor.";
                        break;
                    case "ic":
                        instructionInfo = "Instruction Cache operation.";
                        break;
                    case "isb":
                        instructionInfo = "Instruction Synchronization Barrier.";
                        break;
                    case "lsl":
                        instructionInfo = "Logical Shift Left.";
                        break;
                    case "lsls":
                        instructionInfo = "Logical Shift Left, setting flags.";
                        break;
                    case "lsr":
                        instructionInfo = "Logical Shift Right.";
                        break;
                    case "lsrs":
                        instructionInfo = "Logical Shift Right, setting flags.";
                        break;
                    case "smmul":
                        instructionInfo = "Signed Most significant word Multiply.";
                        break;
                    case "umaal":
                        instructionInfo = "Unsigned Multiply Accumulate Accumulate Long.";
                        break;
                    case "madd":
                        instructionInfo = "Multiply-Add.";
                        break;
                    case "mneg":
                        instructionInfo = "Multiply-Negate.";
                        break;
                    case "beq":
                        instructionInfo = "Conditional branch Equal.";
                        break;
                    case "bne":
                        instructionInfo = "Conditional branch Not equal.";
                        break;
                    case "bcs":
                        instructionInfo = "Conditional branch Carry set (identical to HS).";
                        break;
                    case "bhs":
                        instructionInfo = "Conditional branch Unsigned higher or same (identical to CS).";
                        break;
                    case "bcc":
                        instructionInfo = "Conditional branch Carry clear (identical to LO).";
                        break;
                    case "blo":
                        instructionInfo = "Conditional branch Unsigned lower (identical to CC).";
                        break;
                    case "bmi":
                        instructionInfo = "Conditional branch Minus or negative result.";
                        break;
                    case "bpl":
                        instructionInfo = "Conditional branch Positive or zero result.";
                        break;
                    case "bvs":
                        instructionInfo = "Conditional branch Overflow.";
                        break;
                    case "bvc":
                        instructionInfo = "Conditional branch No overflow.";
                        break;
                    case "bhi":
                        instructionInfo = "Conditional branch Unsigned higher.";
                        break;
                    case "bls":
                        instructionInfo = "Conditional branch Unsigned lower or same.";
                        break;
                    case "bge":
                        instructionInfo = "Conditional branch Signed greater than or equal.";
                        break;
                    case "blt":
                        instructionInfo = "Conditional branch Signed less than.";
                        break;
                    case "bgt":
                        instructionInfo = "Conditional branch Signed greater than.";
                        break;
                    case "ble":
                        instructionInfo = "Conditional branch Signed less than or equal.";
                        break;
                    case "bal":
                        instructionInfo = "Conditional branch Always (this is the default).";
                        break;
                    case "bnv":
                        instructionInfo = "Conditional branch No overflow.";
                        break;
                    case "bx":
                        instructionInfo = "Branch and exchange instruction set.";
                        break;
                    case "blx":
                        instructionInfo =
                            "transfers program execution to the address specified by label and stores the " +
                            "address of the next instruction in the LR (R14) register. BLX can change the core state from ARM to Thumb, or from Thumb to ARM.";
                        break;
                    case "bxj":
                        instructionInfo = "Branch and change Jazelle state. Jazelle state means the processor executes Java bytecodes.";
                        break;
                    case "mov":
                        instructionInfo = "Move.";
                        break;
                    case "movs":
                        instructionInfo = "Move, setting condition flags.";
                        break;
                    case "movl":
                        instructionInfo = "Load a register with eiter a 32-bit or 64-bit immediate value, or any adress.";
                        break;
                    case "vmov":
                        instructionInfo = "Inset a floating-point immediate value in a single-precision or double-precision register, or" +
                                          "copy one register into another register.";
                        break;
                    case "movk":
                        instructionInfo = "Move wide with keep.";
                        break;
                    case "movn":
                        instructionInfo = "Move wide with NOT.";
                        break;
                    case "movz":
                        instructionInfo = "Move wide with zero.";
                        break;
                    case "movt":
                        instructionInfo = "Move to top half of register.";
                        break;
                    case "movw":
                        instructionInfo = "Move word.";
                        break;
                    case "mrs":
                        instructionInfo = "Move System Register.";
                        break;
                    case "msr":
                        instructionInfo = "Move immediate value to Special Register.";
                        break;
                    case "msub":
                        instructionInfo = "Multiply-Subtract.";
                        break;
                    case "mul":
                        instructionInfo = "Multiply.";
                        break;
                    case "muls":
                        instructionInfo = "Multiply, setting flags.";
                        break;
                    case "vmul":
                        instructionInfo = "Multiplies corresponding elements in two vectors, and places the results in the destination vector.";
                        break;
                    case "mvn":
                        instructionInfo = "Bitwise NOT.";
                        break;
                    case "mvns":
                        instructionInfo = "Bitwise NOT, setting flags.";
                        break;
                    case "neg":
                        instructionInfo = "Negate.";
                        break;
                    case "ngc":
                        instructionInfo = "Negate with Carry.";
                        break;
                    case "nop":
                        instructionInfo = "No Operation.";
                        break;
                    case "orn":
                        instructionInfo = "Bitwise OR NOT.";
                        break;
                    case "orr":
                        instructionInfo = "Bitwise OR.";
                        break;
                    case "orrs":
                        instructionInfo = "Bitwise OR, setting flags.";
                        break;
                    case "rbit":
                        instructionInfo = "Reverse Bits.";
                        break;
                    case "ret":
                        instructionInfo = "Return from subroutine.";
                        break;
                    case "rev16":
                        instructionInfo = "Reverse bytes in 16-bit halfwords.";
                        break;
                    case "rev32":
                        instructionInfo = "Reverse bytes in 32-bit words.";
                        break;
                    case "rev64":
                        instructionInfo = "Reverse Bytes.";
                        break;
                    case "rev":
                        instructionInfo = "Reverse Bytes.";
                        break;
                    case "rrx":
                        instructionInfo = "Rotate right with extend.";
                        break;
                    case "ror":
                        instructionInfo = "Rotate right.";
                        break;
                    case "sbc":
                        instructionInfo = "Subtract with Carry.";
                        break;
                    case "sbfiz":
                        instructionInfo = "Signed Bitfield Insert in Zero.";
                        break;
                    case "sbfm":
                        instructionInfo = "Signed Bitfield Move.";
                        break;
                    case "sbfx":
                        instructionInfo = "Signed Bitfield Extract.";
                        break;
                    case "sdiv":
                        instructionInfo = "Signed Divide.";
                        break;
                    case "sev":
                        instructionInfo = "Send Event.";
                        break;
                    case "sevl":
                        instructionInfo = "Send Event Local.";
                        break;
                    case "smaddl":
                        instructionInfo = "Signed Multiply-Add Long.";
                        break;
                    case "smc":
                        instructionInfo = "Supervisor call to allow OS or Hypervisor code to call the Secure Monitor.";
                        break;
                    case "smnegl":
                        instructionInfo = "Signed Multiply-Negate Long.";
                        break;
                    case "smsubl":
                        instructionInfo = "Signed Multiply-Subtract Long.";
                        break;
                    case "smulh":
                        instructionInfo = "Signed Multiply High.";
                        break;
                    case "smull":
                        instructionInfo = "Signed Multiply Long.";
                        break;
                    case "sub":
                    case "subw":
                        instructionInfo = "Subtract.";
                        break;
                    case "vsub":
                        instructionInfo = "Subtract the elements of one vector from the corresponding elements of another " +
                                          "vector, and places the results in the destination vector.";
                        break;
                    case "subs":
                        instructionInfo = "Subtract (extended register), setting flags.";
                        break;
                    case "rsb":
                        instructionInfo = "Reverse subtract i.e. for \"rsb {Rd, } Rn, <Operand2>\" Rn is subtracted " +
                                          "from operand2 and saved in Rd register.";
                        break;
                    case "rsbs":
                        instructionInfo = "Reverse subtract, setting flags, i.e. for \"rsb {Rd, } Rn, <Operand2>\" Rn is subtracted " +
                                          "from operand2 and saved in Rd register.";
                        break;
                    case "svc":
                        instructionInfo = "Supervisor call to allow application code to call the OS.";
                        break;
                    case "sxtah":
                        instructionInfo = "Sign extend Halfword with Add.";
                        break;
                    case "sxtb":
                        instructionInfo = "Signed Extend Byte.";
                        break;
                    case "sxth":
                        instructionInfo = "Sign Extend Halfword.";
                        break;
                    case "sxtw":
                        instructionInfo = "Sign Extend Word.";
                        break;
                    case "sys":
                        instructionInfo = "System instruction.";
                        break;
                    case "sysl":
                        instructionInfo = "System instruction with result.";
                        break;
                    case "tbnz":
                        instructionInfo = "Test bit and Branch if Nonzero.";
                        break;
                    case "tbz":
                        instructionInfo = "Test bit and Branch if Zero.";
                        break;
                    case "tlbi":
                        instructionInfo = "TLB Invalidate operation.";
                        break;
                    case "tst":
                        instructionInfo = "Test the value in a register against another register, setting the condition flags and discarding the result.";
                        break;
                    case "ubfiz":
                        instructionInfo = "Unsigned Bitfield Insert in Zero.";
                        break;
                    case "ubfm":
                        instructionInfo = "Unsigned Bitfield Move.";
                        break;
                    case "ubfx":
                        instructionInfo = "Unsigned Bitfield Extract.";
                        break;
                    case "udiv":
                        instructionInfo = "Unsigned Divide.";
                        break;
                    case "umaddl":
                        instructionInfo = "Unsigned Multiply-Add Long.";
                        break;
                    case "umnegl":
                        instructionInfo = "Unsigned Multiply-Negate Long.";
                        break;
                    case "umsubl":
                        instructionInfo = "Unsigned Multiply-Subtract Long.";
                        break;
                    case "umulh":
                        instructionInfo = "Unsigned Multiply High.";
                        break;
                    case "umull":
                        instructionInfo = "Unsigned Multiply Long.";
                        break;
                    case "uxtb":
                        instructionInfo = "Unsigned Extend Byte.";
                        break;
                    case "uxth":
                        instructionInfo = "Unsigned Extend Halfword.";
                        break;
                    case "wfe":
                        instructionInfo = "Wait For Event.";
                        break;
                    case "wfi":
                        instructionInfo = "Wait For Interrupt.";
                        break;
                    case "yield":
                        instructionInfo = "YIELD.";
                        break;
                    case "ldar":
                        instructionInfo = "Load-Acquire Register.";
                        break;
                    case "ldarb":
                        instructionInfo = "Load-Acquire Register Byte.";
                        break;
                    case "ldarh":
                        instructionInfo = "Load-Acquire Register Halfword.";
                        break;
                    case "ldaxp":
                        instructionInfo = "Load-Acquire Exclusive Pair of Registers.";
                        break;
                    case "ldaxr":
                        instructionInfo = "Load-Acquire Exclusive Register.";
                        break;
                    case "ldaxrb":
                        instructionInfo = "Load-Acquire Exclusive Register Byte.";
                        break;
                    case "ldaxrh":
                        instructionInfo = "Load-Acquire Exclusive Register Halfword.";
                        break;
                    case "ldnp":
                        instructionInfo = "Load Pair of Registers, with non-temporal hint.";
                        break;
                    case "ldp":
                        instructionInfo = "Load Pair of Registers.";
                        break;
                    case "ldpsw":
                        instructionInfo = "Load Pair of Registers Signed Word.";
                        break;
                    case "ldr":
                        instructionInfo = "Load Register.";
                        break;
                    case "ldrb":
                        instructionInfo = "Load Register Byte.";
                        break;
                    case "ldrh":
                        instructionInfo = "Load Register Halfword.";
                        break;
                    case "ldrd":
                        instructionInfo = "Load Register double.";
                        break;
                    case "ldrsb":
                        instructionInfo = "Load Register Signed Byte.";
                        break;
                    case "ldrsh":
                        instructionInfo = "Load Register Signed Halfword.";
                        break;
                    case "ldrsw":
                        instructionInfo = "Load Register Signed Word.";
                        break;
                    case "ldtr":
                        instructionInfo = "Load Register.";
                        break;
                    case "ldtrb":
                        instructionInfo = "Load Register Byte.";
                        break;
                    case "ldtrh":
                        instructionInfo = "Load Register Halfword.";
                        break;
                    case "ldtrsb":
                        instructionInfo = "Load Register Signed Byte.";
                        break;
                    case "ldtrsh":
                        instructionInfo = "Load Register Signed Halfword.";
                        break;
                    case "ldtrsw":
                        instructionInfo = "Load Register Signed Word.";
                        break;
                    case "ldur":
                        instructionInfo = "Load Register.";
                        break;
                    case "ldurb":
                        instructionInfo = "Load Register Byte.";
                        break;
                    case "ldurh":
                        instructionInfo = "Load Register Halfword.";
                        break;
                    case "ldursb":
                        instructionInfo = "Load Register Signed Byte.";
                        break;
                    case "ldursh":
                        instructionInfo = "Load Register Signed Halfword.";
                        break;
                    case "ldursw":
                        instructionInfo = "Load Register Signed Word.";
                        break;
                    case "ldxp":
                        instructionInfo = "Load Exclusive Pair of Registers.";
                        break;
                    case "ldxr":
                        instructionInfo = "Load Exclusive Register.";
                        break;
                    case "ldxrb":
                        instructionInfo = "Load Exclusive Register Byte.";
                        break;
                    case "ldxrh":
                        instructionInfo = "Load Exclusive Register Halfword.";
                        break;
                    case "prfm":
                        instructionInfo = "Prefetch Memory.";
                        break;
                    case "prfum":
                        instructionInfo = "Prefetch Memory.";
                        break;
                    case "stlr":
                        instructionInfo = "Store-Release Register.";
                        break;
                    case "stlrb":
                        instructionInfo = "Store-Release Register Byte.";
                        break;
                    case "stlrh":
                        instructionInfo = "Store-Release Register Halfword.";
                        break;
                    case "stlxp":
                        instructionInfo = "Store-Release Exclusive Pair of registers.";
                        break;
                    case "stlxr":
                        instructionInfo = "Store-Release Exclusive Register.";
                        break;
                    case "stlxrb":
                        instructionInfo = "Store-Release Exclusive Register Byte.";
                        break;
                    case "stlxrh":
                        instructionInfo = "Store-Release Exclusive Register Halfword.";
                        break;
                    case "stnp":
                        instructionInfo = "Store Pair of Registers, with non-temporal hint.";
                        break;
                    case "stp":
                        instructionInfo = "Store Pair of Registers.";
                        break;
                    case "str":
                        instructionInfo = "Store Register.";
                        break;
                    case "strd":
                        instructionInfo = "Store register double.";
                        break;
                    case "strb":
                        instructionInfo = "Store Register Byte.";
                        break;
                    case "strh":
                        instructionInfo = "Store Register Halfword.";
                        break;
                    case "sttr":
                        instructionInfo = "Store Register.";
                        break;
                    case "sttrb":
                        instructionInfo = "Store Register Byte.";
                        break;
                    case "sttrh":
                        instructionInfo = "Store Register Halfword.";
                        break;
                    case "stur":
                        instructionInfo = "Store Register.";
                        break;
                    case "sturb":
                        instructionInfo = "Store Register Byte.";
                        break;
                    case "sturh":
                        instructionInfo = "Store Register Halfword.";
                        break;
                    case "stxp":
                        instructionInfo = "Store Exclusive Pair of registers.";
                        break;
                    case "stxr":
                        instructionInfo = "Store Exclusive Register.";
                        break;
                    case "stxrb":
                        instructionInfo = "Store Exclusive Register Byte.";
                        break;
                    case "stxrh":
                        instructionInfo = "Store Exclusive Register Halfword.";
                        break;
                    case "fabs":
                        instructionInfo = "Floating-point Absolute value.";
                        break;
                    case "fadd":
                        instructionInfo = "Floating-point Add.";
                        break;
                    case "fccmp":
                        instructionInfo = "Floating-point Conditional quiet Compare.";
                        break;
                    case "fccmpe":
                        instructionInfo = "Floating-point Conditional signaling Compare.";
                        break;
                    case "fcmp":
                        instructionInfo = "Floating-point quiet Compare.";
                        break;
                    case "fcmpe":
                        instructionInfo = "Floating-point signaling Compare.";
                        break;
                    case "fcsel":
                        instructionInfo = "Floating-point Conditional Select.";
                        break;
                    case "fcvt":
                        instructionInfo = "Floating-point Convert precision.";
                        break;
                    case "fcvtas":
                        instructionInfo = "Floating-point Convert to Signed integer, rounding to nearest with ties to Away.";
                        break;
                    case "fcvtau":
                        instructionInfo = "Floating-point Convert to Unsigned integer, rounding to nearest with ties to Away.";
                        break;
                    case "fcvtms":
                        instructionInfo = "Floating-point Convert to Signed integer, rounding toward Minus infinity.";
                        break;
                    case "fcvtmu":
                        instructionInfo = "Floating-point Convert to Unsigned integer, rounding toward Minus infinity.";
                        break;
                    case "fcvtns":
                        instructionInfo = "Floating-point Convert to Signed integer, rounding to nearest with ties to even.";
                        break;
                    case "fcvtnu":
                        instructionInfo = "Floating-point Convert to Unsigned integer, rounding to nearest with ties to even.";
                        break;
                    case "fcvtps":
                        instructionInfo = "Floating-point Convert to Signed integer, rounding toward Plus infinity.";
                        break;
                    case "fcvtpu":
                        instructionInfo = "Floating-point Convert to Unsigned integer, rounding toward Plus infinity.";
                        break;
                    case "fcvtzs":
                        instructionInfo = "Floating-point Convert to Signed fixed-point, rounding toward Zero.";
                        break;
                    case "fcvtzu":
                        instructionInfo = "Floating-point Convert to Unsigned fixed-point, rounding toward Zero.";
                        break;
                    case "fdiv":
                        instructionInfo = "Floating-point Divide.";
                        break;
                    case "fmadd":
                        instructionInfo = "Floating-point fused Multiply-Add.";
                        break;
                    case "fmax":
                        instructionInfo = "Floating-point Maximum.";
                        break;
                    case "fmaxnm":
                        instructionInfo = "Floating-point Maximum Number.";
                        break;
                    case "fmin":
                        instructionInfo = "Floating-point Minimum.";
                        break;
                    case "fminnm":
                        instructionInfo = "Floating-point Minimum Number.";
                        break;
                    case "fmov":
                        instructionInfo = "Floating-point Move register without conversion.";
                        break;
                    case "fmsub":
                        instructionInfo = "Floating-point Fused Multiply-Subtract.";
                        break;
                    case "fmul":
                        instructionInfo = "Floating-point Multiply.";
                        break;
                    case "fneg":
                        instructionInfo = "Floating-point Negate.";
                        break;
                    case "fnmadd":
                        instructionInfo = "Floating-point Negated fused Multiply-Add.";
                        break;
                    case "fnmsub":
                        instructionInfo = "Floating-point Negated fused Multiply-Subtract.";
                        break;
                    case "fnmul":
                        instructionInfo = "Floating-point Multiply-Negate.";
                        break;
                    case "frinta":
                        instructionInfo = "Floating-point Round to Integral, to nearest with ties to Away.";
                        break;
                    case "frinti":
                        instructionInfo = "Floating-point Round to Integral, using current rounding mode.";
                        break;
                    case "frintm":
                        instructionInfo = "Floating-point Round to Integral, toward Minus infinity.";
                        break;
                    case "frintn":
                        instructionInfo = "Floating-point Round to Integral, to nearest with ties to even.";
                        break;
                    case "frintp":
                        instructionInfo = "Floating-point Round to Integral, toward Plus infinity.";
                        break;
                    case "frintx":
                        instructionInfo = "Floating-point Round to Integral exact, using current rounding mode.";
                        break;
                    case "frintz":
                        instructionInfo = "Floating-point Round to Integral, toward Zero.";
                        break;
                    case "fsqrt":
                        instructionInfo = "Floating-point Square Root.";
                        break;
                    case "fsub":
                        instructionInfo = "Floating-point Subtract.";
                        break;
                    case "scvtf":
                        instructionInfo = "Signed fixed-point Convert to Floating-point.";
                        break;
                    case "ucvtf":
                        instructionInfo = "Unsigned fixed-point Convert to Floating-point.";
                        break;
                    case "abs":
                        instructionInfo = "Absolute value.";
                        break;
                    case "vabs":
                        instructionInfo = "Returns the absolute value of each element in a vector.";
                        break;
                    case "addp":
                        instructionInfo = "Add Pair of elements.";
                        break;
                    case "cmeq":
                        instructionInfo = "Compare bitwise Equal.";
                        break;
                    case "cmge":
                        instructionInfo = "Compare signed Greater than or Equal.";
                        break;
                    case "cmgt":
                        instructionInfo = "Compare signed Greater than.";
                        break;
                    case "cmhi":
                        instructionInfo = "Compare unsigned Higher.";
                        break;
                    case "cmhs":
                        instructionInfo = "Compare unsigned Higher or Same.";
                        break;
                    case "cmle":
                        instructionInfo = "Compare signed Less than or Equal to zero.";
                        break;
                    case "cmlt":
                        instructionInfo = "Compare signed Less than zero.";
                        break;
                    case "cmtst":
                        instructionInfo = "Compare bitwise Test bits nonzero.";
                        break;
                    case "dup":
                        instructionInfo = "Duplicate vector element to scalar.";
                        break;
                    case "fabd":
                        instructionInfo = "Floating-point Absolute Difference.";
                        break;
                    case "facge":
                        instructionInfo = "Floating-point Absolute Compare Greater than or Equal.";
                        break;
                    case "facgt":
                        instructionInfo = "Floating-point Absolute Compare Greater than.";
                        break;
                    case "faddp":
                        instructionInfo = "Floating-point Add Pair of elements.";
                        break;
                    case "fcmeq":
                        instructionInfo = "Floating-point Compare Equal.";
                        break;
                    case "fcmge":
                        instructionInfo = "Floating-point Compare Greater than or Equal.";
                        break;
                    case "fcmgt":
                        instructionInfo = "Floating-point Compare Greater than.";
                        break;
                    case "fcmle":
                        instructionInfo = "Floating-point Compare Less than or Equal to zero.";
                        break;
                    case "fcmlt":
                        instructionInfo = "Floating-point Compare Less than zero.";
                        break;
                    case "fcvtxn":
                        instructionInfo = "Floating-point Convert to lower precision Narrow, rounding to odd.";
                        break;
                    case "fmaxnmp":
                        instructionInfo = "Floating-point Maximum Number of Pair of elements.";
                        break;
                    case "fmaxp":
                        instructionInfo = "Floating-point Maximum of Pair of elements.";
                        break;
                    case "fminnmp":
                        instructionInfo = "Floating-point Minimum Number of Pair of elements.";
                        break;
                    case "fminp":
                        instructionInfo = "Floating-point Minimum of Pair of elements.";
                        break;
                    case "fmla":
                        instructionInfo = "Floating-point fused Multiply-Add to accumulator.";
                        break;
                    case "fmls":
                        instructionInfo = "Floating-point fused Multiply-Subtract from accumulator.";
                        break;
                    case "fmulx":
                        instructionInfo = "Floating-point Multiply extended.";
                        break;
                    case "frecpe":
                        instructionInfo = "Floating-point Reciprocal Estimate.";
                        break;
                    case "frecps":
                        instructionInfo = "Floating-point Reciprocal Step.";
                        break;
                    case "frsqrte":
                        instructionInfo = "Floating-point Reciprocal Square Root Estimate.";
                        break;
                    case "frsqrts":
                        instructionInfo = "Floating-point Reciprocal Square Root Step.";
                        break;
                    case "shl":
                        instructionInfo = "Shift Left.";
                        break;
                    case "sli":
                        instructionInfo = "Shift Left and Insert.";
                        break;
                    case "sqabs":
                        instructionInfo = "Signed saturating Absolute value.";
                        break;
                    case "sqadd":
                        instructionInfo = "Signed saturating Add.";
                        break;
                    case "sqdmlal":
                        instructionInfo = "Signed saturating Doubling Multiply-Add Long.";
                        break;
                    case "sqdmlsl":
                        instructionInfo = "Signed saturating Doubling Multiply-Subtract Long.";
                        break;
                    case "sqdmulh":
                        instructionInfo = "Signed saturating Doubling Multiply returning High half.";
                        break;
                    case "sqdmull":
                        instructionInfo = "Signed saturating Doubling Multiply Long.";
                        break;
                    case "sqneg":
                        instructionInfo = "Signed saturating Negate.";
                        break;
                    case "sqrdmulh":
                        instructionInfo = "Signed saturating Rounding Doubling Multiply returning High half.";
                        break;
                    case "sqrshl":
                        instructionInfo = "Signed saturating Rounding Shift Left.";
                        break;
                    case "sqrshrn":
                        instructionInfo = "Signed saturating Rounded Shift Right Narrow.";
                        break;
                    case "sqrshrun":
                        instructionInfo = "Signed saturating Rounded Shift Right Unsigned Narrow.";
                        break;
                    case "sqshl":
                        instructionInfo = "Signed saturating Shift Left.";
                        break;
                    case "sqshlu":
                        instructionInfo = "Signed saturating Shift Left Unsigned.";
                        break;
                    case "sqshrn":
                        instructionInfo = "Signed saturating Shift Right Narrow.";
                        break;
                    case "sqshrun":
                        instructionInfo = "Signed saturating Shift Right Unsigned Narrow.";
                        break;
                    case "sqsub":
                        instructionInfo = "Signed saturating Subtract.";
                        break;
                    case "sqxtn":
                        instructionInfo = "Signed saturating extract Narrow.";
                        break;
                    case "sqxtun":
                        instructionInfo = "Signed saturating extract Unsigned Narrow.";
                        break;
                    case "sri":
                        instructionInfo = "Shift Right and Insert.";
                        break;
                    case "srshl":
                        instructionInfo = "Signed Rounding Shift Left.";
                        break;
                    case "srshr":
                        instructionInfo = "Signed Rounding Shift Right.";
                        break;
                    case "srsra":
                        instructionInfo = "Signed Rounding Shift Right and Accumulate.";
                        break;
                    case "sshl":
                        instructionInfo = "Signed Shift Left.";
                        break;
                    case "sshr":
                        instructionInfo = "Signed Shift Right.";
                        break;
                    case "ssra":
                        instructionInfo = "Signed Shift Right and Accumulate.";
                        break;
                    case "suqadd":
                        instructionInfo = "Signed saturating Accumulate of Unsigned value.";
                        break;
                    case "uqadd":
                        instructionInfo = "Unsigned saturating Add.";
                        break;
                    case "uqrshl":
                        instructionInfo = "Unsigned saturating Rounding Shift Left.";
                        break;
                    case "uqrshrn":
                        instructionInfo = "Unsigned saturating Rounded Shift Right Narrow.";
                        break;
                    case "uqshl":
                        instructionInfo = "Unsigned saturating Shift Left.";
                        break;
                    case "uqshrn":
                        instructionInfo = "Unsigned saturating Shift Right Narrow.";
                        break;
                    case "uqsub":
                        instructionInfo = "Unsigned saturating Subtract.";
                        break;
                    case "uqxtn":
                        instructionInfo = "Unsigned saturating extract Narrow.";
                        break;
                    case "urshl":
                        instructionInfo = "Unsigned Rounding Shift Left.";
                        break;
                    case "urshr":
                        instructionInfo = "Unsigned Rounding Shift Right.";
                        break;
                    case "ursra":
                        instructionInfo = "Unsigned Rounding Shift Right and Accumulate.";
                        break;
                    case "ushl":
                        instructionInfo = "Unsigned Shift Left.";
                        break;
                    case "ushr":
                        instructionInfo = "Unsigned Shift Right.";
                        break;
                    case "usqadd":
                        instructionInfo = "Unsigned saturating Accumulate of Signed value.";
                        break;
                    case "usra":
                        instructionInfo = "Unsigned Shift Right and Accumulate.";
                        break;
                    case "addhn":
                    case "addhn2":
                        instructionInfo = "Add returning High Narrow.";
                        break;
                    case "addv":
                        instructionInfo = "Add across Vector.";
                        break;
                    case "bif":
                        instructionInfo = "Bitwise Insert if False.";
                        break;
                    case "bit":
                        instructionInfo = "Bitwise Insert if True.";
                        break;
                    case "bsl":
                        instructionInfo = "Bitwise Select.";
                        break;
                    case "cnt":
                        instructionInfo = "Population Count per byte.";
                        break;
                    case "ext":
                        instructionInfo = "Extract vector from pair of vectors.";
                        break;
                    case "fcvtl":
                    case "fcvtl2":
                        instructionInfo = "Floating-point Convert to higher precision Long.";
                        break;
                    case "fcvtn":
                    case "fcvtn2":
                        instructionInfo = "Floating-point Convert to lower precision Narrow.";
                        break;
                    case "fcvtxn2":
                        instructionInfo = "Floating-point Convert to lower precision Narrow, rounding to odd.";
                        break;
                    case "fmaxnmv":
                        instructionInfo = "Floating-point Maximum Number across Vector.";
                        break;
                    case "fmaxv":
                        instructionInfo = "Floating-point Maximum across Vector.";
                        break;
                    case "fminnmv":
                        instructionInfo = "Floating-point Minimum Number across Vector.";
                        break;
                    case "fminv":
                        instructionInfo = "Floating-point Minimum across Vector.";
                        break;
                    case "frecpx":
                        instructionInfo = "Floating-point Reciprocal exponent.";
                        break;
                    case "ins":
                        instructionInfo = "Insert vector element from another vector element.";
                        break;
                    case "ld1":
                        instructionInfo = "Load multiple single-element structures to one, two, three, or four registers.";
                        break;
                    case "ld1r":
                        instructionInfo = "Load one single-element structure and Replicate to all lanes.";
                        break;
                    case "ld2":
                        instructionInfo = "Load multiple 2-element structures to two registers.";
                        break;
                    case "ld2r":
                        instructionInfo = "Load single 2-element structure and Replicate to all lanes of two registers.";
                        break;
                    case "ld3":
                        instructionInfo = "Load multiple 3-element structures to three registers.";
                        break;
                    case "ld3r":
                        instructionInfo = "Load single 3-element structure and Replicate to all lanes of three registers.";
                        break;
                    case "ld4":
                        instructionInfo = "Load multiple 4-element structures to four registers.";
                        break;
                    case "ld4r":
                        instructionInfo = "Load single 4-element structure and Replicate to all lanes of four registers.";
                        break;
                    case "mla":
                        instructionInfo = "Multiply-Add to accumulator (vector, by element).";
                        break;
                    case "mls":
                        instructionInfo = "Multiply-Subtract from accumulator (vector, by element).";
                        break;
                    case "movi":
                        instructionInfo = "Move Immediate.";
                        break;
                    case "mvni":
                        instructionInfo = "Move inverted Immediate.";
                        break;
                    case "not":
                        instructionInfo = "Bitwise NOT.";
                        break;
                    case "pmul":
                        instructionInfo = "Polynomial Multiply.";
                        break;
                    case "pmull":
                    case "pmull2":
                        instructionInfo = "Polynomial Multiply Long.";
                        break;
                    case "raddhn":
                    case "raddhn2":
                        instructionInfo = "Rounding Add returning High Narrow.";
                        break;
                    case "rshrn":
                    case "rshrn2":
                        instructionInfo = "Rounding Shift Right Narrow.";
                        break;
                    case "rsubhn":
                    case "rsubhn2":
                        instructionInfo = "Rounding Subtract returning High Narrow.";
                        break;
                    case "saba":
                        instructionInfo = "Signed Absolute difference and Accumulate.";
                        break;
                    case "sabal":
                    case "sabal2":
                        instructionInfo = "Signed Absolute difference and Accumulate Long.";
                        break;
                    case "sabd":
                        instructionInfo = "Signed Absolute Difference.";
                        break;
                    case "sabdl":
                    case "sabdl2":
                        instructionInfo = "Signed Absolute Difference Long.";
                        break;
                    case "sadalp":
                        instructionInfo = "Signed Add and Accumulate Long Pairwise.";
                        break;
                    case "saddl":
                    case "saddl2":
                        instructionInfo = "Signed Add Long.";
                        break;
                    case "saddlp":
                        instructionInfo = "Signed Add Long Pairwise.";
                        break;
                    case "saddlv":
                        instructionInfo = "Signed Add Long across Vector.";
                        break;
                    case "saddw":
                    case "saddw2":
                        instructionInfo = "Signed Add Wide.";
                        break;
                    case "shadd":
                        instructionInfo = "Signed Halving Add.";
                        break;
                    case "shll":
                    case "shll2":
                        instructionInfo = "Shift Left Long.";
                        break;
                    case "shrn":
                    case "shrn2":
                        instructionInfo = "Shift Right Narrow.";
                        break;
                    case "shsub":
                        instructionInfo = "Signed Halving Subtract.";
                        break;
                    case "smax":
                        instructionInfo = "Signed Maximum.";
                        break;
                    case "smaxp":
                        instructionInfo = "Signed Maximum Pairwise.";
                        break;
                    case "smaxv":
                        instructionInfo = "Signed Maximum across Vector.";
                        break;
                    case "smin":
                        instructionInfo = "Signed Minimum.";
                        break;
                    case "sminp":
                        instructionInfo = "Signed Minimum Pairwise.";
                        break;
                    case "sminv":
                        instructionInfo = "Signed Minimum across Vector.";
                        break;
                    case "smlabb":
                    case "smlabt":
                    case "smlatb":
                    case "smlatt":
                        instructionInfo = "Signed Multiply Accumulate performs a signed multiply accumulate operation.";
                        break;
                    case "smlal":
                    case "smlal2":
                        instructionInfo = "Signed Multiply-Add Long (vector, by element).";
                        break;
                    case "smlsl":
                    case "smlsl2":
                        instructionInfo = "Signed Multiply-Subtract Long (vector, by element).";
                        break;
                    case "smov":
                        instructionInfo = "Signed Move vector element to general-purpose register.";
                        break;
                    case "smull2":
                        instructionInfo = "Signed Multiply Long (vector, by element).";
                        break;
                    case "sqdmlal2":
                        instructionInfo = "Signed saturating Doubling Multiply-Add Long.";
                        break;
                    case "sqdmlsl2":
                        instructionInfo = "Signed saturating Doubling Multiply-Subtract Long.";
                        break;
                    case "sqdmull2":
                        instructionInfo = "Signed saturating Doubling Multiply Long.";
                        break;
                    case "sqrshrn2":
                        instructionInfo = "Signed saturating Rounded Shift Right Narrow.";
                        break;
                    case "sqrshrun2":
                        instructionInfo = "Signed saturating Rounded Shift Right Unsigned Narrow.";
                        break;
                    case "sqshrn2":
                        instructionInfo = "Signed saturating Shift Right Narrow.";
                        break;
                    case "sqshrun2":
                        instructionInfo = "Signed saturating Shift Right Unsigned Narrow.";
                        break;
                    case "sqxtn2":
                        instructionInfo = "Signed saturating extract Narrow.";
                        break;
                    case "sqxtun2":
                        instructionInfo = "Signed saturating extract Unsigned Narrow.";
                        break;
                    case "srhadd":
                        instructionInfo = "Signed Rounding Halving Add.";
                        break;
                    case "sshll":
                    case "sshll2":
                        instructionInfo = "Signed Shift Left Long.";
                        break;
                    case "ssubl":
                    case "ssubl2":
                        instructionInfo = "Signed Subtract Long.";
                        break;
                    case "ssubw":
                    case "ssubw2":
                        instructionInfo = "Signed Subtract Wide.";
                        break;
                    case "st1":
                        instructionInfo = "Store multiple single-element structures from one, two, three, or four registers.";
                        break;
                    case "st2":
                        instructionInfo = "Store multiple 2-element structures from two registers.";
                        break;
                    case "st3":
                        instructionInfo = "Store multiple 3-element structures from three registers.";
                        break;
                    case "st4":
                        instructionInfo = "Store multiple 4-element structures from four registers.";
                        break;
                    case "subhn":
                    case "subhn2":
                        instructionInfo = "Subtract returning High Narrow.";
                        break;
                    case "tbl":
                        instructionInfo = "Table vector Lookup.";
                        break;
                    case "tbx":
                        instructionInfo = "Table vector lookup extension.";
                        break;
                    case "tbb":
                        instructionInfo = "PC-relative forward branch using table of single byte offsets.";
                        break;
                    case "tbh":
                        instructionInfo = "PC-relative forward branch using table of halfword offsets.";
                        break;
                    case "trn1":
                        instructionInfo = "Transpose vectors.";
                        break;
                    case "trn2":
                        instructionInfo = "Transpose vectors.";
                        break;
                    case "uaba":
                        instructionInfo = "Unsigned Absolute difference and Accumulate.";
                        break;
                    case "uabal":
                    case "uabal2":
                        instructionInfo = "Unsigned Absolute difference and Accumulate Long.";
                        break;
                    case "uabd":
                        instructionInfo = "Unsigned Absolute Difference.";
                        break;
                    case "uabdl":
                    case "uabdl2":
                        instructionInfo = "Unsigned Absolute Difference Long.";
                        break;
                    case "uadalp":
                        instructionInfo = "Unsigned Add and Accumulate Long Pairwise.";
                        break;
                    case "uaddl":
                    case "uaddl2":
                        instructionInfo = "Unsigned Add Long.";
                        break;
                    case "uaddlp":
                        instructionInfo = "Unsigned Add Long Pairwise.";
                        break;
                    case "uaddlv":
                        instructionInfo = "Unsigned sum Long across Vector.";
                        break;
                    case "uaddw":
                    case "uaddw2":
                        instructionInfo = "Unsigned Add Wide.";
                        break;
                    case "uhadd":
                        instructionInfo = "Unsigned Halving Add.";
                        break;
                    case "uhsub":
                        instructionInfo = "Unsigned Halving Subtract.";
                        break;
                    case "umax":
                        instructionInfo = "Unsigned Maximum.";
                        break;
                    case "umaxp":
                        instructionInfo = "Unsigned Maximum Pairwise.";
                        break;
                    case "umaxv":
                        instructionInfo = "Unsigned Maximum across Vector.";
                        break;
                    case "umin":
                        instructionInfo = "Unsigned Minimum.";
                        break;
                    case "uminp":
                        instructionInfo = "Unsigned Minimum Pairwise.";
                        break;
                    case "uminv":
                        instructionInfo = "Unsigned Minimum across Vector.";
                        break;
                    case "umlal":
                    case "umlal2":
                        instructionInfo = "Unsigned Multiply-Add Long (vector, by element).";
                        break;
                    case "umlsl":
                    case "umlsl2":
                        instructionInfo = "Unsigned Multiply-Subtract Long (vector, by element).";
                        break;
                    case "umov":
                        instructionInfo = "Unsigned Move vector element to general-purpose register.";
                        break;
                    case "umull2":
                        instructionInfo = "Unsigned Multiply Long (vector, by element).";
                        break;
                    case "uqrshrn2":
                        instructionInfo = "Unsigned saturating Rounded Shift Right Narrow.";
                        break;
                    case "uqshrn2":
                        instructionInfo = "Unsigned saturating Shift Right Narrow.";
                        break;
                    case "uqxtn2":
                        instructionInfo = "Unsigned saturating extract Narrow.";
                        break;
                    case "urecpe":
                        instructionInfo = "Unsigned Reciprocal Estimate.";
                        break;
                    case "urhadd":
                        instructionInfo = "Unsigned Rounding Halving Add.";
                        break;
                    case "ursqrte":
                        instructionInfo = "Unsigned Reciprocal Square Root Estimate.";
                        break;
                    case "ushll":
                    case "ushll2":
                        instructionInfo = "Unsigned Shift Left Long.";
                        break;
                    case "usubl":
                    case "usubl2":
                        instructionInfo = "Unsigned Subtract Long.";
                        break;
                    case "usubw":
                    case "usubw2":
                        instructionInfo = "Unsigned Subtract Wide.";
                        break;
                    case "uzp1":
                        instructionInfo = "Unzip vectors.";
                        break;
                    case "uzp2":
                        instructionInfo = "Unzip vectors.";
                        break;
                    case "xtn":
                    case "xtn2":
                        instructionInfo = "Extract Narrow.";
                        break;
                    case "zip1":
                        instructionInfo = "Zip vectors.";
                        break;
                    case "zip2":
                        instructionInfo = "Zip vectors.";
                        break;
                    case "aesd":
                        instructionInfo = "AES single round decryption.";
                        break;
                    case "aese":
                        instructionInfo = "AES single round encryption.";
                        break;
                    case "aesimc":
                        instructionInfo = "AES inverse mix columns.";
                        break;
                    case "aesmc":
                        instructionInfo = "AES mix columns.";
                        break;
                    case "sha1c":
                        instructionInfo = "SHA1 hash update.";
                        break;
                    case "sha1h":
                        instructionInfo = "SHA1 fixed rotate.";
                        break;
                    case "sha1m":
                        instructionInfo = "SHA1 hash update.";
                        break;
                    case "sha1p":
                        instructionInfo = "SHA1 hash update.";
                        break;
                    case "sha1su0":
                        instructionInfo = "SHA1 schedule update 0.";
                        break;
                    case "sha1su1":
                        instructionInfo = "SHA1 schedule update 1.";
                        break;
                    case "sha256h2":
                        instructionInfo = "SHA256 hash update.";
                        break;
                    case "sha256h":
                        instructionInfo = "SHA256 hash update.";
                        break;
                    case "sha256su0":
                        instructionInfo = "SHA256 schedule update 0.";
                        break;
                    case "sha256su1":
                        instructionInfo = "SHA256 schedule update 1.";
                        break;
                    case "adcs":
                        instructionInfo = "Add with Carry, setting flags.";
                        break;
                    case "addg":
                        instructionInfo = "Add with Tag.";
                        break;
                    case "ands":
                        instructionInfo = "Bitwise AND (immediate), setting flags.";
                        break;
                    case "asrv":
                        instructionInfo = "Arithmetic Shift Right Variable.";
                        break;
                    case "autda":
                        instructionInfo = "Authenticate Data address, using key A.";
                        break;
                    case "autdza":
                        instructionInfo = "Authenticate Data address, using key A.";
                        break;
                    case "autdb":
                        instructionInfo = "Authenticate Data address, using key B.";
                        break;
                    case "autdzb":
                        instructionInfo = "Authenticate Data address, using key B.";
                        break;
                    case "autia":
                        instructionInfo = "Authenticate Instruction address, using key A.";
                        break;
                    case "autiza":
                        instructionInfo = "Authenticate Instruction address, using key A.";
                        break;
                    case "autia1716":
                        instructionInfo = "Authenticate Instruction address, using key A.";
                        break;
                    case "autiasp":
                        instructionInfo = "Authenticate Instruction address, using key A.";
                        break;
                    case "autiaz":
                        instructionInfo = "Authenticate Instruction address, using key A.";
                        break;
                    case "autib":
                        instructionInfo = "Authenticate Instruction address, using key B.";
                        break;
                    case "autizb":
                        instructionInfo = "Authenticate Instruction address, using key B.";
                        break;
                    case "autib1716":
                        instructionInfo = "Authenticate Instruction address, using key B.";
                        break;
                    case "autibsp":
                        instructionInfo = "Authenticate Instruction address, using key B.";
                        break;
                    case "autibz":
                        instructionInfo = "Authenticate Instruction address, using key B.";
                        break;
                    case "axflag":
                        instructionInfo = "Convert floating-point condition flags from Arm to external format.";
                        break;
                    case "b.cond":
                        instructionInfo = "Branch conditionally.";
                        break;
                    case "bfc":
                        instructionInfo = "Bitfield Clear, leaving other bits unchanged.";
                        break;
                    case "bics":
                        instructionInfo = "Bitwise Bit Clear (shifted register), setting flags.";
                        break;
                    case "blraa":
                        instructionInfo = "Branch with Link to Register, with pointer authentication.";
                        break;
                    case "blraaz":
                        instructionInfo = "Branch with Link to Register, with pointer authentication.";
                        break;
                    case "blrab":
                        instructionInfo = "Branch with Link to Register, with pointer authentication.";
                        break;
                    case "blrabz":
                        instructionInfo = "Branch with Link to Register, with pointer authentication.";
                        break;
                    case "braa":
                        instructionInfo = "Branch to Register, with pointer authentication.";
                        break;
                    case "braaz":
                        instructionInfo = "Branch to Register, with pointer authentication.";
                        break;
                    case "brab":
                        instructionInfo = "Branch to Register, with pointer authentication.";
                        break;
                    case "brabz":
                        instructionInfo = "Branch to Register, with pointer authentication.";
                        break;
                    case "bti":
                        instructionInfo = "Branch Target Identification.";
                        break;
                    case "cinc":
                        instructionInfo = "Conditional Increment.";
                        break;
                    case "cinv":
                        instructionInfo = "Conditional Invert.";
                        break;
                    case "cmpp":
                        instructionInfo = "Compare with Tag.";
                        break;
                    case "cneg":
                        instructionInfo = "Conditional Negate.";
                        break;
                    case "csdb":
                        instructionInfo = "Consumption of Speculative Data Barrier.";
                        break;
                    case "cset":
                        instructionInfo = "Conditional Set.";
                        break;
                    case "csetm":
                        instructionInfo = "Conditional Set Mask.";
                        break;
                    case "eretaa":
                        instructionInfo = "Exception Return, with pointer authentication.";
                        break;
                    case "eretab":
                        instructionInfo = "Exception Return, with pointer authentication.";
                        break;
                    case "esb":
                        instructionInfo = "Error Synchronization Barrier.";
                        break;
                    case "irg":
                        instructionInfo = "Insert Random Tag.";
                        break;
                    case "ldg":
                        instructionInfo = "Load Allocation Tag.";
                        break;
                    case "ldgv":
                        instructionInfo = "Load Allocation Tag.";
                        break;
                    case "lslv":
                        instructionInfo = "Logical Shift Left Variable.";
                        break;
                    case "lsrv":
                        instructionInfo = "Logical Shift Right Variable.";
                        break;
                    case "movl pseudo-instruction":
                        instructionInfo = "Load a register with either a 32-bit or 64-bit immediate value or any address.";
                        break;
                    case "negs":
                        instructionInfo = "Negate, setting flags.";
                        break;
                    case "ngcs":
                        instructionInfo = "Negate with Carry, setting flags.";
                        break;
                    case "pacda":
                        instructionInfo = "Pointer Authentication Code for Data address, using key A.";
                        break;
                    case "pacdza":
                        instructionInfo = "Pointer Authentication Code for Data address, using key A.";
                        break;
                    case "pacdb":
                        instructionInfo = "Pointer Authentication Code for Data address, using key B.";
                        break;
                    case "pacdzb":
                        instructionInfo = "Pointer Authentication Code for Data address, using key B.";
                        break;
                    case "pacga":
                        instructionInfo = "Pointer Authentication Code, using Generic key.";
                        break;
                    case "pacia":
                        instructionInfo = "Pointer Authentication Code for Instruction address, using key A.";
                        break;
                    case "paciza":
                        instructionInfo = "Pointer Authentication Code for Instruction address, using key A.";
                        break;
                    case "pacia1716":
                        instructionInfo = "Pointer Authentication Code for Instruction address, using key A.";
                        break;
                    case "paciasp":
                        instructionInfo = "Pointer Authentication Code for Instruction address, using key A.";
                        break;
                    case "paciaz":
                        instructionInfo = "Pointer Authentication Code for Instruction address, using key A.";
                        break;
                    case "pacib":
                        instructionInfo = "Pointer Authentication Code for Instruction address, using key B.";
                        break;
                    case "pacizb":
                        instructionInfo = "Pointer Authentication Code for Instruction address, using key B.";
                        break;
                    case "pacib1716":
                        instructionInfo = "Pointer Authentication Code for Instruction address, using key B.";
                        break;
                    case "pacibsp":
                        instructionInfo = "Pointer Authentication Code for Instruction address, using key B.";
                        break;
                    case "pacibz":
                        instructionInfo = "Pointer Authentication Code for Instruction address, using key B.";
                        break;
                    case "psb":
                        instructionInfo = "Profiling Synchronization Barrier.";
                        break;
                    case "retaa":
                        instructionInfo = "Return from subroutine, with pointer authentication.";
                        break;
                    case "retab":
                        instructionInfo = "Return from subroutine, with pointer authentication.";
                        break;
                    case "rorv":
                        instructionInfo = "Rotate Right Variable.";
                        break;
                    case "sbcs":
                        instructionInfo = "Subtract with Carry, setting flags.";
                        break;
                    case "st2g":
                        instructionInfo = "Store Allocation Tags.";
                        break;
                    case "stg":
                        instructionInfo = "Store Allocation Tag.";
                        break;
                    case "stgp":
                        instructionInfo = "Store Allocation Tag and Pair of registers.";
                        break;
                    case "stgv":
                        instructionInfo = "Store Tag Vector.";
                        break;
                    case "stz2g":
                        instructionInfo = "Store Allocation Tags, Zeroing.";
                        break;
                    case "stzg":
                        instructionInfo = "Store Allocation Tag, Zeroing.";
                        break;
                    case "subg":
                        instructionInfo = "Subtract with Tag.";
                        break;
                    case "subp":
                        instructionInfo = "Subtract Pointer.";
                        break;
                    case "subps":
                        instructionInfo = "Subtract Pointer, setting Flags.";
                        break;
                    case "xaflag":
                        instructionInfo = "Convert floating-point condition flags from external format to Arm format.";
                        break;
                    case "xpacd":
                        instructionInfo = "Strip Pointer Authentication Code.";
                        break;
                    case "xpaci":
                        instructionInfo = "Strip Pointer Authentication Code.";
                        break;
                    case "xpaclri":
                        instructionInfo = "Strip Pointer Authentication Code.";
                        break;
                    case "casa":
                        instructionInfo = "Compare and Swap word or doubleword in memory.";
                        break;
                    case "casal":
                        instructionInfo = "Compare and Swap word or doubleword in memory.";
                        break;
                    case "cas":
                        instructionInfo = "Compare and Swap word or doubleword in memory.";
                        break;
                    case "casl":
                        instructionInfo = "Compare and Swap word or doubleword in memory.";
                        break;
                    case "casab":
                        instructionInfo = "Compare and Swap byte in memory.";
                        break;
                    case "casalb":
                        instructionInfo = "Compare and Swap byte in memory.";
                        break;
                    case "casb":
                        instructionInfo = "Compare and Swap byte in memory.";
                        break;
                    case "caslb":
                        instructionInfo = "Compare and Swap byte in memory.";
                        break;
                    case "casah":
                        instructionInfo = "Compare and Swap halfword in memory.";
                        break;
                    case "casalh":
                        instructionInfo = "Compare and Swap halfword in memory.";
                        break;
                    case "cash":
                        instructionInfo = "Compare and Swap halfword in memory.";
                        break;
                    case "caslh":
                        instructionInfo = "Compare and Swap halfword in memory.";
                        break;
                    case "caspa":
                        instructionInfo = "Compare and Swap Pair of words or doublewords in memory.";
                        break;
                    case "caspal":
                        instructionInfo = "Compare and Swap Pair of words or doublewords in memory.";
                        break;
                    case "casp":
                        instructionInfo = "Compare and Swap Pair of words or doublewords in memory.";
                        break;
                    case "caspl":
                        instructionInfo = "Compare and Swap Pair of words or doublewords in memory.";
                        break;
                    case "ldadda":
                        instructionInfo = "Atomic add on word or doubleword in memory.";
                        break;
                    case "ldaddal":
                        instructionInfo = "Atomic add on word or doubleword in memory.";
                        break;
                    case "ldadd":
                        instructionInfo = "Atomic add on word or doubleword in memory.";
                        break;
                    case "ldaddl":
                        instructionInfo = "Atomic add on word or doubleword in memory.";
                        break;
                    case "ldaddab":
                        instructionInfo = "Atomic add on byte in memory.";
                        break;
                    case "ldaddalb":
                        instructionInfo = "Atomic add on byte in memory.";
                        break;
                    case "ldaddb":
                        instructionInfo = "Atomic add on byte in memory.";
                        break;
                    case "ldaddlb":
                        instructionInfo = "Atomic add on byte in memory.";
                        break;
                    case "ldaddah":
                        instructionInfo = "Atomic add on halfword in memory.";
                        break;
                    case "ldaddalh":
                        instructionInfo = "Atomic add on halfword in memory.";
                        break;
                    case "ldaddh":
                        instructionInfo = "Atomic add on halfword in memory.";
                        break;
                    case "ldaddlh":
                        instructionInfo = "Atomic add on halfword in memory.";
                        break;
                    case "ldapr":
                        instructionInfo = "Load-Acquire RCpc Register.";
                        break;
                    case "ldaprb":
                        instructionInfo = "Load-Acquire RCpc Register Byte.";
                        break;
                    case "ldaprh":
                        instructionInfo = "Load-Acquire RCpc Register Halfword.";
                        break;
                    case "ldclra":
                        instructionInfo = "Atomic bit clear on word or doubleword in memory.";
                        break;
                    case "ldclral":
                        instructionInfo = "Atomic bit clear on word or doubleword in memory.";
                        break;
                    case "ldclr":
                        instructionInfo = "Atomic bit clear on word or doubleword in memory.";
                        break;
                    case "ldclrl":
                        instructionInfo = "Atomic bit clear on word or doubleword in memory.";
                        break;
                    case "ldclrab":
                        instructionInfo = "Atomic bit clear on byte in memory.";
                        break;
                    case "ldclralb":
                        instructionInfo = "Atomic bit clear on byte in memory.";
                        break;
                    case "ldclrb":
                        instructionInfo = "Atomic bit clear on byte in memory.";
                        break;
                    case "ldclrlb":
                        instructionInfo = "Atomic bit clear on byte in memory.";
                        break;
                    case "ldclrah":
                        instructionInfo = "Atomic bit clear on halfword in memory.";
                        break;
                    case "ldclralh":
                        instructionInfo = "Atomic bit clear on halfword in memory.";
                        break;
                    case "ldclrh":
                        instructionInfo = "Atomic bit clear on halfword in memory.";
                        break;
                    case "ldclrlh":
                        instructionInfo = "Atomic bit clear on halfword in memory.";
                        break;
                    case "ldeora":
                        instructionInfo = "Atomic exclusive OR on word or doubleword in memory.";
                        break;
                    case "ldeoral":
                        instructionInfo = "Atomic exclusive OR on word or doubleword in memory.";
                        break;
                    case "ldeor":
                        instructionInfo = "Atomic exclusive OR on word or doubleword in memory.";
                        break;
                    case "ldeorl":
                        instructionInfo = "Atomic exclusive OR on word or doubleword in memory.";
                        break;
                    case "ldeorab":
                        instructionInfo = "Atomic exclusive OR on byte in memory.";
                        break;
                    case "ldeoralb":
                        instructionInfo = "Atomic exclusive OR on byte in memory.";
                        break;
                    case "ldeorb":
                        instructionInfo = "Atomic exclusive OR on byte in memory.";
                        break;
                    case "ldeorlb":
                        instructionInfo = "Atomic exclusive OR on byte in memory.";
                        break;
                    case "ldeorah":
                        instructionInfo = "Atomic exclusive OR on halfword in memory.";
                        break;
                    case "ldeoralh":
                        instructionInfo = "Atomic exclusive OR on halfword in memory.";
                        break;
                    case "ldeorh":
                        instructionInfo = "Atomic exclusive OR on halfword in memory.";
                        break;
                    case "ldeorlh":
                        instructionInfo = "Atomic exclusive OR on halfword in memory.";
                        break;
                    case "ldlar":
                        instructionInfo = "Load LOAcquire Register.";
                        break;
                    case "ldlarb":
                        instructionInfo = "Load LOAcquire Register Byte.";
                        break;
                    case "ldlarh":
                        instructionInfo = "Load LOAcquire Register Halfword.";
                        break;
                    case "ldr pseudo-instruction":
                        instructionInfo = "Load a register with either a 32-bit or 64-bit immediate value or any address.";
                        break;
                    case "ldraa":
                        instructionInfo = "Load Register, with pointer authentication.";
                        break;
                    case "ldrab":
                        instructionInfo = "Load Register, with pointer authentication.";
                        break;
                    case "ldrex":
                        instructionInfo = "Load Register Exclusive.";
                        break;
                    case "ldrexd":
                        instructionInfo = "Load Register Doubleword Exclusive.";
                        break;
                    case "strex":
                        instructionInfo = "Store Register Exclusive.";
                        break;
                    case "strexd":
                        instructionInfo = "Store Register Doubleword Exclusive.";
                        break;
                    case "ldseta":
                        instructionInfo = "Atomic bit set on word or doubleword in memory.";
                        break;
                    case "ldsetal":
                        instructionInfo = "Atomic bit set on word or doubleword in memory.";
                        break;
                    case "ldset":
                        instructionInfo = "Atomic bit set on word or doubleword in memory.";
                        break;
                    case "ldsetl":
                        instructionInfo = "Atomic bit set on word or doubleword in memory.";
                        break;
                    case "ldsetab":
                        instructionInfo = "Atomic bit set on byte in memory.";
                        break;
                    case "ldsetalb":
                        instructionInfo = "Atomic bit set on byte in memory.";
                        break;
                    case "ldsetb":
                        instructionInfo = "Atomic bit set on byte in memory.";
                        break;
                    case "ldsetlb":
                        instructionInfo = "Atomic bit set on byte in memory.";
                        break;
                    case "ldsetah":
                        instructionInfo = "Atomic bit set on halfword in memory.";
                        break;
                    case "ldsetalh":
                        instructionInfo = "Atomic bit set on halfword in memory.";
                        break;
                    case "ldseth":
                        instructionInfo = "Atomic bit set on halfword in memory.";
                        break;
                    case "ldsetlh":
                        instructionInfo = "Atomic bit set on halfword in memory.";
                        break;
                    case "ldsmaxa":
                        instructionInfo = "Atomic signed maximum on word or doubleword in memory.";
                        break;
                    case "ldsmaxal":
                        instructionInfo = "Atomic signed maximum on word or doubleword in memory.";
                        break;
                    case "ldsmax":
                        instructionInfo = "Atomic signed maximum on word or doubleword in memory.";
                        break;
                    case "ldsmaxl":
                        instructionInfo = "Atomic signed maximum on word or doubleword in memory.";
                        break;
                    case "ldsmaxab":
                        instructionInfo = "Atomic signed maximum on byte in memory.";
                        break;
                    case "ldsmaxalb":
                        instructionInfo = "Atomic signed maximum on byte in memory.";
                        break;
                    case "ldsmaxb":
                        instructionInfo = "Atomic signed maximum on byte in memory.";
                        break;
                    case "ldsmaxlb":
                        instructionInfo = "Atomic signed maximum on byte in memory.";
                        break;
                    case "ldsmaxah":
                        instructionInfo = "Atomic signed maximum on halfword in memory.";
                        break;
                    case "ldsmaxalh":
                        instructionInfo = "Atomic signed maximum on halfword in memory.";
                        break;
                    case "ldsmaxh":
                        instructionInfo = "Atomic signed maximum on halfword in memory.";
                        break;
                    case "ldsmaxlh":
                        instructionInfo = "Atomic signed maximum on halfword in memory.";
                        break;
                    case "ldsmina":
                        instructionInfo = "Atomic signed minimum on word or doubleword in memory.";
                        break;
                    case "ldsminal":
                        instructionInfo = "Atomic signed minimum on word or doubleword in memory.";
                        break;
                    case "ldsmin":
                        instructionInfo = "Atomic signed minimum on word or doubleword in memory.";
                        break;
                    case "ldsminl":
                        instructionInfo = "Atomic signed minimum on word or doubleword in memory.";
                        break;
                    case "ldsminab":
                        instructionInfo = "Atomic signed minimum on byte in memory.";
                        break;
                    case "ldsminalb":
                        instructionInfo = "Atomic signed minimum on byte in memory.";
                        break;
                    case "ldsminb":
                        instructionInfo = "Atomic signed minimum on byte in memory.";
                        break;
                    case "ldsminlb":
                        instructionInfo = "Atomic signed minimum on byte in memory.";
                        break;
                    case "ldsminah":
                        instructionInfo = "Atomic signed minimum on halfword in memory.";
                        break;
                    case "ldsminalh":
                        instructionInfo = "Atomic signed minimum on halfword in memory.";
                        break;
                    case "ldsminh":
                        instructionInfo = "Atomic signed minimum on halfword in memory.";
                        break;
                    case "ldsminlh":
                        instructionInfo = "Atomic signed minimum on halfword in memory.";
                        break;
                    case "ldumaxa":
                        instructionInfo = "Atomic unsigned maximum on word or doubleword in memory.";
                        break;
                    case "ldumaxal":
                        instructionInfo = "Atomic unsigned maximum on word or doubleword in memory.";
                        break;
                    case "ldumax":
                        instructionInfo = "Atomic unsigned maximum on word or doubleword in memory.";
                        break;
                    case "ldumaxl":
                        instructionInfo = "Atomic unsigned maximum on word or doubleword in memory.";
                        break;
                    case "ldumaxab":
                        instructionInfo = "Atomic unsigned maximum on byte in memory.";
                        break;
                    case "ldumaxalb":
                        instructionInfo = "Atomic unsigned maximum on byte in memory.";
                        break;
                    case "ldumaxb":
                        instructionInfo = "Atomic unsigned maximum on byte in memory.";
                        break;
                    case "ldumaxlb":
                        instructionInfo = "Atomic unsigned maximum on byte in memory.";
                        break;
                    case "ldumaxah":
                        instructionInfo = "Atomic unsigned maximum on halfword in memory.";
                        break;
                    case "ldumaxalh":
                        instructionInfo = "Atomic unsigned maximum on halfword in memory.";
                        break;
                    case "ldumaxh":
                        instructionInfo = "Atomic unsigned maximum on halfword in memory.";
                        break;
                    case "ldumaxlh":
                        instructionInfo = "Atomic unsigned maximum on halfword in memory.";
                        break;
                    case "ldumina":
                        instructionInfo = "Atomic unsigned minimum on word or doubleword in memory.";
                        break;
                    case "lduminal":
                        instructionInfo = "Atomic unsigned minimum on word or doubleword in memory.";
                        break;
                    case "ldumin":
                        instructionInfo = "Atomic unsigned minimum on word or doubleword in memory.";
                        break;
                    case "lduminl":
                        instructionInfo = "Atomic unsigned minimum on word or doubleword in memory.";
                        break;
                    case "lduminab":
                        instructionInfo = "Atomic unsigned minimum on byte in memory.";
                        break;
                    case "lduminalb":
                        instructionInfo = "Atomic unsigned minimum on byte in memory.";
                        break;
                    case "lduminb":
                        instructionInfo = "Atomic unsigned minimum on byte in memory.";
                        break;
                    case "lduminlb":
                        instructionInfo = "Atomic unsigned minimum on byte in memory.";
                        break;
                    case "lduminah":
                        instructionInfo = "Atomic unsigned minimum on halfword in memory.";
                        break;
                    case "lduminalh":
                        instructionInfo = "Atomic unsigned minimum on halfword in memory.";
                        break;
                    case "lduminh":
                        instructionInfo = "Atomic unsigned minimum on halfword in memory.";
                        break;
                    case "lduminlh":
                        instructionInfo = "Atomic unsigned minimum on halfword in memory.";
                        break;
                    case "stadd":
                        instructionInfo = "Atomic add on word or doubleword in memory, without return.";
                        break;
                    case "staddl":
                        instructionInfo = "Atomic add on word or doubleword in memory, without return.";
                        break;
                    case "staddb":
                        instructionInfo = "Atomic add on byte in memory, without return.";
                        break;
                    case "staddlb":
                        instructionInfo = "Atomic add on byte in memory, without return.";
                        break;
                    case "staddh":
                        instructionInfo = "Atomic add on halfword in memory, without return.";
                        break;
                    case "staddlh":
                        instructionInfo = "Atomic add on halfword in memory, without return.";
                        break;
                    case "stclr":
                        instructionInfo = "Atomic bit clear on word or doubleword in memory, without return.";
                        break;
                    case "stclrl":
                        instructionInfo = "Atomic bit clear on word or doubleword in memory, without return.";
                        break;
                    case "stclrb":
                        instructionInfo = "Atomic bit clear on byte in memory, without return.";
                        break;
                    case "stclrlb":
                        instructionInfo = "Atomic bit clear on byte in memory, without return.";
                        break;
                    case "stclrh":
                        instructionInfo = "Atomic bit clear on halfword in memory, without return.";
                        break;
                    case "stclrlh":
                        instructionInfo = "Atomic bit clear on halfword in memory, without return.";
                        break;
                    case "steor":
                        instructionInfo = "Atomic exclusive OR on word or doubleword in memory, without return.";
                        break;
                    case "steorl":
                        instructionInfo = "Atomic exclusive OR on word or doubleword in memory, without return.";
                        break;
                    case "steorb":
                        instructionInfo = "Atomic exclusive OR on byte in memory, without return.";
                        break;
                    case "steorlb":
                        instructionInfo = "Atomic exclusive OR on byte in memory, without return.";
                        break;
                    case "steorh":
                        instructionInfo = "Atomic exclusive OR on halfword in memory, without return.";
                        break;
                    case "steorlh":
                        instructionInfo = "Atomic exclusive OR on halfword in memory, without return.";
                        break;
                    case "stllr":
                        instructionInfo = "Store LORelease Register.";
                        break;
                    case "stllrb":
                        instructionInfo = "Store LORelease Register Byte.";
                        break;
                    case "stllrh":
                        instructionInfo = "Store LORelease Register Halfword.";
                        break;
                    case "stset":
                        instructionInfo = "Atomic bit set on word or doubleword in memory, without return.";
                        break;
                    case "stsetl":
                        instructionInfo = "Atomic bit set on word or doubleword in memory, without return.";
                        break;
                    case "stsetb":
                        instructionInfo = "Atomic bit set on byte in memory, without return.";
                        break;
                    case "stsetlb":
                        instructionInfo = "Atomic bit set on byte in memory, without return.";
                        break;
                    case "stseth":
                        instructionInfo = "Atomic bit set on halfword in memory, without return.";
                        break;
                    case "stsetlh":
                        instructionInfo = "Atomic bit set on halfword in memory, without return.";
                        break;
                    case "stsmax":
                        instructionInfo = "Atomic signed maximum on word or doubleword in memory, without return.";
                        break;
                    case "stsmaxl":
                        instructionInfo = "Atomic signed maximum on word or doubleword in memory, without return.";
                        break;
                    case "stsmaxb":
                        instructionInfo = "Atomic signed maximum on byte in memory, without return.";
                        break;
                    case "stsmaxlb":
                        instructionInfo = "Atomic signed maximum on byte in memory, without return.";
                        break;
                    case "stsmaxh":
                        instructionInfo = "Atomic signed maximum on halfword in memory, without return.";
                        break;
                    case "stsmaxlh":
                        instructionInfo = "Atomic signed maximum on halfword in memory, without return.";
                        break;
                    case "stsmin":
                        instructionInfo = "Atomic signed minimum on word or doubleword in memory, without return.";
                        break;
                    case "stsminl":
                        instructionInfo = "Atomic signed minimum on word or doubleword in memory, without return.";
                        break;
                    case "stsminb":
                        instructionInfo = "Atomic signed minimum on byte in memory, without return.";
                        break;
                    case "stsminlb":
                        instructionInfo = "Atomic signed minimum on byte in memory, without return.";
                        break;
                    case "stsminh":
                        instructionInfo = "Atomic signed minimum on halfword in memory, without return.";
                        break;
                    case "stsminlh":
                        instructionInfo = "Atomic signed minimum on halfword in memory, without return.";
                        break;
                    case "stumax":
                        instructionInfo = "Atomic unsigned maximum on word or doubleword in memory, without return.";
                        break;
                    case "stumaxl":
                        instructionInfo = "Atomic unsigned maximum on word or doubleword in memory, without return.";
                        break;
                    case "stumaxb":
                        instructionInfo = "Atomic unsigned maximum on byte in memory, without return.";
                        break;
                    case "stumaxlb":
                        instructionInfo = "Atomic unsigned maximum on byte in memory, without return.";
                        break;
                    case "stumaxh":
                        instructionInfo = "Atomic unsigned maximum on halfword in memory, without return.";
                        break;
                    case "stumaxlh":
                        instructionInfo = "Atomic unsigned maximum on halfword in memory, without return.";
                        break;
                    case "stumin":
                        instructionInfo = "Atomic unsigned minimum on word or doubleword in memory, without return.";
                        break;
                    case "stuminl":
                        instructionInfo = "Atomic unsigned minimum on word or doubleword in memory, without return.";
                        break;
                    case "stuminb":
                        instructionInfo = "Atomic unsigned minimum on byte in memory, without return.";
                        break;
                    case "stuminlb":
                        instructionInfo = "Atomic unsigned minimum on byte in memory, without return.";
                        break;
                    case "stuminh":
                        instructionInfo = "Atomic unsigned minimum on halfword in memory, without return.";
                        break;
                    case "stuminlh":
                        instructionInfo = "Atomic unsigned minimum on halfword in memory, without return.";
                        break;
                    case "swpa":
                        instructionInfo = "Swap word or doubleword in memory.";
                        break;
                    case "swpal":
                        instructionInfo = "Swap word or doubleword in memory.";
                        break;
                    case "swp":
                        instructionInfo = "Swap word or doubleword in memory.";
                        break;
                    case "swpl":
                        instructionInfo = "Swap word or doubleword in memory.";
                        break;
                    case "swpab":
                        instructionInfo = "Swap byte in memory.";
                        break;
                    case "swpalb":
                        instructionInfo = "Swap byte in memory.";
                        break;
                    case "swpb":
                        instructionInfo = "Swap byte in memory.";
                        break;
                    case "swplb":
                        instructionInfo = "Swap byte in memory.";
                        break;
                    case "swpah":
                        instructionInfo = "Swap halfword in memory.";
                        break;
                    case "swpalh":
                        instructionInfo = "Swap halfword in memory.";
                        break;
                    case "swph":
                        instructionInfo = "Swap halfword in memory.";
                        break;
                    case "swplh":
                        instructionInfo = "Swap halfword in memory.";
                        break;
                    case "fjcvtzs":
                        instructionInfo = "Floating-point Javascript Convert to Signed fixed-point, rounding toward Zero.";
                        break;
                    case "fcmla":
                        instructionInfo = "Floating-point Complex Multiply Accumulate (by element).";
                        break;
                    case "fmlal":
                        instructionInfo = "Floating-point fused Multiply-Add Long to accumulator (by element).";
                        break;
                    case "":
                        instructionInfo = "Floating-point fused Multiply-Add Long to accumulator (by element).";
                        break;
                    case "fmlsl":
                        instructionInfo = "Floating-point fused Multiply-Subtract Long from accumulator (by element).";
                        break;
                    case "sqrdmlah":
                        instructionInfo = "Signed Saturating Rounding Doubling Multiply Accumulate returning High Half (by element).";
                        break;
                    case "sqrdmlsh":
                        instructionInfo = "Signed Saturating Rounding Doubling Multiply Subtract returning High Half (by element).";
                        break;
                    case "fcadd":
                        instructionInfo = "Floating-point Complex Add.";
                        break;
                    case "sdot":
                        instructionInfo = "Dot Product signed arithmetic (vector, by element).";
                        break;
                    case "sxtab":
                        instructionInfo = "Signed extend Byte with Add.";
                        break;
                    case "sxtl":
                        instructionInfo = "Signed extend Long.";
                        break;
                    case "sxtl2":
                        instructionInfo = "Signed extend Long.";
                        break;
                    case "udot":
                        instructionInfo = "Dot Product unsigned arithmetic (vector, by element).";
                        break;
                    case "uxtl":
                        instructionInfo = "Unsigned extend Long.";
                        break;
                    case "uxtl2":
                        instructionInfo = "Unsigned extend Long.";
                        break;
                    case "bcax":
                        instructionInfo = "SHA3 Bit Clear and XOR.";
                        break;
                    case "eor3":
                        instructionInfo = "SHA3 Three-way Exclusive OR.";
                        break;
                    case "rax1":
                        instructionInfo = "SHA3 Rotate and Exclusive OR.";
                        break;
                    case "sha512h2":
                        instructionInfo = "SHA512 Hash update part 2.";
                        break;
                    case "sha512h":
                        instructionInfo = "SHA512 Hash update part 1.";
                        break;
                    case "sha512su0":
                        instructionInfo = "SHA512 Schedule Update 0.";
                        break;
                    case "sha512su1":
                        instructionInfo = "SHA512 Schedule Update 1.";
                        break;
                    case "sm3partw1":
                        instructionInfo = "SM3 three-way exclusive OR on the combination of three 128-bit vectors.";
                        break;
                    case "sm3partw2":
                        instructionInfo = "SM3 three-way exclusive OR on the combination of three 128-bit vectors.";
                        break;
                    case "sm3ss1":
                        instructionInfo = "SM3 perform rotates and adds on three 128-bit vectors combined into a destination 128-bit SIMD and FP register.";
                        break;
                    case "sm3tt1a":
                        instructionInfo = "SM3 three-way exclusive OR on the combination of three 128-bit vectors and a 2-bit immediate index value.";
                        break;
                    case "sm3tt1b":
                        instructionInfo = "SM3 perform 32-bit majority function on the combination of three 128-bit vectors and 2-bit immediate index value.";
                        break;
                    case "sm3tt2a":
                        instructionInfo = "SM3 three-way exclusive OR of combined three 128-bit vectors and a 2-bit immediate index value.";
                        break;
                    case "sm3tt2b":
                        instructionInfo = "SM3 perform 32-bit majority function on the combination of three 128-bit vectors and 2-bit immediate index value.";
                        break;
                    case "sm4e":
                        instructionInfo = "SM4 Encode.";
                        break;
                    case "sm4ekey":
                        instructionInfo = "SM4 Key.";
                        break;
                    case "xar":
                        instructionInfo = "SHA3 Exclusive OR and Rotate.";
                        break;
                    case "vaba":
                        instructionInfo = "Absolute difference and Accumulate, Absolute difference and Accumulate Long.";
                        break;
                    case "vabl":
                        instructionInfo = "Absolute difference and Accumulate, Absolute difference and Accumulate Long.";
                        break;
                    case "vabd":
                        instructionInfo = "Absolute difference, Absolute difference Long.";
                        break;
                    case "vabdl":
                        instructionInfo = "Absolute difference, Absolute difference Long.";
                        break;
                    case "vacge":
                        instructionInfo = "Absolute Compare Greater than or Equal, Greater Than.";
                        break;
                    case "vacgt":
                        instructionInfo = "Absolute Compare Greater than or Equal, Greater Than.";
                        break;
                    case "vacle":
                        instructionInfo = "Absolute Compare Less than or Equal, Less Than (pseudo-instructions).";
                        break;
                    case "vaclt":
                        instructionInfo = "Absolute Compare Less than or Equal, Less Than (pseudo-instructions).";
                        break;
                    case "vaddhn":
                        instructionInfo = "Add, select High half.";
                        break;
                    case "vand":
                        instructionInfo = "Bitwise AND.";
                        break;
                    case "vbic":
                        instructionInfo = "Bitwise Bit Clear (register).";
                        break;
                    case "vbif":
                        instructionInfo = "Bitwise Insert if False.";
                        break;
                    case "vbit":
                        instructionInfo = "Bitwise Insert if True.";
                        break;
                    case "vbsl":
                        instructionInfo = "Bitwise Select.";
                        break;
                    case "vceq":
                        instructionInfo = "Compare Equal.";
                        break;
                    case "vcge":
                        instructionInfo = "Compare Greater than or Equal.";
                        break;
                    case "vcgt":
                        instructionInfo = "Compare Greater Than.";
                        break;
                    case "vcle":
                        instructionInfo = "Compare Less than or Equal.";
                        break;
                    case "vcls":
                        instructionInfo = "Count Leading Sign bits.";
                        break;
                    case "vcnt":
                        instructionInfo = "Count set bits.";
                        break;
                    case "vclt":
                        instructionInfo = "Compare Less Than.";
                        break;
                    case "vclz":
                        instructionInfo = "Count Leading Zeros.";
                        break;
                    case "vcvt":
                        instructionInfo = "Convert fixed-point or integer to floating point, floating-point to integer or fixed-point.";
                        break;
                    case "vdup":
                        instructionInfo = "Duplicate scalar to all lanes of vector.";
                        break;
                    case "veor":
                        instructionInfo = "Bitwise Exclusive OR.";
                        break;
                    case "vext":
                        instructionInfo = "Extract.";
                        break;
                    case "vfma":
                        instructionInfo = "Fused Multiply Accumulate, Fused Multiply Subtract (vector).";
                        break;
                    case "vfms":
                        instructionInfo = "Fused Multiply Accumulate, Fused Multiply Subtract (vector).";
                        break;
                    case "vhadd":
                        instructionInfo = "Halving Add.";
                        break;
                    case "vhsub":
                        instructionInfo = "Halving Subtract.";
                        break;
                    case "vld":
                    case "vld1":
                    case "vld2":
                    case "vld3":
                    case "vld4":
                        instructionInfo = "Vector Load.";
                        break;
                    case "vmax":
                        instructionInfo = "Maximum, Minimum.";
                        break;
                    case "vmin":
                        instructionInfo = "Maximum, Minimum.";
                        break;
                    case "vmla":
                        instructionInfo = "Multiply Accumulate (vector).";
                        break;
                    case "vmls":
                        instructionInfo = "Multiply Subtract (vector).";
                        break;
                    case "vmovl":
                        instructionInfo = "Move Long (register).";
                        break;
                    case "vmovn":
                        instructionInfo = "Move Narrow (register).";
                        break;
                    case "vmvn":
                        instructionInfo = "Move Negative (immediate).";
                        break;
                    case "vneg":
                        instructionInfo = "Negate.";
                        break;
                    case "vorn":
                        instructionInfo = "Bitwise OR NOT.";
                        break;
                    case "vorr":
                        instructionInfo = "Bitwise OR (register).";
                        break;
                    case "vpadal":
                        instructionInfo = "Pairwise Add and Accumulate Long.";
                        break;
                    case "vpadd":
                        instructionInfo = "Pairwise Add.";
                        break;
                    case "vpaddl":
                        instructionInfo = "Pairwise Add Long.";
                        break;
                    case "vpmax":
                        instructionInfo = "Pairwise Maximum, Pairwise Minimum.";
                        break;
                    case "vpmin":
                        instructionInfo = "Pairwise Maximum, Pairwise Minimum.";
                        break;
                    case "vqabs":
                        instructionInfo = "Absolute value, saturate.";
                        break;
                    case "vqadd":
                        instructionInfo = "Add, saturate.";
                        break;
                    case "vqdmlal":
                        instructionInfo = "Saturating Doubling Multiply Accumulate, and Multiply Subtract.";
                        break;
                    case "vqdmlsl":
                        instructionInfo = "Saturating Doubling Multiply Accumulate, and Multiply Subtract.";
                        break;
                    case "vqdmull":
                        instructionInfo = "Saturating Doubling Multiply.";
                        break;
                    case "vqdmulh":
                        instructionInfo = "Saturating Doubling Multiply returning High half.";
                        break;
                    case "vqmovn":
                        instructionInfo = "Saturating Move (register).";
                        break;
                    case "vqneg":
                        instructionInfo = "Negate, saturate.";
                        break;
                    case "vqrdmulh":
                        instructionInfo = "Saturating Doubling Multiply returning High half.";
                        break;
                    case "vqrshl":
                        instructionInfo = "Shift Left, Round, saturate (by signed variable).";
                        break;
                    case "vqrshrn":
                        instructionInfo = "Shift Right, Round, saturate (by immediate).";
                        break;
                    case "vqshl":
                        instructionInfo = "Shift Left, saturate (by immediate).";
                        break;
                    case "vqshrn":
                        instructionInfo = "Shift Right, saturate (by immediate).";
                        break;
                    case "vqsub":
                        instructionInfo = "Subtract, saturate.";
                        break;
                    case "vraddhn":
                        instructionInfo = "Add, select High half, Round.";
                        break;
                    case "vrecpe":
                        instructionInfo = "Reciprocal Estimate.";
                        break;
                    case "vrecps":
                        instructionInfo = "Reciprocal Step.";
                        break;
                    case "vrev":
                    case "vrev16":
                    case "vrev32":
                    case "vrev64":
                        instructionInfo = "Reverse elements.";
                        break;
                    case "vrhadd":
                        instructionInfo = "Halving Add, Round.";
                        break;
                    case "vrshr":
                        instructionInfo = "Shift Right and Round (by immediate).";
                        break;
                    case "vrshrn":
                        instructionInfo = "Shift Right, Round, Narrow (by immediate).";
                        break;
                    case "vrsqrte":
                        instructionInfo = "Reciprocal Square Root Estimate.";
                        break;
                    case "vrsqrts":
                        instructionInfo = "Reciprocal Square Root Step.";
                        break;
                    case "vrsra":
                        instructionInfo = "Shift Right, Round, and Accumulate (by immediate).";
                        break;
                    case "vrsubhn":
                        instructionInfo = "Subtract, select High half, Round.";
                        break;
                    case "vshl":
                        instructionInfo = "Shift Left (by immediate).";
                        break;
                    case "vshr":
                        instructionInfo = "Shift Right (by immediate).";
                        break;
                    case "vshrn":
                        instructionInfo = "Shift Right, Narrow (by immediate).";
                        break;
                    case "vsli":
                        instructionInfo = "Shift Left and Insert.";
                        break;
                    case "vsra":
                        instructionInfo = "Shift Right, Accumulate (by immediate).";
                        break;
                    case "vsri":
                        instructionInfo = "Shift Right and Insert.";
                        break;
                    case "vst":
                    case "vst1":
                    case "vst2":
                    case "vst3":
                    case "vst4":
                        instructionInfo = "Vector Store.";
                        break;
                    case "vsubhn":
                        instructionInfo = "Subtract, select High half.";
                        break;
                    case "vswp":
                        instructionInfo = "Swap vectors.";
                        break;
                    case "vtbl":
                        instructionInfo = "Vector table look-up.";
                        break;
                    case "vtbx":
                        instructionInfo = "Vector table look-up.";
                        break;
                    case "vtrn":
                        instructionInfo = "Vector transpose.";
                        break;
                    case "vtst":
                        instructionInfo = "Test bits.";
                        break;
                    case "vuzp":
                        instructionInfo = "Vector de-interleave.";
                        break;
                    case "vzip":
                        instructionInfo = "Vector interleave.";
                        break;
                    case "vldm":
                    case "vldmia":
                    case "vldmdb":
                        instructionInfo = "Load multiple.";
                        break;
                    case "vldr":
                        instructionInfo = "Load (see also VLDR pseudo-instruction).";
                        break;
                    case "vmrs":
                        instructionInfo = "Transfer from NEON and VFP system register to ARM register.";
                        break;
                    case "vmsr":
                        instructionInfo = "Transfer from ARM register to NEON and VFP system register.";
                        break;
                    case "vpop":
                        instructionInfo = "Pop VFP or NEON registers from full-descending stack.";
                        break;
                    case "vpush":
                        instructionInfo = "Push VFP or NEON registers to full-descending stack.";
                        break;
                    case "vstm":
                    case "vstmia":
                        instructionInfo = "Store multiple.";
                        break;
                    case "vstr":
                        instructionInfo = "Store.";
                        break;
                    case "vcmp":
                        instructionInfo = "Compare.";
                        break;
                    case "vcmpe":
                        instructionInfo = "Compare.";
                        break;
                    case "vcvtb":
                        instructionInfo = "Convert between half-precision and single-precision floating-point.";
                        break;
                    case "vcvtt":
                        instructionInfo = "Convert between half-precision and single-precision floating-point.";
                        break;
                    case "vdiv":
                        instructionInfo = "Divide.";
                        break;
                    case "vfnma":
                        instructionInfo = "Fused multiply accumulate with negation, Fused multiply subtract with negation.";
                        break;
                    case "vfnms":
                        instructionInfo = "Fused multiply accumulate with negation, Fused multiply subtract with negation.";
                        break;
                    case "vnmla":
                        instructionInfo = "Negated multiply accumulate.";
                        break;
                    case "vnmls":
                        instructionInfo = "Negated multiply subtract.";
                        break;
                    case "vnmul":
                        instructionInfo = "Negated multiply.";
                        break;
                    case "vsqrt":
                        instructionInfo = "Square Root.";
                        break;
                    case "push":
                        instructionInfo = "Push registers onto a full descending stack.";
                        break;
                    case "pop":
                        instructionInfo = "Pop registers of a full descending stack.";
                        break;
                    case "pkhbt":
                    case "pkhtb":
                        instructionInfo = "Halfword Packing instructions. Combine a halfword from one register with a halfword from another register.";
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
